# Functional Navigation Design

Date: 2026-09-08

## Goal

Replace the five disabled sidebar placeholders with useful pages while preserving the existing Accounts dashboard, isolated Codex sessions, and Antigravity launch behavior.

## Architecture

Use a single observable navigation selection on MainWindowViewModel and keep the existing Accounts/terminal workspace mounted. A page host displays Sessions, Usage, Settings, Documentation, or About in the same main window. Views have focused view models and share the existing profile/session services rather than creating duplicate managers. Navigation must not terminate processes, change global credentials, or reconstruct terminal models. The currently selected page has a visible accessible state, and all six sidebar buttons are enabled.

## Navigation and layout

Accounts remains the default page. Selecting Sessions opens the session management page; selecting a session returns to Accounts and activates its existing terminal. The other pages occupy the available workspace width, with vertical scrolling and no permanently reserved account column. The existing responsive sidebar, account overlay, and terminal-first breakpoints remain intact. On non-Accounts pages the account overlay is closed. Restore the previous Accounts layout when returning. Respect the current application theme and use existing dynamic theme resources. The first window remains compact and centered, constrained to the display work area; do not impose a desktop-sized minimum on a smaller screen.

## Sessions

A session has a stable identity, profile name, working directory, start time, running/exited status, and an explicit lifecycle. The PTY connection is owned by that session, not a global process lookup. Stop requests terminate only the selected session's child process tree and are idempotent. Closing a running session requests confirmation, stops that child, waits for completion, and then removes its tab. Closing an exited session removes only the UI record. A session cannot continue writing into a disposed terminal model. Repeated exit notifications must not overwrite the first exit state or append duplicate messages. Switching pages never stops or recreates sessions. The app's normal shutdown stops its owned embedded sessions safely. External terminals are not silently killed by the manager.

## Settings

Persist non-secret application preferences to a separate settings.json in the manager's application-data root. Fields are DefaultWorkspace, CodexExecutable, AntigravityExecutable, and Appearance (System, Light, Dark). Defaults: user's home directory as workspace, `codex` as Codex executable, automatic Antigravity executable resolution, and System appearance. Preserve the existing ANTIGRAVITY_EXECUTABLE environment override when no explicit preference is configured. Resolve existing executable paths and reject invalid paths with actionable messages; support PATH command names where appropriate. Workspace must exist and be a directory. Browse uses the platform folder picker. Save validates all fields and writes through a temporary file followed by an atomic replacement. Reset restores defaults in the form without silently changing profile directories. Changing workspace applies to future launches only; existing sessions retain their original working directories. Changing executable settings applies to future launches only. Appearance changes apply immediately after a successful save and persist across restarts. Settings do not contain passwords, tokens, or credential contents.

## Usage

The page displays provider/profile names and quota status using an explicit unavailable state. Never interpret an absent quota as zero or show screenshot-demo percentages as actual usage. Do not parse OAuth credentials or call undocumented authenticated endpoints. If a supported public/official usage source is integrated later, use a separate adapter and display source, retrieval time, reset time, and failures. This slice does not invent a quota integration.

## Documentation and About

Documentation provides local guidance for installing Codex/Antigravity, creating/importing profiles, choosing a workspace, isolated versus global activation, and terminal troubleshooting. Link to the official CLI documentation and application repository. Explain Antigravity filesystem isolation and shared OS credential-store limitations, and state that Shared mode is not a guarantee of independent authentication. About reads version from assembly metadata, displays OS/runtime information, and exposes verified repository and license links. If license metadata is unavailable, report that rather than claiming a license.

## Safety and error handling

Preserve child-only CODEX_HOME and existing profile catalog format. No automatic global activation, credential inspection, or destructive profile migration. Catch and report expected process launch, file I/O, validation, and settings errors without crashing the UI. Do not swallow cancellation or label failed launches as successful. Destructive session actions require an explicit user gesture and must be scoped to the selected session.

## Verification

Write failing tests before implementation for navigation, persistence and validation, child launch specs, lifecycle idempotency and isolation, usage unavailable states, and version metadata. Test page selection and binding behavior, retained terminal instances, and layout at 760, 900, 1000, 1200, and 1600 widths. Run the complete .NET 10 solution build/tests on Windows, Linux, and macOS in GitHub Actions. Verify screenshot mode renders all navigation destinations without using live credentials. Use a feature branch and create a reviewable PR; do not merge before successful checks and review. Document any unsupported GUI/native-terminal checks explicitly.
