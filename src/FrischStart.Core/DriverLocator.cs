namespace FrischStart.Core;

public static class DriverLocator
{
    /// <summary>
    /// Sucht SysRestoreDriver.sys/.inf neben der EXE, in ./drivers oder im Repo-Root.
    /// </summary>
    public static bool TryFindDriverFiles(out string sysPath, out string infPath, out string hint)
    {
        sysPath = string.Empty;
        infPath = string.Empty;

        var candidates = new List<string>();
        var baseDir = AppContext.BaseDirectory;
        candidates.Add(Path.Combine(baseDir, "drivers"));
        candidates.Add(Path.Combine(baseDir, "..", "..", "..", "..", "..", "drivers"));
        candidates.Add(Path.Combine(Environment.CurrentDirectory, "drivers"));
        candidates.Add(Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..")));

        // Repo-Root aus typischem bin-Pfad ableiten
        var dir = new DirectoryInfo(baseDir);
        for (var i = 0; i < 8 && dir is not null; i++, dir = dir.Parent)
        {
            candidates.Add(Path.Combine(dir.FullName, "drivers"));
        }

        foreach (var folder in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(folder))
                continue;

            var sys = Path.Combine(folder, ProtectConstants.DriverFileName);
            var inf = Path.Combine(folder, ProtectConstants.InfFileName);
            if (File.Exists(sys) && File.Exists(inf))
            {
                sysPath = Path.GetFullPath(sys);
                infPath = Path.GetFullPath(inf);
                hint = folder;
                return true;
            }
        }

        hint = "Lege SysRestoreDriver.sys und SysRestoreDriver.inf in den Ordner „drivers“.";
        return false;
    }
}
