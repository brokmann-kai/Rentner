namespace FrischStart.Core;

/// <summary>
/// Logik aus QHEngineUIDlg.cpp (CreateProtectStateFiles / DisableProtectByFile).
/// </summary>
public static class ProtectStateFile
{
    public static string GetPath(char driveLetter) =>
        $"{char.ToUpperInvariant(driveLetter)}:\\{ProtectConstants.ProtectStateFileName}";

    public static bool EnableOnVolumes(IEnumerable<char> driveLetters, out string error)
    {
        error = string.Empty;
        foreach (var letter in driveLetters)
        {
            if (!WriteState(letter, enabled: true, out error))
                return false;
        }

        return true;
    }

    public static bool DisableOnAllFixedVolumes(out string error)
    {
        error = string.Empty;
        var any = false;
        foreach (var vol in VolumeEnumerator.GetFixedNtfsVolumes())
        {
            var path = GetPath(vol.DriveLetter);
            if (!File.Exists(path))
                continue;

            if (!WriteState(vol.DriveLetter, enabled: false, out error))
                return false;
            any = true;
        }

        if (!any)
        {
            error = "Keine Schutz-Statusdatei gefunden — Schutz war vermutlich nicht aktiv.";
            return false;
        }

        return true;
    }

    public static bool? ReadEnabled(char driveLetter)
    {
        var path = GetPath(driveLetter);
        if (!File.Exists(path))
            return null;

        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var b = fs.ReadByte();
            if (b < 0)
                return null;
            return b == 1;
        }
        catch
        {
            return null;
        }
    }

    private static bool WriteState(char driveLetter, bool enabled, out string error)
    {
        error = string.Empty;
        var path = GetPath(driveLetter);

        try
        {
            if (File.Exists(path))
                File.SetAttributes(path, FileAttributes.Normal);

            if (enabled)
            {
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    var zeros = new byte[64 * 1024];
                    var remaining = ProtectConstants.ProtectStateFileSize;
                    while (remaining > 0)
                    {
                        var chunk = Math.Min(zeros.Length, remaining);
                        fs.Write(zeros, 0, chunk);
                        remaining -= chunk;
                    }

                    fs.Seek(0, SeekOrigin.Begin);
                    fs.WriteByte(1);
                    fs.Flush(true);
                }

                File.SetAttributes(path, FileAttributes.Hidden | FileAttributes.System);
            }
            else
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.Read);
                fs.WriteByte(0);
                fs.Flush(true);
                File.SetAttributes(path, FileAttributes.Hidden | FileAttributes.System);
            }

            return true;
        }
        catch (Exception ex)
        {
            error = $"Statusdatei auf {driveLetter}: fehlgeschlagen — {ex.Message}";
            return false;
        }
    }
}
