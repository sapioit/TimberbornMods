// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using Timberborn.Common;
using Token = IgorZ.Automation.ScriptingEngine.Parser.TokenizerBase.Token;

namespace IgorZ.Automation.ScriptingEngine.Parser;

/// <summary>Parser for the expressions in the scripting engine.</summary>
sealed class ExpressionParser : ParserBase {

  #region ParserBase implementation

  readonly TokenizerBase _tokenizer = new Tokenizer();

  /// <inheritdoc/>
  protected override IExpression ProcessString(string input) {
    var tokens = _tokenizer.Tokenize(input);
    var result = ReadFromTokens(tokens);
    if (!tokens.IsEmpty()) {
      throw new ScriptError.ParsingError("Unexpected token at the end of the expression: " + tokens.Peek());
    }
    return result;
  }

  #endregion

  #region Implementation

  const string HasSignalFunc = "?sig";
  const string HasActionFunc = "?act";
  const string EqOperator = "eq";
  const string NeOperator = "ne";
  const string LtOperator = "lt";
  const string LeOperator = "le";
  const string GtOperator = "gt";
  const string GeOperator = "ge";
  const string AndOperator = "and";
  const string OrOperator = "or";
  const string AddOperator = "add";
  const string SubOperator = "sub";
  const string MulOperator = "mul";
  const string DivOperator = "div";
  const string ModOperator = "mod";
  const string MinFunc = "min";
  const string MaxFunc = "max";
  const string RoundFunc = "round";
  const string SigFunc = "sig";
  const string ActMethod = "act";
  const string GetStrFunc = "getstr";
  const string GetNumFunc = "getnum";
  const string ConcatFunc = "concat";

  static void CheckHasMoreTokens(Queue<Token> tokens) {
    if (tokens.IsEmpty()) {
      throw new ScriptError.ParsingError("Unexpected EOF while reading expression");
    }
  }

  IExpression ReadFromTokens(Queue<Token> tokens) {
    CheckHasMoreTokens(tokens);
    var token = tokens.Dequeue();
    switch (token.TokenType) {
      case Token.Type.NumericValue:
        return int.TryParse(token.Value, out var value)
            ? ConstantValueExpr.CreateNumericValue(value)
            : throw new ScriptError.ParsingError(token, $"Not a valid integer number: {token.Value}");
      case Token.Type.StringLiteral:
        return ConstantValueExpr.CreateStringLiteral(token.Value);
      case Token.Type.Identifier:
        return SymbolExpr.Create(token.Value);
      case Token.Type.Keyword:
        throw new ScriptError.ParsingError(token, $"Unexpected keyword '{token.Value}'");
      case Token.Type.StopSymbol:
        break;
      default:
        throw new InvalidOperationException($"Unknown token type: {token.TokenType}");
    }
    if (token.Value != "(") {
      throw new ScriptError.ParsingError(token, $"Expected '('");
    }
    CheckHasMoreTokens(tokens);
    var op = tokens.Dequeue();
    if (op.TokenType != Token.Type.Keyword) {
      throw new ScriptError.ParsingError(token, $"Not a valid operator '{op.Value}'");
    }
    CheckHasMoreTokens(tokens);
    var operands = new List<IExpression>();
    while (tokens.Peek().Value != ")") {
      operands.Add(ReadFromTokens(tokens));
      CheckHasMoreTokens(tokens);
    }
    if (operands.Count == 0) {
      throw new ScriptError.ParsingError(token, "Empty operator expression");
    }
    tokens.Dequeue(); // ")"

    // The sequence below should be ordered by the frequency of the usage. The operators that are more likely to be
    // used in the game should come first.
    IExpression expr;
    try {
      expr = op.Value switch {
          HasSignalFunc => HasComponentOperator.CreateHasSignal(CurrentContext, operands),
          HasActionFunc => HasComponentOperator.CreateHasAction(CurrentContext, operands),
          EqOperator => BinaryOperator.CreateEq(CurrentContext, operands),
          NeOperator => BinaryOperator.CreateNe(CurrentContext, operands),
          LtOperator => BinaryOperator.CreateLt(CurrentContext, operands),
          LeOperator => BinaryOperator.CreateLe(CurrentContext, operands),
          GtOperator => BinaryOperator.CreateGt(CurrentContext, operands),
          GeOperator => BinaryOperator.CreateGe(CurrentContext, operands),
          AndOperator => LogicalOperator.CreateAnd(operands),
          OrOperator => LogicalOperator.CreateOr(operands),
          AddOperator => MathOperator.CreateAdd(operands),
          SubOperator => MathOperator.CreateSubtract(operands),
          MulOperator => MathOperator.CreateMultiply(operands),
          DivOperator => MathOperator.CreateDivide(operands),
          ModOperator => MathOperator.CreateModulus(operands),
          MinFunc => MathOperator.CreateMin(operands),
          MaxFunc => MathOperator.CreateMax(operands),
          RoundFunc => MathOperator.CreateRound(operands),
          SigFunc => SignalOperator.Create(CurrentContext, operands),
          ActMethod => ActionOperator.Create(CurrentContext, operands),
          GetStrFunc => GetPropertyOperator.CreateGetString(CurrentContext, operands),
          GetNumFunc => GetPropertyOperator.CreateGetNumber(CurrentContext, operands),
          ConcatFunc => ConcatOperator.Create(operands),
          _ => throw new InvalidOperationException("Operator token not recognized: " + op),
      };
    } catch (ScriptError.ParsingError err) {
      throw new ScriptError.ParsingError(op, err.Message);
    }
    return expr;
  }

  #endregion

  #region Tokenizer

  class Tokenizer : TokenizerBase {
    /// <inheritdoc/>
    protected override string StringQuotes => "'";

    /// <inheritdoc/>
    protected override string StopSymbols => " ()";

    /// <inheritdoc/>
    protected override HashSet<string> Keywords => [
        // Has component operators.
        HasSignalFunc, HasActionFunc,
        // Binary operators
        EqOperator, NeOperator, LtOperator, LeOperator, GtOperator, GeOperator,
        // Logical operators.
        AndOperator, OrOperator,
        // Math operators.
        AddOperator, SubOperator, MulOperator, DivOperator, ModOperator,
        MinFunc, MaxFunc, RoundFunc,
        // Signal/action operators.
        SigFunc, ActMethod,
        // Get property operators.
        GetStrFunc, GetNumFunc,
        // Concat operator.
        ConcatFunc,
    ];

    /// <inheritdoc/>
    protected override string[] StopSymbolsKeywords => [];
  }

  #endregion
}
