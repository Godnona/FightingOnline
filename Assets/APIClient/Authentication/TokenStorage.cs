using System;

namespace Ripp.ApiClient.Authentication
{
    public interface ITokenStorage
    {
        AuthSession Current { get; }
        void Save(AuthSession session);
        void Clear();
    }

    /// Development-safe default: tokens disappear when the process exits.
    /// Replace this with Keychain/Keystore-backed storage for production builds.
    public sealed class InMemoryTokenStorage : ITokenStorage
    {
        private readonly object syncRoot = new object();
        private AuthSession current;

        public AuthSession Current
        {
            get
            {
                lock (syncRoot)
                {
                    return current == null ? null : current.Copy();
                }
            }
        }

        public void Save(AuthSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (string.IsNullOrWhiteSpace(session.accessToken) || string.IsNullOrWhiteSpace(session.refreshToken))
            {
                throw new ArgumentException("Both access and refresh tokens are required.", nameof(session));
            }

            lock (syncRoot)
            {
                current = session.Copy();
            }
        }

        public void Clear()
        {
            lock (syncRoot)
            {
                current = null;
            }
        }
    }
}
