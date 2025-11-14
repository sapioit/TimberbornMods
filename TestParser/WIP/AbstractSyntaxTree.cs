namespace IgorZ.Automation.ScriptingEngine.Parser;

interface IAstNode;

record OperatorNode(string Name, OperatorNode.OpType Operator) : IAstNode {
  /// <summary>Standard comparison, logical and arithmetic operators.</summary>
  public enum OpType {
    // Logical.
    Or,
    And,
    // Comparison.
    Equal,
    NotEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    // Math.
    Plus,
    Minus,
    Multiply,
    Divide,
    Modulus,
    Min,
    Max,
    // Unary
    // FIXME: move to UnaryOperatorNode
    Not,
    Negate,
  }

  public string Name { get; } = Name;
  public OpType Operator { get; } = Operator;
}

record BinaryOperatorNode(
    string Name, BinaryOperatorNode.OpType Operator, IAstNode LeftOperand, IAstNode RightOperand)
    : OperatorNode(Name, Operator) {
  public IAstNode LeftOperand { get; } = LeftOperand;
  public IAstNode RightOperand { get; } = RightOperand;
}

record UnaryOperatorNode(string Name, UnaryOperatorNode.OpType Operator, IAstNode Operand)
    : OperatorNode(Name, Operator) {
  public IAstNode Operand { get; } = Operand;
}

record VariableNode(string Name) : IAstNode {
  public string Name { get; } = Name;
}

record StringValueNode(string Value) : IAstNode {
  public string Value { get; } = Value;
}

record NumberValueNode(string Value) : IAstNode {
  public string Value { get; } = Value;
}

record FunctionNode(string Name, FunctionNode.FuncType Function, IAstNode[] Arguments) : IAstNode {
  public enum FuncType {
    Min,
    Max,
    Round,
    GetProp,
    Custom,
  }

  public string Name { get; } = Name;
  public FuncType Function { get; } = Function;
  public IAstNode[] Arguments { get; } = Arguments;
}
