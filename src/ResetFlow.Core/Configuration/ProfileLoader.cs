using System.Text.Json;
using ResetFlow.Core.IO;

namespace ResetFlow.Core.Configuration;

public sealed class ProfileLoader(IFileSystem fileSystem)
{
    public async Task<ResetProfile> LoadOrCreateAsync(string path, CancellationToken cancellationToken)
    {
        if (!fileSystem.FileExists(path))
        {
            var profile = ResetProfile.CreateDefault();
            fileSystem.CreateDirectory(Path.GetDirectoryName(path)!);
            await fileSystem.WriteAllTextAsync(path, JsonSerializer.Serialize(profile, ResetProfile.JsonOptions), cancellationToken);
            return profile;
        }

        var json = await fileSystem.ReadAllTextAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<ResetProfile>(json, ResetProfile.JsonOptions) ?? ResetProfile.CreateDefault();
    }
}
