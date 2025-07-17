using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Fahrenheit.Mods.Debug;

internal static partial class FhPInvoke {
    [LibraryImport("msvcrt.dll", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
    [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    public static unsafe partial int _vsnprintf_s(nint buffer, nuint sizeOfBuffer, nuint count, string fmt, nint* argptr);
}
