using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leadr.Internal;
using Leadr.Models;
using UnityEngine;

namespace Leadr
{
    public class LeadrClient : MonoBehaviour
    {
        private static LeadrClient instance;
        private static readonly object instanceLock = new object();

        private HttpClient httpClient;
        private AuthManager authManager;
        private string gameId;
        private bool debugLogging;
        private bool isInitialized;

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

        public async Task<LeadrResult<Session>> StartSessionAsync()
        {
            EnsureInitialized();
            return await authManager.StartSessionAsync();
        }

        public async Task<LeadrResult<PagedResult<Board>>> GetBoardsAsync(int limit = 20)
        {
            return await GetBoardsInternalAsync(limit, null);
        }

        private async Task<LeadrResult<PagedResult<Board>>> GetBoardsInternalAsync(int limit, string cursor)
        {
            EnsureInitialized();

            var endpoint = $"/v1/boards?game_id={Uri.EscapeDataString(gameId)}&limit={limit}";
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

        public async Task<LeadrResult<Board>> GetBoardAsync(string boardId)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(boardId))
            {
                return LeadrResult<Board>.Failure(0, "invalid_argument", "boardId is required");
            }

            var endpoint = $"/v1/boards/{Uri.EscapeDataString(boardId)}";

            return await authManager.ExecuteAuthenticatedAsync(
                headers => httpClient.GetAsync(endpoint, headers),
                Board.FromJson);
        }

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

            var endpoint = $"/v1/scores?board_id={Uri.EscapeDataString(boardId)}&limit={limit}";

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
                headers => httpClient.PostAsync("/v1/scores", body, headers),
                Score.FromJson,
                requiresNonce: true);
        }
    }
}
