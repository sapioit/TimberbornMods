// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using IgorZ.Automation.AutomationSystem;
using IgorZ.Automation.ScriptingEngine.Core;
using IgorZ.Automation.ScriptingEngine.Parser;

namespace IgorZ.Automation.ScriptingEngine.Expressions;

class HasComponentOperator : BoolOperator {

  public enum OpType {
    HasSignal,
    HasAction,
  }

  public readonly OpType OperatorType; 

  public static HasComponentOperator CreateHasSignal(ParserBase.Context context, IList<IExpression> operands) =>
      new(OpType.HasSignal, context, operands);

  public static HasComponentOperator CreateHasAction(ParserBase.Context context, IList<IExpression> operands) =>
      new(OpType.HasAction, context, operands); 

  /// <inheritdoc/>
  public override string ToString() {
    return $"{GetType().Name}({OperatorType})";
  }

  readonly AutomationBehavior _component;
  readonly ScriptingService _scriptingService;

  HasComponentOperator(OpType opType, ParserBase.Context context, IList<IExpression> operands) : base(operands) {
    OperatorType = opType;
    AssertNumberOfOperandsRange(1, -1);
    _component = context.ScriptHost;
    _scriptingService = context.ScriptingService;

    var testStrings = new List<string>();
    foreach (var operand in operands) {
      if (operand is not SymbolExpr { Value: var testName }) {
        throw new ScriptError.ParsingError("Expected a symbol: " + operand);
      }
      testStrings.Add(testName);
    }
    Execute = opType switch {
        OpType.HasSignal => () => TrySignals(testStrings),
        OpType.HasAction => () => TryActions(testStrings),
        _ => throw new ArgumentOutOfRangeException(nameof(opType), opType, null),
    };
  }

  bool TrySignals(IList<string> names) {
    try {
      foreach (var name in names) {
        _scriptingService.GetSignalSource(name, _component);
      }
      return true;
    } catch (ScriptError) {
      return false;
    }
  }

  bool TryActions(IList<string> names) {
    try {
      foreach (var name in names) {
        _scriptingService.GetActionExecutor(name, _component);
      }
      return true;
    } catch (ScriptError) {
      return false;
    }
  }
}
