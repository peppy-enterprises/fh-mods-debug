using System;
using System.Collections.Generic;
using System.Text;

using Fahrenheit.Core.FFX.Atel;

namespace Fahrenheit.Modules.Debug.Windows.AtelDecomp;

internal unsafe static partial class AtelAst {
    internal abstract class Expr(i32 offset) {
        public i32 offset = offset;
        public abstract override string ToString();
    }

    // Specifiers for certain things
    internal abstract class AssignableExpr(i32 offset) : Expr(offset) {
        public abstract string get_default_name();
        public override string ToString() => AtelDecompiler.get_symbol(this);

        public static bool equivalent(AssignableExpr a, AssignableExpr b) {
            if (a.GetType() != b.GetType()) return false;
            if (a is Variable va && b is Variable vb) return va.id == vb.id;
            if (a is IntRegister ia && b is IntRegister ib) return ia.idx == ib.idx;
            if (a is FloatRegister fa && b is FloatRegister fb) return fa.idx == fb.idx;
            // rX, rY, rA
            return true;
        }
    }

    // Errors politely separated from the interfaces
    internal sealed class ParseErrorExpr(string message) : Expr(-1) {
        public override string ToString() => $"<ParseErrorExpr: {message}>";
    }

    // ===== Values =====
    internal sealed class Literal : Expr {
        public i16? as_ii { private set; get; }
        public i32? as_i { private set; get; }
        public f32? as_f { private set; get; }

        public Literal(i32 offset, i16 value) : base(offset) {
            as_ii = value;
        }

        public Literal(i32 offset, i32 value) : base(offset) {
            as_i = value;
        }

        public Literal(i32 offset, f32 value) : base(offset) {
            as_f = value;
        }

        public override string ToString() {
            if (as_ii.HasValue) return $"{as_ii}";
            if (as_i.HasValue)  return $"{as_i}";
            // as_f.HasValue
            return $"{as_f}";
        }
    }

    internal sealed class Variable(i32 offset, i16 id) : AssignableExpr(offset) {
        public i16 id = id;
        private Expr? idx; // for arrays

        public Variable(i32 offset, i16 id, Expr idx) : this(offset, id) {
            this.idx = idx;
        }

        //TODO: shared var IDs should be prefixed with s, etc.
        public override string get_default_name() {
            if (idx != null) return $"var{id:X2}[{idx}]";
            return $"var{id:X2}";
        }
    }

    internal sealed class VariablePointer(i32 offset, i16 id) : Expr(offset) {
        public i16 id = id;
        private Expr? idx; // for arrays; technically required because `pushvp` is unimplemented but I do not care

        public VariablePointer(i32 offset, i16 id, Expr idx) : this(offset, id) {
            this.idx = idx;
        }

        public override string ToString() {
            //HACK: Create a temporary Variable instance to get the symbol name
            return "&" + AtelDecompiler.get_symbol(new Variable(offset, id, idx!));
        }
    }

    internal sealed class IntRegister(i32 offset, i16 idx) : AssignableExpr(offset) {
        public i16 idx = idx;

        public override string get_default_name() => $"regI{idx}";
    }

    internal sealed class FloatRegister(i32 offset, i16 idx) : AssignableExpr(offset) {
        public i16 idx = idx;

        public override string get_default_name() => $"regF{idx}";
    }

    internal sealed class RegisterX(i32 offset) : AssignableExpr(offset) {
        public override string get_default_name() => $"rX";
    }

    internal sealed class RegisterY(i32 offset) : AssignableExpr(offset) {
        public override string get_default_name() => $"rY";
    }

    internal sealed class RegisterA(i32 offset) : AssignableExpr(offset) {
        public override string get_default_name() => $"rA";
    }

    internal class FnCall(i32 offset, u16 id, Expr[] args) : Expr(offset) {
        public u16 id = id;
        public Expr[] args = args;

        public override string ToString() {
            string fn_name = AtelCallTargets.get_fn_name(id, true);
            List<string> args_strs = [];
            foreach (Expr arg in args) args_strs.Add(arg.ToString());
            return $"{fn_name}({String.Join(", ", args_strs)})";
        }
    }

    internal sealed class Run(i32 offset, AtelInst inst, Expr entry_point_id, Expr worker_id, Expr level) : Expr(offset) {
        public override string ToString() {
            string worker_name =
                worker_id is Literal w_lit
                    ? $"w{w_lit}"
                    : $"w({worker_id})";
            string entry_point_name =
                entry_point_id is Literal e_lit
                    ? $"e{e_lit}"
                    : $"e({entry_point_id})";
            string level_string =
                level is Literal l_lit
                    ? $"{level}"
                    : $"({level})";
            return $"await {worker_name}::{entry_point_name}() @ {level_string};";
        }
    }

    // ===== Operators =====
    internal sealed class PrefixOp(i32 offset, AtelInst op, Expr expr) : Expr(offset) {
        public AtelInst op = op;
        public Expr expr = expr;

        public override string ToString() {
            if (expr is InfixOp) return $"{op.to_symbol()}({expr})";
            return $"{op.to_symbol()}{expr}";
        }
    }

    // Sidenote, these don't exist
    internal sealed class PostfixOp(i32 offset, Expr expr, AtelInst op) : Expr(offset) {
        public override string ToString() {
            if (expr is InfixOp) return $"({expr}){op.to_symbol()}";
            return $"{expr}{op.to_symbol()}";
        }
    }

    //NOTE: Why is rhs first? Because Parser::expression() requires it
    internal sealed class InfixOp(i32 offset, Expr rhs, AtelInst op, Expr lhs) : Expr(offset) {
        public Expr lhs = lhs;
        public AtelInst op = op;
        public Expr rhs = rhs;

        public override string ToString() {
            StringBuilder builder = new();
            if (lhs is InfixOp lhs_ && lhs_.op.get_binding_power() < op.get_binding_power()) {
                builder.Append($"({lhs})");
            } else {
                builder.Append($"{lhs}");
            }

            builder.Append($" {op.to_symbol()} ");

            if (rhs is InfixOp rhs_ && rhs_.op.get_binding_power() < op.get_binding_power()) {
                builder.Append($"({rhs})");
            } else {
                builder.Append($"{rhs}");
            }

            return builder.ToString();
        }
    }
}
