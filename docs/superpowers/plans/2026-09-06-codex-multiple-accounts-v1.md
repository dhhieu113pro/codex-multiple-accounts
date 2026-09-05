# Codex Multiple Accounts v1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a cross-platform Avalonia desktop app that manages isolated Codex profiles, runs multiple accounts concurrently via per-process `CODEX_HOME`, and optionally activates one profile into the default Codex home.

**Architecture:** A .NET 10 solution separates profile/filesystem/process behavior into a UI-independent core and keeps Avalonia/terminal integration in the desktop project. Every isolated launch passes `CODEX_HOME` only to the child process. Terminal presentation is abstracted so Windows can use the Dev Board-style PTY/native host while external terminals remain available cross-platform.

**Tech Stack:** .NET 10, C# 14, Avalonia 11, CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection, System.Text.Json, Porta.Pty for PTY sessions, xUnit, GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-09-06-codex-multiple-accounts-design.md`

## Global Constraints

- Support Windows, Linux, and macOS from one Avalonia codebase.
- Isolated sessions must use distinct profile-specific `CODEX_HOME` directories and never mutate process-wide `CODEX_HOME`.
- Global activation is explicit and must preserve a recoverable backup before replacing existing default Codex state.
- Codex-owned files are opaque; never parse, transform, or log authentication tokens.
- Windows embedded terminal follows the Dev Board PTY/native-host architecture; unsupported embedded backends fall back to external launch.
- Core tests run on Windows, Linux, and macOS CI.

---

## File Structure

```text
CodexMultipleAccounts.slnx
Directory.Build.props
README.md
src/
  CodexMultipleAccounts.Core/
    CodexMultipleAccounts.Core.csproj
    Profiles/CodexProfile.cs
    Profiles/ProfileCatalog.cs
    Profiles/ProfileStore.cs
    Profiles/ProfileService.cs
    Files/CodexPathService.cs
    Files/SafeDirectoryCopier.cs
    Activation/GlobalActivationService.cs
    Launching/CodexLaunchSpec.cs
    Launching/CodexLaunchService.cs
  CodexMultipleAccounts.App/
    CodexMultipleAccounts.App.csproj
    Program.cs
    App.axaml
    App.axaml.cs
    MainWindow.axaml
    MainWindow.axaml.cs
    ViewModels/MainWindowViewModel.cs
    ViewModels/ProfileViewModel.cs
    Terminal/ITerminalSession.cs
    Terminal/TerminalSessionFactory.cs
    Terminal/WindowsTerminalSession.cs
    Terminal/WindowsTerminalNativeHost.cs
    Terminal/ExternalTerminalLauncher.cs
    Terminal/ExternalTerminalCommandBuilder.cs
    Services/AppServices.cs
tests/
  CodexMultipleAccounts.Core.Tests/
    ProfileServiceTests.cs
    CodexLaunchServiceTests.cs
    GlobalActivationServiceTests.cs
    ParallelLaunchTests.cs
    SafeDirectoryCopierTests.cs
  CodexMultipleAccounts.App.Tests/
    ExternalTerminalCommandBuilderTests.cs
.github/workflows/ci.yml
```

## Task 1: Solution foundation and profile persistence

**Files:** create solution/build props, Core project, profile model/store/service, and `ProfileServiceTests.cs`.

**Interfaces:**
- Produces `record CodexProfile(Guid Id, string Name, string CodexHome, DateTimeOffset? LastUsedAt, bool IsGloballyActive)`.
- Produces `IProfileService` with `ListAsync`, `CreateAsync`, `RenameAsync`, `DeleteAsync`, and `ImportDefaultAsync`.
- `ProfileStore` persists metadata as JSON under the manager application-data directory.

- [ ] Write tests proving create produces a unique dedicated `codex-home`, rename preserves ID/home, delete cannot target the default Codex home, and import recursively copies opaque files.
- [ ] Run `dotnet test tests/CodexMultipleAccounts.Core.Tests/CodexMultipleAccounts.Core.Tests.csproj` and verify the tests fail because the production types do not exist.
- [ ] Implement the minimum profile/path/store/service code. Normalize and validate every recursive-copy/delete path; reject operations where a profile home equals or contains the default Codex home.
- [ ] Re-run the Core tests and require PASS.
- [ ] Commit as `feat: add isolated Codex profile storage`.

## Task 2: Child-only isolated launch specifications

**Files:** create `CodexLaunchSpec.cs`, `CodexLaunchService.cs`, and `CodexLaunchServiceTests.cs`.

**Interfaces:**
- Produces `CodexLaunchSpec(string Executable, IReadOnlyList<string> Arguments, string WorkingDirectory, IReadOnlyDictionary<string,string> Environment)`.
- Produces `ICodexLaunchService.Create(CodexProfile profile, string workingDirectory, IReadOnlyList<string>? arguments = null)`.

- [ ] Write a failing test asserting `Create` sets `Environment["CODEX_HOME"]` to the selected profile home, preserves arguments/working directory, and does not call `Environment.SetEnvironmentVariable`.
- [ ] Run the focused test and verify FAIL.
- [ ] Implement `CodexLaunchService` as a pure launch-spec builder with executable `codex` and a child-environment dictionary containing `CODEX_HOME`.
- [ ] Run the focused and full Core test suites and require PASS.
- [ ] Commit as `feat: build isolated Codex launch specs`.

## Task 3: Safe global activation and recovery

**Files:** create `SafeDirectoryCopier.cs`, `GlobalActivationService.cs`, `SafeDirectoryCopierTests.cs`, and `GlobalActivationServiceTests.cs`.

**Interfaces:**
- Produces `IGlobalActivationService.ActivateAsync(CodexProfile profile, CancellationToken cancellationToken = default)`.
- Activation stages the selected profile, moves the existing default home to a timestamped manager-owned backup, promotes staged contents, and restores the backup if promotion fails.

- [ ] Write failing tests for successful activation, existing-default backup creation, profile metadata active-state update, and injected copy/promotion failure restoring the original default state.
- [ ] Run the focused activation tests and verify FAIL.
- [ ] Implement guarded recursive copy plus staged directory promotion. Never write file contents to logs/exceptions beyond filesystem paths.
- [ ] Run all Core tests and require PASS.
- [ ] Commit as `feat: add recoverable global profile activation`.

## Task 4: Parallel process proof

**Files:** create `ParallelLaunchTests.cs`; extend launch abstractions only if required for testability.

**Interfaces:** consumes `CodexLaunchSpec` and verifies independent child environments.

- [ ] Add a tiny cross-platform test helper process mode that prints its `CODEX_HOME` and waits on stdin.
- [ ] Write an integration test launching two helpers concurrently from two specs and assert each reports a different expected home while the test runner's own `CODEX_HOME` is unchanged.
- [ ] Run the test and verify it fails before the process-runner implementation exists.
- [ ] Implement the minimum process runner needed by the test and app terminal launchers.
- [ ] Run all Core tests and require PASS.
- [ ] Commit as `test: prove parallel isolated Codex homes`.

## Task 5: Cross-platform external terminal launching

**Files:** create App project foundation, `ExternalTerminalCommandBuilder.cs`, `ExternalTerminalLauncher.cs`, and `ExternalTerminalCommandBuilderTests.cs`.

**Interfaces:**
- Produces `IExternalTerminalLauncher.LaunchAsync(CodexLaunchSpec spec, CancellationToken cancellationToken = default)`.
- Command builder selects Windows Terminal / `cmd` fallback on Windows, Terminal/open shell on macOS, and common terminal emulators (`x-terminal-emulator`, `gnome-terminal`, `konsole`) on Linux.

- [ ] Write table-driven failing tests for Windows, macOS, and Linux command construction, including paths containing spaces and child-only `CODEX_HOME` propagation.
- [ ] Run App tests and verify FAIL.
- [ ] Implement OS-specific command construction without changing the parent process environment.
- [ ] Run App tests and require PASS.
- [ ] Commit as `feat: launch isolated Codex in external terminals`.

## Task 6: Avalonia profile UI and MVVM commands

**Files:** create `Program.cs`, `App.axaml(.cs)`, `MainWindow.axaml(.cs)`, `MainWindowViewModel.cs`, `ProfileViewModel.cs`, and `AppServices.cs`.

**Interfaces:**
- `MainWindowViewModel` exposes profiles, selected profile, working directory, status text, and commands: Create, ImportDefault, Rename, Delete, Launch, OpenExternally, ActivateGlobally.
- Uses CommunityToolkit.Mvvm commands and DI-provided Core services.

- [ ] Add ViewModel tests for command enablement: no profile disables launch/activation; selecting a profile enables them; global activation status is surfaced distinctly from isolated launch status.
- [ ] Run App tests and verify FAIL.
- [ ] Implement the minimal ViewModels and service wiring.
- [ ] Build the Avalonia main window with a profile sidebar, clear `Launch`, `Open externally`, and `Activate globally` actions, and a globally-active badge. Add the terminal tab host area required by Task 7.
- [ ] Run tests plus `dotnet build CodexMultipleAccounts.slnx` and require PASS.
- [ ] Commit as `feat: add Avalonia multiple-account dashboard`.

## Task 7: Embedded terminal sessions with Dev Board-style Windows backend

**Files:** create `ITerminalSession.cs`, `TerminalSessionFactory.cs`, `WindowsTerminalSession.cs`, `WindowsTerminalNativeHost.cs`; update MainWindow/ViewModel integration.

**Interfaces:**
- `ITerminalSession` exposes `Control View`, `string BackendName`, `Task StartAsync(CodexLaunchSpec spec)`, `void Stop()`, and `event EventHandler<TerminalExitedEventArgs> Exited`.
- `TerminalSessionFactory` returns the Windows embedded implementation when supported and reports unavailable elsewhere so the UI can offer external launch.

- [ ] Write tests around a fake `ITerminalSession` proving each Launch command creates a separate session/tab with its own spec and closing one session does not stop another.
- [ ] Run tests and verify FAIL.
- [ ] Implement Windows PTY lifecycle following the referenced Dev Board pattern: `NativeControlHost`, `Porta.Pty.PtyProvider.SpawnAsync`, UTF-8 reader/writer channels, resize forwarding, idempotent stop, and exit propagation. Pass every `LaunchSpec.Environment` entry into the PTY child environment.
- [ ] Integrate tabs into the Avalonia window; on unsupported platforms or embedded-start failure, leave the profile untouched and expose `Open externally`.
- [ ] Run App tests and Windows build; require PASS.
- [ ] Commit as `feat: add embedded isolated Codex terminal sessions`.

## Task 8: Cross-platform CI, documentation, and final verification

**Files:** create `.github/workflows/ci.yml`; update `README.md`.

**Interfaces:** CI matrix is `windows-latest`, `ubuntu-latest`, `macos-latest` using .NET 10.

- [ ] Add CI jobs that restore, build, and test the solution on all three operating systems. Keep Windows-specific embedded-terminal code behind compile/runtime guards so Linux/macOS builds succeed.
- [ ] Document profile storage, isolated vs global mode, parallel launch behavior, external-terminal fallback, supported platforms, build/run commands, and the security warning that profile homes contain Codex credentials.
- [ ] Run `dotnet test CodexMultipleAccounts.slnx` and `dotnet build CodexMultipleAccounts.slnx -c Release` locally/in the available execution environment and require PASS.
- [ ] Push the implementation branch and inspect the GitHub Actions matrix; fix failures until all supported matrix jobs are green.
- [ ] Commit final docs/CI changes as `ci: verify Codex multiple accounts cross-platform`.

## Final verification checklist

- [ ] Create/import two profiles in tests.
- [ ] Prove two simultaneous child processes receive distinct `CODEX_HOME` values.
- [ ] Prove parent/global environment is unchanged by isolated launches.
- [ ] Prove global activation creates a recoverable backup and rollback works on failure.
- [ ] Prove profile delete/path guards cannot delete the default Codex home.
- [ ] Build Avalonia app on Windows, Linux, and macOS CI.
- [ ] Run all unit/integration tests green.
- [ ] Inspect logs/tests to ensure no auth file contents are emitted.
- [ ] Review diff against the approved design spec before opening the implementation PR.
