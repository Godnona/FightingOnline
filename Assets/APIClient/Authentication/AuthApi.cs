using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ripp.ApiClient.Core;
using UnityEngine.Networking;
using ApiHttpClient = Ripp.ApiClient.Core.ApiClient;

namespace Ripp.ApiClient.Authentication
{
    public sealed class AuthApi : IRefreshTokenService
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

        public async Task<ApiResult<ProviderAuthResponse>> LoginWithAppleAsync(
            string identityToken,
            string deviceInfo = null,
            CancellationToken cancellationToken = default)
        {
            return await LoginWithProviderAsync("/auth/apple", identityToken, deviceInfo, cancellationToken);
        }

        /// <summary>
        /// Returns the browser entry point for Sign in with Apple. In staging test mode,
        /// pass a mock user id; omit it when the real Apple flow is configured.
        /// </summary>
        public string GetAppleWebLoginUrl(
            string returnUrl,
            string codeChallenge,
            string mockUser = null)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                throw new ArgumentException("Apple web login return URL is required.", nameof(returnUrl));
            }

            if (string.IsNullOrWhiteSpace(codeChallenge) ||
                codeChallenge.Length != 43 ||
                !IsBase64Url(codeChallenge))
            {
                throw new ArgumentException(
                    "Apple web login code challenge must be the 43-character base64url SHA-256 of the PKCE verifier.",
                    nameof(codeChallenge));
            }

            string route = "/auth/apple/web?returnUrl=" + Uri.EscapeDataString(returnUrl.Trim()) +
                           "&codeChallenge=" + Uri.EscapeDataString(codeChallenge);
            if (!string.IsNullOrWhiteSpace(mockUser))
            {
                route += "&mockUser=" + Uri.EscapeDataString(mockUser.Trim());
            }

            return client.GetAbsoluteUrl(route);
        }

        /// <summary>
        /// Creates a complete PKCE browser request. Keep codeVerifier until the app receives
        /// the return URL; it must never be sent in the initial browser request.
        /// </summary>
        public AppleWebLoginRequest CreateAppleWebLoginRequest(
            string returnUrl,
            string mockUser = null)
        {
            byte[] verifierBytes = new byte[32];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create())
            {
                random.GetBytes(verifierBytes);
            }

            string codeVerifier = ToBase64Url(verifierBytes);
            byte[] challengeBytes;
            using (SHA256 sha256 = SHA256.Create())
            {
                challengeBytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
            }

            string codeChallenge = ToBase64Url(challengeBytes);
            return new AppleWebLoginRequest
            {
                url = GetAppleWebLoginUrl(returnUrl, codeChallenge, mockUser),
                codeVerifier = codeVerifier,
                codeChallenge = codeChallenge
            };
        }

        public async Task<ApiResult<NoContent>> LogoutAsync(
            CancellationToken cancellationToken = default)
        {
            AuthSession session = tokenStorage.Current;
            if (session == null || string.IsNullOrWhiteSpace(session.refreshToken))
            {
                tokenStorage.Clear();
                return ApiResult<NoContent>.Success(204, default, null, null);
            }

            ApiResult<NoContent> result = await client.SendAsync<NoContent>(
                UnityWebRequest.kHttpVerbPOST,
                "/auth/logout",
                new LogoutRequest { refreshToken = session.refreshToken },
                allowRefreshRetry: false,
                cancellationToken: cancellationToken);

            if (result.IsSuccess)
            {
                tokenStorage.Clear();
            }

            return result;
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

        public Task<ApiResult<IdentityListResponse>> ListIdentitiesAsync(
            CancellationToken cancellationToken = default)
        {
            return client.SendAsync<IdentityListResponse>(
                UnityWebRequest.kHttpVerbGET,
                "/auth/identities",
                requiresAuthentication: true,
                cancellationToken: cancellationToken);
        }

        public Task<ApiResult<IdentityResponse>> LinkIdentityAsync(
            AuthProvider provider,
            string providerToken,
            CancellationToken cancellationToken = default)
        {
            if (provider == AuthProvider.Email)
            {
                throw new ArgumentException("Only Google, Facebook, and Apple can be linked with a provider token.", nameof(provider));
            }

            if (string.IsNullOrWhiteSpace(providerToken))
            {
                throw new ArgumentException("Provider token is required.", nameof(providerToken));
            }

            return client.SendAsync<IdentityResponse>(
                UnityWebRequest.kHttpVerbPOST,
                "/auth/identities/" + ProviderPath(provider),
                new ProviderTokenRequest { token = providerToken.Trim() },
                requiresAuthentication: true,
                cancellationToken: cancellationToken);
        }

        public Task<ApiResult<NoContent>> UnlinkIdentityAsync(
            AuthProvider provider,
            CancellationToken cancellationToken = default)
        {
            return client.SendAsync<NoContent>(
                UnityWebRequest.kHttpVerbDELETE,
                "/auth/identities/" + ProviderPath(provider),
                requiresAuthentication: true,
                cancellationToken: cancellationToken);
        }

        async Task<ApiResult<AuthSession>> IRefreshTokenService.RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken)
        {
            ApiResult<SessionResponse> result = await client.SendAsync<SessionResponse>(
                UnityWebRequest.kHttpVerbPOST,
                "/auth/refresh",
                new RefreshTokenRequest { refreshToken = refreshToken },
                allowRefreshRetry: false,
                cancellationToken: cancellationToken);

            if (!result.IsSuccess)
            {
                return ApiResult<AuthSession>.Failure(
                    result.StatusCode,
                    result.Error,
                    result.RequestId,
                    result.RawBody,
                    result.RetryAfterSeconds);
            }

            if (result.Data == null || result.Data.session == null)
            {
                return ApiResult<AuthSession>.Failure(
                    result.StatusCode,
                    new ApiError
                    {
                        code = ApiErrorCodes.InvalidResponse,
                        message = "The refresh response did not include a session."
                    },
                    result.RequestId,
                    result.RawBody);
            }

            return ApiResult<AuthSession>.Success(
                result.StatusCode,
                result.Data.session,
                result.RequestId,
                result.RawBody);
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

        private static string ProviderPath(AuthProvider provider)
        {
            switch (provider)
            {
                case AuthProvider.Email:
                    return "email";
                case AuthProvider.Google:
                    return "google";
                case AuthProvider.Facebook:
                    return "facebook";
                case AuthProvider.Apple:
                    return "apple";
                default:
                    throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown authentication provider.");
            }
        }

        private static bool IsBase64Url(string value)
        {
            foreach (char character in value)
            {
                bool valid = character == '-' ||
                             character == '_' ||
                             (character >= 'a' && character <= 'z') ||
                             (character >= 'A' && character <= 'Z') ||
                             (character >= '0' && character <= '9');
                if (!valid)
                {
                    return false;
                }
            }

            return true;
        }

        private static string ToBase64Url(byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
