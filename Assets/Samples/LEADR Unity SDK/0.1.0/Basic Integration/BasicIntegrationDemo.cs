using System.Threading.Tasks;
using Leadr;
using Leadr.Models;
using UnityEngine;

namespace Leadr.Samples.BasicIntegration
{
    /// <summary>
    /// Demonstrates basic LEADR SDK integration.
    ///
    /// This sample shows how to:
    /// - Initialize the LeadrClient with settings
    /// - Fetch the list of boards for your game
    /// - Handle async/await patterns in Unity
    ///
    /// Attach this script to any GameObject in your scene.
    /// </summary>
    public class BasicIntegrationDemo : MonoBehaviour
    {
        [Header("LEADR Configuration")]
        [Tooltip("Reference to your LeadrSettings asset")]
        [SerializeField] private LeadrSettings settings;

        [Header("Alternative: Direct Configuration")]
        [Tooltip("Your Game ID (used if settings is not assigned)")]
        [SerializeField] private string gameId;

        private async void Start()
        {
            // Initialize the LEADR client
            // The client is a singleton - you only need to initialize it once
            InitializeClient();

            // Fetch and display boards
            await FetchAndLogBoardsAsync();
        }

        private void InitializeClient()
        {
            // Option 1: Use a LeadrSettings ScriptableObject (recommended)
            // This allows you to configure settings in the Unity Editor
            if (settings != null)
            {
                LeadrClient.Instance.Initialize(settings);
                Debug.Log("[BasicDemo] Initialized with LeadrSettings asset");
                return;
            }

            // Option 2: Initialize with just a game ID
            // Useful for quick testing or when settings come from elsewhere
            if (!string.IsNullOrEmpty(gameId))
            {
                LeadrClient.Instance.Initialize(gameId);
                Debug.Log("[BasicDemo] Initialized with game ID");
                return;
            }

            Debug.LogError("[BasicDemo] No settings or game ID configured!");
        }

        private async Task FetchAndLogBoardsAsync()
        {
            Debug.Log("[BasicDemo] Fetching boards...");

            // GetBoardsAsync returns a LeadrResult<PagedResult<Board>>
            // LeadrResult wraps the response and includes error handling
            var result = await LeadrClient.Instance.GetBoardsAsync(limit: 10);

            // Always check IsSuccess before accessing Data
            if (!result.IsSuccess)
            {
                // Error contains status code, error code, and message
                Debug.LogError($"[BasicDemo] Failed to fetch boards: {result.Error}");
                return;
            }

            // PagedResult contains Items (the data), Count, and pagination info
            var boards = result.Data;
            Debug.Log($"[BasicDemo] Found {boards.Items.Count} boards:");

            foreach (var board in boards.Items)
            {
                LogBoard(board);
            }

            // Check if there are more pages available
            if (boards.HasNext)
            {
                Debug.Log("[BasicDemo] More boards available. Use NextPageAsync() to fetch them.");
            }
        }

        private void LogBoard(Board board)
        {
            // Board properties available:
            // - Id: Unique identifier (e.g., "brd_abc123...")
            // - Name: Display name
            // - Slug: URL-friendly identifier
            // - ShortCode: Short code for sharing
            // - SortDirection: "ascending" or "descending"
            // - KeepStrategy: "all", "highest", or "latest"
            // - IsActive, IsPublished: Status flags
            // - Tags: List of string tags
            // - StartsAt, EndsAt: Optional season dates

            var status = board.IsActive ? "Active" : "Inactive";
            var published = board.IsPublished ? "Published" : "Draft";

            Debug.Log($"  - {board.Name} ({board.ShortCode})");
            Debug.Log($"    ID: {board.Id}");
            Debug.Log($"    Sort: {board.SortDirection}, Keep: {board.KeepStrategy}");
            Debug.Log($"    Status: {status}, {published}");

            if (board.Tags != null && board.Tags.Count > 0)
            {
                Debug.Log($"    Tags: {string.Join(", ", board.Tags)}");
            }
        }
    }
}
