namespace ResetFlow.Core.IO;

public sealed class AppStorage
{
    public required string RootPath { get; init; }
    public required string LogsPath { get; init; }
    public required string BackupsPath { get; init; }
    public required string ProfilePath { get; init; }

    public static AppStorage Resolve(IFileSystem fileSystem, string executableDirectory)
    {
        var root = CanWrite(fileSystem, executableDirectory)
            ? executableDirectory
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ResetFlow");

        var storage = new AppStorage
        {
            RootPath = root,
            LogsPath = Path.Combine(root, "logs"),
            BackupsPath = Path.Combine(root, "backups"),
            ProfilePath = Path.Combine(root, "resetflow.profile.json")
        };

        fileSystem.CreateDirectory(storage.LogsPath);
        fileSystem.CreateDirectory(storage.BackupsPath);
        return storage;
    }

    private static bool CanWrite(IFileSystem fileSystem, string path)
    {
        try
        {
            fileSystem.CreateDirectory(path);
            var probe = Path.Combine(path, $".write-test-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probe, "");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
