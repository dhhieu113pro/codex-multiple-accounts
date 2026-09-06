using CodexMultipleAccounts.Core.Profiles;
namespace CodexMultipleAccounts.Core.Launching;
public sealed class CodexLaunchService { public CodexLaunchSpec Create(CodexProfile profile,string workingDirectory,IReadOnlyList<string>? arguments=null) => new("codex",arguments??[],Path.GetFullPath(workingDirectory),new Dictionary<string,string>{{"CODEX_HOME",profile.CodexHome}}); }
