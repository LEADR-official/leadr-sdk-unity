using System.Collections.Generic;
using System.Threading.Tasks;
using Leadr;
using Leadr.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Leadr.Samples.ScoreSubmission
{
    /// <summary>
    /// Demonstrates score submission with metadata.
    ///
    /// This sample shows how to:
    /// - Submit scores to a LEADR board
    /// - Include custom metadata with score submissions
    /// - Handle submission success and errors
    /// - Validate input before submission
    ///
    /// Metadata is useful for storing additional context like:
    /// - Game version, difficulty level, character used
    /// - Play time, levels completed, achievements unlocked
    /// - Platform, device info, or any custom game data
    /// </summary>
    public class ScoreSubmissionDemo : MonoBehaviour
    {
        [Header("LEADR Configuration")]
        [SerializeField] private LeadrSettings settings;

        [Tooltip("The board ID to submit scores to")]
        [SerializeField] private string boardId;

        [Header("UI References")]
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private TMP_InputField scoreValueInput;
        [SerializeField] private Button submitButton;
        [SerializeField] private TMP_Text statusText;

        [Header("Sample Metadata (simulated game data)")]
        [Tooltip("Simulated play time in seconds")]
        [SerializeField] private float playTimeSeconds = 120f;

        [Tooltip("Simulated level reached")]
        [SerializeField] private int levelReached = 5;

        [Tooltip("Simulated difficulty setting")]
        [SerializeField] private string difficulty = "normal";

        private bool isSubmitting;

        private void Start()
        {
            // Initialize the LEADR client
            if (settings != null)
            {
                LeadrClient.Instance.Initialize(settings);
            }

            // Set up submit button
            if (submitButton != null)
            {
                submitButton.onClick.AddListener(() => _ = SubmitScoreAsync());
            }

            SetStatus("Enter your name and score, then click Submit.");
        }

        private async Task SubmitScoreAsync()
        {
            // Prevent double-submission
            if (isSubmitting)
            {
                return;
            }

            // Validate inputs
            if (!ValidateInputs())
            {
                return;
            }

            isSubmitting = true;
            SetButtonInteractable(false);
            SetStatus("Submitting score...");

            // Get input values
            var playerName = playerNameInput.text.Trim();
            var scoreValue = long.Parse(scoreValueInput.text);

            // Build metadata dictionary
            // Metadata can contain any JSON-serializable data (max 1KB)
            // Common uses: game state, achievements, platform info, data references
            var metadata = BuildMetadata();

            // Submit the score
            // This requires a valid nonce (handled automatically by the SDK)
            var result = await LeadrClient.Instance.SubmitScoreAsync(
                boardId,
                scoreValue,
                playerName,
                metadata
            );

            isSubmitting = false;
            SetButtonInteractable(true);

            if (result.IsSuccess)
            {
                HandleSuccess(result.Data);
            }
            else
            {
                HandleError(result.Error);
            }
        }

        private bool ValidateInputs()
        {
            // Check board ID is configured
            if (string.IsNullOrEmpty(boardId))
            {
                SetStatus("Error: Board ID not configured. Set it in the Inspector.");
                return false;
            }

            // Check player name
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

            // Check score value
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
            // Build metadata with game context
            // This data is stored with the score and returned when fetching scores
            var metadata = new Dictionary<string, object>
            {
                // Game progress info
                { "level_reached", levelReached },
                { "play_time_seconds", playTimeSeconds },
                { "difficulty", difficulty },

                // You could also include:
                // - Character or loadout used
                // - Game mode or variant
                // - Replay or screenshot references
                // - Client version for compatibility tracking

                { "client_version", Application.version },
                { "platform", Application.platform.ToString() }
            };

            return metadata;
        }

        private void HandleSuccess(Score score)
        {
            // Score was submitted successfully
            // The returned Score object contains the server-assigned ID and timestamps

            var displayValue = !string.IsNullOrEmpty(score.ValueDisplay)
                ? score.ValueDisplay
                : score.Value.ToString("N0");

            SetStatus($"Score submitted! {score.PlayerName}: {displayValue}");
            Debug.Log($"[ScoreSubmission] Score ID: {score.Id}");
            Debug.Log($"[ScoreSubmission] Created at: {score.CreatedAt}");

            // Optionally clear inputs for next submission
            // scoreValueInput.text = "";
        }

        private void HandleError(LeadrError error)
        {
            // Common error scenarios:
            // - Network error (StatusCode = 0): No internet connection
            // - 400 Bad Request: Invalid data format
            // - 401 Unauthorized: Session expired (SDK auto-refreshes, rare to see)
            // - 403 Forbidden: Board not accepting submissions
            // - 404 Not Found: Invalid board ID
            // - 412 Precondition Failed: Nonce issue (SDK auto-retries)
            // - 429 Too Many Requests: Rate limited

            SetStatus($"Error: {error.Message}");
            Debug.LogError($"[ScoreSubmission] {error}");

            // You might want to show different UI for different errors
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
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"[ScoreSubmission] {message}");
        }

        private void SetButtonInteractable(bool interactable)
        {
            if (submitButton != null)
            {
                submitButton.interactable = interactable;
            }
        }
    }
}
