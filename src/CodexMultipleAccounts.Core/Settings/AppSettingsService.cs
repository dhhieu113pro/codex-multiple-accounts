using System.Text.Json;
using CodexMultipleAccounts.Core.Launching;

namespace CodexMultipleAccounts.Core.Settings;

public sealed class AppSettingsService
{
    private readonly string _root;
    private readonly string _file;

    public AppSettingsService(string root)
    {
        _root = Path.GetFullPath(root);
        _file = Path.Combine(_root, "settings.json");
    }

    public AppSettings Defaults() => new(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_file)) return Defaults();
        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(await File.ReadAllTextAsync(_file, cancellationToken));
            return settings ?? throw new InvalidDataException("Settings file contains no settings.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Settings file is invalid. Correct settings.json or restore a backup; no existing profiles have been changed.", ex);
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var validated = Validate(settings);
        Directory.CreateDirectory(_root);
        var temporary = _file + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(validated, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);
            File.Move(temporary, _file, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public static AppSettings Validate(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (string.IsNullOrWhiteSpace(settings.DefaultWorkspace))
            throw new ArgumentException("Choose a workspace directory.", nameof(settings));
        var workspace = Path.GetFullPath(Environment.ExpandEnvironmentVariables(settings.DefaultWorkspace.Trim()));
        if (!Directory.Exists(workspace))
            throw new DirectoryNotFoundException($"Workspace directory does not exist: {workspace}");
        if (!Enum.IsDefined(settings.Appearance))
            throw new ArgumentOutOfRangeException(nameof(settings), "Choose System, Light, or Dark appearance.");
        var codex = ExecutableResolver.Resolve(settings.CodexExecutable, allowMissingDefault: settings.CodexExecutable == "codex");
        var antigravity = string.IsNullOrWhiteSpace(settings.AntigravityExecutable)
            ? string.Empty
            : ExecutableResolver.Resolve(settings.AntigravityExecutable);
        return settings with { DefaultWorkspace = workspace, CodexExecutable = codex, AntigravityExecutable = antigravity };
    }
}
