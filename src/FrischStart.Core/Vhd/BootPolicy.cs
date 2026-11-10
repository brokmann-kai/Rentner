namespace FrischStart.Core.Vhd;

/// <summary>
/// Startverhalten für Angehörige / Senior.
/// </summary>
public enum BootMenuMode
{
    /// <summary>Sofort geschützt starten, kein Menü (Timeout 0).</summary>
    SeniorAuto = 0,

    /// <summary>Menü 3 Sekunden, Standard = geschützt.</summary>
    Menu3Seconds = 3,

    /// <summary>Menü 5 Sekunden, Standard = geschützt.</summary>
    Menu5Seconds = 5,

    /// <summary>Menü 10 Sekunden, Standard = geschützt.</summary>
    Menu10Seconds = 10,

    /// <summary>Menü 8 Sekunden, Standard = Wartung (Parent).</summary>
    MaintenanceDefault = 100
}

public static class BootPolicyInfo
{
    public static int TimeoutSeconds(BootMenuMode mode) => mode switch
    {
        BootMenuMode.SeniorAuto => 0,
        BootMenuMode.Menu3Seconds => 3,
        BootMenuMode.Menu5Seconds => 5,
        BootMenuMode.Menu10Seconds => 10,
        BootMenuMode.MaintenanceDefault => 8,
        _ => 5
    };

    public static bool DefaultIsMaintenance(BootMenuMode mode) =>
        mode == BootMenuMode.MaintenanceDefault;

    public static string Title(BootMenuMode mode) => mode switch
    {
        BootMenuMode.SeniorAuto => "Nur geschützt (automatisch)",
        BootMenuMode.Menu3Seconds => "Menü 3 Sekunden",
        BootMenuMode.Menu5Seconds => "Menü 5 Sekunden (empfohlen)",
        BootMenuMode.Menu10Seconds => "Menü 10 Sekunden",
        BootMenuMode.MaintenanceDefault => "Standard = Wartung",
        _ => mode.ToString()
    };

    public static string Description(BootMenuMode mode) => mode switch
    {
        BootMenuMode.SeniorAuto =>
            "Rentner merkt nichts. Admin: Shift+Neustart oder FrischStart-App.",
        BootMenuMode.Menu3Seconds =>
            "Kurz Menü: „FrischStart (geschützt)“ oder „FrischStart Wartung“.",
        BootMenuMode.Menu5Seconds =>
            "Beim Start kurz wählen. Pfeiltasten + Enter. Gut für Angehörige.",
        BootMenuMode.Menu10Seconds =>
            "Längeres Menü — mehr Zeit zum Auswählen der Wartung.",
        BootMenuMode.MaintenanceDefault =>
            "Startet in Wartung (Parent). Nur für Einrichtung/Tests.",
        _ => ""
    };
}
