using CodexMultipleAccounts.Core.Profiles;
using CodexMultipleAccounts.Core.Storage;
using Xunit;

namespace CodexMultipleAccounts.Core.Tests;

public sealed class SafeFileTreeTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "cma-storage-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task CopyDirectoryAsync_copies_opaque_nested_files_without_parsing_auth()
    {
        var source = Path.Combine(_root, "source");
        var destination = Path.Combine(_root, "destination");
        Directory.CreateDirectory(Path.Combine(source, "sessions"));
        const string auth = "{\"tokens\":{\"access_token\":\"opaque-secret-value\"}}";
        await File.WriteAllTextAsync(Path.Combine(source, "auth.json"), auth);
        await File.WriteAllBytesAsync(Path.Combine(source, "sessions", "raw.bin"), [0, 1, 2, 255]);

        await SafeFileTree.CopyDirectoryAsync(source, destination);

        Assert.Equal(auth, await File.ReadAllTextAsync(Path.Combine(destination, "auth.json")));
        Assert.Equal([0, 1, 2, 255], await File.ReadAllBytesAsync(Path.Combine(destination, "sessions", "raw.bin")));
    }

    [Fact]
    public async Task DeleteManagedDirectoryAsync_rejects_path_outside_allowed_root()
    {
        var allowed = Path.Combine(_root, "managed");
        var outside = Path.Combine(_root, "outside");
        Directory.CreateDirectory(outside);
        await File.WriteAllTextAsync(Path.Combine(outside, "keep.txt"), "keep");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SafeFileTree.DeleteManagedDirectoryAsync(outside, allowed));

        Assert.True(File.Exists(Path.Combine(outside, "keep.txt")));
    }

    [Fact]
    public async Task ImportDefaultAsync_copies_existing_codex_home_into_new_profile()
    {
        var defaultHome = Path.Combine(_root, "default-codex");
        Directory.CreateDirectory(defaultHome);
        await File.WriteAllTextAsync(Path.Combine(defaultHome, "auth.json"), "opaque-auth");
        var paths = new AppPaths(Path.Combine(_root, "app"));
        var service = new ProfileService(paths, new ProfileStore(paths));

        var profile = await service.ImportDefaultAsync("Imported", defaultHome);

        Assert.Equal("opaque-auth", await File.ReadAllTextAsync(Path.Combine(profile.CodexHome, "auth.json")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
