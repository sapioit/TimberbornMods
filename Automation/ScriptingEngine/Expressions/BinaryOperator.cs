// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IgorZ.Automation.ScriptingEngine.Parser;
using IgorZ.Automation.Settings;
using TimberApi.DependencyContainerSystem;
using Timberborn.Localization;
using UnityDev.Utils.LogUtilsLite;

namespace IgorZ.Automation.ScriptingEngine.Expressions;

sealed class BinaryOperator : BoolOperator {

  public enum OpType {
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
  }

  public readonly OpType OperatorType;
  public IValueExpr Left => (IValueExpr)Operands[0];
  public IValueExpr Right => (IValueExpr)Operands[1];

  /// <inheritdoc/>
  public override string Describe() {
    // Special case: check for if the signal has changed (equals to itself).
    if (Left is SignalOperator leftSignal && Right is SignalOperator rightSignal
        && leftSignal.SignalName == rightSignal.SignalName && Name == "eq") {
      return leftSignal.Describe();
    }
    var sb = new StringBuilder();
    sb.Append(Left.Describe());
    sb.Append(Name switch {
        "eq" => " = ",
        "ne" => " \u2260 ",
        "gt" => " > ",
        "lt" => " < ",
        "ge" => " \u2265 ",
        "le" => " \u2264 ",
        _ => throw new InvalidOperationException("Unknown operator: " + Name),
    });
    if (EntityPanelSettings.EvalValuesInConditions || Right is ConstantValueExpr) {
      string rightValue;
      try {
        rightValue = Right.ValueFn().FormatValue(_signalDef?.Result);
      } catch (ScriptError.BadValue e) {
        rightValue = DependencyContainer.GetInstance<ILoc>().T(e.LocKey);
      }
      sb.Append(rightValue);
    } else {
      sb.Append(Right.Describe());
    }

    return sb.ToString();
  }

  public static BinaryOperator CreateEq(ParserBase.Context context, IList<IExpression> operands) =>
      new(OpType.Equal, context, operands);
  public static BinaryOperator CreateNe(ParserBase.Context context, IList<IExpression> operands) =>
      new(OpType.NotEqual, context, operands);
  public static BinaryOperator CreateLt(ParserBase.Context context, IList<IExpression> operands) =>
      new(OpType.LessThan, context, operands);
  public static BinaryOperator CreateLe(ParserBase.Context context, IList<IExpression> operands) =>
      new(OpType.LessThanOrEqual, context, operands);
  public static BinaryOperator CreateGt(ParserBase.Context context, IList<IExpression> operands) =>
      new(OpType.GreaterThan, context, operands);
  public static BinaryOperator CreateGe(ParserBase.Context context, IList<IExpression> operands) =>
      new(OpType.GreaterThanOrEqual, context, operands);

  readonly SignalDef _signalDef;

  static readonly List<string> Names = [ "eq", "ne", "gt", "lt", "ge", "le"];

  BinaryOperator(OpType opType, ParserBase.Context context, IList<IExpression> operands)
      : base(Names[(int)opType], operands) {
    OperatorType = opType;
    AsserNumberOfOperandsExact(2);
    if (Operands[0] is not IValueExpr left) {
      throw new ScriptError.ParsingError("Left operand must be a value, found: " + Operands[0]);
    }
    if (Operands[1] is not IValueExpr right) {
      throw new ScriptError.ParsingError("Right operand must be a value, found: " + Operands[1]);
    }
    if (left.ValueType != right.ValueType) {
      throw new ScriptError.ParsingError($"Arguments type mismatch: {left.ValueType} != {right.ValueType}");
    }
    if (left is SignalOperator leftSignal) {
      _signalDef = context.ScriptingService.GetSignalDefinition(leftSignal.SignalName, context.ScriptHost);
    } else if (right is SignalOperator rightSignal) {
      _signalDef = context.ScriptingService.GetSignalDefinition(rightSignal.SignalName, context.ScriptHost);
    }
    if (_signalDef != null) {
      var otherArgExpr = left is SignalOperator ? right : left;
      _signalDef.Result.ArgumentValidator?.Invoke(otherArgExpr);
      var constantValueExpr = VerifyConstantValueExpr(_signalDef.Result, otherArgExpr);

      // Options compatibility support.
      if (constantValueExpr != null && _signalDef.Result.CompatibilityOptions != null) {
        var value = constantValueExpr.ValueFn().AsString;
        if (_signalDef.Result.CompatibilityOptions.TryGetValue(value, out var replaceOption)) {
          constantValueExpr = ConstantValueExpr.TryCreateFrom($"'{replaceOption}'");
          DebugEx.Warning("BinaryOperator: Replacing constant value '{0}' with '{1}' for signal {2}",
                          value, replaceOption, _signalDef.ScriptName);
          if (Operands[0] == otherArgExpr) {
            Operands[0] = constantValueExpr;
            left = constantValueExpr;
          } else {
            Operands[1] = constantValueExpr;
            right = constantValueExpr;
          }
        }
      }

      if (constantValueExpr != null && _signalDef.Result.Options != null
          && ScriptEngineSettings.CheckOptionsArguments) {
        var value = constantValueExpr.ValueFn().AsString;
        var allowedValues = _signalDef.Result.Options.Select(x => x.Value).ToArray();
        if (!allowedValues.Contains(value)) {
          throw new ScriptError.ParsingError($"Unexpected value: {value}. Allowed: {string.Join(", ", allowedValues)}");
        }
      }
    }
    //FIXME: use ComapreTo for eq/ne
    Execute = left.ValueType switch {
        ScriptValue.TypeEnum.String => opType switch {
            OpType.Equal => () => left.ValueFn().AsString == right.ValueFn().AsString,
            OpType.NotEqual => () => left.ValueFn().AsString != right.ValueFn().AsString,
            _ => throw new ScriptError.ParsingError("Unsupported operator for string operands: " + opType),
        },
        ScriptValue.TypeEnum.Number => opType switch {
            OpType.Equal => () => left.ValueFn().AsNumber == right.ValueFn().AsNumber,
            OpType.NotEqual => () => left.ValueFn().AsNumber != right.ValueFn().AsNumber,
            OpType.LessThan => () => left.ValueFn().AsNumber < right.ValueFn().AsNumber,
            OpType.LessThanOrEqual => () => left.ValueFn().AsNumber <= right.ValueFn().AsNumber,
            OpType.GreaterThan => () => left.ValueFn().AsNumber > right.ValueFn().AsNumber,
            OpType.GreaterThanOrEqual => () => left.ValueFn().AsNumber >= right.ValueFn().AsNumber,
            _ => throw new ArgumentOutOfRangeException(nameof(opType), opType, null),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(ValueType), left.ValueType, null),
    };
  }
}
