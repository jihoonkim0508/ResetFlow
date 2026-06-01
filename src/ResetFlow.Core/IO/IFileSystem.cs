namespace ResetFlow.Core.IO;

public interface IFileSystem
{
    bool DirectoryExists(string path);
    bool FileExists(string path);
    void CreateDirectory(string path);
    bool DeleteFileIfExists(string path);
    bool DeleteDirectoryIfEmpty(string path);
    Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken);
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken);
    string GetFolderPath(Environment.SpecialFolder folder);
}

public sealed class RealFileSystem : IFileSystem
{
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public bool FileExists(string path) => File.Exists(path);
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    public bool DeleteFileIfExists(string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        File.Delete(path);
        return true;
    }

    public bool DeleteDirectoryIfEmpty(string path)
    {
        if (!Directory.Exists(path) || Directory.EnumerateFileSystemEntries(path).Any())
        {
            return false;
        }

        Directory.Delete(path);
        return true;
    }

    public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken) =>
        File.WriteAllTextAsync(path, contents, cancellationToken);
    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken) =>
        File.ReadAllTextAsync(path, cancellationToken);
    public string GetFolderPath(Environment.SpecialFolder folder) => Environment.GetFolderPath(folder);
}
