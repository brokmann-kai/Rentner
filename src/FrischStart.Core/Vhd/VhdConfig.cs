using System.Text.Json;
using System.Text.Json.Serialization;

namespace FrischStart.Core.Vhd;

public sealed class VhdConfig
{
    public bool Enabled { get; set; }

    /// <summary>Ordner mit golden.vhdx und Diffs, z. B. D:\FrischStart</summary>
    public string StorePath { get; set; } = @"D:\FrischStart";

    public string ParentFileName { get; set; } = "golden.vhdx";
    public string DiffAFileName { get; set; } = "diff-a.vhdx";
    public string DiffBFileName { get; set; } = "diff-b.vhdx";

    /// <summary>A oder B — welcher Diff als Nächstes gebootet werden soll.</summary>
    public string NextBootSlot { get; set; } = "A";

    /// <summary>Startmenü-Verhalten (Assistent).</summary>
    public BootMenuMode BootMenuMode { get; set; } = BootMenuMode.Menu5Seconds;

    public int BootTimeoutSeconds { get; set; } = 5;

    [JsonIgnore]
    public string ParentPath => Path.Combine(StorePath, ParentFileName);

    [JsonIgnore]
    public string DiffAPath => Path.Combine(StorePath, DiffAFileName);

    [JsonIgnore]
    public string DiffBPath => Path.Combine(StorePath, DiffBFileName);

    public static string ConfigDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "FrischStart");

    public static string ConfigFilePath => Path.Combine(ConfigDirectory, "vhd-config.json");

    public static VhdConfig LoadOrDefault()
    {
        try
        {
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                return JsonSerializer.Deserialize<VhdConfig>(json) ?? new VhdConfig();
            }
        }
        catch
        {
            // ignore corrupt config
        }

        return new VhdConfig();
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDirectory);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigFilePath, json);
    }

    public string PathForSlot(string slot) =>
        string.Equals(slot, "B", StringComparison.OrdinalIgnoreCase) ? DiffBPath : DiffAPath;

    public static string OtherSlot(string slot) =>
        string.Equals(slot, "B", StringComparison.OrdinalIgnoreCase) ? "A" : "B";
}
