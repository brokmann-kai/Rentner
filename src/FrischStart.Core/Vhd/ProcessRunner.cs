using System.Diagnostics;
using System.Text;

namespace FrischStart.Core.Vhd;

internal static class ProcessRunner
{
    public static (int ExitCode, string Output) Run(string fileName, string arguments, int timeoutMs = 120_000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.Default,
            StandardErrorEncoding = Encoding.Default
        };

        using var p = Process.Start(psi);
        if (p is null)
            return (-1, "Prozess konnte nicht gestartet werden: " + fileName);

        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        if (!p.WaitForExit(timeoutMs))
        {
            try { p.Kill(entireProcessTree: true); } catch { /* ignore */ }
            return (-1, "Timeout: " + fileName);
        }

        var combined = (stdout + "\n" + stderr).Trim();
        return (p.ExitCode, combined);
    }
}
