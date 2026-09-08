namespace CodexMultipleAccounts.Core.Launching;

public static class ExecutableResolver
{
    public static string Resolve(string executable, bool allowMissingDefault = false)
    {
        if (string.IsNullOrWhiteSpace(executable))
            throw new ArgumentException("An executable is required.", nameof(executable));

        executable = executable.Trim().Trim('"');
        if (Path.IsPathRooted(executable) || executable.Contains('/') || executable.Contains('\\'))
        {
            var full = Path.GetFullPath(executable);
            if (!File.Exists(full))
                throw new FileNotFoundException($"Executable not found: {full}. Choose an installed CLI executable.", full);
            return full;
        }

        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var extensions = OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD").Split(';', StringSplitOptions.RemoveEmptyEntries)
            : [string.Empty];
        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var extension in extensions)
            {
                var candidate = Path.Combine(directory, executable);
                if (File.Exists(candidate)) return candidate;
                if (OperatingSystem.IsWindows() && !Path.HasExtension(executable) && File.Exists(candidate + extension))
                    return candidate + extension;
            }
        }

        if (allowMissingDefault) return executable;
        throw new FileNotFoundException($"The command '{executable}' was not found on PATH. Install the CLI or select its executable.");
    }
}
