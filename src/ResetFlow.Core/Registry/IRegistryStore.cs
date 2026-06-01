namespace ResetFlow.Core.Registry;

public sealed record RegistryValueSnapshot(string Name, object? Value);

public interface IRegistryStore
{
    IReadOnlyList<RegistryValueSnapshot> ReadValues(string subKeyPath, IReadOnlyList<string> names);
    void WriteValue(string subKeyPath, string name, object value);
}
