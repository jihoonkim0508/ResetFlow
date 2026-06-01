using System.Security.Principal;
using System.Windows.Forms;

namespace ResetFlow.Core.SystemServices;

public sealed record SystemSnapshot(
    string WindowsVersion,
    bool IsAdministrator,
    Models.DeviceType DeviceType,
    string GpuInfo);

public interface ISystemInfoProvider
{
    Task<SystemSnapshot> CaptureAsync(CancellationToken cancellationToken);
}

public sealed class WindowsSystemInfoProvider(ICommandRunner commandRunner) : ISystemInfoProvider
{
    public async Task<SystemSnapshot> CaptureAsync(CancellationToken cancellationToken)
    {
        var isAdmin = IsAdministrator();
        var deviceType = SystemInformation.PowerStatus.BatteryChargeStatus == BatteryChargeStatus.NoSystemBattery
            ? Models.DeviceType.Desktop
            : Models.DeviceType.Laptop;
        var gpu = "Unknown";

        try
        {
            var result = await commandRunner.RunAsync(
                "powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -Command \"Get-CimInstance Win32_VideoController | Select-Object -ExpandProperty Name\"",
                cancellationToken);
            if (result.Succeeded && !string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                gpu = string.Join(", ", result.StandardOutput.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
            }
        }
        catch
        {
            gpu = "Unknown";
        }

        return new SystemSnapshot(Environment.OSVersion.VersionString, isAdmin, deviceType, gpu);
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
