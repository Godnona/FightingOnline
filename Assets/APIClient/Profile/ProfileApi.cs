using System;
using System.Threading;
using System.Threading.Tasks;
using Ripp.ApiClient.Core;
using UnityEngine.Networking;
using ApiHttpClient = Ripp.ApiClient.Core.ApiClient;

namespace Ripp.ApiClient.Profile
{
    public sealed class ProfileApi
    {
        private readonly ApiHttpClient client;

        public ProfileApi(ApiHttpClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public Task<ApiResult<PlayerProfile>> GetMyProfileAsync(
            CancellationToken cancellationToken = default)
        {
            return client.SendAsync<PlayerProfile>(
                UnityWebRequest.kHttpVerbGET,
                "/profile",
                requiresAuthentication: true,
                cancellationToken: cancellationToken);
        }

        public Task<ApiResult<PublicPlayerProfile>> GetPlayerAsync(
            string playerId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new ArgumentException("Player id is required.", nameof(playerId));
            }

            string route = "/players/" + Uri.EscapeDataString(playerId);
            return client.SendAsync<PublicPlayerProfile>(
                UnityWebRequest.kHttpVerbGET,
                route,
                requiresAuthentication: true,
                cancellationToken: cancellationToken);
        }

        public Task<ApiResult<PlayerProfile>> UpdateDisplayNameAsync(
            string displayName,
            CancellationToken cancellationToken = default)
        {
            return client.SendAsync<PlayerProfile>(
                "PATCH",
                "/profile",
                new DisplayNameUpdateRequest { displayName = displayName },
                requiresAuthentication: true,
                cancellationToken: cancellationToken);
        }

        public Task<ApiResult<PlayerProfile>> UpdateAvatarAsync(
            string avatar,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(avatar))
            {
                throw new ArgumentException(
                    "Avatar id is required. Use RemoveAvatarAsync to send an explicit null.",
                    nameof(avatar));
            }

            return client.SendAsync<PlayerProfile>(
                "PATCH",
                "/profile",
                new AvatarUpdateRequest { avatar = avatar },
                requiresAuthentication: true,
                cancellationToken: cancellationToken);
        }

        public Task<ApiResult<PlayerProfile>> UpdateProfileAsync(
            string displayName,
            string avatar,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name is required.", nameof(displayName));
            }

            if (string.IsNullOrWhiteSpace(avatar))
            {
                throw new ArgumentException(
                    "Avatar id is required. Use RemoveAvatarAsync to send an explicit null.",
                    nameof(avatar));
            }

            return client.SendAsync<PlayerProfile>(
                "PATCH",
                "/profile",
                new ProfileUpdateRequest { displayName = displayName, avatar = avatar },
                requiresAuthentication: true,
                cancellationToken: cancellationToken);
        }

        public Task<ApiResult<PlayerProfile>> RemoveAvatarAsync(
            CancellationToken cancellationToken = default)
        {
            return client.SendJsonAsync<PlayerProfile>(
                "PATCH",
                "/profile",
                "{\"avatar\":null}",
                requiresAuthentication: true,
                cancellationToken: cancellationToken);
        }
    }
}
