# Codex Multiple Accounts

Cross-platform Avalonia manager for running multiple Codex CLI accounts in parallel.

<p align="center">
  <img src="docs/assets/app-screenshot.png" alt="Codex Multiple Accounts app showing isolated Codex profiles and parallel launch controls" width="1000" />
</p>

## Modes

- **Isolated launch**: every profile owns a separate Codex home and each child `codex` process receives its own `CODEX_HOME`. Multiple profiles can therefore run concurrently without swapping the parent/global environment.
- **Activate globally**: explicitly promotes a selected profile into the normal `~/.codex` home for editor integrations, backing up the previous default state first.

## Platforms

Windows, Linux and macOS. The app includes an embedded process terminal view and an **Open externally** action. Platform terminal integration can be enhanced independently from account isolation.

## Build

```bash
dotnet restore CodexMultipleAccounts.slnx
dotnet build CodexMultipleAccounts.slnx
dotnet run --project src/CodexMultipleAccounts.App
```

## Screenshot

The checked-in screenshot is captured from the real Avalonia application by the Windows **App Screenshot** workflow using a deterministic demo state. The workflow also uploads the PNG as an Actions artifact and refreshes `docs/assets/app-screenshot.png` when the app UI changes.

## Security

Profile directories contain Codex-managed authentication state. Treat the application-data `CodexMultipleAccounts/profiles` directory as sensitive. The manager copies Codex files opaquely and never logs token contents.
