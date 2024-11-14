using Fahrenheit.CoreLib.FFX.Atel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fahrenheit.Modules.Debug.Windows.AtelDecomp;

internal unsafe static partial class AtelAst {
    internal abstract class Stmt {
        public abstract override string ToString();
    }

    internal sealed class ParseErrorStmt(string message) : Stmt {
        public override string ToString() => $"<ParseErrorStmt: {message}>";
    }

    public sealed class Block(Stmt[] body) : Stmt {
        public int length => body.Length;
        public Stmt[] body = body;

        public override string ToString() {
            StringBuilder builder = new();

            // Mildly painful way of implementing auto-indentation
            builder.Append("{\n\t");

            List<string> body_strs = [];
            foreach (Stmt stmt in body) {
                body_strs.Add(stmt.ToString());
            }

            builder.Append(String.Join("\n", body_strs).Replace("\n", "\n\t"));
            builder.Append("\n}");

            return builder.ToString();
        }
    }

    internal sealed class DirectExpr(Expr expr) : Stmt {
        public Expr expr = expr;

        public override string ToString() => $"{expr};";
    }

    internal sealed class Assignment(AssignableExpr lhs, Expr rhs) : Stmt {
        public AssignableExpr lhs = lhs;
        public Expr rhs = rhs;

        public override string ToString() {
            if (rhs is InfixOp op && op.lhs is AssignableExpr b && AssignableExpr.equivalent(lhs, b)) {
                return $"{lhs} {op.op.to_symbol()}= {op.rhs};";
            }

            return $"{lhs} = {rhs};";
        }
    }

    internal sealed class Return(Expr? expr) : Stmt {
        public Expr? expr = expr;

        public override string ToString() => expr != null ? $"return {expr};" : "return;";
    }

    internal sealed class Goto(i16 jump_idx) : Stmt {
        public i16 jump_idx = jump_idx;

        public override string ToString() => $"goto j{jump_idx:X2};";
    }

    internal sealed class GotoSubroutine(i16 subroutine_id) : Stmt {
        public i16 subroutine_id = subroutine_id;

        public override string ToString() => $"w{subroutine_id:X2}();";
    }

    internal sealed class Branch(Expr cond, Block body, Block else_body) : Stmt {
        public Expr cond = cond;
        public Block body = body;
        public Block else_body = else_body;

        public override string ToString() {
            StringBuilder builder = new();

            builder.Append($"if ({cond}) ");
            builder.Append(body.ToString());
            if (else_body.length <= 0) return builder.ToString();

            builder.Append(" else ");
            builder.Append(else_body.ToString());
            return builder.ToString();
        }
    }

    internal sealed class Switch(Expr value, Dictionary<Expr, Stmt> cases, Stmt? default_case) : Stmt {
        private Dictionary<Stmt, List<Expr>> collapsed_cases = [];

        public override string ToString() {
            StringBuilder builder = new($"switch ({value}) {{\n");

            // This gets cached for later :3
            if (collapsed_cases.IsEmpty()) {
                foreach (Expr cond in cases.Keys) {
                    if (collapsed_cases.TryAdd(cases[cond], [ cond ])) continue;
                    collapsed_cases[cases[cond]].Add(cond);
                }
            }

            // The building here is a bit messy but shhhh
            foreach (Stmt body in collapsed_cases.Keys) {
                List<Expr> conds = collapsed_cases[body];
                for (int i = 0; i < conds.Count; i++) {
                    Expr cond = conds[i];
                    // Remove the rY to make it nicer to read
                    // Removes "rY == " first because that should show up as just the expression
                    // *Then* removes "rY" for things like ">= 1"
                    builder.Append($"({cond.ToString().Replace("rY == ", "").Replace("rY ", "")})");
                    if (i < conds.Count - 1) {
                        builder.Append("\nor ");
                    }
                }

                if (body == default_case) {
                    builder.Append("\nor _");
                }

                builder.Append($" => {body},\n");
            }

            return $"{builder.ToString().Replace("\n", "\n\t")}\n}}";
        }
    }

    internal sealed class For(Stmt start, Expr cond, Stmt inc, Block body) : Stmt {
        public override string ToString() {
            return $"for ({start} {cond}; {inc.ToString()[..^1]}) {body.ToString().Replace("\n", "\n\t")}\n";
        }
    }

    internal sealed class Await(Expr entry_point, Expr worker_id) : Stmt {
        public override string ToString() => $"await w({worker_id:X2})::e({entry_point:X2})();";
    }

    internal sealed class Change(Expr entry_point_old, Expr worker_id_old, Expr entry_point_new, Expr worker_id_new) : Stmt {
        public override string ToString() => $"change w({worker_id_old:X2})::e({entry_point_old:X2}) => w({worker_id_new:X2})::e({entry_point_new:X2});";
    }
}