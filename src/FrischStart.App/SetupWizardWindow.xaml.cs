using System.Windows;
using System.Windows.Controls;
using FrischStart.Core;
using FrischStart.Core.Vhd;

namespace FrischStart.App;

public partial class SetupWizardWindow : Window
{
    private enum Step
    {
        Welcome,
        ChoosePath,
        ChooseStore,
        PathA_Prepare,
        PathA_WaitInstall,
        PathB_Clone,
        ChooseBoot,
        FinalizeBoot,
        EnableProtect,
        Done
    }

    private enum SetupPath { None, FreshInstall, CloneExisting }

    private Step _step = Step.Welcome;
    private SetupPath _path = SetupPath.None;
    private readonly VhdSetupService _setup = new();
    private readonly VhdProtectService _protect = new();
    private VhdConfig _config = VhdConfig.LoadOrDefault();
    private DriveChoice? _store;
    private int _sizeGb = 80;
    private char _mountLetter = 'V';
    private bool _busy;
    private bool _bootChoiceMade;

    public SetupWizardWindow()
    {
        InitializeComponent();
        Render();
    }

    private void SetProgress(string text) =>
        Dispatcher.Invoke(() => ProgressText.Text = text);

    private void Render()
    {
        OptionsPanel.Children.Clear();
        ProgressText.Text = string.Empty;
        BtnBack.IsEnabled = _step is not Step.Welcome and not Step.Done && !_busy;
        BtnNext.IsEnabled = !_busy;

        switch (_step)
        {
            case Step.Welcome:
                StepTitle.Text = "Willkommen";
                StepBody.Text =
                    "FrischStart macht den PC nach jedem Neustart wieder sauber.\n\n" +
                    "Fotos und Dokumente bitte auf einer externen Festplatte speichern.\n" +
                    "Die Einrichtung dauert je nach Weg ca. 20 Minuten bis einige Stunden.";
                BtnNext.Content = "Weiter";
                break;

            case Step.ChoosePath:
                StepTitle.Text = "Welchen Weg möchten Sie?";
                StepBody.Text = "Beide Wege sind Open Source und brauchen kein Zertifikat.";
                AddPathButton(
                    "Bestehendes Windows übernehmen (empfohlen)",
                    "Aktuelles System wird in eine VHD kopiert. Keine Neuinstallation.",
                    SetupPath.CloneExisting);
                AddPathButton(
                    "Neue Windows-Installation in VHD",
                    "Sauberer Start. Windows-Installationsstick nötig.",
                    SetupPath.FreshInstall);
                BtnNext.Content = "Weiter";
                break;

            case Step.ChooseStore:
                StepTitle.Text = "Wo sollen die VHDs liegen?";
                StepBody.Text =
                    "Platte mit viel freiem Speicher wählen (am besten zweite SSD/Datenpartition).\n" +
                    "Nicht die Systemplatte vollpacken.";
                var usedGb = Math.Max(40,
                    (int)Math.Ceiling(_setup.EstimateUsedBytesOnSystemDrive() / 1024d / 1024d / 1024d) + 25);
                _sizeGb = Math.Clamp(usedGb, 60, 400);
                foreach (var d in _setup.ListCandidateStores())
                {
                    var local = d;
                    var btn = new Button
                    {
                        Content = local.Display,
                        HorizontalContentAlignment = HorizontalAlignment.Left
                    };
                    btn.Click += (_, _) =>
                    {
                        _store = local;
                        _config.StorePath = System.IO.Path.Combine(local.Root + "\\", "FrischStart");
                        ClearOptionHighlights();
                        btn.Background = System.Windows.Media.Brushes.LightGreen;
                    };
                    OptionsPanel.Children.Add(btn);
                }

                OptionsPanel.Children.Add(new TextBlock
                {
                    Text = $"Geplante VHD-Größe: {_sizeGb} GB",
                    Margin = new Thickness(0, 12, 0, 0),
                    FontSize = 15
                });
                BtnNext.Content = "Weiter";
                break;

            case Step.PathA_Prepare:
                StepTitle.Text = "VHD für Neuinstallation vorbereiten";
                StepBody.Text =
                    $"Speicherort: {_config.StorePath}\nGröße: {_sizeGb} GB\n\n" +
                    "Es wird eine leere Parent-VHD erzeugt und als Laufwerk bereitgestellt.\n" +
                    "Danach Windows von USB auf dieses Laufwerk installieren.";
                BtnNext.Content = "VHD jetzt erzeugen";
                break;

            case Step.PathA_WaitInstall:
                StepTitle.Text = "Windows auf die VHD installieren";
                StepBody.Text =
                    "1. Windows-Installationsstick anschließen\n" +
                    "2. PC neu starten und vom Stick booten\n" +
                    $"3. Partition „FrischStartOS“ / Laufwerk {_mountLetter}: wählen\n" +
                    "4. Windows installieren und einmal einrichten\n" +
                    "5. Herunterfahren und wieder in dieses Windows booten\n" +
                    "6. Hier auf Weiter klicken";
                BtnNext.Content = "Windows ist fertig — weiter";
                break;

            case Step.PathB_Clone:
                StepTitle.Text = "Bestehendes Windows übernehmen";
                StepBody.Text =
                    $"Speicherort: {_config.StorePath}\nGröße: {_sizeGb} GB\n\n" +
                    "C: wird per Schattenkopie in die VHD kopiert (oft 30–90 Min).\n" +
                    "PC nicht ausschalten.";
                BtnNext.Content = "Klon jetzt starten";
                break;

            case Step.ChooseBoot:
                StepTitle.Text = "Startreihenfolge / Boot-Menü";
                StepBody.Text =
                    "Wie soll der PC starten?\n\n" +
                    "Admin-Zugang zur Wartung (Dump ändern / nachinstallieren):\n" +
                    "• Mit Menü: beim Start „FrischStart Wartung“ wählen (Pfeiltasten + Enter)\n" +
                    "• Ohne Menü: Shift halten + Neu starten → erweiterte Optionen\n" +
                    "• Es gibt keine eigene F-Taste nur für FrischStart (F8 ist unter UEFI meist aus).";
                AddBootOption(BootMenuMode.SeniorAuto);
                AddBootOption(BootMenuMode.Menu3Seconds);
                AddBootOption(BootMenuMode.Menu5Seconds);
                AddBootOption(BootMenuMode.Menu10Seconds);
                AddBootOption(BootMenuMode.MaintenanceDefault);
                BtnNext.Content = "Weiter";
                break;

            case Step.FinalizeBoot:
                StepTitle.Text = "Boot einrichten";
                StepBody.Text =
                    $"Gewählt: {BootPolicyInfo.Title(_config.BootMenuMode)}\n" +
                    "Diff-VHDs erzeugen, Einträge „geschützt“ + „Wartung“ setzen.";
                BtnNext.Content = "Boot jetzt einrichten";
                break;

            case Step.EnableProtect:
                StepTitle.Text = "Schutz einschalten";
                StepBody.Text =
                    "Autostart-Reset aktivieren.\n" +
                    "In Wartung wird der Dump (Parent) nicht zurückgesetzt — dort Updates/Nachinstallationen machen.";
                BtnNext.Content = "Schutz aktivieren";
                break;

            case Step.Done:
                StepTitle.Text = "Fertig";
                var tip = _config.BootTimeoutSeconds > 0
                    ? "Beim Start ggf. kurz „FrischStart Wartung“ wählen (Pfeiltasten), um den Dump zu ändern."
                    : "Kein Boot-Menü: Admin über Shift+Neu starten oder Timeout in der Config erhöhen.";
                StepBody.Text =
                    "Einrichtung abgeschlossen.\n\n" +
                    $"Start: {BootPolicyInfo.Title(_config.BootMenuMode)}\n" +
                    tip + "\n\n" +
                    "Rentner: einfach neu starten = wieder sauber.";
                BtnNext.Content = "Schließen";
                BtnBack.IsEnabled = false;
                break;
        }
    }

    private void ClearOptionHighlights()
    {
        foreach (UIElement child in OptionsPanel.Children)
        {
            if (child is Button b) b.ClearValue(BackgroundProperty);
        }
    }

    private void AddBootOption(BootMenuMode mode)
    {
        var btn = new Button
        {
            Margin = new Thickness(0, 0, 0, 10),
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = BootPolicyInfo.Title(mode), FontSize = 17, FontWeight = FontWeights.SemiBold },
                    new TextBlock
                    {
                        Text = BootPolicyInfo.Description(mode),
                        FontSize = 14,
                        Margin = new Thickness(0, 4, 0, 0),
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        };
        if (_config.BootMenuMode == mode && _bootChoiceMade)
            btn.Background = System.Windows.Media.Brushes.LightGreen;

        btn.Click += (_, _) =>
        {
            _config.BootMenuMode = mode;
            _config.BootTimeoutSeconds = BootPolicyInfo.TimeoutSeconds(mode);
            _bootChoiceMade = true;
            ClearOptionHighlights();
            btn.Background = System.Windows.Media.Brushes.LightGreen;
        };
        OptionsPanel.Children.Add(btn);
    }

    private void AddPathButton(string title, string subtitle, SetupPath path)
    {
        var btn = new Button
        {
            Margin = new Thickness(0, 0, 0, 10),
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeights.SemiBold },
                    new TextBlock
                    {
                        Text = subtitle,
                        FontSize = 14,
                        Margin = new Thickness(0, 4, 0, 0),
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        };
        btn.Click += (_, _) =>
        {
            _path = path;
            ClearOptionHighlights();
            btn.Background = System.Windows.Media.Brushes.LightGreen;
        };
        OptionsPanel.Children.Add(btn);
    }

    private async void BtnNext_OnClick(object sender, RoutedEventArgs e)
    {
        if (_busy) return;

        switch (_step)
        {
            case Step.Welcome:
                _step = Step.ChoosePath;
                break;

            case Step.ChoosePath:
                if (_path == SetupPath.None)
                {
                    MessageBox.Show("Bitte einen Weg auswählen.", "FrischStart");
                    return;
                }

                _step = Step.ChooseStore;
                break;

            case Step.ChooseStore:
                if (_store is null)
                {
                    MessageBox.Show("Bitte einen Speicherort auswählen.", "FrischStart");
                    return;
                }

                var need = _sizeGb * 1024L * 1024L * 1024L;
                if (_store.FreeBytes < need)
                {
                    MessageBox.Show(
                        $"Zu wenig freier Speicher. Benötigt ca. {_sizeGb} GB, frei sind {_store.FreeBytes / 1024d / 1024d / 1024d:0.0} GB.",
                        "FrischStart");
                    return;
                }

                _step = _path == SetupPath.FreshInstall ? Step.PathA_Prepare : Step.PathB_Clone;
                break;

            case Step.PathA_Prepare:
                if (!await RunBusy(() =>
                    {
                        var letter = VhdSetupService.FindFreeDriveLetter('V', 'W', 'X', 'Y', 'Z');
                        if (letter is null)
                        {
                            SetProgress("Kein freier Laufwerksbuchstabe.");
                            return false;
                        }

                        _mountLetter = letter.Value;
                        SetProgress("Erzeuge Parent-VHD…");
                        if (!_setup.PrepareParentVhd(_config, _sizeGb, _mountLetter.ToString(), out var msg))
                        {
                            SetProgress(msg);
                            Dispatcher.Invoke(() =>
                                MessageBox.Show(msg, "FrischStart", MessageBoxButton.OK, MessageBoxImage.Error));
                            return false;
                        }

                        SetProgress(msg);
                        _step = Step.PathA_WaitInstall;
                        return true;
                    }))
                    return;
                break;

            case Step.PathA_WaitInstall:
                _step = Step.ChooseBoot;
                break;

            case Step.PathB_Clone:
                if (!await RunBusy(() =>
                    {
                        var letter = VhdSetupService.FindFreeDriveLetter('V', 'W', 'X', 'Y', 'Z');
                        if (letter is null)
                        {
                            SetProgress("Kein freier Laufwerksbuchstabe.");
                            return false;
                        }

                        _mountLetter = letter.Value;
                        var progress = new Progress<string>(SetProgress);
                        SetProgress("Erzeuge Parent-VHD…");
                        if (!_setup.PrepareParentVhd(_config, _sizeGb, _mountLetter.ToString(), out var prepMsg))
                        {
                            Dispatcher.Invoke(() =>
                                MessageBox.Show(prepMsg, "FrischStart", MessageBoxButton.OK, MessageBoxImage.Error));
                            return false;
                        }

                        if (!_setup.CloneSystemDriveToLetter(_mountLetter, progress, out var cloneMsg))
                        {
                            _setup.DetachVhd(_config.ParentPath, out _);
                            Dispatcher.Invoke(() =>
                                MessageBox.Show(cloneMsg, "FrischStart", MessageBoxButton.OK, MessageBoxImage.Error));
                            return false;
                        }

                        SetProgress(cloneMsg + " — trenne VHD…");
                        _setup.DetachVhd(_config.ParentPath, out _);
                        _step = Step.ChooseBoot;
                        return true;
                    }))
                    return;
                break;

            case Step.ChooseBoot:
                if (!_bootChoiceMade)
                {
                    // Default empfehlen
                    _config.BootMenuMode = BootMenuMode.Menu5Seconds;
                    _config.BootTimeoutSeconds = 5;
                    _bootChoiceMade = true;
                }

                _config.Save();
                _step = Step.FinalizeBoot;
                break;

            case Step.FinalizeBoot:
                if (!await RunBusy(() =>
                    {
                        SetProgress("Trenne Parent falls noch verbunden…");
                        _setup.DetachVhd(_config.ParentPath, out _);

                        SetProgress("Erzeuge Diff-VHDs…");
                        if (!_setup.CreateDiffPair(_config, out var diffMsg))
                        {
                            Dispatcher.Invoke(() =>
                                MessageBox.Show(diffMsg, "FrischStart", MessageBoxButton.OK, MessageBoxImage.Error));
                            return false;
                        }

                        SetProgress("Richte Boot ein…");
                        if (!_setup.ConfigureBootToDiff(_config, "A", out var bootMsg))
                        {
                            Dispatcher.Invoke(() =>
                                MessageBox.Show(bootMsg, "FrischStart", MessageBoxButton.OK, MessageBoxImage.Error));
                            return false;
                        }

                        SetProgress(diffMsg + "\n" + bootMsg);
                        _step = Step.EnableProtect;
                        return true;
                    }))
                    return;
                break;

            case Step.EnableProtect:
                if (!await RunBusy(() =>
                    {
                        SetProgress("Aktiviere Autostart-Reset…");
                        if (_protect.Enable(out var msg))
                        {
                            SetProgress(msg);
                            _step = Step.Done;
                            return true;
                        }

                        var cfg = VhdConfig.LoadOrDefault();
                        cfg.StorePath = _config.StorePath;
                        cfg.ParentFileName = _config.ParentFileName;
                        cfg.DiffAFileName = _config.DiffAFileName;
                        cfg.DiffBFileName = _config.DiffBFileName;
                        cfg.BootMenuMode = _config.BootMenuMode;
                        cfg.BootTimeoutSeconds = _config.BootTimeoutSeconds;
                        cfg.Enabled = true;
                        cfg.Save();
                        SetProgress(msg +
                                    "\n\nNach dem ersten geschützten Start ggf. Schutz noch einmal aktivieren.");
                        _step = Step.Done;
                        return true;
                    }))
                    return;
                break;

            case Step.Done:
                DialogResult = true;
                Close();
                return;
        }

        Render();
    }

    private void BtnBack_OnClick(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        _step = _step switch
        {
            Step.ChoosePath => Step.Welcome,
            Step.ChooseStore => Step.ChoosePath,
            Step.PathA_Prepare => Step.ChooseStore,
            Step.PathA_WaitInstall => Step.PathA_Prepare,
            Step.PathB_Clone => Step.ChooseStore,
            Step.ChooseBoot => _path == SetupPath.FreshInstall ? Step.PathA_WaitInstall : Step.PathB_Clone,
            Step.FinalizeBoot => Step.ChooseBoot,
            Step.EnableProtect => Step.FinalizeBoot,
            _ => _step
        };
        Render();
    }

    private async Task<bool> RunBusy(Func<bool> work)
    {
        _busy = true;
        BtnNext.IsEnabled = false;
        BtnBack.IsEnabled = false;
        try
        {
            return await Task.Run(work);
        }
        finally
        {
            _busy = false;
            BtnNext.IsEnabled = true;
            BtnBack.IsEnabled = _step is not Step.Welcome and not Step.Done;
            Render();
        }
    }
}
