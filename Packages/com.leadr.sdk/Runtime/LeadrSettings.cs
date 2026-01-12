using UnityEngine;

namespace Leadr
{
    /// <summary>
    /// ScriptableObject containing LEADR SDK configuration.
    /// </summary>
    /// <remarks>
    /// Create a settings asset via Assets > Create > LEADR > Settings in the Unity Editor.
    /// Assign it to your initialization script or reference it from Resources.
    /// </remarks>
    [CreateAssetMenu(fileName = "LeadrSettings", menuName = "LEADR/Settings", order = 1)]
    public class LeadrSettings : ScriptableObject
    {
        /// <summary>
        /// Your LEADR Game ID (required). Found in the LEADR dashboard.
        /// </summary>
        [Tooltip("Your LEADR Game ID (required)")]
        [SerializeField]
        private string gameId;

        /// <summary>
        /// API base URL. Only change this for self-hosted LEADR instances.
        /// </summary>
        [Tooltip("API base URL (default: https://api.leadrcloud.com)")]
        [SerializeField]
        private string baseUrl = "https://api.leadrcloud.com";

        /// <summary>
        /// When enabled, logs HTTP requests and auth events to the console.
        /// Tokens are never logged.
        /// </summary>
        [Tooltip("Enable debug logging to console")]
        [SerializeField]
        private bool debugLogging = false;

        /// <summary>
        /// Gets the configured Game ID.
        /// </summary>
        public string GameId => gameId;

        /// <summary>
        /// Gets the API base URL, defaulting to the LEADR cloud if not set.
        /// </summary>
        public string BaseUrl => string.IsNullOrEmpty(baseUrl) ? "https://api.leadrcloud.com" : baseUrl;

        /// <summary>
        /// Gets whether debug logging is enabled.
        /// </summary>
        public bool DebugLogging => debugLogging;

        /// <summary>
        /// Validates the settings and throws if invalid.
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown when settings are invalid.</exception>
        public void Validate()
        {
            if (string.IsNullOrEmpty(gameId))
            {
                throw new System.ArgumentException("GameId is required in LeadrSettings");
            }

            if (!string.IsNullOrEmpty(baseUrl))
            {
                if (!baseUrl.StartsWith("http://") && !baseUrl.StartsWith("https://"))
                {
                    throw new System.ArgumentException($"Invalid BaseUrl '{baseUrl}': must start with http:// or https://");
                }

                if (!System.Uri.TryCreate(baseUrl, System.UriKind.Absolute, out _))
                {
                    throw new System.ArgumentException($"Invalid BaseUrl '{baseUrl}': not a valid URL");
                }
            }
        }
    }
}
