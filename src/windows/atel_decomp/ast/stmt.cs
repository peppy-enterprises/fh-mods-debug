using System;
using System.Collections.Generic;
using System.Text;

using Fahrenheit.Core.FFX.Atel;

namespace Fahrenheit.Modules.Debug.Windows.AtelDecomp;

internal unsafe static partial class AtelAst {
    internal abstract class Stmt(i32 offset) {
        public i32 offset = offset;
        public abstract override string ToString();
    }

    internal sealed class ParseErrorStmt(string message) : Stmt(-1) {
        public override string ToString() => $"<ParseErrorStmt: {message}>";
    }

    public sealed class Block(i32 offset, List<Stmt> body) : Stmt(offset) {
        public int length => body.Count;
        public List<Stmt> body = body;

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

    internal sealed class DirectExpr(i32 offset, Expr expr) : Stmt(offset) {
        public Expr expr = expr;

        public override string ToString() => $"{expr};";
    }

    internal sealed class Assignment(i32 offset, AssignableExpr lhs, Expr rhs) : Stmt(offset) {
        public AssignableExpr lhs = lhs;
        public Expr rhs = rhs;

        public override string ToString() {
            if (rhs is InfixOp { lhs: AssignableExpr b } op && AssignableExpr.equivalent(lhs, b)) {
                return $"{lhs} {op.op.to_symbol()}= {op.rhs};";
            }

            return $"{lhs} = {rhs};";
        }
    }

    internal sealed class Return(i32 offset, Expr? expr) : Stmt(offset) {
        public Expr? expr = expr;

        public override string ToString() => expr != null ? $"return {expr};" : "return;";
    }

    internal sealed class Goto(i32 offset, i16 jump_idx) : Stmt(offset) {
        public i16 jump_idx = jump_idx;

        public override string ToString() => $"goto j{jump_idx:X2};";
    }

    internal sealed class Label(i32 offset, i16 idx) : Stmt(offset) {
        public i16 idx = idx;

        public override string ToString() => $"j{idx:X2}:";
    }

    internal sealed class GotoSubroutine(i32 offset, i16 subroutine_id) : Stmt(offset) {
        public i16 subroutine_id = subroutine_id;

        public override string ToString() => $"w{subroutine_id:X2}();";
    }

    internal sealed class Branch(i32 offset, Expr cond, Block body, Block else_body) : Stmt(offset) {
        public Expr cond = cond;
        public Block body = body;
        public Block else_body = else_body;

        public override string ToString() {
            StringBuilder builder = new();

            builder.Append($"if ({cond}) ");
            builder.Append(body);
            if (else_body.length <= 0) return builder.ToString();

            builder.Append(" else ");
            builder.Append(else_body);
            return builder.ToString();
        }
    }

    internal sealed class Switch(i32 offset, Expr value, Dictionary<Expr, Block> cases, Block? default_case) : Stmt(offset) {
        public Expr value = value;
        public Dictionary<Expr, Block> cases = cases;
        public Block? default_case = default_case;
        private readonly Dictionary<Stmt, List<Expr>> _collapsed_cases = [];

        public override string ToString() {
            StringBuilder builder = new($"switch ({value}) {{\n");

            // This gets cached for later :3
            if (_collapsed_cases.IsEmpty()) {
                foreach (Expr cond in cases.Keys) {
                    if (_collapsed_cases.TryAdd(cases[cond], [ cond ])) continue;
                    _collapsed_cases[cases[cond]].Add(cond);
                }
            }

            foreach (Stmt body in _collapsed_cases.Keys) {
                List<Expr> conds = _collapsed_cases[body];
                for (int i = 0; i < conds.Count; i++) {
                    Expr cond = conds[i];

                    if (cond is InfixOp { rhs: Literal lit }) {
                        builder.Append($"case {lit}:");
                    } else {
                        // Remove the rY to make it nicer to read
                        builder.Append($"case ({cond.ToString().Replace("rY ", "")}):");
                    }

                    if (i < conds.Count - 1) {
                        builder.Append('\n');
                    }
                }

                builder.Append($" {body}\n");
            }

            if (default_case != null) {
                builder.Append($"default: {default_case}\n");
            }

            // Remove the last (extra) \n
            builder.Remove(builder.Length - 1, 1);

            return $"{builder.ToString().Replace("\n", "\n\t")}\n}}";
        }
    }

    internal sealed class Break(i32 offset, i16 jmp_idx) : Stmt(offset) {
        public i16 jmp_idx = jmp_idx;

        public override string ToString() => "break;";
    }

    internal sealed class For(i32 offset, Stmt start, Expr cond, Stmt inc, Block body) : Stmt(offset) {
        public override string ToString() {
            return $"for ({start} {cond}; {inc.ToString()[..^1]}) {body.ToString().Replace("\n", "\n\t")}\n";
        }
    }

    internal sealed class Await(i32 offset, Expr entry_point_id, Expr worker_id) : Stmt(offset) {
        public override string ToString() {
            string worker_name =
                worker_id is Literal w_lit
                    ? $"w{w_lit}"
                    : $"w({worker_id})";
            string entry_point_name =
                entry_point_id is Literal e_lit
                    ? $"e{e_lit}"
                    : $"e({entry_point_id})";
            return $"await {worker_name}::{entry_point_name}();";
        }
    }

    internal sealed class Change(i32 offset, i32 worker_id, Expr entry_point_new, Expr entry_point_old, Expr value) : Stmt(offset) {
        public override string ToString() {
            Expr old = entry_point_old;

            i16 old_id = -1;
            if (old is Literal old_lit) {
                if (old_lit.as_i is not null) old_id  = (i16)(old_lit.as_i + 2);
                if (old_lit.as_ii is not null) old_id = (i16)(old_lit.as_ii + 2);
                if (old_lit.as_f is not null) old_id  = (i16)(old_lit.as_f + 2);
            } else {
                i32 old_offset = old.offset;
                old = new InfixOp(old_offset, new Literal(old_offset, (i16)2), AtelInst.ADD, old);
            }

            i16 new_id = -1;
            if (entry_point_new is Literal new_lit) {
                if (new_lit.as_i is not null) new_id  = (i16)(new_lit.as_i);
                if (new_lit.as_ii is not null) new_id = (i16)(new_lit.as_ii);
                if (new_lit.as_f is not null) new_id  = (i16)(new_lit.as_f);
            }

            //TODO: Change to actually getting the names (like symbols)
            string work_name = $"w{worker_id:X2}";
            string old_name = old is Literal ? $"e{old_id:X2}" : $"e({old})";
            string new_name = entry_point_new is Literal ? $"e{new_id:X2}" : $"e({entry_point_new})";
            return $"change({value}) {work_name}::{old_name} => {work_name}::{new_name};";
        }
    }
}
