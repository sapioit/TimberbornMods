// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IgorZ.Automation.ScriptingEngine.Core;
using IgorZ.Automation.ScriptingEngine.Expressions;
using IgorZ.Automation.ScriptingEngine.Parser;
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
    return DescribeExpressionInternal(expression);
  }

  #region Implementation

  readonly ILoc _loc;

  ExpressionDescriber(ILoc loc) {
    _loc = loc;
  }

  string DescribeExpressionInternal(IExpression expression) {
    return expression switch {
        AbstractFunction abstractFunction => DescribeFunction(abstractFunction),
        ActionOperator actionOperator => DescribeActionOperator(actionOperator),
        BinaryOperator binaryOperator => DescribeComparisonOperator(binaryOperator),
        ConcatOperator concatOperator => concatOperator.ValueFn().AsString,
        ConstantValueExpr constantValueExpr => DescribeScriptValue(constantValueExpr.ValueFn()),
        GetPropertyOperator getProperty => DescribeGetPropertyOperator(getProperty),
        LogicalOperator logicalOperator => DescribeLogicalOperator(logicalOperator),
        MathOperator mathOperator => DescribeMathOperator(mathOperator),
        SignalOperator signalOperator => signalOperator.SignalDef.DisplayName,
        _ => throw new ScriptError.ParsingError($"Unexpected expression: {expression}"),
    };
  }

  string DescribeScriptValue(ScriptValue scriptValue) {
    return scriptValue.ValueType switch {
        ScriptValue.TypeEnum.String => $"'{scriptValue.AsString}'",
        ScriptValue.TypeEnum.Number => scriptValue.AsFloat.ToString("0.0#"),
        _ => $"ERROR:{scriptValue.ValueType}",
    };
  }

  string DescribeComparisonOperator(BinaryOperator op) {
    // Special case: check for if "signal changed" binding (signal equals to itself).
    if (op.Left is SignalOperator leftSignal && op.Right is SignalOperator rightSignal
        && leftSignal.SignalName == rightSignal.SignalName && op.OperatorType == BinaryOperator.OpType.Equal) {
      return DescribeExpressionInternal(leftSignal);
    }
    var sb = new StringBuilder();
    sb.Append(DescribeLeft(op.Left, op));
    sb.Append(op.OperatorType switch {
        BinaryOperator.OpType.Equal => " = ",
        BinaryOperator.OpType.NotEqual => " \u2260 ",
        BinaryOperator.OpType.GreaterThan => " > ",
        BinaryOperator.OpType.LessThan => " < ",
        BinaryOperator.OpType.GreaterThanOrEqual => " \u2265 ",
        BinaryOperator.OpType.LessThanOrEqual => " \u2264 ",
        _ => throw new InvalidOperationException("Unknown operator: " + this),
    });
    if (EntityPanelSettings.EvalValuesInConditions || IsConstantValueOperand(op.Right)) {
      string rightValue;
      try {
        rightValue = op.Right.ValueFn().FormatValue(op.ResultValueDef);
      } catch (ScriptError.BadValue e) {
        rightValue = _loc.T(e.LocKey);
      }
      sb.Append(rightValue);
    } else {
      sb.Append(DescribeRight(op.Right, op));
    }

    return sb.ToString();
  }

  string DescribeFunction(AbstractFunction function) {
    if (function is GetPropertyFunction getPropertyFunction) {
      var propertyName = getPropertyFunction.PropertyFullName;
      return getPropertyFunction.FunctionName switch {
          GetPropertyFunction.FuncName.Value => $"ValueOf({propertyName})",
          GetPropertyFunction.FuncName.Element =>
              $"GetElement({propertyName}, {DescribeExpressionInternal(getPropertyFunction.IndexExpr)})",
          GetPropertyFunction.FuncName.Length => $"Count({propertyName})",
          _ => throw new InvalidOperationException(
              $"Unexpected GetPropertyFunction type: {getPropertyFunction.FunctionName}")
      };
    }
    throw new InvalidOperationException($"Unexpected function: {function}");
  }

  string DescribeGetPropertyOperator(GetPropertyOperator op) {
    var propertyName = op.GetStringLiteral(0);
    if (op.IsList) {
      return op.Operands.Count == 1
          ? $"Count({propertyName})"
          : $"GetElement({propertyName}, {DescribeExpressionInternal(op.Operands[1])})";
    }
    return $"ValueOf({propertyName})";
  }

  string DescribeLogicalOperator(LogicalOperator op) {
    if (op.OperatorType == LogicalOperator.OpType.Not) {
      var value = DescribeLeft(op.Operands[0], op);
      return $"{_loc.T(NotOperatorLocKey)} {value}";
    }
    var displayName = op.OperatorType switch {
        LogicalOperator.OpType.And => _loc.T(AndOperatorLocKey),
        LogicalOperator.OpType.Or => _loc.T(OrOperatorLocKey),
        _ => throw new InvalidOperationException($"Unsupported operator: {op.OperatorType}"),
    };

    // Resolve the multi-operands operators: (add a b c ...)
    var operands = op.GetReducedOperands();
    var leftValue = DescribeLeft(operands[0], op);
    var rightValue = DescribeRight(operands[1], op);
    return $"{leftValue} {displayName} {rightValue}";
  }

  string DescribeMathOperator(MathOperator op) {
    if (IsConstantValueOperand(op)) {
      return DescribeScriptValue(op.ValueFn());
    }

    // Functions don't need precedence check.
    var funcName = op.OperatorType switch {
        MathOperator.OpType.Min => "Min",
        MathOperator.OpType.Max => "Max",
        MathOperator.OpType.Round => "Round",
        // For constants, the negate operator should have been resolved above.
        MathOperator.OpType.Negate => "-",
        _ => null,
    };
    if (funcName != null) {
      var value = string.Join(", ", op.Operands.Select(DescribeExpressionInternal));
      return $"{funcName}({value})";
    }

    var operands = op.GetReducedOperands();
    var opName = op.OperatorType switch {
        MathOperator.OpType.Add => " + ",
        MathOperator.OpType.Subtract => " - ",
        MathOperator.OpType.Multiply => " × ",
        MathOperator.OpType.Divide => " ÷ ",
        MathOperator.OpType.Modulus => " % ",
        _ => throw new InvalidOperationException($"Unknown operator: {op.OperatorType}"),
    };
    var leftValue = DescribeLeft(operands[0], op);
    var rightValue = DescribeRight(operands[1], op);
    return $"{leftValue} {opName} {rightValue}";
  }

  static bool IsConstantValueOperand(IExpression operand) {
    return operand switch {
        ConstantValueExpr => true,
        MathOperator mathOperator => mathOperator.Operands.All(IsConstantValueOperand),
        _ => false,
    };
  }

  string DescribeActionOperator(ActionOperator op) {
    var args = new string[op.ActionDef.Arguments.Length];
    for (var i = 0; i < op.ActionDef.Arguments.Length; i++) {
      var operand = op.Operands[i] as IValueExpr;
      if (EntityPanelSettings.EvalValuesInActionArguments) {
        ScriptValue value;
        try {
          value = operand!.ValueFn();
        } catch (ScriptError.BadValue e) {
          return _loc.T(e.LocKey);
        }
        args[i] = value.FormatValue(op.ActionDef.Arguments[i]);
      } else {
        args[i] = DescribeExpressionInternal(operand);
      }
    }
    return string.Format(op.ActionDef.DisplayName, args);
  }

  string DescribeLeft(IExpression operand, IExpression parent) {
    var value = DescribeExpressionInternal(operand);
    return InfixExpressionUtil.ResolvePrecedence(parent) > InfixExpressionUtil.ResolvePrecedence(operand)
        ? $"({value})"
        : value;
  }

  string DescribeRight(IExpression operand, IExpression parent) {
    var value = DescribeExpressionInternal(operand);
    return InfixExpressionUtil.ResolvePrecedence(parent) >= InfixExpressionUtil.ResolvePrecedence(operand)
        ? $"({value})"
        : value;
  }

  #endregion
}
