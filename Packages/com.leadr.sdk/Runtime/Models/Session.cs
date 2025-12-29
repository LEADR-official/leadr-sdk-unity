using System.Collections.Generic;
using Leadr.Internal;

namespace Leadr.Models
{
    /// <summary>
    /// Represents an authenticated session with the LEADR API.
    /// </summary>
    /// <remarks>
    /// Sessions are created automatically when needed. You typically don't need
    /// to interact with this class directly. Access tokens are managed internally
    /// by the SDK.
    /// </remarks>
    public class Session
    {
        /// <summary>
        /// Gets the device identifier for this session.
        /// </summary>
        /// <remarks>
        /// The device ID is persisted in PlayerPrefs and reused across sessions.
        /// </remarks>
        public string DeviceId { get; private set; }

        /// <summary>
        /// Gets the game ID this session is authenticated for.
        /// </summary>
        public string GameId { get; private set; }

        /// <summary>
        /// Gets the account ID associated with the game.
        /// </summary>
        public string AccountId { get; private set; }

        /// <summary>
        /// Gets the session status (e.g., "active", "suspended", "banned").
        /// </summary>
        public string Status { get; private set; }

        /// <summary>
        /// Gets the access token lifetime in seconds.
        /// </summary>
        public int ExpiresIn { get; private set; }

        internal string AccessToken { get; private set; }
        internal string RefreshToken { get; private set; }

        internal static Session FromJson(Dictionary<string, object> json)
        {
            if (json == null)
                return null;

            return new Session
            {
                DeviceId = json.GetString("id"),
                GameId = json.GetString("game_id"),
                AccountId = json.GetString("account_id"),
                Status = json.GetString("status"),
                ExpiresIn = json.GetInt("expires_in"),
                AccessToken = json.GetString("access_token"),
                RefreshToken = json.GetString("refresh_token")
            };
        }
    }
}
