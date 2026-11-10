using System.Runtime.InteropServices;

namespace FrischStart.Core;

public static class ProtectQuery
{
    /// <summary>
    /// Fragt den Treiber, ob mindestens ein Volume geschützt ist.
    /// Ist der Treiber nicht geladen, gilt das als „nicht geschützt“.
    /// </summary>
    public static bool TryQueryIsProtecting(out bool isProtecting)
    {
        isProtecting = false;
        var handle = CreateFileW(
            ProtectConstants.ControlDevicePath,
            0x80000000, // GENERIC_READ
            0,
            IntPtr.Zero,
            3, // OPEN_EXISTING
            0,
            IntPtr.Zero);

        if (handle == new IntPtr(-1) || handle == IntPtr.Zero)
            return true; // Treiber nicht geladen → nicht geschützt

        try
        {
            byte result = 0;
            var ok = DeviceIoControl(
                handle,
                ProtectConstants.IoctlQueryProtectStatus,
                IntPtr.Zero,
                0,
                ref result,
                1,
                out _,
                IntPtr.Zero);

            if (!ok)
                return false;

            isProtecting = result != 0;
            return true;
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateFileW(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        IntPtr hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        uint nInBufferSize,
        ref byte lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
}
