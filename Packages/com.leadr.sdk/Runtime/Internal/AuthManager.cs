using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Leadr.Models;
using UnityEngine;

namespace Leadr.Internal
{
    internal class AuthManager
    {
        private readonly HttpClient httpClient;
        private readonly string gameId;
        private readonly bool debugLogging;
        private readonly SemaphoreSlim refreshLock = new SemaphoreSlim(1, 1);

        private static readonly TimeSpan RefreshThreshold = TimeSpan.FromMinutes(2);

        public AuthManager(HttpClient httpClient, string gameId, bool debugLogging)
        {
            this.httpClient = httpClient;
            this.gameId = gameId;
            this.debugLogging = debugLogging;
        }

        public async Task<LeadrResult<Session>> StartSessionAsync()
        {
            var fingerprint = TokenStorage.GetOrCreateFingerprint();

            var body = new Dictionary<string, object>
            {
                { "game_id", gameId },
                { "client_fingerprint", fingerprint }
            };

            var response = await httpClient.PostAsync("/v1/client/sessions", body);

            if (!response.IsSuccess)
            {
                return LeadrResult<Session>.Failure(response.ToError());
            }

            var json = response.ParseJson();
            if (json == null)
            {
                return LeadrResult<Session>.Failure(0, "parse_error", "Failed to parse session response");
            }

            var session = Session.FromJson(json);
            if (session == null)
            {
                return LeadrResult<Session>.Failure(0, "parse_error", "Failed to parse session");
            }

            var expiresAt = DateTime.UtcNow.AddSeconds(session.ExpiresIn);
            TokenStorage.SaveTokens(session.AccessToken, session.RefreshToken, expiresAt);

            if (debugLogging)
            {
                Debug.Log("[LEADR] Session started");
            }

            return LeadrResult<Session>.Success(session);
        }

        public async Task<LeadrResult<bool>> RefreshTokenAsync()
        {
            var refreshToken = TokenStorage.GetRefreshToken();
            if (string.IsNullOrEmpty(refreshToken))
            {
                return LeadrResult<bool>.Failure(0, "no_refresh_token", "No refresh token available");
            }

            var headers = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {refreshToken}" }
            };

            var response = await httpClient.PostAsync("/v1/client/sessions/refresh", null, headers);

            if (!response.IsSuccess)
            {
                TokenStorage.ClearTokens();
                return LeadrResult<bool>.Failure(response.ToError());
            }

            var json = response.ParseJson();
            if (json == null)
            {
                return LeadrResult<bool>.Failure(0, "parse_error", "Failed to parse refresh response");
            }

            var accessToken = json.GetString("access_token");
            var newRefreshToken = json.GetString("refresh_token");
            var expiresIn = json.GetInt("expires_in");

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(newRefreshToken))
            {
                return LeadrResult<bool>.Failure(0, "parse_error", "Missing tokens in refresh response");
            }

            var expiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
            TokenStorage.SaveTokens(accessToken, newRefreshToken, expiresAt);

            if (debugLogging)
            {
                Debug.Log("[LEADR] Token refreshed");
            }

            return LeadrResult<bool>.Success(true);
        }

        public async Task<LeadrResult<string>> GetNonceAsync()
        {
            var accessToken = await GetValidAccessTokenAsync();
            if (accessToken == null)
            {
                return LeadrResult<string>.Failure(0, "not_authenticated", "Not authenticated");
            }

            var headers = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {accessToken}" }
            };

            var response = await httpClient.GetAsync("/v1/client/nonce", headers);

            if (!response.IsSuccess)
            {
                return LeadrResult<string>.Failure(response.ToError());
            }

            var json = response.ParseJson();
            var nonce = json?.GetString("nonce_value");

            if (string.IsNullOrEmpty(nonce))
            {
                return LeadrResult<string>.Failure(0, "parse_error", "Failed to parse nonce");
            }

            return LeadrResult<string>.Success(nonce);
        }

        public async Task<string> GetValidAccessTokenAsync()
        {
            await EnsureAuthenticatedAsync();
            return TokenStorage.GetAccessToken();
        }

        public async Task EnsureAuthenticatedAsync()
        {
            if (!TokenStorage.HasValidToken())
            {
                var refreshToken = TokenStorage.GetRefreshToken();
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    await SafeRefreshAsync();
                    if (TokenStorage.HasValidToken())
                        return;
                }

                await StartSessionAsync();
                return;
            }

            if (TokenStorage.IsTokenExpiringSoon(RefreshThreshold))
            {
                await SafeRefreshAsync();
            }
        }

        private async Task SafeRefreshAsync()
        {
            await refreshLock.WaitAsync();
            try
            {
                if (!TokenStorage.IsTokenExpiringSoon(RefreshThreshold))
                    return;

                var result = await RefreshTokenAsync();
                if (!result.IsSuccess)
                {
                    await StartSessionAsync();
                }
            }
            finally
            {
                refreshLock.Release();
            }
        }

        public async Task<LeadrResult<T>> ExecuteAuthenticatedAsync<T>(
            Func<Dictionary<string, string>, Task<HttpResponse>> request,
            Func<Dictionary<string, object>, T> parser,
            bool requiresNonce = false)
        {
            await EnsureAuthenticatedAsync();

            var accessToken = TokenStorage.GetAccessToken();
            if (string.IsNullOrEmpty(accessToken))
            {
                return LeadrResult<T>.Failure(0, "not_authenticated", "Failed to authenticate");
            }

            var headers = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {accessToken}" }
            };

            if (requiresNonce)
            {
                var nonceResult = await GetNonceAsync();
                if (!nonceResult.IsSuccess)
                {
                    return LeadrResult<T>.Failure(nonceResult.Error);
                }
                headers["leadr-client-nonce"] = nonceResult.Data;
            }

            var response = await request(headers);

            if (response.StatusCode == 401)
            {
                var refreshResult = await SafeRefreshAndRetryAsync(request, headers, requiresNonce);
                if (refreshResult != null)
                    response = refreshResult;
            }

            if (response.StatusCode == 412 && requiresNonce)
            {
                var nonceResult = await GetNonceAsync();
                if (nonceResult.IsSuccess)
                {
                    headers["leadr-client-nonce"] = nonceResult.Data;
                    response = await request(headers);
                }
            }

            if (!response.IsSuccess)
            {
                return LeadrResult<T>.Failure(response.ToError());
            }

            var json = response.ParseJson();
            if (json == null)
            {
                return LeadrResult<T>.Failure(0, "parse_error", "Failed to parse response");
            }

            var data = parser(json);
            if (data == null)
            {
                return LeadrResult<T>.Failure(0, "parse_error", "Failed to parse data");
            }

            return LeadrResult<T>.Success(data);
        }

        private async Task<HttpResponse> SafeRefreshAndRetryAsync(
            Func<Dictionary<string, string>, Task<HttpResponse>> request,
            Dictionary<string, string> headers,
            bool requiresNonce)
        {
            await refreshLock.WaitAsync();
            try
            {
                var refreshResult = await RefreshTokenAsync();
                if (!refreshResult.IsSuccess)
                {
                    var sessionResult = await StartSessionAsync();
                    if (!sessionResult.IsSuccess)
                        return null;
                }

                var newToken = TokenStorage.GetAccessToken();
                if (string.IsNullOrEmpty(newToken))
                    return null;

                headers["Authorization"] = $"Bearer {newToken}";

                if (requiresNonce)
                {
                    var nonceResult = await GetNonceAsync();
                    if (nonceResult.IsSuccess)
                    {
                        headers["leadr-client-nonce"] = nonceResult.Data;
                    }
                }

                return await request(headers);
            }
            finally
            {
                refreshLock.Release();
            }
        }
    }
}
