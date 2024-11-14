using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Fahrenheit.CoreLib;
using Fahrenheit.Modules.Debug.Windows;
using Fahrenheit.Modules.Debug.Windows.F3;

namespace Fahrenheit.Modules.Debug;

public sealed record DebugModuleConfig : FhModuleConfig {
    [JsonConstructor]
    public DebugModuleConfig(string configName, bool configEnabled)
                      : base(configName, configEnabled) {}

    public override FhModule SpawnModule() {
        return new DebugModule(this);
    }
}

public unsafe partial class DebugModule : FhModule {
    private readonly DebugModuleConfig _moduleConfig;

    public DebugModule(DebugModuleConfig moduleConfig) : base(moduleConfig) {
        _moduleConfig = moduleConfig;

        init_hooks();
    }

    public override bool init() {
        return hook();
    }

    public override void post_update() {
        AtelDebugger.update();
    }

    public override void render_imgui() {
        SphereGridEditor.render();
        F3Screen.render();
        ChrDebugger.render();
        AtelDebugger.render();
    }

    public override void handle_input() {
        //SphereGridEditor.handle_input();
    }
}
