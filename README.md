# LEADR Unity SDK

[![Unity 2020.3+](https://img.shields.io/badge/Unity-2020.3%2B-blue)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Add beautiful cross-platform leaderboards to your game in minutes.

**[Documentation](https://docs.leadr.gg)** · **[Status](https://status.leadr.gg)** · **[Discord](https://discord.gg/RMUukcAxSZ)**

## Features

- **Drop-in UI Components** - Pre-built leaderboard and submission forms to get you started
- **Anti-cheat Protection** - Server-side validation and rate limiting
- **Seasons & Time Windows** - Weekly, monthly, or custom periods
- **Rich Metadata** - Attach custom data to every score
- **Web Views** - Shareable leaderboard pages for each board
- **Async/Await** - Modern Unity async patterns

## Requirements

- Unity 2020.3 or later
- Unity 2021.2+ for UI Toolkit components

## Installation

1. Open **Window > Package Manager**
2. Click **+** > **Add package from git URL**
3. Enter: `https://github.com/LEADR-Official/leadr-sdk-unity.git?path=Packages/com.leadr.sdk`

## Quick Start

1. **Get your Game ID**: Visit our [Get Started](https://docs.leadr.gg/latest/quick_start/) page to download the CLI and create your account
2. **Create Settings**: In Unity, go to `Assets > Create > LEADR > Settings` and enter your Game ID
3. **Import a Sample**: In Package Manager, expand "Samples" and import **Basic Integration**
4. **Configure & Run**: Assign your Settings asset and board slug, then enter Play mode and check the console output

For full usage examples and API reference, see the **[SDK Documentation](https://docs.leadr.gg/sdks/unity)**.

## UI Toolkit Components

Pre-built components for Unity 2021.2+ (UI Toolkit):

| Component | Description |
|-----------|-------------|
| `LeadrBoardView` | Paginated leaderboard with loading/error/empty states |
| `LeadrScoreSubmitter` | Score submission form with validation |
| `LeadrScoreEntry` | Individual score row |

See the **LeadrUIComponents** sample for a complete demo.

## Samples

Import via **Window > Package Manager > LEADR Unity SDK > Samples**.

| Sample | Description |
|--------|-------------|
| **Basic Integration** | Minimal example: initialize, fetch board by slug, display scores |
| **Canvas Example** | Full uGUI example with board selector, pagination, and submission |
| **LeadrUIComponents** | UI Toolkit components demo with board selector |

## Development

### Local API Testing

When developing/testing locally (ie against `http://localhost:3000`):

1. Go to **Edit > Project Settings > Player**
2. Expand **Other Settings**
3. Set **Allow downloads over HTTP** to **Development builds only**

## Support

- [Documentation](https://docs.leadr.gg)
- [Discord Community](https://discord.gg/RMUukcAxSZ)
- [Report Issues](https://github.com/LEADR-Official/leadr-sdk-unity/issues)

## License

Apache v2 License - see [LICENSE](LICENSE) for details.
