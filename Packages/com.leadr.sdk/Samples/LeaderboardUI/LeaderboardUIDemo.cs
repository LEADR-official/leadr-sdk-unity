using System.Collections.Generic;
using System.Threading.Tasks;
using Leadr;
using Leadr.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Leadr.Samples.LeaderboardUI
{
    /// <summary>
    /// Demonstrates a complete leaderboard UI with pagination.
    ///
    /// This sample shows how to:
    /// - Create a board selector dropdown populated from the API
    /// - Display scores in a scrollable list
    /// - Implement pagination with Next/Previous buttons
    /// - Handle loading states and errors
    ///
    /// Set up your Canvas with the required UI elements and assign them in the Inspector.
    /// </summary>
    public class LeaderboardUIDemo : MonoBehaviour
    {
        [Header("LEADR Configuration")]
        [SerializeField] private LeadrSettings settings;

        [Header("UI References")]
        [SerializeField] private TMP_Dropdown boardDropdown;
        [SerializeField] private Transform scoreListContainer;
        [SerializeField] private GameObject scoreEntryPrefab;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private Button prevPageButton;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text pageInfoText;

        [Header("Settings")]
        [SerializeField] private int scoresPerPage = 10;

        // Store boards for lookup when dropdown selection changes
        private List<Board> boards = new List<Board>();

        // Current scores page - needed for pagination navigation
        private PagedResult<Score> currentScoresPage;

        // Track the currently selected board
        private string selectedBoardId;

        private async void Start()
        {
            // Initialize the LEADR client
            if (settings != null)
            {
                LeadrClient.Instance.Initialize(settings);
            }

            // Set up button listeners
            SetupButtonListeners();

            // Populate the board dropdown
            await LoadBoardsAsync();
        }

        private void SetupButtonListeners()
        {
            // Refresh reloads the current board's scores
            if (refreshButton != null)
            {
                refreshButton.onClick.AddListener(() => _ = RefreshScoresAsync());
            }

            // Next/Previous page buttons use PagedResult's built-in pagination
            if (nextPageButton != null)
            {
                nextPageButton.onClick.AddListener(() => _ = LoadNextPageAsync());
            }

            if (prevPageButton != null)
            {
                prevPageButton.onClick.AddListener(() => _ = LoadPrevPageAsync());
            }

            // Board selection triggers score reload
            if (boardDropdown != null)
            {
                boardDropdown.onValueChanged.AddListener(OnBoardSelected);
            }
        }

        private async Task LoadBoardsAsync()
        {
            SetStatus("Loading boards...");
            SetButtonsInteractable(false);

            var result = await LeadrClient.Instance.GetBoardsAsync();

            if (!result.IsSuccess)
            {
                SetStatus($"Error: {result.Error.Message}");
                return;
            }

            boards = result.Data.Items;

            // Populate dropdown with board names
            if (boardDropdown != null)
            {
                boardDropdown.ClearOptions();
                var options = new List<string>();

                foreach (var board in boards)
                {
                    // Show name and short code for easy identification
                    options.Add($"{board.Name} ({board.ShortCode})");
                }

                boardDropdown.AddOptions(options);

                // Select first board if available
                if (boards.Count > 0)
                {
                    boardDropdown.value = 0;
                    OnBoardSelected(0);
                }
                else
                {
                    SetStatus("No boards found for this game.");
                }
            }
        }

        private void OnBoardSelected(int index)
        {
            if (index >= 0 && index < boards.Count)
            {
                selectedBoardId = boards[index].Id;
                _ = LoadScoresAsync(selectedBoardId);
            }
        }

        private async Task LoadScoresAsync(string boardId)
        {
            SetStatus("Loading scores...");
            SetButtonsInteractable(false);
            ClearScoreList();

            // GetScoresAsync fetches scores for a specific board
            // The 'sort' parameter is optional - boards have a default sort direction
            var result = await LeadrClient.Instance.GetScoresAsync(
                boardId,
                limit: scoresPerPage,
                sort: null // Use board's default sort
            );

            if (!result.IsSuccess)
            {
                SetStatus($"Error: {result.Error.Message}");
                return;
            }

            // Store the page for pagination navigation
            currentScoresPage = result.Data;

            DisplayScores(currentScoresPage);
            UpdatePaginationButtons();
            SetStatus($"Loaded {currentScoresPage.Items.Count} scores");
        }

        private async Task RefreshScoresAsync()
        {
            if (!string.IsNullOrEmpty(selectedBoardId))
            {
                await LoadScoresAsync(selectedBoardId);
            }
        }

        private async Task LoadNextPageAsync()
        {
            if (currentScoresPage == null || !currentScoresPage.HasNext)
                return;

            SetStatus("Loading next page...");
            SetButtonsInteractable(false);

            // PagedResult handles cursor-based pagination internally
            // NextPageAsync() fetches the next page using the stored cursor
            var result = await currentScoresPage.NextPageAsync();

            if (!result.IsSuccess)
            {
                SetStatus($"Error: {result.Error.Message}");
                SetButtonsInteractable(true);
                return;
            }

            currentScoresPage = result.Data;
            ClearScoreList();
            DisplayScores(currentScoresPage);
            UpdatePaginationButtons();
            SetStatus("Loaded next page");
        }

        private async Task LoadPrevPageAsync()
        {
            if (currentScoresPage == null || !currentScoresPage.HasPrev)
                return;

            SetStatus("Loading previous page...");
            SetButtonsInteractable(false);

            // PrevPageAsync() works the same way as NextPageAsync()
            var result = await currentScoresPage.PrevPageAsync();

            if (!result.IsSuccess)
            {
                SetStatus($"Error: {result.Error.Message}");
                SetButtonsInteractable(true);
                return;
            }

            currentScoresPage = result.Data;
            ClearScoreList();
            DisplayScores(currentScoresPage);
            UpdatePaginationButtons();
            SetStatus("Loaded previous page");
        }

        private void DisplayScores(PagedResult<Score> page)
        {
            if (scoreListContainer == null || scoreEntryPrefab == null)
            {
                // Fallback: log scores to console
                foreach (var score in page.Items)
                {
                    Debug.Log($"{score.PlayerName}: {score.Value}");
                }
                return;
            }

            int rank = 1; // Would need to calculate actual rank based on page position
            foreach (var score in page.Items)
            {
                var entry = Instantiate(scoreEntryPrefab, scoreListContainer);
                var entryUI = entry.GetComponent<ScoreEntryUI>();

                if (entryUI != null)
                {
                    entryUI.SetScore(rank, score);
                }
                else
                {
                    // Fallback for simple text-based prefab
                    var text = entry.GetComponentInChildren<TMP_Text>();
                    if (text != null)
                    {
                        // Use ValueDisplay if available (formatted by server)
                        // Otherwise fall back to raw Value
                        var displayValue = !string.IsNullOrEmpty(score.ValueDisplay)
                            ? score.ValueDisplay
                            : score.Value.ToString("N0");

                        text.text = $"{rank}. {score.PlayerName} - {displayValue}";
                    }
                }

                rank++;
            }
        }

        private void ClearScoreList()
        {
            if (scoreListContainer == null)
                return;

            foreach (Transform child in scoreListContainer)
            {
                Destroy(child.gameObject);
            }
        }

        private void UpdatePaginationButtons()
        {
            if (nextPageButton != null)
            {
                nextPageButton.interactable = currentScoresPage?.HasNext ?? false;
            }

            if (prevPageButton != null)
            {
                prevPageButton.interactable = currentScoresPage?.HasPrev ?? false;
            }

            if (refreshButton != null)
            {
                refreshButton.interactable = !string.IsNullOrEmpty(selectedBoardId);
            }

            // Update page info text
            if (pageInfoText != null && currentScoresPage != null)
            {
                var hasNav = currentScoresPage.HasNext || currentScoresPage.HasPrev;
                pageInfoText.text = hasNav
                    ? $"Showing {currentScoresPage.Items.Count} scores"
                    : $"{currentScoresPage.Items.Count} scores";
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (refreshButton != null) refreshButton.interactable = interactable;
            if (nextPageButton != null) nextPageButton.interactable = interactable;
            if (prevPageButton != null) prevPageButton.interactable = interactable;
            if (boardDropdown != null) boardDropdown.interactable = interactable;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"[LeaderboardUI] {message}");
        }
    }
}
