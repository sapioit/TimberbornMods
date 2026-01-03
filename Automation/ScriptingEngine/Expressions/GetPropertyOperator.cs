// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using IgorZ.Automation.ScriptingEngine.Core;

namespace IgorZ.Automation.ScriptingEngine.Expressions;

// FIXME: Deprecate. Use GetPropertyFunction.
sealed class GetPropertyOperator : AbstractOperator, IValueExpr {

  public enum OpType {
    GetString,
    GetNumber,
  }

  public OpType OperatorType { get; private init; }
  /// <inheritdoc/>
  public ScriptValue.TypeEnum ValueType { get; private init; }
  /// <inheritdoc/>
  public Func<ScriptValue> ValueFn { get; private init; }

  /// <summary>Tells if this operator accesses a list property.</summary>
  public bool IsList { get; private init; }

  /// <summary>
  /// Returns the specified operand string value. The operant must be a constant string or the call will fail.
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

  // FIXME: Deprecate.
  public static GetPropertyOperator CreateGetNumber(ExpressionContext context, IList<IExpression> operands) {
    GetPropertyFunction res;
    if (operands.Count == 2) {
      res = GetPropertyFunction.CreateGetCollectionElement(
          context, GetStringLiteral(operands, 0), GetValueExpr(operands, 1));
    } else if (operands.Count != 1) {
      throw new ScriptError.ParsingError($"Expected 1 or 2 operands, got {operands.Count}");
    } else {
      try {
        res = GetPropertyFunction.CreateGetCollectionLength(context, GetStringLiteral(operands, 0));
      } catch (ScriptError.ParsingError) {
        res = GetPropertyFunction.CreateGetOrdinary(context, GetStringLiteral(operands, 0));
      }
    }
    return Wrap(OpType.GetNumber, res);
  }

  // FIXME: Deprecate.
  public static GetPropertyOperator CreateGetString(ExpressionContext context, IList<IExpression> operands) {
    var res = operands.Count switch {
        1 => GetPropertyFunction.CreateGetOrdinary(context, GetStringLiteral(operands, 0)),
        2 => GetPropertyFunction.CreateGetCollectionElement(
            context, GetStringLiteral(operands, 0), GetValueExpr(operands, 1)),
        _ => throw new ScriptError.ParsingError($"Expected 1 or 2 operands, got {operands.Count}"),
    };
    return Wrap(OpType.GetString, res);
  }

  static string GetStringLiteral(IList<IExpression> expressions, int index) {
    if (expressions.Count <= index) {
      throw new ScriptError.ParsingError($"Expected at least #{index + 1} arguments");
    }
    if (expressions[index] is not ConstantValueExpr { ValueType: ScriptValue.TypeEnum.String } strValueExpr) {
      throw new ScriptError.ParsingError($"Argument #{index + 1} must be a string literal");
    }
    return strValueExpr.ValueFn().AsString;
  }

  static IValueExpr GetValueExpr(IList<IExpression> expressions, int index) {
    if (expressions.Count <= index) {
      throw new ScriptError.ParsingError($"Expected at least #{index + 1} arguments");
    }
    if (expressions[index] is not IValueExpr valueExpr) {
      throw new ScriptError.ParsingError($"Argument #{index + 1} must be a value");
    }
    return valueExpr;
  }

  static GetPropertyOperator Wrap(OpType opType, GetPropertyFunction func) {
    IExpression[] operands = func.IndexExpr == null
        ? [ConstantValueExpr.CreateStringLiteral(func.PropertyFullName)]
        : [ConstantValueExpr.CreateStringLiteral(func.PropertyFullName), func.IndexExpr];
    return new GetPropertyOperator(opType, func.ValueType, func.ValueFn, operands) {
        IsList = func.FunctionName is GetPropertyFunction.FuncName.Element or GetPropertyFunction.FuncName.Length,
    };
  }

  GetPropertyOperator(
      OpType opType, ScriptValue.TypeEnum valueType, Func<ScriptValue> valueFn, IList<IExpression> operands)
      : base(operands) {
    OperatorType = opType;
    ValueType = valueType;
    ValueFn = valueFn;
  }
}
