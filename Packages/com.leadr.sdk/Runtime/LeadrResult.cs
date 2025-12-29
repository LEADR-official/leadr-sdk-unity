using System.Collections.Generic;
using Leadr.Internal;

namespace Leadr
{
    /// <summary>
    /// Wraps the result of a LEADR API operation, containing either data or an error.
    /// </summary>
    /// <typeparam name="T">The type of data on success.</typeparam>
    /// <remarks>
    /// Always check <see cref="IsSuccess"/> before accessing <see cref="Data"/>.
    /// On failure, <see cref="Error"/> contains details about what went wrong.
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await LeadrClient.Instance.GetBoardsAsync();
    /// if (result.IsSuccess)
    /// {
    ///     // Use result.Data
    /// }
    /// else
    /// {
    ///     Debug.LogError(result.Error.Message);
    /// }
    /// </code>
    /// </example>
    public class LeadrResult<T>
    {
        /// <summary>
        /// Gets whether the operation succeeded.
        /// </summary>
        public bool IsSuccess { get; private set; }

        /// <summary>
        /// Gets the result data. Only valid when <see cref="IsSuccess"/> is true.
        /// </summary>
        public T Data { get; private set; }

        /// <summary>
        /// Gets the error details. Only valid when <see cref="IsSuccess"/> is false.
        /// </summary>
        public LeadrError Error { get; private set; }

        private LeadrResult() { }

        /// <summary>
        /// Creates a successful result with the given data.
        /// </summary>
        /// <param name="data">The result data.</param>
        /// <returns>A successful result.</returns>
        public static LeadrResult<T> Success(T data)
        {
            return new LeadrResult<T>
            {
                IsSuccess = true,
                Data = data,
                Error = null
            };
        }

        /// <summary>
        /// Creates a failed result with the given error.
        /// </summary>
        /// <param name="error">The error details.</param>
        /// <returns>A failed result.</returns>
        public static LeadrResult<T> Failure(LeadrError error)
        {
            return new LeadrResult<T>
            {
                IsSuccess = false,
                Data = default,
                Error = error
            };
        }

        /// <summary>
        /// Creates a failed result with the given error details.
        /// </summary>
        /// <param name="statusCode">HTTP status code (0 for network errors).</param>
        /// <param name="code">Error code string.</param>
        /// <param name="message">Human-readable error message.</param>
        /// <returns>A failed result.</returns>
        public static LeadrResult<T> Failure(int statusCode, string code, string message)
        {
            return Failure(new LeadrError(statusCode, code, message));
        }
    }

    /// <summary>
    /// Contains details about an API error.
    /// </summary>
    public class LeadrError
    {
        /// <summary>
        /// Gets the HTTP status code. Zero indicates a network error (no response received).
        /// </summary>
        /// <remarks>
        /// Common status codes:
        /// - 0: Network error (no connection)
        /// - 400: Bad request (invalid data)
        /// - 401: Unauthorized (session expired)
        /// - 404: Not found (invalid ID)
        /// - 412: Precondition failed (nonce issue)
        /// - 429: Too many requests (rate limited)
        /// </remarks>
        public int StatusCode { get; private set; }

        /// <summary>
        /// Gets the error code string (e.g., "api_error", "validation_error", "network_error").
        /// </summary>
        public string Code { get; private set; }

        /// <summary>
        /// Gets the human-readable error message.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Creates a new error with the specified details.
        /// </summary>
        /// <param name="statusCode">HTTP status code (0 for network errors).</param>
        /// <param name="code">Error code string.</param>
        /// <param name="message">Human-readable error message.</param>
        public LeadrError(int statusCode, string code, string message)
        {
            StatusCode = statusCode;
            Code = code;
            Message = message;
        }

        internal static LeadrError FromJson(int statusCode, string json)
        {
            if (string.IsNullOrEmpty(json))
                return new LeadrError(statusCode, "unknown", "Unknown error");

            var parsed = Json.Deserialize(json) as Dictionary<string, object>;
            if (parsed == null)
                return new LeadrError(statusCode, "unknown", json);

            var error = parsed.GetString("error");
            if (error != null)
                return new LeadrError(statusCode, "api_error", error);

            var errorList = parsed.GetList("error");
            if (errorList != null && errorList.Count > 0)
            {
                var firstError = errorList[0] as Dictionary<string, object>;
                if (firstError != null)
                {
                    var msg = firstError.GetString("msg") ?? "Validation error";
                    return new LeadrError(statusCode, "validation_error", msg);
                }
            }

            return new LeadrError(statusCode, "unknown", json);
        }

        /// <summary>
        /// Returns a string representation of the error.
        /// </summary>
        /// <returns>A formatted error string.</returns>
        public override string ToString()
        {
            return $"[{StatusCode}] {Code}: {Message}";
        }
    }
}
