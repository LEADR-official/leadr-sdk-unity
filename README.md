# LEADR SDK for Unity

[![Unity 2020.3+](https://img.shields.io/badge/Unity-2020.3%2B-blue)](https://unity.com/)
[![CI](https://github.com/LEADR-official/leadr-sdk-unity/actions/workflows/ci.yml/badge.svg)](https://github.com/LEADR-official/leadr-sdk-unity/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)

Add cross-platform leaderboards to your game in minutes with the official LEADR SDK for Unity Engine.

> **New to LEADR?** Follow the [Quick Start guide](https://docs.leadr.gg/latest/quick-start/)
> to create your account and set up your first leaderboard.

## Features

- **Easy integration** - Automatic authentication and token management
- **Anti-cheat Protection** - Server-side validation and rate limiting
- **Async/Await** - Modern Unity async patterns
- **Drop-in UI Components** - Pre-built leaderboard and submission forms to get you started

## Prerequisites

- Unity 2020.3 or later
- (Unity 2021.2+ for UI Toolkit components)
- A LEADR account and game ID - if you don't have these yet, follow the [Quick Start guide](https://docs.leadr.gg/latest/quick-start/) first

## Installation

### From Unity Asset Store

_Coming soon..._

### Manual Installation

1. Open **Window > Package Manager**
2. Click **+** > **Install package from git URL...**
3. Enter: `https://github.com/LEADR-Official/leadr-sdk-unity.git?path=Packages/com.leadr.sdk`

## Quick Start

### 1. Configure Settings

Create a `LeadrSettings` asset with your game ID:

1. In the Project panel, right-click in your desired folder (e.g., Assets/Settings/)
1. Select Create > LEADR > Settings
1. Name the asset LeadrSettings (or any name you prefer)
1. Select the asset to configure it in the Inspector
1. Update the `LeadrSettings` asset with your GameId (get this from the LEADR app).

For full usage examples and API reference, see the **[SDK Documentation](https://docs.leadr.gg/sdks/unity)**.

### 2. Use the API

```csharp
using Leadr;
using UnityEngine;

public class LeadrDemo : MonoBehaviour
{
    async void Start()
    {
        // Get leaderboards
        var boardsResult = await LeadrClient.Instance.GetBoardsAsync();
        if (boardsResult.IsSuccess)
        {
            foreach (var board in boardsResult.Data.Items)
                Debug.Log($"Board: {board.Name}");
        }

        // Get scores for a specific board
        var scoresResult = await LeadrClient.Instance.GetScoresAsync("brd_your_board_id", limit: 10);
        if (scoresResult.IsSuccess)
        {
            foreach (var score in scoresResult.Data.Items)
                Debug.Log($"#{score.Rank} {score.PlayerName}: {score.Value}");
        }

        // Submit a score
        var submitResult = await LeadrClient.Instance.SubmitScoreAsync(
            "brd_your_board_id", 12345, "Player Name");
        if (submitResult.IsSuccess)
            Debug.Log($"Score submitted! Rank: #{submitResult.Data.Rank}");
        else
            Debug.LogError($"Error: {submitResult.Error}");
    }
}
```

## Configuration

### LeadrSettings Asset

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `gameId` | String | *required* | Your LEADR game UUID |
| `baseUrl` | String | `https://api.leadrcloud.com` | API endpoint |
| `debugLogging` | bool | `false` | Enable verbose logging |
| `testMode` | bool | `false` | Mark scores as test data |

## Samples

Import samples via **Window > Package Manager > LEADR Unity SDK > Samples**. Includes Basic Integration, Canvas Example, and LeadrUIComponents demos.

## What's Next

- **[Full Integration Guide](https://docs.leadr.gg/latest/sdks/unity/)** - Complete documentation with UI components, advanced usage, and troubleshooting
- **[Samples](./Samples~/)** - Sample scenes demonstrating SDK features
- **[Join the Community!](https://discord.gg/RMUukcAxSZ){"target"="_blank"}** - Get support and inspiration on the LEADR Discord

## Need Help?

- [Discord](https://discord.gg/RMUukcAxSZ)
- [Full Documentation](https://docs.leadr.gg)
- [Report an issue](https://github.com/LEADR-official/leadr-sdk-unity/issues)
