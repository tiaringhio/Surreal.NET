using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SurrealDB.Common;

internal static class MemoryHelper {
    [DllImport("msvcrt.dll")]
    private static extern unsafe int memcmp(byte* b1, byte* b2, int count);

    public static unsafe int CompareRef<T>(ref T lhs, ref T rhs)
        where T: struct {
        void* pLhs = Unsafe.AsPointer(ref lhs);
        void* pRhs = Unsafe.AsPointer(ref rhs);
        int size = Unsafe.SizeOf<T>();
        return memcmp((byte*)pLhs, (byte*)pRhs, size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare<T>(in T lhs, in T rhs)
        where T: struct {
        return CompareRef(ref Unsafe.AsRef(in lhs), ref Unsafe.AsRef(in rhs));
    }
}
