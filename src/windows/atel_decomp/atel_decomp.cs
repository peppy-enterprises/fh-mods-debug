using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Fahrenheit.CoreLib.FFX;
using Fahrenheit.CoreLib.FFX.Atel;
using ImGuiNET;

namespace Fahrenheit.Modules.Debug.Windows.AtelDecomp;

internal unsafe static class AtelDecompiler {
    public struct AtelDecompTarget {
        public i32 ctrl_id;
        public i32 work_id;
        public i32 selected_func;
    }

    public struct AtelDOpCode {
        public i32 offset;
        public AtelInst inst;
        public i16? operand;
    }

    public struct AtelDecompFunc {
        public List<AtelDOpCode> opcodes;
        public AtelAst.Block ast;
        public i32 offset;
        public i32 entry_point;

        public AtelDecompFunc(List<AtelDOpCode> opcodes, i32 offset) {
            this.opcodes = opcodes;
            this.offset = offset;

            ast = AtelAst.Parser.parse(new(opcodes));
        }
    }

    private static List<AtelDecompFunc> funcs = [];
    internal static AtelDecompTarget target;

    private static void decompile() {
        AtelWorkerController* ctrl = Globals.Atel.controllers + target.ctrl_id;
        if (ctrl->runnable_script_count - 1 < target.work_id) return;

        AtelBasicWorker* work = ctrl->worker(target.work_id);
        if (work->script_header->entry_point_count < 0) return;

        i32* entries = (i32*)work->table_entry_points;
        i32 first_offset = entries[0];

        for (i32 i = 0; i < work->script_header->entry_point_count; i++) {
            i32 func_size = 0;

            for (i32 j = i + 1; (j < work->script_header->entry_point_count) && (func_size <= 0); j++) {
                func_size = entries[j] - entries[i];
            }

            if (func_size <= 0) { // it's the last func
                if (target.work_id + 1 >= ctrl->runnable_script_count) {
                    // There isn't a next worker to determine the func end
                    func_size = (i32)ctrl->script_chunk->code_length - entries[i];
                } else {
                    // Use the next worker's first func offset to determine the func end
                    for (i32 j = target.work_id + 1; j < ctrl->runnable_script_count && func_size <= 0; j++) {
                        AtelBasicWorker* next_work = ctrl->worker(j);
                        if (next_work->script_header->entry_point_count <= 0) continue;

                        func_size = (i32)next_work->table_entry_points[0] - entries[i];
                        break;
                    }
                }
            }

            u8[] bytes = new u8[func_size];
            isize func_ptr = (isize)(work->code_ptr + entries[i]);
            Marshal.Copy(func_ptr, bytes, 0, func_size);

            //TODO: Define the entry point properly (hard) (sad face)
            funcs.Add(new(AtelDisassembler.disasm_bytes(entries[i], bytes), entries[i]));
        }
    }

    public static void update_funcs() {
        funcs.Clear();
        decompile();
    }

    private static void init() {
        update_funcs();
        init_done = true;
    }

    private static bool init_done = false;
    public static void render() {
        if (!init_done) init();

        ImGui.Text("Controller:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f);
        ImGui.SameLine();

        if (ImGui.InputInt("##CtrlIdx", ref target.ctrl_id)) {
            target.ctrl_id = Math.Clamp(target.ctrl_id, 0, 7);
            target.work_id = 0;
            target.selected_func = 0;
            update_funcs();
        }

        ImGui.SameLine();
        ImGui.Text("Worker:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100f);
        ImGui.SameLine();

        AtelWorkerController* ctrl = Globals.Atel.controllers + target.ctrl_id;
        if (ImGui.InputInt("##WorkIdx", ref target.work_id)) {
            target.work_id = Math.Clamp(target.work_id, 0, ctrl->runnable_script_count - 1);
            target.selected_func = 0;
            update_funcs();
        }

        if (funcs.Count == 0) {
            ctrl = Globals.Atel.controllers + target.ctrl_id;
            if (ctrl->runnable_script_count == 0) {
                ImGui.Text($"Functions unavailable for controller #{target.ctrl_id}");
                return;
            }
        }

        AtelBasicWorker* worker = ctrl->worker(target.work_id);

        i32 func_count = worker->script_header->entry_point_count;
        string[] func_strs = new string[func_count];

        for (i32 i = 0; i < func_count; i++) {
            func_strs[i] = i.ToString();
        }

        ImGui.SameLine();
        ImGui.Text($"Function:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(150f);
        ImGui.SameLine();
        ImGui.Combo("##FunctionIdx", ref target.selected_func, func_strs, func_count);

        Vector2 child_size = ImGui.GetContentRegionAvail();
        child_size.X *= 0.5f;

        ImGui.BeginChild("Disassembly", child_size, ImGuiChildFlags.ResizeX | ImGuiChildFlags.Border);
        {
            ImGui.SeparatorText("Disassembly");
            render_func();
        }
        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("Decompilation", ImGui.GetContentRegionAvail(), ImGuiChildFlags.Border);
        {
            ImGui.SeparatorText("Decompilation");
            render_decomp();
        }
        ImGui.EndChild();
    }

    private static void render_func() {
        AtelDecompFunc func = funcs[target.selected_func];

        ImGui.BeginTable("", 5,
                ImGuiTableFlags.Resizable
              | ImGuiTableFlags.Hideable
              | ImGuiTableFlags.BordersOuter
              | ImGuiTableFlags.NoBordersInBodyUntilResize
              | ImGuiTableFlags.ScrollY);

        ImGui.TableSetupScrollFreeze(0, 1); // Make top row always visible
        ImGui.TableSetupColumn("Offset");
        ImGui.TableSetupColumn("Bytes");
        ImGui.TableSetupColumn("Instruction");
        ImGui.TableSetupColumn("Operand");
        ImGui.TableSetupColumn("Comment");
        ImGui.TableHeadersRow();

        // We use a list clipper so we don't lag the fuck out on large functions
        // ImGuiNet is a bit bugged here; only the "Ptr" struct actually contains the method definitions
        ImGuiListClipper clipper = new();
        ImGuiListClipperPtr p_clipper = &clipper;
        p_clipper.Begin(func.opcodes.Count);
        while (p_clipper.Step()) {
            for (i32 row = clipper.DisplayStart; row < clipper.DisplayEnd; row++) {
                ImGui.TableNextRow();

                AtelDOpCode opcode = func.opcodes[row];

                // Offset
                ImGui.TableSetColumnIndex(0);
                ImGui.Text($"{opcode.offset - func.offset:X4}");

                // Bytes
                ImGui.TableSetColumnIndex(1);
                StringBuilder sb = new();
                sb.Append($"{(u8)opcode.inst:X2}");
                if (opcode.operand != null) {
                    sb.Append($" {opcode.operand & 0xFF:X2}");
                    sb.Append($"{(opcode.operand >> 8) & 0xFF:X2}");
                }

                ImGui.Text(sb.ToString());

                // Opcode
                ImGui.TableSetColumnIndex(2);

                // Uncombine the combined opcodes to be nice
                switch (opcode.inst) {
                    case AtelInst.CALLPOPA:
                        ImGui.TextColored(AtelHighlight.InstType.Call.to_color(), "call");
                        ImGui.TextColored(AtelHighlight.InstType.Pop.to_color(), "popa");
                        break;
                    case AtelInst.POPXJMP:
                        ImGui.TextColored(AtelHighlight.InstType.Pop.to_color(), "popx");
                        ImGui.TextColored(AtelHighlight.InstType.Jump.to_color(), "jmp");
                        break;
                    case AtelInst.POPXCJMP:
                        ImGui.TextColored(AtelHighlight.InstType.Pop.to_color(), "popx");
                        ImGui.TextColored(AtelHighlight.InstType.Jump.to_color(), "cjmp");
                        break;
                    case AtelInst.POPXNCJMP:
                        ImGui.TextColored(AtelHighlight.InstType.Pop.to_color(), "popx");
                        ImGui.TextColored(AtelHighlight.InstType.Jump.to_color(), "ncjmp");
                        break;
                    default:
                        Vector4 inst_color = AtelHighlight.inst_to_color(opcode.inst);
                        ImGui.TextColored(inst_color, opcode.inst.ToString().ToLower());
                        break;
                }

                bool should_newline_operand = opcode.inst >= AtelInst.POPXJMP && opcode.inst <= AtelInst.POPXNCJMP;

                // Operand
                if (opcode.operand == null) {
                    ImGui.TableSetColumnIndex(3);
                    ImGui.Text("");
                    ImGui.TableSetColumnIndex(4);
                    ImGui.Text("");
                    continue;
                }

                ImGui.TableSetColumnIndex(3);
                ImGui.Text($"{(should_newline_operand ? "\n" : "")}{(i16)opcode.operand}");

                ImGui.TableSetColumnIndex(4);
                string[] leading_zeroes = [
                    "000",
                    "00",
                    "0",
                    ""
                ];

                string operand_str = $"{opcode.operand:X}";
                ImGui.Text($"{(should_newline_operand ? "\n" : "")}[");
                ImGui.SameLine(0, 0);
                ImGui.TextColored(new() { X = 0.7f, Y = 0.7f, Z = 0.7f, W = 1f }, $"{(should_newline_operand ? "\n" : "")}{leading_zeroes[operand_str.Length - 1]}");
                ImGui.SameLine(0, 0);
                ImGui.Text($"{(should_newline_operand ? "\n" : "")}{operand_str}h]");
            }
        }

        ImGui.EndTable();
    }

    private static void render_decomp() {
        AtelDecompFunc func = funcs[target.selected_func];

        ImGui.Text(func.ast.ToString());
    }
}