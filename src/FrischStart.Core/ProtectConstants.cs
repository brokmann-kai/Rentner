namespace FrischStart.Core;

/// <summary>
/// Konstanten aus SysRestoreDriver (QHIoctl.h) — Upstream Apache-2.0, Xuhui Jiang.
/// </summary>
public static class ProtectConstants
{
    public const string ProtectStateFileName = "_qh_protect_state.data";
    public const int ProtectStateFileSize = 1024 * 1024;
    public const string ControlDevicePath = @"\\.\QHEngineControlDevice";
    public const string ServiceName = "SysRestoreDriver";
    public const string DriverFileName = "SysRestoreDriver.sys";
    public const string InfFileName = "SysRestoreDriver.inf";

    // CTL_CODE(0x8000, 0x801, METHOD_BUFFERED, FILE_ANY_ACCESS) = 0x80002004
    public const uint IoctlQueryProtectStatus = 0x80002004u;

    public const string VolumeClassUpperFiltersKey =
        @"SYSTEM\CurrentControlSet\Control\Class\{71a27cdd-812a-11d0-bec7-08002be2092f}";
}
