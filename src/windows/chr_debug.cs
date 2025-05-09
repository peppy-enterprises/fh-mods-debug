using Fahrenheit.Core;
using Fahrenheit.Core.FFX;
using Fahrenheit.Core.FFX.Battle;

using ImGuiNET;

namespace Fahrenheit.Modules.Debug.Windows;

public unsafe static class ChrDebugger {
    public static bool enabled;

    public static void render() {
        if (!enabled) return;

        if (ImGui.Begin("Character Debugger", ref enabled)) {
            if (Globals.btl->battle_state == 0) {
                ImGui.Text("Not in a battle");
            } else {
                if (ImGui.CollapsingHeader("Player Characters")) {
                    ImGui.Indent();

                    for (u8 i = 0; i < 3; i++) {
                        u8 chr_id = *(Globals.btl->frontline + i);
                        if (chr_id == 0xFF) continue;

                        Chr* chr = Globals.Battle.player_characters + chr_id;
                        render_chr(i, chr);
                    }

                    for (u8 i = 0; i < 4; i++) {
                        u8 chr_id = *(Globals.btl->backline + i);
                        if (chr_id == 0xFF) continue;

                        Chr* chr = Globals.Battle.player_characters + chr_id;
                        render_chr((u8)(i + 3), chr);
                    }

                    ImGui.Unindent();
                }
            }

            ImGui.End();
        }
    }

    private static void render_chr(u8 idx, Chr* chr) {
        string chr_name = FhCharset.Us.to_string(chr->name);

        if (ImGui.CollapsingHeader($"{chr_name}###{idx}")) {
            ImGui.Indent();

            render_stat_table(chr);

            ImGui.Unindent();
        }
    }

    private static void render_stat_table(Chr* chr) {
        string[,] text = new string[6, 4] {
            { "HP", "Max HP", "MP", "Max MP" },
            { $"{chr->hp}", $"{chr->max_hp}", $"{chr->mp}", $"{chr->max_mp}" },
            { "Strength", "Defense", "Magic", "Magic Defense" },
            { get_stat_text(chr->strength, chr->cheer_stacks), get_stat_text(chr->defense, chr->cheer_stacks),
                    get_stat_text(chr->magic, chr->focus_stacks), get_stat_text(chr->magic_defense, chr->focus_stacks) },
            { "Accuracy", "Evasion", "Luck", "Agility" },
            { get_stat_text(chr->accuracy, chr->aim_stacks), get_stat_text(chr->evasion, chr->reflex_stacks),
                    get_stat_text(chr->luck, chr->luck_stacks), get_stat_text(chr->agility, 0) },
        };

        if (ImGui.BeginTable("Stats", 4)) {
            for (u32 row = 0; row < 6; row++) {
                ImGui.TableNextRow();
                for (u32 col = 0; col < 4; col++) {
                    ImGui.TableNextColumn();

                    f32 x_for_center = ImGui.GetCursorPosX() + ImGui.GetColumnWidth() / 2f - ImGui.CalcTextSize(text[row, col]).X / 2f;
                    ImGui.SetCursorPosX(x_for_center);
                    ImGui.Text(text[row, col]);
                }
            }

            ImGui.EndTable();
        }
    }

    private static string get_stat_text(u8 stat_value, u8 stat_boost) {
        return stat_boost != 0
            ? $"{stat_value}+{stat_boost}"
            : $"{stat_value}";
    }
}
