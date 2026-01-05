using System;
using Leadr.Models;
using UnityEngine.UIElements;

namespace Leadr.UI
{
    /// <summary>
    /// A custom VisualElement that displays a single score entry.
    /// Can be used standalone or within LeadrBoardView.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <see cref="SetScore"/> to populate the entry with score data and rank.
    /// The component automatically formats the score value using the server-provided
    /// ValueDisplay if available, falling back to numeric formatting.
    /// </para>
    /// <para>
    /// Dates are displayed as relative time (e.g., "5m ago", "2d ago") for recent scores.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var entry = new LeadrScoreEntry();
    /// entry.SetScore(1, myScore);
    /// entry.Clicked += e => Debug.Log($"Clicked: {e.Score.PlayerName}");
    /// </code>
    /// </example>
    [UxmlElement]
    public partial class LeadrScoreEntry : VisualElement
    {
        /// <summary>USS class name for the root element.</summary>
        public const string UssClassName = "leadr-score-entry";
        /// <summary>USS class name for the rank label.</summary>
        public const string UssRankClassName = UssClassName + "__rank";
        /// <summary>USS class name for the player name label.</summary>
        public const string UssPlayerNameClassName = UssClassName + "__player-name";
        /// <summary>USS class name for the score value label.</summary>
        public const string UssValueClassName = UssClassName + "__value";
        /// <summary>USS class name for the date label.</summary>
        public const string UssDateClassName = UssClassName + "__date";
        /// <summary>USS class name applied when entry is selected.</summary>
        public const string UssSelectedClassName = UssClassName + "--selected";
        /// <summary>USS class name applied when entry is highlighted (e.g., current player).</summary>
        public const string UssHighlightedClassName = UssClassName + "--highlighted";

        private readonly Label m_RankLabel;
        private readonly Label m_PlayerNameLabel;
        private readonly Label m_ValueLabel;
        private readonly Label m_DateLabel;

        private Score m_Score;
        private int m_Rank;
        private bool m_ShowDate = true;
        private bool m_ShowRank = true;
        private bool m_IsSelected;
        private bool m_IsHighlighted;

        /// <summary>
        /// Raised when the entry is clicked.
        /// </summary>
        public event Action<LeadrScoreEntry> Clicked;

        /// <summary>
        /// Gets or sets the score data displayed by this entry.
        /// </summary>
        public Score Score
        {
            get => m_Score;
            set
            {
                m_Score = value;
                UpdateDisplay();
            }
        }

        /// <summary>
        /// Gets or sets the rank number displayed (e.g., 1 for "#1").
        /// </summary>
        public int Rank
        {
            get => m_Rank;
            set
            {
                m_Rank = value;
                UpdateRankDisplay();
            }
        }

        /// <summary>
        /// Gets or sets whether to show the date column.
        /// </summary>
        [UxmlAttribute("show-date")]
        public bool ShowDate
        {
            get => m_ShowDate;
            set
            {
                m_ShowDate = value;
                m_DateLabel.EnableInClassList("leadr-hidden", !value);
            }
        }

        /// <summary>
        /// Gets or sets whether to show the rank column.
        /// </summary>
        [UxmlAttribute("show-rank")]
        public bool ShowRank
        {
            get => m_ShowRank;
            set
            {
                m_ShowRank = value;
                m_RankLabel.EnableInClassList("leadr-hidden", !value);
            }
        }

        /// <summary>
        /// Gets or sets whether this entry is selected.
        /// </summary>
        public bool IsSelected
        {
            get => m_IsSelected;
            set
            {
                m_IsSelected = value;
                EnableInClassList(UssSelectedClassName, value);
            }
        }

        /// <summary>
        /// Gets or sets whether this entry is highlighted (e.g., current player's score).
        /// </summary>
        public bool IsHighlighted
        {
            get => m_IsHighlighted;
            set
            {
                m_IsHighlighted = value;
                EnableInClassList(UssHighlightedClassName, value);
            }
        }

        /// <summary>
        /// Creates a new LeadrScoreEntry.
        /// </summary>
        public LeadrScoreEntry()
        {
            AddToClassList(UssClassName);

            m_RankLabel = new Label { name = "rank" };
            m_RankLabel.AddToClassList(UssRankClassName);

            m_PlayerNameLabel = new Label { name = "player-name" };
            m_PlayerNameLabel.AddToClassList(UssPlayerNameClassName);

            m_ValueLabel = new Label { name = "value" };
            m_ValueLabel.AddToClassList(UssValueClassName);

            m_DateLabel = new Label { name = "date" };
            m_DateLabel.AddToClassList(UssDateClassName);

            Add(m_RankLabel);
            Add(m_PlayerNameLabel);
            Add(m_ValueLabel);
            Add(m_DateLabel);

            RegisterCallback<ClickEvent>(OnClick);
        }

        /// <summary>
        /// Populates the entry with score data and rank.
        /// </summary>
        /// <param name="rank">The rank position (1-based).</param>
        /// <param name="score">The score data to display.</param>
        public void SetScore(int rank, Score score)
        {
            m_Rank = rank;
            m_Score = score;
            UpdateDisplay();
        }

        /// <summary>
        /// Clears all displayed data.
        /// </summary>
        public void ClearData()
        {
            m_Score = null;
            m_Rank = 0;
            m_RankLabel.text = "";
            m_PlayerNameLabel.text = "";
            m_ValueLabel.text = "";
            m_DateLabel.text = "";
        }

        private void UpdateDisplay()
        {
            UpdateRankDisplay();

            if (m_Score == null)
            {
                m_PlayerNameLabel.text = "";
                m_ValueLabel.text = "";
                m_DateLabel.text = "";
                return;
            }

            m_PlayerNameLabel.text = m_Score.PlayerName ?? "";
            m_ValueLabel.text = FormatValue(m_Score);
            m_DateLabel.text = FormatDate(m_Score.CreatedAt);
        }

        private void UpdateRankDisplay()
        {
            m_RankLabel.text = m_Rank > 0 ? $"#{m_Rank}" : "";
        }

        private static string FormatValue(Score score)
        {
            if (!string.IsNullOrEmpty(score.ValueDisplay))
            {
                return score.ValueDisplay;
            }

            return score.Value >= 1000
                ? score.Value.ToString("N0")
                : score.Value.ToString("F0");
        }

        private static string FormatDate(DateTime date)
        {
            var diff = DateTime.UtcNow - date;

            if (diff.TotalSeconds < 60)
            {
                return "Just now";
            }
            if (diff.TotalMinutes < 60)
            {
                return $"{(int)diff.TotalMinutes}m ago";
            }
            if (diff.TotalHours < 24)
            {
                return $"{(int)diff.TotalHours}h ago";
            }
            if (diff.TotalDays < 7)
            {
                return $"{(int)diff.TotalDays}d ago";
            }

            return date.ToString("MMM d");
        }

        private void OnClick(ClickEvent evt)
        {
            Clicked?.Invoke(this);
        }
    }
}
