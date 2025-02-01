using System.Numerics;

using Fahrenheit.Core.FFX.Atel;

public static class AtelHighlight {
    public enum InstType {
        Jump,
        Push,
        Pop,
        Call,
        Ret,
        Misc,
    }

    public static Vector4 to_color(this InstType type) {
        return type switch {
            InstType.Jump => new() { X = 0.31f, Y = 0.78f, Z = 0.47f, W = 1f },
            InstType.Push => new() { X = 0.47f, Y = 0.71f, Z = 1.00f, W = 1f },
            InstType.Pop  => new() { X = 0.00f, Y = 0.45f, Z = 0.73f, W = 1f },
            InstType.Call => new() { X = 0.95f, Y = 0.61f, Z = 0.73f, W = 1f },
            InstType.Ret  => new() { X = 0.89f, Y = 0.26f, Z = 0.20f, W = 1f },
            InstType.Misc => new() { X = 1f, Y = 1f, Z = 1f, W = 1f },
        };
    }

    public static Vector4 inst_to_color(AtelInst opcode) {
        return opcode switch {
            AtelInst.PUSHA
            or AtelInst.PUSHAINTER
            or AtelInst.PUSHAR
            or AtelInst.PUSHARP
            or AtelInst.PUSHF
            or (>= AtelInst.PUSHF0 and <= AtelInst.PUSHF9)
            or AtelInst.PUSHFIX
            or AtelInst.PUSHI
            or (>= AtelInst.PUSHI0 and <= AtelInst.PUSHI3)
            or AtelInst.PUSHII
            or AtelInst.PUSHN
            or AtelInst.PUSHT
            or AtelInst.PUSHV
            or AtelInst.PUSHVP
            or AtelInst.PUSHX
            or AtelInst.PUSHY
            or AtelInst.REPUSH => to_color(InstType.Push),

            AtelInst.POPA
            or AtelInst.POPAR
            or AtelInst.POPARL
            or (>= AtelInst.POPF0 and <= AtelInst.POPF9)
            or (>= AtelInst.POPI0 and <= AtelInst.POPI3)
            or AtelInst.POPV
            or AtelInst.POPVL
            or AtelInst.POPX
            or AtelInst.POPY => to_color(InstType.Pop),

            AtelInst.JMP
            or AtelInst.CJMP
            or AtelInst.NCJMP
            or AtelInst.POPXJMP
            or AtelInst.POPXCJMP
            or AtelInst.POPXNCJMP => to_color(InstType.Jump),

            AtelInst.CALL
            or AtelInst.CALLPOPA
            or AtelInst.JSR => to_color(InstType.Call),

            AtelInst.RET
            or AtelInst.RETN
            or AtelInst.RETT
            or AtelInst.RETTN
            or AtelInst.DRET => to_color(InstType.Ret),

            _ => to_color(InstType.Misc),
        };
    }
}
