using System.Runtime.InteropServices;

namespace FrameworkFanControl.Infrastructure;

internal class StructWrapper<TStruct> : IDisposable
{
    public nint Ptr { get; private set; }

    public StructWrapper(TStruct obj)
    {
        if (obj != null)
        {
            Ptr = Marshal.AllocHGlobal(Marshal.SizeOf(obj));
            Marshal.StructureToPtr(obj, Ptr, false);
        }
        else
        {
            Ptr = nint.Zero;
        }
    }

    ~StructWrapper()
    {
        if (Ptr != nint.Zero)
        {
            Marshal.FreeHGlobal(Ptr);
            Ptr = nint.Zero;
        }
    }

    public void Dispose()
    {
        Marshal.FreeHGlobal(Ptr);
        Ptr = nint.Zero;
        GC.SuppressFinalize(this);
    }

    public static implicit operator nint(StructWrapper<TStruct> w) => w.Ptr;
}