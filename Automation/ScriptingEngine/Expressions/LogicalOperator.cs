// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using System.Linq;
using IgorZ.Automation.ScriptingEngine.Core;

namespace IgorZ.Automation.ScriptingEngine.Expressions;

class LogicalOperator : BoolOperator {

  public enum OpType {
    And,
    Or,
  }

  public readonly OpType OperatorType;

  public static LogicalOperator CreateOr(IList<IExpression> operands) => new(OpType.Or, operands);
  public static LogicalOperator CreateAnd(IList<IExpression> operands) => new(OpType.And, operands);

  /// <inheritdoc/>
  public override string ToString() {
    return $"{GetType().Name}({OperatorType})";
  }

  LogicalOperator(OpType opType, IList<IExpression> operands) : base(operands) {
    OperatorType = opType;
    AssertNumberOfOperandsRange(2, -1);
    var boolOperands = new List<BoolOperator>();
    for (var i = 0; i < operands.Count; i++) {
      var op = Operands[i];
      if (op is not BoolOperator result) {
        throw new ScriptError.ParsingError($"Operand #{i + 1} must be a boolean value, found: {op}");
      }
      boolOperands.Add(result);
    }
    Execute = opType switch {
        OpType.And => () => boolOperands.All(x => x.Execute()),
        OpType.Or => () => boolOperands.Any(x => x.Execute()),
        _ => throw new ArgumentOutOfRangeException(nameof(opType), opType, null),
    };
  }
}
