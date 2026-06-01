using ResetFlow.Core.Configuration;
using ResetFlow.Core.Features;

namespace ResetFlow.Core.Tests;

public sealed class FolderFeatureTests
{
    [Fact]
    public async Task ApplyAsync_CreatesFolders_AndDoesNotOverwriteExistingTodoFile()
    {
        var fileSystem = new MemoryFileSystem();
        var profile = ResetProfile.CreateDefault();
        var todoPath = Path.Combine(fileSystem.DocumentsPath, profile.Folders.TodoFile);
        await fileSystem.WriteAllTextAsync(todoPath, "existing", CancellationToken.None);
        var context = TestContextFactory.Create(fileSystem, profile);
        var feature = (FolderFeature)FeatureCatalog.CreateDefault().Features.First(feature => feature.Id == "folders.documents");

        await feature.ApplyAsync(context);

        Assert.True(fileSystem.DirectoryExists(Path.Combine(fileSystem.DocumentsPath, "toeic")));
        Assert.True(fileSystem.DirectoryExists(Path.Combine(fileSystem.DocumentsPath, "Tools", "구라 제거기")));
        Assert.Equal("existing", fileSystem.ReadFile(todoPath));
    }

    [Fact]
    public async Task RollbackAsync_RemovesOnlyNewlyCreatedEmptyPaths()
    {
        var fileSystem = new MemoryFileSystem();
        var profile = ResetProfile.CreateDefault();
        var existingTodoPath = Path.Combine(fileSystem.DocumentsPath, profile.Folders.TodoFile);
        await fileSystem.WriteAllTextAsync(existingTodoPath, "existing", CancellationToken.None);
        var context = TestContextFactory.Create(fileSystem, profile);
        var feature = (FolderFeature)FeatureCatalog.CreateDefault().Features.First(feature => feature.Id == "folders.documents");

        await feature.BackupAsync(context);
        await feature.ApplyAsync(context);
        await feature.RollbackAsync(context);

        Assert.False(fileSystem.DirectoryExists(Path.Combine(fileSystem.DocumentsPath, "toeic")));
        Assert.Equal("existing", fileSystem.ReadFile(existingTodoPath));
    }
}
