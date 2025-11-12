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
    Not,
  }

  public readonly OpType OperatorType;

  //FIXME: Return back when supported in Python & Describe
  //public static LogicalOperator CreateOr(IList<IExpression> operands) => new(OpType.Or, operands, 2, -1);
  public static LogicalOperator CreateOr(IList<IExpression> operands) => new(OpType.Or, operands, 2, 2);
  //FIXME: Return back when supported in Python & Describe
  //public static LogicalOperator CreateAnd(IList<IExpression> operands) => new(OpType.And, operands, 2, -1);
  public static LogicalOperator CreateAnd(IList<IExpression> operands) => new(OpType.And, operands, 2, 2);
  public static LogicalOperator CreateNot(IExpression operand) => new(OpType.Not, [operand], 1, 1);

  /// <inheritdoc/>
  public override string ToString() {
    return $"{GetType().Name}({OperatorType})";
  }

  LogicalOperator(OpType opType, IList<IExpression> operands, int minArgs, int maxArgs) : base(operands) {
    OperatorType = opType;
    AssertNumberOfOperandsRange(minArgs, maxArgs);
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
        OpType.Not => () => !boolOperands[0].Execute(), 
        _ => throw new ArgumentOutOfRangeException(nameof(opType), opType, null),
    };
  }
}
