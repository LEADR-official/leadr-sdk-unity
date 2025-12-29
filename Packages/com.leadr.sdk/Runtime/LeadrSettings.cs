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
        [Tooltip("API base URL (default: https://api.leadrcloud.com/v1/)")]
        [SerializeField]
        private string baseUrl = "https://api.leadrcloud.com/v1/";

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
        public string BaseUrl => string.IsNullOrEmpty(baseUrl) ? "https://api.leadrcloud.com/v1/" : baseUrl;

        /// <summary>
        /// Gets whether debug logging is enabled.
        /// </summary>
        public bool DebugLogging => debugLogging;

        /// <summary>
        /// Validates the settings and logs errors for missing required values.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrEmpty(gameId))
            {
                Debug.LogError("[LEADR] GameId is required in LeadrSettings");
            }
        }
    }
}
