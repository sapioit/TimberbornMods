// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using IgorZ.Automation.ScriptingEngine.Core;
using IgorZ.Automation.ScriptingEngine.ScriptableComponents;

namespace IgorZ.Automation.ScriptingEngine.Expressions;

sealed class SignalOperator : AbstractOperator, IValueExpr {

  const string OnUnfinishedNamePrefix = ".OnUnfinished.";

  public string FullSignalName => ((SymbolExpr)Operands[0]).Value;
  public readonly string SignalName;
  public bool OnUnfinished => SignalName.Contains(OnUnfinishedNamePrefix);
  public readonly SignalDef SignalDef;

  /// <inheritdoc/>
  public ScriptValue.TypeEnum ValueType => SignalDef.Result.ValueType;

  /// <inheritdoc/>
  public Func<ScriptValue> ValueFn { get; }

  public static SignalOperator Create(ParserBase.Context context, IList<IExpression> operands) =>
  public static SignalOperator Create(ExpressionContext context, IList<IExpression> operands) =>
      new(context, operands);

  /// <inheritdoc/>
  public override string ToString() {
    return $"{GetType().Name}";
  }

  SignalOperator(ExpressionContext context, IList<IExpression> operands) : base(operands) {
    AssertNumberOfOperandsExact(1);
    if (Operands[0] is not SymbolExpr symbol) {
      throw new ScriptError.ParsingError("Bad signal name: " + Operands[0]);
    }
    SignalName = symbol.Value;
    SignalDef = context.ScriptingService.GetSignalDefinition(SignalName, context.ScriptHost);
    ValueFn = context.ScriptingService.GetSignalSource(SignalName, context.ScriptHost);
  }
}
