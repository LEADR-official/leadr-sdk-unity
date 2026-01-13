using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leadr.Internal;
using Leadr.Models;
using UnityEngine;

namespace Leadr
{
    /// <summary>
    /// Main entry point for the LEADR SDK. Provides methods to interact with the LEADR leaderboard API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// LeadrClient is a singleton MonoBehaviour that persists across scene loads.
    /// Access it via <see cref="Instance"/> which auto-creates if needed.
    /// </para>
    /// <para>
    /// You must call <see cref="Initialize(LeadrSettings)"/> or <see cref="Initialize(string, string)"/>
    /// before using any API methods. Authentication is handled automatically.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Initialize with settings asset
    /// LeadrClient.Instance.Initialize(mySettings);
    ///
    /// // Fetch board by slug, then fetch scores
    /// var boardResult = await LeadrClient.Instance.GetBoardAsync("weekly");
    /// if (boardResult.IsSuccess)
    /// {
    ///     var scoresResult = await LeadrClient.Instance.GetScoresAsync(boardResult.Data.Id);
    ///     foreach (var score in scoresResult.Data.Items)
    ///         Debug.Log($"{score.PlayerName}: {score.Value}");
    /// }
    /// </code>
    /// </example>
    public class LeadrClient : MonoBehaviour
    {
        private static LeadrClient instance;
        private static readonly object instanceLock = new object();

        private HttpClient httpClient;
        private AuthManager authManager;
        private string gameId;
        private bool debugLogging;
        private bool isInitialized;

        /// <summary>
        /// Gets the singleton instance of LeadrClient, creating it if necessary.
        /// </summary>
        /// <remarks>
        /// If no instance exists, a new GameObject with LeadrClient is created and marked
        /// with DontDestroyOnLoad. The instance persists across scene loads.
        /// </remarks>
        public static LeadrClient Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (instanceLock)
                    {
                        if (instance == null)
                        {
                            var go = new GameObject("LeadrClient");
                            instance = go.AddComponent<LeadrClient>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>
        /// Initializes the LEADR client with a settings asset.
        /// </summary>
        /// <param name="settings">The LeadrSettings ScriptableObject containing configuration.</param>
        /// <remarks>
        /// Create a LeadrSettings asset via Assets > Create > LEADR > Settings.
        /// This method must be called before any API methods.
        /// </remarks>
        public void Initialize(LeadrSettings settings)
        {
            if (settings == null)
            {
                Debug.LogError("[LEADR] Settings cannot be null");
                return;
            }

            settings.Validate();
            Initialize(settings.GameId, settings.BaseUrl, settings.DebugLogging);
        }

        /// <summary>
        /// Initializes the LEADR client with a game ID and optional base URL.
        /// </summary>
        /// <param name="gameId">Your LEADR game ID (e.g., "gam_abc123...").</param>
        /// <param name="baseUrl">
        /// Optional API base URL. Defaults to "https://api.leadrcloud.com/v1/" if null or empty.
        /// </param>
        /// <remarks>
        /// Use this overload for programmatic configuration or when settings come from
        /// an external source. For most cases, prefer <see cref="Initialize(LeadrSettings)"/>.
        /// </remarks>
        public void Initialize(string gameId, string baseUrl = null)
        {
            Initialize(gameId, baseUrl, false);
        }

        private void Initialize(string gameId, string baseUrl, bool debugLogging)
        {
            if (string.IsNullOrEmpty(gameId))
            {
                Debug.LogError("[LEADR] GameId is required");
                return;
            }

            this.gameId = gameId;
            this.debugLogging = debugLogging;

            var url = string.IsNullOrEmpty(baseUrl) ? "https://api.leadrcloud.com/v1/" : baseUrl;
            httpClient = new HttpClient(url, debugLogging);
            authManager = new AuthManager(httpClient, gameId, debugLogging);
            isInitialized = true;

            if (debugLogging)
            {
                Debug.Log($"[LEADR] Initialized with GameId: {gameId}");
            }
        }

        private void EnsureInitialized()
        {
            if (!isInitialized)
            {
                throw new InvalidOperationException(
                    "LeadrClient is not initialized. Call Initialize() first.");
            }
        }

        /// <summary>
        /// Manually starts a new session with the LEADR API.
        /// </summary>
        /// <returns>A result containing the session information or an error.</returns>
        /// <remarks>
        /// You typically don't need to call this directly. Sessions are started automatically
        /// on the first API call that requires authentication. Use this only if you need
        /// to pre-authenticate before making requests.
        /// </remarks>
        public async Task<LeadrResult<Session>> StartSessionAsync()
        {
            EnsureInitialized();
            return await authManager.StartSessionAsync();
        }

        /// <summary>
        /// Fetches a paginated list of boards for the current game.
        /// </summary>
        /// <param name="limit">Maximum boards per page (1-100, default 20).</param>
        /// <returns>A result containing the paginated boards or an error.</returns>
        /// <remarks>
        /// Use <see cref="PagedResult{T}.NextPageAsync"/> to fetch additional pages.
        /// </remarks>
        public async Task<LeadrResult<PagedResult<Board>>> GetBoardsAsync(int limit = 20)
        {
            return await GetBoardsInternalAsync(limit, null);
        }

        private async Task<LeadrResult<PagedResult<Board>>> GetBoardsInternalAsync(int limit, string cursor)
        {
            EnsureInitialized();

            var endpoint = $"/v1/client/boards?game_id={Uri.EscapeDataString(gameId)}&limit={limit}";
            if (!string.IsNullOrEmpty(cursor))
            {
                endpoint += $"&cursor={Uri.EscapeDataString(cursor)}";
            }

            return await authManager.ExecuteAuthenticatedAsync(
                headers => httpClient.GetAsync(endpoint, headers),
                json => PagedResult<Board>.FromJson(
                    json,
                    Board.FromJson,
                    c => GetBoardsInternalAsync(limit, c)));
        }

        /// <summary>
        /// Fetches a single board by its slug.
        /// </summary>
        /// <param name="boardSlug">The board slug (e.g., "weekly", "all-time").</param>
        /// <returns>A result containing the board or an error.</returns>
        public async Task<LeadrResult<Board>> GetBoardAsync(string boardSlug)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(boardSlug))
            {
                return LeadrResult<Board>.Failure(0, "invalid_argument", "boardSlug is required");
            }

            var endpoint = $"/v1/client/boards/?slug={Uri.EscapeDataString(boardSlug)}&game_id={Uri.EscapeDataString(gameId)}";

            // The slug endpoint returns a paginated list, so we parse it and extract the first item
            var pagedResult = await authManager.ExecuteAuthenticatedAsync(
                headers => httpClient.GetAsync(endpoint, headers),
                json => PagedResult<Board>.FromJson(json, Board.FromJson, null));

            if (!pagedResult.IsSuccess)
            {
                return LeadrResult<Board>.Failure(pagedResult.Error);
            }

            if (pagedResult.Data.Items.Count == 0)
            {
                return LeadrResult<Board>.Failure(404, "not_found", $"Board with slug '{boardSlug}' not found");
            }

            return LeadrResult<Board>.Success(pagedResult.Data.Items[0]);
        }

        /// <summary>
        /// Fetches a paginated list of scores for the specified board.
        /// </summary>
        /// <param name="boardId">The board ID (e.g., "brd_abc123...").</param>
        /// <param name="limit">Maximum scores per page (1-100, default 20).</param>
        /// <param name="sort">
        /// Optional sort string (e.g., "value:desc,created_at:desc").
        /// If null, uses the board's default sort direction.
        /// </param>
        /// <param name="aroundScoreId">
        /// Optional score ID to center results around (e.g., "scr_abc123...").
        /// Cannot be used with <paramref name="aroundScoreValue"/>.
        /// </param>
        /// <param name="aroundScoreValue">
        /// Optional score value to center results around.
        /// Cannot be used with <paramref name="aroundScoreId"/>.
        /// </param>
        /// <returns>A result containing the paginated scores or an error.</returns>
        /// <remarks>
        /// Use <see cref="PagedResult{T}.NextPageAsync"/> to fetch additional pages.
        /// When using <paramref name="aroundScoreId"/> or <paramref name="aroundScoreValue"/>,
        /// scores will be centered around the specified target with context above and below.
        /// </remarks>
        public async Task<LeadrResult<PagedResult<Score>>> GetScoresAsync(
            string boardId,
            int limit = 20,
            string sort = null,
            string aroundScoreId = null,
            double? aroundScoreValue = null)
        {
            return await GetScoresInternalAsync(boardId, limit, sort, null, aroundScoreId, aroundScoreValue);
        }

        private async Task<LeadrResult<PagedResult<Score>>> GetScoresInternalAsync(
            string boardId,
            int limit,
            string sort,
            string cursor,
            string aroundScoreId = null,
            double? aroundScoreValue = null)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(boardId))
            {
                return LeadrResult<PagedResult<Score>>.Failure(
                    0, "invalid_argument", "boardId is required");
            }

            // Validate mutual exclusivity
            if (!string.IsNullOrEmpty(aroundScoreId) && aroundScoreValue.HasValue)
            {
                return LeadrResult<PagedResult<Score>>.Failure(
                    0, "invalid_argument", "aroundScoreId and aroundScoreValue cannot both be specified");
            }

            if (!string.IsNullOrEmpty(cursor) && (!string.IsNullOrEmpty(aroundScoreId) || aroundScoreValue.HasValue))
            {
                return LeadrResult<PagedResult<Score>>.Failure(
                    0, "invalid_argument", "around parameters cannot be used with cursor pagination");
            }

            var endpoint = $"/v1/client/scores?board_id={Uri.EscapeDataString(boardId)}&limit={limit}";

            if (!string.IsNullOrEmpty(sort))
            {
                endpoint += $"&sort={Uri.EscapeDataString(sort)}";
            }

            if (!string.IsNullOrEmpty(cursor))
            {
                endpoint += $"&cursor={Uri.EscapeDataString(cursor)}";
            }

            if (!string.IsNullOrEmpty(aroundScoreId))
            {
                endpoint += $"&around_score_id={Uri.EscapeDataString(aroundScoreId)}";
            }

            if (aroundScoreValue.HasValue)
            {
                endpoint += $"&around_score_value={aroundScoreValue.Value}";
            }

            return await authManager.ExecuteAuthenticatedAsync(
                headers => httpClient.GetAsync(endpoint, headers),
                json => PagedResult<Score>.FromJson(
                    json,
                    Score.FromJson,
                    c => GetScoresInternalAsync(boardId, limit, sort, c)));
        }

        /// <summary>
        /// Fetches a single score by its ID.
        /// </summary>
        /// <param name="scoreId">The score ID (e.g., "scr_abc123...").</param>
        /// <returns>A result containing the score or an error.</returns>
        public async Task<LeadrResult<Score>> GetScoreAsync(string scoreId)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(scoreId))
            {
                return LeadrResult<Score>.Failure(0, "invalid_argument", "scoreId is required");
            }

            var endpoint = $"/v1/client/scores/{Uri.EscapeDataString(scoreId)}";

            return await authManager.ExecuteAuthenticatedAsync(
                headers => httpClient.GetAsync(endpoint, headers),
                Score.FromJson);
        }

        /// <summary>
        /// Submits a new score to the specified board.
        /// </summary>
        /// <param name="boardId">The board ID (e.g., "brd_abc123...").</param>
        /// <param name="score">The score value.</param>
        /// <param name="playerName">The player's display name (required).</param>
        /// <param name="valueDisplay">
        /// Optional formatted display string (e.g., "1:23.45" for time, "1,234 points").
        /// If not provided, clients should fall back to formatting the raw score value.
        /// </param>
        /// <param name="metadata">
        /// Optional custom metadata (max 1KB). Can include game state, achievements,
        /// platform info, or any JSON-serializable data.
        /// </param>
        /// <returns>A result containing the created score or an error.</returns>
        /// <remarks>
        /// This method automatically handles nonce generation required for mutations.
        /// On 412 errors (nonce issues), it will retry once with a fresh nonce.
        /// </remarks>
        /// <example>
        /// <code>
        /// var metadata = new Dictionary&lt;string, object&gt;
        /// {
        ///     { "level", 5 },
        ///     { "difficulty", "hard" }
        /// };
        /// var result = await LeadrClient.Instance.SubmitScoreAsync(
        ///     "brd_abc123", 1000, "PlayerOne", "1,000 pts", metadata);
        /// </code>
        /// </example>
        public async Task<LeadrResult<Score>> SubmitScoreAsync(
            string boardId,
            long score,
            string playerName,
            string valueDisplay = null,
            Dictionary<string, object> metadata = null)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(boardId))
            {
                return LeadrResult<Score>.Failure(0, "invalid_argument", "boardId is required");
            }

            if (string.IsNullOrEmpty(playerName))
            {
                return LeadrResult<Score>.Failure(0, "invalid_argument", "playerName is required");
            }

            var body = new Dictionary<string, object>
            {
                { "board_id", boardId },
                { "value", score },
                { "player_name", playerName }
            };

            if (!string.IsNullOrEmpty(valueDisplay))
            {
                body["value_display"] = valueDisplay;
            }

            if (metadata != null)
            {
                body["metadata"] = metadata;
            }

            return await authManager.ExecuteAuthenticatedAsync(
                headers => httpClient.PostAsync("/v1/client/scores", body, headers),
                Score.FromJson,
                requiresNonce: true);
        }
    }
}
