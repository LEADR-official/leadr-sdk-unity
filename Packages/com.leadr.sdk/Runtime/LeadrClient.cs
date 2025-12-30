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
    /// // Fetch scores
    /// var result = await LeadrClient.Instance.GetScoresAsync("brd_abc123");
    /// if (result.IsSuccess)
    /// {
    ///     foreach (var score in result.Data.Items)
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
        /// Fetches a single board by its ID.
        /// </summary>
        /// <param name="boardId">The board ID (e.g., "brd_abc123...").</param>
        /// <returns>A result containing the board or an error.</returns>
        public async Task<LeadrResult<Board>> GetBoardAsync(string boardId)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(boardId))
            {
                return LeadrResult<Board>.Failure(0, "invalid_argument", "boardId is required");
            }

            var endpoint = $"/v1/client/boards/{Uri.EscapeDataString(boardId)}";

            return await authManager.ExecuteAuthenticatedAsync(
                headers => httpClient.GetAsync(endpoint, headers),
                Board.FromJson);
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
        /// <returns>A result containing the paginated scores or an error.</returns>
        /// <remarks>
        /// Use <see cref="PagedResult{T}.NextPageAsync"/> to fetch additional pages.
        /// </remarks>
        public async Task<LeadrResult<PagedResult<Score>>> GetScoresAsync(
            string boardId,
            int limit = 20,
            string sort = null)
        {
            return await GetScoresInternalAsync(boardId, limit, sort, null);
        }

        private async Task<LeadrResult<PagedResult<Score>>> GetScoresInternalAsync(
            string boardId,
            int limit,
            string sort,
            string cursor)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(boardId))
            {
                return LeadrResult<PagedResult<Score>>.Failure(
                    0, "invalid_argument", "boardId is required");
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

            return await authManager.ExecuteAuthenticatedAsync(
                headers => httpClient.GetAsync(endpoint, headers),
                json => PagedResult<Score>.FromJson(
                    json,
                    Score.FromJson,
                    c => GetScoresInternalAsync(boardId, limit, sort, c)));
        }

        /// <summary>
        /// Submits a new score to the specified board.
        /// </summary>
        /// <param name="boardId">The board ID (e.g., "brd_abc123...").</param>
        /// <param name="score">The score value.</param>
        /// <param name="playerName">The player's display name (required).</param>
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
        ///     "brd_abc123", 1000, "PlayerOne", metadata);
        /// </code>
        /// </example>
        public async Task<LeadrResult<Score>> SubmitScoreAsync(
            string boardId,
            long score,
            string playerName,
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
