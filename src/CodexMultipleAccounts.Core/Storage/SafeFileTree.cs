namespace CodexMultipleAccounts.Core.Storage;

public static class SafeFileTree
{
    public static async Task CopyDirectoryAsync(
        string source,
        string destination,
        CancellationToken cancellationToken = default)
    {
        var sourceFull = Path.GetFullPath(source);
        var destinationFull = Path.GetFullPath(destination);
        if (!Directory.Exists(sourceFull))
            throw new DirectoryNotFoundException($"Source directory '{sourceFull}' does not exist.");

        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(destinationFull);

        foreach (var directory in Directory.EnumerateDirectories(sourceFull, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(sourceFull, directory);
            Directory.CreateDirectory(Path.Combine(destinationFull, relative));
        }

        foreach (var file in Directory.EnumerateFiles(sourceFull, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(sourceFull, file);
            var target = Path.Combine(destinationFull, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);

            await using var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            await using var output = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await input.CopyToAsync(output, cancellationToken);
        }
    }

    public static Task DeleteManagedDirectoryAsync(
        string path,
        string allowedRoot,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureDescendant(path, allowedRoot);
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
        return Task.CompletedTask;
    }

    public static void EnsureDescendant(string path, string allowedRoot)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(allowedRoot));
        var target = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!target.StartsWith(root + Path.DirectorySeparatorChar, comparison))
            throw new InvalidOperationException($"Path '{target}' is outside the allowed root '{root}'.");
    }
}
