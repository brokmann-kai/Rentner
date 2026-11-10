namespace FrischStart.Core.Vhd;

public sealed class VhdProtectService
{
    public VhdStatus GetStatus()
    {
        var cfg = VhdConfig.LoadOrDefault();
        return new VhdStatus(
            Enabled: cfg.Enabled && ScheduledTaskService.IsInstalled(),
            Config: cfg,
            ParentExists: File.Exists(cfg.ParentPath),
            DiffAExists: File.Exists(cfg.DiffAPath),
            DiffBExists: File.Exists(cfg.DiffBPath),
            BootsFromVhd: BcdService.CurrentOsLooksLikeVhd(),
            ActiveSlot: BcdService.DetectActiveSlot(cfg),
            TaskInstalled: ScheduledTaskService.IsInstalled(),
            OsDeviceLine: BcdService.GetCurrentOsDevice());
    }

    public bool Enable(out string message)
    {
        if (!AdminHelper.IsAdministrator())
        {
            message = "Bitte als Administrator starten.";
            return false;
        }

        var cfg = VhdConfig.LoadOrDefault();
        if (!DiskPartService.EnsureDifferencingPair(cfg, out message))
            return false;

        if (!BcdService.CurrentOsLooksLikeVhd())
        {
            message =
                "Windows bootet aktuell nicht von einer VHD.\n" +
                "Zuerst Setup laut docs/VHD-SETUP.md durchführen, dann Schutz aktivieren.\n" +
                "Aktuell: " + BcdService.GetCurrentOsDevice();
            return false;
        }

        var worker = EnsureWorkerScript(out var workerMsg);
        if (worker is null)
        {
            message = workerMsg;
            return false;
        }

        // Sofort einmal vorbereiten
        if (!DifferencingResetService.PrepareNextCleanBoot(cfg, out var prepMsg))
        {
            message = prepMsg;
            return false;
        }

        if (!ScheduledTaskService.InstallStartupResetTask(worker, out var taskMsg))
        {
            message = taskMsg;
            return false;
        }

        cfg.Enabled = true;
        cfg.Save();

        message = "VHD-Schutz ist aktiv.\n" + prepMsg + "\n" + taskMsg +
                  "\n\nBitte neu starten. Danach ist jede Session nach dem nächsten Neustart wieder sauber.";
        return true;
    }

    public bool Disable(out string message)
    {
        if (!AdminHelper.IsAdministrator())
        {
            message = "Bitte als Administrator starten.";
            return false;
        }

        ScheduledTaskService.RemoveStartupResetTask(out var taskMsg);
        var cfg = VhdConfig.LoadOrDefault();
        cfg.Enabled = false;
        cfg.Save();
        message = "VHD-Schutz deaktiviert. " + taskMsg +
                  " Windows bootet weiter von der aktuellen VHD, Diffs werden nicht mehr zurückgesetzt.";
        return true;
    }

    public bool RunResetNow(out string message)
    {
        if (!AdminHelper.IsAdministrator())
        {
            message = "Bitte als Administrator starten.";
            return false;
        }

        var cfg = VhdConfig.LoadOrDefault();
        return DifferencingResetService.PrepareNextCleanBoot(cfg, out message);
    }

    private static string? EnsureWorkerScript(out string message)
    {
        var dir = VhdConfig.ConfigDirectory;
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "ResetDiff.ps1");

        // Eigenständiges Minimal-Skript (diskpart + bcdedit) anhand der JSON-Config.
        var script = string.Join(Environment.NewLine, new[]
        {
            "$ErrorActionPreference = 'Stop'",
            "$cfgPath = Join-Path $env:ProgramData 'FrischStart\\vhd-config.json'",
            "if (-not (Test-Path $cfgPath)) { exit 0 }",
            "$cfg = Get-Content $cfgPath -Raw | ConvertFrom-Json",
            "if (-not $cfg.Enabled) { exit 0 }",
            "$store = $cfg.StorePath",
            "$parent = Join-Path $store $cfg.ParentFileName",
            "$diffA = Join-Path $store $cfg.DiffAFileName",
            "$diffB = Join-Path $store $cfg.DiffBFileName",
            "$out = bcdedit /enum '{current}' | Out-String",
            // In Wartung (Parent) keinen Diff-Reset — sonst kaputt Admin-Session
            "if ($out -match [regex]::Escape($cfg.ParentFileName)) { exit 0 }",
            "$active = $cfg.NextBootSlot",
            "if ($out -match 'diff-a\\.vhdx') { $active = 'A' }",
            "if ($out -match 'diff-b\\.vhdx') { $active = 'B' }",
            "if ($out -notmatch 'diff-[ab]\\.vhdx') { exit 0 }",
            "$next = if ($active -eq 'A') { 'B' } else { 'A' }",
            "$nextPath = if ($next -eq 'A') { $diffA } else { $diffB }",
            "if (Test-Path $nextPath) { attrib -r -s -h $nextPath -ErrorAction SilentlyContinue; Remove-Item -Force $nextPath }",
            "$dp = [IO.Path]::GetTempFileName()",
            "Set-Content -Path $dp -Encoding ASCII -Value \"create vdisk file=`\"$nextPath`\" parent=`\"$parent`\"`r`nexit\"",
            "diskpart /s $dp | Out-Null",
            "Remove-Item $dp -Force -ErrorAction SilentlyContinue",
            "$root = [IO.Path]::GetPathRoot($nextPath).TrimEnd('\\')",
            "$rel = $nextPath.Substring($root.Length).TrimStart('\\')",
            "$spec = \"vhd=[$($root.TrimEnd(':')):]\\$rel\"",
            "bcdedit /set '{current}' device $spec | Out-Null",
            "bcdedit /set '{current}' osdevice $spec | Out-Null",
            "$cfg.NextBootSlot = $next",
            "($cfg | ConvertTo-Json -Depth 5) | Set-Content -Encoding UTF8 $cfgPath"
        });

        File.WriteAllText(path, script);
        message = "Worker-Skript bereit: " + path;
        return path;
    }
}

public sealed record VhdStatus(
    bool Enabled,
    VhdConfig Config,
    bool ParentExists,
    bool DiffAExists,
    bool DiffBExists,
    bool BootsFromVhd,
    string? ActiveSlot,
    bool TaskInstalled,
    string OsDeviceLine);
