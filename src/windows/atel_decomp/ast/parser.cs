using System;
using System.Collections.Generic;

using Fahrenheit.Core.FFX;
using Fahrenheit.Core.FFX.Atel;

using static Fahrenheit.Modules.Debug.Windows.AtelDecomp.AtelDecompiler;

namespace Fahrenheit.Modules.Debug.Windows.AtelDecomp;

internal unsafe static partial class AtelAst {
    public static  i32                   ctrl_id;
    public static  i32                   work_id;
    private static AtelWorkerController* ctrl => Globals.Atel.controllers + ctrl_id;
    private static AtelBasicWorker*      work => ctrl->worker(work_id);

    public static class Parser {
        private static List<Stmt> _func_statements = [];
        private static Stack<Expr> _stack = [];
        private static Queue<AtelDOpCode> _opcodes = [];

        private static AtelDOpCode consume() {
            return _opcodes.Dequeue();
        }

        private static AtelDOpCode consume(AtelInst value) {
            if (_opcodes.IsEmpty()) throw new InvalidOperationException($"Expected {value}, but queue was empty");
            if (_opcodes.Peek().inst != value) throw new InvalidOperationException($"Expected {value}, got {consume().inst}");
            return _opcodes.Dequeue();
        }

        private static i16 consume_operand() {
            return (i16)consume().operand!;
        }

        public static Block parse(Queue<AtelDOpCode> opcodes) {
            _func_statements = [];
            _stack = [];
            _opcodes = opcodes;
            while (!opcodes.IsEmpty()) {
                _func_statements.Add(statement());
            }
            return new(0, _func_statements);
        }

        public static Expr expression() {
            if (_opcodes.IsEmpty()) {
                return new ParseErrorExpr("Tried to parse expression from no opcodes");
            }

            Expr? ret_expr = null;

            bool first_passthrough = true;
            while (true) {
                i32 offset = _opcodes.Peek().offset;
                Func<Expr> gen = _opcodes.Peek().inst switch {
                    AtelInst.PUSHII => () => new Literal(offset, consume_operand()),
                    AtelInst.PUSHI => () => new Literal(offset, work->table_int[consume_operand()]),
                    AtelInst.PUSHF => () => new Literal(offset, work->table_float[consume_operand()]),

                    >= AtelInst.PUSHI0 and <= AtelInst.PUSHI3 => () => new IntRegister(offset, (i16)((u8)consume().inst - (u8)AtelInst.PUSHI0)),
                    >= AtelInst.PUSHF0 and <= AtelInst.PUSHF9 => () => new FloatRegister(offset, (i16)((u8)consume().inst - (u8)AtelInst.PUSHF0)),

                    AtelInst.PUSHV => () => new Variable(offset, consume_operand()),
                    AtelInst.PUSHAR => () => new Variable(offset, consume_operand(), _stack.PopOrDefault(new ParseErrorExpr("Array index missing from stack"))),
                    AtelInst.PUSHARP => () => new VariablePointer(offset, consume_operand(), _stack.PopOrDefault(new ParseErrorExpr("Array index missing from stack"))),

                    AtelInst.PUSHX => () => { consume(); return new RegisterX(offset); },
                    AtelInst.PUSHY => () => { consume(); return new RegisterY(offset); },
                    AtelInst.PUSHA => () => { consume(); return new RegisterA(offset); },

                    AtelInst.REPUSH => () => { consume(); return _stack.PeekOrDefault(new ParseErrorExpr("Repushed value missing from stack")); },

                    AtelInst.UMINUS
                    or AtelInst.NOT
                    or AtelInst.BNOT => () => new PrefixOp(offset, consume().inst, _stack.PopOrDefault(new ParseErrorExpr("Operand missing from stack"))),

                    AtelInst.OR or AtelInst.EOR or AtelInst.AND
                    or AtelInst.LOR or AtelInst.LAND
                    or AtelInst.EQ or AtelInst.NE
                    or AtelInst.GT or AtelInst.LS or AtelInst.GTE or AtelInst.LSE
                    or AtelInst.GTU or AtelInst.LSU or AtelInst.GTEU or AtelInst.LSEU
                    or AtelInst.BOFF or AtelInst.BON
                    or AtelInst.SLL or AtelInst.SRL
                    or AtelInst.ADD or AtelInst.SUB
                    or AtelInst.MUL or AtelInst.DIV
                    or AtelInst.MOD => () => new InfixOp(
                            offset,
                            _stack.PopOrDefault(new ParseErrorExpr("Operand missing from stack")),
                            consume().inst,
                            _stack.PopOrDefault(new ParseErrorExpr("Operand missing from stack"))
                        ),

                    AtelInst.CALL => () => {
                        u16 id = BitConverter.ToUInt16(BitConverter.GetBytes(consume_operand()));
                        AtelCallTargets.CallTarget ct = AtelCallTargets.CallTarget.get(id);
                        i32 args_count = ct.args.Length;

                        Expr[] args = new Expr[args_count];
                        for (i32 i = args_count - 1; i >= 0; i--) {
                            args[i] = _stack.PopOrDefault(new ParseErrorExpr("Parameter missing from stack"));
                        }

                        return new FnCall(offset, id, args);
                    },

                    AtelInst.REQ or AtelInst.REQSW or AtelInst.REQEW
                    or AtelInst.BREQ or AtelInst.BREQSW or AtelInst.BREQEW
                    or AtelInst.PREQ or AtelInst.PREQSW or AtelInst.PREQEW
                    or AtelInst.FREQ or AtelInst.FREQSW or AtelInst.FREQEW
                    or AtelInst.TREQ or AtelInst.TREQSW or AtelInst.TREQEW
                    or AtelInst.BFREQ or AtelInst.BFREQSW or AtelInst.BFREQEW
                    or AtelInst.BTREQ or AtelInst.BTREQSW or AtelInst.BTREQEW => () => new Run(
                            offset,
                            consume().inst,
                            _stack.PopOrDefault(new ParseErrorExpr("Entry point missing from stack")),
                            _stack.PopOrDefault(new ParseErrorExpr("Worker ID missing from stack")),
                            _stack.PopOrDefault(new ParseErrorExpr("Level missing from stack"))
                        ),

                    // If it's anything else, we're done with the expression
                    _ => () => {
                        if (first_passthrough) throw new NotImplementedException($"Could not parse opcode {consume().inst}");
                        return ret_expr = _stack.Pop();
                    },
                };

                Expr expr = gen();
                first_passthrough = false;

                if (ret_expr != null) return ret_expr;

                _stack.Push(expr);
            }
        }

        public static Stmt statement() {
            if (_opcodes.IsEmpty()) {
                return new ParseErrorStmt("Tried to parse statement from no opcodes");
            }

            int offset = _opcodes.Peek().offset;
            // Func to use a switch expression because of how much nicer the syntax is
            Func<Stmt> gen = _opcodes.Peek().inst switch {
                AtelInst.RET
                or AtelInst.DRET
                or AtelInst.RTS => () => { consume(); return new Return(offset, _stack.TryPop()); }, //WARN: This is not at all how it works, but I don't care to fix it atm

                AtelInst.POPXNCJMP => () => branch(_stack.PopOrDefault(new ParseErrorExpr("Branch condition missing from stack"))),
                AtelInst.POPXCJMP => () => branch(_stack.PopOrDefault(new ParseErrorExpr("Branch condition missing from stack"))),
                AtelInst.POPY => () => match(_stack.PopOrDefault(new ParseErrorExpr("Switch variable missing from stack"))),
                AtelInst.JMP => () => new Goto(offset, consume_operand()),
                AtelInst.JSR => () => new GotoSubroutine(offset, consume_operand()),

                AtelInst.POPX => () => {
                        consume();
                        return new Assignment(
                            offset,
                            new RegisterX(offset),
                            _stack.PopOrDefault(new ParseErrorExpr("Assignment value missing from stack"))
                        );
                    },
                AtelInst.POPA => () => {
                        consume();
                        return new Assignment(
                            offset,
                            new RegisterA(offset),
                            _stack.PopOrDefault(new ParseErrorExpr("Assignment value missing from stack"))
                        );
                    },

                // I'm not sure what the difference is exactly so I can't represent V vs VL and AR vs ARL correctly
                AtelInst.POPV or AtelInst.POPVL => () => new Assignment(
                        offset,
                        new Variable(offset, consume_operand()),
                        _stack.PopOrDefault(new ParseErrorExpr("Assignment value missing from stack"))
                    ),
                AtelInst.POPAR or AtelInst.POPARL => () => {
                    Expr value = _stack.PopOrDefault(new ParseErrorExpr("Assignment value missing from stack"));
                    Variable var = new Variable(
                        offset,
                        consume_operand(),
                        _stack.PopOrDefault(new ParseErrorExpr("Array index missing from stack"))
                    );
                    return new Assignment(offset, var, value);
                },

                >= AtelInst.POPF0 and <= AtelInst.POPF9 => () => new Assignment(
                        offset,
                        new FloatRegister(offset, (i16)((u8)consume().inst - (u8)AtelInst.POPF0)),
                        _stack.PopOrDefault(new ParseErrorExpr("Assignment value missing from stack"))
                    ),
                >= AtelInst.POPI0 and <= AtelInst.POPI3 => () => new Assignment(
                        offset,
                        new IntRegister(offset, (i16)((u8)consume().inst - (u8)AtelInst.POPI0)),
                        _stack.PopOrDefault(new ParseErrorExpr("Assignment value missing from stack"))
                    ),

                AtelInst.REQWAIT or AtelInst.PREQWAIT => () => {
                        consume();
                        return new Await(
                            offset,
                            _stack.PopOrDefault(new ParseErrorExpr("Entry point missing from stack")),
                            _stack.PopOrDefault(new ParseErrorExpr("Worker ID missing from stack"))
                        );
                    },

                AtelInst.REQCHG => () => {
                        consume();
                        return new Change(
                            offset,
                            work_id,
                            _stack.PopOrDefault(new ParseErrorExpr("Value missing from stack")),
                            _stack.PopOrDefault(new ParseErrorExpr("Old entry point missing from stack")),
                            _stack.PopOrDefault(new ParseErrorExpr("New entry point missing from stack"))
                        );
                    },

                AtelInst.CALLPOPA => () => {
                    u16 id = BitConverter.ToUInt16(BitConverter.GetBytes(consume_operand()));
                    AtelCallTargets.CallTarget ct = AtelCallTargets.CallTarget.get(id);
                    i32 args_count = ct.args.Length;

                    Expr[] args = new Expr[args_count];
                    for (i32 i = args_count - 1; i >= 0; i--) {
                        args[i] = _stack.PopOrDefault(new ParseErrorExpr("Parameter missing from stack"));
                    }

                    return new DirectExpr(offset, new FnCall(offset, id, args));
                },

                _ => () => {
                    // There must be some expression before the statement
                    Expr expr = expression();
                    _stack.Push(expr);
                    return statement();
                },
            };

            return gen();
        }

        public static Branch branch(Expr cond) {
            List<Stmt> body = [];
            List<Stmt> else_body = [];

            if (_opcodes.Peek().inst != AtelInst.POPXNCJMP && _opcodes.Peek().inst != AtelInst.POPXCJMP) {
                throw new NotImplementedException($"Tried to create a branch statement using {consume().inst}");
            }

            if (_opcodes.Peek().inst == AtelInst.POPXCJMP) {
                i32 offset = _opcodes.Peek().offset;
                return new(
                    offset,
                    cond,
                    new(offset, [ new Goto(offset, (i16)consume().operand!) ]),
                    new(offset, [])
                );
            }

            /* How do we parse popxncjmp?
             *
             *   popxncjmp jELSE/jOUT
             *   |body|
             *   (jmp jOUT
             * jELSE:
             *   |else body|)
             * jOUT:
             */

            var popxncjmp = consume(AtelInst.POPXNCJMP);
            i16 else_jmp_idx = (i16)popxncjmp.operand!;
            i32 else_jmp_offset = (i32)work->table_jump[else_jmp_idx];

            while (_opcodes.Peek().offset < else_jmp_offset
                    && _opcodes.Peek().inst != AtelInst.JMP
                    && _opcodes.Peek().inst != AtelInst.RET
                    && _opcodes.Peek().inst != AtelInst.DRET
                    && _opcodes.Peek().inst != AtelInst.RTS) {
                body.Add(statement());
            }

            if (_opcodes.Peek().offset < else_jmp_offset || _opcodes.Peek().inst == AtelInst.JMP) {
                body.Add(statement());
            }

            // If the last instruction is a `jmp` that jumps to after the start of the potential `else` block
            if (body[^1] is Goto jmp && work->table_jump[jmp.jump_idx] >= else_jmp_offset) {
                // There's an `else`! (hopefully) (we don't actually know) (there isn't a way to tell that works 100% of the time)
                Goto out_jmp = jmp;
                i32 out_jmp_offset = (i32)work->table_jump[out_jmp.jump_idx];
                body.RemoveAt(body.Count - 1);

                while (_opcodes.Peek().offset < out_jmp_offset
                        && _opcodes.Peek().inst != AtelInst.JMP
                        && _opcodes.Peek().inst != AtelInst.RET
                        && _opcodes.Peek().inst != AtelInst.DRET
                        && _opcodes.Peek().inst != AtelInst.RTS) {
                    else_body.Add(statement());
                }

                if (_opcodes.Peek().offset < out_jmp_offset && _opcodes.Peek().inst != AtelInst.JMP) {
                    else_body.Add(statement());
                }

                if (body.IsEmpty()) {
                    cond = new PrefixOp(cond.offset, AtelInst.NOT, cond);
                    body = else_body;
                    else_body = [];
                }
            }

            return new(popxncjmp.offset, cond, new(popxncjmp.offset + 3, body), new(else_jmp_offset, else_body));
        }

        public static Switch match(Expr value) {
            Dictionary<Expr, Block> cases = [];
            Block? def = null;

            /* How do we parse a switch?
             *
             *   popy
             *   jmp jCONDS0
             * jCOND0:
             *   |case0 body|
             *   (jmp jOUT OR ret OR dret OR rts)
             * jCOND1:
             *   |case1 body|
             *   jmp jOUT
             * jDEFAULT:
             *   |default body|
             *   jmp jOUT
             * jCONDS:
             *   |cond0a|
             *   popxncjmp jCONDS1 ; used as && for next cond
             *   |cond0b|
             *   popxcjmp jCOND0
             * jCONDS1:
             *   |cond1|
             *   popxcjmp jCOND1
             *   jmp jDEFAULT
             * jOUT:
             */

            i32 switch_offset = consume(AtelInst.POPY).offset;

            i16 conds_jmp_idx = (i16)consume(AtelInst.JMP).operand!;
            i32 conds_jmp_offset = (i32)work->table_jump[conds_jmp_idx];

            i16? out_jmp_idx = null;

            // jmp idx -> block info
            Dictionary<i16, (i32 offset, List<Stmt> block)> case_blocks = [];

            // This is purely for fallthroughs and I am livid about it
            // jmp idx -> case block active
            Dictionary<i16, bool> active_case_blocks = [];

            while (_opcodes.Peek().offset < conds_jmp_offset) {
                i32 case_block_offset = _opcodes.Peek().offset;

                for (i16 case_jmp_idx = (i16)(conds_jmp_idx + 1); case_jmp_idx < work->script_header->jump_count; case_jmp_idx++) {
                    if (case_block_offset == work->table_jump[case_jmp_idx]) {
                        case_blocks.Add(case_jmp_idx, ((i32)work->table_jump[case_jmp_idx], [ ]));
                        active_case_blocks[case_jmp_idx] = true;
                    }
                }

                if (_opcodes.Peek().inst == AtelInst.JMP
                 || _opcodes.Peek().inst == AtelInst.RET
                 || _opcodes.Peek().inst == AtelInst.DRET
                 || _opcodes.Peek().inst == AtelInst.RTS) {
                    Stmt end_stmt;
                    if (_opcodes.Peek().inst != AtelInst.JMP) {
                        end_stmt = statement();
                    } else {
                        end_stmt = new Break(_opcodes.Peek().offset, consume_operand());
                        out_jmp_idx = ((Break)end_stmt).jmp_idx;
                    }

                    foreach (var block in active_case_blocks) {
                        if (!block.Value) continue;

                        case_blocks[block.Key].block.Add(end_stmt);
                        active_case_blocks[block.Key] = false;
                    }

                    continue;
                }

                Stmt stmt = statement();

                foreach (var block in active_case_blocks) {
                    if (block.Value) {
                        case_blocks[block.Key].block.Add(stmt);

                        // I hope this works
                        // Update: I have no idea what this is doing
                        if (case_blocks[block.Key].block[^1] is Branch branch) {
                            if (branch.body.length > 0
                             && branch.body.body[^1] is Goto
                             && branch.else_body.length > 0
                             && branch.else_body.body[^1] is Goto) {
                                active_case_blocks[block.Key] = false;
                            }
                        }
                    }
                }
            }

            i32 out_jmp_offset = (i32)work->table_jump[(i16)out_jmp_idx!];

            void sort_rY(Expr expr) {
                // Make sure rY is on the left
                if (expr is InfixOp { rhs: RegisterY rhs1 } op1) {
                    op1.rhs = op1.lhs;
                    op1.lhs = new RegisterY(rhs1.offset);
                }

                // Also make sure > before <, >= before <=, etc.
                if (expr is InfixOp { lhs: InfixOp lhs2, rhs: InfixOp rhs2 } op2) {
                    if (rhs2.op.ToString().StartsWith("GT") && lhs2.op.ToString().StartsWith("LS")) {
                        Expr tmp = rhs2;
                        op2.rhs = op2.lhs;
                        op2.lhs = tmp;
                    }
                }
            }

            // We've reached our `jCONDS:`
            // I just hope this works lol
            // I'll explain when asked (maybe) (no promises)
            while (_opcodes.Peek().offset < out_jmp_offset) {
                if (_opcodes.Peek().inst == AtelInst.JMP) {
                    // Default case
                    var default_block_info = case_blocks[consume_operand()];
                    def = new(default_block_info.offset, default_block_info.block);
                    continue;
                }

                Expr case_cond = expression();
                sort_rY(case_cond);

                while (_opcodes.Peek().inst == AtelInst.POPXNCJMP) {
                    consume(AtelInst.POPXNCJMP); // && next cond

                    Expr next_case_cond = expression();
                    sort_rY(next_case_cond);

                    case_cond = new InfixOp(case_cond.offset, case_cond, AtelInst.LAND, next_case_cond);
                    sort_rY(case_cond);
                }

                var block_info = case_blocks[consume_operand()]; // consumes jump (`popxncjmp`) to case block
                cases[case_cond] = new(block_info.offset, block_info.block);
            }

            foreach (var case_block in cases.Values)  {

            }

            return new(switch_offset, value, cases, def);
        }
    }
}
