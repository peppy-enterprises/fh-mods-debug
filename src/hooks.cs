using Fahrenheit.CoreLib;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;
using static Fahrenheit.CoreLib.FhHookDelegates;
using static Fahrenheit.Modules.Debug.Delegates;
using Fahrenheit.Modules.Debug.Windows.F3;
using Fahrenheit.Modules.Debug.Windows;
using Fahrenheit.CoreLib.FFX;

namespace Fahrenheit.Modules.Debug;

public unsafe partial class DebugModule {
    private static FhMethodHandle<FUN_00a594c0> _FUN_00a594c0;
    private static FhMethodHandle<FUN_00a5a640> _FUN_00a5a640;

    private static FhMethodHandle<AtelEventSetUp> _atel_event_setup;
    private static FhMethodHandle<FUN_0086e990> _FUN_0086e990;

    private static FhMethodHandle<PrintfVarargDelegate> _printf_22F6B0;
    private static FhMethodHandle<PrintfVarargDelegate> _printf_22FDA0;
    private static FhMethodHandle<PrintfVarargDelegate> _printf_473C10;
    private static FhMethodHandle<PrintfVarargDelegate> _printf_473C20;

    private static FhMethodHandle<FUN_00642f50> _mirror_world;

    public void init_hooks() {
        const string game = "FFX.exe";

        // Sphere Grid Editor
        _FUN_00a594c0 = new FhMethodHandle<FUN_00a594c0>(this, game, render_sphere_grid, offset: 0x6594c0);
        _FUN_00a5a640 = new FhMethodHandle<FUN_00a5a640>(this, game, update_node_type_early, offset: 0x65a640);

        // Atel Debug
        _atel_event_setup = new(this, game, clear_recent_atel_signals, offset: 0x472e90);
        _FUN_0086e990 = new(this, game, add_recent_atel_signal, offset: 0x46e990);

        _printf_22F6B0 = new(this, game, h_printf_ansi, offset: 0x22F6B0);
        _printf_22FDA0 = new(this, game, h_printf_ansi, offset: 0x22FDA0);
        _printf_473C10 = new(this, game, h_printf_ansi, offset: 0x473C10);
        _printf_473C20 = new(this, game, h_printf_ansi, offset: 0x473C20);

        // lol
        _mirror_world = new(this, game, mirror_world, offset: 0x342f50);
    }

    public bool hook() {
        return _FUN_00a594c0.hook()
            && _FUN_00a5a640.hook()
            && _FUN_0086e990.hook()
            && _atel_event_setup.hook()
            && _mirror_world.hook();
            //&& _printf_22F6B0.hook()
            //&& _printf_22FDA0.hook()
            //&& _printf_473C10.hook()
            //&& _printf_473C20.hook();
    }

    private static void mirror_world(float p1, float p2, float x, float y, float p5) {
        _mirror_world.orig_fptr(p1, p2, x, y, p5);

        Mat4f* world_matrix = (Mat4f*)(*FhUtil.ptr_at<nint>(0x8cb9d8) + 0xd34);
        Vec4f* col3 = (Vec4f*)world_matrix + 3;

        for (int i = 0; i < 4; i++) {
            *((float*)col3 + i) *= -1;
        }
    }

    private static void render_sphere_grid(u8* text, i32 p2, i32 p3) {
        _FUN_00a594c0.orig_fptr(text, p2, p3);

        //SphereGridEditor.render();
    }

    private static void update_node_type_early(i32 new_node_type, i32 node_idx) {
        _FUN_00a5a640.orig_fptr(new_node_type, node_idx);

        SphereGridEditor.update_node_type();
    }

    private static void clear_recent_atel_signals(u32 event_id) {
        AtelDebugger.clear_recent_signals();

        _atel_event_setup.orig_fptr(event_id);
    }

    private static u32 add_recent_atel_signal(nint signal_info_ptr) {
        u32 ret = _FUN_0086e990.orig_fptr(signal_info_ptr);

        i16 worker_id = *(i16*)(signal_info_ptr + 0x4);
        i16 entry_id = *(i16*)(signal_info_ptr + 0x10);
        u32 ctrl_idx = *(u32*)(signal_info_ptr + 0x14);
        if (ctrl_idx > 7) return ret;

        AtelDebugger.add_recent_signal(ctrl_idx, worker_id, entry_id);

        return ret;
    }

    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static void h_printf_ansi(string fmt, nint va0, nint va1, nint va2, nint va3, nint va4, nint va5, nint va6, nint va7, nint va8, nint va9, nint va10, nint va11, nint va12, nint va13, nint va14, nint va15) {
        int argc = 0;
        fmt = fmt.Trim();

        for (int i = 0; i < fmt.Length; i++) if (fmt[i] == '%') argc++;

        int bl = argc switch {
            0  => PInvoke._scprintf(fmt, __arglist()),
            1  => PInvoke._scprintf(fmt, __arglist(va0)),
            2  => PInvoke._scprintf(fmt, __arglist(va0, va1)),
            3  => PInvoke._scprintf(fmt, __arglist(va0, va1, va2)),
            4  => PInvoke._scprintf(fmt, __arglist(va0, va1, va2, va3)),
            5  => PInvoke._scprintf(fmt, __arglist(va0, va1, va2, va3, va4)),
            6  => PInvoke._scprintf(fmt, __arglist(va0, va1, va2, va3, va4, va5)),
            7  => PInvoke._scprintf(fmt, __arglist(va0, va1, va2, va3, va4, va5, va6)),
            8  => PInvoke._scprintf(fmt, __arglist(va0, va1, va2, va3, va4, va5, va6, va7)),
            9  => PInvoke._scprintf(fmt, __arglist(va0, va1, va2, va3, va4, va5, va6, va7, va8)),
            10 => PInvoke._scprintf(fmt, __arglist(va0, va1, va2, va3, va4, va5, va6, va7, va8, va9)),
            11 => PInvoke._scprintf(fmt, __arglist(va0, va1, va2, va3, va4, va5, va6, va7, va8, va9, va10)),
            12 => PInvoke._scprintf(fmt, __arglist(va0, va1, va2, va3, va4, va5, va6, va7, va8, va9, va10, va11)),
            13 => PInvoke._scprintf(fmt, __arglist(va0, va1, va2, va3, va4, va5, va6, va7, va8, va9, va10, va11, va12)),
            14 => PInvoke._scprintf(fmt, __arglist(va0, va1, va2, va3, va4, va5, va6, va7, va8, va9, va10, va11, va12, va13)),
            15 => PInvoke._scprintf(fmt, __arglist(va0, va1, va2, va3, va4, va5, va6, va7, va8, va9, va10, va11, va12, va13, va14)),
            16 => PInvoke._scprintf(fmt, __arglist(va0, va1, va2, va3, va4, va5, va6, va7, va8, va9, va10, va11, va12, va13, va14, va15)),
            _  => throw new Exception("FH_E_PFHOOK_RAH_OVERREACH")
        };

        nint buf = Marshal.AllocHGlobal(bl + 1);

        try {
            int rv = argc switch {
                0  => PInvoke.sprintf(buf, fmt, __arglist()),
                1  => PInvoke.sprintf(buf, fmt, __arglist(va0)),
                2  => PInvoke.sprintf(buf, fmt, __arglist(va0, va1)),
                3  => PInvoke.sprintf(buf, fmt, __arglist(va0, va1, va2)),
                4  => PInvoke.sprintf(buf, fmt, __arglist(va0, va1, va2, va3)),
                5  => PInvoke.sprintf(buf, fmt, __arglist(va0, va1, va2, va3, va4)),
                6  => PInvoke.sprintf(buf, fmt, __arglist(va0, va1, va2, va3, va4, va5)),
                7  => PInvoke.sprintf(buf, fmt, __arglist(va0, va1, va2, va3, va4, va5, va6)),
                8  => PInvoke.sprintf(buf, fmt, __arglist(va0, va1, va2, va3, va4, va5, va6, va7)),
                9  => PInvoke.sprintf(buf, fmt, __arglist(va0, va1, va2, va3, va4, va5, va6, va7, va8)),
                10 => PInvoke.sprintf(buf, fmt, __arglist(va0, va1, va2, va3, va4, va5, va6, va7, va8, va9)),
                11 => PInvoke.sprintf(buf, fmt, __arglist(va0, va1, va2, va3, va4, va5, va6, va7, va8, va9, va10)),
                12 => PInvoke.sprintf(buf, fmt, __arglist(va0, va1, va2, va3, va4, va5, va6, va7, va8, va9, va10, va11)),
                13 => PInvoke.sprintf(buf, fmt, __arglist(va0, va1, va2, va3, va4, va5, va6, va7, va8, va9, va10, va11, va12)),
                14 => PInvoke.sprintf(buf, fmt, __arglist(va0, va1, va2, va3, va4, va5, va6, va7, va8, va9, va10, va11, va12, va13)),
                15 => PInvoke.sprintf(buf, fmt, __arglist(va0, va1, va2, va3, va4, va5, va6, va7, va8, va9, va10, va11, va12, va13, va14)),
                16 => PInvoke.sprintf(buf, fmt, __arglist(va0, va1, va2, va3, va4, va5, va6, va7, va8, va9, va10, va11, va12, va13, va14, va15)),
                _  => throw new Exception("FH_E_PFHOOK_RAH_OVERREACH")
            };

            FhLog.Info(Marshal.PtrToStringAnsi(buf) ?? "FH_E_PFHOOK_STRING_NUL");
        }
        finally {
            Marshal.FreeHGlobal(buf);
        }
    }
}
