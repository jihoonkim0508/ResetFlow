using System.Text.Json;
using ResetFlow.Core.Models;

namespace ResetFlow.Core.Features;

public sealed class FolderFeature(
    string id,
    string name,
    string category,
    string description,
    Environment.SpecialFolder rootFolder,
    Func<FeatureContext, IReadOnlyList<string>> relativePaths,
    bool createTodoFile,
    int order) : FeatureBase
{
    public override string Id => id;
    public override string Name => name;
    public override string Category => category;
    public override string Description => description;
    public override AutomationLevel AutomationLevel => AutomationLevel.Automatic;
    public override RollbackCapability RollbackCapability => RollbackCapability.Full;
    public override int Order => order;

    public override async Task<FeatureResult> BackupAsync(FeatureContext context)
    {
        var root = context.FileSystem.GetFolderPath(rootFolder);
        var snapshots = relativePaths(context)
            .Select(path => new FolderPathSnapshot(path, context.FileSystem.DirectoryExists(Path.Combine(root, path)), context.FileSystem.FileExists(Path.Combine(root, path))))
            .ToList();

        if (createTodoFile)
        {
            var todo = context.Profile.Folders.TodoFile;
            snapshots.Add(new FolderPathSnapshot(todo, context.FileSystem.DirectoryExists(Path.Combine(root, todo)), context.FileSystem.FileExists(Path.Combine(root, todo))));
        }

        var backupPath = Path.Combine(context.GetFeatureBackupDirectory(Id), "backup.json");
        await context.FileSystem.WriteAllTextAsync(backupPath, JsonSerializer.Serialize(snapshots, JsonOptions), context.CancellationToken);
        return FeatureResult.Success(Id, FeatureStep.Backup, backupPath);
    }

    public override async Task<FeatureResult> ApplyAsync(FeatureContext context)
    {
        var root = context.FileSystem.GetFolderPath(rootFolder);
        foreach (var relativePath in relativePaths(context))
        {
            context.FileSystem.CreateDirectory(Path.Combine(root, relativePath));
        }

        if (createTodoFile)
        {
            var todoPath = Path.Combine(root, context.Profile.Folders.TodoFile);
            if (!context.FileSystem.FileExists(todoPath))
            {
                await context.FileSystem.WriteAllTextAsync(todoPath, TodoText, context.CancellationToken);
            }
        }

        return FeatureResult.Success(Id, FeatureStep.Apply);
    }

    public override Task<FeatureResult> VerifyAsync(FeatureContext context)
    {
        var root = context.FileSystem.GetFolderPath(rootFolder);
        var missing = relativePaths(context)
            .Select(path => Path.Combine(root, path))
            .Where(path => !context.FileSystem.DirectoryExists(path))
            .ToList();

        if (missing.Count > 0)
        {
            return Task.FromResult(FeatureResult.Failed(Id, FeatureStep.Verify, $"Missing paths: {string.Join(", ", missing)}"));
        }

        return Task.FromResult(FeatureResult.Success(Id, FeatureStep.Verify));
    }

    public override async Task<RollbackResult> RollbackAsync(FeatureContext context)
    {
        var backupPath = Path.Combine(context.GetFeatureBackupDirectory(Id), "backup.json");
        if (!context.FileSystem.FileExists(backupPath))
        {
            return new RollbackResult(Id, FeatureStatus.Skipped, "No folder backup was found.");
        }

        var json = await context.FileSystem.ReadAllTextAsync(backupPath, context.CancellationToken);
        var root = context.FileSystem.GetFolderPath(rootFolder);
        var snapshots = JsonSerializer.Deserialize<List<FolderPathSnapshot>>(json, JsonOptions) ?? [];
        var removed = 0;
        var todoFile = context.Profile.Folders.TodoFile;
        if (createTodoFile)
        {
            var todoSnapshot = snapshots.FirstOrDefault(path => path.RelativePath.Equals(todoFile, StringComparison.OrdinalIgnoreCase));
            if (todoSnapshot is { DirectoryExisted: false, FileExisted: false }
                && context.FileSystem.DeleteFileIfExists(Path.Combine(root, todoSnapshot.RelativePath)))
            {
                removed++;
            }
        }

        foreach (var snapshot in snapshots
            .Where(path => !path.DirectoryExisted && !path.FileExisted && (!createTodoFile || !path.RelativePath.Equals(todoFile, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(path => path.RelativePath.Length))
        {
            if (context.FileSystem.DeleteDirectoryIfEmpty(Path.Combine(root, snapshot.RelativePath)))
            {
                removed++;
            }
        }

        return new RollbackResult(Id, FeatureStatus.RolledBack, $"Removed {removed} newly-created empty paths. Existing or user-modified paths were preserved.");
    }

    private sealed record FolderPathSnapshot(string RelativePath, bool DirectoryExisted, bool FileExisted);

    private const string TodoText = """
        # TO DO

        - Chrome account login check
        - Windows settings check
        - Driver installation check
        - Development tool installation check
        - Taskbar cleanup
        - Start menu cleanup
        """;

    private static JsonSerializerOptions JsonOptions { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
