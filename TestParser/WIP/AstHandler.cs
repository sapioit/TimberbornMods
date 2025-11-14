using System;
using System.Linq;
using IgorZ.Automation.ScriptingEngine.Core;
using IgorZ.Automation.ScriptingEngine.Expressions;

namespace IgorZ.Automation.ScriptingEngine.Parser;

sealed class AstParser {

  ExpressionContext CurrentContext;

  public IExpression Compile(IAstNode node) {
    return node switch {
        StringValueNode stringValue => ConstantValueExpr.CreateStringLiteral(stringValue.Value),
        // FIXME: parse in tokenizer?
        NumberValueNode numberValue => float.TryParse(numberValue.Value, out var value)
            ? ConstantValueExpr.CreateNumericValue((int) Math.Round((double) value * 100))
            : throw new ScriptError.ParsingError($"Not a valid float number: {numberValue.Value}"),
        VariableNode variable => SignalOperator.Create(variable.Name, CurrentContext),//FIXME: or use normal operands notion?
        UnaryOperatorNode unaryOperator => CompileUnaryOperator(unaryOperator),
        BinaryOperatorNode binaryOperator => CompileBinaryOperator(binaryOperator),
        FunctionNode function => CompileFunction(function),
        _ => throw new InvalidOperationException($"Unsupported node: {node}"),
    };
  }

  IExpression CompileUnaryOperator(UnaryOperatorNode unaryOperator) {
    return unaryOperator.Operator switch {
        OperatorNode.OpType.Negate => MathOperator.CreateNegate(Compile(unaryOperator.Operand)),
        OperatorNode.OpType.Not => LogicalOperator.CreateNot(Compile(unaryOperator.Operand)),
        _ => throw new InvalidOperationException($"Unsupported unary operator: {unaryOperator}"),
    };
  }

  IExpression CompileBinaryOperator(BinaryOperatorNode binaryOperator) {
    IExpression[] operands = [Compile(binaryOperator.LeftOperand), Compile(binaryOperator.RightOperand)];
    return binaryOperator.Operator switch {
        // Logical operators.
        OperatorNode.OpType.Or => LogicalOperator.CreateOr(operands), // FIXME: support multi
        OperatorNode.OpType.And => LogicalOperator.CreateAnd(operands), // FIXME: support multi
        // Comparison operators.
        OperatorNode.OpType.Equal => BinaryOperator.CreateEq(CurrentContext, operands),
        OperatorNode.OpType.NotEqual => BinaryOperator.CreateNe(CurrentContext, operands),
        OperatorNode.OpType.GreaterThan => BinaryOperator.CreateGt(CurrentContext, operands),
        OperatorNode.OpType.GreaterThanOrEqual => BinaryOperator.CreateGe(CurrentContext, operands),
        OperatorNode.OpType.LessThan => BinaryOperator.CreateLt(CurrentContext, operands),
        OperatorNode.OpType.LessThanOrEqual => BinaryOperator.CreateLe(CurrentContext, operands),
        // Math operators.
        OperatorNode.OpType.Plus => MathOperator.CreateAdd(operands), // FIXME: support multi
        OperatorNode.OpType.Minus => MathOperator.CreateSubtract(operands),
        OperatorNode.OpType.Multiply => MathOperator.CreateMultiply(operands),
        OperatorNode.OpType.Divide => MathOperator.CreateDivide(operands),
        OperatorNode.OpType.Modulus => MathOperator.CreateModulus(operands),
        _ => throw new InvalidOperationException($"Unsupported binary operator: {binaryOperator}"),
    };
  }

  IExpression CompileFunction(FunctionNode function) {
    var operands = function.Arguments.Select(Compile).ToArray();
    return function.Function switch {
        FunctionNode.FuncType.Min => MathOperator.CreateMin(operands),
        FunctionNode.FuncType.Max => MathOperator.CreateMax(operands),
        FunctionNode.FuncType.Round => MathOperator.CreateRound(operands),
        FunctionNode.FuncType.GetProp => GetPropertyOperator.CreateGetNumber(CurrentContext, operands),
        FunctionNode.FuncType.Custom => ActionOperator.Create(CurrentContext, operands),
        _ => throw new InvalidOperationException($"Unsupported function: {function}"),
    };
  }
}
