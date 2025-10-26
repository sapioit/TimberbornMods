using System;
using System.Collections.Generic;

namespace TestParser;

public class Class1 {
  public static void Main(string[] args) {
    var parser = new Class1();
    parser.Run();
  }

  void Run() {
    //var testFormula = "12 * ( 13 + 3 / 2 + 4 )";
    //var testFormula = "12 * 13 + 3 / 2 + 4";
    //var testFormula = "12 + 1 * 2 * 3 + 3 / 2 / 32 + 4 * 7";
    //var testFormula = "12 + 1 * 2 * 3 + 3 / 2 / ( 32 + 4 ) * 7";
    var testFormula = "( 12 * 1 - 2 ) * 3 + 3 / 2 / ( 32 + 4 ) * 7";
    //var testFormula = "2 * 3 + 4";
    //var testFormula = "2 + 3 * 4";
    var tokens = new Queue<string>(testFormula.Split(' '));
    var expression = ParseExpression(-1, tokens);
    Console.WriteLine("Test: " + testFormula);
    Console.WriteLine("Deconstruct2: " + DeconstructPython(expression));
    Console.WriteLine(expression);
  }

  static readonly Dictionary<OperatorNode.OpType, int> InfixOperatorsPrecedence = new() {
      { OperatorNode.OpType.Or, 0 },
      { OperatorNode.OpType.And, 1 },
      { OperatorNode.OpType.Equal, 2 },
      { OperatorNode.OpType.NotEqual, 2 },
      { OperatorNode.OpType.LessThan, 3 },
      { OperatorNode.OpType.LessThanOrEqual, 3 },
      { OperatorNode.OpType.GreaterThan, 3 },
      { OperatorNode.OpType.GreaterThanOrEqual, 3 },
      { OperatorNode.OpType.Plus, 4 },
      { OperatorNode.OpType.Minus, 4 },
      { OperatorNode.OpType.Multiply, 5 },
      { OperatorNode.OpType.Divide, 5 },
      { OperatorNode.OpType.Not, 6 },
      { OperatorNode.OpType.Negative, 6 },
  };

  static readonly Dictionary<string, OperatorNode.OpType> PythonBinaryOperatorsNames = new() {
      { "or", OperatorNode.OpType.Or },
      { "and", OperatorNode.OpType.And },
      { "=", OperatorNode.OpType.Equal },
      { "!=", OperatorNode.OpType.NotEqual },
      { "<", OperatorNode.OpType.LessThan },
      { "<=", OperatorNode.OpType.LessThanOrEqual },
      { ">", OperatorNode.OpType.GreaterThan },
      { ">=", OperatorNode.OpType.GreaterThanOrEqual },
      { "+", OperatorNode.OpType.Plus },
      { "-", OperatorNode.OpType.Minus },
      { "*", OperatorNode.OpType.Multiply },
      { "/", OperatorNode.OpType.Divide },
  };

  static readonly Dictionary<string, OperatorNode.OpType> PythonUnaryOperatorsNames = new() {
      { "not", OperatorNode.OpType.Not },
      { "-", OperatorNode.OpType.Negative },
  };

  static readonly Dictionary<OperatorNode.OpType, string> PythonOperatorsTypes = new() {
      { OperatorNode.OpType.And, "and" },
      { OperatorNode.OpType.Or, "or" },
      { OperatorNode.OpType.Equal, "==" },
      { OperatorNode.OpType.NotEqual, "!=" },
      { OperatorNode.OpType.LessThan, "<" },
      { OperatorNode.OpType.LessThanOrEqual, "<=" },
      { OperatorNode.OpType.GreaterThan, ">" },
      { OperatorNode.OpType.GreaterThanOrEqual, ">=" },
      { OperatorNode.OpType.Plus, "+" },
      { OperatorNode.OpType.Minus, "-" },
      { OperatorNode.OpType.Multiply, "*" },
      { OperatorNode.OpType.Divide, "/" },
      { OperatorNode.OpType.Not, "not" },
      { OperatorNode.OpType.Negative, "-" },
  };

  string DeconstructPython(IAstNode node) {
    return DeconstructNode(node, -1);
  }

  string DeconstructNode(IAstNode node, int parentOrder) {
    if (node is ValueNode valueNode) {
      return valueNode.Value;
    }
    if (node is BinaryOperatorNode binaryOperatorNode) {
      var nodePrecedence = InfixOperatorsPrecedence[binaryOperatorNode.Operator];
      var leftValue = DeconstructNode(binaryOperatorNode.LeftOperand, nodePrecedence);
      var rightValue = DeconstructNode(binaryOperatorNode.RightOperand, nodePrecedence);
      var opName = PythonOperatorsTypes[binaryOperatorNode.Operator];
      if (opName == null) {
        throw new Exception("Unknown operator: " + binaryOperatorNode.Operator);
      }
      var res = $"{leftValue} {opName} {rightValue}";
      if (parentOrder > InfixOperatorsPrecedence[binaryOperatorNode.Operator]) {
        res = "(" +  res + ")";
      }
      return res;
    }
    //FIXME
    throw new InvalidOperationException("Unexpected node type");
  }
  IAstNode ParseExpression(int parentOrder, Queue<string> tokens) {
    var operand = ConsumeOperand(tokens);
    while (tokens.Count > 0) {
      var opName = tokens.Peek();
      if (!PythonBinaryOperatorsNames.TryGetValue(opName, out var opType)) {
        throw new InvalidOperationException("Unknown operator name: " + opName);
      }
      var opOrder = InfixOperatorsPrecedence[opType];
      if (parentOrder > opOrder) {
        return operand;
      }
      tokens.Dequeue(); // Consume operator.
      operand = new BinaryOperatorNode(opType, operand, ParseExpression(opOrder, tokens));
    }
    return operand;
  }

  IAstNode ConsumeOperand(Queue<string> tokens) {
    var token = tokens.Dequeue();
    if (token != "(") {
      //FIXME: cane be function or variable.
      return new ValueNode(token);
    }
    var subExpressionTokens = new Queue<string>();
    var parenCount = 1;
    while (tokens.Count > 0 && parenCount > 0) {
      var subToken = tokens.Dequeue();
      if (subToken == "(") {
        parenCount++;
      } else if (subToken == ")") {
        parenCount--;
        if (parenCount == 0) {
          break;
        }
      }
      subExpressionTokens.Enqueue(subToken);
    }
    return ParseExpression(0, subExpressionTokens);
  }
}
