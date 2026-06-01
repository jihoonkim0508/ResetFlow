using ResetFlow.Core.Configuration;
using ResetFlow.Core.Features;
using ResetFlow.Core.IO;
using ResetFlow.Core.Logging;
using ResetFlow.Core.Models;
using ResetFlow.Core.Registry;
using ResetFlow.Core.SystemServices;

namespace ResetFlow.Core.Tests;

internal sealed class MemoryFileSystem : IFileSystem
{
    private readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _directories = new(StringComparer.OrdinalIgnoreCase);

    public string DocumentsPath { get; } = Normalize(@"C:\Users\Test\Documents");
    public string PicturesPath { get; } = Normalize(@"C:\Users\Test\Pictures");

    public IReadOnlyDictionary<string, string> Files => _files;
    public IReadOnlySet<string> Directories => _directories;

    public bool DirectoryExists(string path) => _directories.Contains(Normalize(path));
    public bool FileExists(string path) => _files.ContainsKey(Normalize(path));
    public void CreateDirectory(string path) => _directories.Add(Normalize(path));
    public bool DeleteFileIfExists(string path) => _files.Remove(Normalize(path));
    public bool DeleteDirectoryIfEmpty(string path)
    {
        path = Normalize(path);
        if (_files.Keys.Any(file => file.StartsWith(path + "\\", StringComparison.OrdinalIgnoreCase))
            || _directories.Any(directory => directory.StartsWith(path + "\\", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return _directories.Remove(path);
    }

    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken)
    {
        _files[Normalize(path)] = contents;
        return Task.CompletedTask;
    }

    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken) =>
        Task.FromResult(_files[Normalize(path)]);

    public string GetFolderPath(Environment.SpecialFolder folder) =>
        folder == Environment.SpecialFolder.MyPictures ? PicturesPath : DocumentsPath;

    public string ReadFile(string path) => _files[Normalize(path)];

    public static string Normalize(string path) => path.Replace('/', '\\').TrimEnd('\\');
}

internal sealed class FakeClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset Now { get; } = now;
}

internal sealed class FakeRegistryStore : IRegistryStore
{
    private readonly Dictionary<string, object?> _values = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<RegistryValueSnapshot> ReadValues(string subKeyPath, IReadOnlyList<string> names) =>
        names.Select(name => new RegistryValueSnapshot(name, _values.GetValueOrDefault($"{subKeyPath}\\{name}"))).ToList();

    public void WriteValue(string subKeyPath, string name, object value) =>
        _values[$"{subKeyPath}\\{name}"] = value;
}

internal sealed class FakeCommandRunner(params CommandResult[] results) : ICommandRunner
{
    private readonly Queue<CommandResult> _results = new(results);

    public Task<CommandResult> RunAsync(string fileName, string arguments, CancellationToken cancellationToken) =>
        Task.FromResult(_results.Count == 0 ? new CommandResult(fileName, arguments, 0, "", "") : _results.Dequeue());
}

internal sealed class TestFeature : FeatureBase
{
    private readonly bool _failApply;
    private readonly List<string> _events;

    public TestFeature(string id, int order, List<string> events, bool failApply = false)
    {
        Id = id;
        Order = order;
        _events = events;
        _failApply = failApply;
    }

    public override string Id { get; }
    public override string Name => Id;
    public override string Category => "Test";
    public override string Description => "Test feature";
    public override AutomationLevel AutomationLevel => AutomationLevel.Automatic;
    public override RollbackCapability RollbackCapability => RollbackCapability.Full;
    public override int Order { get; }

    public override Task<FeatureResult> ApplyAsync(FeatureContext context)
    {
        _events.Add($"apply:{Id}");
        return Task.FromResult(_failApply
            ? FeatureResult.Failed(Id, FeatureStep.Apply, "fail")
            : FeatureResult.Success(Id, FeatureStep.Apply));
    }

    public override Task<RollbackResult> RollbackAsync(FeatureContext context)
    {
        _events.Add($"rollback:{Id}");
        return Task.FromResult(new RollbackResult(Id, FeatureStatus.RolledBack, "rolled back"));
    }
}

internal static class TestContextFactory
{
    public static FeatureContext Create(MemoryFileSystem? fileSystem = null, ResetProfile? profile = null)
    {
        fileSystem ??= new MemoryFileSystem();
        var root = MemoryFileSystem.Normalize(@"C:\ResetFlow");
        fileSystem.CreateDirectory(root);
        var storage = new AppStorage
        {
            RootPath = root,
            LogsPath = Path.Combine(root, "logs"),
            BackupsPath = Path.Combine(root, "backups"),
            ProfilePath = Path.Combine(root, "profile.json")
        };

        return new FeatureContext
        {
            Profile = profile ?? ResetProfile.CreateDefault(),
            Storage = storage,
            Logger = new RunLogger(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.log")),
            Registry = new FakeRegistryStore(),
            FileSystem = fileSystem,
            CommandRunner = new FakeCommandRunner(),
            System = new SystemSnapshot("Windows", true, DeviceType.Desktop, "GPU"),
            Clock = new FakeClock(new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero)),
            CancellationToken = CancellationToken.None,
            RunBackupPath = Path.Combine(storage.BackupsPath, "20260601_100000")
        };
    }
}
