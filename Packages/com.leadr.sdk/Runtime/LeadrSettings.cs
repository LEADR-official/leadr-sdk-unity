using UnityEngine;

namespace Leadr
{
    [CreateAssetMenu(fileName = "LeadrSettings", menuName = "LEADR/Settings", order = 1)]
    public class LeadrSettings : ScriptableObject
    {
        [Tooltip("Your LEADR Game ID (required)")]
        [SerializeField]
        private string gameId;

        [Tooltip("API base URL (default: https://api.leadrcloud.com/v1/)")]
        [SerializeField]
        private string baseUrl = "https://api.leadrcloud.com/v1/";

        [Tooltip("Enable debug logging to console")]
        [SerializeField]
        private bool debugLogging = false;

        public string GameId => gameId;
        public string BaseUrl => string.IsNullOrEmpty(baseUrl) ? "https://api.leadrcloud.com/v1/" : baseUrl;
        public bool DebugLogging => debugLogging;

        public void Validate()
        {
            if (string.IsNullOrEmpty(gameId))
            {
                Debug.LogError("[LEADR] GameId is required in LeadrSettings");
            }
        }
    }
}
