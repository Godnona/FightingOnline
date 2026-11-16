using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace Ripp.ApiClient.Core
{
    public sealed class UnityWebRequestTransport : IApiTransport
    {
        public async Task<ApiRawResponse> SendAsync(
            string baseUrl,
            ApiRequest request,
            string bearerToken,
            int timeoutSeconds,
            CancellationToken cancellationToken)
        {
            string url = BuildUrl(baseUrl, request.Route);

            using (var webRequest = new UnityWebRequest(url, request.Method))
            {
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.timeout = Math.Max(1, timeoutSeconds);
                webRequest.SetRequestHeader("Accept", "application/json");
                webRequest.SetRequestHeader("Content-Type", "application/json");

                if (!string.IsNullOrEmpty(request.JsonBody))
                {
                    webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(request.JsonBody));
                    webRequest.uploadHandler.contentType = "application/json";
                }

                if (request.RequiresAuthentication && !string.IsNullOrEmpty(bearerToken))
                {
                    webRequest.SetRequestHeader("Authorization", "Bearer " + bearerToken);
                }

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    UnityWebRequestAsyncOperation operation = webRequest.SendWebRequest();
                    while (!operation.isDone)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.Yield();
                    }
                }
                catch (OperationCanceledException)
                {
                    webRequest.Abort();
                    throw;
                }

                string retryAfterHeader = webRequest.GetResponseHeader("Retry-After");
                int? retryAfterSeconds = null;
                if (int.TryParse(retryAfterHeader, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds))
                {
                    retryAfterSeconds = seconds;
                }

                string transportError = null;
                if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                    webRequest.result == UnityWebRequest.Result.DataProcessingError)
                {
                    transportError = webRequest.error;
                }

                return new ApiRawResponse
                {
                    StatusCode = webRequest.responseCode,
                    Body = webRequest.downloadHandler == null ? null : webRequest.downloadHandler.text,
                    RequestId = webRequest.GetResponseHeader("x-request-id"),
                    RetryAfterSeconds = retryAfterSeconds,
                    TransportError = transportError
                };
            }
        }

        private static string BuildUrl(string baseUrl, string route)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentException("API base URL is required.", nameof(baseUrl));
            }

            if (string.IsNullOrWhiteSpace(route))
            {
                throw new ArgumentException("API route is required.", nameof(route));
            }

            if (Uri.TryCreate(route, UriKind.Absolute, out _))
            {
                throw new ArgumentException("API routes must be relative to the configured base URL.", nameof(route));
            }

            return baseUrl.TrimEnd('/') + "/" + route.TrimStart('/');
        }
    }
}
