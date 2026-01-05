using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Leadr.Models;
using Leadr.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Leadr.Samples.LeadrUIComponents
{
    /// <summary>
    /// Sample controller showcasing the LEADR UI Toolkit components.
    /// Demonstrates programmatic setup and event handling.
    /// </summary>
    public class UIToolkitController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private LeadrSettings settings;
        [SerializeField] private string board = "";

        [Header("UI Document")]
        [SerializeField] private UIDocument uiDocument;

        [Header("Styles")]
        [SerializeField] private StyleSheet styles;

        private LeadrBoardView m_BoardView;
        private LeadrScoreSubmitter m_ScoreSubmitter;
        private Button m_SimulateScoreButton;
        private Label m_StatusLabel;
        private DropdownField m_BoardDropdown;
        private List<Board> m_Boards = new List<Board>();

        private void Awake()
        {
            if (uiDocument == null)
            {
                Debug.LogError("[UIToolkit] UIDocument not assigned!");
                return;
            }
        }

        private async void Start()
        {
            // Initialize LEADR SDK
            if (settings != null)
            {
                LeadrClient.Instance.Initialize(settings);
            }
            else
            {
                Debug.LogWarning("[UIToolkit] LeadrSettings not assigned. " +
                    "Assuming SDK is already initialized.");
            }

            var root = uiDocument.rootVisualElement;

            // Apply styles
            if (styles != null)
            {
                root.styleSheets.Add(styles);
            }

            // Build UI
            BuildUI(root);

            // Load boards and populate dropdown
            await LoadBoardsAsync();
        }

        private void BuildUI(VisualElement root)
        {
            root.Clear();
            root.style.backgroundColor = new Color(0.95f, 0.95f, 0.95f);

            // Main container
            var container = new VisualElement
            {
                name = "main-container",
                style =
                {
                    flexGrow = 1,
                    paddingLeft = 16,
                    paddingRight = 16,
                    paddingTop = 16,
                    paddingBottom = 16
                }
            };

            // Header with controls
            var header = new VisualElement
            {
                name = "header",
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween,
                    marginBottom = 16
                }
            };

            var title = new Label
            {
                text = "LEADR UI Toolkit",
                style =
                {
                    fontSize = 24,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            header.Add(title);

            // Header controls container (right side)
            var headerControls = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            // Board dropdown
            m_BoardDropdown = new DropdownField
            {
                label = "Board",
                style = { marginRight = 8, minWidth = 150 }
            };
            m_BoardDropdown.RegisterValueChangedCallback(evt =>
            {
                _ = OnBoardSelected(m_BoardDropdown.index);
            });
            headerControls.Add(m_BoardDropdown);

            m_SimulateScoreButton = new Button(SimulateScore)
            {
                text = "Simulate Score"
            };
            headerControls.Add(m_SimulateScoreButton);

            header.Add(headerControls);
            container.Add(header);

            // Status label
            m_StatusLabel = new Label
            {
                name = "status",
                style =
                {
                    marginBottom = 16,
                    color = new Color(0.5f, 0.5f, 0.5f)
                }
            };
            container.Add(m_StatusLabel);

            // Content area (side by side on wide screens)
            var content = new VisualElement
            {
                name = "content",
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Row
                }
            };

            // Leaderboard panel
            var leaderboardPanel = new VisualElement
            {
                style =
                {
                    flexGrow = 2,
                    marginRight = 16,
                    minWidth = 300
                }
            };

            m_BoardView = new LeadrBoardView
            {
                Board = board,
                ScoresPerPage = 10
            };
            m_BoardView.style.flexGrow = 1;

            // Subscribe to events
            m_BoardView.ScoreSelected += OnScoreSelected;
            m_BoardView.StateChanged += OnLeaderboardStateChanged;
            m_BoardView.ErrorOccurred += OnError;
            m_BoardView.PageLoaded += () => SetStatus("Leaderboard loaded");

            leaderboardPanel.Add(m_BoardView);
            content.Add(leaderboardPanel);

            // Submission panel
            var submissionPanel = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    minWidth = 250
                }
            };

            var submissionTitle = new Label
            {
                text = "Submit Score",
                style =
                {
                    fontSize = 18,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 8
                }
            };
            submissionPanel.Add(submissionTitle);

            m_ScoreSubmitter = new LeadrScoreSubmitter
            {
                Board = board,
                ShowScoreInput = false,
                ClearOnSuccess = false
            };
            m_ScoreSubmitter.SetScore(0, "0 pts");

            // Subscribe to events
            m_ScoreSubmitter.ScoreSubmitted += OnScoreSubmitted;
            m_ScoreSubmitter.SubmissionFailed += OnError;
            m_ScoreSubmitter.StateChanged += OnSubmitterStateChanged;

            submissionPanel.Add(m_ScoreSubmitter);
            content.Add(submissionPanel);

            container.Add(content);
            root.Add(container);
        }

        private void SimulateScore()
        {
            var score = Random.Range(100, 10000);
            m_ScoreSubmitter.SetScore(score, $"{score:N0} pts");
            SetStatus($"Simulated score: {score:N0}");
        }

        private async Task LoadBoardsAsync()
        {
            SetStatus("Loading boards...");

            var result = await LeadrClient.Instance.GetBoardsAsync();
            if (!result.IsSuccess)
            {
                SetStatus($"Error loading boards: {result.Error.Message}", true);
                return;
            }

            m_Boards = result.Data.Items;

            // Populate dropdown
            m_BoardDropdown.choices = m_Boards.Select(b => b.Name).ToList();

            if (m_Boards.Count > 0)
            {
                // Select first board (or match the inspector-configured slug)
                int defaultIndex = 0;
                if (!string.IsNullOrEmpty(board))
                {
                    var idx = m_Boards.FindIndex(b => b.Slug == board);
                    if (idx >= 0) defaultIndex = idx;
                }
                m_BoardDropdown.index = defaultIndex;
                await OnBoardSelected(defaultIndex);
            }
            else
            {
                SetStatus("No boards found for this game.");
            }
        }

        private async Task OnBoardSelected(int index)
        {
            if (index < 0 || index >= m_Boards.Count) return;

            var selectedBoard = m_Boards[index];

            // Update both components to use the selected board's slug
            m_BoardView.Board = selectedBoard.Slug;
            m_ScoreSubmitter.Board = selectedBoard.Slug;

            // Load the leaderboard
            await m_BoardView.LoadAsync();

            SetStatus($"Switched to {selectedBoard.Name}");
        }

        private void OnScoreSelected(ScoreSelectedEventArgs args)
        {
            SetStatus($"Selected: #{args.Rank} {args.Score.PlayerName} - {args.Score.ValueDisplay ?? args.Score.Value.ToString("N0")}");
        }

        private void OnLeaderboardStateChanged(LeaderboardState state)
        {
            Debug.Log($"[UIToolkit] Leaderboard state: {state}");
        }

        private void OnSubmitterStateChanged(SubmitterState state)
        {
            Debug.Log($"[UIToolkit] Submitter state: {state}");
        }

        private async void OnScoreSubmitted(ScoreSubmittedEventArgs args)
        {
            SetStatus($"Score submitted! ID: {args.Score.Id}");

            // Refresh leaderboard to show new score
            await m_BoardView.RefreshAsync();

            // Highlight the submitted score
            m_BoardView.HighlightScore(args.Score.Id);
        }

        private void OnError(LeadrError error)
        {
            SetStatus($"Error: {error.Message}", true);
            Debug.LogError($"[UIToolkit] {error}");
        }

        private void SetStatus(string message, bool isError = false)
        {
            m_StatusLabel.text = message;
            m_StatusLabel.style.color = isError
                ? new Color(0.9f, 0.3f, 0.3f)
                : new Color(0.5f, 0.5f, 0.5f);
        }

        private void OnDestroy()
        {
            if (m_BoardView != null)
            {
                m_BoardView.ScoreSelected -= OnScoreSelected;
                m_BoardView.StateChanged -= OnLeaderboardStateChanged;
                m_BoardView.ErrorOccurred -= OnError;
            }

            if (m_ScoreSubmitter != null)
            {
                m_ScoreSubmitter.ScoreSubmitted -= OnScoreSubmitted;
                m_ScoreSubmitter.SubmissionFailed -= OnError;
                m_ScoreSubmitter.StateChanged -= OnSubmitterStateChanged;
            }
        }
    }
}
