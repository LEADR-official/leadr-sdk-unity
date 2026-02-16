using Leadr;
using UnityEngine;

namespace Leadr.Samples.BasicIntegration
{
    /// <summary>
    /// Demonstrates basic LEADR SDK integration.
    ///
    /// This sample shows how to:
    /// - Initialize the LeadrClient
    /// - Fetch a board by slug
    /// - Fetch scores for that board
    /// - Fetch the current player's own scores
    /// </summary>
    public class BasicIntegrationDemo : MonoBehaviour
    {
        [Header("LEADR Configuration")]
        [SerializeField] private LeadrSettings settings;

        [Header("Board")]
        [SerializeField] private string board;

        private async void Start()
        {
            // Initialize the LEADR client
            if (settings != null)
            {
                LeadrClient.Instance.Initialize(settings);
            }
            else
            {
                Debug.LogError("[BasicDemo] LeadrSettings not assigned!");
                return;
            }

            if (string.IsNullOrEmpty(board))
            {
                Debug.LogError("[BasicDemo] Board slug not configured!");
                return;
            }

            // Fetch board by slug
            Debug.Log($"[BasicDemo] Fetching board '{board}'...");
            var boardResult = await LeadrClient.Instance.GetBoardAsync(board);

            if (!boardResult.IsSuccess)
            {
                Debug.LogError($"[BasicDemo] Failed to fetch board: {boardResult.Error}");
                return;
            }

            var fetchedBoard = boardResult.Data;
            Debug.Log($"[BasicDemo] Board: {fetchedBoard.Name} ({fetchedBoard.ShortCode})");

            // Fetch scores for the board
            Debug.Log("[BasicDemo] Fetching scores...");
            var scoresResult = await LeadrClient.Instance.GetScoresAsync(fetchedBoard.Id, limit: 10);

            if (!scoresResult.IsSuccess)
            {
                Debug.LogError($"[BasicDemo] Failed to fetch scores: {scoresResult.Error}");
                return;
            }

            var scores = scoresResult.Data;
            Debug.Log($"[BasicDemo] Found {scores.Items.Count} scores:");

            int rank = 1;
            foreach (var score in scores.Items)
            {
                var display = !string.IsNullOrEmpty(score.ValueDisplay)
                    ? score.ValueDisplay
                    : score.Value.ToString("N0");
                Debug.Log($"  {rank}. {score.PlayerName}: {display}");
                rank++;
            }

            // Fetch the current player's own scores
            Debug.Log("[BasicDemo] Fetching my scores...");
            var myScoresResult = await LeadrClient.Instance.GetMyScoresAsync(fetchedBoard.Id);

            if (myScoresResult.IsSuccess)
            {
                var myScores = myScoresResult.Data;
                Debug.Log($"[BasicDemo] Found {myScores.Items.Count} of my scores on this board:");

                foreach (var myScore in myScores.Items)
                {
                    var display = !string.IsNullOrEmpty(myScore.ValueDisplay)
                        ? myScore.ValueDisplay
                        : myScore.Value.ToString("N0");
                    Debug.Log($"  - {display} (submitted {myScore.CreatedAt:g})");
                }
            }
            else
            {
                Debug.Log($"[BasicDemo] Could not fetch my scores: {myScoresResult.Error}");
            }
        }
    }
}
