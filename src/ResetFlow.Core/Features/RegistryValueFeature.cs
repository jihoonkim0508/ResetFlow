using System.Text.Json;
using ResetFlow.Core.Models;
using ResetFlow.Core.Registry;

namespace ResetFlow.Core.Features;

public sealed class RegistryValueFeature(
    string id,
    string name,
    string category,
    string description,
    string subKeyPath,
    IReadOnlyDictionary<string, object> desiredValues,
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
        var names = desiredValues.Keys.ToList();
        var values = context.Registry.ReadValues(subKeyPath, names);
        var backupPath = GetBackupPath(context);
        await context.FileSystem.WriteAllTextAsync(
            backupPath,
            JsonSerializer.Serialize(new RegistryBackup(subKeyPath, values), BackupJsonOptions),
            context.CancellationToken);
        return FeatureResult.Success(Id, FeatureStep.Backup, backupPath);
    }

    public override Task<FeatureResult> ApplyAsync(FeatureContext context)
    {
        foreach (var (key, value) in desiredValues)
        {
            context.Registry.WriteValue(subKeyPath, key, value);
        }

        return Task.FromResult(FeatureResult.Success(Id, FeatureStep.Apply));
    }

    public override Task<FeatureResult> VerifyAsync(FeatureContext context)
    {
        var actual = context.Registry.ReadValues(subKeyPath, desiredValues.Keys.ToList());
        foreach (var expected in desiredValues)
        {
            var actualValue = actual.First(value => value.Name == expected.Key).Value;
            if (!Equals(Convert.ToString(actualValue), Convert.ToString(expected.Value)))
            {
                return Task.FromResult(FeatureResult.Failed(Id, FeatureStep.Verify, $"{expected.Key} was not applied."));
            }
        }

        return Task.FromResult(FeatureResult.Success(Id, FeatureStep.Verify));
    }

    public override async Task<RollbackResult> RollbackAsync(FeatureContext context)
    {
        var backupPath = GetBackupPath(context);
        if (!context.FileSystem.FileExists(backupPath))
        {
            return new RollbackResult(Id, FeatureStatus.Skipped, "No registry backup was found.");
        }

        var json = await context.FileSystem.ReadAllTextAsync(backupPath, context.CancellationToken);
        var backup = JsonSerializer.Deserialize<RegistryBackup>(json, BackupJsonOptions);
        if (backup is null)
        {
            return new RollbackResult(Id, FeatureStatus.Failed, "Registry backup could not be read.");
        }

        foreach (var value in backup.Values)
        {
            if (value.Value is null)
            {
                continue;
            }

            context.Registry.WriteValue(backup.SubKeyPath, value.Name, NormalizeJsonElement(value.Value));
        }

        return new RollbackResult(Id, FeatureStatus.RolledBack, "Registry values restored.");
    }

    private string GetBackupPath(FeatureContext context) =>
        Path.Combine(context.GetFeatureBackupDirectory(Id), "backup.json");

    private static object NormalizeJsonElement(object value)
    {
        if (value is not JsonElement element)
        {
            return value;
        }

        return element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetInt32(out var intValue) => intValue,
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.True => 1,
            JsonValueKind.False => 0,
            _ => element.ToString()
        };
    }

    private sealed record RegistryBackup(string SubKeyPath, IReadOnlyList<RegistryValueSnapshot> Values);

    private static JsonSerializerOptions BackupJsonOptions { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
