using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace FrameworkFanControl.Infrastructure;

internal static class CrosEcConstants
{
	#region winioctl.h
	public const uint METHOD_BUFFERED = 0;
	#endregion

	#region winnt.h
	public const uint FILE_READ_DATA = 0x0001; // FILE_READ_ACCESS
	public const uint FILE_WRITE_DATA = 0x0002; // FILE_WRITE_ACCESS
	public const uint FILE_SHARE_READ = 0x00000001;
	public const uint FILE_SHARE_WRITE = 0x00000002;
	public const uint GENERIC_READ = 0x80000000;
	public const uint GENERIC_WRITE = 0x40000000;
	#endregion

	#region fileapi.h
	public const uint OPEN_EXISTING = 3;

	[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	public static extern SafeFileHandle CreateFileW(
		string lpFileName,
		uint dwDesiredAccess,
		uint dwShareMode,
		nint lpSecurityAttributes,
		uint dwCreationDisposition,
		uint dwFlagsAndAttributes,
		nint hTemplateFile
	);
	#endregion

	#region ioapiset.h
	[DllImport("kernel32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool DeviceIoControl(
		SafeFileHandle hDevice,
		uint dwIoControlCode,
		nint lpInBuffer,
		uint nInBufferSize,
		nint lpOutBuffer,
		uint nOutBufferSize,
		out uint lpBytesReturned,
		nint lpOverlapped
	);
	#endregion

	#region CrosEC Public.h
	public const uint FILE_DEVICE_CROS_EMBEDDED_CONTROLLER = 0x80EC;
	public const int CROSEC_CMD_MAX_REQUEST = 0x100;

	public static uint CTL_CODE(uint deviceType, uint function, uint method, uint access)
	{
		return deviceType << 16 | access << 14 | function << 2 | method;
	}

	public static readonly uint IOCTL_CROSEC_XCMD = CTL_CODE(
		FILE_DEVICE_CROS_EMBEDDED_CONTROLLER,
		0x801,
		METHOD_BUFFERED,
		FILE_READ_DATA | FILE_WRITE_DATA
	);
	#endregion

	#region Commands
	public const int EC_CMD_PWM_SET_FAN_DUTY = 0x24;
	public const int EC_CMD_PWM_SET_FAN_TARGET_RPM = 0x21;
	public const int EC_CMD_THERMAL_AUTO_FAN_CTRL = 0x52;
	#endregion
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct CROSEC_COMMAND
{
	public uint version;
	public uint command;
	public uint outsize;
	public uint insize;
	public uint result;
	public fixed byte data[CrosEcConstants.CROSEC_CMD_MAX_REQUEST];
}
