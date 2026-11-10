namespace FrischStart.Core.Vhd;

public static class ScheduledTaskService
{
    public const string TaskName = "FrischStart-VhdReset";

    public static bool InstallStartupResetTask(string workerScriptPath, out string message)
    {
        if (!File.Exists(workerScriptPath))
        {
            message = "Worker-Skript fehlt: " + workerScriptPath;
            return false;
        }

        // Bei jedem Start (SYSTEM): Diff-Reset für den *nächsten* Boot vorbereiten
        var args =
            $"/Create /F /TN \"{TaskName}\" /SC ONSTART /RU SYSTEM /RL HIGHEST " +
            $"/TR \"powershell.exe -NoProfile -ExecutionPolicy Bypass -File \\\"{workerScriptPath}\\\"\"";

        var (code, output) = ProcessRunner.Run("schtasks.exe", args);
        if (code != 0)
        {
            message = "Geplante Aufgabe konnte nicht angelegt werden: " + output;
            return false;
        }

        message = "Geplante Aufgabe „" + TaskName + "“ ist aktiv (bei jedem Start).";
        return true;
    }

    public static bool RemoveStartupResetTask(out string message)
    {
        var (code, output) = ProcessRunner.Run("schtasks.exe", $"/Delete /F /TN \"{TaskName}\"");
        if (code != 0 && !output.Contains("nicht vorhanden", StringComparison.OrdinalIgnoreCase)
                       && !output.Contains("cannot find", StringComparison.OrdinalIgnoreCase)
                       && !output.Contains("ERROR: The system cannot find", StringComparison.OrdinalIgnoreCase))
        {
            message = "Aufgabe konnte nicht entfernt werden: " + output;
            return false;
        }

        message = "Geplante Aufgabe entfernt.";
        return true;
    }

    public static bool IsInstalled()
    {
        var (code, _) = ProcessRunner.Run("schtasks.exe", $"/Query /TN \"{TaskName}\"");
        return code == 0;
    }
}
