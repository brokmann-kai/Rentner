namespace FrischStart.Core;

public sealed record VolumeInfo(
    char DriveLetter,
    string FileSystem,
    string Label,
    DriveType DriveType)
{
    public string RootPath => $"{DriveLetter}:\\";

    public string DisplayName
    {
        get
        {
            var name = string.IsNullOrWhiteSpace(Label) ? "Lokal" : Label;
            return $"{DriveLetter}: ({name}) — {FileSystem}";
        }
    }

    public bool IsNtfs =>
        string.Equals(FileSystem, "NTFS", StringComparison.OrdinalIgnoreCase);

    public bool LooksLikeSystemDrive =>
        char.ToUpperInvariant(DriveLetter) == 'C';
}
