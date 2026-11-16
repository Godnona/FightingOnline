using System;
using UnityEngine;

namespace Ripp.ApiClient.Configuration
{
    public enum ApiEnvironment
    {
        Local,
        Staging,
        Production
    }

    [Serializable]
    public sealed class ApiEnvironmentUrl
    {
        public ApiEnvironment environment;
        public string baseUrl;
    }

    [CreateAssetMenu(fileName = "ApiClientSettings", menuName = "Ripp/API Client Settings")]
    public sealed class ApiClientSettings : ScriptableObject
    {
        [SerializeField] private ApiEnvironment activeEnvironment = ApiEnvironment.Local;
        [SerializeField] private ApiEnvironmentUrl[] environments = Array.Empty<ApiEnvironmentUrl>();
        [SerializeField, Min(1)] private int timeoutSeconds = 15;

        public ApiEnvironment ActiveEnvironment => activeEnvironment;
        public int TimeoutSeconds => Mathf.Max(1, timeoutSeconds);

        public string GetBaseUrl()
        {
            foreach (ApiEnvironmentUrl entry in environments)
            {
                if (entry == null || entry.environment != activeEnvironment)
                {
                    continue;
                }

                string value = entry.baseUrl == null ? string.Empty : entry.baseUrl.Trim().TrimEnd('/');
                if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    throw new InvalidOperationException(
                        $"API base URL for {activeEnvironment} must be an absolute HTTP or HTTPS URL.");
                }

                if (activeEnvironment == ApiEnvironment.Production && uri.Scheme != Uri.UriSchemeHttps)
                {
                    throw new InvalidOperationException("The production API must use HTTPS.");
                }

                return value;
            }

            throw new InvalidOperationException($"No API base URL is configured for {activeEnvironment}.");
        }
    }
}
