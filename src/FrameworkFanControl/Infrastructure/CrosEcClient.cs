using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using static FrameworkFanControl.Infrastructure.CrosEcConstants;

namespace FrameworkFanControl.Infrastructure;

public sealed class CrosEcClient : IDisposable
{
	private readonly SafeFileHandle _handle;
	private bool _disposed;

	private CrosEcClient(SafeFileHandle handle)
	{
		_handle = handle ?? throw new ArgumentNullException(nameof(handle));
	}

	public static CrosEcClient Open()
	{
		// Path used by the native tool
		const string devicePath = @"\\.\GLOBALROOT\Device\CrosEC";

		SafeFileHandle handle = CreateFileW(
			devicePath,
			GENERIC_READ | GENERIC_WRITE,
			FILE_SHARE_READ | FILE_SHARE_WRITE,
			nint.Zero,
			OPEN_EXISTING,
			0,
			nint.Zero
		);

		if (handle == null || handle.IsInvalid)
		{
			throw new Win32Exception(
				Marshal.GetLastWin32Error(),
				$"Failed to open device {devicePath}"
			);
		}

		return new CrosEcClient(handle);
	}

	public void SendOffCommand()
	{
		SendCommand(EC_CMD_PWM_SET_FAN_DUTY, 0);
	}

	public byte[] SendCommand(uint command, bool outValue, uint version = 0)
	{
		Span<byte> outPayload = [(byte)Convert.ToInt32(outValue)];
		return SendCommand(command, outPayload, outPayload.Length, version);
	}

	public byte[] SendCommand(uint command, uint outValue, uint version = 0)
	{
		Span<byte> outPayload = stackalloc byte[4];
		BinaryPrimitives.WriteUInt32LittleEndian(outPayload, outValue);
		return SendCommand(command, outPayload, outPayload.Length, version);
	}

	/// <summary>
	/// Envoie une commande vers l'EC.
	/// - command/version : valeurs analogues aux champs C++.
	/// - outPayload : octets envoyés au contrôleur (peut être null).
	/// - inSizeHint : taille max acceptée en lecture (équivalent insize).
	/// Retourne (result, responseBytes, bytesReturned).
	/// </summary>
	public byte[] SendCommand(
		uint command,
		ReadOnlySpan<byte> outPayload,
		int outSize,
		uint version = 0
	)
	{
		outSize = Math.Min(outSize, outPayload.Length);

		var cmd = PrepareCommand(command, outPayload, outSize, version);

		var bytesReturned = SendCommand(ref cmd);

		return ReadCommandResponse(cmd, bytesReturned);
	}

	public uint SendCommand(ref CROSEC_COMMAND cmd)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		int cmdSize = Marshal.SizeOf<CROSEC_COMMAND>();
		using var cmdPtr = new StructWrapper<CROSEC_COMMAND>(cmd);

		bool ok = DeviceIoControl(
			_handle,
			IOCTL_CROSEC_XCMD,
			cmdPtr,
			(uint)cmdSize,
			cmdPtr,
			(uint)cmdSize,
			out uint bytesReturned,
			nint.Zero
		);

		cmd = Marshal.PtrToStructure<CROSEC_COMMAND>(cmdPtr);

		if (!ok)
		{
			throw new Win32Exception(Marshal.GetLastWin32Error(), "DeviceIoControl failed");
		}

		if (cmd.result != 0)
		{
			throw new CrosEcException(cmd.result);
		}

		return bytesReturned;
	}

	private static CROSEC_COMMAND PrepareCommand(
		uint command,
		ReadOnlySpan<byte> outPayload,
		int outSize,
		uint version
	)
	{
		CROSEC_COMMAND cmd = new()
		{
			command = command,
			insize = CROSEC_CMD_MAX_REQUEST,
			outsize = (uint)outSize,
			result = 0xFF, // initial value like in C++
			version = version,
		};

		unsafe
		{
			fixed (byte* outPtr = outPayload)
			{
				Buffer.MemoryCopy(outPtr, cmd.data, CROSEC_CMD_MAX_REQUEST, outSize);
			}
		}

		return cmd;
	}

	private static byte[] ReadCommandResponse(CROSEC_COMMAND cmd, uint bytesReturned)
	{
		if (bytesReturned == 0)
		{
			return [];
		}

		// Read result field
		// Compute read payload size reported by ioctl: bytesReturned - sizeof(CROSEC_COMMAND)
		int cmdHeaderSize = Marshal.SizeOf<CROSEC_COMMAND>() - CROSEC_CMD_MAX_REQUEST;
		int payloadLen = (int)bytesReturned - cmdHeaderSize;
		payloadLen = Math.Max(0, Math.Min(payloadLen, CROSEC_CMD_MAX_REQUEST));

		byte[] response = new byte[payloadLen];
		if (payloadLen > 0)
		{
			unsafe
			{
				fixed (byte* responsePtr = response)
				{
					Buffer.MemoryCopy(cmd.data, responsePtr, payloadLen, payloadLen);
				}
			}
		}

		return response;
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_handle?.Dispose();
			_disposed = true;
		}
	}
}
