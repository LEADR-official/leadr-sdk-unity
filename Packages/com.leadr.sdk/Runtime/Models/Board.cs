using System;
using System.Collections.Generic;
using Leadr.Internal;

namespace Leadr.Models
{
    /// <summary>
    /// Represents a leaderboard configuration.
    /// </summary>
    /// <remarks>
    /// Boards define how scores are sorted, filtered, and displayed.
    /// Fetch boards using <see cref="LeadrClient.GetBoardsAsync"/> or
    /// <see cref="LeadrClient.GetBoardAsync"/>.
    /// </remarks>
    public class Board
    {
        /// <summary>
        /// Gets the unique board identifier (e.g., "brd_abc123...").
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Gets the account ID that owns this board.
        /// </summary>
        public string AccountId { get; private set; }

        /// <summary>
        /// Gets the game ID this board belongs to.
        /// </summary>
        public string GameId { get; private set; }

        /// <summary>
        /// Gets the display name of the board.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the URL-friendly slug for the board.
        /// </summary>
        public string Slug { get; private set; }

        /// <summary>
        /// Gets the short code for sharing (e.g., "ABC123").
        /// </summary>
        public string ShortCode { get; private set; }

        /// <summary>
        /// Gets the optional icon URL for the board. May be null.
        /// </summary>
        public string Icon { get; private set; }

        /// <summary>
        /// Gets the unit label for scores (e.g., "points", "seconds"). May be null.
        /// </summary>
        public string Unit { get; private set; }

        /// <summary>
        /// Gets whether the board is active and accepting scores.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Gets whether the board is publicly visible.
        /// </summary>
        public bool IsPublished { get; private set; }

        /// <summary>
        /// Gets the sort direction for scores.
        /// </summary>
        public SortDirection SortDirection { get; private set; }

        /// <summary>
        /// Gets the score keeping strategy (RUN_IDENTITY boards only).
        /// </summary>
        public KeepStrategy KeepStrategy { get; private set; }

        /// <summary>
        /// Gets the list of tags associated with this board.
        /// </summary>
        public List<string> Tags { get; private set; }

        /// <summary>
        /// Gets the optional description of the board. May be null.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets the optional start date for seasonal boards. Null if always active.
        /// </summary>
        public DateTime? StartsAt { get; private set; }

        /// <summary>
        /// Gets the optional end date for seasonal boards. Null if never expires.
        /// </summary>
        public DateTime? EndsAt { get; private set; }

        /// <summary>
        /// Gets when the board was created (UTC).
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Gets when the board was last updated (UTC).
        /// </summary>
        public DateTime UpdatedAt { get; private set; }

        /// <summary>
        /// Gets the board type determining how scores are tracked.
        /// </summary>
        /// <remarks>
        /// - RunIdentity: One entry per identity, uses keep_strategy (BEST, LATEST, FIRST)
        /// - RunRuns: Every submission ranked (arcade-style)
        /// - Counter: Cumulative deltas (XP, wins)
        /// - Ratio: Derived from two boards (win rate, K/D ratio)
        /// </remarks>
        public BoardType Type { get; private set; }

        /// <summary>
        /// Gets the ratio configuration for RATIO boards. Null for other board types.
        /// </summary>
        /// <remarks>
        /// Contains keys: numerator_board_id, denominator_board_id, zero_denominator_policy,
        /// min_denominator, min_numerator, scale, display, decimals, tie_breaker.
        /// </remarks>
        public Dictionary<string, object> RatioConfig { get; private set; }

        internal static Board FromJson(Dictionary<string, object> json)
        {
            if (json == null)
                return null;

            return new Board
            {
                Id = json.GetString("id"),
                AccountId = json.GetString("account_id"),
                GameId = json.GetString("game_id"),
                Name = json.GetString("name"),
                Slug = json.GetString("slug"),
                ShortCode = json.GetString("short_code"),
                Icon = json.GetString("icon"),
                Unit = json.GetString("unit"),
                IsActive = json.GetBool("is_active"),
                IsPublished = json.GetBool("is_published"),
                SortDirection = ParseSortDirection(json.GetString("sort_direction")),
                KeepStrategy = ParseKeepStrategy(json.GetString("keep_strategy")),
                Tags = json.GetStringList("tags"),
                Description = json.GetString("description"),
                StartsAt = json.GetDateTime("starts_at"),
                EndsAt = json.GetDateTime("ends_at"),
                CreatedAt = json.GetDateTimeRequired("created_at"),
                UpdatedAt = json.GetDateTimeRequired("updated_at"),
                Type = ParseBoardType(json.GetString("board_type")),
                RatioConfig = json.GetDict("ratio_config")
            };
        }

        private static BoardType ParseBoardType(string value)
        {
            return value switch
            {
                "RUN_IDENTITY" => BoardType.RunIdentity,
                "RUN_RUNS" => BoardType.RunRuns,
                "COUNTER" => BoardType.Counter,
                "RATIO" => BoardType.Ratio,
                _ => BoardType.RunIdentity
            };
        }

        private static SortDirection ParseSortDirection(string value)
        {
            return value switch
            {
                "ASCENDING" => SortDirection.Ascending,
                _ => SortDirection.Descending
            };
        }

        private static KeepStrategy ParseKeepStrategy(string value)
        {
            return value switch
            {
                "FIRST" => KeepStrategy.First,
                "BEST" => KeepStrategy.Best,
                "LATEST" => KeepStrategy.Latest,
                _ => KeepStrategy.NA
            };
        }
    }
}
