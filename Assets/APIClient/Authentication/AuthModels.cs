using System;

namespace Ripp.ApiClient.Authentication
{
    [Serializable]
    public sealed class PlayerSummary
    {
        public string id;
        public string displayName;
        public string avatar;
        public string role;
    }

    [Serializable]
    public sealed class AuthSession
    {
        public string tokenType;
        public string accessToken;
        public string accessTokenExpiresAt;
        public string refreshToken;
        public string refreshTokenExpiresAt;

        public AuthSession Copy()
        {
            return new AuthSession
            {
                tokenType = tokenType,
                accessToken = accessToken,
                accessTokenExpiresAt = accessTokenExpiresAt,
                refreshToken = refreshToken,
                refreshTokenExpiresAt = refreshTokenExpiresAt
            };
        }
    }

    [Serializable]
    public sealed class AuthResponse
    {
        public PlayerSummary user;
        public AuthSession session;
    }

    [Serializable]
    public sealed class ProviderAuthResponse
    {
        public PlayerSummary user;
        public AuthSession session;
        public bool isNewUser;
    }

    [Serializable]
    internal sealed class EmailPasswordRequest
    {
        public string email;
        public string password;
    }

    [Serializable]
    internal sealed class EmailPasswordDeviceRequest
    {
        public string email;
        public string password;
        public string deviceInfo;
    }

    [Serializable]
    internal sealed class RegisterDisplayNameRequest
    {
        public string email;
        public string password;
        public string displayName;
    }

    [Serializable]
    internal sealed class RegisterDeviceRequest
    {
        public string email;
        public string password;
        public string deviceInfo;
    }

    [Serializable]
    internal sealed class RegisterFullRequest
    {
        public string email;
        public string password;
        public string displayName;
        public string deviceInfo;
    }

    [Serializable]
    internal sealed class EmailRequest
    {
        public string email;
    }

    [Serializable]
    internal sealed class ResetPasswordRequest
    {
        public string email;
        public string code;
        public string newPassword;
    }

    [Serializable]
    internal sealed class ProviderTokenRequest
    {
        public string token;
    }

    [Serializable]
    internal sealed class ProviderTokenDeviceRequest
    {
        public string token;
        public string deviceInfo;
    }
}
