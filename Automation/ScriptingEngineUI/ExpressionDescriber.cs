// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IgorZ.Automation.ScriptingEngine.Core;
using IgorZ.Automation.ScriptingEngine.Expressions;
using IgorZ.Automation.Settings;
using Timberborn.Localization;

namespace IgorZ.Automation.ScriptingEngineUI;

/// <summary>Makes a human-readable description of the parsed expression.</summary>
sealed class ExpressionDescriber {

  const string AndOperatorLocKey = "IgorZ.Automation.Scripting.Expressions.AndOperator";
  const string OrOperatorLocKey = "IgorZ.Automation.Scripting.Expressions.OrOperator";

  /// <summary>Returns a human-friendly description of the expression.</summary>
  /// <exception cref="ScriptError.RuntimeError">if values need to be calculated, but it results in error.</exception>
  public string DescribeExpression(IExpression expression) {
    return expression switch {
        ConstantValueExpr constantValueExpr => DescribeConstantValueExpr(constantValueExpr),
        BinaryOperator binaryOperator => DescribeComparisonOperator(binaryOperator),
        GetPropertyOperator getProperty => DescribeGetPropertyOperator(getProperty),
        LogicalOperator logicalOperator => DescribeLogicalOperator(logicalOperator),
        MathOperator mathOperator => DescribeMathOperator(mathOperator),
        SignalOperator signalOperator => DescribeSignalOperator(signalOperator),
        ActionOperator actionOperator => DescribeActionOperator(actionOperator),
        _ => expression.ToString(),
    };
  }

  #region Implementation

  readonly ILoc _loc;

  ExpressionDescriber(ILoc loc) {
    _loc = loc;
  }

  string DescribeConstantValueExpr(ConstantValueExpr op) {
    return op.ValueType switch {
        ScriptValue.TypeEnum.String => $"'{op.ValueFn().AsString}'",
        ScriptValue.TypeEnum.Number => op.ValueFn().AsFloat.ToString("0.0#"),
        _ => $"ERROR:{op.ValueType}",
    };
  }

  string DescribeComparisonOperator(BinaryOperator op) {
    // Special case: check for if the signal has changed (equals to itself).
    if (op.Left is SignalOperator leftSignal && op.Right is SignalOperator rightSignal
        && leftSignal.SignalName == rightSignal.SignalName && op.OperatorType == BinaryOperator.OpType.Equal) {
      return DescribeExpression(leftSignal);
    }
    var sb = new StringBuilder();
    sb.Append(DescribeExpression(op.Left));
    sb.Append(op.OperatorType switch {
        BinaryOperator.OpType.Equal => " = ",
        BinaryOperator.OpType.NotEqual => " \u2260 ",
        BinaryOperator.OpType.GreaterThan => " > ",
        BinaryOperator.OpType.LessThan => " < ",
        BinaryOperator.OpType.GreaterThanOrEqual => " \u2265 ",
        BinaryOperator.OpType.LessThanOrEqual => " \u2264 ",
        _ => throw new InvalidOperationException("Unknown operator: " + this),
    });
    if (EntityPanelSettings.EvalValuesInConditions || op.Right is ConstantValueExpr) {
      string rightValue;
      try {
        rightValue = op.Right.ValueFn().FormatValue(op.ResultValueDef);
      } catch (ScriptError.BadValue e) {
        rightValue = _loc.T(e.LocKey);
      }
      sb.Append(rightValue);
    } else {
      sb.Append(DescribeExpression(op.Right));
    }

    return sb.ToString();
  }

  string DescribeGetPropertyOperator(GetPropertyOperator op) {
    var symbol = (op.Operands[0] as SymbolExpr)!.Value;
    if (op.IsList) {
      return op.Operands.Count == 1 ?
          $"Count({symbol})"
          : $"GetElement({symbol}, {DescribeExpression(op.Operands[0])})";
    }
    return $"ValueOf({symbol})";
  }

  string DescribeLogicalOperator(LogicalOperator op) {
    var displayName = op.OperatorType switch {
        LogicalOperator.OpType.And => _loc.T(AndOperatorLocKey),
        LogicalOperator.OpType.Or => _loc.T(OrOperatorLocKey),
        _ => throw new InvalidOperationException($"Unknown operator: {op.OperatorType}"),
    };
    var descriptions = new List<string>();
    foreach (var operand in op.Operands) {
      if (op.OperatorType == LogicalOperator.OpType.And
          && operand is LogicalOperator { OperatorType: LogicalOperator.OpType.Or } logicalOperatorExpr) {
        descriptions.Add($"({DescribeExpression(operand)})");
      } else {
        descriptions.Add(DescribeExpression(operand));
      }
    }
    return string.Join(" " + displayName + " ", descriptions);
  }

  string DescribeMathOperator(MathOperator op) {
    return op.OperatorType switch {
        MathOperator.OpType.Add => $"({op.Operands.Select(DescribeExpression).Aggregate((a, b) => a + " + " + b)})",
        MathOperator.OpType.Subtract => $"({DescribeExpression(op.Operands[0])} - {DescribeExpression(op.Operands[1])})",
        MathOperator.OpType.Multiply => $"{DescribeExpression(op.Operands[0])} × {DescribeExpression(op.Operands[1])}",
        MathOperator.OpType.Divide => $"{DescribeExpression(op.Operands[0])} ÷ {DescribeExpression(op.Operands[1])}",
        MathOperator.OpType.Modulus => $"{DescribeExpression(op.Operands[0])} % {DescribeExpression(op.Operands[1])}",
        MathOperator.OpType.Min => $"min({string.Join(", ", op.Operands.Select(DescribeExpression))})",
        MathOperator.OpType.Max => $"max({string.Join(", ", op.Operands.Select(DescribeExpression))})",
        MathOperator.OpType.Round => $"round({DescribeExpression(op.Operands[0])})",
        MathOperator.OpType.Negate => $"-({DescribeExpression(op.Operands[0])})",
        _ => throw new InvalidOperationException($"Unknown operator: {op.OperatorType}"),
    };
  }

  string DescribeSignalOperator(SignalOperator op) {
    return op.SignalDef.DisplayName;
  }

  string DescribeActionOperator(ActionOperator op) {
    var args = new string[op.ActionDef.Arguments.Length];
    for (var i = 0; i < op.ActionDef.Arguments.Length; i++) {
      var operand = op.Operands[i + 1] as IValueExpr;
      if (EntityPanelSettings.EvalValuesInActionArguments) {
        ScriptValue value;
        try {
          value = operand!.ValueFn();
        } catch (ScriptError.BadValue e) {
          return _loc.T(e.LocKey);
        }
        args[i] = value.FormatValue(op.ActionDef.Arguments[i]);
      } else {
        args[i] = DescribeExpression(operand);
      }
    }
    return string.Format(op.ActionDef.DisplayName, args);
  }

  #endregion
}
