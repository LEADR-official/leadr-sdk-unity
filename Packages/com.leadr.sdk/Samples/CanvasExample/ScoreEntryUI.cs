using Leadr.Models;
using TMPro;
using UnityEngine;

namespace Leadr.Samples.CanvasExample
{
    /// <summary>
    /// UI component for displaying a single score entry.
    /// Attach this to your score entry prefab.
    /// </summary>
    public class ScoreEntryUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text scoreValueText;
        [SerializeField] private TMP_Text dateText;

        /// <summary>
        /// Populate the UI with score data.
        /// </summary>
        /// <param name="rank">Display rank (1-based)</param>
        /// <param name="score">Score data from LEADR API</param>
        public void SetScore(int rank, Score score)
        {
            if (rankText != null)
            {
                rankText.text = $"#{rank}";
            }

            if (playerNameText != null)
            {
                playerNameText.text = score.PlayerName;
            }

            if (scoreValueText != null)
            {
                // ValueDisplay is server-formatted (e.g., "1:23.45" for time-based scores)
                // Falls back to raw numeric value if not set
                scoreValueText.text = !string.IsNullOrEmpty(score.ValueDisplay)
                    ? score.ValueDisplay
                    : FormatValue(score.Value);
            }

            if (dateText != null)
            {
                // Show relative or formatted date
                dateText.text = FormatDate(score.CreatedAt);
            }
        }

        private string FormatValue(double value)
        {
            // Format large numbers with commas
            if (value >= 1000)
            {
                return value.ToString("N0");
            }
            return value.ToString("F0");
        }

        private string FormatDate(System.DateTime date)
        {
            var now = System.DateTime.UtcNow;
            var diff = now - date;

            if (diff.TotalMinutes < 1)
                return "Just now";
            if (diff.TotalHours < 1)
                return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalDays < 1)
                return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays}d ago";

            return date.ToString("MMM d");
        }
    }
}
