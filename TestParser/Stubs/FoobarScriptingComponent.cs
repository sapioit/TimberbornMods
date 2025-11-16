using System;
using IgorZ.Automation.AutomationSystem;
using IgorZ.Automation.ScriptingEngine.ScriptableComponents.Components;

namespace TestParser.Stubs;

sealed class FoobarScriptingComponent : ScriptableComponentBase {
  public override string Name => "Foobar";

  /// <inheritdoc/>
  public override Func<object> GetPropertySource(string name, AutomationBehavior behavior) {
    return name switch {
        "Foobar.strOverridden" => () => "overridden",
        _ => null,
    };
  }
}
