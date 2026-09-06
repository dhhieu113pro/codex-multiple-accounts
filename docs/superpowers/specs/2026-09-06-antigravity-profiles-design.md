# Antigravity Profiles Design

## Goal

Extend Codex Multiple Accounts into a provider-aware Avalonia account launcher while preserving existing Codex behavior and adding Multigravity-style isolated Antigravity profiles that can run in parallel on Windows, Linux, and macOS.

## Architecture

Keep `CodexMultipleAccounts.App` as the Avalonia shell and `CodexMultipleAccounts.Core` as the reusable domain layer. Introduce a small provider abstraction so Codex and Antigravity can share profile presentation without sharing provider-specific launch semantics.

Codex continues to launch through the existing PTY with a child-scoped `CODEX_HOME`. Antigravity launches as an external desktop process with a profile-specific filesystem root and platform-specific environment/arguments.

## Provider model

Add `AccountProvider` with stable IDs `codex` and `antigravity`. Existing persisted Codex profiles remain readable and are treated as provider `codex` when no provider field exists.

Antigravity profiles support two modes:

- `Full`: isolated profile root, settings, extensions, caches, and application state.
- `Shared`: isolated account/application state while settings/extensions are shared with the normal Antigravity installation where the platform supports the required links/arguments.

## Launch isolation

Windows Antigravity launch sets child-only `USERPROFILE`, `APPDATA`, and `LOCALAPPDATA` rooted in the selected profile. Linux sets child-only `HOME`, `XDG_CONFIG_HOME`, `XDG_CACHE_HOME`, `XDG_DATA_HOME`, and `XDG_STATE_HOME` and passes `--user-data-dir` and `--extensions-dir`. macOS sets child-only `HOME` and passes `--user-data-dir` and `--extensions-dir`.

No Antigravity launch changes the parent process environment.

## Process tracking

Track Antigravity process IDs per profile for the current manager session. The UI exposes Running/Stopped state and Start/Stop/Restart actions. Process tracking is intentionally best-effort across app restarts; persisted PID recovery is out of scope for this slice.

## UI

Keep the current account-card dashboard. Add a provider badge, provider-aware Add Account flow, and Antigravity actions. Codex retains embedded PTY, external launch, and global activation. Antigravity launches externally and does not expose global activation in this slice.

## Compatibility and safety

- Existing Codex profiles and `profiles.json` must continue to load.
- Existing Codex launch, import, delete, activation, PTY, and external launch behavior must not regress.
- Never inspect or log credential/token contents.
- Never mutate the normal Antigravity profile while creating isolated profiles.
- Delete operations remain constrained to manager-owned profile roots.

## Testing

Add unit tests for provider compatibility, Antigravity profile path construction, platform launch specifications, child-only environment isolation, and process-state transitions using an injectable process runner. Existing cross-platform CI remains the acceptance gate.
