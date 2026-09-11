using System.Diagnostics;
using CodexMultipleAccounts.Core.Launching;
using CodexMultipleAccounts.Core.Profiles;

namespace CodexMultipleAccounts.Dnx;

public static class Program
{
    private static readonly string Root = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CodexMultipleAccounts");

    public static async Task<int> Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || IsHelp(args[0]))
            {
                PrintHelp();
                return 0;
            }

            var profiles = new ProfileService(
                Environment.GetEnvironmentVariable("CODEX_MULTIPLE_ACCOUNTS_ROOT") ?? Root,
                Environment.GetEnvironmentVariable("CODEX_HOME") ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex"));

            return args[0].ToLowerInvariant() switch
            {
                "profile" => await ProfileCommandAsync(profiles, args[1..]),
                "login" => await LaunchCommandAsync(profiles, args[1..], login: true),
                "launch" => await LaunchCommandAsync(profiles, args[1..], login: false),
                _ => UsageError($"Unknown command '{args[0]}'.")
            };
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException or DirectoryNotFoundException)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> ProfileCommandAsync(ProfileService profiles, string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            Console.WriteLine("Usage: profile <list|create|import|rename|delete> ...");
            return 0;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "list":
                var providerOption = GetOption(args[1..], "--provider");
                var providerFilter = providerOption is null ? null : ParseProvider(providerOption).ToString();
                foreach (var profile in await profiles.ListAsync())
                {
                    if (providerFilter is not null && !string.Equals(profile.Provider.ToString(), providerFilter, StringComparison.OrdinalIgnoreCase))
                        continue;
                    Console.WriteLine($"{profile.Id}\t{profile.Name}\t{profile.Provider}\t{profile.CodexHome}");
                }
                return 0;

            case "create":
                var createProvider = ParseProvider(GetOption(args[1..], "--provider") ?? "codex");
                var createName = ValuesWithoutOption(args[1..], "--provider");
                Require(createName, "profile create <name> [--provider codex|antigravity]");
                var created = createProvider == AccountProvider.Antigravity
                    ? await new AntigravityProfileService(profiles).CreateAsync(string.Join(' ', createName))
                    : await profiles.CreateAsync(string.Join(' ', createName));
                Console.WriteLine($"Created {created.Name} ({created.Id}).");
                return 0;

            case "import":
                var importProvider = ParseProvider(GetOption(args[1..], "--provider") ?? "codex");
                if (importProvider != AccountProvider.Codex)
                    throw new ArgumentException("Only Codex profiles can import the default Codex home.");
                var importName = ValuesWithoutOption(args[1..], "--provider");
                Require(importName, "profile import <name> [--provider codex]");
                var imported = await profiles.ImportDefaultAsync(string.Join(' ', importName));
                Console.WriteLine($"Imported default Codex home into {imported.Name} ({imported.Id}).");
                return 0;

            case "rename":
                Require(args[1..], "profile rename <id|name> <new-name>");
                var renameTarget = await FindProfileAsync(profiles, args[1]);
                await profiles.RenameAsync(renameTarget.Id, string.Join(' ', args[2..]));
                Console.WriteLine($"Renamed {renameTarget.Name}.");
                return 0;

            case "delete":
                Require(args[1..], "profile delete <id|name>");
                var deleteTarget = await FindProfileAsync(profiles, args[1]);
                await profiles.DeleteAsync(deleteTarget.Id);
                Console.WriteLine($"Deleted {deleteTarget.Name}.");
                return 0;

            default:
                return UsageError($"Unknown profile command '{args[0]}'.");
        }
    }

    private static async Task<int> LaunchCommandAsync(ProfileService profiles, string[] args, bool login)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            Console.WriteLine(login
                ? "Usage: login <profile-id|name>"
                : "Usage: launch <profile-id|name> [workspace] [-- codex-arguments]");
            return 0;
        }

        var profile = await FindProfileAsync(profiles, args[0]);
        var separator = Array.IndexOf(args, "--");
        var workspace = separator > 0 && separator > 1 ? args[1] : (separator < 0 && args.Length > 1 ? args[1] : Environment.CurrentDirectory);
        workspace = Path.GetFullPath(workspace);
        if (!Directory.Exists(workspace))
            throw new DirectoryNotFoundException($"Workspace directory does not exist: {workspace}");

        var codexArguments = login ? new[] { "login" } : separator >= 0 ? args[(separator + 1)..] : [];
        var spec = new CodexLaunchService().Create(profile, workspace, codexArguments);
        var startInfo = new ProcessStartInfo(spec.Executable)
        {
            WorkingDirectory = spec.WorkingDirectory,
            UseShellExecute = false
        };
        foreach (var argument in spec.Arguments)
            startInfo.ArgumentList.Add(argument);
        foreach (var variable in spec.Environment)
            startInfo.Environment[variable.Key] = variable.Value;

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start Codex.");
        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    private static async Task<CodexProfile> FindProfileAsync(ProfileService profiles, string value)
    {
        var all = await profiles.ListAsync();
        if (Guid.TryParse(value, out var id))
            return all.SingleOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException($"Profile not found: {value}");
        return all.SingleOrDefault(x => string.Equals(x.Name, value, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Profile not found: {value}");
    }

    private static void Require(string[] args, string usage)
    {
        if (args.Length == 0 || args.All(string.IsNullOrWhiteSpace))
            throw new ArgumentException($"Usage: {usage}");
    }

    private static string? GetOption(string[] args, string option)
    {
        var index = Array.FindIndex(args, value => string.Equals(value, option, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            return null;
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            throw new ArgumentException($"Option {option} requires a value.");
        return args[index + 1];
    }

    private static string[] ValuesWithoutOption(string[] args, string option)
    {
        var values = new List<string>();
        for (var index = 0; index < args.Length; index++)
        {
            if (string.Equals(args[index], option, StringComparison.OrdinalIgnoreCase))
            {
                index++;
                continue;
            }
            values.Add(args[index]);
        }
        return values.ToArray();
    }

    private static AccountProvider ParseProvider(string value) => value.ToLowerInvariant() switch
    {
        "codex" => AccountProvider.Codex,
        "antigravity" => AccountProvider.Antigravity,
        _ => throw new ArgumentException($"Unknown provider '{value}'. Supported providers: codex, antigravity.")
    };

    private static bool IsHelp(string value) => value is "-h" or "--help" or "help";

    private static int UsageError(string message)
    {
        Console.Error.WriteLine(message);
        PrintHelp();
        return 2;
    }

    private static void PrintHelp() => Console.WriteLine("""
        CodexMultipleAccounts.Dnx

        Usage:
          dnx profile list [--provider codex|antigravity]
          dnx profile create <name> [--provider codex|antigravity]
          dnx profile import <name> [--provider codex]
          dnx profile rename <id|name> <new-name>
          dnx profile delete <id|name>
          dnx login <id|name>
          dnx launch <id|name> [workspace] [-- codex-arguments]

        Examples:
          dnx profile create Personal --provider codex
          dnx profile create Work --provider antigravity
          dnx profile list --provider antigravity
          dnx login Personal
          dnx launch Personal ./workspace
          dnx launch Personal ./workspace -- --model gpt-5

        Notes:
          Provider is stored on each profile, so login and launch infer it from the profile.
          Default Codex-home import is supported only for the Codex provider.

        Environment:
          CODEX_MULTIPLE_ACCOUNTS_ROOT  Override the profile catalog root.
          CODEX_HOME                    Default Codex home used by profile import.
        """);
}
