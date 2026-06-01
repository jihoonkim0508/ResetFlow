using System.Text.Json;
using ResetFlow.Core.Configuration;

namespace ResetFlow.Core.Tests;

public sealed class ProfileTests
{
    [Fact]
    public async Task LoadOrCreateAsync_CreatesDefaultProfile_WhenFileIsMissing()
    {
        var fileSystem = new MemoryFileSystem();
        var profile = await new ProfileLoader(fileSystem).LoadOrCreateAsync(@"C:\ResetFlow\profile.json", CancellationToken.None);

        Assert.Equal("default", profile.ProfileName);
        Assert.Contains("windows.mouse.disableAcceleration", profile.Features.Keys);
        Assert.True(fileSystem.FileExists(@"C:\ResetFlow\profile.json"));
    }

    [Fact]
    public async Task LoadOrCreateAsync_ParsesJsonProfile()
    {
        var fileSystem = new MemoryFileSystem();
        var path = @"C:\ResetFlow\profile.json";
        await fileSystem.WriteAllTextAsync(path, JsonSerializer.Serialize(new ResetProfile
        {
            ProfileName = "custom",
            Features = new Dictionary<string, bool> { ["apps.install.git"] = true }
        }, ResetProfile.JsonOptions), CancellationToken.None);

        var profile = await new ProfileLoader(fileSystem).LoadOrCreateAsync(path, CancellationToken.None);

        Assert.Equal("custom", profile.ProfileName);
        Assert.True(profile.Features["apps.install.git"]);
    }
}
