using ResetFlow.Core.Configuration;
using ResetFlow.Core.IO;
using ResetFlow.Core.Logging;
using ResetFlow.Core.Registry;
using ResetFlow.Core.SystemServices;

namespace ResetFlow.Core.Features;

public sealed class FeatureContext
{
    public required ResetProfile Profile { get; init; }
    public required AppStorage Storage { get; init; }
    public required RunLogger Logger { get; init; }
    public required IRegistryStore Registry { get; init; }
    public required IFileSystem FileSystem { get; init; }
    public required ICommandRunner CommandRunner { get; init; }
    public required SystemSnapshot System { get; init; }
    public required IClock Clock { get; init; }
    public required CancellationToken CancellationToken { get; init; }
    public required string RunBackupPath { get; init; }

    public string GetFeatureBackupDirectory(string featureId)
    {
        var path = Path.Combine(RunBackupPath, featureId);
        FileSystem.CreateDirectory(path);
        return path;
    }
}
