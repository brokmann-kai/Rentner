namespace FrischStart.Core;

/// <summary>
/// Orchestriert den Senioren-Standardfall: nur Systemplatte (C:) schützen.
/// </summary>
public sealed class ProtectService
{
    public ProtectStatus GetStatus()
    {
        ProtectQuery.TryQueryIsProtecting(out var active);
        var cState = ProtectStateFile.ReadEnabled('C');
        var testSigning = TestSigningService.IsTestSigningEnabled();
        var service = DriverInstaller.IsServiceInstalled();
        var hasDriverFiles = DriverLocator.TryFindDriverFiles(out _, out _, out var hint);

        return new ProtectStatus(
            IsProtecting: active || cState == true,
            TestSigningEnabled: testSigning,
            DriverServiceInstalled: service,
            DriverFilesPresent: hasDriverFiles,
            DriverHint: hint,
            SystemDriveConfigured: cState);
    }

    public bool EnableSystemDriveProtection(out string message)
    {
        if (!AdminHelper.IsAdministrator())
        {
            message = "Bitte FrischStart als Administrator starten.";
            return false;
        }

        if (!TestSigningService.IsTestSigningEnabled())
        {
            message = "Testmodus ist aus. Zuerst „Testmodus aktivieren“, dann neu starten.";
            return false;
        }

        if (!DriverLocator.TryFindDriverFiles(out _, out var inf, out var hint))
        {
            message = hint;
            return false;
        }

        if (!DriverInstaller.IsServiceInstalled())
        {
            if (!DriverInstaller.Install(inf, out var installError))
            {
                message = installError;
                return false;
            }
        }

        if (!ProtectStateFile.EnableOnVolumes(new[] { 'C' }, out var fileError))
        {
            message = fileError;
            return false;
        }

        message = "Schutz für C: vorbereitet. Jetzt neu starten — danach ist der Schutz aktiv. " +
                  "Dateien bitte auf der externen Festplatte ablegen.";
        return true;
    }

    public bool DisableProtection(out string message)
    {
        if (!AdminHelper.IsAdministrator())
        {
            message = "Bitte FrischStart als Administrator starten.";
            return false;
        }

        if (!ProtectStateFile.DisableOnAllFixedVolumes(out var error))
        {
            message = error;
            return false;
        }

        message = "Schutz wird beim nächsten Neustart deaktiviert. Bitte jetzt neu starten.";
        return true;
    }

    public bool EnableTestSigning(out string message) =>
        TestSigningService.EnableTestSigning(out message);
}

public sealed record ProtectStatus(
    bool IsProtecting,
    bool TestSigningEnabled,
    bool DriverServiceInstalled,
    bool DriverFilesPresent,
    string DriverHint,
    bool? SystemDriveConfigured);
