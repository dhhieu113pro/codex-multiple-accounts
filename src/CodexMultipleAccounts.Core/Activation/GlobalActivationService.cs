using CodexMultipleAccounts.Core.Profiles;
using CodexMultipleAccounts.Core.Storage;

namespace CodexMultipleAccounts.Core.Activation;

public sealed class GlobalActivationService(ProfileService? profiles = null)
{
    private readonly ProfileService? _profiles = profiles;

    public async Task<GlobalActivationResult> ActivateAsync(
        CodexProfile profile,
        string defaultCodexHome,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultCodexHome);
        if (!Directory.Exists(profile.CodexHome))
            throw new DirectoryNotFoundException($"Profile Codex home '{profile.CodexHome}' does not exist.");

        var defaultFull = Path.GetFullPath(defaultCodexHome);
        var parent = Path.GetDirectoryName(defaultFull)
            ?? throw new InvalidOperationException("Default Codex home must have a parent directory.");
        Directory.CreateDirectory(parent);

        var suffix = Guid.NewGuid().ToString("N");
        var staging = defaultFull + ".cma-staging-" + suffix;
        var backup = defaultFull + ".cma-backup-" + suffix;
        var hadPrevious = Directory.Exists(defaultFull);
        var promoted = false;

        await SafeFileTree.CopyDirectoryAsync(profile.CodexHome, staging, cancellationToken);

        try
        {
            if (hadPrevious)
                Directory.Move(defaultFull, backup);

            Directory.Move(staging, defaultFull);
            promoted = true;

            if (_profiles is not null)
                await _profiles.SetGloballyActiveAsync(profile.Id, cancellationToken);
        }
        catch
        {
            if (Directory.Exists(staging))
                await SafeFileTree.DeleteManagedDirectoryAsync(staging, parent, CancellationToken.None);

            if (promoted && Directory.Exists(defaultFull))
                await SafeFileTree.DeleteManagedDirectoryAsync(defaultFull, parent, CancellationToken.None);

            if (hadPrevious && Directory.Exists(backup) && !Directory.Exists(defaultFull))
                Directory.Move(backup, defaultFull);

            throw;
        }

        return new GlobalActivationResult(hadPrevious, hadPrevious ? backup : null);
    }

    public async Task RollbackActivationAsync(
        string defaultCodexHome,
        GlobalActivationResult activation,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultCodexHome);
        ArgumentNullException.ThrowIfNull(activation);

        if (activation.HadPreviousDefault)
        {
            if (string.IsNullOrWhiteSpace(activation.BackupDirectory))
                throw new InvalidOperationException("Activation reports a previous default home but no backup directory.");

            await RestoreBackupAsync(defaultCodexHome, activation.BackupDirectory, cancellationToken);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var defaultFull = Path.GetFullPath(defaultCodexHome);
        var parent = Path.GetDirectoryName(defaultFull)
            ?? throw new InvalidOperationException("Default Codex home must have a parent directory.");
        if (Directory.Exists(defaultFull))
            await SafeFileTree.DeleteManagedDirectoryAsync(defaultFull, parent, cancellationToken);
    }

    public async Task RestoreBackupAsync(
        string defaultCodexHome,
        string backupDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultCodexHome);
        ArgumentException.ThrowIfNullOrWhiteSpace(backupDirectory);
        cancellationToken.ThrowIfCancellationRequested();

        var defaultFull = Path.GetFullPath(defaultCodexHome);
        var backupFull = Path.GetFullPath(backupDirectory);
        var parent = Path.GetDirectoryName(defaultFull)
            ?? throw new InvalidOperationException("Default Codex home must have a parent directory.");
        SafeFileTree.EnsureDescendant(backupFull, parent);
        if (!Directory.Exists(backupFull))
            throw new DirectoryNotFoundException($"Backup directory '{backupFull}' does not exist.");

        var displaced = defaultFull + ".cma-displaced-" + Guid.NewGuid().ToString("N");
        if (Directory.Exists(defaultFull))
            Directory.Move(defaultFull, displaced);

        try
        {
            Directory.Move(backupFull, defaultFull);
            if (Directory.Exists(displaced))
                await SafeFileTree.DeleteManagedDirectoryAsync(displaced, parent, cancellationToken);
        }
        catch
        {
            if (!Directory.Exists(defaultFull) && Directory.Exists(displaced))
                Directory.Move(displaced, defaultFull);
            throw;
        }
    }
}
