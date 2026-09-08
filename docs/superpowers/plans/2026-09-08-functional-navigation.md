# Functional Navigation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make all six sidebar destinations functional without disrupting existing isolated accounts or terminal sessions.

**Architecture:** Retain the existing XAML workspace and mount a separate page host over its workspace column. A shared observable navigation model selects pages and independent page models own settings, usage, and session management. Preserve the existing main-window layout and make narrowly scoped changes to bootstrap and launch services.

**Tech Stack:** .NET 10, C# 14, Avalonia 12, CommunityToolkit.Mvvm, Porta.Pty, xUnit.

**Spec:** `docs/superpowers/specs/2026-09-08-functional-navigation-design.md`

## Global Constraints

- Never parse or log authentication token contents.
- Never mutate process-wide CODEX_HOME for an isolated launch.
- Preserve explicit global activation and its backup behavior.
- Keep existing terminal models mounted and alive across navigation.
- A stop/close action affects only the selected manager-owned embedded session.
- No fabricated quota values or undocumented authenticated usage endpoints.
- Use the existing theme brushes and test all three supported desktop platforms.
- Do not merge before full CI and review.

## Task 1: Navigation and retained workspace

**Files:** Create `src/CodexMultipleAccounts.App/Navigation/WorkspaceNavigationViewModel.cs`, `src/CodexMultipleAccounts.App/MainWindow.Navigation.cs`; modify `src/CodexMultipleAccounts.App/App.axaml.cs`; test `tests/CodexMultipleAccounts.App.Tests/WorkspaceNavigationTests.cs`.

**Interfaces:** `WorkspacePage` enum; `WorkspaceNavigationViewModel.SelectedPage`, `Navigate(WorkspacePage)`, `OpenSession(TerminalSessionViewModel)`; `MainWindow.InitializeNavigationShell(MainWindowViewModel,string)`.

- [ ] Write tests asserting Accounts default, all six destinations, invalid selection rejection, and selecting a session preserving the same object.
- [ ] Run the focused tests and observe the expected missing-type failures.
- [ ] Implement observable navigation with a stable MainWindowViewModel reference.
- [ ] Wire the six existing sidebar buttons, active visual state, and a separate page host. Keep WorkspaceShell mounted; hide it on non-Accounts pages. Restore adaptive layout on return.
- [ ] Test navigation on the CI matrix and verify no placeholder buttons remain disabled.

## Task 2: Settings and launch configuration

**Files:** Create `Core/Settings/AppSettings.cs`, `Core/Settings/AppSettingsService.cs`, `Core/Launching/ExecutableResolver.cs`; create app settings view model/view; modify `App.axaml.cs`, `CodexLaunchService.cs`, and `AntigravityExecutableLocator.cs`; tests in Core.Tests and App.Tests.

**Interfaces:** `AppSettings(DefaultWorkspace,CodexExecutable,AntigravityExecutable,Appearance)`; `AppSettingsService.LoadAsync()`, `SaveAsync(AppSettings)`, `Defaults()`; executable resolver accepts an explicit executable or PATH command and validates it; settings model Save/Reset/Browse commands.

- [ ] Write failing round-trip, malformed JSON, invalid workspace, invalid executable, atomic save, and default tests.
- [ ] Run focused tests, then implement non-secret persistence and strict validation.
- [ ] Add editable settings fields, folder picker, Save/Reset, validation errors, and appearance selection.
- [ ] Route future Codex and Antigravity launches through saved configuration, preserving environment fallback and existing-session working directories.
- [ ] Verify tests and regression checks for child-only environment and explicit global activation.

## Task 3: Per-session lifecycle

**Files:** Modify `TerminalSessionViewModel.cs`, `ProcessTerminalLauncher.cs`, and `MainWindowViewModel.cs`; create a session management view and lifecycle abstraction as required; add App.Tests lifecycle tests.

**Interfaces:** `StopAsync()` idempotently terminates one owned session; Close requests confirmation for a live session, then stops and removes it; `OpenSession` returns to the existing terminal.

- [ ] Write failing tests for stop isolation, repeated stop, exited-close, running-close cancellation, duplicate exit handling, and preserving sessions across page navigation.
- [ ] Run focused tests to observe failures.
- [ ] Add a per-session PTY owner with cancellation and safe process termination; wire stop/close actions into session management and terminal headers.
- [ ] Ensure shutdown releases manager-owned sessions and no detached output pump writes into disposed state.
- [ ] Verify lifecycle tests and two concurrent fake sessions.

## Task 4: Usage, documentation, and About

**Files:** Create focused page view models/views and tests; update README navigation and safety documentation.

- [ ] Write failing tests asserting absent quota is unavailable, no zero-percent substitution, and assembly-derived version metadata.
- [ ] Implement usage rows from profile metadata without credential inspection; show source/timestamp only when actual supported data exists.
- [ ] Add local documentation for installation, isolation, workspace selection, and troubleshooting with verified official destinations.
- [ ] Add assembly version, OS/runtime, repository, and license discovery; do not invent a license.
- [ ] Verify all three pages render in dark/light themes.

## Task 5: Layout, CI, and review

**Files:** Modify window initialization/layout only as needed; add App.Tests layout checks; update README and existing screenshot workflow if required.

- [ ] Write layout tests for 760, 900, 1000, 1200, 1600 widths and constrained-screen startup sizing.
- [ ] Keep the existing adaptive account pane and use the full workspace for non-Accounts pages. All page forms must scroll vertically and avoid fixed wide content.
- [ ] Run restore/build/tests for the full solution on Windows, Linux, and macOS; investigate failures with concrete logs and rerun.
- [ ] Inspect the actual screenshot workflow result and any native terminal smoke-test limitations.
- [ ] Review the complete diff for credentials, destructive operations, navigation regressions, and settings migration safety.
- [ ] Open a reviewable PR with exact test results and remaining limitations; leave main unchanged until approval and green CI.

## Execution environment

The current container has neither a .NET SDK nor DNS access to github.com, so local restore/build is unavailable. Use the connected GitHub repository and its existing three-OS CI for compilation and test execution. Record missing-environment failures explicitly rather than claiming local verification. This constraint does not waive the required red/green verification or allow placeholder implementation.
