using System.Runtime.InteropServices;

namespace FrameworkFanControl.CrosEc;

internal class StructWrapper<TStruct> : IDisposable
{
    public IntPtr Ptr { get; private set; }

    public StructWrapper(TStruct obj)
    {
        if (obj != null)
        {
            Ptr = Marshal.AllocHGlobal(Marshal.SizeOf(obj));
            Marshal.StructureToPtr(obj, Ptr, false);
        }
        else
        {
            Ptr = IntPtr.Zero;
        }
    }

    ~StructWrapper()
    {
        if (Ptr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(Ptr);
            Ptr = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        Marshal.FreeHGlobal(Ptr);
        Ptr = IntPtr.Zero;
        GC.SuppressFinalize(this);
    }

    public static implicit operator IntPtr(StructWrapper<TStruct> w) => w.Ptr;
}