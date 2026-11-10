using System.Text.RegularExpressions;

namespace FrischStart.Core.Vhd;

public static class BcdService
{
    public const string ProtectedDescription = "FrischStart (geschützt)";
    public const string MaintenanceDescription = "FrischStart Wartung";
    public const string EmergencyDescription = "Windows Notfall (vorher)";

    public static string ToVhdSpec(string vhdFullPath)
    {
        vhdFullPath = Path.GetFullPath(vhdFullPath);
        var root = Path.GetPathRoot(vhdFullPath)?.TrimEnd('\\', '/') ?? string.Empty;
        var relative = vhdFullPath[root.Length..].TrimStart('\\', '/');
        return $"vhd=[{root.TrimEnd(':')}:]\\{relative}";
    }

    public static bool SetEntryToVhd(string entryId, string vhdFullPath, string description, out string message)
    {
        var spec = ToVhdSpec(vhdFullPath);
        var (c1, o1) = ProcessRunner.Run("bcdedit.exe", $"/set {entryId} device {spec}");
        var (c2, o2) = ProcessRunner.Run("bcdedit.exe", $"/set {entryId} osdevice {spec}");
        var (c3, o3) = ProcessRunner.Run("bcdedit.exe", $"/set {entryId} description \"{description}\"");
        if (c1 != 0 || c2 != 0)
        {
            message = "bcdedit fehlgeschlagen:\n" + o1 + "\n" + o2 + "\n" + o3;
            return false;
        }

        message = $"{description} → {vhdFullPath}";
        return true;
    }

    /// <summary>
    /// Setzt den aktuellen Boot-Eintrag auf eine VHD-Datei.
    /// </summary>
    public static bool SetCurrentOsToVhd(string vhdFullPath, out string message) =>
        SetEntryToVhd("{current}", vhdFullPath, ProtectedDescription, out message);

    public static string GetCurrentOsDevice()
    {
        var (_, output) = ProcessRunner.Run("bcdedit.exe", "/enum {current}");
        foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.TrimStart().StartsWith("osdevice", StringComparison.OrdinalIgnoreCase))
                return line;
        }

        return output;
    }

    public static bool CurrentOsLooksLikeVhd()
    {
        var line = GetCurrentOsDevice();
        return line.Contains("vhd=", StringComparison.OrdinalIgnoreCase)
               || line.Contains(".vhdx", StringComparison.OrdinalIgnoreCase)
               || line.Contains(".vhd", StringComparison.OrdinalIgnoreCase);
    }

    public static bool CurrentOsLooksLikeParent(VhdConfig config)
    {
        var line = GetCurrentOsDevice();
        return line.Contains(config.ParentFileName, StringComparison.OrdinalIgnoreCase);
    }

    public static string? DetectActiveSlot(VhdConfig config)
    {
        var line = GetCurrentOsDevice();
        if (line.Contains(config.DiffAFileName, StringComparison.OrdinalIgnoreCase))
            return "A";
        if (line.Contains(config.DiffBFileName, StringComparison.OrdinalIgnoreCase))
            return "B";

        var match = Regex.Match(line, @"diff-([ab])\.vhdx", RegexOptions.IgnoreCase);
        if (match.Success)
            return match.Groups[1].Value.ToUpperInvariant();

        return null;
    }

    /// <summary>
    /// Kopiert {current} als Notfall-Eintrag (einmalig), legt Wartung an,
    /// setzt geschützten Eintrag, Timeout und Standard laut Policy.
    /// </summary>
    public static bool ApplyBootLayout(
        VhdConfig config,
        string protectedDiffPath,
        BootMenuMode mode,
        out string message)
    {
        var log = new System.Text.StringBuilder();

        // 1) Notfall-Kopie der bisherigen Windows-Installation (falls noch physisch)
        EnsureNamedCopy("{current}", EmergencyDescription, log);

        // 2) Geschützter Eintrag = current auf Diff zeigen
        if (!SetEntryToVhd("{current}", protectedDiffPath, ProtectedDescription, out var protMsg))
        {
            message = protMsg;
            return false;
        }

        log.AppendLine(protMsg);

        // 3) Wartungs-Eintrag (Parent) — Änderungen am Dump möglich
        var maintId = EnsureNamedCopy("{current}", MaintenanceDescription, log);
        if (maintId is not null)
        {
            if (!SetEntryToVhd(maintId, config.ParentPath, MaintenanceDescription, out var mMsg))
            {
                message = mMsg;
                return false;
            }

            log.AppendLine(mMsg);
        }
        else
        {
            log.AppendLine("Wartungs-Eintrag konnte nicht angelegt werden (bcdedit /copy).");
        }

        // 4) Timeout + Default
        var timeout = BootPolicyInfo.TimeoutSeconds(mode);
        ProcessRunner.Run("bcdedit.exe", $"/timeout {timeout}");

        if (BootPolicyInfo.DefaultIsMaintenance(mode) && maintId is not null)
        {
            ProcessRunner.Run("bcdedit.exe", $"/default {maintId}");
            log.AppendLine("Standard-Boot: Wartung");
        }
        else
        {
            ProcessRunner.Run("bcdedit.exe", "/default {current}");
            log.AppendLine("Standard-Boot: geschützt");
        }

        // Anzeigereihenfolge, soweit möglich
        if (maintId is not null)
            ProcessRunner.Run("bcdedit.exe", $"/displayorder {{current}} {maintId} /addlast");

        config.BootMenuMode = mode;
        config.BootTimeoutSeconds = timeout;
        config.Save();

        log.AppendLine($"Timeout: {timeout} s");
        if (timeout > 0)
        {
            log.AppendLine(
                "Admin-Zugang: Beim Start kurz das Boot-Menü — Pfeiltasten, dann „FrischStart Wartung“.");
        }
        else
        {
            log.AppendLine(
                "Kein Boot-Menü. Admin: Shift gedrückt halten + Neu starten → Erweiterter Start, oder Timeout später erhöhen.");
        }

        message = log.ToString().Trim();
        return true;
    }

    private static string? EnsureNamedCopy(string sourceId, string description, System.Text.StringBuilder log)
    {
        var existing = FindEntryIdByDescription(description);
        if (existing is not null)
            return existing;

        var (code, output) = ProcessRunner.Run("bcdedit.exe", $"/copy {sourceId} /d \"{description}\"");
        if (code != 0)
        {
            log.AppendLine("bcdedit /copy: " + output);
            return null;
        }

        // Ausgabe enthält GUID in {...}
        var match = Regex.Match(output, "\\{[0-9a-fA-F-]+\\}");
        if (!match.Success)
        {
            log.AppendLine("Keine GUID in /copy-Ausgabe: " + output);
            return null;
        }

        return match.Value;
    }

    public static string? FindEntryIdByDescription(string description)
    {
        var (_, output) = ProcessRunner.Run("bcdedit.exe", "/enum all");
        string? currentId = null;
        foreach (var raw in output.Split(new[] { '\r', '\n' }, StringSplitOptions.None))
        {
            var line = raw.Trim();
            if (line.StartsWith("Bezeichner", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("identifier", StringComparison.OrdinalIgnoreCase))
            {
                var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                currentId = parts.Length >= 2 ? parts[^1] : null;
            }
            else if (line.StartsWith("description", StringComparison.OrdinalIgnoreCase) ||
                     line.StartsWith("Beschreibung", StringComparison.OrdinalIgnoreCase))
            {
                if (line.Contains(description, StringComparison.OrdinalIgnoreCase))
                    return currentId;
            }
        }

        return null;
    }
}
