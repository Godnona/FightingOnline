using System;
using Ripp.ApiClient.Authentication;
using Ripp.ApiClient.Configuration;
using Ripp.ApiClient.Core;
using Ripp.ApiClient.Profile;
using ApiHttpClient = Ripp.ApiClient.Core.ApiClient;

namespace Ripp.ApiClient
{


    /// Composition root for game code. Create one instance and keep it across scenes.
    public sealed class ApiClientContext : IDisposable
    {
        private RefreshCoordinator refreshCoordinator;

        public ITokenStorage TokenStorage { get; }
        public ApiHttpClient Client { get; }
        public AuthApi Auth { get; }
        public ProfileApi Profile { get; }

        public event Action SessionInvalidated;

        public ApiClientContext(
            ApiClientSettings settings,
            ITokenStorage tokenStorage = null,
            IApiTransport transport = null,
            IJsonSerializer jsonSerializer = null)
        {
            TokenStorage = tokenStorage ?? new InMemoryTokenStorage();
            Client = new ApiHttpClient(settings, TokenStorage, transport, jsonSerializer);
            Auth = new AuthApi(Client, TokenStorage);
            Profile = new ProfileApi(Client);
        }

  
        /// Call this after implementing the documented /auth/refresh request contract.
        public void ConfigureRefresh(IRefreshTokenService refreshTokenService)
        {
            if (refreshTokenService == null)
            {
                throw new ArgumentNullException(nameof(refreshTokenService));
            }

            if (refreshCoordinator != null)
            {
                refreshCoordinator.SessionInvalidated -= OnSessionInvalidated;
                refreshCoordinator.Dispose();
            }

            refreshCoordinator = new RefreshCoordinator(TokenStorage, refreshTokenService);
            refreshCoordinator.SessionInvalidated += OnSessionInvalidated;
            Client.ConfigureRefreshCoordinator(refreshCoordinator);
        }

        public void Dispose()
        {
            Client.ConfigureRefreshCoordinator(null);
            if (refreshCoordinator != null)
            {
                refreshCoordinator.SessionInvalidated -= OnSessionInvalidated;
                refreshCoordinator.Dispose();
                refreshCoordinator = null;
            }
        }

        private void OnSessionInvalidated()
        {
            SessionInvalidated?.Invoke();
        }
    }
}
