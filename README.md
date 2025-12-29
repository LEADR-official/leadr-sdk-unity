# leadr-sdk-unity

The official LEADR Unity SDK.

Add beautiful cross-platform leaderboards to your game in minutes.

## Getting started

1. Download the LEADR CLI: 
2. Run `leadr register` and follow the instructions to create an account, your game and leaderboards
3. Install the LEADR Unity SDK
4. Add your game id to the LeadrSettings config
5. Check out the [Basic Integration]() sample

## Usage example

```csharp
// Initialize
LeadrClient.Instance.Initialize("gam_your_game_id");

// Get scores
var result = await LeadrClient.Instance.GetScoresAsync("brd_board_id");
if (result.IsSuccess)
{
    foreach (var score in result.Data.Items)
        Debug.Log($"{score.PlayerName}: {score.Value}");
}

// Submit score
var submitResult = await LeadrClient.Instance.SubmitScoreAsync(
    "brd_board_id",
    1000,
    "PlayerOne");
```
