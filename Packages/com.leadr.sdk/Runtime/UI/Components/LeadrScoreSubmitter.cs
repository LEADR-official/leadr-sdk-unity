using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leadr.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace Leadr.UI
{
    /// <summary>
    /// State enum for the score submitter.
    /// </summary>
    public enum SubmitterState
    {
        /// <summary>Ready to submit.</summary>
        Idle,
        /// <summary>Currently submitting.</summary>
        Submitting,
        /// <summary>Submission succeeded.</summary>
        Success,
        /// <summary>Submission failed.</summary>
        Error
    }

    /// <summary>
    /// Event args for successful score submission.
    /// </summary>
    public class ScoreSubmittedEventArgs
    {
        /// <summary>Gets the submitted score.</summary>
        public Score Score { get; }
        /// <summary>Gets the player name used.</summary>
        public string PlayerName { get; }
        /// <summary>Gets the score value submitted.</summary>
        public long Value { get; }

        /// <summary>
        /// Creates new score submitted event args.
        /// </summary>
        public ScoreSubmittedEventArgs(Score score, string playerName, long value)
        {
            Score = score;
            PlayerName = playerName;
            Value = value;
        }
    }

    /// <summary>
    /// A custom VisualElement for submitting scores to a leaderboard.
    /// Includes input validation, loading states, and success/error feedback.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For games that track score internally, use <see cref="SetScore"/> to set the value
    /// programmatically, then let the player enter their name and submit.
    /// </para>
    /// <para>
    /// For manual score entry, set <see cref="ShowScoreInput"/> to true.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var submitter = new LeadrScoreSubmitter
    /// {
    ///     Board = "weekly"
    /// };
    /// submitter.SetScore(1000, "1,000 pts");
    /// submitter.ScoreSubmitted += args => Debug.Log($"Submitted: {args.Score.Id}");
    /// </code>
    /// </example>
    [UxmlElement]
    public partial class LeadrScoreSubmitter : VisualElement
    {
        /// <summary>USS class name for the root element.</summary>
        public const string UssClassName = "leadr-score-submitter";
        /// <summary>USS class name for the form container.</summary>
        public const string UssFormClassName = UssClassName + "__form";
        /// <summary>USS class name for input fields.</summary>
        public const string UssInputClassName = UssClassName + "__input";
        /// <summary>USS class name for the submit button.</summary>
        public const string UssButtonClassName = UssClassName + "__button";
        /// <summary>USS class name for the feedback container.</summary>
        public const string UssFeedbackClassName = UssClassName + "__feedback";
        /// <summary>USS class name for the score display.</summary>
        public const string UssScoreDisplayClassName = UssClassName + "__score-display";
        /// <summary>USS class name for the validation message.</summary>
        public const string UssValidationClassName = UssClassName + "__validation";
        /// <summary>USS class name prefix for state modifiers.</summary>
        public const string UssStatePrefix = UssClassName + "--state-";

        // Child elements
        private readonly VisualElement m_Form;
        private readonly TextField m_PlayerNameField;
        private readonly TextField m_ScoreValueField;
        private readonly Label m_ScoreDisplayLabel;
        private readonly Button m_SubmitButton;
        private readonly VisualElement m_FeedbackContainer;
        private readonly Label m_FeedbackLabel;
        private readonly Label m_ValidationLabel;

        // State
        private SubmitterState m_State = SubmitterState.Idle;
        private long m_ScoreValue;
        private string m_ValueDisplay;
        private Dictionary<string, object> m_Metadata;
        private bool m_IsValid;

        // Configuration
        private string m_Board = "";
        private string m_ResolvedBoardId;
        private int m_MinNameLength = 1;
        private int m_MaxNameLength = 50;
        private bool m_ShowScoreInput;
        private bool m_ClearOnSuccess;
        private string m_SubmitButtonText = "Submit Score";
        private string m_SubmittingText = "Submitting...";

        // Client reference
        private LeadrClient m_Client;

        /// <summary>Raised when score submission succeeds.</summary>
        public event Action<ScoreSubmittedEventArgs> ScoreSubmitted;
        /// <summary>Raised when score submission fails.</summary>
        public event Action<LeadrError> SubmissionFailed;
        /// <summary>Raised when the state changes.</summary>
        public event Action<SubmitterState> StateChanged;
        /// <summary>Raised when validation state changes.</summary>
        public event Action<bool> ValidationChanged;

        /// <summary>
        /// Gets or sets the board slug to submit scores to (e.g., "weekly", "all-time").
        /// </summary>
        [UxmlAttribute("board")]
        public string Board
        {
            get => m_Board;
            set
            {
                m_Board = value;
                m_ResolvedBoardId = null;  // Clear cached ID when slug changes
                Validate();
            }
        }

        /// <summary>
        /// Gets or sets the minimum player name length.
        /// </summary>
        [UxmlAttribute("min-name-length")]
        public int MinNameLength
        {
            get => m_MinNameLength;
            set
            {
                m_MinNameLength = Math.Max(1, value);
                Validate();
            }
        }

        /// <summary>
        /// Gets or sets the maximum player name length.
        /// </summary>
        [UxmlAttribute("max-name-length")]
        public int MaxNameLength
        {
            get => m_MaxNameLength;
            set
            {
                m_MaxNameLength = Math.Max(m_MinNameLength, value);
                m_PlayerNameField.maxLength = m_MaxNameLength;
                Validate();
            }
        }

        /// <summary>
        /// Gets or sets whether to show the score input field (for manual entry).
        /// </summary>
        [UxmlAttribute("show-score-input")]
        public bool ShowScoreInput
        {
            get => m_ShowScoreInput;
            set
            {
                m_ShowScoreInput = value;
                m_ScoreValueField.EnableInClassList("leadr-hidden", !value);
                m_ScoreDisplayLabel.EnableInClassList("leadr-hidden", value);
            }
        }

        /// <summary>
        /// Gets or sets whether to clear the form on successful submission.
        /// </summary>
        [UxmlAttribute("clear-on-success")]
        public bool ClearOnSuccess
        {
            get => m_ClearOnSuccess;
            set => m_ClearOnSuccess = value;
        }

        /// <summary>
        /// Gets the current state of the submitter.
        /// </summary>
        public SubmitterState State
        {
            get => m_State;
            private set
            {
                if (m_State != value)
                {
                    RemoveFromClassList(UssStatePrefix + m_State.ToString().ToLowerInvariant());
                    m_State = value;
                    AddToClassList(UssStatePrefix + value.ToString().ToLowerInvariant());
                    UpdateStateVisibility();
                    StateChanged?.Invoke(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the player name input value.
        /// </summary>
        public string PlayerName
        {
            get => m_PlayerNameField.value;
            set
            {
                m_PlayerNameField.value = value;
                Validate();
            }
        }

        /// <summary>
        /// Gets or sets the score value.
        /// </summary>
        public long ScoreValue
        {
            get => m_ScoreValue;
            set
            {
                m_ScoreValue = value;
                UpdateScoreDisplay();
            }
        }

        /// <summary>
        /// Gets or sets the formatted display value (e.g., "1:23.45").
        /// </summary>
        public string ValueDisplay
        {
            get => m_ValueDisplay;
            set
            {
                m_ValueDisplay = value;
                UpdateScoreDisplay();
            }
        }

        /// <summary>
        /// Gets or sets optional metadata to include with the submission.
        /// </summary>
        public Dictionary<string, object> Metadata
        {
            get => m_Metadata;
            set => m_Metadata = value;
        }

        /// <summary>
        /// Gets whether the current input is valid.
        /// </summary>
        public bool IsValid => m_IsValid;

        /// <summary>
        /// Gets or sets the LeadrClient instance. Uses LeadrClient.Instance if not set.
        /// </summary>
        public LeadrClient Client
        {
            get => m_Client ?? LeadrClient.Instance;
            set => m_Client = value;
        }

        /// <summary>
        /// Creates a new LeadrScoreSubmitter.
        /// </summary>
        public LeadrScoreSubmitter()
        {
            AddToClassList(UssClassName);

            // Form container
            m_Form = new VisualElement { name = "form" };
            m_Form.AddToClassList(UssFormClassName);

            // Score display (for programmatically set scores)
            m_ScoreDisplayLabel = new Label { name = "score-display" };
            m_ScoreDisplayLabel.AddToClassList(UssScoreDisplayClassName);
            m_Form.Add(m_ScoreDisplayLabel);

            // Score value input (optional, for manual entry)
            m_ScoreValueField = new TextField("Score") { name = "score-value-input" };
            m_ScoreValueField.AddToClassList(UssInputClassName);
            m_ScoreValueField.AddToClassList("leadr-hidden");
            m_Form.Add(m_ScoreValueField);

            // Player name input
            m_PlayerNameField = new TextField("Player Name") { name = "player-name-input" };
            m_PlayerNameField.AddToClassList(UssInputClassName);
            m_PlayerNameField.maxLength = m_MaxNameLength;
            m_Form.Add(m_PlayerNameField);

            // Validation message
            m_ValidationLabel = new Label { name = "validation-label" };
            m_ValidationLabel.AddToClassList(UssValidationClassName);
            m_Form.Add(m_ValidationLabel);

            // Submit button
            m_SubmitButton = new Button { text = m_SubmitButtonText, name = "submit-button" };
            m_SubmitButton.AddToClassList(UssButtonClassName);
            m_Form.Add(m_SubmitButton);

            Add(m_Form);

            // Feedback container (success/error messages)
            m_FeedbackContainer = new VisualElement { name = "feedback-container" };
            m_FeedbackContainer.AddToClassList(UssFeedbackClassName);
            m_FeedbackLabel = new Label { name = "feedback-label" };
            m_FeedbackContainer.Add(m_FeedbackLabel);
            Add(m_FeedbackContainer);

            // Register callbacks
            m_PlayerNameField.RegisterValueChangedCallback(_ => Validate());
            m_ScoreValueField.RegisterValueChangedCallback(evt => OnScoreInputChanged(evt.newValue));
            m_SubmitButton.clicked += () => _ = SubmitAsync();

            // Initial state
            State = SubmitterState.Idle;
            Validate();
        }

        private void OnScoreInputChanged(string value)
        {
            if (long.TryParse(value, out var parsed))
            {
                m_ScoreValue = parsed;
            }
            Validate();
        }

        private void Validate()
        {
            var errors = new List<string>();

            // Validate player name
            var name = PlayerName?.Trim() ?? "";
            if (string.IsNullOrEmpty(name))
            {
                errors.Add("Player name is required");
            }
            else if (name.Length < m_MinNameLength)
            {
                errors.Add($"Name must be at least {m_MinNameLength} character(s)");
            }
            else if (name.Length > m_MaxNameLength)
            {
                errors.Add($"Name must be at most {m_MaxNameLength} characters");
            }

            // Validate score (if using input)
            if (m_ShowScoreInput && !string.IsNullOrEmpty(m_ScoreValueField.value))
            {
                if (!long.TryParse(m_ScoreValueField.value, out var score))
                {
                    errors.Add("Score must be a valid number");
                }
                else if (score < 0)
                {
                    errors.Add("Score cannot be negative");
                }
            }

            // Validate board
            if (string.IsNullOrEmpty(m_Board))
            {
                errors.Add("Board not configured");
            }

            m_IsValid = errors.Count == 0;
            m_ValidationLabel.text = errors.Count > 0 ? errors[0] : "";

            m_SubmitButton.SetEnabled(m_IsValid && State != SubmitterState.Submitting);

            ValidationChanged?.Invoke(m_IsValid);
        }

        private void UpdateScoreDisplay()
        {
            if (!string.IsNullOrEmpty(m_ValueDisplay))
            {
                m_ScoreDisplayLabel.text = m_ValueDisplay;
            }
            else
            {
                m_ScoreDisplayLabel.text = m_ScoreValue.ToString("N0");
            }

            if (!m_ShowScoreInput)
            {
                m_ScoreValueField.SetValueWithoutNotify(m_ScoreValue.ToString());
            }
        }

        private void UpdateStateVisibility()
        {
            m_SubmitButton.SetEnabled(m_IsValid && State != SubmitterState.Submitting);
            m_SubmitButton.text = State == SubmitterState.Submitting ? m_SubmittingText : m_SubmitButtonText;

            m_PlayerNameField.SetEnabled(State != SubmitterState.Submitting);
            m_ScoreValueField.SetEnabled(State != SubmitterState.Submitting);

            // Show feedback based on state
            var showFeedback = State == SubmitterState.Success || State == SubmitterState.Error;
            m_FeedbackContainer.style.display = showFeedback ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// Submits the score to the configured board.
        /// </summary>
        /// <returns>A result containing the created score or an error.</returns>
        public async Task<LeadrResult<Score>> SubmitAsync()
        {
            if (State == SubmitterState.Submitting)
            {
                return LeadrResult<Score>.Failure(0, "already_submitting", "A submission is already in progress");
            }

            Validate();
            if (!m_IsValid)
            {
                return LeadrResult<Score>.Failure(0, "validation_error", m_ValidationLabel.text);
            }

            State = SubmitterState.Submitting;

            // Resolve board slug to board_id if not already cached
            if (string.IsNullOrEmpty(m_ResolvedBoardId))
            {
                var boardResult = await Client.GetBoardAsync(m_Board);
                if (!boardResult.IsSuccess)
                {
                    State = SubmitterState.Error;
                    m_FeedbackLabel.text = $"Could not resolve board: {boardResult.Error.Message}";
                    SubmissionFailed?.Invoke(boardResult.Error);
                    return LeadrResult<Score>.Failure(
                        boardResult.Error.StatusCode,
                        boardResult.Error.Code,
                        boardResult.Error.Message);
                }
                m_ResolvedBoardId = boardResult.Data.Id;
            }

            var result = await Client.SubmitScoreAsync(
                m_ResolvedBoardId,
                m_ScoreValue,
                PlayerName.Trim(),
                m_ValueDisplay,
                m_Metadata
            );

            if (result.IsSuccess)
            {
                State = SubmitterState.Success;
                m_FeedbackLabel.text = $"Score submitted! {result.Data.PlayerName}: {FormatScore(result.Data)}";
                ScoreSubmitted?.Invoke(new ScoreSubmittedEventArgs(result.Data, PlayerName, m_ScoreValue));

                if (m_ClearOnSuccess)
                {
                    ClearForm();
                }
            }
            else
            {
                State = SubmitterState.Error;
                m_FeedbackLabel.text = GetUserFriendlyError(result.Error);
                SubmissionFailed?.Invoke(result.Error);
            }

            return result;
        }

        /// <summary>
        /// Resets the form to initial state, clearing all inputs.
        /// </summary>
        public void ClearForm()
        {
            m_PlayerNameField.value = "";
            m_ScoreValueField.value = "";
            m_ScoreValue = 0;
            m_ValueDisplay = null;
            m_Metadata = null;
            m_FeedbackLabel.text = "";
            State = SubmitterState.Idle;
            UpdateScoreDisplay();
            Validate();
        }

        /// <summary>
        /// Resets to idle state (clears feedback but keeps input values).
        /// </summary>
        public void ResetState()
        {
            m_FeedbackLabel.text = "";
            State = SubmitterState.Idle;
        }

        /// <summary>
        /// Sets the score programmatically (for games that track score internally).
        /// </summary>
        /// <param name="value">The score value.</param>
        /// <param name="displayValue">Optional formatted display string.</param>
        public void SetScore(long value, string displayValue = null)
        {
            m_ScoreValue = value;
            m_ValueDisplay = displayValue;
            UpdateScoreDisplay();
            Validate();
        }

        private static string FormatScore(Score score)
        {
            return !string.IsNullOrEmpty(score.ValueDisplay)
                ? score.ValueDisplay
                : score.Value.ToString("N0");
        }

        private static string GetUserFriendlyError(LeadrError error)
        {
            return error.StatusCode switch
            {
                0 => "Network error. Check your connection.",
                404 => "Leaderboard not found. Check configuration.",
                429 => "Too many requests. Please wait and try again.",
                _ => $"Error: {error.Message}"
            };
        }

        /// <summary>Sets custom submit button text.</summary>
        public void SetSubmitButtonText(string text)
        {
            m_SubmitButtonText = text;
            if (State != SubmitterState.Submitting)
            {
                m_SubmitButton.text = text;
            }
        }

        /// <summary>Sets custom submitting text.</summary>
        public void SetSubmittingText(string text) => m_SubmittingText = text;
        /// <summary>Sets the player name field label.</summary>
        public void SetPlayerNameLabel(string label) => m_PlayerNameField.label = label;
        /// <summary>Sets the score field label.</summary>
        public void SetScoreLabel(string label) => m_ScoreValueField.label = label;
    }
}
