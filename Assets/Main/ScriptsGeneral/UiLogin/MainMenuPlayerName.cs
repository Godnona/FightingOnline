using System;
using System.Threading;
using Ripp.ApiClient;
using Ripp.ApiClient.Core;
using Ripp.ApiClient.Profile;
using TMPro;
using UnityEngine;

namespace Ripp.UiLogin
{
    [DisallowMultipleComponent]
    public sealed class MainMenuPlayerName : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private string prefix = "Name: ";
        [SerializeField] private string guestName = "Guest";
        [SerializeField] private bool refreshFromServer = true;

        private CancellationTokenSource lifetimeCancellation;

        private void Awake()
        {
            if (playerNameText == null)
            {
                playerNameText = GetComponent<TMP_Text>();
            }
        }

        private void OnEnable()
        {
            lifetimeCancellation = new CancellationTokenSource();
            PlayerSession.Changed += RenderCachedName;
            ApiBootstrap.SessionInvalidated += HandleSessionInvalidated;
            RenderCachedName();

            if (refreshFromServer)
            {
                RefreshProfile();
            }
        }

        private void OnDisable()
        {
            PlayerSession.Changed -= RenderCachedName;
            ApiBootstrap.SessionInvalidated -= HandleSessionInvalidated;
            lifetimeCancellation?.Cancel();
            lifetimeCancellation?.Dispose();
            lifetimeCancellation = null;
        }

        private async void RefreshProfile()
        {
            ApiClientContext api = ApiBootstrap.Api;
            if (api == null || api.TokenStorage.Current == null || lifetimeCancellation == null)
            {
                return;
            }

            try
            {
                ApiResult<PlayerProfile> result = await api.Profile.GetMyProfileAsync(
                    lifetimeCancellation.Token);

                if (this == null || lifetimeCancellation == null)
                {
                    return;
                }

                if (result.IsSuccess && result.Data != null)
                {
                    PlayerSession.Set(result.Data);
                    return;
                }

                Debug.LogWarning(
                    $"Could not refresh player profile. Code={result.Error?.code}, RequestId={result.RequestId}",
                    this);
            }
            catch (OperationCanceledException)
            {
                // Expected when leaving the main menu.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private void HandleSessionInvalidated()
        {
            PlayerSession.Clear();
        }

        private void RenderCachedName()
        {
            if (playerNameText == null)
            {
                return;
            }

            string displayName = string.IsNullOrWhiteSpace(PlayerSession.DisplayName)
                ? guestName
                : PlayerSession.DisplayName;
            playerNameText.text = prefix + displayName;
        }
    }
}
