using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace CodexMultipleAccounts.App.Branding;

public static class BrandAssets
{
    private static readonly Uri WindowIconUri =
        new("avares://CodexMultipleAccounts.App/!__AvaloniaDefaultWindowIcon");
    private static readonly Uri LogoUri =
        new("avares://CodexMultipleAccounts.App/Assets/CodexMultipleAccountsLogo.png");

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
