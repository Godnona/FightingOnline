using System;
using System.Threading;
using Ripp.ApiClient;
using Ripp.ApiClient.Authentication;
using Ripp.ApiClient.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if GOOGLE_SIGN_IN
using Google;
#endif

namespace Ripp.UiLogin
{
    [DisallowMultipleComponent]
    public sealed class GoogleLoginController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button loginButton;
        [SerializeField] private TMP_Text alertText;

        [Header("Google")]
        [Tooltip("OAuth client with type Web application. Required to request a Google ID token.")]
        [SerializeField] private string webClientId;

        [Header("Navigation")]
        [SerializeField] private string mainMenuSceneName = "MainMenuScene";

        [Header("Editor Testing")]
        [Tooltip("Uses the backend's mock:<id>:<email> token because Google Sign-In does not run in Editor.")]
        [SerializeField] private bool useMockTokenInEditor = true;
        [SerializeField] private string mockUserId = "google-editor-user";
        [SerializeField] private string mockEmail = "google.editor@example.com";

        private CancellationTokenSource lifetimeCancellation;
        private bool isBusy;

#if GOOGLE_SIGN_IN
        private GoogleSignInConfiguration googleConfiguration;
#endif

        private void OnEnable()
        {
            lifetimeCancellation = new CancellationTokenSource();
            SetButtonInteractable(true);
        }

        private void OnDisable()
        {
            lifetimeCancellation?.Cancel();
            lifetimeCancellation?.Dispose();
            lifetimeCancellation = null;
        }

        /// <summary>Assign this method to the Login Google button.</summary>
        public void LoginWithGoogle()
        {
            if (isBusy)
            {
                return;
            }

            SetAlert(string.Empty);

#if UNITY_EDITOR
            if (useMockTokenInEditor)
            {
                SubmitGoogleIdToken(BuildMockToken(mockUserId, mockEmail));
                return;
            }
#endif

#if GOOGLE_SIGN_IN
            SignInWithGoogleSdk();
#else
            SetAlert("Google Sign-In SDK is not installed. See SETUP_SOCIAL_LOGIN_VI.md.");
#endif
        }

        /// <summary>
        /// Call this from any Google SDK callback after it returns an ID token.
        /// Do not pass a Google access token or server auth code here.
        /// </summary>
        public async void SubmitGoogleIdToken(string idToken)
        {
            if (isBusy)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(idToken))
            {
                SetAlert("Google did not return an ID token.");
                return;
            }

            if (ApiBootstrap.Api == null)
            {
                SetAlert("API is not ready. Check the ApiBootstrap object.");
                return;
            }

            SetBusy(true);
            SetAlert("Signing in with Google...");
            try
            {
                ApiResult<ProviderAuthResponse> result = await ApiBootstrap.Api.Auth.LoginWithGoogleAsync(
                    idToken.Trim(),
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
                    SetAlert("The server returned an invalid Google login response.");
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
                SetAlert("Google login failed. Please try again.");
            }
            finally
            {
                if (this != null)
                {
                    SetBusy(false);
                }
            }
        }

#if GOOGLE_SIGN_IN
        private async void SignInWithGoogleSdk()
        {
            if (string.IsNullOrWhiteSpace(webClientId))
            {
                SetAlert("Enter the Google Web Client ID in the Inspector.");
                return;
            }

            SetBusy(true);
            SetAlert("Waiting for Google...");
            try
            {
                if (googleConfiguration == null)
                {
                    googleConfiguration = new GoogleSignInConfiguration
                    {
                        WebClientId = webClientId.Trim(),
                        RequestEmail = true,
                        RequestIdToken = true,
                        UseGameSignIn = false
                    };
                }

                GoogleSignIn.Configuration = googleConfiguration;
                GoogleSignInUser user = await GoogleSignIn.DefaultInstance.SignIn();

                SetBusy(false);
                if (user == null || string.IsNullOrWhiteSpace(user.IdToken))
                {
                    SetAlert("Google did not return an ID token.");
                    return;
                }

                SubmitGoogleIdToken(user.IdToken);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                SetBusy(false);
                SetAlert("Google sign-in was cancelled or failed.");
            }
        }
#endif

        private void SetApiError(ApiResult<ProviderAuthResponse> result)
        {
            switch (result.Error?.code)
            {
                case ApiErrorCodes.EmailAlreadyInUse:
                    SetAlert("This email already has an account. Log in and link Google in settings.");
                    break;
                case ApiErrorCodes.InvalidProviderToken:
                    SetAlert("Google ID token is invalid or expired.");
                    break;
                case ApiErrorCodes.ProviderNotConfigured:
                    SetAlert("Google login is not configured on the server.");
                    break;
                case ApiErrorCodes.ProviderUnavailable:
                    SetAlert("Google is temporarily unavailable. Please try again.");
                    break;
                case ApiErrorCodes.AccountBanned:
                    SetAlert("This account has been banned.");
                    break;
                case ApiErrorCodes.NetworkError:
                    SetAlert("Cannot reach the server. Check your network and API URL.");
                    break;
                default:
                    SetAlert(string.IsNullOrWhiteSpace(result.Error?.message)
                        ? "Google login failed. Please try again."
                        : result.Error.message);
                    break;
            }

            if (!string.IsNullOrWhiteSpace(result.RequestId))
            {
                Debug.LogWarning(
                    $"Google backend login failed. Code={result.Error?.code}, RequestId={result.RequestId}",
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
            string safeUserId = string.IsNullOrWhiteSpace(userId) ? "google-editor-user" : userId.Trim();
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
