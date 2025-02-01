using System;
using System.Collections.Generic;

namespace Fahrenheit.Modules.Debug.Windows.AtelDecomp;

internal unsafe static partial class AtelAst {
    // Functionally a 2nd step of the parser but shhhh
    public static class Cleaner {
        // Returns input for "chaining" (i.e. I was too lazy to edit AtelDecompFunc)
        internal static Block clean(Block ast) {
            List<i16> goto_indices = [];

            // Scan all statements for `goto`s
            for_all_stmts(ast, stmt => {
                if (stmt is Goto jmp) {
                    goto_indices.Add(jmp.jump_idx);
                }
            });

            List<(i16 idx, i32 offset)> labels = [];
            foreach (i16 idx in goto_indices) {
                labels.Add((idx, (i32)work->table_jump[idx]));
            }

            // Add labels where releveant
            i32 last_i = 0;
            enumerate_all_stmts(ast, (i, stmt) => {
                foreach (var label in labels) {
                    if (!goto_indices.Contains(label.idx)) continue;
                    if (label.offset < stmt.offset) {
                        ast.body.Insert(last_i, new Label(label.offset, label.idx));
                        goto_indices.Remove(label.idx);
                    }
                    last_i = i;
                }
            });

            // Parser can't reasonably be expected to detect loops
            add_loops(ast);

            return ast;
        }

        private static void add_loops(Block ast) {
            // Add for loops

            /* Pattern:
             *
             * Assignment; of VALUE
             * Label; jLOOP
             * Branch; (jumps to jOUT, but that's not retrievable)
             * Goto; jBODY
             * Label; jINC
             * Assignment; of VALUE, must be AtelInst.ADD or AtelInst.SUB
             * Label; jBODY
             * Goto; jLOOP
             */
            for_all_blocks(ast, block => {
                for (int i = 0; i < block.length; i++) {

                }
            });
        }

        internal static void for_all_blocks(Block ast, Action<Block> action) {
            action(ast);
            foreach (Stmt stmt in ast.body) {
                switch(stmt) {
                    case Branch branch:
                        for_all_blocks(branch.body, action);
                        for_all_blocks(branch.else_body, action);
                        break;
                    case Switch match:
                        foreach (var block in match.cases.Values)
                            for_all_blocks(block, action);
                        if (match.default_case != null)
                            for_all_blocks(match.default_case, action);
                        break;
                    case Block block:
                        for_all_blocks(block, action);
                        break;
                }
            }
        }

        internal static void for_all_stmts(Block ast, Action<Stmt> action) {
            enumerate_all_stmts(ast, (i, stmt) => action(stmt));
        }

        internal static void enumerate_all_stmts(Block ast, Action<i32, Stmt> action) {
            for (int i = 0; i < ast.length; i++) {
                switch (ast.body[i]) {
                    case Branch branch:
                        enumerate_all_stmts(branch.body, action);
                        enumerate_all_stmts(branch.else_body, action);
                        break;
                    case Switch match:
                        foreach (var block in match.cases.Values)
                            enumerate_all_stmts(block, action);
                        if (match.default_case != null)
                            enumerate_all_stmts(match.default_case, action);
                        break;
                    case Block block:
                        enumerate_all_stmts(block, action);
                        break;
                    default:
                        action(i, ast.body[i]);
                        break;
                }
            }
        }
    }
}
