namespace FrischStart.Core.Vhd;

/// <summary>
/// Dual-Diff-Reset: inaktive Diff neu erzeugen und als nächsten Boot setzen.
/// Dadurch ist jeder Neustart wieder „golden“.
/// </summary>
public static class DifferencingResetService
{
    public static bool PrepareNextCleanBoot(VhdConfig config, out string message)
    {
        if (!File.Exists(config.ParentPath))
        {
            message = "Parent-VHD fehlt: " + config.ParentPath;
            return false;
        }

        var active = BcdService.DetectActiveSlot(config) ?? config.NextBootSlot;
        var next = VhdConfig.OtherSlot(active);
        var nextPath = config.PathForSlot(next);

        // Aktive Diff nie löschen — nur die andere Seite erneuern
        if (!DiskPartService.RecreateDifferencingVhd(nextPath, config.ParentPath, out var recreateMsg))
        {
            message = recreateMsg;
            return false;
        }

        if (!BcdService.SetCurrentOsToVhd(nextPath, out var bcdMsg))
        {
            message = bcdMsg;
            return false;
        }

        config.NextBootSlot = next;
        config.Save();

        message = $"Slot {next} frisch erzeugt. Beim nächsten Start bootet Windows sauber von:\n{nextPath}\n({recreateMsg})";
        return true;
    }
}
