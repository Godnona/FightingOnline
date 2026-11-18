using System;
using Ripp.ApiClient.Authentication;
using Ripp.ApiClient.Profile;
using UnityEngine;

namespace Ripp.UiLogin
{
    /// <summary>
    /// Keeps the signed-in player's public data available while scenes change.
    /// Authentication tokens remain owned by ApiBootstrap/ApiClientContext.
    /// </summary>
    public static class PlayerSession
    {
        public static string PlayerId { get; private set; }
        public static string DisplayName { get; private set; }
        public static string Avatar { get; private set; }
        public static string Role { get; private set; }

        public static bool HasPlayer => !string.IsNullOrWhiteSpace(PlayerId);

        public static event Action Changed;

        public static void Set(PlayerSummary player)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            SetValues(player.id, player.displayName, player.avatar, player.role);
        }

        public static void Set(PlayerProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            SetValues(profile.id, profile.displayName, profile.avatar, profile.role);
        }

        public static void Clear()
        {
            PlayerId = null;
            DisplayName = null;
            Avatar = null;
            Role = null;
            Changed?.Invoke();
        }

        private static void SetValues(string id, string displayName, string avatar, string role)
        {
            PlayerId = id;
            DisplayName = displayName;
            Avatar = avatar;
            Role = role;
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            PlayerId = null;
            DisplayName = null;
            Avatar = null;
            Role = null;
            Changed = null;
        }
    }
}
