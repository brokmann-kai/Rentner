using System.ComponentModel;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using Microsoft.Win32;

namespace FrischStart.Core;

public static class DriverInstaller
{
    public static bool IsServiceInstalled()
    {
        try
        {
            using var sc = new ServiceController(ProtectConstants.ServiceName);
            _ = sc.Status;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    public static bool Install(string infPath, out string error)
    {
        error = string.Empty;
        if (!File.Exists(infPath))
        {
            error = "INF-Datei nicht gefunden: " + infPath;
            return false;
        }

        // Entspricht QHEngineUI: InstallHinfSectionW("DefaultInstall 132 …")
        var cmd = $"DefaultInstall 132 {infPath}";
        InstallHinfSection(IntPtr.Zero, IntPtr.Zero, cmd, 0);

        if (!IsServiceInstalled())
        {
            error = "Treiberdienst wurde nicht installiert. Testmodus aktiv? INF korrekt?";
            return false;
        }

        if (!EnsureUpperFilter(out error))
            return false;

        return true;
    }

    private static bool EnsureUpperFilter(out string error)
    {
        error = string.Empty;
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                ProtectConstants.VolumeClassUpperFiltersKey,
                writable: true);

            if (key is null)
            {
                error = "Volume-Klassen-Registry nicht gefunden.";
                return false;
            }

            var existing = key.GetValue("UpperFilters") as string[] ?? Array.Empty<string>();
            if (existing.Any(x => string.Equals(x, ProtectConstants.ServiceName, StringComparison.OrdinalIgnoreCase)))
                return true;

            var next = existing.Concat(new[] { ProtectConstants.ServiceName }).ToArray();
            key.SetValue("UpperFilters", next, RegistryValueKind.MultiString);
            return true;
        }
        catch (Exception ex)
        {
            error = "UpperFilters konnte nicht gesetzt werden: " + ex.Message;
            return false;
        }
    }

    [DllImport("Setupapi.dll", CharSet = CharSet.Unicode, EntryPoint = "InstallHinfSectionW")]
    private static extern void InstallHinfSection(IntPtr hwnd, IntPtr module, string cmdLine, int nCmdShow);
}
