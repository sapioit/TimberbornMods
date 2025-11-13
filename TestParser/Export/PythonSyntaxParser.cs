// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using System.Linq;
using IgorZ.Automation.ScriptingEngine.Core;
using IgorZ.Automation.ScriptingEngine.Parser;
using Token = IgorZ.Automation.ScriptingEngine.Parser.TokenizerBase.Token;

namespace TestParser.Export;

class PythonSyntaxParser {

  public IAstNode ParseExpression(string input) {
    var tokenizer = new Tokenizer();
    var tokens = tokenizer.Tokenize(input);
    var res = ParseExpressionInternal(-1, tokens);
    if (tokens.Count > 0) {
      throw new Exception("Unexpected token at the end of expression: " + tokens.Peek());
    }
    return res;
  }

  #region Imlementation

  static readonly Dictionary<OperatorNode.OpType, int> InfixOperatorsPrecedence = new() {
      { OperatorNode.OpType.Or, 0 },
      { OperatorNode.OpType.And, 1 },
      { OperatorNode.OpType.Not, 2 },
      { OperatorNode.OpType.Equal, 3 },
      { OperatorNode.OpType.NotEqual, 3 },
      { OperatorNode.OpType.LessThan, 3 },
      { OperatorNode.OpType.LessThanOrEqual, 3 },
      { OperatorNode.OpType.GreaterThan, 3 },
      { OperatorNode.OpType.GreaterThanOrEqual, 3 },
      { OperatorNode.OpType.Plus, 4 },
      { OperatorNode.OpType.Minus, 4 },
      { OperatorNode.OpType.Modulus, 5 },
      { OperatorNode.OpType.Divide, 5 },
      { OperatorNode.OpType.Multiply, 5 },
  };

  static readonly Dictionary<string, OperatorNode.OpType> BinaryOperatorsNames = new() {
      { "or", OperatorNode.OpType.Or },
      { "and", OperatorNode.OpType.And },
      { "==", OperatorNode.OpType.Equal },
      { "!=", OperatorNode.OpType.NotEqual },
      { "<", OperatorNode.OpType.LessThan },
      { "<=", OperatorNode.OpType.LessThanOrEqual },
      { ">", OperatorNode.OpType.GreaterThan },
      { ">=", OperatorNode.OpType.GreaterThanOrEqual },
      { "+", OperatorNode.OpType.Plus },
      { "-", OperatorNode.OpType.Minus },
      { "*", OperatorNode.OpType.Multiply },
      { "/", OperatorNode.OpType.Divide },
      { "%", OperatorNode.OpType.Modulus },
  };

  IAstNode ParseExpressionInternal(int parentOrder, Queue<Token> tokens) {
    var operand = ConsumeOperand(tokens);
    while (tokens.Count > 0) {
      var opName = tokens.Peek();
      if (opName.TokenType == Token.Type.StopSymbol) {
        return operand;
      }
      if (!BinaryOperatorsNames.TryGetValue(opName.Value, out var opType)) {
        throw new ScriptError.ParsingError(opName, "Unknown operator");
      }
      var opOrder = InfixOperatorsPrecedence[opType];
      if (parentOrder >= opOrder) {
        return operand;
      }
      tokens.Dequeue(); // Consume operator.
      operand = new BinaryOperatorNode(opType, operand, ParseExpressionInternal(opOrder, tokens));
    }
    return operand;
  }

  /// <summary>It consumes a value operand and throws if the operand is not a value.</summary>
  /// <remarks>Functions are value operators. A group that starts with '(' is expected to be it as well.</remarks>
  IAstNode ConsumeOperand(Queue<Token> tokens) {
    var token = PopToken(tokens);
    // Keywords are not allowed unless they are unary operators.
    if (token.TokenType == Token.Type.Keyword) {
      return token.Value switch {
          "-" => new StringValueNode("-" + ConsumeOperand(tokens)),
          "not" => new UnaryOperatorNode(
              OperatorNode.OpType.Not,
              ParseExpressionInternal(InfixOperatorsPrecedence[OperatorNode.OpType.Not], tokens)),
          _ => throw new ScriptError.ParsingError(token, "Unexpected token"),
      };
    }
    return token.TokenType switch {
        Token.Type.StringLiteral => new StringValueNode(token.Value),
        Token.Type.NumericValue => new NumberValueNode(token.Value),
        Token.Type.Identifier or Token.Type.Keyword => ConsumeOperator(token, tokens),
        Token.Type.StopSymbol => ConsumeGroup(token, tokens),
        _ => throw new Exception($"Unexpected token type: {token.TokenType}")
    };
  }

  IAstNode ConsumeGroup(Token stopSymbol, Queue<Token> tokens) {
    if (stopSymbol.Value != "(") {
      throw new ScriptError.ParsingError(stopSymbol, "Unexpected token");
    }
    var subExpressionTokens = new Queue<Token>();
    var parenCount = 1;
    while (parenCount > 0) {
      var subToken = PopToken(tokens);
      if (subToken is { TokenType: Token.Type.StopSymbol, Value: "(" }) {
        parenCount++;
      } else if (subToken is { TokenType: Token.Type.StopSymbol, Value: ")" }) {
        parenCount--;
        if (parenCount == 0) {
          break;
        }
      }
      subExpressionTokens.Enqueue(subToken);
    }
    var res = ParseExpressionInternal(-1, subExpressionTokens);
    if (subExpressionTokens.Count > 0) {
      throw new InvalidOperationException("Unexpected token inside the group: " + subExpressionTokens.Peek());
    }
    return res;
  }

  IAstNode ConsumeOperator(Token opToken, Queue<Token> tokens) {
    if (opToken.TokenType == Token.Type.Identifier) {
      if (tokens.Count == 0 || tokens.Peek().Value != "(") {
        return new VariableNode(opToken.Value);
      }
    }
    var token = PopToken(tokens);
    if (token is not { TokenType: Token.Type.StopSymbol, Value: "(" }) {
      throw new ScriptError.ParsingError(token, "Expected opening bracket");
    }
    var arguments = new List<IAstNode>();
    while (tokens.Count > 0 || tokens.Peek() is not { TokenType: Token.Type.StopSymbol, Value: ")" }) {
      arguments.Add(ParseExpressionInternal(-1, tokens));
      var terminator = PopToken(tokens);
      if (terminator is { TokenType: Token.Type.StopSymbol, Value: ")" }) {
        break;
      }
      if (terminator is not { TokenType: Token.Type.StopSymbol, Value: "," }) {
        throw new ScriptError.ParsingError(terminator, "Expected comma");
      }
    }
    return new FunctionNode(opToken.Value, arguments.ToArray());
  }

  static Token PopToken(Queue<Token> tokens) {
    return tokens.Count == 0
        ? throw new ScriptError.ParsingError("Unexpected EOF while reading expression")
        : tokens.Dequeue();
  }

  static readonly Dictionary<OperatorNode.OpType, string> PythonOperators = new() {
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
      { OperatorNode.OpType.Negate, "-" },
      { OperatorNode.OpType.Modulus, "%" },
  };

  public string DeconstructPython(IAstNode node) {
    return DeconstructNode(node);
  }

  static string DeconstructNode(IAstNode node) {
    if (node is NumberValueNode numberValueNode) {
      return numberValueNode.Value;
    }
    if (node is StringValueNode stringValueNode) {
      return $"'{stringValueNode.Value}'";
    }
    if (node is FunctionNode functionNode) {
      var args = string.Join(", ", functionNode.Arguments.Select(DeconstructNode));
      return $"{functionNode.Name}({args})";
    }
    if (node is VariableNode variableNode) {
      return variableNode.Name;
    }
    if (node is UnaryOperatorNode unaryOperatorNode) {
      var opName = PythonOperators[unaryOperatorNode.Operator];
      var opDesc = DeconstructLeft(unaryOperatorNode.Operand, unaryOperatorNode);
      return $"{opName} {opDesc}";
    }
    if (node is BinaryOperatorNode binaryOperatorNode) {
      var opName = PythonOperators[binaryOperatorNode.Operator];
      var leftValue = DeconstructLeft(binaryOperatorNode.LeftOperand, binaryOperatorNode);
      var rightValue = DeconstructRight(binaryOperatorNode.RightOperand, binaryOperatorNode);
      return $"{leftValue} {opName} {rightValue}";
    }
    throw new InvalidOperationException("Unexpected node type: " + node);
  }

  static string DeconstructLeft(IAstNode operand, IAstNode parent) {
    var value = DeconstructNode(operand);
    return ResolvePrecedence(parent) > ResolvePrecedence(operand) ? $"({value})" : value;
  }

  static string DeconstructRight(IAstNode operand, IAstNode parent) {
    var value = DeconstructNode(operand);
    return ResolvePrecedence(parent) >= ResolvePrecedence(operand) ? $"({value})" : value;
  }

  static int ResolvePrecedence(IAstNode expression) {
    return expression switch {
        BinaryOperatorNode { Operator: OperatorNode.OpType.Or} => 0,
        BinaryOperatorNode { Operator: OperatorNode.OpType.And} => 1,
        UnaryOperatorNode { Operator: OperatorNode.OpType.Not} => 2,
        BinaryOperatorNode { Operator: OperatorNode.OpType.Equal} => 3,
        BinaryOperatorNode { Operator: OperatorNode.OpType.NotEqual} => 3,
        BinaryOperatorNode { Operator: OperatorNode.OpType.LessThan} => 4,
        BinaryOperatorNode { Operator: OperatorNode.OpType.LessThanOrEqual} => 4,
        BinaryOperatorNode { Operator: OperatorNode.OpType.GreaterThan} => 4,
        BinaryOperatorNode { Operator: OperatorNode.OpType.GreaterThanOrEqual} => 4,
        BinaryOperatorNode { Operator: OperatorNode.OpType.Plus} => 5,
        BinaryOperatorNode { Operator: OperatorNode.OpType.Minus} => 5,
        BinaryOperatorNode { Operator: OperatorNode.OpType.Modulus} => 6,
        BinaryOperatorNode { Operator: OperatorNode.OpType.Divide} => 6,
        BinaryOperatorNode { Operator: OperatorNode.OpType.Multiply} => 6,
        // Values, variables and functions.
        _ => 100,
    };
  }

  #endregion

  #region Tokenizer

  class Tokenizer : TokenizerBase {
    /// <inheritdoc/>
    protected override string StringQuotes => "\"";

    /// <inheritdoc/>
    protected override string StopSymbols => "(),+-/*%=<>";

    /// <inheritdoc/>
    protected override HashSet<string> Keywords => [
        // Logical operators.
        "and", "or", "not",
    ];

    /// <inheritdoc/>
    protected override string[] StopSymbolsKeywords => [ 
        "==", "!=", ">", ">=", "<", "<=",  // stops: =<>
        "+", "-", "/", "*", "%", // stops: +-/*%
    ];
  }

  #endregion
}
