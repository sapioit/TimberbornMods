namespace TestParser;

public interface IAstNode;

public record OperatorNode(OperatorNode.OpType Operator) : IAstNode {
  /// <summary>Standard logical and arithmetic operators.</summary>
  public enum OpType {
    Or,
    And,
    Equal,
    NotEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    Plus,
    Minus,
    Multiply,
    Divide,
    Not,
    Negative,
  }

  public OpType Operator { get; } = Operator;
}

public record BinaryOperatorNode(BinaryOperatorNode.OpType Operator, IAstNode LeftOperand, IAstNode RightOperand)
    : OperatorNode(Operator) {

  public IAstNode LeftOperand { get; } = LeftOperand;
  public IAstNode RightOperand { get; } = RightOperand;
}

public record UnaryOperatorNode(UnaryOperatorNode.OpType Operator, IAstNode Operand) : OperatorNode(Operator) {
  public IAstNode Operand { get; } = Operand;
}

public record VariableNode(string Name) : IAstNode {
  public string Name { get; } = Name;
}

public record ValueNode(string Value) : IAstNode {
  public string Value { get; } = Value;
}

public record FunctionNode(string Name, IAstNode[] Arguments) : IAstNode {
  public string Name { get; } = Name;
  public IAstNode[] Arguments { get; } = Arguments;
}
