# App Logo Branding Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Apply the generated Codex Multiple Accounts icon everywhere the desktop app and repository expose product branding, including a Windows multi-size ICO using the same Avalonia pattern as ButChi.

**Architecture:** Add canonical PNG/ICO files under the Avalonia app `Assets` folder, configure the project `ApplicationIcon`, add a small `BrandAssets` helper mirroring ButChi, use it for the native window icon, and replace the temporary sidebar `C` badge with the real bitmap. Add repository-level branding coverage so future changes cannot silently drop the icon wiring.

**Tech Stack:** .NET 10, Avalonia 12, xUnit, GitHub Actions.

**Spec:** Approved generated icon from the current conversation and the ButChi branding pattern (`ApplicationIcon` + AvaloniaResource + `BrandAssets`).

## Global Constraints

- Preserve the current adaptive sidebar behavior and breakpoints.
- Preserve dark/light theme behavior.
- Do not change account/session commands or data flow.
- Use `Assets/CodexMultipleAccounts.ico` for the executable icon.
- Use `Assets/CodexMultipleAccountsLogo.png` as the in-app/README canonical PNG.
- Keep the generated icon artwork unchanged.

---

### Task 1: Add branding regression coverage

**Files:**
- Create: `tests/CodexMultipleAccounts.App.Tests/BrandingAssetTests.cs`

**Interfaces:**
- Consumes: repository files under `src/CodexMultipleAccounts.App` and `README.md`.
- Produces: tests that fail until project, window, sidebar, and README branding are wired.

- [ ] **Step 1: Write the failing tests**

Create xUnit tests that locate the repository root and assert:

```csharp
Assert.Contains("<ApplicationIcon>Assets\\CodexMultipleAccounts.ico</ApplicationIcon>", project);
Assert.Contains("<AvaloniaResource Include=\"Assets\\CodexMultipleAccountsLogo.png\" />", project);
Assert.Contains("BrandAssets.CreateWindowIcon()", mainWindowCodeBehind);
Assert.Contains("avares://CodexMultipleAccounts.App/Assets/CodexMultipleAccountsLogo.png", mainWindowXaml);
Assert.Contains("docs/assets/codex-multiple-accounts-logo.png", readme);
```

- [ ] **Step 2: Run the app test project and verify RED**

Run: `dotnet test tests/CodexMultipleAccounts.App.Tests/CodexMultipleAccounts.App.Tests.csproj -c Release`

Expected: branding assertions fail because the current project still has the placeholder `C` badge and no icon resources.

### Task 2: Add native and Avalonia branding assets

**Files:**
- Create: `src/CodexMultipleAccounts.App/Assets/CodexMultipleAccountsLogo.png`
- Create: `src/CodexMultipleAccounts.App/Assets/CodexMultipleAccounts.ico`
- Create: `src/CodexMultipleAccounts.App/Branding/BrandAssets.cs`
- Modify: `src/CodexMultipleAccounts.App/CodexMultipleAccounts.App.csproj`

**Interfaces:**
- Produces: `BrandAssets.CreateWindowIcon()` and `BrandAssets.CreateBitmap()`.

- [ ] **Step 1: Add the generated binary assets**

Use the approved icon-only transparent PNG and the generated multi-size ICO containing 16, 24, 32, 48, 64, 128, and 256 pixel variants.

- [ ] **Step 2: Configure project resources**

Add:

```xml
<ApplicationIcon>Assets\CodexMultipleAccounts.ico</ApplicationIcon>
```

and:

```xml
<AvaloniaResource Include="Assets\CodexMultipleAccountsLogo.png" />
```

- [ ] **Step 3: Add the ButChi-style branding helper**

```csharp
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace CodexMultipleAccounts.App.Branding;

public static class BrandAssets
{
    private static readonly Uri WindowIconUri = new("avares://CodexMultipleAccounts.App/!__AvaloniaDefaultWindowIcon");
    private static readonly Uri LogoUri = new("avares://CodexMultipleAccounts.App/Assets/CodexMultipleAccountsLogo.png");

    public static WindowIcon CreateWindowIcon()
    {
        using var stream = AssetLoader.Open(WindowIconUri);
        return new WindowIcon(stream);
    }

    public static Bitmap CreateBitmap()
    {
        using var stream = AssetLoader.Open(LogoUri);
        return new Bitmap(stream);
    }
}
```

### Task 3: Apply the icon to the actual app UI

**Files:**
- Modify: `src/CodexMultipleAccounts.App/MainWindow.axaml`
- Modify: `src/CodexMultipleAccounts.App/MainWindow.axaml.cs`

**Interfaces:**
- Consumes: `BrandAssets` and the PNG Avalonia resource.

- [ ] **Step 1: Wire the native window icon**

Add `using CodexMultipleAccounts.App.Branding;` and set:

```csharp
Icon = BrandAssets.CreateWindowIcon();
```

immediately after `InitializeComponent()`.

- [ ] **Step 2: Replace the temporary sidebar badge**

Replace the purple `C` border/text block with a 30x30 `Image` using:

```xml
<Image Source="avares://CodexMultipleAccounts.App/Assets/CodexMultipleAccountsLogo.png"
       Width="30"
       Height="30"
       Stretch="Uniform"/>
```

Keep existing adaptive brand-text visibility unchanged.

### Task 4: Apply repository branding and verify GREEN

**Files:**
- Create: `docs/assets/codex-multiple-accounts-logo.png`
- Modify: `README.md`

**Interfaces:**
- Produces: repository-facing logo that matches the app binary.

- [ ] **Step 1: Add the canonical README logo**

Copy the same approved PNG to `docs/assets/codex-multiple-accounts-logo.png`.

- [ ] **Step 2: Add the centered logo above the README title/description**

Use a compact centered image reference so GitHub displays the same brand mark as the app.

- [ ] **Step 3: Run branding tests**

Run: `dotnet test tests/CodexMultipleAccounts.App.Tests/CodexMultipleAccounts.App.Tests.csproj -c Release`

Expected: PASS.

- [ ] **Step 4: Run full verification**

Run:

```bash
dotnet restore CodexMultipleAccounts.slnx
dotnet build CodexMultipleAccounts.slnx -c Release --no-restore
dotnet test CodexMultipleAccounts.slnx -c Release --no-build
```

Expected: all commands exit 0.

- [ ] **Step 5: Run the App Screenshot workflow**

Verify the real Avalonia app launches and the captured screenshot shows the generated icon in the adaptive sidebar.
