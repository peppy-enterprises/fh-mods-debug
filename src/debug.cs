using System.Text.Json.Serialization;

using Fahrenheit.Core;
using Fahrenheit.Modules.Debug.Windows;

namespace Fahrenheit.Modules.Debug;

public sealed record DebugModuleConfig : FhModuleConfig {
    [JsonConstructor]
    public DebugModuleConfig(string configName, bool configEnabled)
        : base(configName, configEnabled, []) { }

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
        //AtelDebugger.update();
    }

    public override void render_imgui() {
        //SphereGridEditor.render();
        //F3Screen.render();
        BattleDebugger.render();
        //AtelDebugger.render();
    }

    public override void handle_input() {
        //SphereGridEditor.handle_input();
    }
}
