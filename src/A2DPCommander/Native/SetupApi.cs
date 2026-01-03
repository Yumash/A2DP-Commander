using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace BTAudioDriver.Native;

internal static class SetupApi
{
    private const string SetupApiDll = "setupapi.dll";

    #region Constants

    public static readonly Guid GUID_DEVCLASS_BLUETOOTH = new("e0cbf06c-cd8b-4647-bb8a-263b43f0f974");
    public static readonly Guid GUID_DEVCLASS_MEDIA = new("4d36e96c-e325-11ce-bfc1-08002be10318");
    public static readonly Guid GUID_DEVCLASS_SOUND = new("4d36e97c-e325-11ce-bfc1-08002be10318");

    public const int DIGCF_PRESENT = 0x00000002;
    public const int DIGCF_ALLCLASSES = 0x00000004;
    public const int DIGCF_DEVICEINTERFACE = 0x00000010;

    public const int SPDRP_DEVICEDESC = 0x00000000;
    public const int SPDRP_HARDWAREID = 0x00000001;
    public const int SPDRP_FRIENDLYNAME = 0x0000000C;
    public const int SPDRP_LOCATION_INFORMATION = 0x0000000D;

    public const int DIF_PROPERTYCHANGE = 0x00000012;

    public const int DICS_ENABLE = 0x00000001;
    public const int DICS_DISABLE = 0x00000002;

    public const int DICS_FLAG_GLOBAL = 0x00000001;
    public const int DICS_FLAG_CONFIGSPECIFIC = 0x00000002;

    public const int ERROR_NO_MORE_ITEMS = 259;

    public const uint DN_STARTED = 0x00000008;
    public const uint DN_DISABLEABLE = 0x00002000;

    #endregion

    #region Structures

    [StructLayout(LayoutKind.Sequential)]
    public struct SP_DEVINFO_DATA
    {
        public int cbSize;
        public Guid ClassGuid;
        public int DevInst;
        public IntPtr Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SP_CLASSINSTALL_HEADER
    {
        public int cbSize;
        public int InstallFunction;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SP_PROPCHANGE_PARAMS
    {
        public SP_CLASSINSTALL_HEADER ClassInstallHeader;
        public int StateChange;
        public int Scope;
        public int HwProfile;
    }

    #endregion

    #region SafeHandle

    public class SafeDevInfoHandle : SafeHandleMinusOneIsInvalid
    {
        public SafeDevInfoHandle() : base(true) { }

        protected override bool ReleaseHandle()
        {
            return SetupDiDestroyDeviceInfoList(handle);
        }
    }

    #endregion

    #region Functions

    [DllImport(SetupApiDll, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern SafeDevInfoHandle SetupDiGetClassDevs(
        ref Guid classGuid,
        string? enumerator,
        IntPtr hwndParent,
        int flags);

    [DllImport(SetupApiDll, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern SafeDevInfoHandle SetupDiGetClassDevs(
        IntPtr classGuid,
        string? enumerator,
        IntPtr hwndParent,
        int flags);

    [DllImport(SetupApiDll, SetLastError = true)]
    public static extern bool SetupDiEnumDeviceInfo(
        SafeDevInfoHandle deviceInfoSet,
        int memberIndex,
        ref SP_DEVINFO_DATA deviceInfoData);

    [DllImport(SetupApiDll, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool SetupDiGetDeviceRegistryProperty(
        SafeDevInfoHandle deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        int property,
        out int propertyRegDataType,
        byte[]? propertyBuffer,
        int propertyBufferSize,
        out int requiredSize);

    [DllImport(SetupApiDll, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool SetupDiGetDeviceInstanceId(
        SafeDevInfoHandle deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        char[] deviceInstanceId,
        int deviceInstanceIdSize,
        out int requiredSize);

    [DllImport(SetupApiDll, SetLastError = true)]
    public static extern bool SetupDiSetClassInstallParams(
        SafeDevInfoHandle deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        ref SP_PROPCHANGE_PARAMS classInstallParams,
        int classInstallParamsSize);

    [DllImport(SetupApiDll, SetLastError = true)]
    public static extern bool SetupDiCallClassInstaller(
        int installFunction,
        SafeDevInfoHandle deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData);

    [DllImport(SetupApiDll, SetLastError = true)]
    public static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    [DllImport(SetupApiDll, SetLastError = true)]
    public static extern bool SetupDiChangeState(
        SafeDevInfoHandle deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData);

    [DllImport("cfgmgr32.dll", SetLastError = true)]
    public static extern int CM_Get_DevNode_Status(
        out uint status,
        out uint problemNumber,
        int devInst,
        int flags);

    #endregion

    #region Helper Methods

    public static string? GetDeviceProperty(SafeDevInfoHandle deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, int property)
    {
        SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, property,
            out _, null, 0, out var requiredSize);

        if (requiredSize == 0) return null;

        var buffer = new byte[requiredSize];
        if (!SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, property,
            out _, buffer, requiredSize, out _))
        {
            return null;
        }

        var result = System.Text.Encoding.Unicode.GetString(buffer).TrimEnd('\0');
        return string.IsNullOrEmpty(result) ? null : result;
    }

    public static string? GetDeviceInstanceId(SafeDevInfoHandle deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData)
    {
        var buffer = new char[256];
        if (!SetupDiGetDeviceInstanceId(deviceInfoSet, ref deviceInfoData, buffer, buffer.Length, out _))
        {
            return null;
        }

        return new string(buffer).TrimEnd('\0');
    }

    public static bool SetDeviceEnabled(SafeDevInfoHandle deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, bool enable)
    {
        var propChangeParams = new SP_PROPCHANGE_PARAMS
        {
            ClassInstallHeader = new SP_CLASSINSTALL_HEADER
            {
                cbSize = Marshal.SizeOf<SP_CLASSINSTALL_HEADER>(),
                InstallFunction = DIF_PROPERTYCHANGE
            },
            StateChange = enable ? DICS_ENABLE : DICS_DISABLE,
            Scope = DICS_FLAG_GLOBAL,
            HwProfile = 0
        };

        if (!SetupDiSetClassInstallParams(deviceInfoSet, ref deviceInfoData,
            ref propChangeParams, Marshal.SizeOf<SP_PROPCHANGE_PARAMS>()))
        {
            return false;
        }

        return SetupDiCallClassInstaller(DIF_PROPERTYCHANGE, deviceInfoSet, ref deviceInfoData);
    }

    public static uint GetDeviceStatus(SafeDevInfoHandle deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData)
    {
        if (CM_Get_DevNode_Status(out var status, out _, deviceInfoData.DevInst, 0) == 0)
        {
            return status;
        }
        return 0;
    }

    #endregion
}
