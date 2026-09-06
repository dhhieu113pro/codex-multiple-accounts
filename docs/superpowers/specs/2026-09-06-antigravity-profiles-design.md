# Antigravity Profiles Design

## Goal

Extend Codex Multiple Accounts into a provider-aware Avalonia launcher while preserving existing Codex behavior and adding Multigravity-style Antigravity filesystem profiles that can run in parallel on Windows, Linux, and macOS.

## Architecture

Keep `CodexMultipleAccounts.App` as the Avalonia shell and `CodexMultipleAccounts.Core` as the reusable domain layer. Introduce a small provider abstraction so Codex and Antigravity can share profile presentation without sharing provider-specific launch semantics.

Codex continues to launch through the existing PTY with a child-scoped `CODEX_HOME`. Antigravity launches as an external desktop process with a profile-specific filesystem root and platform-specific environment/arguments.

## Provider model

Add `AccountProvider` with stable enum values for Codex and Antigravity. Existing persisted Codex profiles remain readable because missing provider metadata defaults to Codex.

Antigravity profile metadata includes `Full` and `Shared` modes. `Full` is the supported isolation mode in this slice. `Shared` is persisted and exposed for forward compatibility, but real cross-platform settings/extensions sharing is deferred until safe symlink/junction behavior is implemented.

## Launch isolation

Windows Antigravity launch sets child-only `USERPROFILE`, `APPDATA`, and `LOCALAPPDATA` rooted in the selected profile. Linux sets child-only `HOME`, `XDG_CONFIG_HOME`, `XDG_CACHE_HOME`, `XDG_DATA_HOME`, and `XDG_STATE_HOME` and passes `--user-data-dir` and `--extensions-dir`. macOS sets child-only `HOME` and passes `--user-data-dir` and `--extensions-dir`.

No Antigravity launch changes the parent process environment.

## Authentication boundary

Current Antigravity versions use a fixed OS credential-store identity. Filesystem isolation therefore does not guarantee independent logged-in Antigravity accounts under the same OS user. The implementation must expose this limitation clearly and must not claim independent Antigravity authentication until a stronger OS-user/keychain boundary exists.

## Process tracking

Track Antigravity process IDs per profile for the current manager session. The UI exposes Running/Stopped state and Start/Stop/Restart actions. Process tracking is intentionally best-effort across app restarts; persisted PID recovery is out of scope for this slice.

## UI

Keep the current account-card dashboard. Add a provider badge, provider-aware Add Account controls, and Antigravity actions. Codex retains embedded PTY, external launch, and global activation. Antigravity launches externally and does not expose global activation in this slice.

## Compatibility and safety

- Existing Codex profiles and `profiles.json` must continue to load.
- Existing Codex launch, import, delete, activation, PTY, and external launch behavior must not regress.
- Never inspect or log credential/token contents.
- Never mutate the normal Antigravity profile while creating isolated profiles.
- Delete operations remain constrained to manager-owned profile roots.
- Antigravity independent-auth support is reported as false for this isolation strategy.

## Testing

Add unit tests for provider compatibility, Antigravity profile path construction, platform launch specifications, child-only environment isolation, and process-state transitions using an injectable process runner. Existing cross-platform CI remains the acceptance gate.
