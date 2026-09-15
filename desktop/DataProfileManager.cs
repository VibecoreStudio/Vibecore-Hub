using System.IO;
using System.Text.Json;

namespace VibecoreHub.Desktop;

public sealed class DataProfileInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = Environment.MachineName;
    public override string ToString() => Name;
}

internal sealed class DataProfileRegistry
{
    public string ActiveProfileId { get; set; } = "";
    public List<DataProfileInfo> Profiles { get; set; } = [];
    public Dictionary<string, string> MachineProfiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public static class DataProfileManager
{
    private static readonly object Gate = new();
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
    private static string DataRoot => Path.Combine(AppContext.BaseDirectory, "data");
    private static string ProfilesRoot => Path.Combine(DataRoot, "profiles");
    private static string RegistryFile => Path.Combine(DataRoot, "profiles.json");

    public static string CurrentDirectory
    {
        get
        {
            var registry = LoadRegistry();
            var directory = Path.Combine(ProfilesRoot, registry.ActiveProfileId);
            Directory.CreateDirectory(directory);
            return directory;
        }
    }

    public static DataProfileInfo ActiveProfile
    {
        get
        {
            var registry = LoadRegistry();
            return registry.Profiles.First(profile => profile.Id == registry.ActiveProfileId);
        }
    }

    public static IReadOnlyList<DataProfileInfo> GetProfiles() => LoadRegistry().Profiles.Select(profile => new DataProfileInfo { Id = profile.Id, Name = profile.Name }).ToList();

    public static DataProfileInfo Create(string name, bool copyCurrent)
    {
        lock (Gate)
        {
            var registry = LoadRegistryCore();
            var profile = new DataProfileInfo { Name = string.IsNullOrWhiteSpace(name) ? $"配置 {registry.Profiles.Count + 1}" : name.Trim() };
            var destination = Path.Combine(ProfilesRoot, profile.Id);
            Directory.CreateDirectory(destination);
            if (copyCurrent)
            {
                var source = Path.Combine(ProfilesRoot, registry.ActiveProfileId);
                CopyDirectory(source, destination);
            }
            registry.Profiles.Add(profile);
            SaveRegistry(registry);
            return profile;
        }
    }

    public static bool Switch(string profileId)
    {
        lock (Gate)
        {
            var registry = LoadRegistryCore();
            if (registry.Profiles.All(profile => profile.Id != profileId) || registry.ActiveProfileId == profileId) return false;
            registry.ActiveProfileId = profileId;
            registry.MachineProfiles[Environment.MachineName] = profileId;
            SaveRegistry(registry);
            Directory.CreateDirectory(Path.Combine(ProfilesRoot, profileId));
            return true;
        }
    }

    public static bool Rename(string profileId, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        lock (Gate)
        {
            var registry = LoadRegistryCore();
            var profile = registry.Profiles.FirstOrDefault(candidate => candidate.Id == profileId);
            if (profile is null) return false;
            profile.Name = name.Trim();
            SaveRegistry(registry);
            return true;
        }
    }

    private static DataProfileRegistry LoadRegistry()
    {
        lock (Gate) return LoadRegistryCore();
    }

    private static DataProfileRegistry LoadRegistryCore()
    {
        Directory.CreateDirectory(DataRoot);
        Directory.CreateDirectory(ProfilesRoot);
        try
        {
            if (File.Exists(RegistryFile))
            {
                var loaded = JsonSerializer.Deserialize<DataProfileRegistry>(File.ReadAllText(RegistryFile), Options);
                if (loaded is not null && loaded.Profiles.Count > 0)
                {
                    loaded.MachineProfiles ??= new(StringComparer.OrdinalIgnoreCase);
                    if (loaded.Profiles.All(profile => profile.Id != loaded.ActiveProfileId)) loaded.ActiveProfileId = loaded.Profiles[0].Id;
                    if (loaded.MachineProfiles.TryGetValue(Environment.MachineName, out var machineProfile)
                        && loaded.Profiles.Any(profile => profile.Id == machineProfile))
                    {
                        loaded.ActiveProfileId = machineProfile;
                    }
                    else if (loaded.MachineProfiles.Count == 0)
                    {
                        loaded.MachineProfiles[Environment.MachineName] = loaded.ActiveProfileId;
                        SaveRegistry(loaded);
                    }
                    else
                    {
                        var local = new DataProfileInfo { Name = Environment.MachineName };
                        loaded.Profiles.Add(local);
                        loaded.ActiveProfileId = local.Id;
                        loaded.MachineProfiles[Environment.MachineName] = local.Id;
                        Directory.CreateDirectory(Path.Combine(ProfilesRoot, local.Id));
                        SaveRegistry(loaded);
                    }
                    return loaded;
                }
            }
        }
        catch { }

        var first = new DataProfileInfo { Name = Environment.MachineName };
        var registry = new DataProfileRegistry
        {
            ActiveProfileId = first.Id,
            Profiles = [first],
            MachineProfiles = new(StringComparer.OrdinalIgnoreCase) { [Environment.MachineName] = first.Id }
        };
        var destination = Path.Combine(ProfilesRoot, first.Id);
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(DataRoot))
        {
            if (string.Equals(file, RegistryFile, StringComparison.OrdinalIgnoreCase)) continue;
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        }
        var legacyIcons = Path.Combine(DataRoot, "custom-icons");
        if (Directory.Exists(legacyIcons)) CopyDirectory(legacyIcons, Path.Combine(destination, "custom-icons"));
        SaveRegistry(registry);
        return registry;
    }

    private static void SaveRegistry(DataProfileRegistry registry)
    {
        Directory.CreateDirectory(DataRoot);
        var temporary = RegistryFile + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(registry, Options));
        File.Move(temporary, RegistryFile, true);
    }

    private static void CopyDirectory(string source, string destination)
    {
        if (!Directory.Exists(source)) return;
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, true);
        }
    }
}
