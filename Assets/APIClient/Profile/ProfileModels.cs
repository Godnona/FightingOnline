using System;

namespace Ripp.ApiClient.Profile
{
    [Serializable]
    public sealed class PlayerProfile
    {
        public string id;
        public string displayName;
        public string avatar;
        public string role;
        public string createdAt;
        public string nextDisplayNameChangeAt;
    }

    [Serializable]
    public sealed class PublicPlayerProfile
    {
        public string id;
        public string displayName;
        public string avatar;
    }

    [Serializable]
    internal sealed class DisplayNameUpdateRequest
    {
        public string displayName;
    }

    [Serializable]
    internal sealed class AvatarUpdateRequest
    {
        public string avatar;
    }

    [Serializable]
    internal sealed class ProfileUpdateRequest
    {
        public string displayName;
        public string avatar;
    }
}
