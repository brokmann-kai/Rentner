using System.Management;

namespace FrischStart.Core.Vhd;

public sealed class VhdSetupService
{
    public IReadOnlyList<DriveChoice> ListCandidateStores()
    {
        var list = new List<DriveChoice>();
        foreach (var d in DriveInfo.GetDrives())
        {
            if (!d.IsReady || d.DriveType is not (DriveType.Fixed or DriveType.Removable))
                continue;
            try
            {
                list.Add(new DriveChoice(
                    d.Name.TrimEnd('\\'),
                    d.VolumeLabel,
                    d.TotalSize,
                    d.AvailableFreeSpace,
                    d.DriveType));
            }
            catch
            {
                // ignore
            }
        }

        return list.OrderByDescending(x => x.FreeBytes).ToList();
    }

    public long EstimateUsedBytesOnSystemDrive()
    {
        var c = new DriveInfo("C");
        return c.IsReady ? c.TotalSize - c.AvailableFreeSpace : 0;
    }

    public bool PrepareParentVhd(VhdConfig config, int sizeGb, string mountLetter, out string message)
    {
        if (!AdminHelper.IsAdministrator())
        {
            message = "Administratorrechte nötig.";
            return false;
        }

        if (mountLetter.Length != 1 || !char.IsLetter(mountLetter[0]))
        {
            message = "Ungültiger Laufwerksbuchstabe.";
            return false;
        }

        Directory.CreateDirectory(config.StorePath);

        if (File.Exists(config.ParentPath))
        {
            message = "Parent existiert bereits: " + config.ParentPath +
                      "\nBitte Ordner leeren oder anderen Speicherort wählen.";
            return false;
        }

        var letter = char.ToUpperInvariant(mountLetter[0]);
        if (Directory.Exists(letter + ":\\"))
        {
            message = $"Laufwerk {letter}: ist schon vergeben. Bitte anderen Buchstaben wählen.";
            return false;
        }

        var maxMb = sizeGb * 1024;
        var script = $"""
            create vdisk file="{config.ParentPath}" maximum={maxMb} type=expandable
            select vdisk file="{config.ParentPath}"
            attach vdisk
            create partition primary
            format fs=ntfs quick label="FrischStartOS"
            assign letter={letter}
            exit
            """;

        if (!RunDiskPart(script, out message))
            return false;

        if (!Directory.Exists(letter + ":\\"))
        {
            message = "VHD wurde erzeugt, aber der Buchstabe erscheint nicht: " + letter;
            return false;
        }

        config.Save();
        message = $"Parent-VHD bereit und als {letter}: gemountet.\n{config.ParentPath}";
        return true;
    }

    public bool DetachVhd(string vhdPath, out string message)
    {
        var script = $"""
            select vdisk file="{vhdPath}"
            detach vdisk
            exit
            """;
        return RunDiskPart(script, out message);
    }

    public bool AttachVhd(string vhdPath, out string message)
    {
        var script = $"""
            select vdisk file="{vhdPath}"
            attach vdisk
            exit
            """;
        return RunDiskPart(script, out message);
    }

    public bool CreateDiffPair(VhdConfig config, out string message)
    {
        // Diffs nur erzeugen, wenn Parent nicht gemountet ist
        if (!DiskPartService.RecreateDifferencingVhd(config.DiffAPath, config.ParentPath, out var m1))
        {
            message = m1;
            return false;
        }

        if (!DiskPartService.RecreateDifferencingVhd(config.DiffBPath, config.ParentPath, out var m2))
        {
            message = m2;
            return false;
        }

        message = "Diff-A und Diff-B erzeugt.";
        return true;
    }

    public bool ConfigureBootToDiff(VhdConfig config, string slot, out string message)
    {
        var diffPath = config.PathForSlot(slot);
        if (!File.Exists(diffPath))
        {
            message = "Diff fehlt: " + diffPath;
            return false;
        }

        if (!AttachVhd(diffPath, out var attachMsg))
        {
            message = attachMsg;
            return false;
        }

        // Windows-Ordner auf der Diff finden
        var windowsPath = FindWindowsPathOnAttachedVhds();
        if (windowsPath is null)
        {
            DetachVhd(diffPath, out _);
            message = "Auf der VHD wurde kein \\Windows-Ordner gefunden. Installation/Klon unvollständig?";
            return false;
        }

        var (code, output) = ProcessRunner.Run("bcdboot.exe", $"\"{windowsPath}\" /l de-de");
        if (code != 0)
        {
            DetachVhd(diffPath, out _);
            message = "bcdboot fehlgeschlagen: " + output;
            return false;
        }

        DetachVhd(diffPath, out _);

        if (!BcdService.ApplyBootLayout(config, diffPath, config.BootMenuMode, out var bcdMsg))
        {
            message = bcdMsg;
            return false;
        }

        config.NextBootSlot = slot;
        config.Save();
        message = "Boot eingerichtet (Diff-" + slot + ").\n" + bcdMsg;
        return true;
    }

    public bool CloneSystemDriveToLetter(char targetLetter, IProgress<string>? progress, out string message)
    {
        if (!AdminHelper.IsAdministrator())
        {
            message = "Administratorrechte nötig.";
            return false;
        }

        var destRoot = char.ToUpperInvariant(targetLetter) + ":\\";
        if (!Directory.Exists(destRoot))
        {
            message = "Ziel-Laufwerk nicht gefunden: " + destRoot;
            return false;
        }

        progress?.Report("Schattenkopie von C: wird erzeugt…");
        if (!TryCreateShadowCopy(@"C:\", out var shadowRoot, out var shadowError))
        {
            message = "VSS-Schattenkopie fehlgeschlagen: " + shadowError;
            return false;
        }

        try
        {
            progress?.Report("Kopiere Windows auf die VHD (kann lange dauern)…");
            var log = Path.Combine(Path.GetTempPath(), "frischstart-robocopy.log");
            // /MIR /XJ /COPY:DATSO /R:1 /W:1 — konsistenter Klon über Schattenkopie
            var args =
                $"\"{shadowRoot}\" \"{destRoot}\" /MIR /XJ /COPY:DATSO /DCOPY:DAT /R:1 /W:1 " +
                $"/XD \"System Volume Information\" \"$RECYCLE.BIN\" \"Recovery\" " +
                $"/XF pagefile.sys swapfile.sys hiberfil.sys /NFL /NDL /NP /LOG:\"{log}\"";

            var (code, output) = ProcessRunner.Run("robocopy.exe", args, timeoutMs: 6 * 60 * 60 * 1000);
            // robocopy: 0–7 = Erfolg mit verschiedenen Bit-Flags
            if (code >= 8)
            {
                message = "robocopy fehlgeschlagen (Code " + code + "): " + output;
                return false;
            }

            if (!Directory.Exists(Path.Combine(destRoot, "Windows")))
            {
                message = "Klon scheint unvollständig (kein Windows-Ordner auf Ziel).";
                return false;
            }

            message = "System wurde auf die VHD kopiert.";
            return true;
        }
        finally
        {
            TryDeleteAllShadowCopies();
        }
    }

    private static string? FindWindowsPathOnAttachedVhds()
    {
        foreach (var d in DriveInfo.GetDrives())
        {
            if (!d.IsReady || d.DriveType != DriveType.Fixed)
                continue;
            var candidate = Path.Combine(d.RootDirectory.FullName, "Windows");
            if (Directory.Exists(candidate) &&
                !string.Equals(d.Name, @"C:\", StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        // Fallback: jeder Windows-Ordner außer C:
        foreach (var d in DriveInfo.GetDrives())
        {
            if (!d.IsReady) continue;
            var candidate = Path.Combine(d.RootDirectory.FullName, "Windows");
            if (Directory.Exists(candidate) && d.Name.StartsWith("C", StringComparison.OrdinalIgnoreCase) == false)
                return candidate;
        }

        return null;
    }

    private static bool RunDiskPart(string scriptBody, out string message)
    {
        var script = Path.Combine(Path.GetTempPath(), "frischstart-setup-" + Guid.NewGuid().ToString("N") + ".txt");
        try
        {
            File.WriteAllText(script, scriptBody.Replace("\r\n", "\n").Replace("\n", Environment.NewLine));
            var (code, output) = ProcessRunner.Run("diskpart.exe", $"/s \"{script}\"");
            if (code != 0 ||
                output.Contains("Fehler", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("error", StringComparison.OrdinalIgnoreCase))
            {
                message = output;
                return false;
            }

            message = output;
            return true;
        }
        finally
        {
            try { File.Delete(script); } catch { /* ignore */ }
        }
    }

    private static bool TryCreateShadowCopy(string volume, out string shadowRoot, out string error)
    {
        shadowRoot = string.Empty;
        error = string.Empty;
        try
        {
            using var cls = new ManagementClass("Win32_ShadowCopy");
            var inParams = cls.GetMethodParameters("Create");
            inParams["Volume"] = volume;
            inParams["Context"] = "ClientAccessible";
            var outParams = cls.InvokeMethod("Create", inParams, null);
            var ret = Convert.ToUInt32(outParams["ReturnValue"]);
            if (ret != 0)
            {
                error = "Win32_ShadowCopy.Create ReturnValue=" + ret;
                return false;
            }

            var shadowId = outParams["ShadowID"]?.ToString();
            using var searcher = new ManagementObjectSearcher(
                string.IsNullOrEmpty(shadowId)
                    ? "SELECT * FROM Win32_ShadowCopy"
                    : $"SELECT * FROM Win32_ShadowCopy WHERE ID='{shadowId}'");

            foreach (ManagementObject mo in searcher.Get())
            {
                var device = mo["DeviceObject"]?.ToString();
                if (string.IsNullOrWhiteSpace(device))
                    continue;
                shadowRoot = device.TrimEnd('\\') + "\\";
                return true;
            }

            error = "Keine Schattenkopie gefunden.";
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static void TryDeleteAllShadowCopies()
    {
        try
        {
            ProcessRunner.Run("vssadmin.exe", "delete shadows /for=C: /Quiet");
        }
        catch
        {
            // ignore
        }
    }

    public static char? FindFreeDriveLetter(params char[] preferred)
    {
        var used = new HashSet<char>(
            DriveInfo.GetDrives().Select(d => char.ToUpperInvariant(d.Name[0])));

        foreach (var p in preferred)
        {
            var c = char.ToUpperInvariant(p);
            if (!used.Contains(c))
                return c;
        }

        for (var c = 'Z'; c >= 'D'; c--)
        {
            if (!used.Contains(c))
                return c;
        }

        return null;
    }
}

public sealed record DriveChoice(
    string Root,
    string Label,
    long TotalBytes,
    long FreeBytes,
    DriveType DriveType)
{
    public string Display =>
        $"{Root}  {(string.IsNullOrWhiteSpace(Label) ? "" : Label + "  ")}" +
        $"frei {FreeBytes / 1024d / 1024d / 1024d:0.0} GB / {TotalBytes / 1024d / 1024d / 1024d:0.0} GB";
}
