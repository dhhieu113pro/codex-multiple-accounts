namespace CodexMultipleAccounts.App.Tests;

public sealed class BrandingAssetTests
{
    [Fact]
    public void Project_configures_generated_app_icon_and_logo_resource()
    {
        var root = FindRepositoryRoot();
        var project = File.ReadAllText(Path.Combine(
            root,
            "src",
            "CodexMultipleAccounts.App",
            "CodexMultipleAccounts.App.csproj"));

        Assert.Contains("<ApplicationIcon>Assets\\CodexMultipleAccounts.ico</ApplicationIcon>", project);
        Assert.Contains("<AvaloniaResource Include=\"Assets\\CodexMultipleAccountsLogo.png\" />", project);
    }

    [Fact]
    public void Main_window_uses_brand_assets_for_native_icon_and_sidebar_logo()
    {
        var root = FindRepositoryRoot();
        var codeBehind = File.ReadAllText(Path.Combine(
            root,
            "src",
            "CodexMultipleAccounts.App",
            "MainWindow.axaml.cs"));

        Assert.Contains("BrandAssets.CreateWindowIcon()", codeBehind);
        Assert.Contains("BrandAssets.CreateBitmap()", codeBehind);
    }

    [Fact]
    public void Readme_displays_the_same_product_logo()
    {
        var root = FindRepositoryRoot();
        var readme = File.ReadAllText(Path.Combine(root, "README.md"));

        Assert.Contains("docs/assets/codex-multiple-accounts-logo.png", readme);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CodexMultipleAccounts.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from the test output directory.");
    }
}
