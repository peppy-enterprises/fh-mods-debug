using Fahrenheit.CoreLib.FFX.Atel;

namespace Fahrenheit.Modules.Debug.Windows.AtelDecomp;

internal static class AtelInstExt {
        public static string to_symbol(this AtelInst inst) {
            return inst switch {
                AtelInst.OR => "|",
                AtelInst.EOR => "^",
                AtelInst.AND => "&",

                AtelInst.LOR => "||",
                AtelInst.LAND => "&&",

                AtelInst.EQ => "==",
                AtelInst.NE => "!=",

                AtelInst.GT => ">",
                AtelInst.LS => "<",
                AtelInst.GTE => ">=",
                AtelInst.LSE => "<=",

                AtelInst.GTU => "u>",
                AtelInst.LSU => "u<",
                AtelInst.GTEU => "u>=",
                AtelInst.LSEU => "u<=",

                AtelInst.BOFF => "b!",
                AtelInst.BON => "b!!",

                AtelInst.SLL => "<<",
                AtelInst.SRL => ">>",

                AtelInst.ADD => "+",
                AtelInst.SUB => "-",
                AtelInst.MUL => "*",
                AtelInst.DIV => "/",
                AtelInst.MOD => "%",

                AtelInst.NOT => "!",

                AtelInst.UMINUS => "-",
                AtelInst.BNOT => "~",

                _ => throw new System.NotImplementedException(""),
            };
        }

        public static int get_binding_power(this AtelInst inst) {
            return inst switch {
                AtelInst.LOR => 1,
                AtelInst.LAND => 2,

                AtelInst.OR => 3,
                AtelInst.EOR => 4,
                AtelInst.AND => 5,

                AtelInst.BOFF => 6,
                AtelInst.BON => 6,

                AtelInst.EQ => 7,
                AtelInst.NE => 7,

                AtelInst.LS => 8,
                AtelInst.LSE => 8,
                AtelInst.GT => 8,
                AtelInst.GTE => 8,

                AtelInst.LSU => 9,
                AtelInst.LSEU => 9,
                AtelInst.GTU => 9,
                AtelInst.GTEU => 9,

                AtelInst.SLL => 10,
                AtelInst.SRL => 10,

                AtelInst.ADD => 11,
                AtelInst.SUB => 11,
                AtelInst.MUL => 12,
                AtelInst.DIV => 12,
                AtelInst.MOD => 12,

                AtelInst.NOT => 20,

                AtelInst.UMINUS => 20,
                AtelInst.BNOT => 20,

                _ => throw new System.NotImplementedException(""),
            };
        }
    }