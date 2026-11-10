using System.Text;
using System.Windows;
using System.Windows.Controls;
using FrischStart.Core;

namespace FrischStart.App;

public partial class MainWindow : Window
{
    private readonly ProtectService _service = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshUi();
    }

    private void RefreshUi()
    {
        var status = _service.GetStatus();
        var volumes = VolumeEnumerator.GetFixedNtfsVolumes();

        StatusTitle.Text = status.IsProtecting
            ? "Schutz ist aktiv (bzw. vorbereitet)"
            : "Schutz ist aus";

        var sb = new StringBuilder();
        sb.AppendLine(AdminHelper.IsAdministrator()
            ? "Als Administrator gestartet."
            : "Nicht als Administrator — bitte neu mit Rechtsklick „Als Administrator“ starten.");
        sb.AppendLine(status.TestSigningEnabled
            ? "Testmodus: an"
            : "Testmodus: aus (für unsignierte Treiber nötig)");
        sb.AppendLine(status.DriverServiceInstalled
            ? "Treiberdienst: installiert"
            : "Treiberdienst: noch nicht installiert");
        sb.AppendLine(status.DriverFilesPresent
            ? "Treiberdateien: gefunden"
            : "Treiberdateien: fehlen — " + status.DriverHint);
        sb.AppendLine(status.SystemDriveConfigured switch
        {
            true => "C: Statusdatei: Schutz = an",
            false => "C: Statusdatei: Schutz = aus",
            _ => "C: Statusdatei: nicht vorhanden"
        });
        StatusDetails.Text = sb.ToString().TrimEnd();

        VolumeList.Items.Clear();
        foreach (var vol in volumes)
        {
            var role = vol.LooksLikeSystemDrive
                ? "→ wird geschützt (System)"
                : "→ bleibt ungeschützt (Dateien hier speichern)";
            VolumeList.Items.Add(new TextBlock
            {
                Text = $"{vol.DisplayName}  {role}",
                Margin = new Thickness(0, 4, 0, 0),
                FontSize = 16
            });
        }

        BtnEnable.IsEnabled = !status.IsProtecting;
        BtnDisable.IsEnabled = status.IsProtecting || status.SystemDriveConfigured == true;
    }

    private void BtnTestSigning_OnClick(object sender, RoutedEventArgs e)
    {
        if (_service.EnableTestSigning(out var msg))
        {
            MessageBox.Show(msg + "\n\nJetzt neu starten?", "FrischStart",
                MessageBoxButton.OK, MessageBoxImage.Information);
            if (AskReboot())
                TryReboot();
        }
        else
        {
            MessageText.Text = msg;
        }

        RefreshUi();
    }

    private void BtnEnable_OnClick(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "Alles auf C: geht nach dem nächsten Neustart wieder verloren,\n" +
            "sobald der Schutz aktiv ist.\n\n" +
            "Liegen Fotos und Dokumente schon auf der externen Platte?\n\n" +
            "Schutz für C: jetzt einschalten?",
            "FrischStart — Bestätigung",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
            return;

        if (_service.EnableSystemDriveProtection(out var msg))
        {
            MessageText.Text = msg;
            if (AskReboot())
                TryReboot();
        }
        else
        {
            MessageText.Text = msg;
            MessageBox.Show(msg, "FrischStart", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        RefreshUi();
    }

    private void BtnDisable_OnClick(object sender, RoutedEventArgs e)
    {
        if (_service.DisableProtection(out var msg))
        {
            MessageText.Text = msg;
            if (AskReboot())
                TryReboot();
        }
        else
        {
            MessageText.Text = msg;
            MessageBox.Show(msg, "FrischStart", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        RefreshUi();
    }

    private void BtnRefresh_OnClick(object sender, RoutedEventArgs e) => RefreshUi();

    private static bool AskReboot() =>
        MessageBox.Show("Jetzt neu starten?", "FrischStart",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    private static void TryReboot()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "shutdown.exe",
                Arguments = "/r /t 5 /c \"FrischStart: Neustart für Schutz\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Neustart konnte nicht ausgelöst werden: " + ex.Message,
                "FrischStart", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
