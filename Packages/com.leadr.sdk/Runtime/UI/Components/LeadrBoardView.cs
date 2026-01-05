using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Leadr.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace Leadr.UI
{
    /// <summary>
    /// State enum for the leaderboard view.
    /// </summary>
    public enum LeaderboardState
    {
        /// <summary>Initial state before loading.</summary>
        Idle,
        /// <summary>Currently fetching data.</summary>
        Loading,
        /// <summary>Data loaded and displayed.</summary>
        Loaded,
        /// <summary>No scores to display.</summary>
        Empty,
        /// <summary>An error occurred.</summary>
        Error
    }

    /// <summary>
    /// Event args for score selection events.
    /// </summary>
    public class ScoreSelectedEventArgs
    {
        /// <summary>Gets the selected score.</summary>
        public Score Score { get; }
        /// <summary>Gets the rank of the selected score.</summary>
        public int Rank { get; }
        /// <summary>Gets the UI entry that was selected.</summary>
        public LeadrScoreEntry Entry { get; }

        /// <summary>
        /// Creates new score selected event args.
        /// </summary>
        public ScoreSelectedEventArgs(Score score, int rank, LeadrScoreEntry entry)
        {
            Score = score;
            Rank = rank;
            Entry = entry;
        }
    }

    /// <summary>
    /// A custom VisualElement that displays a paginated leaderboard.
    /// Integrates with LeadrClient for data fetching.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Set <see cref="BoardId"/> and call <see cref="LoadAsync"/> to fetch and display scores.
    /// Alternatively, set <see cref="AutoLoad"/> to true to load automatically when BoardId changes.
    /// </para>
    /// <para>
    /// The component handles loading states, errors, empty states, and pagination automatically.
    /// Subscribe to events like <see cref="ScoreSelected"/> and <see cref="ErrorOccurred"/> for notifications.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var board = new LeadrBoardView
    /// {
    ///     BoardId = "brd_abc123",
    ///     ScoresPerPage = 10,
    ///     Title = "High Scores"
    /// };
    /// board.ScoreSelected += args => Debug.Log($"Selected: {args.Score.PlayerName}");
    /// await board.LoadAsync();
    /// </code>
    /// </example>
    [UxmlElement]
    public partial class LeadrBoardView : VisualElement
    {
        /// <summary>USS class name for the root element.</summary>
        public const string UssClassName = "leadr-board-view";
        /// <summary>USS class name for the header section.</summary>
        public const string UssHeaderClassName = UssClassName + "__header";
        /// <summary>USS class name for the title label.</summary>
        public const string UssTitleClassName = UssClassName + "__title";
        /// <summary>USS class name for the score list container.</summary>
        public const string UssListClassName = UssClassName + "__list";
        /// <summary>USS class name for the footer section.</summary>
        public const string UssFooterClassName = UssClassName + "__footer";
        /// <summary>USS class name for the loading overlay.</summary>
        public const string UssLoadingClassName = UssClassName + "__loading";
        /// <summary>USS class name for the error container.</summary>
        public const string UssErrorClassName = UssClassName + "__error";
        /// <summary>USS class name for the empty state container.</summary>
        public const string UssEmptyClassName = UssClassName + "__empty";
        /// <summary>USS class name for the pagination container.</summary>
        public const string UssPaginationClassName = UssClassName + "__pagination";
        /// <summary>USS class name prefix for state modifiers.</summary>
        public const string UssStatePrefix = UssClassName + "--state-";

        // Child elements
        private readonly VisualElement m_Header;
        private readonly Label m_TitleLabel;
        private readonly ScrollView m_ScrollView;
        private readonly VisualElement m_ListContainer;
        private readonly VisualElement m_Footer;
        private readonly VisualElement m_LoadingOverlay;
        private readonly Label m_LoadingLabel;
        private readonly VisualElement m_ErrorContainer;
        private readonly Label m_ErrorLabel;
        private readonly Button m_RetryButton;
        private readonly VisualElement m_EmptyContainer;
        private readonly Label m_EmptyLabel;
        private readonly VisualElement m_PaginationContainer;
        private readonly Button m_PrevButton;
        private readonly Button m_NextButton;
        private readonly Label m_PageInfoLabel;

        // State
        private LeaderboardState m_State = LeaderboardState.Idle;
        private PagedResult<Score> m_CurrentPage;
        private readonly List<LeadrScoreEntry> m_ScoreEntries = new();
        private LeadrScoreEntry m_SelectedEntry;
        private int m_CurrentPageNumber = 1;

        // Configuration
        private string m_BoardId = "";
        private int m_ScoresPerPage = 10;
        private bool m_AutoLoad;
        private bool m_ShowPagination = true;
        private string m_Title = "";
        private bool m_TitleOverridden;
        private string m_SortOverride;

        // Client reference
        private LeadrClient m_Client;

        /// <summary>Raised when a score entry is selected.</summary>
        public event Action<ScoreSelectedEventArgs> ScoreSelected;
        /// <summary>Raised when the state changes.</summary>
        public event Action<LeaderboardState> StateChanged;
        /// <summary>Raised when an error occurs.</summary>
        public event Action<LeadrError> ErrorOccurred;
        /// <summary>Raised when a page finishes loading.</summary>
        public event Action PageLoaded;

        /// <summary>
        /// Gets or sets the board ID to display scores from.
        /// </summary>
        [UxmlAttribute("board-id")]
        public string BoardId
        {
            get => m_BoardId;
            set
            {
                if (m_BoardId != value)
                {
                    m_BoardId = value;
                    if (m_AutoLoad && !string.IsNullOrEmpty(value))
                    {
                        _ = LoadAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the number of scores per page (1-100).
        /// </summary>
        [UxmlAttribute("scores-per-page")]
        public int ScoresPerPage
        {
            get => m_ScoresPerPage;
            set => m_ScoresPerPage = Mathf.Clamp(value, 1, 100);
        }

        /// <summary>
        /// Gets or sets whether to auto-load when BoardId changes.
        /// </summary>
        [UxmlAttribute("auto-load")]
        public bool AutoLoad
        {
            get => m_AutoLoad;
            set => m_AutoLoad = value;
        }

        /// <summary>
        /// Gets or sets whether to show pagination controls.
        /// </summary>
        [UxmlAttribute("show-pagination")]
        public bool ShowPagination
        {
            get => m_ShowPagination;
            set
            {
                m_ShowPagination = value;
                m_PaginationContainer.EnableInClassList("leadr-hidden", !value);
            }
        }

        /// <summary>
        /// Gets or sets the title displayed in the header.
        /// If not set, defaults to the board's name from the API.
        /// </summary>
        [UxmlAttribute("title")]
        public string Title
        {
            get => m_Title;
            set
            {
                m_Title = value;
                m_TitleLabel.text = value;
                m_TitleOverridden = !string.IsNullOrEmpty(value);
            }
        }

        /// <summary>
        /// Gets or sets an optional sort override (e.g., "value:desc,created_at:desc").
        /// </summary>
        public string SortOverride
        {
            get => m_SortOverride;
            set => m_SortOverride = value;
        }

        /// <summary>
        /// Gets the current state of the leaderboard view.
        /// </summary>
        public LeaderboardState State
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

        /// <summary>Gets the current page of results.</summary>
        public PagedResult<Score> CurrentPage => m_CurrentPage;
        /// <summary>Gets the current page number (1-based).</summary>
        public int CurrentPageNumber => m_CurrentPageNumber;
        /// <summary>Gets whether there is a next page.</summary>
        public bool HasNextPage => m_CurrentPage?.HasNext ?? false;
        /// <summary>Gets whether there is a previous page.</summary>
        public bool HasPrevPage => m_CurrentPage?.HasPrev ?? false;
        /// <summary>Gets the scores on the current page.</summary>
        public IReadOnlyList<Score> Scores => m_CurrentPage?.Items;

        /// <summary>
        /// Gets or sets the LeadrClient instance. Uses LeadrClient.Instance if not set.
        /// </summary>
        public LeadrClient Client
        {
            get => m_Client ?? LeadrClient.Instance;
            set => m_Client = value;
        }

        /// <summary>
        /// Creates a new LeadrBoardView.
        /// </summary>
        public LeadrBoardView()
        {
            AddToClassList(UssClassName);

            // Header
            m_Header = new VisualElement { name = "header" };
            m_Header.AddToClassList(UssHeaderClassName);

            m_TitleLabel = new Label { name = "title", text = "" };
            m_TitleLabel.AddToClassList(UssTitleClassName);
            m_Header.Add(m_TitleLabel);

            Add(m_Header);

            // Main list area with scroll
            m_ScrollView = new ScrollView(ScrollViewMode.Vertical) { name = "scroll-view" };
            m_ScrollView.AddToClassList(UssListClassName);

            m_ListContainer = new VisualElement { name = "list-container" };
            m_ScrollView.Add(m_ListContainer);

            Add(m_ScrollView);

            // Loading overlay
            m_LoadingOverlay = new VisualElement { name = "loading-overlay" };
            m_LoadingOverlay.AddToClassList(UssLoadingClassName);
            m_LoadingLabel = new Label { text = "Loading..." };
            m_LoadingOverlay.Add(m_LoadingLabel);
            Add(m_LoadingOverlay);

            // Error container
            m_ErrorContainer = new VisualElement { name = "error-container" };
            m_ErrorContainer.AddToClassList(UssErrorClassName);
            m_ErrorLabel = new Label { name = "error-message" };
            m_RetryButton = new Button { text = "Retry", name = "retry-button" };
            m_ErrorContainer.Add(m_ErrorLabel);
            m_ErrorContainer.Add(m_RetryButton);
            Add(m_ErrorContainer);

            // Empty state container
            m_EmptyContainer = new VisualElement { name = "empty-container" };
            m_EmptyContainer.AddToClassList(UssEmptyClassName);
            m_EmptyLabel = new Label { text = "No scores yet. Be the first!", name = "empty-message" };
            m_EmptyContainer.Add(m_EmptyLabel);
            Add(m_EmptyContainer);

            // Footer with pagination
            m_Footer = new VisualElement { name = "footer" };
            m_Footer.AddToClassList(UssFooterClassName);

            m_PaginationContainer = new VisualElement { name = "pagination" };
            m_PaginationContainer.AddToClassList(UssPaginationClassName);

            m_PrevButton = new Button { text = "Previous", name = "prev-button" };
            m_PageInfoLabel = new Label { name = "page-info" };
            m_PageInfoLabel.AddToClassList(UssClassName + "__page-info");
            m_NextButton = new Button { text = "Next", name = "next-button" };

            m_PaginationContainer.Add(m_PrevButton);
            m_PaginationContainer.Add(m_PageInfoLabel);
            m_PaginationContainer.Add(m_NextButton);

            m_Footer.Add(m_PaginationContainer);
            Add(m_Footer);

            // Register callbacks
            m_PrevButton.clicked += () => _ = LoadPrevPageAsync();
            m_NextButton.clicked += () => _ = LoadNextPageAsync();
            m_RetryButton.clicked += () => _ = LoadAsync();

            // Initial state
            State = LeaderboardState.Idle;
        }

        private void UpdateStateVisibility()
        {
            var isLoaded = State == LeaderboardState.Loaded || State == LeaderboardState.Idle;
            m_ScrollView.style.display = isLoaded ? DisplayStyle.Flex : DisplayStyle.None;
            m_LoadingOverlay.style.display = State == LeaderboardState.Loading ? DisplayStyle.Flex : DisplayStyle.None;
            m_ErrorContainer.style.display = State == LeaderboardState.Error ? DisplayStyle.Flex : DisplayStyle.None;
            m_EmptyContainer.style.display = State == LeaderboardState.Empty ? DisplayStyle.Flex : DisplayStyle.None;

            UpdatePaginationState();
        }

        private void UpdatePaginationState()
        {
            m_PrevButton.SetEnabled(HasPrevPage && State != LeaderboardState.Loading);
            m_NextButton.SetEnabled(HasNextPage && State != LeaderboardState.Loading);

            if (m_CurrentPage != null)
            {
                m_PageInfoLabel.text = $"Page {m_CurrentPageNumber}";
            }
            else
            {
                m_PageInfoLabel.text = "";
            }
        }

        /// <summary>
        /// Loads (or reloads) the first page of scores.
        /// </summary>
        /// <returns>A task that completes when loading finishes.</returns>
        public async Task LoadAsync()
        {
            if (string.IsNullOrEmpty(m_BoardId))
            {
                SetError("Board ID not configured");
                return;
            }

            State = LeaderboardState.Loading;
            m_CurrentPageNumber = 1;

            // Fetch board name for title if not explicitly set
            if (!m_TitleOverridden)
            {
                var boardResult = await Client.GetBoardAsync(m_BoardId);
                if (boardResult.IsSuccess)
                {
                    m_TitleLabel.text = boardResult.Data.Name;
                }
            }

            var result = await Client.GetScoresAsync(m_BoardId, m_ScoresPerPage, m_SortOverride);

            if (result.IsSuccess)
            {
                m_CurrentPage = result.Data;
                DisplayScores();
                State = m_CurrentPage.Items.Count > 0 ? LeaderboardState.Loaded : LeaderboardState.Empty;
                PageLoaded?.Invoke();
            }
            else
            {
                SetError(result.Error.Message);
                ErrorOccurred?.Invoke(result.Error);
            }
        }

        /// <summary>
        /// Loads the next page of scores.
        /// </summary>
        /// <returns>A task that completes when loading finishes.</returns>
        public async Task LoadNextPageAsync()
        {
            if (m_CurrentPage == null || !m_CurrentPage.HasNext)
            {
                return;
            }

            State = LeaderboardState.Loading;

            var result = await m_CurrentPage.NextPageAsync();

            if (result.IsSuccess)
            {
                m_CurrentPage = result.Data;
                m_CurrentPageNumber++;
                DisplayScores();
                State = LeaderboardState.Loaded;
                PageLoaded?.Invoke();
            }
            else
            {
                State = LeaderboardState.Loaded;
                ErrorOccurred?.Invoke(result.Error);
            }
        }

        /// <summary>
        /// Loads the previous page of scores.
        /// </summary>
        /// <returns>A task that completes when loading finishes.</returns>
        public async Task LoadPrevPageAsync()
        {
            if (m_CurrentPage == null || !m_CurrentPage.HasPrev)
            {
                return;
            }

            State = LeaderboardState.Loading;

            var result = await m_CurrentPage.PrevPageAsync();

            if (result.IsSuccess)
            {
                m_CurrentPage = result.Data;
                m_CurrentPageNumber = Math.Max(1, m_CurrentPageNumber - 1);
                DisplayScores();
                State = LeaderboardState.Loaded;
                PageLoaded?.Invoke();
            }
            else
            {
                State = LeaderboardState.Loaded;
                ErrorOccurred?.Invoke(result.Error);
            }
        }

        /// <summary>
        /// Refreshes the current page.
        /// </summary>
        /// <returns>A task that completes when loading finishes.</returns>
        public Task RefreshAsync() => LoadAsync();

        private void DisplayScores()
        {
            ClearScoreEntries();

            if (m_CurrentPage == null)
            {
                return;
            }

            int baseRank = (m_CurrentPageNumber - 1) * m_ScoresPerPage + 1;

            foreach (var score in m_CurrentPage.Items)
            {
                var entry = new LeadrScoreEntry();
                entry.SetScore(baseRank, score);
                entry.Clicked += OnScoreEntryClicked;

                m_ListContainer.Add(entry);
                m_ScoreEntries.Add(entry);

                baseRank++;
            }

            m_ScrollView.scrollOffset = Vector2.zero;
            UpdatePaginationState();
        }

        private void ClearScoreEntries()
        {
            foreach (var entry in m_ScoreEntries)
            {
                entry.Clicked -= OnScoreEntryClicked;
            }
            m_ScoreEntries.Clear();
            m_ListContainer.Clear();
            m_SelectedEntry = null;
        }

        private void OnScoreEntryClicked(LeadrScoreEntry entry)
        {
            if (m_SelectedEntry != null)
            {
                m_SelectedEntry.IsSelected = false;
            }

            entry.IsSelected = true;
            m_SelectedEntry = entry;

            ScoreSelected?.Invoke(new ScoreSelectedEventArgs(entry.Score, entry.Rank, entry));
        }

        private void SetError(string message)
        {
            m_ErrorLabel.text = message;
            State = LeaderboardState.Error;
        }

        /// <summary>Sets custom loading text.</summary>
        public void SetLoadingText(string text) => m_LoadingLabel.text = text;
        /// <summary>Sets custom empty state text.</summary>
        public void SetEmptyText(string text) => m_EmptyLabel.text = text;
        /// <summary>Sets custom previous button text.</summary>
        public void SetPrevButtonText(string text) => m_PrevButton.text = text;
        /// <summary>Sets custom next button text.</summary>
        public void SetNextButtonText(string text) => m_NextButton.text = text;

        /// <summary>
        /// Highlights a specific score entry (e.g., the current player's score).
        /// </summary>
        /// <param name="scoreId">The score ID to highlight.</param>
        public void HighlightScore(string scoreId)
        {
            foreach (var entry in m_ScoreEntries)
            {
                entry.IsHighlighted = entry.Score?.Id == scoreId;
            }
        }

        /// <summary>
        /// Clears all highlights.
        /// </summary>
        public void ClearHighlights()
        {
            foreach (var entry in m_ScoreEntries)
            {
                entry.IsHighlighted = false;
            }
        }
    }
}
