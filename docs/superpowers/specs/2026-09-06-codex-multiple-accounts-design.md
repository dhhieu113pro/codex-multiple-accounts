# Codex Multiple Accounts — v1 Design

Date: 2026-09-06

## Goal

Build a cross-platform Avalonia desktop app that lets a developer use multiple OpenAI Codex CLI accounts safely and concurrently without repeatedly overwriting one shared `~/.codex` state.

The app supports two explicit modes:

1. **Launch isolated** — run Codex with a profile-specific `CODEX_HOME`, allowing multiple accounts to run in parallel.
2. **Activate globally** — copy a selected profile's Codex state into the normal default Codex home for tools such as editor integrations that only observe the default home.

## Chosen approach

Use one shared account-management core with a platform terminal abstraction.

- UI: Avalonia, one desktop codebase for Windows, Linux, and macOS.
- Profile isolation: each managed account owns a dedicated directory used as `CODEX_HOME`.
- Embedded terminal: preferred launch experience. On Windows, follow the proven `dev-board` architecture: Avalonia `NativeControlHost` + PTY process connection + terminal surface abstraction.
- External terminal: always exposed as an action and used as the fallback where the embedded native terminal backend is unavailable.
- Global activation: explicit user action that synchronizes the chosen profile into the platform's default Codex home; it is never done implicitly by opening an isolated session.

This keeps account storage independent from terminal presentation and prevents Windows-specific terminal code from leaking into profile management.

## Alternatives considered

### 1. Swap only the default `~/.codex`

Simplest implementation, but it cannot safely support parallel Codex sessions because all processes share the same mutable state. Rejected as the primary architecture.

### 2. Launch only external terminals

Cross-platform and simple, but gives a weaker UX and makes session lifecycle harder to manage from the app. Kept as a fallback/action, not the primary experience.

### 3. Shared core + embedded terminal abstraction (chosen)

More initial structure, but cleanly supports parallel sessions, native Windows terminal hosting, portable fallback behavior, and future terminal backends without coupling them to account state.

## Profile model

A profile contains only local metadata required by the manager:

- stable profile ID
- display name
- path to its dedicated Codex home
- optional last-used timestamp
- whether it is currently the profile activated globally

The Codex-owned files inside that profile home are treated as opaque. The manager does not parse or rewrite authentication tokens unless a future feature explicitly requires it.

Recommended storage layout:

```text
<AppData>/CodexMultipleAccounts/
  profiles.json
  profiles/
    <profile-id>/
      codex-home/
        ... Codex-managed files ...
```

## Import and account creation

v1 supports creating a profile directory and importing an existing default Codex home into a profile. Import is a filesystem copy, preserving Codex-owned contents without understanding credential internals.

A new empty profile can also be launched; Codex can then perform its normal sign-in flow inside that isolated `CODEX_HOME`.

## Isolated launch flow

1. User selects a profile and working directory.
2. App builds a launch specification for `codex`.
3. App sets `CODEX_HOME` only in the child process environment to the profile's dedicated home.
4. App starts the session in an embedded terminal tab when a supported embedded backend is available.
5. User may instead choose **Open externally**, which launches an OS terminal with the same working directory and `CODEX_HOME`.
6. Closing one terminal/session only terminates that session; it must not mutate another profile or global Codex state.

The app must never set process-wide or machine-wide `CODEX_HOME` merely to launch an isolated profile.

## Global activation flow

Global activation exists for compatibility with clients that use the default Codex home.

1. Resolve the OS/user default Codex home.
2. Back up the current default Codex state when it is not already represented by the selected profile.
3. Synchronize the selected profile's Codex-owned contents into the default home.
4. Record which profile is globally active.
5. Show the active profile clearly in the UI.

Activation must use staged filesystem operations so a failed copy does not leave a half-written default home. The app should preserve a recoverable backup before replacing an existing default state.

## Terminal architecture

Define an app-level abstraction similar to Dev Board's terminal surfaces:

```text
ITerminalSession
  View
  BackendName
  StartAsync(LaunchSpec)
  Stop()
  Exited
```

`LaunchSpec` carries executable, arguments, working directory, and environment overrides including `CODEX_HOME`.

### Windows embedded backend

Reference `dhhieu113pro/dev-board`:

- Avalonia `NativeControlHost`
- PTY-backed child process
- UTF-8 output/input streams
- terminal resize forwarding to PTY
- explicit lifecycle/kill handling
- terminal tab visibility management

The implementation may reuse the same architectural pattern and compatible dependencies, but remains isolated behind the terminal interface.

### Linux/macOS

Keep the terminal interface platform-neutral. For v1, use an embedded PTY-backed surface if the chosen terminal rendering dependency supports the platform reliably; otherwise expose external-terminal launch as the supported fallback rather than blocking the entire app.

## External terminal launch

Provide a platform service:

```text
IExternalTerminalLauncher.LaunchAsync(LaunchSpec)
```

It builds an OS-appropriate command that starts the user's terminal with the requested working directory and child environment. Exact terminal selection is platform-specific and should prefer widely available defaults rather than hard-coding one vendor globally.

## UI

Main window v1:

- profile list/sidebar
- profile name and status
- **Launch** primary action
- **Open externally** secondary action
- **Activate globally** explicit action
- import existing default account
- create new isolated profile
- delete/rename profile
- tabbed embedded terminal area for active sessions
- visible badge/indicator for the globally active profile

The app should make the difference between **isolated launch** and **global activation** visually obvious so the user does not accidentally replace editor-visible credentials.

## Safety and filesystem behavior

- Never log authentication file contents or tokens.
- Do not inspect or transform Codex credentials in v1.
- Profile deletion requires confirmation and must not delete the default Codex home.
- Global activation creates/restores backups using atomic/staged moves where possible.
- Parallel isolated sessions always point at separate profile homes.
- Validate paths before recursive copy/delete operations.

## Error handling

Surface actionable errors for:

- `codex` executable not found
- failed PTY/native terminal creation
- unsupported embedded terminal backend
- profile directory unavailable
- import/copy failure
- global activation backup/synchronization failure
- external terminal unavailable

If embedded startup fails and external launch is supported, the UI may offer the external action without changing profile state.

## Testing

Use test-driven implementation for the shared core.

Unit tests:

- profile create/rename/delete metadata
- per-profile Codex home resolution
- isolated launch spec includes only child `CODEX_HOME`
- profile import copies opaque files
- global activation backup and replace behavior
- failure leaves default state recoverable
- path safety guards
- platform command construction for external terminals

Integration tests:

- two fake Codex processes launched concurrently receive different `CODEX_HOME` values
- profile import + activation round trip using temporary directories
- terminal lifecycle abstraction with a fake PTY/backend

Platform-specific UI/native terminal tests can be gated per OS in GitHub Actions. Core tests should run on Windows, Linux, and macOS.

## v1 non-goals

- cloud syncing account profiles
- editing OAuth/token files directly
- automatic account rotation based on quotas
- sharing one profile's mutable Codex home between simultaneous sessions
- embedding an editor or full IDE
- remote terminal/session support

## Success criteria

v1 is ready for review when:

1. A user can import or create at least two profiles.
2. Two Codex sessions can run concurrently with distinct `CODEX_HOME` directories.
3. Embedded terminal tabs work on Windows using the Dev Board-style native/PTY architecture.
4. **Open externally** launches the selected isolated profile without modifying global state.
5. A profile can be explicitly activated into the default Codex home with backup/recovery behavior.
6. Shared/core tests pass on Windows, Linux, and macOS CI.
7. Authentication contents never appear in application logs.
