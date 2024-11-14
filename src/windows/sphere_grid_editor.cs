using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Fahrenheit.CoreLib;
using Fahrenheit.CoreLib.FFX;
using ImGuiNET;
using static Fahrenheit.Modules.Debug.FuncLib;
using static Fahrenheit.Modules.Debug.Windows.SphereGridEditor;

namespace Fahrenheit.Modules.Debug.Windows;

public static unsafe class SphereGridEditor {
    public enum NodeType {
        Lock_3 = 0x00,
        EmptyNode = 0x01,
        Str_1 = 0x02,
        Str_2 = 0x03,
        Str_3 = 0x04,
        Str_4 = 0x05,
        Def_1 = 0x06,
        Def_2 = 0x07,
        Def_3 = 0x08,
        Def_4 = 0x09,
        Mag_1 = 0x0A,
        Mag_2 = 0x0B,
        Mag_3 = 0x0C,
        Mag_4 = 0x0D,
        MDef_1 = 0x0E,
        MDef_2 = 0x0F,
        MDef_3 = 0x10,
        MDef_4 = 0x11,
        Agi_1 = 0x12,
        Agi_2 = 0x13,
        Agi_3 = 0x14,
        Agi_4 = 0x15,
        Lck_1 = 0x16,
        Lck_2 = 0x17,
        Lck_3 = 0x18,
        Lck_4 = 0x19,
        Eva_1 = 0x1A,
        Eva_2 = 0x1B,
        Eva_3 = 0x1C,
        Eva_4 = 0x1D,
        Acc_1 = 0x1E,
        Acc_2 = 0x1F,
        Acc_3 = 0x20,
        Acc_4 = 0x21,
        HP_200 = 0x22,
        HP_300 = 0x23,
        MP_40 = 0x24,
        MP_20 = 0x25,
        MP_10 = 0x26,
        Lock_1 = 0x27,
        Lock_2 = 0x28,
        Lock_4 = 0x29,
        DelayAttack = 0x2A,
        DelayBuster = 0x2B,
        SleepAttack = 0x2C,
        SilenceAttack = 0x2D,
        DarkAttack = 0x2E,
        ZombieAttack = 0x2F,
        SleepBuster = 0x30,
        SilenceBuster = 0x31,
        DarkBuster = 0x32,
        TripleFoul = 0x33,
        PowerBreak = 0x34,
        MagicBreak = 0x35,
        ArmorBreak = 0x36,
        MentalBreak = 0x37,
        Mug = 0x38,
        QuickHit = 0x39,
        Steal = 0x3A,
        Use = 0x3B,
        Flee = 0x3C,
        Pray = 0x3D,
        Cheer = 0x3E,
        Focus = 0x3F,
        Reflex = 0x40,
        Aim = 0x41,
        Luck = 0x42,
        Jinx = 0x43,
        Lancet = 0x44,
        Guard = 0x45,
        Sentinel = 0x46,
        SpareChange = 0x47,
        Threaten = 0x48,
        Provoke = 0x49,
        Entrust = 0x4A,
        Copycat = 0x4B,
        Doublecast = 0x4C,
        Bribe = 0x4D,
        Cure = 0x4E,
        Cura = 0x4F,
        Curaga = 0x50,
        NulFrost = 0x51,
        NulBlaze = 0x52,
        NulShock = 0x53,
        NulTide = 0x54,
        Scan = 0x55,
        Esuna = 0x56,
        Life = 0x57,
        FullLife = 0x58,
        Haste = 0x59,
        Hastega = 0x5A,
        Slow = 0x5B,
        Slowga = 0x5C,
        Shell = 0x5D,
        Protect = 0x5E,
        Reflect = 0x5F,
        Dispel = 0x60,
        Regen = 0x61,
        Holy = 0x62,
        AutoLife = 0x63,
        Blizzard = 0x64,
        Fire = 0x65,
        Thunder = 0x66,
        Water = 0x67,
        Fira = 0x68,
        Blizzara = 0x69,
        Thundara = 0x6A,
        Watera = 0x6B,
        Firaga = 0x6C,
        Blizzaga = 0x6D,
        Thundaga = 0x6E,
        Waterga = 0x6F,
        Bio = 0x70,
        Demi = 0x71,
        Death = 0x72,
        Drain = 0x73,
        Osmose = 0x74,
        Flare = 0x75,
        Ultima = 0x76,
        PilferGil = 0x77,
        FullBreak = 0x78,
        ExtractPower = 0x79,
        ExtractMana = 0x7A,
        ExtractSpeed = 0x7B,
        ExtractAbility = 0x7C,
        NabGil = 0x7D,
        QuickPockets = 0x7E,
        Null = 0xFF,
    }

    public static System.Collections.Generic.LinkedList<NodeType> NODE_TYPE_ORDER = new(
        new NodeType[] {
            NodeType.EmptyNode,
            NodeType.Lock_1, NodeType.Lock_2, NodeType.Lock_3, NodeType.Lock_4,

            NodeType.HP_200, NodeType.HP_300,
            NodeType.MP_10, NodeType.MP_20, NodeType.MP_40,

            NodeType.Str_1, NodeType.Str_2, NodeType.Str_3, NodeType.Str_4,
            NodeType.Mag_1, NodeType.Mag_2, NodeType.Mag_3, NodeType.Mag_4,
            NodeType.Def_1, NodeType.Def_2, NodeType.Def_3, NodeType.Def_4,
            NodeType.MDef_1, NodeType.MDef_2, NodeType.MDef_3, NodeType.MDef_4,
            NodeType.Acc_1, NodeType.Acc_2, NodeType.Acc_3, NodeType.Acc_4,
            NodeType.Eva_1, NodeType.Eva_2, NodeType.Eva_3, NodeType.Eva_4,
            NodeType.Agi_1, NodeType.Agi_2, NodeType.Agi_3, NodeType.Agi_4,
            NodeType.Lck_1, NodeType.Lck_2, NodeType.Lck_3, NodeType.Lck_4,

            NodeType.Fire, NodeType.Blizzard, NodeType.Water, NodeType.Thunder,
            NodeType.Fira, NodeType.Blizzara, NodeType.Watera, NodeType.Thundara,
            NodeType.Firaga, NodeType.Blizzaga, NodeType.Waterga, NodeType.Thundaga,
            NodeType.Bio, NodeType.Death,
            NodeType.Flare, NodeType.Ultima,
            NodeType.Doublecast,
            NodeType.Demi,
            NodeType.Drain, NodeType.Osmose, NodeType.Lancet,

            NodeType.DarkAttack, NodeType.DarkBuster,
            NodeType.SilenceAttack, NodeType.SilenceBuster,
            NodeType.SleepAttack, NodeType.SleepBuster,
            NodeType.TripleFoul,
            NodeType.ZombieAttack,

            NodeType.DelayAttack, NodeType.DelayBuster,
            NodeType.QuickHit,

            NodeType.Steal, NodeType.Mug,
            NodeType.Use, NodeType.QuickPockets,
            NodeType.PilferGil, NodeType.NabGil,
            NodeType.SpareChange, NodeType.Bribe,
            NodeType.Copycat,

            NodeType.Guard, NodeType.Sentinel,
            NodeType.Provoke, NodeType.Threaten,
            NodeType.Entrust,

            NodeType.Esuna,
            NodeType.Cure, NodeType.Cura, NodeType.Curaga,
            NodeType.Life, NodeType.FullLife, NodeType.AutoLife,
            NodeType.NulBlaze, NodeType.NulFrost, NodeType.NulTide, NodeType.NulShock,
            NodeType.Protect, NodeType.Shell,
            NodeType.Reflect, NodeType.Dispel,
            NodeType.Pray, NodeType.Regen,
            NodeType.Holy,

            NodeType.Scan,
            NodeType.Cheer, NodeType.Focus,
            NodeType.Aim, NodeType.Reflex,
            NodeType.Luck, NodeType.Jinx,

            NodeType.Haste, NodeType.Hastega,
            NodeType.Slow, NodeType.Slowga,
            NodeType.Flee,

            NodeType.PowerBreak, NodeType.MagicBreak,
            NodeType.ArmorBreak, NodeType.MentalBreak,
            NodeType.FullBreak,

            NodeType.ExtractPower, NodeType.ExtractMana,
            NodeType.ExtractSpeed, NodeType.ExtractAbility,
        }
    );

    public const string OUT_PATH_STANDARD = ".\\sphere-grid-standard.bin";
    public const string OUT_PATH_EXPERT = ".\\sphere-grid-expert.bin";
    public const string OUT_PATH_ORIGINAL = ".\\sphere-grid-original.bin";
    public const int MAX_NODE_COUNT = 1024;
    private const int INPUT_FRAME_DELAY = 5;

    private static LpAbilityMapEngine* lpamng => Globals.SphereGrid.lpamng;

    private static int node_count;
    private static int input_delay = 0;
    private static NodeType new_node_type;

    private static class Settings {
        public static bool align_cluster_nodes = true;
        public static bool anchor_cluster_links = true;
        public static bool move_cluster_nodes = true;
        public static bool display_node_details = false;
        public static bool display_invis_nodes = true;
    }

    public static void handle_input() {
        if (input_delay > 0) {
            input_delay--;
            if (enabled) {
                Globals.Input.l1.consume();
                Globals.Input.r1.consume();
                Globals.Input.select.consume();
                Globals.Input.square.consume();
                Globals.Input.confirm.consume();
                Globals.Input.cancel.consume();
            }
            return;
        }

        if (/* abmap is open && */ Globals.Input.start.held && Globals.Input.triangle.just_pressed) {
            enabled = !enabled;
            end_input();
            return;
        }

        if (!enabled) return;

        if (Globals.Input.l1.held) {
            cycle_node_type_prev();
            end_input();
            return;
        }

        if (Globals.Input.r1.held) {
            cycle_node_type_next();
            end_input();
            return;
        }

        if (Globals.Input.triangle.held && Globals.Input.start.just_pressed) {
            save_nodes();
            end_input();
            return;
        }

        Globals.Input.l1.consume();
        Globals.Input.r1.consume();
        Globals.Input.select.consume();
        Globals.Input.square.consume();
        Globals.Input.confirm.consume();
        Globals.Input.cancel.consume();
    }

    private static void end_input() {
        Globals.Input.consume_all();
        input_delay = INPUT_FRAME_DELAY;
    }

    private static void cycle_node_type_next() {
        i32 cur_idx = lpamng->selected_node_idx;

        NodeType cur_node = (NodeType)lpamng->nodes[cur_idx].node_type;
        System.Collections.Generic.LinkedListNode<NodeType> next_node = NODE_TYPE_ORDER.Find(cur_node).Next;
        next_node ??= NODE_TYPE_ORDER.First!;

        new_node_type = next_node.Value;

        FUN_00a48740((i32)new_node_type, cur_idx);
        SndSepPlaySimple(0x8000006d);
    }

    private static void cycle_node_type_prev() {
        i32 cur_idx = lpamng->selected_node_idx;

        NodeType cur_node = (NodeType)lpamng->nodes[cur_idx].node_type;
        System.Collections.Generic.LinkedListNode<NodeType> next_node = NODE_TYPE_ORDER.Find(cur_node).Previous;
        next_node ??= NODE_TYPE_ORDER.Last!;

        new_node_type = next_node.Value;

        FUN_00a48740((i32)new_node_type, cur_idx);
        SndSepPlaySimple(0x8000006d);
    }

    public static void update_node_type() {
        lpamng->nodes[lpamng->selected_node_idx].node_type = (u16)new_node_type;
    }

    private static bool enabled;
    private static void enable() {
        enabled = true;
        if (lpamng == null) return;

        target_zoom = lpamng->current_zoom;
        lpamng->start_zoom = lpamng->current_zoom;
        lpamng->target_zoom = lpamng->current_zoom;
        lpamng->zoom_time_left = 0;
    }

    private static void disable() {
        enabled = false;
        if (lpamng == null) return;

        // handle zoom nicely
        lpamng->zoom_level = SphereGridZoomExt.get_closest(lpamng->current_zoom);
        lpamng->start_zoom = lpamng->current_zoom;
        lpamng->target_zoom = SphereGridZoomExt.get_closest(lpamng->current_zoom).get_zoom();
        target_zoom = lpamng->target_zoom;
        lpamng->zoom_time_left = 30;
    }

    public static void render() {
        // If the abmap manager isn't initialized
        if (*(void**)(FhGlobal.base_addr + 0x8CC838) == null) {
            disable();
            return;
        }

        bool flip_enabled = ImGui.IsKeyPressed(ImGuiKey.F10);
        if (flip_enabled) {
            if (enabled) disable();
            else enable();
        }

        if (!enabled) return;

        // Setup full-screen windows style
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.SetNextWindowPos(new Vector2 { } );
        ImGui.SetNextWindowSize(ImGui.GetIO().DisplaySize);

        if (ImGui.Begin("Debug.SphereGridEditor", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoBringToFrontOnFocus)) {
            process_input();

            render_menu_bar();
            //render_wireframe();
            render_pos_debug();

            ImGui.End();
        }

        // Reset style
        ImGui.PopStyleVar(3);

         TOMkpCrossExtMesFontLClut(
                0, FhCharset.Us.to_bytes(lpamng->selected_node_idx.ToString()),
                50f, 50f, 0x00, 0, 0.69f, 0);
    }

    private const f32 SCROLL_ZOOM_MIN = 0.2f;
    private const f32 SCROLL_ZOOM_MAX = 1.25f;
    private const f32 PAN_SPEED_MIN = 0.2f;
    private const f32 PAN_SPEED_MAX = 1.5f;
    private const f32 SCROLL_ZOOM_SPEED = 0.05f;
    private const f32 SCROLL_ZOOM_LERP_SPEED = 0.5f;
    private static f32 target_zoom = 1;
    private static void process_input() {
        update_zoom();

        float wheel = ImGui.GetIO().MouseWheel;
        if (ImGui.IsWindowHovered()) {
            if (ImGui.IsMouseDown(ImGuiMouseButton.Left) || ImGui.IsKeyDown(ImGuiKey.Space)) {
                Vector2 delta = ImGui.GetIO().MouseDelta;
                ImGui.ResetMouseDragDelta(ImGuiMouseButton.Left);
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll);

                float zoom_ratio = (lpamng->current_zoom - SCROLL_ZOOM_MIN) / (SCROLL_ZOOM_MAX - SCROLL_ZOOM_MIN);
                float pan_speed = (1 - zoom_ratio) * (PAN_SPEED_MAX - PAN_SPEED_MIN) + PAN_SPEED_MIN;
                delta *= pan_speed;
                delta *= -1; // inverse for "dragging" instead of moving in the same direction

                lpamng->cam_desired_pos = lpamng->cam_limited_pos;
                lpamng->cam_desired_pos.x += delta.X;
                lpamng->cam_desired_pos.y += delta.Y;
                return;
            }

            if (wheel != 0) change_zoom(wheel);
        }
    }

    private static void change_zoom(float amount) {
        target_zoom += amount * SCROLL_ZOOM_SPEED;
        if (target_zoom > SCROLL_ZOOM_MAX) target_zoom = SCROLL_ZOOM_MAX;
        if (target_zoom < SCROLL_ZOOM_MIN) target_zoom = SCROLL_ZOOM_MIN;
        lpamng->zoom_level = SphereGridZoomExt.get_closest(target_zoom);
        lpamng->target_zoom = SphereGridZoomExt.get_closest(target_zoom).get_zoom();
    }

    private static bool reset_vanilla_zoom_time = false;
    private static void update_zoom() {
        //TODO: Make this fucken impossible (requires imgui to consume the input instead of passing it along)
        if (lpamng->zoom_time_left > 0) { // If the player zoomed using the in-game function
            // handle zoom nicely
            lpamng->zoom_level = SphereGridZoomExt.get_closest(lpamng->current_zoom);
            lpamng->start_zoom = lpamng->current_zoom;
            lpamng->target_zoom = SphereGridZoomExt.get_closest(lpamng->current_zoom).get_zoom();
            target_zoom = lpamng->target_zoom;
            if (reset_vanilla_zoom_time) lpamng->zoom_time_left = 30;
            reset_vanilla_zoom_time = false;
            return;
        }
        reset_vanilla_zoom_time = lpamng->zoom_time_left == 0;

        if (MathF.Abs(target_zoom - lpamng->current_zoom) < 0.01) {
            lpamng->current_zoom = target_zoom;
            return;
        }

        lpamng->current_zoom = lpamng->current_zoom + (target_zoom - lpamng->current_zoom) * SCROLL_ZOOM_LERP_SPEED;

        // politely do the same thing as vanilla (hides activation indicators below 0.375 zoom)
        if (lpamng->current_zoom < 0.375) *(i32*)((nint)lpamng + 0x116b0) = -2;
        else *(i32*)((nint)lpamng + 0x116b0) = 2;
    }

    // File
    //  > Open Layout Data
    //  > Open Nodes Data
    //  ---
    //  > Save Layout Data
    //  > Save Nodes Data
    //  ---
    //  > Close

    //Edit
    //  > Deselect
    //  ---
    //  > Add...
    //    > Node
    //    > Link
    //    > Cluster
    //  >? Remove Node/Link/Cluster
    //  >? Unlink Node
    //  >? Unanchor link
    //  ---
    //  > Clear Sphere Grid

    //Settings
    //  > Automatically align nodes in clusters
    //  > Automatically anchor links in clusters
    //  > Move nodes in clusters along when moving clusters
    //  > Display Node Details
    //  > Display Invisible Nodes
    private static void render_menu_bar() {
        if (ImGui.BeginMenuBar()) {
            if (ImGui.BeginMenu("File")) {
                if (ImGui.MenuItem("Open Layout Data"));
                if (ImGui.MenuItem("Open Nodes Data"));
                ImGui.Separator();
                if (ImGui.MenuItem("Save Layout Data"));
                if (ImGui.MenuItem("Save Nodes Data")) save_nodes();
                ImGui.Separator();
                if (ImGui.MenuItem("Close")) close();

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Edit")) {
                if (ImGui.MenuItem("Deselect"));
                ImGui.Separator();
                if (ImGui.BeginMenu("Add...")) {
                    if (ImGui.MenuItem("Node"));
                    if (ImGui.MenuItem("Link"));
                    if (ImGui.MenuItem("Cluster"));

                    ImGui.EndMenu();
                }
                //TODO: Automatically change the name to "Remove Node", "Remove Link", "Remove Cluster", or "Remove Selected" depending on what is selected
                if (ImGui.MenuItem("Remove Selected Object"));
                //TODO: Disable this if a node isn't selected
                if (ImGui.MenuItem("Unlink Node", false));
                //TODO: Disable this if a link isn't selected
                if (ImGui.MenuItem("Unanchor Link", false));
                ImGui.Separator();
                ImGui.MenuItem("Clear Sphere Grid");

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Settings")) {
                Settings.align_cluster_nodes  ^= ImGui.MenuItem("Automatically align nodes in clusters", "", Settings.align_cluster_nodes, true);
                Settings.anchor_cluster_links ^= ImGui.MenuItem("Automatically anchor links in clusters", "", Settings.anchor_cluster_links, true);
                Settings.move_cluster_nodes   ^= ImGui.MenuItem("Move nodes in clusters along when moving clusters", "", Settings.move_cluster_nodes, true);
                Settings.display_node_details ^= ImGui.MenuItem("Display node details", "", Settings.display_node_details, true);
                Settings.display_invis_nodes  ^= ImGui.MenuItem("Display invisible nodes", "", Settings.display_invis_nodes, true);

                ImGui.EndMenu();
            }

            ImGui.EndMenuBar();
        }
    }

    private static void render_wireframe() {
        ImDrawListPtr draw_list = ImGui.GetWindowDrawList();

        Vector2 cam_pos = new Vector2 { X = lpamng->cam_limited_pos.x, Y = lpamng->cam_limited_pos.y };
        Vector2 mouse_pos = ImGui.GetMousePos();
        Vector2 half_screen_size = ImGui.GetIO().DisplaySize/2f;
        Vector2 mouse_pos_rel_to_center = mouse_pos - half_screen_size;
        Vector2 ps2_mult = new Vector2 { X = 512, Y = 418 };

        foreach (var node in lpamng->nodes) {
            u16 node_type = node.node_type;

            // Move on if the node is invisible and we're not supposed to display it
            if (node_type == 0xFFFF && !Settings.display_invis_nodes) continue;

            // If it *is* an invisible node,
            // treat it as an empty node for wireframe purposes
            // (we know we're displaying invis nodes if it got this far)
            if (node_type == 0xFFFF) {
                node_type = 0x1;
            }

            var type_info = lpamng->node_type_infos[node_type];
            Vector2 node_size = new Vector2 { X = type_info.width, Y = type_info.height };
            Vector2 node_pos = new Vector2 { X = node.x, Y = node.y };
            Vector2 node_pos_rel_to_center = node_pos - cam_pos;
            Vector2 mouse_pos_rel_to_node = mouse_pos_rel_to_center - node_pos_rel_to_center;

            // Move on if the mouse is not hovering the node
            if (mouse_pos_rel_to_node.LengthSquared() > node_size.LengthSquared()) continue;

            // Mouse is hovering over the node, time to draw its wireframe!
            draw_list.AddCircle(node_pos_rel_to_center - half_screen_size, node_size.Length(), 0x800080FF, 16);
        }
    }

    private static u16 selected_idx = 0;
    private static i32 selected_cam_idx = 0;
    private static void render_pos_debug() {
        if (ImGui.Begin("Pos Debug")) {
            Vector2 cam_desired_pos = new Vector2 { X = lpamng->cam_desired_pos.x, Y = lpamng->cam_desired_pos.y };
            Vector2 cam_limited_pos = new Vector2 { X = lpamng->cam_limited_pos.x, Y = lpamng->cam_limited_pos.y };
            Vector2 mouse_pos = ImGui.GetMousePos();
            Vector2 screen_size = ImGui.GetIO().DisplaySize;
            Vector2 half_screen_size = screen_size/2f;
            Vector2 mouse_pos_rel_to_center = mouse_pos - half_screen_size;
            Vector2 ps2_screen_size = new Vector2 { X = 512, Y = 418 };

            ImGui.Text($"Current Zoom: {lpamng->current_zoom}");
            ImGui.Text($"Desired Zoom: {target_zoom}");
            ImGui.Text($"Cam Desired Pos: {cam_desired_pos}");
            ImGui.Text($"Cam Limited Pos: {cam_limited_pos}");
            ImGui.Text($"Mouse Pos: {mouse_pos}");
            ImGui.Text($"Screen Size / 2: {half_screen_size}");
            ImGui.Text($"Mouse pos relative to center: {mouse_pos_rel_to_center}");
            ImGui.Text($"PS2 Screen Size: {ps2_screen_size}");

            if (ImGui.CollapsingHeader("World Matrix")) {
                Mat4f* world_matrix = (Mat4f*)(*FhUtil.ptr_at<nint>(0x8cb9d8) + 0xd34);
                render_matrix(*world_matrix);
            }

            if (ImGui.CollapsingHeader("View Matrix")) {
                Mat4f* view_matrix = (Mat4f*)(*FhUtil.ptr_at<nint>(0x8cb9d8) + 0xbe0);
                render_matrix(*view_matrix);
            }

            if (ImGui.Button("Select current node")) {
                selected_idx = lpamng->selected_node_idx;
            }

            var node = lpamng->nodes[selected_idx];
            if (ImGui.CollapsingHeader("Selected Node")) {
                Vector2 node_size = new();
                if (node.node_type != 0xFFFF) {
                    var type_info = lpamng->node_type_infos[node.node_type];
                    node_size = new Vector2 { X = type_info.width, Y = type_info.height };
                }
                Vector2 node_pos = new Vector2 { X = node.x, Y = node.y };
                Vector2 node_pos_rel_to_center = node_pos - cam_limited_pos;
                Vector2 mouse_pos_rel_to_node = mouse_pos_rel_to_center - node_pos_rel_to_center;

                ImGui.Text($"Node Size: {node_size}");
                ImGui.Text($"Node Pos: {node_pos}");
                ImGui.Text($"Node Pos remapped: {node_pos_rel_to_center.game_remap()}");
                ImGui.Text($"Node pos relative to center: {node_pos_rel_to_center}");
                ImGui.Text($"Mouse pos relative to node: {mouse_pos_rel_to_node}");
                ImGui.Text($"With mouse pos remapped: {mouse_pos_rel_to_center.screen_remap() - node_pos_rel_to_center}");
                ImGui.Text($"With node pos remapped: {mouse_pos_rel_to_center - node_pos_rel_to_center.game_remap()}");
                ImGui.Text($"With both remapped: {mouse_pos_rel_to_center.screen_remap() - node_pos_rel_to_center.game_remap()}");
            }

            ImGui.End();
        }
    }

    private static void render_matrix(Mat4f matrix) {
        if (ImGui.BeginTable("Matrix", 4)) {
            for (i32 row = 0; row < 4; row++) {
                ImGui.TableNextRow();

                for (i32 col = 0; col < 4; col++) {
                    ImGui.TableNextColumn();

                    ImGui.Text($"{matrix[col][row]}");
                }
            }

            ImGui.EndTable();
        }
    }

    private static Vector2 game_remap(this Vector2 vec) {
        return new Vector2 {
            X = vec.X * 512 / 1280,
            Y = vec.Y * 418 / 720,
        };
    }

    private static Vector2 screen_remap(this Vector2 vec) {
        return new Vector2 {
            X = vec.X * 1920 / 1280,
            Y = vec.Y * 1080 / 720,
        };
    }

    public static void save_nodes() {
        string path = Globals.save_data->config_grid_type == 2 ? OUT_PATH_EXPERT : Globals.save_data->config_grid_type == 1 ? OUT_PATH_STANDARD : OUT_PATH_ORIGINAL;
        byte[] data = new byte[lpamng->node_count];

        for (int i = 0; i < lpamng->node_count; i++) {
            data[i] = (u8)lpamng->nodes[i].node_type;
        }

        using (FileStream fs = new(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)) {
            fs.Write(data);
            fs.SetLength(data.Length);
        }

        SndSepPlaySimple(0x80000070);

    }

    public static void close() {
        enabled = false;
    }
}
