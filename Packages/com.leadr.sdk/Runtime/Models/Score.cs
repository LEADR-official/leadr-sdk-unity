using System;
using System.Collections.Generic;
using Leadr.Internal;

namespace Leadr.Models
{
    /// <summary>
    /// Represents a score entry on a leaderboard.
    /// </summary>
    /// <remarks>
    /// Scores are returned from <see cref="LeadrClient.GetScoresAsync"/> and
    /// <see cref="LeadrClient.SubmitScoreAsync"/>.
    /// </remarks>
    public class Score
    {
        /// <summary>
        /// Gets the unique score identifier (e.g., "scr_abc123...").
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Gets the account ID that owns this score.
        /// </summary>
        public string AccountId { get; private set; }

        /// <summary>
        /// Gets the game ID this score belongs to.
        /// </summary>
        public string GameId { get; private set; }

        /// <summary>
        /// Gets the board ID this score was submitted to.
        /// </summary>
        public string BoardId { get; private set; }

        /// <summary>
        /// Gets the player's display name.
        /// </summary>
        public string PlayerName { get; private set; }

        /// <summary>
        /// Gets the raw numeric score value.
        /// </summary>
        public double Value { get; private set; }

        /// <summary>
        /// Gets the server-formatted display value (e.g., "1:23.45" for time).
        /// May be null if no formatting is configured.
        /// </summary>
        /// <remarks>
        /// Use this for display when available, falling back to <see cref="Value"/> otherwise.
        /// </remarks>
        public string ValueDisplay { get; private set; }

        /// <summary>
        /// Gets the custom metadata attached to this score. May be null.
        /// </summary>
        /// <remarks>
        /// Metadata is set during score submission and can contain any
        /// JSON-serializable data (max 1KB).
        /// </remarks>
        public Dictionary<string, object> Metadata { get; private set; }

        /// <summary>
        /// Gets when the score was submitted (UTC).
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Gets when the score was last updated (UTC).
        /// </summary>
        public DateTime UpdatedAt { get; private set; }

        internal static Score FromJson(Dictionary<string, object> json)
        {
            if (json == null)
                return null;

            return new Score
            {
                Id = json.GetString("id"),
                AccountId = json.GetString("account_id"),
                GameId = json.GetString("game_id"),
                BoardId = json.GetString("board_id"),
                PlayerName = json.GetString("player_name"),
                Value = json.GetDouble("value"),
                ValueDisplay = json.GetString("value_display"),
                Metadata = json.GetDict("metadata"),
                CreatedAt = json.GetDateTimeRequired("created_at"),
                UpdatedAt = json.GetDateTimeRequired("updated_at")
            };
        }
    }
}
