# leadr-sdk-unity

The official LEADR Unity SDK.

Add beautiful cross-platform leaderboards to your game in minutes.

## Getting started

1. Download the LEADR CLI:
2. Run `leadr register` and follow the instructions to create an account, your game and leaderboards
3. Install the LEADR Unity SDK
4. Add your game id to the LeadrSettings config
5. Check out the [Basic Integration](/Packages/com.leadr.sdk/Samples/BasicIntegration) sample

For full documentation, visit [docs.leadr.gg/latest/sdks/unity](https://docs.leadr.gg/latest/sdks/unity).

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

## UI Toolkit Components

The SDK includes ready-to-use UI Toolkit components for Unity 2021.2+:

- **LeadrBoardView** - Paginated leaderboard display with loading/error/empty states
- **LeadrScoreSubmitter** - Score submission form with validation
- **LeadrScoreEntry** - Individual score row component

```csharp
// Programmatic usage
var board = new LeadrBoardView {
    Board = "weekly",  // Use board slug
    ScoresPerPage = 10,
    Title = "High Scores"
};
board.ScoreSelected += args => Debug.Log($"Selected: {args.Score.PlayerName}");
await board.LoadAsync();
```

```xml
<!-- Or declarative in UXML -->
<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:leadr="Leadr.UI">
    <leadr:LeadrBoardView board="weekly" auto-load="true" />
</ui:UXML>
```

Styles are provided via `LeadrCommon.uss`. You can customize colors by overriding the CSS variables defined in that file.

## Samples

Import samples via **Window > Package Manager > LEADR Unity SDK > Samples**.

| Sample | Description |
|--------|-------------|
| **Basic Integration** | Minimal example showing SDK initialization and fetching boards |
| **Canvas Example** | Complete uGUI example with board selector, score list, pagination, and submission |
| **LeadrUIComponents** | Demo of modern UI Toolkit components (Unity 2021.2+) |

### Using the Samples

1. **Import**: In Package Manager, expand "Samples" and click "Import" next to the sample you want
2. **Create Settings**: `Assets > Create > LEADR > Settings`, enter your Game ID
3. **Configure Scene**: Open the sample scene and assign your LeadrSettings asset to the demo script
4. **Set Board**: Enter your board slug (e.g., "weekly") in the demo script's inspector
5. **Play**: Enter Play mode to test

## Development

### Local API Testing

When testing against a local LEADR server using HTTP (i.e., `Base Url` in LeadrSettings is `http://localhost:3000`), Unity blocks the request by default.

To enable HTTP connections:

1. Go to **Edit > Project Settings > Player**
2. Expand **Other Settings**
3. Find **Allow downloads over HTTP (nonsecure)**
4. Set to **Development builds only**

> **Note:** Only use HTTP for local development. Production should always use HTTPS.
