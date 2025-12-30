using System;
using System.Collections.Generic;
using Leadr.Internal;

namespace Leadr.Models
{
    /// <summary>
    /// Represents an authenticated device session with the LEADR API.
    /// </summary>
    /// <remarks>
    /// Sessions are created automatically when needed. You typically don't need
    /// to interact with this class directly. Access tokens are managed internally
    /// by the SDK.
    /// </remarks>
    public class Session
    {
        /// <summary>
        /// Gets the unique device identifier assigned by LEADR.
        /// </summary>
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
        /// Gets the client fingerprint used to identify this device.
        /// </summary>
        public string ClientFingerprint { get; private set; }

        /// <summary>
        /// Gets the device platform (e.g., "WindowsPlayer", "Android", "IPhonePlayer").
        /// </summary>
        public string Platform { get; private set; }

        /// <summary>
        /// Gets the device status (e.g., "active", "suspended", "banned").
        /// </summary>
        public string Status { get; private set; }

        /// <summary>
        /// Gets optional device metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; private set; }

        /// <summary>
        /// Gets the access token lifetime in seconds.
        /// </summary>
        public int ExpiresIn { get; private set; }

        /// <summary>
        /// Gets when this device was first seen by LEADR.
        /// </summary>
        public DateTime FirstSeenAt { get; private set; }

        /// <summary>
        /// Gets when this device was last seen by LEADR.
        /// </summary>
        public DateTime LastSeenAt { get; private set; }

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
                ClientFingerprint = json.GetString("client_fingerprint"),
                Platform = json.GetString("platform"),
                Status = json.GetString("status"),
                Metadata = json.GetDict("metadata") ?? new Dictionary<string, object>(),
                ExpiresIn = json.GetInt("expires_in"),
                FirstSeenAt = json.GetDateTime("first_seen_at") ?? DateTime.MinValue,
                LastSeenAt = json.GetDateTime("last_seen_at") ?? DateTime.MinValue,
                AccessToken = json.GetString("access_token"),
                RefreshToken = json.GetString("refresh_token")
            };
        }
    }
}
