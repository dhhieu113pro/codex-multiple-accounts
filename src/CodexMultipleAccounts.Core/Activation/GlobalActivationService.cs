using CodexMultipleAccounts.Core.Profiles;
namespace CodexMultipleAccounts.Core.Activation;
public sealed class GlobalActivationService(ProfileService profiles,string managerRoot,string defaultHome)
{
    public async Task<string?> ActivateAsync(CodexProfile profile,CancellationToken cancellationToken=default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var target=Path.GetFullPath(defaultHome);
        var stage=Path.Combine(managerRoot,"activation-stage-"+Guid.NewGuid().ToString("N"));
        var backups=Path.Combine(managerRoot,"backups");
        Directory.CreateDirectory(backups);
        ProfileService.CopyDirectory(profile.CodexHome,stage);
        string? backup=null;
        var promoted=false;
        try
        {
            if(Directory.Exists(target))
            {
                backup=Path.Combine(backups,DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff"));
                Directory.Move(target,backup);
            }
            Directory.Move(stage,target);
            promoted=true;
            await profiles.SetActiveAsync(profile.Id);
            return backup;
        }
        catch
        {
            if(Directory.Exists(stage)) Directory.Delete(stage,true);
            if(promoted && Directory.Exists(target)) Directory.Delete(target,true);
            if(backup is not null && Directory.Exists(backup)) Directory.Move(backup,target);
            throw;
        }
    }
}
