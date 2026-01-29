namespace Leadr.Models
{
    /// <summary>
    /// The type of identity used for player identification.
    /// </summary>
    public enum IdentityKind
    {
        /// <summary>Device fingerprint-based identity (default).</summary>
        Device,
        /// <summary>Steam authentication identity.</summary>
        Steam,
        /// <summary>Custom identity provider.</summary>
        Custom
    }

    /// <summary>
    /// The type of leaderboard determining how scores are tracked.
    /// </summary>
    public enum BoardType
    {
        /// <summary>One entry per identity, uses keep_strategy (BEST, LATEST, FIRST).</summary>
        RunIdentity,
        /// <summary>Every submission ranked (arcade-style).</summary>
        RunRuns,
        /// <summary>Cumulative deltas (XP, wins).</summary>
        Counter,
        /// <summary>Derived from two boards (win rate, K/D ratio).</summary>
        Ratio
    }

    /// <summary>
    /// The sort direction for board scores.
    /// </summary>
    public enum SortDirection
    {
        /// <summary>Lower scores rank first (e.g., time-based).</summary>
        Ascending,
        /// <summary>Higher scores rank first (e.g., points).</summary>
        Descending
    }

    /// <summary>
    /// The strategy for keeping scores from the same player (RUN_IDENTITY boards only).
    /// </summary>
    public enum KeepStrategy
    {
        /// <summary>Keep the first score submitted.</summary>
        First,
        /// <summary>Keep the best score.</summary>
        Best,
        /// <summary>Keep the most recent score.</summary>
        Latest,
        /// <summary>Not applicable (non-RUN_IDENTITY boards).</summary>
        NA
    }
}
