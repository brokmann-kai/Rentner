using System.Diagnostics;
using Microsoft.Win32;

namespace FrischStart.Core;

public static class TestSigningService
{
    public static bool IsTestSigningEnabled()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\CI\Policy");
            // Fallback: bcdedit abfragen
        }
        catch
        {
            // ignore
        }

        var output = RunBcdedit("/enum {current}");
        // DE: "Ja" / EN: "Yes"
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (!line.Contains("testsigning", StringComparison.OrdinalIgnoreCase))
                continue;
            return line.Contains("Yes", StringComparison.OrdinalIgnoreCase)
                   || line.Contains("Ja", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    public static bool EnableTestSigning(out string message)
    {
        if (!RunBcdeditSuccess("/set testsigning on", out var err))
        {
            message = "Testmodus konnte nicht aktiviert werden: " + err;
            return false;
        }

        message = "Testmodus ist aktiviert. Bitte einmal neu starten, danach Schutz einschalten.";
        return true;
    }

    private static bool RunBcdeditSuccess(string args, out string error)
    {
        error = string.Empty;
        try
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = "bcdedit.exe",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
            if (p is null)
            {
                error = "bcdedit konnte nicht gestartet werden.";
                return false;
            }

            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0)
            {
                error = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string RunBcdedit(string args)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = "bcdedit.exe",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
            if (p is null)
                return string.Empty;
            var stdout = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return stdout;
        }
        catch
        {
            return string.Empty;
        }
    }
}
