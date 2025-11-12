// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using System.Linq;
using IgorZ.Automation.ScriptingEngine.Core;
using IgorZ.Automation.ScriptingEngine.Expressions;
using UnityEngine;
using Token = IgorZ.Automation.ScriptingEngine.Parser.TokenizerBase.Token;

namespace IgorZ.Automation.ScriptingEngine.Parser;

class PythonSyntaxParser : ParserBase {

  #region ParserBase implementation

  readonly TokenizerBase _tokenizer = new Tokenizer();

  protected override IExpression ProcessString(string input) {
    var tokens = _tokenizer.Tokenize(input);
    var result = ParseExpressionInternal(0, tokens);
    if (tokens.Count > 0) {
      throw new ScriptError.ParsingError(tokens.Peek(), "Unexpected token at the end of the expression");
    }
    return result;
  }

  #endregion

  #region API

  /// <inheritdoc/>
  public override string Decompile(IExpression expression) {
    return DecompileInternal(expression, 0);
  }

  #endregion

  #region Implementation

  const string HasSignalFunc = "?sig";//FIXME
  const string HasActionFunc = "?act";//FIXME
  const string EqOperator = "==";
  const string NeOperator = "!=";
  const string LtOperator = "<";
  const string LeOperator = "<=";
  const string GtOperator = ">";
  const string GeOperator = ">=";
  const string AndOperator = "and";
  const string OrOperator = "or";
  const string NotOperator = "not";
  const string AddOperator = "+";
  const string SubOperator = "-";
  const string MulOperator = "*";
  const string DivOperator = "/";
  const string ModOperator = "%";
  const string MinFunc = "min";
  const string MaxFunc = "max";
  const string RoundFunc = "round";
  const string GetStrFunc = "getstr";
  const string GetNumFunc = "getnum";
  const string ConcatFunc = "concat";

  static readonly Dictionary<string, int> InfixOperatorsPrecedence = new() {
      { OrOperator, 0 },
      { AndOperator, 1 },
      { NotOperator, 2 },
      { EqOperator, 3 },
      { NeOperator, 3 },
      { LtOperator, 4 },
      { LeOperator, 4 },
      { GtOperator, 4 },
      { GeOperator, 4 },
      { AddOperator, 5 },
      { SubOperator, 5 },
      { ModOperator, 6 },
      { DivOperator, 7 },
      { MulOperator, 8 },
  };

  IExpression ParseExpressionInternal(int parentPrecedence, Queue<Token> tokens) {
    var left = ConsumeValueOperand(tokens);
    while (tokens.Count > 0) {
      var opName = tokens.Peek();
      if (opName.TokenType == Token.Type.StopSymbol) {
        return left;
      }
      if (!InfixOperatorsPrecedence.TryGetValue(opName.Value, out var precedence)) {
        throw new Exception("Unexpected operator keyword: " + opName);
      }
      if (parentPrecedence > precedence) {
        return left;
      }
      tokens.Dequeue(); // Consume operator.
      IExpression[] operands = [left, ParseExpressionInternal(precedence, tokens)];
      left = opName.Value switch {
          OrOperator => LogicalOperator.CreateOr(operands),
          AndOperator => LogicalOperator.CreateAnd(operands),
          EqOperator => BinaryOperator.CreateEq(CurrentContext, operands),
          NeOperator => BinaryOperator.CreateNe(CurrentContext, operands),
          LtOperator => BinaryOperator.CreateLt(CurrentContext, operands),
          LeOperator => BinaryOperator.CreateLe(CurrentContext, operands),
          GtOperator => BinaryOperator.CreateGt(CurrentContext, operands),
          GeOperator => BinaryOperator.CreateGe(CurrentContext, operands),
          AddOperator => MathOperator.CreateAdd(operands),
          SubOperator => MathOperator.CreateSubtract(operands),
          DivOperator => MathOperator.CreateDivide(operands),
          MulOperator => MathOperator.CreateMultiply(operands),
          ModOperator => MathOperator.CreateModulus(operands),
          _ => throw new Exception("Unexpected operator keyword: " + opName),
      };
    }
    return left;
  }

  /// <summary>It consumes a value operand and throws if the operand is not a value.</summary>
  /// <remarks>Functions are value operators. A group that starts with '(' is expected to be it as well.</remarks>
  IExpression ConsumeValueOperand(Queue<Token> tokens) {
    var token = PopToken(tokens);

    // Keywords are not allowed unless they are unary operators.
    if (token.TokenType == Token.Type.Keyword) {
      switch (token.Value) {
        case NotOperator:
          return LogicalOperator.CreateNot(ParseExpressionInternal(InfixOperatorsPrecedence[NotOperator], tokens));
        case SubOperator: {
          var operand = ConsumeValueOperand(tokens);
          return operand is ConstantValueExpr constantValueExpr
              ? ConstantValueExpr.CreateNumericValue(-constantValueExpr.ValueFn().AsNumber) 
              : MathOperator.CreateNegate(operand);
        }
        default:
          throw new ScriptError.ParsingError(token, "Unexpected token");
      }
    }

    // Simple values. Kind of.
    return token.TokenType switch {
        Token.Type.StringLiteral => ConstantValueExpr.CreateStringLiteral(token.Value),
        Token.Type.NumericValue => float.TryParse(token.Value, out var value)
            ? ConstantValueExpr.CreateNumericValue(Mathf.RoundToInt(value * 100))
            : throw new ScriptError.ParsingError(token, "Not a valid float number"),
        Token.Type.Identifier or Token.Type.Keyword => ConsumeOperator(token, tokens),
        Token.Type.StopSymbol => ConsumeGroup(token, tokens),
        _ => throw new Exception($"Unexpected token type: {token.TokenType}"),
    };
  }

  IExpression ConsumeGroup(Token stopSymbol, Queue<Token> tokens) {
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
    return ParseExpressionInternal(0, subExpressionTokens);
  }

  IExpression ConsumeOperator(Token opToken, Queue<Token> tokens) {
    if (opToken.TokenType == Token.Type.Identifier) {
      if (tokens.Count == 0 || tokens.Peek().Value != "(") {
        return SignalOperator.Create(CurrentContext, [SymbolExpr.Create(opToken.Value)]);
      }
    }
    var token = PopToken(tokens);
    if (token is not { TokenType: Token.Type.StopSymbol, Value: "(" }) {
      throw new ScriptError.ParsingError(token, "Expected opening bracket");
    }
    var arguments = new List<IExpression>();
    while (tokens.Count > 0 || tokens.Peek() is not { TokenType: Token.Type.StopSymbol, Value: ")" }) {
      arguments.Add(ParseExpressionInternal(0, tokens));
      var terminator = PopToken(tokens);
      if (terminator is { TokenType: Token.Type.StopSymbol, Value: ")" }) {
        break;
      }
      if (terminator is not { TokenType: Token.Type.StopSymbol, Value: "," }) {
        throw new ScriptError.ParsingError(terminator, "Expected comma");
      }
    }
    if (opToken.TokenType == Token.Type.Identifier) {
      arguments.Insert(0, SymbolExpr.Create(opToken.Value)); 
      return ActionOperator.Create(CurrentContext, arguments);
    }
    return opToken.Value switch {
        MinFunc => MathOperator.CreateMin(arguments),
        MaxFunc => MathOperator.CreateMax(arguments),
        RoundFunc => MathOperator.CreateRound(arguments),
        GetStrFunc => GetPropertyOperator.CreateGetString(CurrentContext, arguments),
        GetNumFunc => GetPropertyOperator.CreateGetNumber(CurrentContext, arguments),
        ConcatFunc => ConcatOperator.Create(arguments),
        _ => throw new InvalidOperationException($"Unexpected keyword token: {opToken}"),
    };
  }

  static Token PopToken(Queue<Token> tokens) {
    return tokens.Count == 0
        ? throw new ScriptError.ParsingError("Unexpected EOF while reading expression")
        : tokens.Dequeue();
  }

  static string DecompileInternal(IExpression expression, int parentOrder) {
    return expression switch {
        AbstractOperator abstractOperator => DecompileOperator(abstractOperator, parentOrder),
        ConstantValueExpr constExpr => constExpr.ValueType switch {
            ScriptValue.TypeEnum.String => $"'{constExpr.ValueFn().AsString}'",
            ScriptValue.TypeEnum.Number => constExpr.ValueFn().AsFloat.ToString("0.##"),
            _ => throw new InvalidOperationException($"Unsupported value type: {constExpr.ValueType}"),
        },
        SymbolExpr symbolExpr => symbolExpr.Value,
        _ => throw new InvalidOperationException($"Unsupported expression type: {expression}"),
    };
  }

  static string DecompileOperator(AbstractOperator expression, int parentOrder) {
    // Signals. They are technically variables: My.variable.name1
    if (expression is SignalOperator signalOperator) {
      return signalOperator.SignalName;
    }

    // Functions: func(a, b, c).
    var funcName = expression switch {
        MathOperator mathOperator => mathOperator.OperatorType switch {
            MathOperator.OpType.Min => MinFunc,
            MathOperator.OpType.Max => MaxFunc,
            MathOperator.OpType.Round => RoundFunc,
            _ => null,
        },
        GetPropertyOperator getPropertyOperator => getPropertyOperator.OperatorType switch {
            GetPropertyOperator.OpType.GetNumber => GetNumFunc,
            GetPropertyOperator.OpType.GetString => GetStrFunc,
            _ => throw new InvalidOperationException($"Unsupported operator: {getPropertyOperator.OperatorType}"),
        },
        ConcatOperator => ConcatFunc,
        ActionOperator actionOperator => actionOperator.ActionName,
        _ => null,
    };
    if (funcName != null) {
      var args = string.Join(", ", expression.Operands.Select(a => DecompileInternal(a, 0)));
      return $"{funcName}({args})";
    }

    // Unary operators.
    if (expression is LogicalOperator { OperatorType: LogicalOperator.OpType.Not }) {
      return $"{NotOperator} {DecompileInternal(expression.Operands[0], InfixOperatorsPrecedence[NotOperator])}";
    }
    if (expression is MathOperator { OperatorType: MathOperator.OpType.Negate }) {
      return $"{SubOperator}{DecompileInternal(expression.Operands[0], 0)}";
    }

    // Binary operators: a + b
    if (expression.Operands.Count != 2) {
      throw new InvalidOperationException(
          $"Unexpected number of arguments {expression.Operands.Count} in {expression}");
    }
    var opName = expression switch {
        BinaryOperator binaryOperator => binaryOperator.OperatorType switch {
            BinaryOperator.OpType.Equal => EqOperator,
            BinaryOperator.OpType.NotEqual => NeOperator,
            BinaryOperator.OpType.LessThan => LtOperator,
            BinaryOperator.OpType.LessThanOrEqual => LeOperator,
            BinaryOperator.OpType.GreaterThan => GtOperator,
            BinaryOperator.OpType.GreaterThanOrEqual => GeOperator,
            _ => throw new InvalidOperationException($"Unsupported operator: {binaryOperator.OperatorType}"),
        },
        MathOperator mathOperator => mathOperator.OperatorType switch {
            MathOperator.OpType.Add => AddOperator,
            MathOperator.OpType.Subtract => SubOperator,
            MathOperator.OpType.Multiply => MulOperator,
            MathOperator.OpType.Divide => DivOperator,
            MathOperator.OpType.Modulus => ModOperator,
            _ => throw new InvalidOperationException($"Unsupported operator: {mathOperator.OperatorType}"),
        },
        LogicalOperator logicalOperator => logicalOperator.OperatorType switch {
            LogicalOperator.OpType.And => AndOperator,
            LogicalOperator.OpType.Or => OrOperator,
            _ => throw new InvalidOperationException($"Unsupported operator: {logicalOperator.OperatorType}"),
        },
        _ => throw new InvalidOperationException($"Unexpected expression type: {expression}"),
    };
    var nodePrecedence = InfixOperatorsPrecedence[opName];
    //FIXME: add, or, and - can have many arguments.
    var leftValue = DecompileInternal(expression.Operands[0], nodePrecedence);
    var rightValue = DecompileInternal(expression.Operands[1], nodePrecedence);
    var res = $"{leftValue} {opName} {rightValue}";
    if (parentOrder > InfixOperatorsPrecedence[opName]) {
      res = "(" +  res + ")";
    }
    return res;
  }

  #endregion

  #region Tokenizer

  class Tokenizer : TokenizerBase {
    /// <inheritdoc/>
    protected override string StringQuotes => "'\"";

    /// <inheritdoc/>
    protected override string StopSymbols => "(),+-/*%=<>";

    /// <inheritdoc/>
    protected override HashSet<string> Keywords => [
        // Logical operators.
        AndOperator, OrOperator, NotOperator,
        // Math functions.
        MinFunc, MaxFunc, RoundFunc,
        // Get property operators.
        GetStrFunc, GetNumFunc,
        // Concat operator.
        ConcatFunc,
    ];

    /// <inheritdoc/>
    protected override string[] StopSymbolsKeywords => [
        EqOperator, NeOperator, LtOperator, LeOperator, GtOperator, GeOperator, // stops: =<>
        AddOperator, SubOperator, DivOperator, MulOperator, ModOperator, // stops: +-/*%
    ];
  }

  #endregion
}
