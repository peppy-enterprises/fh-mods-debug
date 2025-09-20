// SPDX-License-Identifier: MIT

namespace Fahrenheit.Mods.Debug;

/* [fkelava 17/7/25 02:33]
 * For vararg functions the delegate signature should have an argument count >=
 * the argument count of the invocation with the most varargs in the executable.
 *
 * For now we assume sixteen. If you crash with a buffer/stack overrun, increase it.
 */

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void PrintfVarargDelegate(string fmt,
    nint va0,  nint va1,  nint va2,  nint va3,
    nint va4,  nint va5,  nint va6,  nint va7,
    nint va8,  nint va9,  nint va10, nint va11,
    nint va12, nint va13, nint va14, nint va15);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void PhyrePrintfDelegate(int rc, string fmt,
    nint va0,  nint va1,  nint va2,  nint va3,
    nint va4,  nint va5,  nint va6,  nint va7,
    nint va8,  nint va9,  nint va10, nint va11,
    nint va12, nint va13, nint va14, nint va15);

/// <summary>
///     Restores the bodies of stubbed-out debug print calls within the game. The output
///     is logged to the Stage0 console and to disk.
///     <para/>
///     Do not interface with this module directly. It is self-contained.
/// </summary>
[FhLoad(FhGameType.FFX)]
public unsafe class FhDebugPrintModule : FhModule {

    /* [fkelava 17/7/25 02:33]
     * Taken experimentally from PhyrePrintf. If this is not large enough, increase it.
     */
    private const int _buf_sz = 16384;

    private readonly FhMethodHandle<PhyrePrintfDelegate>  _h_PhyrePrintf;
    private readonly FhMethodHandle<PrintfVarargDelegate> _h_rcPrint;
    private readonly FhMethodHandle<PrintfVarargDelegate> _h_dbgPrintf;
    private readonly FhMethodHandle<PrintfVarargDelegate> _h_scePrintf;
    private readonly FhMethodHandle<PrintfVarargDelegate> _h_AtelPs2DebugString;
    private readonly FhMethodHandle<PrintfVarargDelegate> _h_AtelPs2DebugString2;

    public FhDebugPrintModule() {
        _h_PhyrePrintf         = new FhMethodHandle<PhyrePrintfDelegate> (this, "FFX.exe", 0x0353F0, h_pprintf);
        _h_rcPrint             = new FhMethodHandle<PrintfVarargDelegate>(this, "FFX.exe", 0x527550, h_printf);
        _h_dbgPrintf           = new FhMethodHandle<PrintfVarargDelegate>(this, "FFX.exe", 0x22F6B0, h_printf);
        _h_scePrintf           = new FhMethodHandle<PrintfVarargDelegate>(this, "FFX.exe", 0x22FDA0, h_printf);
        _h_AtelPs2DebugString  = new FhMethodHandle<PrintfVarargDelegate>(this, "FFX.exe", 0x473C10, h_printf);
        _h_AtelPs2DebugString2 = new FhMethodHandle<PrintfVarargDelegate>(this, "FFX.exe", 0x473C20, h_printf);
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private void h_pprintf(int rc, string fmt, nint va0, nint va1, nint va2, nint va3, nint va4, nint va5, nint va6, nint va7, nint va8, nint va9, nint va10, nint va11, nint va12, nint va13, nint va14, nint va15) {
        if (fmt.StartsWith("[FFX_section", StringComparison.InvariantCulture)) return; // EFL logs supersede Phyre load prints

        fmt      = fmt.Trim();
        nint buf = Marshal.AllocHGlobal(_buf_sz);

        try {
            int rv = FhPInvoke._vsnprintf_s(buf, _buf_sz, 0xFFFF_FFFF, fmt, &va0);
            _logger.Log(FhLogLevel.Info, $"[{rc}] {Marshal.PtrToStringAnsi(buf)!}");
        }
        finally {
            Marshal.FreeHGlobal(buf);
        }
    }

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private void h_printf(string fmt, nint va0, nint va1, nint va2, nint va3, nint va4, nint va5, nint va6, nint va7, nint va8, nint va9, nint va10, nint va11, nint va12, nint va13, nint va14, nint va15) {
        fmt      = fmt.Trim();
        nint buf = Marshal.AllocHGlobal(_buf_sz);

        try {
            int rv = FhPInvoke._vsnprintf_s(buf, _buf_sz, 0xFFFF_FFFF, fmt, &va0);
            _logger.Log(FhLogLevel.Info, Marshal.PtrToStringAnsi(buf)!);
        }
        finally {
            Marshal.FreeHGlobal(buf);
        }
    }

    public override bool init(FhModContext mod_context, FileStream global_state_file) {
        return _h_PhyrePrintf        .hook() &&
               _h_rcPrint            .hook() &&
               _h_dbgPrintf          .hook() &&
               _h_scePrintf          .hook() &&
               _h_AtelPs2DebugString .hook() &&
               _h_AtelPs2DebugString2.hook();
    }
}
