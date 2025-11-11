// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using IgorZ.Automation.ScriptingEngine.Core;
using IgorZ.Automation.ScriptingEngine.Parser;
using IgorZ.Automation.ScriptingEngine.ScriptableComponents;
using IgorZ.Automation.Settings;

namespace IgorZ.Automation.ScriptingEngine.Expressions;

sealed class ActionOperator : AbstractOperator {

  const string ActOnceNameSuffix = ".Once";

  public string FullActionName => ((SymbolExpr)Operands[0]).Value;
  public readonly string ActionName;
  public readonly bool ExecuteOnce; 
  public readonly Action Execute;
  public readonly ActionDef ActionDef;

  public static ActionOperator Create(ParserBase.Context context, IList<IExpression> operands) =>
      new(context, operands);

  /// <inheritdoc/>
  public override string ToString() {
    return $"{GetType().Name}";
  }

  ActionOperator(ParserBase.Context context, IList<IExpression> operands) : base(operands) {
    if (Operands[0] is not SymbolExpr symbol) {
      throw new ScriptError.ParsingError("Bad action name: " + Operands[0]);
    }
    var actionName = symbol.Value;
    if (actionName.EndsWith(ActOnceNameSuffix)) {
      ExecuteOnce = true;
      actionName = actionName[..^ActOnceNameSuffix.Length];
    }
    ActionName = actionName;
    ActionDef = context.ScriptingService.GetActionDefinition(ActionName, context.ScriptHost);
    AssertNumberOfOperandsExact(ActionDef.Arguments.Length + 1);
    var argValues = new Func<ScriptValue>[ActionDef.Arguments.Length];
    for (var i = 0; i < ActionDef.Arguments.Length; i++) {
      var operand = Operands[i + 1];
      if (operand is not IValueExpr valueExpr) {
        throw new ScriptError.ParsingError($"Argument #{i + 1} must be a value, but found: {operand}");
      }
      var argDef = ActionDef.Arguments[i];
      if (argDef.ValueType != valueExpr.ValueType) {
        throw new ScriptError.ParsingError(
            $"Argument #{i + 1} must be of type '{argDef.ValueType}', but found: {valueExpr.ValueType}");
      }
      argDef.ArgumentValidator?.Invoke(valueExpr);
      if (argDef.ValueValidator == null || VerifyConstantValueExpr(argDef, valueExpr) != null) {
        argValues[i] = valueExpr.ValueFn;
      } else {
        argValues[i] = () => {
          var value = valueExpr.ValueFn();
          if (ScriptEngineSettings.CheckArgumentValues) {
            argDef.ValueValidator(value);
          }
          return value;
        };
      }
    }
    var action = context.ScriptingService.GetActionExecutor(ActionName, context.ScriptHost);
    Execute = () => action(argValues.Select(v => v()).ToArray());
  }
}
