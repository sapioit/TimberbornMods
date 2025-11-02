// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using System.IO;

namespace IgorZ.Automation.ScriptingEngine.Parser;

class ConcatOperator : AbstractOperator, IValueExpr {

  /// <inheritdoc/>
  public ScriptValue.TypeEnum ValueType => ScriptValue.TypeEnum.String;

  /// <inheritdoc/>
  public Func<ScriptValue> ValueFn { get; }

  /// <inheritdoc/>
  public override string Describe() {
    return Name switch {
        _ => throw new InvalidDataException("Unknown operator: " + Name),
    };
  }

  public static ConcatOperator Create(IList<IExpression> operands) => new(operands, 2, -1);

  ConcatOperator(IList<IExpression> operands, int minArgs, int maxArgs) : base("concat", operands) {
    AsserNumberOfOperandsRange(minArgs, maxArgs);
    var valueExprs = new List<IValueExpr>();
    for (var i = 0; i < operands.Count; i++) {
      var op = Operands[i];
      if (op is not IValueExpr result) {
        throw new ScriptError.ParsingError($"Operand #{i + 1} must be a value, found: {op}");
      }
      valueExprs.Add(result);
    }
    ValueFn = () => {
      var result = "";
      foreach (var valueExpr in valueExprs) {
        if (valueExpr.ValueType == ScriptValue.TypeEnum.Number) {
          result += valueExpr.ValueFn().AsFloat.ToString("0.##");
        } else {
          result += valueExpr.ValueFn().AsString;
        }
      }
      return ScriptValue.Of(result);
    };
  }
}
