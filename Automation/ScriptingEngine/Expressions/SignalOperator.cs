// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using IgorZ.Automation.ScriptingEngine.Parser;

namespace IgorZ.Automation.ScriptingEngine.Expressions;

sealed class SignalOperator : AbstractOperator, IValueExpr {

  const string OnUnfinishedNamePrefix = ".OnUnfinished.";

  public string FullSignalName => ((SymbolExpr)Operands[0]).Value;
  public readonly string SignalName;
  public bool OnUnfinished => SignalName.Contains(OnUnfinishedNamePrefix);

  /// <inheritdoc/>
  public ScriptValue.TypeEnum ValueType { get; }

  /// <inheritdoc/>
  public Func<ScriptValue> ValueFn { get; }

  readonly SignalDef _signalDef;

  /// <inheritdoc/>
  public override string Describe() {
    return _signalDef.DisplayName;
  }

  static readonly Regex SignalNameRegexp = new("^([a-zA-Z][a-zA-Z0-9]+)(.[a-zA-Z][a-zA-Z0-9]+)*$");

  public static SignalOperator Create(ParserBase.Context context, IList<IExpression> operands) =>
      new(context, operands);

  SignalOperator(ParserBase.Context context, IList<IExpression> operands) : base("sig", operands) {
    AsserNumberOfOperandsExact(1);
    if (Operands[0] is not SymbolExpr symbol || !SignalNameRegexp.IsMatch(symbol.Value)) {
      throw new ScriptError.ParsingError("Bad signal name: " + Operands[0]);
    }
    SignalName = symbol.Value;
    _signalDef = context.ScriptingService.GetSignalDefinition(SignalName, context.ScriptHost);
    ValueType = _signalDef.Result.ValueType;
    ValueFn = context.ScriptingService.GetSignalSource(SignalName, context.ScriptHost);
  }
}
