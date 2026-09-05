# Codex Multiple Accounts

Cross-platform Avalonia manager for running multiple Codex CLI accounts in parallel.

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

## Security

Profile directories contain Codex-managed authentication state. Treat the application-data `CodexMultipleAccounts/profiles` directory as sensitive. The manager copies Codex files opaquely and never logs token contents.
