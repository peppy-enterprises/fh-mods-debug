using System;
using System.Collections.Generic;

using Fahrenheit.CoreLib.FFX;
using Fahrenheit.CoreLib.FFX.Atel;
using static Fahrenheit.Modules.Debug.Windows.AtelDecomp.AtelDecompiler;

namespace Fahrenheit.Modules.Debug.Windows.AtelDecomp;

internal unsafe static partial class AtelAst {
    static AtelWorkerController* ctrl => Globals.Atel.controllers + target.ctrl_id;
    static AtelBasicWorker* work => ctrl->worker(target.work_id);

    public static class Parser {
        private static List<Stmt> func_statements = [];
        private static Stack<Expr> stack = [];
        private static Queue<AtelDOpCode> opcodes = [];

        private static AtelDOpCode consume() {
            return opcodes.Dequeue();
        }

        private static AtelDOpCode consume(AtelInst value) {
            if (opcodes.IsEmpty()) throw new InvalidOperationException($"Expected {value}, but queue was empty");
            if (opcodes.Peek().inst != value) throw new InvalidOperationException($"Expected {value}, got {consume().inst}");
            return opcodes.Dequeue();
        }

        private static i16 consume_operand() {
            return (i16)consume().operand!;
        }

        public static Block parse(Queue<AtelDOpCode> opcodes) {
            func_statements = [];
            stack = [];
            Parser.opcodes = opcodes;
            while (!opcodes.IsEmpty()) {
                func_statements.Add(statement());
            }
            return new([.. func_statements]);
        }

        public static Expr expression() {
            if (opcodes.IsEmpty()) {
                return new ParseErrorExpr("Tried to parse expression from no opcodes");
            }

            Expr? ret_expr = null;

            bool first_passthrough = true;
            while (true) {
                Func<Expr> gen = opcodes.Peek().inst switch {
                    AtelInst.PUSHII => () => new Literal(consume_operand()),
                    AtelInst.PUSHI => () => new Literal(work->table_int[consume_operand()]),
                    AtelInst.PUSHF => () => new Literal(work->table_float[consume_operand()]),

                    >= AtelInst.PUSHI0 and <= AtelInst.PUSHI3 => () => new IntRegister((i16)((u8)consume().inst - (u8)AtelInst.PUSHI0)),
                    >= AtelInst.PUSHF0 and <= AtelInst.PUSHF9 => () => new FloatRegister((i16)((u8)consume().inst - (u8)AtelInst.PUSHF0)),

                    AtelInst.PUSHV => () => new Variable(consume_operand()),
                    AtelInst.PUSHAR => () => new Variable(consume_operand(), stack.PopOrDefault(new ParseErrorExpr("Array index missing from stack"))),
                    AtelInst.PUSHARP => () => new VariablePointer(consume_operand(), stack.PopOrDefault(new ParseErrorExpr("Array index missing from stack"))),

                    AtelInst.PUSHX => () => { consume(); return new RegisterX(); },
                    AtelInst.PUSHY => () => { consume(); return new RegisterY(); },
                    AtelInst.PUSHA => () => { consume(); return new RegisterA(); },

                    AtelInst.REPUSH => () => { consume(); return stack.PeekOrDefault(new ParseErrorExpr("Repushed value missing from stack")); },

                    AtelInst.UMINUS
                    or AtelInst.NOT
                    or AtelInst.BNOT => () => new PrefixOp(consume().inst, stack.PopOrDefault(new ParseErrorExpr("Operand missing from stack"))),

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
                            stack.PopOrDefault(new ParseErrorExpr("Operand missing from stack")),
                            consume().inst,
                            stack.PopOrDefault(new ParseErrorExpr("Operand missing from stack"))
                        ),

                    AtelInst.CALL => () => {
                        u16 id = BitConverter.ToUInt16(BitConverter.GetBytes(consume_operand()));
                        AtelCallTargets.CallTarget ct = AtelCallTargets.CallTarget.get(id);
                        i32 args_count = ct.args.Length;

                        Expr[] args = new Expr[args_count];
                        for (i32 i = args_count - 1; i >= 0; i--) {
                            args[i] = stack.PopOrDefault(new ParseErrorExpr("Parameter missing from stack"));
                        }

                        return new FnCall(id, args);
                    },

                    AtelInst.REQ or AtelInst.REQSW or AtelInst.REQEW
                    or AtelInst.BREQ or AtelInst.BREQSW or AtelInst.BREQEW
                    or AtelInst.PREQ or AtelInst.PREQSW or AtelInst.PREQEW
                    or AtelInst.FREQ or AtelInst.FREQSW or AtelInst.FREQEW
                    or AtelInst.TREQ or AtelInst.TREQSW or AtelInst.TREQEW
                    or AtelInst.BFREQ or AtelInst.BFREQSW or AtelInst.BFREQEW
                    or AtelInst.BTREQ or AtelInst.BTREQSW or AtelInst.BTREQEW => () => new Run(
                            consume().inst,
                            stack.PopOrDefault(new ParseErrorExpr("Entry point missing from stack")),
                            stack.PopOrDefault(new ParseErrorExpr("Worker ID missing from stack")),
                            stack.PopOrDefault(new ParseErrorExpr("Level missing from stack"))
                        ),

                    // If it's anything else, we're done with the expression
                    _ => () => {
                        if (first_passthrough) throw new NotImplementedException($"Could not parse opcode {consume().inst}");
                        return ret_expr = stack.Pop();
                    }
                };

                Expr expr = gen();
                first_passthrough = false;

                if (ret_expr != null) return ret_expr;

                stack.Push(expr);
            }
        }

        public static Stmt statement() {
            if (opcodes.IsEmpty()) {
                return new ParseErrorStmt("Tried to parse statement from no opcodes");
            }

            // Func to use a switch expression because of how much nicer the syntax is
            Func<Stmt> gen = opcodes.Peek().inst switch {
                AtelInst.RET
                or AtelInst.DRET
                or AtelInst.RTS => () => { consume(); return new Return(stack.TryPop()); }, //WARN: This is not at all how it works, but I don't care to fix it atm

                AtelInst.POPXNCJMP => () => branch(stack.PopOrDefault(new ParseErrorExpr("Branch condition missing from stack"))),
                AtelInst.POPXCJMP => () => branch(stack.PopOrDefault(new ParseErrorExpr("Branch condition missing from stack"))),
                AtelInst.POPY => () => match(stack.PopOrDefault(new ParseErrorExpr("Switch variable missing from stack"))),
                AtelInst.JMP => () => new Goto(consume_operand()),
                AtelInst.JSR => () => new GotoSubroutine(consume_operand()),

                AtelInst.POPX => () => {
                        consume();
                        return new Assignment(
                            new RegisterX(),
                            stack.PopOrDefault(new ParseErrorExpr("Assignment value missing from stack"))
                        );
                    },
                AtelInst.POPA => () => {
                        consume();
                        return new Assignment(
                            new RegisterA(),
                            stack.PopOrDefault(new ParseErrorExpr("Assignment value missing from stack"))
                        );
                    },

                // I'm not sure what the difference is exactly so I can't represent V vs VL and AR vs ARL correctly
                AtelInst.POPV or AtelInst.POPVL => () => new Assignment(
                        new Variable(consume_operand()),
                        stack.PopOrDefault(new ParseErrorExpr("Assignment value missing from stack"))
                    ),
                AtelInst.POPAR or AtelInst.POPARL => () => new Assignment(
                        new Variable(
                            consume_operand(),
                            stack.PopOrDefault(new ParseErrorExpr("Array index missing from stack"))
                        ),
                        stack.PopOrDefault(new ParseErrorExpr("Assignment value missing from stack"))
                    ),

                >= AtelInst.POPF0 and <= AtelInst.POPF9 => () => new Assignment(
                        new FloatRegister((i16)((u8)consume().inst - (u8)AtelInst.POPF0)),
                        stack.PopOrDefault(new ParseErrorExpr("Assignment value missing from stack"))
                    ),
                >= AtelInst.POPI0 and <= AtelInst.POPI3 => () => new Assignment(
                        new IntRegister((i16)((u8)consume().inst - (u8)AtelInst.POPI0)),
                        stack.PopOrDefault(new ParseErrorExpr("Assignment value missing from stack"))
                    ),

                AtelInst.REQWAIT or AtelInst.PREQWAIT => () => {
                        consume();
                        return new Await(
                            stack.PopOrDefault(new ParseErrorExpr("Entry point missing from stack")),
                            stack.PopOrDefault(new ParseErrorExpr("Worker ID missing from stack"))
                        );
                    },

                AtelInst.REQCHG => () => {
                        consume();
                        return new Change(
                            stack.PopOrDefault(new ParseErrorExpr("Old entry point missing from stack")),
                            stack.PopOrDefault(new ParseErrorExpr("Old worker ID missing from stack")),
                            stack.PopOrDefault(new ParseErrorExpr("New entry point missing from stack")),
                            stack.PopOrDefault(new ParseErrorExpr("New worker ID missing from stack"))
                        );
                    },

                AtelInst.CALLPOPA => () => {
                    u16 id = BitConverter.ToUInt16(BitConverter.GetBytes(consume_operand()));
                    AtelCallTargets.CallTarget ct = AtelCallTargets.CallTarget.get(id);
                    i32 args_count = ct.args.Length;

                    Expr[] args = new Expr[args_count];
                    for (i32 i = args_count - 1; i >= 0; i--) {
                        args[i] = stack.PopOrDefault(new ParseErrorExpr("Parameter missing from stack"));
                    }

                    return new DirectExpr(new FnCall(id, args));
                },

                _ => () => {
                    // There must be some expression before the statement
                    Expr expr = expression();
                    stack.Push(expr);
                    return statement();
                },
            };

            return gen();
        }

        // I hate what I've done, yet there is not other way
        public static Branch branch(Expr cond) {
            List<Stmt> body = [];
            List<Stmt> else_body = [];

            if (opcodes.Peek().inst != AtelInst.POPXNCJMP && opcodes.Peek().inst != AtelInst.POPXCJMP) {
                throw new NotImplementedException($"Tried to create a branch statement using {consume().inst}");
            }

            if (opcodes.Peek().inst == AtelInst.POPXCJMP) {
                return new(cond, new([ new Goto((i16)consume().operand!) ]), new([]));
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

            i16 else_jmp_idx = (i16)consume(AtelInst.POPXNCJMP).operand!;
            i32 else_jmp_offset = (i32)work->table_jump[else_jmp_idx];

            while (opcodes.Peek().offset < else_jmp_offset
                    && opcodes.Peek().inst != AtelInst.JMP
                    && opcodes.Peek().inst != AtelInst.RET
                    && opcodes.Peek().inst != AtelInst.DRET
                    && opcodes.Peek().inst != AtelInst.RTS) {
                body.Add(statement());
            }

            if (opcodes.Peek().offset < else_jmp_offset && opcodes.Peek().inst != AtelInst.JMP) {
                body.Add(statement());
            }

            if (opcodes.Peek().inst == AtelInst.JMP) {
                body.Add(statement());
            }

            // If the last instruction is a `jmp` that jumps to after the next opcode
            if (body[^1] is Goto jmp && work->table_jump[jmp.jump_idx] > opcodes.Peek().offset) {
                // There's an `else`! (hopefully) (we don't actually know) (there isn't a way to tell that works 100% of the time)
                Goto out_jmp = jmp;
                i32 out_jmp_offset = (i32)work->table_jump[out_jmp.jump_idx];
                body.RemoveAt(body.Count - 1);

                while (opcodes.Peek().offset < out_jmp_offset
                        && opcodes.Peek().inst != AtelInst.JMP
                        && opcodes.Peek().inst != AtelInst.RET
                        && opcodes.Peek().inst != AtelInst.DRET
                        && opcodes.Peek().inst != AtelInst.RTS) {
                    else_body.Add(statement());
                }

                if (opcodes.Peek().offset < out_jmp_offset && opcodes.Peek().inst != AtelInst.JMP) {
                    else_body.Add(statement());
                }

                if (body.IsEmpty()) {
                    cond = new PrefixOp(AtelInst.NOT, cond);
                    body = else_body;
                    else_body = [];
                }
            }

            return new(cond, new([.. body]), new([.. else_body]));
        }

        public static Switch match(Expr value) {
            Dictionary<Expr, Stmt> cases = [];
            Stmt? def = null;

            /* How do we parse a switch?
             *
             *   popy
             *   jmp jCONDS0
             * jCOND0:
             *   |case0 body|
             *   jmp jOUT
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

            consume(AtelInst.POPY);

            i16 conds_jmp_idx = (i16)consume(AtelInst.JMP).operand!;
            i32 conds_jmp_offset = (i32)work->table_jump[conds_jmp_idx];

            i16? out_jmp_idx = null;

            List<Block> case_blocks = [];
            while (opcodes.Peek().offset < conds_jmp_offset) {
                List<Stmt> case_block = [];
                while (opcodes.Peek().inst != AtelInst.JMP) {
                    case_block.Add(statement());

                    // Special case for the *one* optimization they added
                    if (case_block[^1] is Branch branch) {
                        if (branch.body.length > 0 && branch.body.body[^1] is Goto jmp) {
                            break;
                        }
                    }
                }
                // We've reached our `jmp jOUT`
                if (out_jmp_idx.HasValue) consume(AtelInst.JMP);
                else out_jmp_idx = (i16)consume(AtelInst.JMP).operand!;

                case_blocks.Add(new([.. case_block]));
            }

            i32 out_jmp_offset = (i32)work->table_jump[(i16)out_jmp_idx!];

            // We've reached our `jCONDS:`
            List<Expr> case_conds = [];

            // I just hope this works lol
            // I'll explain when asked (maybe) (no promises)
            int i = -1;
            while (opcodes.Peek().offset < out_jmp_offset) {
                if (opcodes.Peek().inst == AtelInst.JMP) {
                    // Default case
                    def = case_blocks[i];
                    consume(AtelInst.JMP);
                    continue;
                }

                Expr case_cond = expression();
                while (opcodes.Peek().inst == AtelInst.POPXNCJMP) {
                    consume(AtelInst.POPXNCJMP); // && next cond

                    // Make sure rY is on the left
                    if (case_cond is InfixOp _op1 && _op1.rhs is RegisterY) {
                        _op1.rhs = _op1.lhs;
                        _op1.lhs = new RegisterY();
                    }

                    case_cond = new InfixOp(case_cond, AtelInst.LAND, expression());

                    // Also make sure > before <, >= before <=, etc.
                    if (case_cond is InfixOp _op2 && _op2.lhs is InfixOp _lhs && _op2.rhs is InfixOp _rhs) {
                        if (_rhs.op.ToString().StartsWith("GT") && _lhs.op.ToString().StartsWith("LS")) {
                            Expr tmp = _rhs;
                            _op2.rhs = _op2.lhs;
                            _op2.lhs = _rhs;
                        }
                    }
                }
                consume(AtelInst.POPXCJMP); // jump to case block

                cases[case_cond] = case_blocks[++i];
            }

            return new(value, cases, def);
        }
    }
}