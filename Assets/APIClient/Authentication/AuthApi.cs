using System;
using System.Threading;
using System.Threading.Tasks;
using Ripp.ApiClient.Core;
using UnityEngine.Networking;
using ApiHttpClient = Ripp.ApiClient.Core.ApiClient;

namespace Ripp.ApiClient.Authentication
{
    public sealed class AuthApi
    {
        private readonly ApiHttpClient client;
        private readonly ITokenStorage tokenStorage;

        public AuthApi(ApiHttpClient client, ITokenStorage tokenStorage)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.tokenStorage = tokenStorage ?? throw new ArgumentNullException(nameof(tokenStorage));
        }

        public async Task<ApiResult<AuthResponse>> RegisterAsync(
            string email,
            string password,
            string displayName = null,
            string deviceInfo = null,
            CancellationToken cancellationToken = default)
        {
            object body;
            if (!string.IsNullOrWhiteSpace(displayName) && !string.IsNullOrWhiteSpace(deviceInfo))
            {
                body = new RegisterFullRequest
                {
                    email = email,
                    password = password,
                    displayName = displayName,
                    deviceInfo = deviceInfo
                };
            }
            else if (!string.IsNullOrWhiteSpace(displayName))
            {
                body = new RegisterDisplayNameRequest
                {
                    email = email,
                    password = password,
                    displayName = displayName
                };
            }
            else if (!string.IsNullOrWhiteSpace(deviceInfo))
            {
                body = new RegisterDeviceRequest
                {
                    email = email,
                    password = password,
                    deviceInfo = deviceInfo
                };
            }
            else
            {
                body = new EmailPasswordRequest { email = email, password = password };
            }

            ApiResult<AuthResponse> result = await client.SendAsync<AuthResponse>(
                UnityWebRequest.kHttpVerbPOST,
                "/auth/register",
                body,
                cancellationToken: cancellationToken);
            SaveSession(result);
            return result;
        }

        public async Task<ApiResult<AuthResponse>> LoginAsync(
            string email,
            string password,
            string deviceInfo = null,
            CancellationToken cancellationToken = default)
        {
            object body;
            if (string.IsNullOrWhiteSpace(deviceInfo))
            {
                body = new EmailPasswordRequest { email = email, password = password };
            }
            else
            {
                body = new EmailPasswordDeviceRequest
                {
                    email = email,
                    password = password,
                    deviceInfo = deviceInfo
                };
            }

            ApiResult<AuthResponse> result = await client.SendAsync<AuthResponse>(
                UnityWebRequest.kHttpVerbPOST,
                "/auth/login",
                body,
                cancellationToken: cancellationToken);
            SaveSession(result);
            return result;
        }

        public Task<ApiResult<NoContent>> ForgotPasswordAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            return client.SendAsync<NoContent>(
                UnityWebRequest.kHttpVerbPOST,
                "/auth/password/forgot",
                new EmailRequest { email = email },
                cancellationToken: cancellationToken);
        }

        public async Task<ApiResult<NoContent>> ResetPasswordAsync(
            string email,
            string code,
            string newPassword,
            CancellationToken cancellationToken = default)
        {
            ApiResult<NoContent> result = await client.SendAsync<NoContent>(
                UnityWebRequest.kHttpVerbPOST,
                "/auth/password/reset",
                new ResetPasswordRequest { email = email, code = code, newPassword = newPassword },
                cancellationToken: cancellationToken);

            // The backend invalidates every session after a successful password reset.
            if (result.IsSuccess)
            {
                tokenStorage.Clear();
            }

            return result;
        }

        public async Task<ApiResult<ProviderAuthResponse>> LoginWithGoogleAsync(
            string idToken,
            string deviceInfo = null,
            CancellationToken cancellationToken = default)
        {
            return await LoginWithProviderAsync("/auth/google", idToken, deviceInfo, cancellationToken);
        }

        public async Task<ApiResult<ProviderAuthResponse>> LoginWithFacebookAsync(
            string accessToken,
            string deviceInfo = null,
            CancellationToken cancellationToken = default)
        {
            return await LoginWithProviderAsync("/auth/facebook", accessToken, deviceInfo, cancellationToken);
        }

        public async Task<ApiResult<NoContent>> LogoutAllAsync(CancellationToken cancellationToken = default)
        {
            ApiResult<NoContent> result = await client.SendAsync<NoContent>(
                UnityWebRequest.kHttpVerbPOST,
                "/auth/logout-all",
                requiresAuthentication: true,
                cancellationToken: cancellationToken);

            if (result.IsSuccess)
            {
                tokenStorage.Clear();
            }

            return result;
        }

        private async Task<ApiResult<ProviderAuthResponse>> LoginWithProviderAsync(
            string route,
            string providerToken,
            string deviceInfo,
            CancellationToken cancellationToken)
        {
            object body;
            if (string.IsNullOrWhiteSpace(deviceInfo))
            {
                body = new ProviderTokenRequest { token = providerToken };
            }
            else
            {
                body = new ProviderTokenDeviceRequest { token = providerToken, deviceInfo = deviceInfo };
            }

            ApiResult<ProviderAuthResponse> result = await client.SendAsync<ProviderAuthResponse>(
                UnityWebRequest.kHttpVerbPOST,
                route,
                body,
                cancellationToken: cancellationToken);

            if (result.IsSuccess && result.Data != null && result.Data.session != null)
            {
                tokenStorage.Save(result.Data.session);
            }

            return result;
        }

        private void SaveSession(ApiResult<AuthResponse> result)
        {
            if (result.IsSuccess && result.Data != null && result.Data.session != null)
            {
                tokenStorage.Save(result.Data.session);
            }
        }
    }
}
