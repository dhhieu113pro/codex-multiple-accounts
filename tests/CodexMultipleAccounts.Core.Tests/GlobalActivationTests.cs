using CodexMultipleAccounts.Core.Activation;
using CodexMultipleAccounts.Core.Profiles;
namespace CodexMultipleAccounts.Core.Tests;
public sealed class GlobalActivationTests
{
 [Fact] public async Task ActivateAsync_BacksUpDefaultAndPromotesProfile(){ using var temp=new TempDirectory(); var def=Path.Combine(temp.Path,".codex"); Directory.CreateDirectory(def); await File.WriteAllTextAsync(Path.Combine(def,"old.txt"),"old"); var profiles=new ProfileService(Path.Combine(temp.Path,"manager"),def); var p=await profiles.CreateAsync("Work"); await File.WriteAllTextAsync(Path.Combine(p.CodexHome,"new.txt"),"new"); var backup=await new GlobalActivationService(profiles,Path.Combine(temp.Path,"manager"),def).ActivateAsync(p); Assert.NotNull(backup); Assert.True(File.Exists(Path.Combine(def,"new.txt"))); Assert.True(File.Exists(Path.Combine(backup!,"old.txt"))); Assert.True((await profiles.ListAsync()).Single().IsGloballyActive); }
}
