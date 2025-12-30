using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Leadr.Internal
{
    internal class HttpClient
    {
        private readonly string baseUrl;
        private readonly bool debugLogging;

        public HttpClient(string baseUrl, bool debugLogging)
        {
            this.baseUrl = baseUrl.TrimEnd('/');
            this.debugLogging = debugLogging;
        }

        public async Task<HttpResponse> GetAsync(string endpoint, Dictionary<string, string> headers = null)
        {
            var url = BuildUrl(endpoint);
            return await SendRequestAsync("GET", url, null, headers);
        }

        public async Task<HttpResponse> PostAsync(string endpoint, object body, Dictionary<string, string> headers = null)
        {
            var url = BuildUrl(endpoint);
            var jsonBody = body != null ? Json.Serialize(body) : null;
            return await SendRequestAsync("POST", url, jsonBody, headers);
        }

        private string BuildUrl(string endpoint)
        {
            if (endpoint.StartsWith("http"))
                return endpoint;

            var path = endpoint.TrimStart('/');
            return $"{baseUrl}/{path}";
        }

        private async Task<HttpResponse> SendRequestAsync(
            string method,
            string url,
            string jsonBody,
            Dictionary<string, string> headers)
        {
            var startTime = DateTime.UtcNow;

            using (var request = new UnityWebRequest(url, method))
            {
                if (!string.IsNullOrEmpty(jsonBody))
                {
                    var bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
                    request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                }

                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.SetRequestHeader(header.Key, header.Value);
                    }
                }

                if (debugLogging)
                {
                    LogRequest(method, url, jsonBody);
                }

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
                var responseBody = request.downloadHandler?.text;
                var response = new HttpResponse(
                    (int)request.responseCode,
                    responseBody,
                    request.result == UnityWebRequest.Result.Success ||
                    request.result == UnityWebRequest.Result.ProtocolError);

                if (debugLogging)
                {
                    LogResponse(response.StatusCode, elapsed, request.result, responseBody);
                }

                return response;
            }
        }

        private void LogRequest(string method, string url, string jsonBody)
        {
            var logMessage = $"[LEADR] → {method} {url}";

            if (!string.IsNullOrEmpty(jsonBody))
            {
                // Sanitize sensitive fields from request body
                var sanitizedBody = SanitizeJsonForLogging(jsonBody);
                logMessage += $"\n  Body: {sanitizedBody}";
            }

            Debug.Log(logMessage);
        }

        private void LogResponse(int statusCode, double elapsedMs, UnityWebRequest.Result result, string responseBody)
        {
            var status = statusCode > 0 ? statusCode.ToString() : result.ToString();
            var logMessage = $"[LEADR] ← {status} ({elapsedMs:F0}ms)";

            if (!string.IsNullOrEmpty(responseBody))
            {
                // Truncate long responses and sanitize tokens
                var sanitizedBody = SanitizeJsonForLogging(responseBody);
                if (sanitizedBody.Length > 500)
                {
                    sanitizedBody = sanitizedBody.Substring(0, 500) + "...";
                }
                logMessage += $"\n  Body: {sanitizedBody}";
            }

            if (statusCode >= 400 || statusCode == 0)
            {
                Debug.LogWarning(logMessage);
            }
            else
            {
                Debug.Log(logMessage);
            }
        }

        private string SanitizeJsonForLogging(string json)
        {
            // Redact sensitive tokens from logs
            var sanitized = System.Text.RegularExpressions.Regex.Replace(
                json,
                @"""(access_token|refresh_token|client_fingerprint)""\s*:\s*""[^""]*""",
                @"""$1"": ""[REDACTED]"""
            );
            return sanitized;
        }
    }

    internal class HttpResponse
    {
        public int StatusCode { get; }
        public string Body { get; }
        public bool IsNetworkSuccess { get; }

        public HttpResponse(int statusCode, string body, bool isNetworkSuccess)
        {
            StatusCode = statusCode;
            Body = body;
            IsNetworkSuccess = isNetworkSuccess;
        }

        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300;

        public Dictionary<string, object> ParseJson()
        {
            if (string.IsNullOrEmpty(Body))
                return null;

            return Json.Deserialize(Body) as Dictionary<string, object>;
        }

        public LeadrError ToError()
        {
            if (StatusCode == 0)
                return new LeadrError(0, "network_error", "Network request failed");

            return LeadrError.FromJson(StatusCode, Body);
        }
    }
}
