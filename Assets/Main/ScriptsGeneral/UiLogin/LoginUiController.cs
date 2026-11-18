using System;
using System.Threading;
using System.Threading.Tasks;
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
    public sealed class LoginUiController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private GameObject forgotPasswordPanel;
        [SerializeField] private GameObject resetPasswordPanel;

        [Header("Login")]
        [SerializeField] private TMP_InputField loginEmailInput;
        [SerializeField] private TMP_InputField loginPasswordInput;
        [SerializeField] private TMP_Text loginAlertText;

        [Header("Register")]
        [SerializeField] private TMP_InputField registerEmailInput;
        [SerializeField] private TMP_InputField registerDisplayNameInput;
        [SerializeField] private TMP_InputField registerPasswordInput;
        [SerializeField] private TMP_InputField registerConfirmPasswordInput;
        [SerializeField] private TMP_Text registerAlertText;

        [Header("Forgot Password")]
        [SerializeField] private TMP_InputField forgotEmailInput;
        [SerializeField] private TMP_Text forgotAlertText;

        [Header("Reset Password")]
        [SerializeField] private TMP_InputField resetEmailInput;
        [SerializeField] private TMP_InputField resetCodeInput;
        [SerializeField] private TMP_InputField resetNewPasswordInput;
        [SerializeField] private TMP_InputField resetConfirmPasswordInput;
        [SerializeField] private TMP_Text resetAlertText;

        [Header("Behaviour")]
        [Tooltip("Every submit button that should be disabled while an API request is running.")]
        [SerializeField] private Button[] submitButtons = Array.Empty<Button>();
        [SerializeField] private string mainMenuSceneName = "MainMenuScene";
        [SerializeField] private bool loginAfterRegister = true;

        private CancellationTokenSource lifetimeCancellation;
        private bool isBusy;

        private void Awake()
        {
            lifetimeCancellation = new CancellationTokenSource();
            ClearAllAlerts();
            ShowLogin();
        }

        private void OnEnable()
        {
            ApiBootstrap.SessionInvalidated += HandleSessionInvalidated;
        }

        private void OnDisable()
        {
            ApiBootstrap.SessionInvalidated -= HandleSessionInvalidated;
        }

        private void OnDestroy()
        {
            lifetimeCancellation?.Cancel();
            lifetimeCancellation?.Dispose();
            lifetimeCancellation = null;
        }

        public void ShowLogin()
        {
            SetActivePanel(loginPanel);
        }

        public void ShowRegister()
        {
            ClearAllAlerts();
            CopyText(loginEmailInput, registerEmailInput);
            SetActivePanel(registerPanel);
        }

        public void ShowForgotPassword()
        {
            ClearAllAlerts();
            CopyText(loginEmailInput, forgotEmailInput);
            SetActivePanel(forgotPasswordPanel);
        }

        public void ShowResetPassword()
        {
            ClearAllAlerts();
            CopyText(forgotEmailInput, resetEmailInput);
            SetActivePanel(resetPasswordPanel);
        }

        public async void Login()
        {
            if (isBusy)
            {
                return;
            }

            ClearAllAlerts();
            string email = Read(loginEmailInput);
            string password = loginPasswordInput == null ? string.Empty : loginPasswordInput.text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            {
                SetAlert(loginAlertText, "Enter your email and password.");
                return;
            }

            if (!TryGetApi(loginAlertText, out ApiClientContext api))
            {
                return;
            }

            await RunRequestAsync(loginAlertText, async cancellationToken =>
            {
                ApiResult<AuthResponse> result = await api.Auth.LoginAsync(
                    email,
                    password,
                    BuildDeviceInfo(),
                    cancellationToken);

                if (!result.IsSuccess)
                {
                    SetApiError(loginAlertText, result);
                    return;
                }

                if (result.Data == null || result.Data.user == null)
                {
                    SetAlert(loginAlertText, "The server returned an invalid login response.");
                    return;
                }

                PlayerSession.Set(result.Data.user);
                LoadMainMenu(loginAlertText);
            });
        }

        public async void Register()
        {
            if (isBusy)
            {
                return;
            }

            ClearAllAlerts();
            string email = Read(registerEmailInput);
            string displayName = Read(registerDisplayNameInput);
            string password = registerPasswordInput == null ? string.Empty : registerPasswordInput.text;
            string confirmPassword = registerConfirmPasswordInput == null
                ? string.Empty
                : registerConfirmPasswordInput.text;

            if (string.IsNullOrWhiteSpace(email))
            {
                SetAlert(registerAlertText, "Enter your email.");
                return;
            }

            if (!IsValidDisplayName(displayName))
            {
                SetAlert(registerAlertText, "Name must be 3-16 letters, numbers, or underscores.");
                return;
            }

            if (!TryValidatePassword(password, confirmPassword, registerAlertText))
            {
                return;
            }

            if (!TryGetApi(registerAlertText, out ApiClientContext api))
            {
                return;
            }

            await RunRequestAsync(registerAlertText, async cancellationToken =>
            {
                ApiResult<AuthResponse> result = await api.Auth.RegisterAsync(
                    email,
                    password,
                    displayName,
                    BuildDeviceInfo(),
                    cancellationToken);

                if (!result.IsSuccess)
                {
                    SetApiError(registerAlertText, result);
                    return;
                }

                if (result.Data == null || result.Data.user == null)
                {
                    SetAlert(registerAlertText, "The server returned an invalid register response.");
                    return;
                }

                PlayerSession.Set(result.Data.user);
                if (loginAfterRegister)
                {
                    LoadMainMenu(registerAlertText);
                    return;
                }

                if (loginEmailInput != null)
                {
                    loginEmailInput.text = email;
                }

                ShowLogin();
                SetAlert(loginAlertText, "Account created. You can now log in.");
            });
        }

        public async void SendResetCode()
        {
            if (isBusy)
            {
                return;
            }

            ClearAllAlerts();
            string email = Read(forgotEmailInput);
            if (string.IsNullOrWhiteSpace(email))
            {
                SetAlert(forgotAlertText, "Enter your email.");
                return;
            }

            if (!TryGetApi(forgotAlertText, out ApiClientContext api))
            {
                return;
            }

            await RunRequestAsync(forgotAlertText, async cancellationToken =>
            {
                ApiResult<NoContent> result = await api.Auth.ForgotPasswordAsync(email, cancellationToken);
                if (!result.IsSuccess)
                {
                    SetApiError(forgotAlertText, result);
                    return;
                }

                if (resetEmailInput != null)
                {
                    resetEmailInput.text = email;
                }

                SetActivePanel(resetPasswordPanel);
                SetAlert(resetAlertText, "If the account exists, a 6-digit code has been sent.");
            });
        }

        public async void SaveNewPassword()
        {
            if (isBusy)
            {
                return;
            }

            ClearAllAlerts();
            string email = Read(resetEmailInput);
            string code = Read(resetCodeInput);
            string password = resetNewPasswordInput == null ? string.Empty : resetNewPasswordInput.text;
            string confirmPassword = resetConfirmPasswordInput == null
                ? string.Empty
                : resetConfirmPasswordInput.text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
            {
                SetAlert(resetAlertText, "Enter your email and reset code.");
                return;
            }

            if (!TryValidatePassword(password, confirmPassword, resetAlertText))
            {
                return;
            }

            if (!TryGetApi(resetAlertText, out ApiClientContext api))
            {
                return;
            }

            await RunRequestAsync(resetAlertText, async cancellationToken =>
            {
                ApiResult<NoContent> result = await api.Auth.ResetPasswordAsync(
                    email,
                    code,
                    password,
                    cancellationToken);

                if (!result.IsSuccess)
                {
                    SetApiError(resetAlertText, result);
                    return;
                }

                PlayerSession.Clear();
                if (loginEmailInput != null)
                {
                    loginEmailInput.text = email;
                }

                ClearPasswordFields();
                ShowLogin();
                SetAlert(loginAlertText, "Password changed. Log in with your new password.");
            });
        }

        private async Task RunRequestAsync(TMP_Text alert, Func<CancellationToken, Task> request)
        {
            SetBusy(true);
            try
            {
                await request(lifetimeCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected when this scene/object is destroyed.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                SetAlert(alert, "Unexpected error. Please try again.");
            }
            finally
            {
                if (this != null)
                {
                    SetBusy(false);
                }
            }
        }

        private void LoadMainMenu(TMP_Text alert)
        {
            if (string.IsNullOrWhiteSpace(mainMenuSceneName) ||
                !Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                SetAlert(alert, $"Scene '{mainMenuSceneName}' is missing from Build Settings.");
                return;
            }

            SceneManager.LoadScene(mainMenuSceneName);
        }

        private static bool TryGetApi(TMP_Text alert, out ApiClientContext api)
        {
            api = ApiBootstrap.Api;
            if (api != null)
            {
                return true;
            }

            SetAlert(alert, "API is not ready. Check the ApiBootstrap object and its settings.");
            return false;
        }

        private void HandleSessionInvalidated()
        {
            PlayerSession.Clear();
            ShowLogin();
            SetAlert(loginAlertText, "Your session expired. Please log in again.");
        }

        private void SetActivePanel(GameObject activePanel)
        {
            SetPanelActive(loginPanel, activePanel == loginPanel);
            SetPanelActive(registerPanel, activePanel == registerPanel);
            SetPanelActive(forgotPasswordPanel, activePanel == forgotPasswordPanel);
            SetPanelActive(resetPasswordPanel, activePanel == resetPasswordPanel);
        }

        private static void SetPanelActive(GameObject panel, bool value)
        {
            if (panel != null)
            {
                panel.SetActive(value);
            }
        }

        private void SetBusy(bool value)
        {
            isBusy = value;
            foreach (Button button in submitButtons)
            {
                if (button != null)
                {
                    button.interactable = !value;
                }
            }
        }

        private void ClearAllAlerts()
        {
            SetAlert(loginAlertText, string.Empty);
            SetAlert(registerAlertText, string.Empty);
            SetAlert(forgotAlertText, string.Empty);
            SetAlert(resetAlertText, string.Empty);
        }

        private void ClearPasswordFields()
        {
            SetText(loginPasswordInput, string.Empty);
            SetText(registerPasswordInput, string.Empty);
            SetText(registerConfirmPasswordInput, string.Empty);
            SetText(resetNewPasswordInput, string.Empty);
            SetText(resetConfirmPasswordInput, string.Empty);
            SetText(resetCodeInput, string.Empty);
        }

        private static bool TryValidatePassword(string password, string confirmation, TMP_Text alert)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8 || password.Length > 128)
            {
                SetAlert(alert, "Password must contain 8-128 characters.");
                return false;
            }

            if (!string.Equals(password, confirmation, StringComparison.Ordinal))
            {
                SetAlert(alert, "Password confirmation does not match.");
                return false;
            }

            return true;
        }

        private static bool IsValidDisplayName(string displayName)
        {
            if (displayName.Length < 3 || displayName.Length > 16)
            {
                return false;
            }

            foreach (char character in displayName)
            {
                bool isValid = character == '_' ||
                               (character >= 'a' && character <= 'z') ||
                               (character >= 'A' && character <= 'Z') ||
                               (character >= '0' && character <= '9');
                if (!isValid)
                {
                    return false;
                }
            }

            return true;
        }

        private static void SetApiError<T>(TMP_Text alert, ApiResult<T> result)
        {
            string message;
            switch (result.Error?.code)
            {
                case ApiErrorCodes.InvalidCredentials:
                    message = "Email or password is incorrect.";
                    break;
                case ApiErrorCodes.EmailAlreadyInUse:
                    message = "This email is already registered.";
                    break;
                case ApiErrorCodes.DisplayNameTaken:
                    message = "This player name is already taken.";
                    break;
                case ApiErrorCodes.InvalidPasswordResetCode:
                    message = "The reset code is invalid or expired.";
                    break;
                case ApiErrorCodes.AccountBanned:
                    message = "This account has been banned.";
                    break;
                case ApiErrorCodes.RateLimited:
                    message = result.RetryAfterSeconds.HasValue
                        ? $"Too many attempts. Try again in {result.RetryAfterSeconds.Value} seconds."
                        : "Too many attempts. Please wait and try again.";
                    break;
                case ApiErrorCodes.NetworkError:
                    message = "Cannot reach the server. Check your network and API URL.";
                    break;
                case ApiErrorCodes.ValidationError:
                    message = string.IsNullOrWhiteSpace(result.Error?.message)
                        ? "Please check the information you entered."
                        : result.Error.message;
                    break;
                default:
                    message = string.IsNullOrWhiteSpace(result.Error?.message)
                        ? "Request failed. Please try again."
                        : result.Error.message;
                    break;
            }

            SetAlert(alert, message);
            if (!string.IsNullOrWhiteSpace(result.RequestId))
            {
                Debug.LogWarning($"API request failed. Code={result.Error?.code}, RequestId={result.RequestId}");
            }
        }

        private static string BuildDeviceInfo()
        {
            return $"{Application.platform} / {SystemInfo.deviceModel}";
        }

        private static string Read(TMP_InputField input)
        {
            return input == null || input.text == null ? string.Empty : input.text.Trim();
        }

        private static void CopyText(TMP_InputField source, TMP_InputField destination)
        {
            if (source != null && destination != null && string.IsNullOrWhiteSpace(destination.text))
            {
                destination.text = source.text;
            }
        }

        private static void SetText(TMP_InputField input, string value)
        {
            if (input != null)
            {
                input.text = value;
            }
        }

        private static void SetAlert(TMP_Text alert, string message)
        {
            if (alert != null)
            {
                alert.text = message;
            }
        }
    }
}
