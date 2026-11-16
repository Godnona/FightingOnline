using System;
using System.Threading;
using System.Threading.Tasks;
using Ripp.ApiClient.Core;

namespace Ripp.ApiClient.Authentication
{
    public interface IRefreshTokenService
    {
        Task<ApiResult<AuthSession>> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    }

    public interface IRefreshCoordinator
    {
        Task<RefreshOutcome> RefreshIfNeededAsync(
            string failedAccessToken,
            CancellationToken cancellationToken);
    }

    public sealed class RefreshOutcome
    {
        public bool IsSuccess { get; private set; }
        public long StatusCode { get; private set; }
        public ApiError Error { get; private set; }
        public string RequestId { get; private set; }
        public string RawBody { get; private set; }

        public static RefreshOutcome Success()
        {
            return new RefreshOutcome { IsSuccess = true };
        }

        public static RefreshOutcome Failure(ApiResult<AuthSession> result)
        {
            return new RefreshOutcome
            {
                IsSuccess = false,
                StatusCode = result.StatusCode,
                Error = result.Error,
                RequestId = result.RequestId,
                RawBody = result.RawBody
            };
        }

        public static RefreshOutcome MissingSession()
        {
            return new RefreshOutcome
            {
                IsSuccess = false,
                Error = new ApiError
                {
                    code = ApiErrorCodes.AuthenticationRequired,
                    message = "No refresh token is available."
                }
            };
        }
    }

    /// Serializes refresh calls because the backend rotates refresh tokens after every use.
    public sealed class RefreshCoordinator : IRefreshCoordinator, IDisposable
    {
        private readonly ITokenStorage tokenStorage;
        private readonly IRefreshTokenService refreshTokenService;
        private readonly SemaphoreSlim refreshLock = new SemaphoreSlim(1, 1);

        public event Action SessionInvalidated;

        public RefreshCoordinator(ITokenStorage tokenStorage, IRefreshTokenService refreshTokenService)
        {
            this.tokenStorage = tokenStorage ?? throw new ArgumentNullException(nameof(tokenStorage));
            this.refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
        }

        public async Task<RefreshOutcome> RefreshIfNeededAsync(
            string failedAccessToken,
            CancellationToken cancellationToken)
        {
            AuthSession current = tokenStorage.Current;
            if (HasAlreadyBeenRefreshed(current, failedAccessToken))
            {
                return RefreshOutcome.Success();
            }

            await refreshLock.WaitAsync(cancellationToken);
            try
            {
                current = tokenStorage.Current;
                if (HasAlreadyBeenRefreshed(current, failedAccessToken))
                {
                    return RefreshOutcome.Success();
                }

                if (current == null || string.IsNullOrWhiteSpace(current.refreshToken))
                {
                    InvalidateSession();
                    return RefreshOutcome.MissingSession();
                }

                ApiResult<AuthSession> result =
                    await refreshTokenService.RefreshAsync(current.refreshToken, cancellationToken);

                if (result.IsSuccess && result.Data != null)
                {
                    // Save the rotated access/refresh pair as one operation.
                    tokenStorage.Save(result.Data);
                    return RefreshOutcome.Success();
                }

                if (result.Error != null && result.Error.code == ApiErrorCodes.InvalidRefreshToken)
                {
                    InvalidateSession();
                }

                return RefreshOutcome.Failure(result);
            }
            finally
            {
                refreshLock.Release();
            }
        }

        public void Dispose()
        {
            refreshLock.Dispose();
        }

        private static bool HasAlreadyBeenRefreshed(AuthSession session, string failedAccessToken)
        {
            return session != null &&
                   !string.IsNullOrEmpty(session.accessToken) &&
                   !string.Equals(session.accessToken, failedAccessToken, StringComparison.Ordinal);
        }

        private void InvalidateSession()
        {
            tokenStorage.Clear();
            SessionInvalidated?.Invoke();
        }
    }
}
