using Microsoft.Win32.SafeHandles;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace FauxECTool
{
    public sealed class CrosEcClient : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct CROSEC_COMMAND
        {
            public uint version;
            public uint command;
            public uint outsize;
            public uint insize;
            public uint result;
            public fixed byte data[CROSEC_CMD_MAX_REQUEST];
        }

        // Public constants from Public.h
        private const uint FILE_DEVICE_CROS_EMBEDDED_CONTROLLER = 0x80EC;
        private const uint METHOD_BUFFERED = 0;
        private const uint FILE_READ_DATA = 0x0001;  // FILE_READ_ACCESS
        private const uint FILE_WRITE_DATA = 0x0002; // FILE_WRITE_ACCESS
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint OPEN_EXISTING = 3;

        // Sizes from Public.h
        public const int CROSEC_CMD_MAX_REQUEST = 0x100;
        private const int CROSEC_CMD_HEADER_SIZE = 5 * 4; // five ULONG fields = 20 bytes

        // Commands
        private const int EC_CMD_PWM_SET_FAN_DUTY = 0x24;
        private const int EC_CMD_PWM_SET_FAN_TARGET_RPM = 0x21;
        private const int EC_CMD_THERMAL_AUTO_FAN_CTRL = 0x52;

        private static uint CTL_CODE(uint deviceType, uint function, uint method, uint access)
        {
            return (deviceType << 16) | (access << 14) | (function << 2) | method;
        }

        // IOCTL defined in Public.h
        private static readonly uint IOCTL_CROSEC_XCMD = CTL_CODE(FILE_DEVICE_CROS_EMBEDDED_CONTROLLER, 0x801, METHOD_BUFFERED, FILE_READ_DATA | FILE_WRITE_DATA);

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

            SafeFileHandle handle = CreateFileW(devicePath, GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);

            if (handle == null || handle.IsInvalid)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to open device {devicePath}");
            }

            return new CrosEcClient(handle);
        }

        public void SendOffCommand()
        {
            SendCommand(EC_CMD_PWM_SET_FAN_DUTY, 0);
        }

        public (uint Result, byte[] Response, uint BytesReturned) SendCommand(
            uint command,
            uint outValue,
            uint version = 0)
        {
            byte[] outPayload = new byte[4];
            WriteUInt32To(outPayload, 0, outValue);
            return SendCommand(command, outPayload, outPayload.Length, version);
        }

        /// <summary>
        /// Envoie une commande vers l'EC. 
        /// - command/version : valeurs analogues aux champs C++.
        /// - outPayload : octets envoyés au contrôleur (peut être null).
        /// - inSizeHint : taille max acceptée en lecture (équivalent insize).
        /// Retourne (result, responseBytes, bytesReturned).
        /// </summary>
        public (uint Result, byte[] Response, uint BytesReturned) SendCommand(
            uint command,
            byte[] outPayload,
            int outSize,
            uint version = 0)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(CrosEcClient));
            }

            outSize = Math.Min(outSize, outPayload.Length);

            int cmdSize = Marshal.SizeOf<CROSEC_COMMAND>();
            int cmdHeaderSize = cmdSize - CROSEC_CMD_MAX_REQUEST;

            // layout of CROSEC_COMMAND (little-endian on x86/x64 Windows):
            // ULONG version; ULONG command; ULONG outsize; ULONG insize; ULONG result;
            // then data starts at offset CROSEC_CMD_HEADER_SIZE.
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

            IntPtr cmdPtr = Marshal.AllocHGlobal(cmdSize);
            Marshal.StructureToPtr(cmd, cmdPtr, false);


            bool ok = DeviceIoControl(_handle, IOCTL_CROSEC_XCMD,
                cmdPtr, (uint)cmdSize,
                cmdPtr, (uint)cmdSize,
                out uint bytesReturned, IntPtr.Zero);

            cmd = Marshal.PtrToStructure<CROSEC_COMMAND>(cmdPtr);

            if (!ok)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "DeviceIoControl failed");
            }

            // Read result field
            // Compute read payload size reported by ioctl: bytesReturned - sizeof(CROSEC_COMMAND)
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

            return (cmd.result, response, bytesReturned);
        }

        private static void WriteUInt32To(byte[] buf, int offset, uint value)
        {
            buf[offset + 0] = (byte)(value & 0xFF);
            buf[offset + 1] = (byte)((value >> 8) & 0xFF);
            buf[offset + 2] = (byte)((value >> 16) & 0xFF);
            buf[offset + 3] = (byte)((value >> 24) & 0xFF);
        }

        private static uint ReadUInt32From(byte[] buf, int offset)
        {
            return (uint)(buf[offset] | (buf[offset + 1] << 8) | (buf[offset + 2] << 16) | (buf[offset + 3] << 24));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _handle?.Dispose();
                _disposed = true;
            }
        }

        #region PInvoke
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeFileHandle CreateFileW(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped
        );
        #endregion
    }

    public static class ByteWriter
    {
        public static void WriteUInt8(Span<byte> buffer, ref int offset, byte value)
            => Write(buffer, ref offset, value, sizeof(byte), static (b, v) => b[0] = v);

        public static void WriteUInt16(Span<byte> buffer, ref int offset, ushort value)
            => Write(buffer, ref offset, value, sizeof(ushort), BinaryPrimitives.WriteUInt16LittleEndian);

        public static void WriteUInt32(Span<byte> buffer, ref int offset, uint value)
            => Write(buffer, ref offset, value, sizeof(uint), BinaryPrimitives.WriteUInt32LittleEndian);

        public static void WriteUInt64(Span<byte> buffer, ref int offset, ulong value)
            => Write(buffer, ref offset, value, sizeof(ulong), BinaryPrimitives.WriteUInt64LittleEndian);

        // Core generic helper
        private static void Write<T>(Span<byte> buffer, ref int offset, T value, int size, Action<Span<byte>, T> writer)
        {
            if (buffer.Length < offset + size)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "Buffer too small for write operation.");
            }

            writer(buffer.Slice(offset, size), value);
            offset += size;
        }
    }
}