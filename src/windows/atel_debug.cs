using System;
using System.Collections.Generic;
using System.Numerics;

using Fahrenheit.Core;
using Fahrenheit.Core.FFX;
using Fahrenheit.Core.FFX.Atel;
using Fahrenheit.Core.FFX.Battle;
using Fahrenheit.Modules.Debug.Windows.AtelDecomp;

using ImGuiNET;

namespace Fahrenheit.Modules.Debug.Windows;

public unsafe static class AtelDebugger {
    private const ImGuiKey SHORTCUT_OPEN = ImGuiKey.ModCtrl | ImGuiKey.ModShift | ImGuiKey.A;
    public static string event_name = String.Empty;
    public static u32 event_id = 0;

    public struct RecentSignalInfo {
        public i16 work_id;
        public i16 entry_id;
        public f32 last_updated;
    }

    private const f32 _RECENT_SIGNAL_FLASH_LENGTH = 0.5f;
    private const i32 _MAX_RECENT_SIGNALS = 120;
    private const f32 _MIN_SIGNAL_COL_WIDTH = 150f;
    private static readonly List<RecentSignalInfo>[] _recent_signals = new List<RecentSignalInfo>[7];

    static AtelDebugger() {
        for (i32 i = 0; i < _recent_signals.Length; i++) {
            _recent_signals[i] = new(_MAX_RECENT_SIGNALS);
        }
    }

    public static void clear_recent_signals() {
        foreach (var signals in _recent_signals) {
            signals.Clear();
        }
    }

    public static void add_recent_signal(u32 ctrl_idx, i16 worker_id, i16 entry_id) {
        var signals = _recent_signals[ctrl_idx];

        // Was it already added recently?
        for (i32 i = 0; i < signals.Count; i++) {
            RecentSignalInfo info = signals[i];
            if (info.work_id == worker_id && info.entry_id == entry_id) {
                info.last_updated = 0;
                signals[i] = info;
                return;
            }
        }

        // Can we just add?
        if (signals.Count < _MAX_RECENT_SIGNALS) {
            signals.Add(new RecentSignalInfo { work_id = worker_id, entry_id = entry_id });
            return;
        }

        // Find oldest, to replace
        f32 oldest_update = signals[0].last_updated;
        i32 oldest_idx = 0;
        for (i32 i = 1; i < signals.Count; i++) {
            RecentSignalInfo info = signals[i];
            if (info.last_updated > oldest_update) {
                oldest_update = info.last_updated;
                oldest_idx = i;
            }
        }

        signals[oldest_idx] = new RecentSignalInfo { work_id = worker_id, entry_id = entry_id };
    }

    public static void update() {
        for (i32 ctrl_idx = 0; ctrl_idx < 7; ctrl_idx++) {
            var signals = _recent_signals[ctrl_idx];

            for (i32 info_idx = 0; info_idx < signals.Count; info_idx++) {
                RecentSignalInfo info = signals[info_idx];
                info.last_updated += 1f/60f;
                signals[info_idx] = info;
            }
        }
    }

    private static bool _enabled;
    public static void render() {
        _enabled ^= ImGui.IsKeyPressed(ImGuiKey.F8);
        if (!_enabled) return;

        ImGui.Begin($"Atel Debugger - {event_name} [{event_id:X}h]###Atel Debugger");
        if (ImGui.BeginTabBar("Tabs")) {
            if (ImGui.BeginTabItem("Recent Signals")) {
                render_recent_signals();

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Decompiler")) {
                AtelDecompiler.render();

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
        ImGui.End();
    }

    private static void render_recent_signals() {
        i32 column_count = (i32)(ImGui.GetWindowWidth() / _MIN_SIGNAL_COL_WIDTH);
        if (column_count < 1) column_count = 1;

        string[] controller_names = [
            "Default", "", "", "Battle", "", "", "",
        ];

        for (i32 i = 0; i < 7; i++) {
            if (ImGui.CollapsingHeader($"{controller_names[i]} Controller (#{i})")) {
                render_recent_signals_table(i, column_count);
            }
        }
    }

    private static void render_recent_signals_table(i32 ctrl_idx, i32 column_count) {
        List<RecentSignalInfo> signals = _recent_signals[ctrl_idx];
        // ceil(MAX_RECENT_SIGNALS / column_count)
        i32 row_count = (_MAX_RECENT_SIGNALS * column_count + (column_count - 1)) / column_count;

        if (ImGui.BeginTable("Recent Signals", column_count)) {
            for (i32 row = 0; row < row_count; row++) {
                ImGui.TableNextRow();

                for (i32 col = 0; col < column_count; col++) {
                    ImGui.TableNextColumn();

                    i32 idx = row * column_count + col;
                    if (idx >= signals.Count) {
                        ImGui.EndTable();
                        return;
                    }

                    var info = signals[row * column_count + col];
                    f32 color_low = _RECENT_SIGNAL_FLASH_LENGTH - info.last_updated;
                    if (color_low < 0) color_low = 0;
                    float r = Math.Max(1f - color_low, 0);
                    float g = Math.Max(1f - color_low * 2f, 0);
                    float b = 1f;
                    float a = 1f;
                    Vector4 color = new() { X = r, Y = g, Z = b, W = a };

                    string worker_name = get_worker_name(ctrl_idx, info.work_id);
                    string entry_name = get_entry_name(ctrl_idx, info.work_id, info.entry_id);

                    ImGui.TextColored(color, $"{worker_name}::{entry_name}");
                }
            }

            ImGui.EndTable();
        }
    }

    private static string get_worker_name(i32 ctrl_idx, i32 work_id) {
        return ctrl_idx switch {
            0 => work_id switch { // Default Controller
                _ => $"w{work_id:X2}"
            },

            3 => work_id switch { // Battle Controller
                < 18 => get_chr_name(work_id),
                20 => $"{get_chr_name(work_id)} (A)",
                21 => $"{get_chr_name(work_id)} (B)",
                22 => $"{get_chr_name(work_id)} (C)",
                23 => $"{get_chr_name(work_id)} (D)",
                24 => $"{get_chr_name(work_id)} (E)",
                25 => $"{get_chr_name(work_id)} (F)",
                26 => $"{get_chr_name(work_id)} (G)",
                27 => $"{get_chr_name(work_id)} (H)",
                _ => $"w{work_id:X2}"
            },

            _ => $"w{work_id:X2}"
        };
    }

    private static string get_entry_name(i32 ctrl_idx, i32 work_id, i32 entry_id) {
        if (entry_id == 0) return "init";
        if (entry_id == 1) return "main";

        AtelBasicWorker* worker = Globals.Atel.controllers[ctrl_idx].worker(work_id);
        if (worker == null) return $"e{entry_id:X2}";

        return ctrl_idx switch {
            0 => worker->script_header->script_type switch {
                1 => entry_id switch { // Field Object
                    2 => "talk",
                    3 => "scout",
                    5 => "touch",
                    _ => $"e{entry_id:X2}"
                },

                2 => entry_id switch { // Player Edge
                    4 => "cross",
                    5 => "touch",
                    _ => $"e{entry_id:X2}"
                },

                3 => entry_id switch { // Player Zone
                    2 => "talk",
                    3 => "scout",
                    4 => "cross",
                    5 => "touch",
                    6 => "enter",
                    7 => "leave",
                    _ => $"e{entry_id:X2}"
                },

                4 => entry_id switch { // Edge
                    2 => "cross",
                    _ => $"e{entry_id:X2}"
                },

                5 => entry_id switch { // Zone
                    2 => "enter",
                    3 => "leave",
                    _ => $"e{entry_id:X2}"
                },

                _ => $"e{entry_id:X2}"
            },

            3 => battle_gepfeti(work_id, entry_id) switch {
                0 => "on_turn",
                1 => "pre_turn",
                2 => "on_targeted",
                3 => "on_hit",
                4 => "on_death",
                5 => "on_move",
                6 => "post_turn",
                7 => "post_move",
                8 => "post_poison",
                9 => "yojimbo_pay",
                10 => "yojimbo_dismiss",
                11 => "yojimbo_death",
                12 => "magus_turn",
                13 => "magus_freewill",
                14 => "magus_repeat",
                15 => "magus_fight",
                16 => "magus_gogo",
                17 => "magus_help",
                18 => "magus_delta",
                19 => "magus_defense",
                20 => "magus_are_alright",
                _ => $"e{entry_id:X2}"
            },

            _ => $"e{entry_id:X2}"
        };
    }

    private static i32? battle_gepfeti(i32 work_id, i32 entry_idx) {
        AtelBasicWorker* worker = Globals.Atel.controllers[3].worker(work_id);
        if (worker == null) return null;

        for (i32 i = 0; i < 0x15; i++) {
            i32 o = i;
            FuncLib.FUN_00797420((nint)worker->script_chunk - 0x30 + *(nint*)((nint)worker->script_chunk - 0x30 + 0x8), 0x3D, &o);
            if (entry_idx == o) return o;
        }

        return entry_idx;
    }

    private static string get_chr_name(i32 chr_id) {
        Chr* chr = chr_id < 20 ? Globals.Battle.player_characters + chr_id : Globals.Battle.monster_characters + (chr_id - 20);

        return FhCharset.Us.to_string(chr->name);
    }
}
