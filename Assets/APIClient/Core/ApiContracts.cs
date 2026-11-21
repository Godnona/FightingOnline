using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Ripp.ApiClient.Core
{
    public static class ApiErrorCodes
    {
        // Client-side errors. These codes are not returned by the backend.
        public const string NetworkError = "NETWORK_ERROR";
        public const string InvalidResponse = "INVALID_RESPONSE";

        // 400 Bad Request
        public const string ValidationError = "VALIDATION_ERROR";
        public const string InvalidPasswordResetCode = "INVALID_PASSWORD_RESET_CODE";
        public const string InvalidAvatar = "INVALID_AVATAR";

        // 401 Unauthorized
        public const string InvalidCredentials = "INVALID_CREDENTIALS";
        public const string AuthenticationRequired = "AUTHENTICATION_REQUIRED";
        public const string InvalidAccessToken = "INVALID_ACCESS_TOKEN";
        public const string AccessTokenExpired = "ACCESS_TOKEN_EXPIRED";
        public const string InvalidRefreshToken = "INVALID_REFRESH_TOKEN";
        public const string InvalidProviderToken = "INVALID_PROVIDER_TOKEN";

        // 403 Forbidden
        public const string AccountBanned = "ACCOUNT_BANNED";
        public const string Forbidden = "FORBIDDEN";

        // 404 Not Found
        public const string IdentityNotFound = "IDENTITY_NOT_FOUND";
        public const string PlayerNotFound = "PLAYER_NOT_FOUND";

        // 409 Conflict
        public const string EmailAlreadyInUse = "EMAIL_ALREADY_IN_USE";
        public const string DisplayNameTaken = "DISPLAY_NAME_TAKEN";
        public const string DisplayNameChangeTooSoon = "DISPLAY_NAME_CHANGE_TOO_SOON";
        public const string IdentityAlreadyLinked = "IDENTITY_ALREADY_LINKED";
        public const string ProviderAlreadyLinked = "PROVIDER_ALREADY_LINKED";
        public const string LastIdentity = "LAST_IDENTITY";

        // 429 Too Many Requests
        public const string RateLimited = "RATE_LIMITED";

        // 503 Service Unavailable
        public const string ProviderNotConfigured = "PROVIDER_NOT_CONFIGURED";
        public const string ProviderUnavailable = "PROVIDER_UNAVAILABLE";
    }

    [Serializable]
    public sealed class ApiError
    {
        public string code;
        public string message;
        public ApiErrorDetails details;
    }

    [Serializable]
    public sealed class ApiErrorDetails
    {
        public string location;
        public string nextChangeAt;
        public ApiValidationIssue[] issues;
    }

    [Serializable]
    public sealed class ApiValidationIssue
    {
        public string path;
        public string message;
    }

    public sealed class ApiResult<T>
    {
        public bool IsSuccess { get; private set; }
        public long StatusCode { get; private set; }
        public T Data { get; private set; }
        public ApiError Error { get; private set; }
        public string RequestId { get; private set; }
        public int? RetryAfterSeconds { get; private set; }
        public string RawBody { get; private set; }

        public static ApiResult<T> Success(long statusCode, T data, string requestId, string rawBody)
        {
            return new ApiResult<T>
            {
                IsSuccess = true,
                StatusCode = statusCode,
                Data = data,
                RequestId = requestId,
                RawBody = rawBody
            };
        }

        public static ApiResult<T> Failure(
            long statusCode,
            ApiError error,
            string requestId,
            string rawBody,
            int? retryAfterSeconds = null)
        {
            return new ApiResult<T>
            {
                IsSuccess = false,
                StatusCode = statusCode,
                Error = error,
                RequestId = requestId,
                RawBody = rawBody,
                RetryAfterSeconds = retryAfterSeconds
            };
        }
    }

    public sealed class NoContent
    {
        private NoContent()
        {
        }
    }

    public sealed class ApiRequest
    {
        public string Method { get; }
        public string Route { get; }
        public string JsonBody { get; }
        public bool RequiresAuthentication { get; }
        public bool AllowRefreshRetry { get; }

        public ApiRequest(
            string method,
            string route,
            string jsonBody,
            bool requiresAuthentication,
            bool allowRefreshRetry)
        {
            Method = method ?? throw new ArgumentNullException(nameof(method));
            Route = route ?? throw new ArgumentNullException(nameof(route));
            JsonBody = jsonBody;
            RequiresAuthentication = requiresAuthentication;
            AllowRefreshRetry = allowRefreshRetry;
        }
    }

    public sealed class ApiRawResponse
    {
        public long StatusCode { get; set; }
        public string Body { get; set; }
        public string RequestId { get; set; }
        public int? RetryAfterSeconds { get; set; }
        public string TransportError { get; set; }

        public bool IsSuccessStatusCode => StatusCode >= 200 && StatusCode <= 299;
        public bool IsTransportError => !string.IsNullOrEmpty(TransportError);
    }

    public interface IApiTransport
    {
        Task<ApiRawResponse> SendAsync(
            string baseUrl,
            ApiRequest request,
            string bearerToken,
            int timeoutSeconds,
            CancellationToken cancellationToken);
    }

    public interface IJsonSerializer
    {
        string Serialize(object value);
        T Deserialize<T>(string json);
    }

    public sealed class UnityJsonSerializer : IJsonSerializer
    {
        public string Serialize(object value)
        {
            return value == null ? null : JsonUtility.ToJson(value);
        }

        public T Deserialize<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }
    }
}
