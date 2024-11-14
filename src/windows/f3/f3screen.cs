using ImGuiNET;
using System.Numerics;

namespace Fahrenheit.Modules.Debug.Windows.F3;

public unsafe static class F3Screen {
    public delegate void OnRenderDelegate();
    public static event OnRenderDelegate on_render;

    public static bool enabled;
    private static bool show_left = true;
    private static bool show_right;

    private static readonly Vector2 PANE_BUTTON_SIZE = new Vector2(16f);

    public static void render() {
        enabled ^= ImGui.IsKeyPressed(ImGuiKey.F6);
        if (!enabled) return;

        float pane_width = ImGui.GetIO().DisplaySize.X * 0.2f;

        // Setup windows style
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4 { W = 0.6f });
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);

        render_left_pane(pane_width);
        if (on_render != null) render_right_pane(pane_width);

        // Reset style
        ImGui.PopStyleVar(3);
        ImGui.PopStyleColor();
    }

    private static void render_left_pane(f32 pane_width) {
        ImGuiStylePtr style = ImGui.GetStyle();

        // Render left info window open/close button
        ImGui.SetNextWindowPos(new Vector2 { X = style.WindowPadding.X + (show_left ? pane_width : 0), Y = style.WindowPadding.Y });
        ImGui.SetNextWindowSize(PANE_BUTTON_SIZE);
        ImGui.Begin("Debug.F3Screen.LeftPaneButton",
                ImGuiWindowFlags.NoDecoration
              | ImGuiWindowFlags.NoMove
              | ImGuiWindowFlags.NoBackground);

        string arrow = show_left ? "<" : ">";
        Vector2 arrow_size = ImGui.CalcTextSize(arrow);
        ImGui.SetCursorPos(ImGui.GetCursorPos() + PANE_BUTTON_SIZE/2f - arrow_size/2f);
        show_left ^= ImGui.Button(arrow, PANE_BUTTON_SIZE);

        ImGui.End();

        if (show_left) {
            // Setup left info window
            ImGui.SetNextWindowPos(new Vector2 { X = 0, Y = 0 });
            ImGui.SetNextWindowSize(new Vector2 { X = pane_width });
            ImGui.Begin("Debug.F3Screen.Left", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse);

            render_core();

            ImGui.End();
        }
    }

    private static void render_right_pane(f32 pane_width) {
        ImGuiStylePtr style = ImGui.GetStyle();
        Vector2 screen_size = ImGui.GetIO().DisplaySize;

        // Render right info window open/close button
        ImGui.SetNextWindowPos(new Vector2 { X = screen_size.X - style.WindowPadding.X - (show_right ? pane_width : 0) - PANE_BUTTON_SIZE.X, Y = style.WindowPadding.Y });
        ImGui.SetNextWindowSize(PANE_BUTTON_SIZE);
        ImGui.Begin("Debug.F3Screen.LeftPaneButton",
                ImGuiWindowFlags.NoDecoration
                | ImGuiWindowFlags.NoMove
                | ImGuiWindowFlags.NoBackground);

        string arrow = show_right ? ">" : "<";
        Vector2 arrow_size = ImGui.CalcTextSize(arrow);
        ImGui.SetCursorPos(ImGui.GetCursorPos() + PANE_BUTTON_SIZE/2f - arrow_size/2f);
        show_right ^= ImGui.Button(arrow, PANE_BUTTON_SIZE);

        ImGui.End();

        if (show_right) {
            // Setup right info window
            ImGui.SetNextWindowPos(new Vector2 { X = screen_size.X - pane_width, Y = 0 });
            ImGui.SetNextWindowSize(new Vector2 { X = pane_width } );
            ImGui.Begin("Debug.F3Screen.Right", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse);

            on_render?.Invoke();

            ImGui.End();
        }
    }

    private static void render_core() {
        PlayerDebugInfo.render();
        AtelDebugInfo.render();
    }
}