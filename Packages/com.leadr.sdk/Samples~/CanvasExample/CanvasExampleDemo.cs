using System.Collections.Generic;
using System.Threading.Tasks;
using Leadr;
using Leadr.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Leadr.Samples.CanvasExample
{
    /// <summary>
    /// Complete Canvas UI example demonstrating leaderboard display and score submission.
    ///
    /// This sample shows how to:
    /// - Create a board selector dropdown populated from the API
    /// - Display scores in a scrollable list with pagination
    /// - Submit scores with custom metadata
    /// - Handle loading states and errors
    ///
    /// Set up your Canvas with the required UI elements and assign them in the Inspector.
    /// </summary>
    public class CanvasExampleDemo : MonoBehaviour
    {
        [Header("LEADR Configuration")]
        [SerializeField] private LeadrSettings settings;

        [Header("Leaderboard UI References")]
        [SerializeField] private TMP_Dropdown boardDropdown;
        [SerializeField] private Transform scoreListContainer;
        [SerializeField] private GameObject scoreEntryPrefab;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private Button prevPageButton;
        [SerializeField] private TMP_Text pageInfoText;

        [Header("Score Submission UI References")]
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private TMP_InputField scoreValueInput;
        [SerializeField] private TMP_InputField scoreDisplayInput;
        [SerializeField] private Button submitButton;

        [Header("Status")]
        [SerializeField] private TMP_Text statusText;

        [Header("Leaderboard Settings")]
        [SerializeField] private int scoresPerPage = 10;

        [Header("Sample Metadata (simulated game data)")]
        [Tooltip("Simulated play time in seconds")]
        [SerializeField] private float playTimeSeconds = 120f;

        [Tooltip("Simulated level reached")]
        [SerializeField] private int levelReached = 5;

        [Tooltip("Simulated difficulty setting")]
        [SerializeField] private string difficulty = "normal";

        // Store boards for lookup when dropdown selection changes
        private List<Board> boards = new List<Board>();

        // Current scores page - needed for pagination navigation
        private PagedResult<Score> currentScoresPage;

        // Track the currently selected board
        private string selectedBoardId;

        // Prevent double-submission
        private bool isSubmitting;

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

            // Score submission
            if (submitButton != null)
            {
                submitButton.onClick.AddListener(() => _ = SubmitScoreAsync());
            }
        }

        #region Leaderboard Display

        private async Task LoadBoardsAsync()
        {
            SetStatus("Loading boards...");
            SetLeaderboardButtonsInteractable(false);

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
            SetLeaderboardButtonsInteractable(false);
            ClearScoreList();

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
            SetLeaderboardButtonsInteractable(false);

            var result = await currentScoresPage.NextPageAsync();

            if (!result.IsSuccess)
            {
                SetStatus($"Error: {result.Error.Message}");
                SetLeaderboardButtonsInteractable(true);
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
            SetLeaderboardButtonsInteractable(false);

            var result = await currentScoresPage.PrevPageAsync();

            if (!result.IsSuccess)
            {
                SetStatus($"Error: {result.Error.Message}");
                SetLeaderboardButtonsInteractable(true);
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

            foreach (var score in page.Items)
            {
                var entry = Instantiate(scoreEntryPrefab, scoreListContainer);
                var entryUI = entry.GetComponent<ScoreEntryUI>();

                if (entryUI != null)
                {
                    entryUI.SetScore(score.Rank, score);
                }
                else
                {
                    // Fallback for simple text-based prefab
                    var text = entry.GetComponentInChildren<TMP_Text>();
                    if (text != null)
                    {
                        var displayValue = !string.IsNullOrEmpty(score.ValueDisplay)
                            ? score.ValueDisplay
                            : score.Value.ToString("N0");

                        text.text = $"{score.Rank}. {score.PlayerName} - {displayValue}";
                    }
                }
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

            if (boardDropdown != null)
            {
                boardDropdown.interactable = boards.Count > 0;
            }

            if (pageInfoText != null && currentScoresPage != null)
            {
                var hasNav = currentScoresPage.HasNext || currentScoresPage.HasPrev;
                pageInfoText.text = hasNav
                    ? $"Showing {currentScoresPage.Items.Count} scores"
                    : $"{currentScoresPage.Items.Count} scores";
            }
        }

        private void SetLeaderboardButtonsInteractable(bool interactable)
        {
            if (refreshButton != null) refreshButton.interactable = interactable;
            if (nextPageButton != null) nextPageButton.interactable = interactable;
            if (prevPageButton != null) prevPageButton.interactable = interactable;
            if (boardDropdown != null) boardDropdown.interactable = interactable;
        }

        #endregion

        #region Score Submission

        private async Task SubmitScoreAsync()
        {
            if (isSubmitting)
            {
                return;
            }

            if (!ValidateSubmissionInputs())
            {
                return;
            }

            isSubmitting = true;
            SetSubmitButtonInteractable(false);
            SetStatus("Submitting score...");

            var playerName = playerNameInput.text.Trim();
            var scoreValue = long.Parse(scoreValueInput.text);
            var metadata = BuildMetadata();

            // Use custom display value if provided (e.g., "1:23.45" for time-based scores)
            var displayValue = string.IsNullOrWhiteSpace(scoreDisplayInput?.text)
                ? null
                : scoreDisplayInput.text.Trim();

            var result = await LeadrClient.Instance.SubmitScoreAsync(
                selectedBoardId,
                scoreValue,
                playerName,
                displayValue,
                metadata
            );

            isSubmitting = false;
            SetSubmitButtonInteractable(true);

            if (result.IsSuccess)
            {
                HandleSubmitSuccess(result.Data);
            }
            else
            {
                HandleSubmitError(result.Error);
            }
        }

        private bool ValidateSubmissionInputs()
        {
            if (string.IsNullOrEmpty(selectedBoardId))
            {
                SetStatus("Error: Select a board first.");
                return false;
            }

            if (playerNameInput == null || string.IsNullOrWhiteSpace(playerNameInput.text))
            {
                SetStatus("Error: Please enter a player name.");
                return false;
            }

            var playerName = playerNameInput.text.Trim();
            if (playerName.Length < 1 || playerName.Length > 50)
            {
                SetStatus("Error: Player name must be 1-50 characters.");
                return false;
            }

            if (scoreValueInput == null || string.IsNullOrWhiteSpace(scoreValueInput.text))
            {
                SetStatus("Error: Please enter a score value.");
                return false;
            }

            if (!long.TryParse(scoreValueInput.text, out var scoreValue))
            {
                SetStatus("Error: Score must be a valid number.");
                return false;
            }

            if (scoreValue < 0)
            {
                SetStatus("Error: Score cannot be negative.");
                return false;
            }

            return true;
        }

        private Dictionary<string, object> BuildMetadata()
        {
            return new Dictionary<string, object>
            {
                { "level_reached", levelReached },
                { "play_time_seconds", playTimeSeconds },
                { "difficulty", difficulty },
                { "client_version", Application.version },
                { "platform", Application.platform.ToString() }
            };
        }

        private void HandleSubmitSuccess(Score score)
        {
            var displayValue = !string.IsNullOrEmpty(score.ValueDisplay)
                ? score.ValueDisplay
                : score.Value.ToString("N0");

            SetStatus($"Score submitted! {score.PlayerName}: {displayValue}");
            Debug.Log($"[CanvasExample] Score ID: {score.Id}");

            // Refresh the leaderboard to show the new score
            _ = RefreshScoresAsync();
        }

        private void HandleSubmitError(LeadrError error)
        {
            switch (error.StatusCode)
            {
                case 0:
                    SetStatus("Network error. Check your connection.");
                    break;
                case 404:
                    SetStatus("Board not found. Check the board ID.");
                    break;
                case 429:
                    SetStatus("Too many requests. Please wait and try again.");
                    break;
                default:
                    SetStatus($"Error: {error.Message}");
                    break;
            }
            Debug.LogError($"[CanvasExample] {error}");
        }

        private void SetSubmitButtonInteractable(bool interactable)
        {
            if (submitButton != null)
            {
                submitButton.interactable = interactable;
            }
        }

        #endregion

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"[CanvasExample] {message}");
        }
    }
}
