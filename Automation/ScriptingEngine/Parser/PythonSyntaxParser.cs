// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    var result = ParseExpressionInternal(-1, tokens);
    if (tokens.Count > 0) {
      throw new InvalidOperationException("Unexpected token at the end of the expression: " + tokens.Peek());
    }
    return result;
  }

  #endregion

  #region API

  /// <inheritdoc/>
  public override string Decompile(IExpression expression) {
    return DecompileInternal(expression);
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
      { ModOperator, 7 },
      { DivOperator, 7 },
      { MulOperator, 7 },
  };

  static readonly string[] GroupTerminator = [ ")" ];
  static readonly string[] ArgumentsTerminators = [ ")", "," ];

  IExpression ParseExpressionInternal(int parentPrecedence, Queue<Token> tokens) {
    var left = ConsumeValueOperand(tokens);
    while (tokens.Count > 0) {
      var opName = tokens.Peek();
      if (!InfixOperatorsPrecedence.TryGetValue(opName.Value, out var precedence)) {
        throw new ScriptError.ParsingError(opName, "Expected operator");
      }
      if (parentPrecedence >= precedence) {
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
          _ => throw new ScriptError.ParsingError(opName, "Expected operator"),
      };
    }
    return left;
  }

  /// <summary>It consumes a value operand and throws if the operand is not a value.</summary>
  /// <remarks>Functions are value operators. A group that starts with '(' is expected to be it as well.</remarks>
  IExpression ConsumeValueOperand(Queue<Token> tokens) {
    var token = PopToken(tokens);

    // Unary operators.
    if (token is { TokenType: Token.Type.Keyword, Value: NotOperator }) {
      return LogicalOperator.CreateNot(ParseExpressionInternal(InfixOperatorsPrecedence[NotOperator], tokens));
    }
    if (token is { TokenType: Token.Type.Keyword, Value: SubOperator }) {
      var operand = ConsumeValueOperand(tokens);
      return operand is ConstantValueExpr constantValueExpr
          ? ConstantValueExpr.CreateNumericValue(-constantValueExpr.ValueFn().AsNumber)
          : MathOperator.CreateNegate(operand);
    }

    // Expression group: ( ... )
    if (IsGroupOpenToken(token)) {
      if (IsGroupCloseToken(PreviewToken(tokens))) {
        throw new ScriptError.ParsingError(token, "Expected value or operator");
      }
      return ConsumeSequence(tokens, GroupTerminator, out _);
    }

    // Signals ("variables" in Python syntax): Floodgate.Height
    if (token.TokenType == Token.Type.Identifier && (tokens.Count == 0 || !IsGroupOpenToken(tokens.Peek()))) {
      return SignalOperator.Create(CurrentContext, token.Value);
    }

    // Constant values.
    if (token.TokenType == Token.Type.StringLiteral) {
      return ConstantValueExpr.CreateStringLiteral(token.Value);
    }
    if (token.TokenType == Token.Type.NumericValue) {
      return float.TryParse(token.Value, out var value)
          ? ConstantValueExpr.CreateNumericValue(Mathf.RoundToInt(value * 100))
          : throw new ScriptError.ParsingError(token, "Not a valid float number");
    }

    // Functions and actions.
    if (token.TokenType is Token.Type.Identifier or Token.Type.Keyword) {
      var arguments = ConsumeArgumentsGroup(tokens);
      if (token.TokenType == Token.Type.Identifier) {
        arguments.Insert(0, SymbolExpr.Create(token.Value)); 
        return ActionOperator.Create(CurrentContext, arguments);
      }
      return token.Value switch {
          MinFunc => MathOperator.CreateMin(arguments),
          MaxFunc => MathOperator.CreateMax(arguments),
          RoundFunc => MathOperator.CreateRound(arguments),
          ConcatFunc => ConcatOperator.Create(arguments),
          GetNumFunc => GetPropertyOperator.CreateGetNumber(CurrentContext, arguments),
          GetStrFunc => GetPropertyOperator.CreateGetString(CurrentContext, arguments),
          _ => throw new ScriptError.ParsingError(token, "Expected value or operator"),
      };
    }
    throw new Exception($"Unexpected token: {token}");
  }

  IExpression ConsumeSequence(Queue<Token> tokens, string[] terminators, out Token terminator) {
    var sequence = new Queue<Token>();
    var parenCount = 0;
    while (true) {
      var token = PopToken(tokens);
      if (parenCount == 0 && token.TokenType == Token.Type.StopSymbol && terminators.Contains(token.Value)) {
        terminator = token;
        break;
      } 
      if (IsGroupOpenToken(token)) {
        parenCount++;
      } else if (IsGroupCloseToken(token)) {
        if (parenCount == 0) {
          throw new ScriptError.ParsingError("Unexpected EOF while reading sequence");
        }
        parenCount--;
      }
      sequence.Enqueue(token);
    }
    return ParseExpressionInternal(-1, sequence);
  }

  static Token PopToken(Queue<Token> tokens) {
    return tokens.Count == 0
        ? throw new ScriptError.ParsingError("Unexpected EOF while reading expression")
        : tokens.Dequeue();
  }

  static Token PreviewToken(Queue<Token> tokens) {
    return tokens.Count == 0
        ? throw new ScriptError.ParsingError("Unexpected EOF while reading expression")
        : tokens.Peek();
  }

  IList<IExpression> ConsumeArgumentsGroup(Queue<Token> tokens) {
    var openToken = PopToken(tokens);
    if (!IsGroupOpenToken(openToken)) {
      throw new ScriptError.ParsingError(openToken, "Expected opening parenthesis");
    }
    var arguments = new List<IExpression>();
    if (!IsGroupCloseToken(PreviewToken(tokens))) {
      while (true) {
        arguments.Add(ConsumeSequence(tokens, ArgumentsTerminators, out var terminator));
        if (IsGroupCloseToken(terminator)) {
          break;
        }
      }
    }
    return arguments;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  static bool IsGroupOpenToken(Token token) {
    return token is { TokenType: Token.Type.StopSymbol, Value: "(" };
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  static bool IsGroupCloseToken(Token token) {
    return token is { TokenType: Token.Type.StopSymbol, Value: ")" };
  }

  static string DecompileInternal(IExpression expression) {
    return expression switch {
        AbstractOperator abstractOperator => DecompileOperator(abstractOperator),
        ConstantValueExpr constExpr => constExpr.ValueType switch {
            ScriptValue.TypeEnum.String => $"'{constExpr.ValueFn().AsString.Replace(@"\", @"\\").Replace("'", @"\'")}'",
            ScriptValue.TypeEnum.Number => constExpr.ValueFn().AsFloat.ToString("0.##"),
            _ => throw new InvalidOperationException($"Unsupported value type: {constExpr.ValueType}"),
        },
        SymbolExpr symbolExpr => symbolExpr.Value,
        _ => throw new InvalidOperationException($"Unsupported expression type: {expression}"),
    };
  }

  static string DecompileOperator(AbstractOperator expression) {
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
            // FIXME: fix first operand to literal
            GetPropertyOperator.OpType.GetNumber => GetNumFunc,
            GetPropertyOperator.OpType.GetString => GetStrFunc,
            _ => throw new InvalidOperationException($"Unsupported operator: {getPropertyOperator}"),
        },
        ConcatOperator => ConcatFunc,
        ActionOperator actionOperator => actionOperator.ActionName,
        _ => null,
    };
    if (funcName != null) {
      var args = string.Join(", ", expression.Operands.Select(DecompileInternal));
      return $"{funcName}({args})";
    }

    // Unary operators.
    if (expression is LogicalOperator { OperatorType: LogicalOperator.OpType.Not }) {
      var value = DecompileLeft(expression.Operands[0], expression);
      return $"{NotOperator} {value}";
    }
    if (expression is MathOperator { OperatorType: MathOperator.OpType.Negate }) {
      var value = DecompileLeft(expression.Operands[0], expression);
      return $"{SubOperator}{value}";
    }

    // Binary operators: a + b
    //FIXME: add, or, and - can have many arguments.
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
    var leftValue = DecompileLeft(expression.Operands[0], expression);
    var rightValue = DecompileRight(expression.Operands[1], expression);
    return $"{leftValue} {opName} {rightValue}";
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  static string DecompileLeft(IExpression operand, IExpression parent) {
    var value = DecompileInternal(operand);
    return InfixExpressionUtil.ResolvePrecedence(parent) > InfixExpressionUtil.ResolvePrecedence(operand)
        ? $"({value})"
        : value;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  static string DecompileRight(IExpression operand, IExpression parent) {
    var value = DecompileInternal(operand);
    return InfixExpressionUtil.ResolvePrecedence(parent) >= InfixExpressionUtil.ResolvePrecedence(operand)
        ? $"({value})"
        : value;
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
