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
  const string NotOperatorLocKey = "IgorZ.Automation.Scripting.Expressions.NotOperator";

  /// <summary>Returns a human-friendly description of the expression.</summary>
  /// <exception cref="ScriptError.RuntimeError">if values need to be calculated, but it results in error.</exception>
  public string DescribeExpression(IExpression expression) {
    return expression switch {
        ActionOperator actionOperator => DescribeActionOperator(actionOperator),
        BinaryOperator binaryOperator => DescribeComparisonOperator(binaryOperator),
        ConcatOperator concatOperator => DescribeConcatOperator(concatOperator),
        ConstantValueExpr constantValueExpr => DescribeScriptValue(constantValueExpr.ValueFn()),
        GetPropertyOperator getProperty => DescribeGetPropertyOperator(getProperty),
        LogicalOperator logicalOperator => DescribeLogicalOperator(logicalOperator),
        MathOperator mathOperator => DescribeMathOperator(mathOperator),
        SignalOperator signalOperator => DescribeSignalOperator(signalOperator),
        _ => expression.ToString(),
    };
  }

  #region Implementation

  readonly ILoc _loc;

  ExpressionDescriber(ILoc loc) {
    _loc = loc;
  }

  string DescribeScriptValue(ScriptValue scriptValue) {
    return scriptValue.ValueType switch {
        ScriptValue.TypeEnum.String => $"'{scriptValue.AsString}'",
        ScriptValue.TypeEnum.Number => scriptValue.AsFloat.ToString("0.0#"),
        _ => $"ERROR:{scriptValue.ValueType}",
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
      return op.Operands.Count == 1
          ? $"Count({symbol})"
          : $"GetElement({symbol}, {DescribeExpression(op.Operands[0])})";
    }
    return $"ValueOf({symbol})";
  }

  string DescribeLogicalOperator(LogicalOperator op) {
    if (op.OperatorType == LogicalOperator.OpType.Not) {
      return $"{_loc.T(NotOperatorLocKey)} ({DescribeExpression(op.Operands[0])})";
    }
    var displayName = op.OperatorType switch {
        LogicalOperator.OpType.And => _loc.T(AndOperatorLocKey),
        LogicalOperator.OpType.Or => _loc.T(OrOperatorLocKey),
        _ => throw new InvalidOperationException($"Unsupported operator: {op.OperatorType}"),
    };
    var descriptions = new List<string>();
    foreach (var operand in op.Operands) {
      if (op.OperatorType == LogicalOperator.OpType.And
          && operand is LogicalOperator { OperatorType: LogicalOperator.OpType.Or }) {
        descriptions.Add($"({DescribeExpression(operand)})");
      } else {
        descriptions.Add(DescribeExpression(operand));
      }
    }
    return string.Join(" " + displayName + " ", descriptions);
  }

  string DescribeMathOperator(MathOperator op) {
    return op.OperatorType switch {
        MathOperator.OpType.Add => "(" + MaybeCollapseMathOp(op, " + ") + ")",
        MathOperator.OpType.Subtract => "(" + MaybeCollapseMathOp(op, " - ") + ")",
        MathOperator.OpType.Multiply => MaybeCollapseMathOp(op, " × "),
        MathOperator.OpType.Divide => MaybeCollapseMathOp(op, " ÷ "),
        MathOperator.OpType.Modulus => MaybeCollapseMathOp(op, " % "),
        MathOperator.OpType.Min => $"min({MaybeCollapseMathOp(op, ", ")})",
        MathOperator.OpType.Max => $"max({MaybeCollapseMathOp(op, ", ")})",
        MathOperator.OpType.Round => $"round({MaybeCollapseMathOp(op, "")})",
        MathOperator.OpType.Negate => $"-({MaybeCollapseMathOp(op, "")})",
        _ => throw new InvalidOperationException($"Unknown operator: {op.OperatorType}"),
    };
  }

  string MaybeCollapseMathOp(MathOperator mathOperator, string separator) {
    return IsConstantValueOperand(mathOperator)
        ? DescribeScriptValue(mathOperator.ValueFn())
        : mathOperator.Operands.Select(DescribeExpression).Aggregate((a, b) => a + separator + b);
  }

  bool IsConstantValueOperand(IExpression operand) {
    return operand switch {
        ConstantValueExpr => true,
        MathOperator mathOperator => mathOperator.Operands.All(IsConstantValueOperand),
        _ => false,
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

  string DescribeConcatOperator(ConcatOperator op) {
    return op.ValueFn().AsString;  // Always evaluate.
  }

  #endregion
}
