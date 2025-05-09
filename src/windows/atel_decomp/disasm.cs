using System;
using System.Collections.Generic;

using Fahrenheit.Core.FFX.Atel;

using static Fahrenheit.Modules.Debug.Windows.AtelDecomp.AtelDecompiler;

namespace Fahrenheit.Modules.Debug.Windows.AtelDecomp;

internal unsafe static class AtelDisassembler {
    public static List<AtelDOpCode> disasm_bytes(i32 initial_offset, ReadOnlySpan<u8> bytes) {
        List<AtelDOpCode> opcodes = new();

        fixed (u8* p = bytes) {
            u8* ptr = p;
            isize max = (isize)ptr + bytes.Length;
            while ((isize)ptr < max) {
                AtelOpCode opcode = construct_opcode(ptr);
                opcodes.Add(new AtelDOpCode {
                    offset = initial_offset + (i32)ptr - (i32)p,
                    inst = (AtelInst)opcode.instruction,
                    operand = (i16?)opcode.operand,
                });
                ptr += opcode.operand != null ? 3 : 1;
            }
        }

        return opcodes;
    }

    private static AtelOpCode construct_opcode(u8* ptr) {
        u8 inst = *ptr;

        u16? operand = null;
        if (((AtelInst)inst).has_operand()) {
            operand = *(u16*)(ptr + 1);
        }

        return new AtelOpCode {
            instruction = inst,
            operand = operand,
        };
    }
}
