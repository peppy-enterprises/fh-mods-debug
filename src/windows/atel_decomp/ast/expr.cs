using System;
using System.Collections.Generic;
using System.Text;
using Fahrenheit.CoreLib.FFX.Atel;

namespace Fahrenheit.Modules.Debug.Windows.AtelDecomp;

internal unsafe static partial class AtelAst {
    internal abstract class Expr {
        public abstract override string ToString();
    }

    // Specifiers for certain things
    internal abstract class AssignableExpr : Expr {
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
    internal sealed class ParseErrorExpr(string message) : Expr {
        public override string ToString() => $"<ParseErrorExpr: {message}>";
    }

    // ===== Values =====
    internal sealed class Literal : Expr {
        public i16? as_ii { private set; get; }
        public i32? as_i { private set; get; }
        public f32? as_f { private set; get; }

        public Literal(i16 value) {
            as_ii = value;
        }

        public Literal(i32 value) {
            as_i = value;
        }

        public Literal(f32 value) {
            as_f = value;
        }

        public override string ToString() {
            if (as_ii.HasValue) return $"{as_ii}";
            if (as_i.HasValue)  return $"{as_i}";
            // as_f.HasValue
            return $"{as_f}";
        }
    }

    internal sealed class Variable(i16 id) : AssignableExpr {
        public i16 id = id;
        private Expr? idx; // for arrays

        public Variable(i16 id, Expr idx) : this(id) {
            this.idx = idx;
        }

        //TODO: Replace with getting the variable name as set by user, otherwise defaulting
        public override string ToString() {
            if (idx != null) return $"var{id:X2}[{idx}]";
            return $"var{id:X2}";
        }
    }

    internal sealed class VariablePointer(i16 id) : Expr {
        public i16 id = id;
        private Expr? idx; // for arrays; technically required because `pushvp` is unimplemented but I do not care

        public VariablePointer(i16 id, Expr idx) : this(id) {
            this.idx = idx;
        }

        //TODO: Replace with getting the variable name as set by user, otherwise defaulting
        public override string ToString() {
            if (idx != null) return $"&var{id:X2}[{idx}]";
            return $"&var{id:X2}";
        }
    }

    internal sealed class IntRegister(i16 idx) : AssignableExpr {
        public i16 idx = idx;

        public override string ToString() => $"regI{idx}";
    }

    internal sealed class FloatRegister(i16 idx) : AssignableExpr {
        public i16 idx = idx;

        public override string ToString() => $"regF{idx}";
    }

    internal sealed class RegisterX : AssignableExpr {
        public override string ToString() => $"rX";
    }

    internal sealed class RegisterY : AssignableExpr {
        public override string ToString() => $"rY";
    }

    internal sealed class RegisterA : AssignableExpr {
        public override string ToString() => $"rA";
    }

    internal class FnCall(u16 id, Expr[] args) : Expr {
        public u16 id = id;
        public Expr[] args = args;

        public override string ToString() {
            string fn_name = AtelCallTargets.get_fn_name(id, true);
            List<string> args_strs = [];
            foreach (Expr arg in args) args_strs.Add(arg.ToString());
            return $"{fn_name}({String.Join(", ", args_strs)})";
        }
    }

    internal sealed class Run(AtelInst inst, Expr entry_point, Expr worker_id, Expr level) : Expr {
        public override string ToString() => $"{inst} w{worker_id:X2}e{entry_point:X2} @ {level};";
    }

    // ===== Operators =====
    internal sealed class PrefixOp(AtelInst op, Expr expr) : Expr {
        public AtelInst op = op;
        public Expr expr = expr;

        public override string ToString() {
            if (expr is InfixOp) return $"{op.to_symbol()}({expr})";
            return $"{op.to_symbol()}{expr}";
        }
    }

    // Sidenote, these don't exist
    internal sealed class PostfixOp(Expr expr, AtelInst op) : Expr {
        public override string ToString() {
            if (expr is InfixOp) return $"({expr}){op.to_symbol()}";
            return $"{expr}{op.to_symbol()}";
        }
    }

    //?: Why is rhs first? Because Parser::expression() requires it
    internal sealed class InfixOp(Expr rhs, AtelInst op, Expr lhs) : Expr {
        public Expr lhs = lhs;
        public AtelInst op = op;
        public Expr rhs = rhs;

        public override string ToString() {
            StringBuilder builder = new();
            if (lhs is InfixOp _lhs && _lhs.op.get_binding_power() < op.get_binding_power()) {
                builder.Append($"({lhs})");
            } else {
                builder.Append($"{lhs}");
            }

            builder.Append($" {op.to_symbol()} ");

            if (rhs is InfixOp _rhs && _rhs.op.get_binding_power() < op.get_binding_power()) {
                builder.Append($"({rhs})");
            } else {
                builder.Append($"{rhs}");
            }

            return builder.ToString();
        }
    }
}