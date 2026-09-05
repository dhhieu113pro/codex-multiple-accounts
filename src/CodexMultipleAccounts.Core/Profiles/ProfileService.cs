using System.Text.Json;
namespace CodexMultipleAccounts.Core.Profiles;
public sealed class ProfileService
{
    private readonly string _root;
    private readonly string _defaultHome;
    private readonly string _catalog;
    private readonly Action<string,string> _copyDirectory;

    public ProfileService(string root, string defaultHome, Action<string,string>? copyDirectory = null)
    {
        _root=Path.GetFullPath(root);
        _defaultHome=Path.GetFullPath(defaultHome);
        _catalog=Path.Combine(_root,"profiles.json");
        _copyDirectory=copyDirectory??CopyDirectory;
        Directory.CreateDirectory(Path.Combine(_root,"profiles"));
    }

    public async Task<IReadOnlyList<CodexProfile>> ListAsync() => await LoadAsync();

    public async Task<CodexProfile> CreateAsync(string name)
    {
        if(string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Profile name is required.",nameof(name));
        var p=CreateProfile(name);
        Directory.CreateDirectory(p.CodexHome);
        var all=await LoadAsync();
        all.Add(p);
        await SaveAsync(all);
        return p;
    }

    public async Task<CodexProfile> ImportDefaultAsync(string name)
    {
        if(string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Profile name is required.",nameof(name));
        var p=CreateProfile(name);
        var profileDirectory=Directory.GetParent(p.CodexHome)!.FullName;
        try
        {
            Directory.CreateDirectory(p.CodexHome);
            if(Directory.Exists(_defaultHome)) _copyDirectory(_defaultHome,p.CodexHome);
            var all=await LoadAsync();
            all.Add(p);
            await SaveAsync(all);
            return p;
        }
        catch
        {
            if(Directory.Exists(profileDirectory)) Directory.Delete(profileDirectory,true);
            throw;
        }
    }

    public async Task RenameAsync(Guid id,string name) { var all=await LoadAsync(); var i=all.FindIndex(x=>x.Id==id); if(i<0) throw new KeyNotFoundException(); all[i]=all[i] with{Name=name.Trim()}; await SaveAsync(all); }
    public async Task DeleteAsync(Guid id) { var all=await LoadAsync(); var p=all.Single(x=>x.Id==id); GuardManaged(p.CodexHome); if(Directory.Exists(p.CodexHome)) Directory.Delete(p.CodexHome,true); all.Remove(p); await SaveAsync(all); }
    internal async Task SetActiveAsync(Guid id) { var all=await LoadAsync(); for(var i=0;i<all.Count;i++) all[i]=all[i] with{IsGloballyActive=all[i].Id==id}; await SaveAsync(all); }

    private CodexProfile CreateProfile(string name)
    {
        var id=Guid.NewGuid();
        return new CodexProfile(id,name.Trim(),Path.Combine(_root,"profiles",id.ToString("N"),"codex-home"),null,false);
    }

    private void GuardManaged(string path) { var full=Path.GetFullPath(path); var profiles=Path.GetFullPath(Path.Combine(_root,"profiles"))+Path.DirectorySeparatorChar; if(!full.StartsWith(profiles,StringComparison.OrdinalIgnoreCase)||string.Equals(full,_defaultHome,StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe profile path."); }
    private async Task<List<CodexProfile>> LoadAsync() => !File.Exists(_catalog)?[]:JsonSerializer.Deserialize<List<CodexProfile>>(await File.ReadAllTextAsync(_catalog))??[];
    private async Task SaveAsync(List<CodexProfile> p) { Directory.CreateDirectory(_root); var tmp=_catalog+".tmp"; await File.WriteAllTextAsync(tmp,JsonSerializer.Serialize(p,new JsonSerializerOptions{WriteIndented=true})); File.Move(tmp,_catalog,true); }
    internal static void CopyDirectory(string source,string target) { Directory.CreateDirectory(target); foreach(var file in Directory.EnumerateFiles(source)) File.Copy(file,Path.Combine(target,Path.GetFileName(file)),true); foreach(var dir in Directory.EnumerateDirectories(source)) CopyDirectory(dir,Path.Combine(target,Path.GetFileName(dir))); }
}
