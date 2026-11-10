using System.Runtime.InteropServices;
using System.Text;

namespace FrischStart.Core;

public static class VolumeEnumerator
{
    public static IReadOnlyList<VolumeInfo> GetFixedNtfsVolumes()
    {
        var result = new List<VolumeInfo>();
        var buffer = new char[512];
        var len = GetLogicalDriveStringsW(buffer.Length, buffer);
        if (len == 0)
            return result;

        var offset = 0;
        while (offset < len && buffer[offset] != '\0')
        {
            var end = offset;
            while (end < len && buffer[end] != '\0')
                end++;

            var drive = new string(buffer, offset, end - offset);
            offset = end + 1;

            if (drive.Length < 2)
                continue;

            var root = drive.EndsWith('\\') ? drive : drive + "\\";
            var type = (DriveType)GetDriveTypeW(root);
            if (type != DriveType.Fixed)
                continue;

            var fs = new StringBuilder(64);
            var label = new StringBuilder(64);
            if (!GetVolumeInformationW(root, label, label.Capacity, out _, out _, out _, fs, fs.Capacity))
                continue;

            var info = new VolumeInfo(char.ToUpperInvariant(drive[0]), fs.ToString(), label.ToString(), type);
            if (info.IsNtfs)
                result.Add(info);
        }

        return result;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetLogicalDriveStringsW(int nBufferLength, [Out] char[] lpBuffer);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern uint GetDriveTypeW(string lpRootPathName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetVolumeInformationW(
        string lpRootPathName,
        StringBuilder lpVolumeNameBuffer,
        int nVolumeNameSize,
        out uint lpVolumeSerialNumber,
        out uint lpMaximumComponentLength,
        out uint lpFileSystemFlags,
        StringBuilder lpFileSystemNameBuffer,
        int nFileSystemNameSize);
}
