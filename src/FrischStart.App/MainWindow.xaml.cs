using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using FrischStart.Core;
using FrischStart.Core.Vhd;

namespace FrischStart.App;

public partial class MainWindow : Window
{
    private readonly VhdProtectService _vhd = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshUi();
    }

    private void RefreshUi()
    {
        var status = _vhd.GetStatus();
        var volumes = VolumeEnumerator.GetFixedNtfsVolumes();

        StatusTitle.Text = status.Enabled
            ? "VHD-Schutz ist aktiv"
            : "VHD-Schutz ist aus";

        var sb = new StringBuilder();
        sb.AppendLine(AdminHelper.IsAdministrator()
            ? "Als Administrator gestartet."
            : "Nicht als Administrator — bitte mit Rechtsklick als Admin starten.");
        sb.AppendLine(status.BootsFromVhd
            ? "Boot: von VHD (gut)"
            : "Boot: noch nicht von VHD — Setup nötig");
        sb.AppendLine(status.ParentExists
            ? "Parent golden.vhdx: gefunden"
            : "Parent golden.vhdx: fehlt → " + status.Config.ParentPath);
        sb.AppendLine($"Diff A/B: {(status.DiffAExists ? "A ok" : "A fehlt")} / {(status.DiffBExists ? "B ok" : "B fehlt")}");
        sb.AppendLine(status.ActiveSlot is null
            ? "Aktiver Slot: unbekannt"
            : "Aktiver Slot: " + status.ActiveSlot);
        sb.AppendLine(status.TaskInstalled
            ? "Autostart-Reset: an"
            : "Autostart-Reset: aus");
        sb.AppendLine("BCD: " + status.OsDeviceLine.Trim());
        StatusDetails.Text = sb.ToString().TrimEnd();

        VolumeList.Items.Clear();
        foreach (var vol in volumes)
        {
            var role = vol.LooksLikeSystemDrive
                ? "→ System (in der VHD, wird zurückgesetzt)"
                : "→ Dateien hier speichern (bleibt erhalten)";
            VolumeList.Items.Add(new TextBlock
            {
                Text = $"{vol.DisplayName}  {role}",
                Margin = new Thickness(0, 4, 0, 0),
                FontSize = 15
            });
        }

        BtnEnable.IsEnabled = !status.Enabled;
        BtnDisable.IsEnabled = status.Enabled || status.TaskInstalled;
        BtnResetNow.IsEnabled = status.ParentExists;
    }

    private void BtnInstall_OnClick(object sender, RoutedEventArgs e)
    {
        var wizard = new SetupWizardWindow { Owner = this };
        wizard.ShowDialog();
        RefreshUi();
    }

    private void BtnEnable_OnClick(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "VHD-Schutz aktivieren?\n\n" +
            "Voraussetzung: Windows bootet bereits von Diff-VHD.\n" +
            "Fotos/Dokumente müssen auf der externen Platte liegen.\n\n" +
            "Nach dem nächsten Neustart ist das System wieder sauber.",
            "FrischStart",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
            return;

        if (_vhd.Enable(out var msg))
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

    private void BtnResetNow_OnClick(object sender, RoutedEventArgs e)
    {
        if (_vhd.RunResetNow(out var msg))
        {
            MessageText.Text = msg;
            MessageBox.Show(msg, "FrischStart", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageText.Text = msg;
            MessageBox.Show(msg, "FrischStart", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        RefreshUi();
    }

    private void BtnDisable_OnClick(object sender, RoutedEventArgs e)
    {
        if (_vhd.Disable(out var msg))
            MessageText.Text = msg;
        else
            MessageBox.Show(msg, "FrischStart", MessageBoxButton.OK, MessageBoxImage.Warning);

        RefreshUi();
    }

    private void BtnOpenDocs_OnClick(object sender, RoutedEventArgs e)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "docs", "VHD-SETUP.md"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "FrischStart", "docs", "VHD-SETUP.md"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "VHD-SETUP.md")),
            Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "docs", "VHD-SETUP.md")),
            Path.Combine(VhdConfig.ConfigDirectory, "VHD-SETUP.md")
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path))
                continue;
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            return;
        }

        MessageBox.Show(
            "Bitte im Repo die Datei docs/VHD-SETUP.md öffnen.\n" +
            "Kurz: Windows von Differencing-VHD booten, dann hier Schutz aktivieren.",
            "FrischStart",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void BtnRefresh_OnClick(object sender, RoutedEventArgs e) => RefreshUi();

    private static bool AskReboot() =>
        MessageBox.Show("Jetzt neu starten?", "FrischStart",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    private static void TryReboot()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "shutdown.exe",
                Arguments = "/r /t 5 /c \"FrischStart: Neustart für VHD-Schutz\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Neustart fehlgeschlagen: " + ex.Message,
                "FrischStart", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
