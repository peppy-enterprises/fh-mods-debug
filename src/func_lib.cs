using Fahrenheit.CoreLib;
using static Fahrenheit.Modules.Debug.Delegates;

namespace Fahrenheit.Modules.Debug;

internal static unsafe class FuncLib {
    public static void TOMkpCrossExtMesFontLClutTypeRGBA(
            u32 p1,
            u8[] text,
            f32 x, f32 y,
            u8 color,
            u8 p6,
            u8 tint_r, u8 tint_g, u8 tint_b, u8 tint_a,
            f32 scale,
            f32 _) {
        fixed (u8 *_text = text)
        FhUtil.get_fptr<TOMkpCrossExtMesFontLClutTypeRGBA>(0x501700)(p1, _text, x, y, color, p6, tint_r, tint_g, tint_b, tint_a, scale, _);
    }

    public static void TOMkpCrossExtMesFontLClut(
            u32 p1,
            u8[] text,
            f32 x, f32 y,
            u8 color,
            u8 p6,
            f32 scale,
            f32 _) {
        TOMkpCrossExtMesFontLClutTypeRGBA(p1, text, x, y, color, p6, 0x80, 0x80, 0x80, 0x80, scale, _);
    }

    public static void SndSepPlaySimple(u32 snd_id) {
        FhUtil.get_fptr<SndSepPlaySimple>(0x486de0)(snd_id);
    }

    public static void FUN_00a48740(i32 p1, i32 node_idx) {
        FhUtil.get_fptr<FUN_00a48740>(0x648740)(p1, node_idx);
    }

    private static FhMethodHandle<FUN_00797420> _FUN_00797420 = new(null, "FFX.exe", null, offset: 0x397420);

    public static nint FUN_00797420(nint script_data_ptr, i32 slot, i32* entry_point) {
        return _FUN_00797420.orig_fptr(script_data_ptr, slot, entry_point);
    }
}
