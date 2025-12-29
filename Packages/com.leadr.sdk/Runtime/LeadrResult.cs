using System.Collections.Generic;
using Leadr.Internal;

namespace Leadr
{
    public class LeadrResult<T>
    {
        public bool IsSuccess { get; private set; }
        public T Data { get; private set; }
        public LeadrError Error { get; private set; }

        private LeadrResult() { }

        public static LeadrResult<T> Success(T data)
        {
            return new LeadrResult<T>
            {
                IsSuccess = true,
                Data = data,
                Error = null
            };
        }

        public static LeadrResult<T> Failure(LeadrError error)
        {
            return new LeadrResult<T>
            {
                IsSuccess = false,
                Data = default,
                Error = error
            };
        }

        public static LeadrResult<T> Failure(int statusCode, string code, string message)
        {
            return Failure(new LeadrError(statusCode, code, message));
        }
    }

    public class LeadrError
    {
        public int StatusCode { get; private set; }
        public string Code { get; private set; }
        public string Message { get; private set; }

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

        public override string ToString()
        {
            return $"[{StatusCode}] {Code}: {Message}";
        }
    }
}
