# Codex Multiple Accounts

Cross-platform Avalonia desktop manager for running multiple OpenAI Codex CLI accounts without repeatedly replacing one shared `~/.codex` directory.

## What it does

The app supports two deliberately separate modes:

- **Launch isolated** — each managed profile gets its own `CODEX_HOME`. You can run Personal, Work, Test, or other Codex accounts at the same time because each Codex process reads and writes a different home directory.
- **Activate globally** — explicitly copy one profile into the normal `~/.codex` location for editor integrations or other clients that only use the default Codex home. The previous default is backed up first.

Isolated launching never changes the global account.

## Platforms

| Platform | App | Isolated Codex | Embedded terminal | External terminal |
| --- | --- | --- | --- | --- |
| Windows x64/arm64 | Avalonia | Yes | Microsoft Terminal + PTY | Windows Terminal |
| Linux | Avalonia | Yes | v1 fallback | `x-terminal-emulator` |
| macOS | Avalonia | Yes | v1 fallback | Terminal.app |

The Windows embedded terminal follows the same native-host architecture used by [Dev Board](https://github.com/dhhieu113pro/dev-board): Avalonia `NativeControlHost`, `Microsoft.Terminal.Control.dll`, and `Porta.Pty`.

## Parallel account model

```text
Codex Multiple Accounts
        |
        +-- Personal profile
        |     CODEX_HOME = .../profiles/<id>/codex-home
        |                     |
        |                     +-- codex process A
        |
        +-- Work profile
              CODEX_HOME = .../profiles/<id>/codex-home
                              |
                              +-- codex process B
```

The application sets `CODEX_HOME` on the child process only. It does not change the machine environment or the manager process environment.

## Getting started

Prerequisites:

- .NET 10 SDK when building from source
- Codex CLI available as `codex` on `PATH`

Build and test:

```bash
dotnet restore CodexMultipleAccounts.slnx
dotnet build CodexMultipleAccounts.slnx -c Release
dotnet test CodexMultipleAccounts.slnx -c Release --no-build
```

Run:

```bash
dotnet run --project src/CodexMultipleAccounts.App/CodexMultipleAccounts.App.csproj
```

Then either:

1. Enter a profile name and choose **Create**. Launching the empty profile opens Codex with its own home, where normal Codex sign-in can take place.
2. Choose **Import ~/.codex** to copy the current default Codex state into a new managed profile.
3. Select a profile and choose **Launch isolated**. Repeat with another profile to run both accounts concurrently.
4. Use **Open externally** when you prefer a separate OS terminal window.
5. Use **Activate globally** only when you want the selected account to become the editor/default Codex account.

## Storage

The manager metadata and profile homes are local-only:

- Windows: `%LOCALAPPDATA%/CodexMultipleAccounts`
- macOS: `~/Library/Application Support/CodexMultipleAccounts`
- Linux: `$XDG_DATA_HOME/codex-multiple-accounts`, or `~/.local/share/codex-multiple-accounts`

Each profile contains a dedicated `codex-home` directory. Codex-owned files such as `auth.json`, configuration, history, and sessions are treated as opaque files by this app.

## Credential safety

- Authentication content is never written to application logs.
- Import copies Codex files without parsing or transforming tokens.
- Profile deletion is restricted to the application's managed profiles root and requires confirmation in the UI.
- Global activation stages the selected profile before replacing the default home.
- Existing global state is moved to a recoverable sibling directory named like `.codex.cma-backup-<id>` before activation completes.
- Two isolated profiles never share a mutable Codex home.

Because profile homes can contain Codex credentials, protect your operating-system user account and do not publish or sync the profile storage directory to an untrusted location.

## Architecture

```text
CodexMultipleAccounts.App (Avalonia)
        |
        +-- Profile UI / terminal tabs
        +-- External terminal launcher
        +-- Windows native terminal host
        |
CodexMultipleAccounts.Core
        |
        +-- ProfileStore / ProfileService
        +-- SafeFileTree
        +-- CodexLaunchService
        +-- GlobalActivationService
        +-- ExternalTerminalCommandBuilder
```

Core behavior is intentionally independent from Avalonia so profile and filesystem logic can be tested on Windows, Linux, and macOS.

## CI

GitHub Actions restores, builds, and tests the solution on:

- `windows-latest`
- `ubuntu-latest`
- `macos-latest`

Tests cover profile lifecycle, opaque import/copy, destructive path guards, isolated launch environments, parallel child-process isolation, global activation/restore, and platform terminal command construction.

## Current v1 scope

Included:

- create/import/rename/delete profiles
- isolated per-profile `CODEX_HOME`
- multiple parallel Codex processes
- explicit global activation with backup
- embedded Windows terminal tabs
- external terminal launching on Windows/Linux/macOS
- system light/dark theme through Avalonia

Not yet included:

- quota-aware automatic account rotation
- cloud profile sync
- token editing
- embedded Linux/macOS terminal rendering

## Development notes

Design and implementation planning are tracked under `docs/superpowers/`.
