using CodexMultipleAccounts.Core.Activation;
using CodexMultipleAccounts.Core.Profiles;
namespace CodexMultipleAccounts.Core.Tests;
public sealed class GlobalActivationTests
{
 [Fact] public async Task ActivateAsync_BacksUpDefaultAndPromotesProfile(){ using var temp=new TempDirectory(); var def=Path.Combine(temp.Path,".codex"); Directory.CreateDirectory(def); await File.WriteAllTextAsync(Path.Combine(def,"old.txt"),"old"); var manager=Path.Combine(temp.Path,"manager"); var profiles=new ProfileService(manager,def); var p=await profiles.CreateAsync("Work"); await File.WriteAllTextAsync(Path.Combine(p.CodexHome,"new.txt"),"new"); var backup=await new GlobalActivationService(profiles,manager,def).ActivateAsync(p); Assert.NotNull(backup); Assert.True(File.Exists(Path.Combine(def,"new.txt"))); Assert.True(File.Exists(Path.Combine(backup!,"old.txt"))); Assert.True((await profiles.ListAsync()).Single().IsGloballyActive); }

 [Fact] public async Task ActivateAsync_RestoresDefault_WhenProfileMetadataPersistenceFails(){ using var temp=new TempDirectory(); var def=Path.Combine(temp.Path,".codex"); Directory.CreateDirectory(def); await File.WriteAllTextAsync(Path.Combine(def,"old.txt"),"old"); var manager=Path.Combine(temp.Path,"manager"); var profiles=new ProfileService(manager,def); var p=await profiles.CreateAsync("Work"); await File.WriteAllTextAsync(Path.Combine(p.CodexHome,"new.txt"),"new"); var catalog=Path.Combine(manager,"profiles.json"); File.Delete(catalog); Directory.CreateDirectory(catalog); await Assert.ThrowsAnyAsync<Exception>(()=>new GlobalActivationService(profiles,manager,def).ActivateAsync(p)); Assert.True(File.Exists(Path.Combine(def,"old.txt"))); Assert.False(File.Exists(Path.Combine(def,"new.txt"))); }
}
