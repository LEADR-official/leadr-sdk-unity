using System;
using UnityEngine;

namespace Leadr.Internal
{
    internal static class TokenStorage
    {
        private const string FingerprintKey = "leadr_client_fingerprint";
        private const string AccessTokenKey = "leadr_access_token";
        private const string RefreshTokenKey = "leadr_refresh_token";
        private const string ExpiresAtKey = "leadr_token_expires_at";

        public static string GetOrCreateFingerprint()
        {
            var fingerprint = PlayerPrefs.GetString(FingerprintKey, null);
            if (string.IsNullOrEmpty(fingerprint))
            {
                fingerprint = Guid.NewGuid().ToString();
                PlayerPrefs.SetString(FingerprintKey, fingerprint);
                PlayerPrefs.Save();
            }
            return fingerprint;
        }

        public static void SaveTokens(string accessToken, string refreshToken, DateTime expiresAt)
        {
            PlayerPrefs.SetString(AccessTokenKey, accessToken);
            PlayerPrefs.SetString(RefreshTokenKey, refreshToken);
            PlayerPrefs.SetString(ExpiresAtKey, expiresAt.ToString("O"));
            PlayerPrefs.Save();
        }

        public static string GetAccessToken()
        {
            return PlayerPrefs.GetString(AccessTokenKey, null);
        }

        public static string GetRefreshToken()
        {
            return PlayerPrefs.GetString(RefreshTokenKey, null);
        }

        public static DateTime? GetExpiresAt()
        {
            var str = PlayerPrefs.GetString(ExpiresAtKey, null);
            if (string.IsNullOrEmpty(str))
                return null;

            if (DateTime.TryParse(str, out var result))
                return result;

            return null;
        }

        public static bool HasValidToken()
        {
            var accessToken = GetAccessToken();
            var expiresAt = GetExpiresAt();

            if (string.IsNullOrEmpty(accessToken) || !expiresAt.HasValue)
                return false;

            return DateTime.UtcNow < expiresAt.Value;
        }

        public static bool IsTokenExpiringSoon(TimeSpan threshold)
        {
            var expiresAt = GetExpiresAt();
            if (!expiresAt.HasValue)
                return true;

            return DateTime.UtcNow.Add(threshold) >= expiresAt.Value;
        }

        public static void ClearTokens()
        {
            PlayerPrefs.DeleteKey(AccessTokenKey);
            PlayerPrefs.DeleteKey(RefreshTokenKey);
            PlayerPrefs.DeleteKey(ExpiresAtKey);
            PlayerPrefs.Save();
        }

        public static void ClearAll()
        {
            PlayerPrefs.DeleteKey(FingerprintKey);
            ClearTokens();
        }
    }
}
