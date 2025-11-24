// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using IgorZ.Automation.ScriptingEngine.Core;
using IgorZ.Automation.ScriptingEngine.ScriptableComponents;
using IgorZ.Automation.Settings;

namespace IgorZ.Automation.ScriptingEngine.Expressions;

abstract class AbstractOperator(IList<IExpression> operands) : IExpression {

  public readonly IList<IExpression> Operands = operands;

  /// <inheritdoc/>
  public void VisitNodes(Action<IExpression> visitorFn) {
    visitorFn(this);
    foreach (var expression in Operands) {
      expression.VisitNodes(visitorFn);
    }
  }

  /// <inheritdoc/>
  public override string ToString() {
    return $"{GetType().Name}";
  }

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

  /// <summary>Unwraps operands to the binary tree if there are more than 2 operands in the expression.</summary>
  /// <remarks>Operands that allow multi-arguments must override this method.</remarks>
  /// <exception cref="InvalidOperationException">if more than 2 operands and no override given.</exception>
  public virtual IList<IExpression> GetReducedOperands() {
    return Operands.Count <= 2
        ? Operands
        : throw new InvalidOperationException($"Operands reducing is not supported");
  }

  protected void AssertNumberOfOperandsExact(int expected) {
    var count = Operands.Count;
    if (expected != count) {
      throw new ScriptError.ParsingError($"Operator '{this}' requires {expected} arguments, but got {count}");
    }
  }

  protected void AssertNumberOfOperandsRange(int min, int max) {
    var count = Operands.Count;
    if (min >= 0 && count < min) {
      throw new ScriptError.ParsingError($"Operator '{this}' requires at least {min} arguments, but got {count}");
    }
    if (max >= 0 && count > max) {
      throw new ScriptError.ParsingError($"Operator '{this}' requires at most {max} arguments, but got {count}");
    }
  }

  protected static ConstantValueExpr VerifyConstantValueExpr(ValueDef valueDef, IValueExpr valueExpr) {
    if (valueExpr is not ConstantValueExpr constantValueExpr) {
      return null;
    }
    if (!ScriptEngineSettings.CheckArgumentValues) {
      return constantValueExpr;
    }
    try {
      valueDef.ValueValidator?.Invoke(valueExpr.ValueFn());
    } catch (ScriptError e) {
      // Report as parsing error since it is checked at parse time.
      throw new ScriptError.ParsingError(e.Message);
    }
    return constantValueExpr;
  }

  /// <summary>Returns a binary tree of expressions if there are more than 2 operands in the operator.</summary>
  /// <remarks>Use it to reduce multi-argument operators to binary operators.</remarks>
  /// <param name="reduceOperandsFn">
  /// Function to join left and right operands. It must return the same type as the operator being reduced.
  /// </param>
  /// <exception cref="InvalidOperationException">if reduce function returns incompatible type.</exception>
  /// <seealso cref="GetReducedOperands"/>
  protected IList<IExpression> ReducedOperands(Func<IList<IExpression>, AbstractOperator> reduceOperandsFn) {
    if (Operands.Count < 3) {
      return Operands;
    }
    var operands = new List<IExpression>(Operands);  // MUST obtain a copy! We will be modifying.
    while (operands.Count > 2) {
      var reducedOperand = reduceOperandsFn([operands[0], operands[1]]);
      if (reducedOperand.GetType() != GetType()) {
        throw new InvalidOperationException(
            $"Reduce operands function must return type {GetType()}, but got {reducedOperand.GetType()}");
      }
      operands.RemoveAt(0);
      operands.RemoveAt(0);
      operands.Insert(0, reducedOperand);
    }
    return operands;
  }
}
