# Antigravity Profiles Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add provider-aware Antigravity profiles to the existing Avalonia Codex Multiple Accounts app without regressing Codex behavior.

**Architecture:** Keep the existing App/Core split. Add provider metadata and Antigravity-specific profile/launch/process services in Core; adapt the existing Avalonia cards and commands to dispatch by provider. Codex remains PTY-based with isolated `CODEX_HOME`; Antigravity runs as isolated external desktop processes.

**Tech Stack:** .NET 10, Avalonia 12, CommunityToolkit.Mvvm, Porta.Pty, AvaloniaTerminal, xUnit.

**Spec:** `docs/superpowers/specs/2026-09-06-antigravity-profiles-design.md`

## Global Constraints

- Existing Codex profiles and persisted catalogs remain readable.
- Existing Codex PTY, external launch, import, delete, and global activation behavior must remain unchanged.
- Antigravity environment changes are child-process scoped only.
- Antigravity global activation is out of scope.
- Credential/token contents are never inspected or logged.
- Windows, Linux, and macOS launch specifications are covered by tests.

---

### Task 1: Provider-aware profile metadata

**Files:**
- Create: `src/CodexMultipleAccounts.Core/Profiles/AccountProvider.cs`
- Create: `src/CodexMultipleAccounts.Core/Profiles/AntigravityProfileMode.cs`
- Modify: `src/CodexMultipleAccounts.Core/Profiles/CodexProfile.cs`
- Modify: `src/CodexMultipleAccounts.Core/Profiles/ProfileService.cs`
- Test: existing Core profile tests plus new provider compatibility cases.

**Interfaces:**
- Produces `AccountProvider.Codex`, `AccountProvider.Antigravity`.
- `CodexProfile` gains optional provider/mode metadata with Codex-compatible defaults.

- [ ] Add failing tests proving old JSON without provider fields deserializes as Codex.
- [ ] Run the focused profile tests and verify failure.
- [ ] Add provider/mode types and backward-compatible record defaults.
- [ ] Run focused tests and verify pass.

### Task 2: Antigravity profile storage

**Files:**
- Create: `src/CodexMultipleAccounts.Core/Profiles/AntigravityProfileService.cs`
- Test: `tests/CodexMultipleAccounts.Core.Tests/AntigravityProfileServiceTests.cs`

**Interfaces:**
- `CreateAsync(string name, AntigravityProfileMode mode)` creates manager-owned profile roots.
- Profile root contains deterministic home/config/cache/data/state subpaths.

- [ ] Write failing tests for full/shared profile creation and managed-root delete safety.
- [ ] Run focused tests and verify failure.
- [ ] Implement minimal profile creation/list/delete support using the existing catalog model.
- [ ] Run focused tests and verify pass.

### Task 3: Platform Antigravity launch specifications

**Files:**
- Create: `src/CodexMultipleAccounts.Core/Launching/HostPlatform.cs`
- Create: `src/CodexMultipleAccounts.Core/Launching/AntigravityLaunchSpec.cs`
- Create: `src/CodexMultipleAccounts.Core/Launching/AntigravityLaunchService.cs`
- Test: `tests/CodexMultipleAccounts.Core.Tests/AntigravityLaunchServiceTests.cs`

**Interfaces:**
- `Create(CodexProfile profile, HostPlatform platform, string executablePath)` returns executable, arguments, working directory, and child environment.
- Windows emits `USERPROFILE`, `APPDATA`, `LOCALAPPDATA`.
- Linux emits `HOME` plus XDG variables and user-data/extensions arguments.
- macOS emits `HOME` plus user-data/extensions arguments.

- [ ] Write one failing test per platform plus a test that parent environment is not mutated.
- [ ] Run focused tests and verify failure.
- [ ] Implement launch-spec construction only; do not start processes yet.
- [ ] Run focused tests and verify pass.

### Task 4: Process lifecycle tracking

**Files:**
- Create: `src/CodexMultipleAccounts.Core/Launching/IProcessRunner.cs`
- Create: `src/CodexMultipleAccounts.Core/Launching/SystemProcessRunner.cs`
- Create: `src/CodexMultipleAccounts.Core/Launching/AntigravityProcessManager.cs`
- Test: `tests/CodexMultipleAccounts.Core.Tests/AntigravityProcessManagerTests.cs`

**Interfaces:**
- `Start(profile, spec)`, `Stop(profileId)`, `Restart(profile, spec)`, `IsRunning(profileId)`.
- Process runner is injectable so tests never launch Antigravity.

- [ ] Write failing lifecycle tests using a fake runner.
- [ ] Run focused tests and verify failure.
- [ ] Implement process tracking and safe exited-process cleanup.
- [ ] Run focused tests and verify pass.

### Task 5: Avalonia provider-aware account cards

**Files:**
- Modify: `src/CodexMultipleAccounts.App/ViewModels/ProfileCardViewModel.cs`
- Modify: `src/CodexMultipleAccounts.App/ViewModels/MainWindowViewModel.cs`
- Modify: `src/CodexMultipleAccounts.App/MainWindow.axaml`

**Interfaces:**
- Cards expose provider label, provider-specific actions, and Antigravity running state.
- Codex actions continue to use existing PTY/external/global activation paths.
- Antigravity Start/Stop/Restart use `AntigravityProcessManager`.

- [ ] Add view-model tests for provider dispatch and command visibility/state.
- [ ] Run focused tests and verify failure.
- [ ] Implement provider-aware cards and commands with minimal XAML changes to the current dashboard.
- [ ] Run app/view-model tests and verify pass.

### Task 6: Documentation and full verification

**Files:**
- Modify: `README.md`

- [ ] Document Codex and Antigravity provider behavior, profile modes, and parallel launch semantics.
- [ ] Run `dotnet test CodexMultipleAccounts.slnx`.
- [ ] Run `dotnet build CodexMultipleAccounts.slnx`.
- [ ] Inspect the resulting PR diff for accidental credential handling or parent-environment mutation.
- [ ] Open PR targeting `main` with verification notes.
