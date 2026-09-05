namespace CodexMultipleAccounts.Core.Launching;

public enum TerminalPlatform
{
    Windows,
    Linux,
    MacOS
}

public sealed record ExternalTerminalCommand(
    string FileName,
    IReadOnlyList<string> Arguments);

public static class ExternalTerminalCommandBuilder
{
    public static ExternalTerminalCommand Build(LaunchSpec spec, TerminalPlatform platform)
    {
        ArgumentNullException.ThrowIfNull(spec);
        if (!spec.Environment.TryGetValue("CODEX_HOME", out var codexHome) || string.IsNullOrWhiteSpace(codexHome))
            throw new ArgumentException("Launch spec must include CODEX_HOME.", nameof(spec));

        return platform switch
        {
            TerminalPlatform.Windows => BuildWindows(spec, codexHome),
            TerminalPlatform.Linux => BuildLinux(spec, codexHome),
            TerminalPlatform.MacOS => BuildMac(spec, codexHome),
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
        };
    }

    public static TerminalPlatform CurrentPlatform() =>
        OperatingSystem.IsWindows() ? TerminalPlatform.Windows :
        OperatingSystem.IsMacOS() ? TerminalPlatform.MacOS :
        OperatingSystem.IsLinux() ? TerminalPlatform.Linux :
        throw new PlatformNotSupportedException("No supported desktop terminal platform was detected.");

    private static ExternalTerminalCommand BuildWindows(LaunchSpec spec, string codexHome)
    {
        var command = $"set \"CODEX_HOME={EscapeCmdValue(codexHome)}\" && {QuoteCmd(spec.FileName)}";
        foreach (var argument in spec.Arguments)
            command += " " + QuoteCmd(argument);

        return new ExternalTerminalCommand(
            "wt.exe",
            ["-d", spec.WorkingDirectory, "cmd.exe", "/k", command]);
    }

    private static ExternalTerminalCommand BuildLinux(LaunchSpec spec, string codexHome)
    {
        var args = new List<string>
        {
            "-e",
            "env",
            $"CODEX_HOME={codexHome}",
            spec.FileName
        };
        args.AddRange(spec.Arguments);
        return new ExternalTerminalCommand("x-terminal-emulator", args);
    }

    private static ExternalTerminalCommand BuildMac(LaunchSpec spec, string codexHome)
    {
        var shell = $"cd {QuotePosix(spec.WorkingDirectory)} && CODEX_HOME={QuotePosix(codexHome)} {QuotePosix(spec.FileName)}";
        foreach (var argument in spec.Arguments)
            shell += " " + QuotePosix(argument);

        var appleScript = $"tell application \"Terminal\" to do script {QuoteAppleScript(shell)}";
        return new ExternalTerminalCommand("osascript", ["-e", appleScript]);
    }

    private static string EscapeCmdValue(string value) => value.Replace("%", "%%", StringComparison.Ordinal).Replace("\"", "\"\"", StringComparison.Ordinal);

    private static string QuoteCmd(string value) => "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";

    private static string QuotePosix(string value) => "'" + value.Replace("'", "'\\''", StringComparison.Ordinal) + "'";

    private static string QuoteAppleScript(string value) => "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
}
