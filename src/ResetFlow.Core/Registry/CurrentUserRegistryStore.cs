using Microsoft.Win32;

namespace ResetFlow.Core.Registry;

public sealed class CurrentUserRegistryStore : IRegistryStore
{
    public IReadOnlyList<RegistryValueSnapshot> ReadValues(string subKeyPath, IReadOnlyList<string> names)
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(subKeyPath);
        return names.Select(name => new RegistryValueSnapshot(name, key?.GetValue(name))).ToList();
    }

    public void WriteValue(string subKeyPath, string name, object value)
    {
        using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(subKeyPath, true);
        key.SetValue(name, value);
    }
}
