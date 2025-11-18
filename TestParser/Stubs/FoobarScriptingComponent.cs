using System;
using IgorZ.Automation.AutomationSystem;
using IgorZ.Automation.ScriptingEngine.Expressions;
using IgorZ.Automation.ScriptingEngine.ScriptableComponents;
using IgorZ.Automation.ScriptingEngine.ScriptableComponents.Components;

namespace TestParser.Stubs;

sealed class FoobarScriptingComponent : ScriptableComponentBase {
  public override string Name => "Foobar";

  const string EmptyActionName = "Foobar.EmptyAction";

  /// <inheritdoc/>
  public override Func<object> GetPropertySource(string name, AutomationBehavior behavior) {
    return name switch {
        "Foobar.strOverridden" => () => "overridden",
        _ => null,
    };
  }

  /// <inheritdoc/>
  public override ActionDef GetActionDefinition(string name, AutomationBehavior behavior) {
    return name switch {
        EmptyActionName => new ActionDef {
            ScriptName = EmptyActionName,
            DisplayName = "EmptyActionName",
            Arguments = [],
        },
        _ => throw new UnknownActionException(name),
    };
  }

  /// <inheritdoc/>
  public override Action<ScriptValue[]> GetActionExecutor(string name, AutomationBehavior behavior) {
    return name switch {
        EmptyActionName => args => {},
        _ => throw new UnknownActionException(name),
    };
  }
}
