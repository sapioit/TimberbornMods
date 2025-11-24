using IgorZ.Automation.ScriptingEngine.Expressions;

namespace IgorZ.Automation.ScriptingEngine.Parser;

static class InfixExpressionUtil {
  public static int ResolvePrecedence(IExpression expression) {
    return expression switch {
        LogicalOperator { OperatorType: LogicalOperator.OpType.Or} => 0,
        LogicalOperator { OperatorType: LogicalOperator.OpType.And} => 1,
        LogicalOperator { OperatorType: LogicalOperator.OpType.Not} => 2,
        BinaryOperator { OperatorType: BinaryOperator.OpType.Equal} => 3,
        BinaryOperator { OperatorType: BinaryOperator.OpType.NotEqual} => 3,
        BinaryOperator { OperatorType: BinaryOperator.OpType.LessThan} => 4,
        BinaryOperator { OperatorType: BinaryOperator.OpType.LessThanOrEqual} => 4,
        BinaryOperator { OperatorType: BinaryOperator.OpType.GreaterThan} => 4,
        BinaryOperator { OperatorType: BinaryOperator.OpType.GreaterThanOrEqual} => 4,
        MathOperator { OperatorType: MathOperator.OpType.Add} => 5,
        MathOperator { OperatorType: MathOperator.OpType.Subtract} => 5,
        MathOperator { OperatorType: MathOperator.OpType.Modulus} => 6,
        MathOperator { OperatorType: MathOperator.OpType.Divide} => 6,
        MathOperator { OperatorType: MathOperator.OpType.Multiply} => 6,
        // Values, variables and functions.
        _ => 100,
    };
  }
}
