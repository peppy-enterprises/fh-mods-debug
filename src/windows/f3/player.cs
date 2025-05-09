using Fahrenheit.Core;
using Fahrenheit.Core.FFX;

using ImGuiNET;

namespace Fahrenheit.Modules.Debug.Windows.F3;

public static unsafe class PlayerDebugInfo {
    public static void render() {
        if (ImGui.CollapsingHeader("Player")) {
            ImGui.Indent();

            if (Globals.actors == null || Globals.save_data == null) {
                ImGui.Text("Player not available");
            } else {
                // This is a horrible way of getting the name lol
                string tidus_name = FhCharset.Us.to_string(Globals.save_data->character_names);
                ImGui.Text($"Name: {tidus_name}");

                Vec4f pos = Globals.actors[0].chr_pos_vec;
                ImGui.Text($"Position: {pos.x:F4}, {pos.y:F4}, {pos.z:F4}");

                float rotation = Globals.actors[0].chr_direction;
                ImGui.Text($"Rotation: {rotation}");

                ImGui.Text($"Walked Distance: {Globals.btl->walked_dist:F4}/10.0 (total: {Globals.btl->walked_dist_total:F4})");
                ImGui.Text($"Encounter Grace: {Globals.btl->grace}");
            }

            ImGui.Unindent();
        }
    }
}
