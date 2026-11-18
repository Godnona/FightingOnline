using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Facebook.Unity;
using Ripp.ApiClient;
using Ripp.ApiClient.Authentication;
using Ripp.ApiClient.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Ripp.UiLogin
{
    [DisallowMultipleComponent]
    public sealed class FacebookLoginController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button loginButton;
        [SerializeField] private TMP_Text alertText;

        [Header("Navigation")]
        [SerializeField] private string mainMenuSceneName = "MainMenuScene";

        [Header("Editor Testing")]
        [Tooltip("Uses the backend's mock:<id>:<email> token instead of opening Facebook.")]
        [SerializeField] private bool useMockTokenInEditor = true;
        [SerializeField] private string mockUserId = "facebook-editor-user";
        [SerializeField] private string mockEmail = "facebook.editor@example.com";

        private CancellationTokenSource lifetimeCancellation;
        private bool isBusy;

        private void OnEnable()
        {
            lifetimeCancellation = new CancellationTokenSource();
            SetButtonInteractable(true);

#if !UNITY_EDITOR
            InitializeFacebook();
#endif
        }

        private void OnDisable()
        {
            lifetimeCancellation?.Cancel();
            lifetimeCancellation?.Dispose();
            lifetimeCancellation = null;
        }

        /// <summary>Assign this method to the Login Facebook button.</summary>
        public void LoginWithFacebook()
        {
            if (isBusy)
            {
                return;
            }

            SetAlert(string.Empty);

#if UNITY_EDITOR
            if (useMockTokenInEditor)
            {
                SubmitFacebookToken(BuildMockToken(mockUserId, mockEmail));
                return;
            }
#endif

            if (!FB.IsInitialized)
            {
                SetAlert("Initializing Facebook...");
                InitializeFacebook(true);
                return;
            }

            StartFacebookLogin();
        }

        /// <summary>
        /// Can also be called by a custom Facebook/Limited Login bridge with its token.
        /// </summary>
        public async void SubmitFacebookToken(string accessToken)
        {
            if (isBusy)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                SetAlert("Facebook did not return an access token.");
                return;
            }

            if (ApiBootstrap.Api == null)
            {
                SetAlert("API is not ready. Check the ApiBootstrap object.");
                return;
            }

            SetBusy(true);
            SetAlert("Signing in with Facebook...");
            try
            {
                ApiResult<ProviderAuthResponse> result = await ApiBootstrap.Api.Auth.LoginWithFacebookAsync(
                    accessToken.Trim(),
                    BuildDeviceInfo(),
                    lifetimeCancellation.Token);

                if (this == null || lifetimeCancellation == null)
                {
                    return;
                }

                if (!result.IsSuccess)
                {
                    SetApiError(result);
                    return;
                }

                if (result.Data == null || result.Data.user == null)
                {
                    SetAlert("The server returned an invalid Facebook login response.");
                    return;
                }

                PlayerSession.Set(result.Data.user);
                LoadMainMenu();
            }
            catch (OperationCanceledException)
            {
                // Expected when this object/scene is destroyed.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                SetAlert("Facebook login failed. Please try again.");
            }
            finally
            {
                if (this != null)
                {
                    SetBusy(false);
                }
            }
        }

        private void InitializeFacebook(bool loginWhenReady = false)
        {
            if (FB.IsInitialized)
            {
                FB.ActivateApp();
                if (loginWhenReady)
                {
                    StartFacebookLogin();
                }

                return;
            }

            FB.Init(
                () =>
                {
                    if (this == null)
                    {
                        return;
                    }

                    if (!FB.IsInitialized)
                    {
                        SetAlert("Facebook SDK initialization failed. Check Facebook Settings.");
                        return;
                    }

                    FB.ActivateApp();
                    SetAlert(string.Empty);
                    if (loginWhenReady)
                    {
                        StartFacebookLogin();
                    }
                },
                _ => { });
        }

        private void StartFacebookLogin()
        {
            SetBusy(true);
            SetAlert("Waiting for Facebook...");
            FB.LogInWithReadPermissions(
                new List<string> { "public_profile", "email" },
                HandleFacebookLoginResult);
        }

        private void HandleFacebookLoginResult(ILoginResult result)
        {
            SetBusy(false);

            if (result == null)
            {
                SetAlert("Facebook returned no login result.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                Debug.LogWarning("Facebook SDK login error: " + result.Error, this);
                SetAlert("Facebook login failed. Please try again.");
                return;
            }

            if (result.Cancelled)
            {
                SetAlert("Facebook login was cancelled.");
                return;
            }

            AccessToken token = AccessToken.CurrentAccessToken;
            if (token == null || string.IsNullOrWhiteSpace(token.TokenString))
            {
                SetAlert("Facebook did not return an access token.");
                return;
            }

            SubmitFacebookToken(token.TokenString);
        }

        private void SetApiError(ApiResult<ProviderAuthResponse> result)
        {
            switch (result.Error?.code)
            {
                case ApiErrorCodes.EmailAlreadyInUse:
                    SetAlert("This email already has an account. Log in and link Facebook in settings.");
                    break;
                case ApiErrorCodes.InvalidProviderToken:
                    SetAlert("Facebook token is invalid or expired.");
                    break;
                case ApiErrorCodes.ProviderNotConfigured:
                    SetAlert("Facebook login is not configured on the server.");
                    break;
                case ApiErrorCodes.ProviderUnavailable:
                    SetAlert("Facebook is temporarily unavailable. Please try again.");
                    break;
                case ApiErrorCodes.AccountBanned:
                    SetAlert("This account has been banned.");
                    break;
                case ApiErrorCodes.NetworkError:
                    SetAlert("Cannot reach the server. Check your network and API URL.");
                    break;
                default:
                    SetAlert(string.IsNullOrWhiteSpace(result.Error?.message)
                        ? "Facebook login failed. Please try again."
                        : result.Error.message);
                    break;
            }

            if (!string.IsNullOrWhiteSpace(result.RequestId))
            {
                Debug.LogWarning(
                    $"Facebook backend login failed. Code={result.Error?.code}, RequestId={result.RequestId}",
                    this);
            }
        }

        private void LoadMainMenu()
        {
            if (!Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                SetAlert($"Scene '{mainMenuSceneName}' is missing from Build Settings.");
                return;
            }

            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void SetBusy(bool value)
        {
            isBusy = value;
            SetButtonInteractable(!value);
        }

        private void SetButtonInteractable(bool value)
        {
            if (loginButton != null)
            {
                loginButton.interactable = value;
            }
        }

        private void SetAlert(string message)
        {
            if (alertText != null)
            {
                alertText.text = message;
            }
        }

        private static string BuildMockToken(string userId, string email)
        {
            string safeUserId = string.IsNullOrWhiteSpace(userId) ? "facebook-editor-user" : userId.Trim();
            return string.IsNullOrWhiteSpace(email)
                ? "mock:" + safeUserId
                : "mock:" + safeUserId + ":" + email.Trim();
        }

        private static string BuildDeviceInfo()
        {
            return $"{Application.platform} / {SystemInfo.deviceModel}";
        }
    }
}
