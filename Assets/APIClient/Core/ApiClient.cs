using System;
using System.Threading;
using System.Threading.Tasks;
using Ripp.ApiClient.Authentication;
using Ripp.ApiClient.Configuration;

namespace Ripp.ApiClient.Core
{
    public sealed class ApiClient
    {
        private readonly string baseUrl;
        private readonly int timeoutSeconds;
        private readonly IApiTransport transport;
        private readonly IJsonSerializer jsonSerializer;
        private readonly ITokenStorage tokenStorage;
        private IRefreshCoordinator refreshCoordinator;

        public ApiClient(
            ApiClientSettings settings,
            ITokenStorage tokenStorage,
            IApiTransport transport = null,
            IJsonSerializer jsonSerializer = null,
            IRefreshCoordinator refreshCoordinator = null)
            : this(
                settings == null ? throw new ArgumentNullException(nameof(settings)) : settings.GetBaseUrl(),
                settings.TimeoutSeconds,
                tokenStorage,
                transport,
                jsonSerializer,
                refreshCoordinator)
        {
        }

        public ApiClient(
            string baseUrl,
            int timeoutSeconds,
            ITokenStorage tokenStorage,
            IApiTransport transport = null,
            IJsonSerializer jsonSerializer = null,
            IRefreshCoordinator refreshCoordinator = null)
        {
            this.baseUrl = string.IsNullOrWhiteSpace(baseUrl)
                ? throw new ArgumentException("API base URL is required.", nameof(baseUrl))
                : baseUrl.TrimEnd('/');
            this.timeoutSeconds = Math.Max(1, timeoutSeconds);
            this.tokenStorage = tokenStorage ?? throw new ArgumentNullException(nameof(tokenStorage));
            this.transport = transport ?? new UnityWebRequestTransport();
            this.jsonSerializer = jsonSerializer ?? new UnityJsonSerializer();
            this.refreshCoordinator = refreshCoordinator;
        }

        public void ConfigureRefreshCoordinator(IRefreshCoordinator coordinator)
        {
            refreshCoordinator = coordinator;
        }

        public Task<ApiResult<TResponse>> SendAsync<TResponse>(
            string method,
            string route,
            object body = null,
            bool requiresAuthentication = false,
            bool allowRefreshRetry = true,
            CancellationToken cancellationToken = default)
        {
            string jsonBody = body == null ? null : jsonSerializer.Serialize(body);
            return SendJsonAsync<TResponse>(
                method,
                route,
                jsonBody,
                requiresAuthentication,
                allowRefreshRetry,
                cancellationToken);
        }

        public async Task<ApiResult<TResponse>> SendJsonAsync<TResponse>(
            string method,
            string route,
            string jsonBody,
            bool requiresAuthentication = false,
            bool allowRefreshRetry = true,
            CancellationToken cancellationToken = default)
        {
            var request = new ApiRequest(
                method,
                route,
                jsonBody,
                requiresAuthentication,
                allowRefreshRetry);

            AuthSession session = tokenStorage.Current;
            string accessToken = session == null ? null : session.accessToken;
            ApiRawResponse response = await transport.SendAsync(
                baseUrl,
                request,
                accessToken,
                timeoutSeconds,
                cancellationToken);

            if (ShouldRefresh(request, response) && refreshCoordinator != null)
            {
                RefreshOutcome refresh = await refreshCoordinator.RefreshIfNeededAsync(
                    accessToken,
                    cancellationToken);

                if (!refresh.IsSuccess)
                {
                    return ApiResult<TResponse>.Failure(
                        refresh.StatusCode,
                        refresh.Error,
                        refresh.RequestId,
                        refresh.RawBody);
                }

                session = tokenStorage.Current;
                string newAccessToken = session == null ? null : session.accessToken;
                var retryRequest = new ApiRequest(method, route, jsonBody, requiresAuthentication, false);
                response = await transport.SendAsync(
                    baseUrl,
                    retryRequest,
                    newAccessToken,
                    timeoutSeconds,
                    cancellationToken);
            }

            return ConvertResponse<TResponse>(response);
        }

        private bool ShouldRefresh(ApiRequest request, ApiRawResponse response)
        {
            if (!request.RequiresAuthentication || !request.AllowRefreshRetry || response.IsTransportError)
            {
                return false;
            }

            ApiError error = TryParseError(response.Body);
            return error != null && error.code == ApiErrorCodes.AccessTokenExpired;
        }

        private ApiResult<TResponse> ConvertResponse<TResponse>(ApiRawResponse response)
        {
            if (response.IsTransportError)
            {
                return ApiResult<TResponse>.Failure(
                    response.StatusCode,
                    new ApiError { code = ApiErrorCodes.NetworkError, message = response.TransportError },
                    response.RequestId,
                    response.Body);
            }

            if (!response.IsSuccessStatusCode)
            {
                ApiError error = TryParseError(response.Body) ?? new ApiError
                {
                    code = ApiErrorCodes.InvalidResponse,
                    message = "The server returned an error that does not match the API error contract."
                };

                return ApiResult<TResponse>.Failure(
                    response.StatusCode,
                    error,
                    response.RequestId,
                    response.Body,
                    response.RetryAfterSeconds);
            }

            if (typeof(TResponse) == typeof(NoContent) || string.IsNullOrWhiteSpace(response.Body))
            {
                return ApiResult<TResponse>.Success(
                    response.StatusCode,
                    default,
                    response.RequestId,
                    response.Body);
            }

            try
            {
                TResponse data = jsonSerializer.Deserialize<TResponse>(response.Body);
                return ApiResult<TResponse>.Success(
                    response.StatusCode,
                    data,
                    response.RequestId,
                    response.Body);
            }
            catch (Exception exception)
            {
                return ApiResult<TResponse>.Failure(
                    response.StatusCode,
                    new ApiError { code = ApiErrorCodes.InvalidResponse, message = exception.Message },
                    response.RequestId,
                    response.Body);
            }
        }

        private ApiError TryParseError(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            try
            {
                ApiError error = jsonSerializer.Deserialize<ApiError>(body);
                return error == null || string.IsNullOrWhiteSpace(error.code) ? null : error;
            }
            catch
            {
                return null;
            }
        }
    }
}
