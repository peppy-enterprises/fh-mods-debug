using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

using Fahrenheit.Core;
using Fahrenheit.Core.FFX;
using Fahrenheit.Core.FFX.Atel;

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

    public struct AtelDecompFunc(List<AtelDOpCode> opcodes, i32 offset) {
        public List<AtelDOpCode> opcodes = opcodes;
        public AtelAst.Block     ast     = AtelAst.Cleaner.clean( AtelAst.Parser.parse( new(opcodes) ) );
        public i32               offset  = offset;
        public i32               entry_point;
    }

    private static List<AtelDecompFunc> _funcs = [];
    internal static AtelDecompTarget target;

    private static void decompile() {
        decompile_funcs(target.ctrl_id, target.work_id, ref _funcs);
    }

    private static void decompile_funcs(i32 ctrl_id, i32 work_id, ref List<AtelDecompFunc> funcs) {
        AtelWorkerController* ctrl = Globals.Atel.controllers + ctrl_id;
        if (ctrl->namespace_count - 1 < work_id) return;

        AtelBasicWorker* work = ctrl->worker(work_id);

        ReadOnlySpan<u32> entries = work->table_entry_points;

        AtelAst.ctrl_id = ctrl_id;
        AtelAst.work_id = work_id;

        for (i32 i = 0; i < work->script_header->entry_point_count; i++) {
            u32 func_size = 0;

            for (i32 j = i + 1; (j < work->script_header->entry_point_count) && (func_size <= 0); j++) {
                func_size = entries[j] - entries[i];
            }

            if (func_size <= 0) { // it's the last func
                if (work_id + 1 >= ctrl->namespace_count) {

                    // There isn't a next worker to determine the func end
                    func_size = work->script_chunk->code_length - entries[i];
                } else {
                    // Use the next worker's first func offset to determine the func end
                    for (i32 j = work_id + 1; j < ctrl->namespace_count; j++) {
                        AtelBasicWorker* next_work = ctrl->worker(j);
                        if (next_work->script_header->entry_point_count <= 0) continue;
                        if (next_work->script_chunk != work->script_chunk) {
                            func_size = work->script_chunk->code_length - entries[i];
                            break;
                        }

                        func_size = next_work->table_entry_points[0] - entries[i];
                        break;
                    }
                }
            }

            u8[] bytes = new u8[func_size];
            isize func_ptr = (isize)(work->code_ptr + entries[i]);
            Marshal.Copy(func_ptr, bytes, 0, (i32)func_size);

            //TODO: Define the entry point properly (hard) (sad face)
            funcs.Add(new(AtelDisassembler.disasm_bytes((i32)entries[i], bytes), (i32)entries[i]));
        }
    }

    public static void update_funcs() {
        _funcs.Clear();
        load_symbol_map();
        decompile();
    }

    private static void init() {
        update_funcs();
        _init_done = true;
    }

    private static bool _init_done;
    public static void render() {
        if (!_init_done) init();

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
            target.work_id = Math.Clamp(target.work_id, 0, ctrl->namespace_count - 1);
            target.selected_func = 0;
            update_funcs();
        }

        if (_funcs.Count == 0) {
            ctrl = Globals.Atel.controllers + target.ctrl_id;
            if (ctrl->namespace_count == 0) {
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

        ImGui.SameLine();
        if (ImGui.Button("Save decompilation")) {
            using StreamWriter sw = new($"./fahrenheit/modules/fhdbg/out/{AtelDebugger.event_name}.txt");
            FhLog.Info($"Saving decompilation of {AtelDebugger.event_name}");
            for (int ctrl_idx = 0; ctrl_idx < 7; ctrl_idx++) {
                AtelWorkerController* controller = Globals.Atel.controllers + ctrl_idx;
                for (int worker_idx = 0; worker_idx < controller->namespace_count; worker_idx++) {
                    AtelBasicWorker* work = controller->worker(worker_idx);
                    FhLog.Info($"Saving worker {worker_idx:X2} @ {(nint)work:X8}");
                    FhLog.Info($"  Saving {work->table_entry_points.Length} functions");
                    FhLog.Info($"  Using {work->table_jump.Length} jumps ({work->script_header->jump_count})");
                    List<AtelDecompFunc> out_funcs = [];
                    decompile_funcs(ctrl_idx, worker_idx, ref out_funcs);
                    for (int func_idx = 0; func_idx < work->script_header->entry_point_count; func_idx++) {
                        AtelDecompFunc func = out_funcs[func_idx];
                        sw.Write($"w{worker_idx:X2}e{func_idx:X2} ");
                        sw.Write(func.ast.ToString());
                        sw.Write("\n\n");
                    }
                }
            }
        }

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
        AtelDecompFunc func = _funcs[target.selected_func];

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
                        ImGui.TextColored(AtelHighlight.InstType.Pop.to_color(),  "popa");
                        break;
                    case AtelInst.POPXJMP:
                        ImGui.TextColored(AtelHighlight.InstType.Pop.to_color(),  "popx");
                        ImGui.TextColored(AtelHighlight.InstType.Jump.to_color(), "jmp");
                        break;
                    case AtelInst.POPXCJMP:
                        ImGui.TextColored(AtelHighlight.InstType.Pop.to_color(),  "popx");
                        ImGui.TextColored(AtelHighlight.InstType.Jump.to_color(), "cjmp");
                        break;
                    case AtelInst.POPXNCJMP:
                        ImGui.TextColored(AtelHighlight.InstType.Pop.to_color(),  "popx");
                        ImGui.TextColored(AtelHighlight.InstType.Jump.to_color(), "ncjmp");
                        break;
                    default:
                        Vector4 inst_color = AtelHighlight.inst_to_color(opcode.inst);
                        ImGui.TextColored(inst_color, opcode.inst.ToString().ToLower());
                        break;
                }

                bool should_newline_operand = opcode.inst is >= AtelInst.POPXJMP and <= AtelInst.POPXNCJMP;

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
                    "",
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
        AtelDecompFunc func = _funcs[target.selected_func];

        ImGui.Text(func.ast.ToString());
    }

    // id -> <from_offset -> name>
    private static readonly Dictionary<string, Dictionary<int, string>> _symbol_map = [];

    public static void load_symbol_map() {
        string map_path = $"./fahrenheit/modules/fhdbg/data/atel/symbols/{AtelDebugger.event_name}.ini";

        using StreamReader sr = new(
            new FileStream(map_path, FileMode.OpenOrCreate)
        );

        // Ad-hoc .ini-ish impl follows
        while (sr.Peek() >= 0) {
            string line = sr.ReadLine()!;

            string[] pair = line.Split('=');
            if (pair.Length != 2)
                throw new InvalidDataException($"Expected key=value pairs, got tuple of length {pair.Length}");

            string[] key = pair[0].Split(':');
            if (key.Length > 2)
                throw new InvalidDataException($"Expected id or id:from_offset pair, go tuple of length {key.Length}");

            string id = key[0];
            int from_offset = key.Length > 1 ? Int32.Parse(key[1], NumberStyles.HexNumber) : 0;
            string symbol = pair[1];

            _symbol_map.TryAdd(id, []);
            _symbol_map[id][from_offset] = symbol;
        }
    }

    public static void change_symbol(string id, int at_offset, string value) {
        _symbol_map[id][at_offset] = value;
    }

    public static string get_symbol(AtelAst.AssignableExpr expr) {
        string default_name = expr.get_default_name();
        if (!_symbol_map.ContainsKey(default_name)) return default_name;

        int last_offset = 0;
        string symbol = default_name;
        foreach (int offset in _symbol_map[default_name].Keys) {
            if (last_offset <= offset && offset <= expr.offset) {
                last_offset = offset;
                symbol = _symbol_map[default_name][offset];
            }
        }

        return symbol;
    }
}
