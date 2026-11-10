namespace FrischStart.Core.Vhd;

public static class DiskPartService
{
    public static bool RecreateDifferencingVhd(string childPath, string parentPath, out string message)
    {
        message = string.Empty;
        if (!File.Exists(parentPath))
        {
            message = "Parent-VHD fehlt: " + parentPath;
            return false;
        }

        try
        {
            if (File.Exists(childPath))
            {
                File.SetAttributes(childPath, FileAttributes.Normal);
                File.Delete(childPath);
            }
        }
        catch (Exception ex)
        {
            message = "Alte Diff-VHD konnte nicht gelöscht werden (noch gemountet/aktiv?): " + ex.Message;
            return false;
        }

        var script = Path.Combine(Path.GetTempPath(), "frischstart-diskpart-" + Guid.NewGuid().ToString("N") + ".txt");
        try
        {
            File.WriteAllText(script,
                $"create vdisk file=\"{childPath}\" parent=\"{parentPath}\"{Environment.NewLine}exit{Environment.NewLine}");

            var (code, output) = ProcessRunner.Run("diskpart.exe", $"/s \"{script}\"");
            if (code != 0 || output.Contains("Fehler", StringComparison.OrdinalIgnoreCase)
                          || output.Contains("error", StringComparison.OrdinalIgnoreCase))
            {
                message = "diskpart fehlgeschlagen: " + output;
                return false;
            }

            if (!File.Exists(childPath))
            {
                message = "Diff-VHD wurde nicht erzeugt: " + childPath;
                return false;
            }

            message = "Diff-VHD neu erzeugt: " + childPath;
            return true;
        }
        finally
        {
            try { File.Delete(script); } catch { /* ignore */ }
        }
    }

    public static bool EnsureDifferencingPair(VhdConfig config, out string message)
    {
        Directory.CreateDirectory(config.StorePath);

        if (!File.Exists(config.ParentPath))
        {
            message = "Parent fehlt. Bitte zuerst golden.vhdx anlegen — siehe docs/VHD-SETUP.md";
            return false;
        }

        if (!File.Exists(config.DiffAPath))
        {
            if (!RecreateDifferencingVhd(config.DiffAPath, config.ParentPath, out message))
                return false;
        }

        if (!File.Exists(config.DiffBPath))
        {
            if (!RecreateDifferencingVhd(config.DiffBPath, config.ParentPath, out message))
                return false;
        }

        message = "Parent und beide Diff-VHDs sind vorhanden.";
        return true;
    }
}
