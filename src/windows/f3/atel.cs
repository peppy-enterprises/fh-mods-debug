using Fahrenheit.CoreLib.FFX;
using ImGuiNET;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Fahrenheit.Modules.Debug.Windows.F3;

public unsafe static class AtelDebugInfo {
    public static void render() {
        if (ImGui.CollapsingHeader("Atel")) {
            ImGui.Indent();

            if (ImGui.CollapsingHeader("Default Controller (#0)")) {
                ImGui.Indent();

                ImGui.TextColored(new Vector4 { X = 0.31f, Y = 0.76f, Z = 0.97f, W = 1f }, "Top Text");
                ImGui.TextColored(new Vector4 { X = 0.73f, Y = 0.50f, Z = 0.78f, W = 1f }, "Middle Text");
                ImGui.TextColored(new Vector4 { X = 0.94f, Y = 0.33f, Z = 0.31f, W = 1f }, "Bottom Text");

                ImGui.Unindent();
            }

            if (ImGui.CollapsingHeader("Battle Controller (#3)")) {
                ImGui.Indent();

                if (Globals.btl->battle_state == 0) {
                    ImGui.Text("Not in a battle");
                } else {
                    string field_name = Marshal.PtrToStringAnsi((nint)Globals.btl->field_name)!;
                    ImGui.Text($"Field: {field_name}");
                    ChrDebugger.enabled ^= ImGui.Button(ChrDebugger.enabled ? "Hide Character Debugger" : "Show Character Debugger");
                }

                ImGui.Unindent();
            }

            ImGui.Unindent();
        }
    }
}