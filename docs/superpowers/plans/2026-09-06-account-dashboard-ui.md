# Account Dashboard UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild the Avalonia main window to match the approved dark Codex Multiple Accounts dashboard reference while preserving existing profile/session behavior.

**Architecture:** Keep profile and terminal behavior in the existing services. Add a small presentation wrapper for account-card-only visual data, then restyle the shell and terminal workspace in Avalonia XAML. Screenshot mode provides deterministic usage/demo metadata without parsing Codex credentials in normal runtime.

**Tech Stack:** .NET 10, Avalonia, CommunityToolkit.Mvvm.

**Spec:** `docs/superpowers/specs/2026-09-06-codex-multiple-accounts-design.md`

## Global Constraints

- Do not inspect or transform Codex credential contents.
- Isolated launch keeps `CODEX_HOME` child-process scoped.
- Global activation remains explicit and visually distinct from Launch.
- Existing external terminal action remains available.
- Dark/light system theme support remains enabled.

---

### Task 1: Add account-card presentation model

**Files:**
- Create: `src/CodexMultipleAccounts.App/ViewModels/ProfileCardViewModel.cs`
- Modify: `src/CodexMultipleAccounts.App/ViewModels/MainWindowViewModel.cs`

**Interfaces:**
- `ProfileCardViewModel.Profile : CodexProfile`
- `ProfileCardViewModel.Name`, `CodexHome`, `Accent`, `IsGloballyActive`, `FiveHourPercent`, `WeeklyPercent`, `FiveHourLabel`, `WeeklyLabel`
- `MainWindowViewModel.ProfileCards : ObservableCollection<ProfileCardViewModel>`
- `LaunchProfileCommand`, `ExternalProfileCommand`, `ActivateProfileCommand` accept a card parameter.

- [ ] Add a presentation wrapper that never reads auth/token files.
- [ ] Build real cards from existing profile metadata; usage is unavailable by default.
- [ ] Build deterministic screenshot cards with Personal/Work/Testing usage values from the approved reference.
- [ ] Keep `SelectedProfile` synchronized when selecting a card.

### Task 2: Implement Hallmark-style desktop shell

**Files:**
- Modify: `src/CodexMultipleAccounts.App/App.axaml`
- Modify: `src/CodexMultipleAccounts.App/MainWindow.axaml`

**Interfaces:**
- Binds `ProfileCards`, `SelectedProfileCard`, `Sessions`, `SelectedSession`, and profile commands from Task 1.

- [ ] Add reusable brushes/styles for dark navy surfaces, borders, compact buttons, account cards, nav rows, and terminal surfaces.
- [ ] Build left navigation rail with Accounts active and future sections visually present.
- [ ] Build account pane with heading, Add Account, Import, account cards, usage bars, and per-card actions.
- [ ] Build terminal pane with session tabs, compact toolbar, terminal output, input row, and session footer.
- [ ] Preserve responsive terminal-first sizing using weighted grid columns and minimum widths.

### Task 3: Verification

**Files:**
- No new production files.

- [ ] Trigger CI from the PR branch and require cross-platform build/tests to pass.
- [ ] Verify screenshot workflow builds the actual Avalonia app and uploads the screenshot artifact without pushing to feature branches.
- [ ] Inspect PR review threads and do not resolve the PTY review blocker unless a real PTY implementation exists.
