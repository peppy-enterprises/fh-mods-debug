using System.Runtime.InteropServices;

namespace Fahrenheit.Modules.Debug;

internal static unsafe class Delegates {
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void TOMkpCrossExtMesFontLClutTypeRGBA(
            u32 p1,
            u8 *text,
            f32 x, f32 y,
            u8 color,
            u8 p6,
            u8 tint_r, u8 tint_g, u8 tint_b, u8 tint_a,
            f32 scale,
            f32 _);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FUN_00a594c0(u8* txt, i32 p2, i32 p3);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SndSepPlaySimple(u32 snd_id);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FUN_00a48740(i32 p1, i32 node_idx);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FUN_00a5a640(i32 new_node_type, i32 node_idx);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate u32 FUN_0086e990(nint signal_info_ptr);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void AtelEventSetUp(u32 event_id);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate nint FUN_00797420(nint script_data_ptr, i32 slot, i32* entry_point);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FUN_00642f50(float p1, float p2, float x, float y, float p5);
}
