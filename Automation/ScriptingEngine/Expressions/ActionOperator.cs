// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using System.Linq;
using IgorZ.Automation.ScriptingEngine.Core;
using IgorZ.Automation.ScriptingEngine.ScriptableComponents;
using IgorZ.Automation.Settings;

namespace IgorZ.Automation.ScriptingEngine.Expressions;

sealed class ActionOperator : AbstractOperator {

  const string ActOnceNameSuffix = ".Once";

  public readonly string FullActionName;
  public readonly string ActionName;
  public readonly bool ExecuteOnce; 
  public readonly Action Execute;
  public readonly ActionDef ActionDef;

  public static ActionOperator Create(ExpressionContext context, string actionName, IList<IExpression> operands) {
    return new ActionOperator(context, actionName, operands);
  }

  /// <inheritdoc/>
  public override string ToString() {
    return $"{GetType().Name}('{FullActionName}')";
  }

  /// <summary>
  /// Returns the specified argument string value. The value must be a constant string or the call will fail.
  /// </summary>
  /// <seealso cref="ConstantValueExpr"/>
  /// <exception cref="InvalidOperationException">
  /// if index is out of bounds or the operant type is not a constant string value.
  /// </exception>
  public string GetStringLiteral(int index) {
    if (index >= Operands.Count) {
      throw new InvalidOperationException($"Operator {this} has {Operands.Count} operands, #{index} was requested");
    }
    return Operands[index] is ConstantValueExpr { ValueType: ScriptValue.TypeEnum.String } constantValueExpr
        ? constantValueExpr.ValueFn().AsString
        : throw new InvalidOperationException(
            $"Expected a string literal at #{index + 1} of {this}, but got: {Operands[index]}");
  }

  ActionOperator(ExpressionContext context, string actionName, IList<IExpression> operands) : base(operands) {
    FullActionName = actionName;
    if (actionName.EndsWith(ActOnceNameSuffix)) {
      ExecuteOnce = true;
      actionName = actionName[..^ActOnceNameSuffix.Length];
    }
    ActionName = actionName;
    ActionDef = context.ScriptingService.GetActionDefinition(ActionName, context.ScriptHost);
    AssertNumberOfOperandsExact(ActionDef.Arguments.Length);
    var argValues = new Func<ScriptValue>[ActionDef.Arguments.Length];
    for (var i = 0; i < ActionDef.Arguments.Length; i++) {
      var operand = Operands[i];
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
