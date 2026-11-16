using System;
using Ripp.ApiClient.Configuration;
using UnityEngine;

namespace Ripp.ApiClient
{
    /// Creates the shared API context once and keeps it alive across scenes.
    /// Add this component to one GameObject in the startup scene.
    [DefaultExecutionOrder(-1000)]
    public sealed class ApiBootstrap : MonoBehaviour
    {
        [SerializeField] private ApiClientSettings settings;
        [SerializeField] private bool persistAcrossScenes = true;

        public static ApiBootstrap Instance { get; private set; }
        public static ApiClientContext Api { get; private set; }
        public static bool IsInitialized => Api != null;

        public static event Action SessionInvalidated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (settings == null)
            {
                Debug.LogError(
                    "ApiBootstrap requires an ApiClientSettings asset. " +
                    "Create one from Create > Ripp > API Client Settings and assign it in the Inspector.",
                    this);
                enabled = false;
                return;
            }

            Instance = this;
            Api = new ApiClientContext(settings);
            Api.SessionInvalidated += HandleSessionInvalidated;

            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            if (Api != null)
            {
                Api.SessionInvalidated -= HandleSessionInvalidated;
                Api.Dispose();
                Api = null;
            }

            Instance = null;
        }

        private void HandleSessionInvalidated()
        {
            SessionInvalidated?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            Api = null;
            SessionInvalidated = null;
        }
    }
}
