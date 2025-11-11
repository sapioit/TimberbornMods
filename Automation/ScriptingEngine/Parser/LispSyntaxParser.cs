// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using System.Text;
using IgorZ.Automation.ScriptingEngine.Core;
using IgorZ.Automation.ScriptingEngine.Expressions;
using Token = IgorZ.Automation.ScriptingEngine.Parser.TokenizerBase.Token;

namespace IgorZ.Automation.ScriptingEngine.Parser;

/// <summary>Parser for the expressions in the scripting engine.</summary>
sealed class LispSyntaxParser : ParserBase {

  #region ParserBase implementation

  readonly TokenizerBase _tokenizer = new Tokenizer();

  /// <inheritdoc/>
  protected override IExpression ProcessString(string input) {
    var tokens = _tokenizer.Tokenize(input);
    var result = ReadFromTokens(tokens);
    if (tokens.Count > 0) {
      throw new ScriptError.ParsingError(tokens.Peek(), "Unexpected token at the end of the expression");
    }
    return result;
  }

  #endregion

  #region API

  /// <summary>Comparison operators to Lisp-syntax keyword map.</summary>
  public static readonly Dictionary<BinaryOperator.OpType, string> ComparisonOperators = new() {
      { BinaryOperator.OpType.Equal, EqOperator },
      { BinaryOperator.OpType.NotEqual, NeOperator },
      { BinaryOperator.OpType.GreaterThan, GtOperator },
      { BinaryOperator.OpType.GreaterThanOrEqual, GeOperator },
      { BinaryOperator.OpType.LessThan, LtOperator },
      { BinaryOperator.OpType.LessThanOrEqual, LeOperator },
  };

  /// <inheritdoc/>
  public override string Decompile(IExpression expression) {
    var sb = new StringBuilder();
    DecompileInternal(sb, expression);
    return sb.ToString();
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
    if (tokens.Count == 0) {
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
            : throw new ScriptError.ParsingError(token, "Not a valid integer number");
      case Token.Type.StringLiteral:
        return ConstantValueExpr.CreateStringLiteral(token.Value);
      case Token.Type.Identifier:
        return SymbolExpr.Create(token.Value);
      case Token.Type.Keyword:
        throw new ScriptError.ParsingError(token, "Unexpected keyword");
      case Token.Type.StopSymbol:
        break; // Will be handed outside this switch.
      default:
        throw new InvalidOperationException($"Unknown token type: {token}");
    }
    if (token.Value != "(") {
      throw new ScriptError.ParsingError(token, $"Expected '('");
    }
    CheckHasMoreTokens(tokens);
    var op = tokens.Dequeue();
    if (op.TokenType != Token.Type.Keyword) {
      throw new ScriptError.ParsingError(token, "Not a valid operator");
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
    tokens.Dequeue();// ")"

    // The sequence below should be ordered by the frequency of the usage. The operators that are more likely to be
    // used in the game should come first.
    return op.Value switch {
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
  }

  static void DecompileInternal(StringBuilder sb, IExpression expression) {
    switch (expression) {
      case AbstractOperator abstractOperator:
        DecompileOperator(sb, abstractOperator);
        break;
      case ConstantValueExpr constExpr:
        sb.Append(constExpr.ValueType switch {
            ScriptValue.TypeEnum.String => $"'{constExpr.ValueFn().AsString}'",
            ScriptValue.TypeEnum.Number => constExpr.ValueFn().AsNumber.ToString(),
            _ => throw new InvalidOperationException($"Unsupported value type: {constExpr.ValueType}"),
        });
        break;
      case SymbolExpr symbolExpr:
        sb.Append(symbolExpr.Value);
        break;
      default:
        throw new InvalidOperationException($"Unsupported expression type: {expression}");
    }
  }

  static void DecompileOperator(StringBuilder sb, AbstractOperator abstractOperator) {
    sb.Append("(");
    var operatorName = abstractOperator switch {
        HasComponentOperator hasComponentOperator => hasComponentOperator.OperatorType switch {
            HasComponentOperator.OpType.HasSignal => HasSignalFunc,
            HasComponentOperator.OpType.HasAction => HasActionFunc,
            _ => throw new InvalidOperationException($"Unsupported operator: {hasComponentOperator}"),
        },
        BinaryOperator binaryOperator => binaryOperator.OperatorType switch {
            BinaryOperator.OpType.Equal => EqOperator,
            BinaryOperator.OpType.NotEqual => NeOperator,
            BinaryOperator.OpType.GreaterThan => GtOperator,
            BinaryOperator.OpType.GreaterThanOrEqual => GeOperator,
            BinaryOperator.OpType.LessThan => LtOperator,
            BinaryOperator.OpType.LessThanOrEqual => LeOperator,
            _ => throw new InvalidOperationException($"Unsupported operator: {binaryOperator}"),
        },
        LogicalOperator logicalOperator => logicalOperator.OperatorType switch {
            LogicalOperator.OpType.And => AndOperator,
            LogicalOperator.OpType.Or => OrOperator,
            _ => throw new InvalidOperationException($"Unsupported operator: {logicalOperator}"),
        },
        MathOperator mathOperator => mathOperator.OperatorType switch {
            MathOperator.OpType.Add => AddOperator,
            MathOperator.OpType.Subtract => SubOperator,
            MathOperator.OpType.Multiply => MulOperator,
            MathOperator.OpType.Divide => DivOperator,
            MathOperator.OpType.Modulus => ModOperator,
            MathOperator.OpType.Min => MinFunc,
            MathOperator.OpType.Max => MaxFunc,
            MathOperator.OpType.Round => RoundFunc,
            _ => throw new InvalidOperationException($"Unsupported operator: {mathOperator}"),
        },
        SignalOperator => SigFunc,
        ActionOperator => ActMethod,
        GetPropertyOperator getPropertyOperator => getPropertyOperator.OperatorType switch {
            GetPropertyOperator.OpType.GetString => GetStrFunc,
            GetPropertyOperator.OpType.GetNumber => GetNumFunc,
            _ => throw new InvalidOperationException($"Unsupported operator: {getPropertyOperator}"),
        },
        ConcatOperator => ConcatFunc,
        _ => throw new InvalidOperationException($"Unsupported operator: {abstractOperator}"),
    };
    sb.Append(operatorName);
    foreach (var operand in abstractOperator.Operands) {
      sb.Append(" ");
      DecompileInternal(sb, operand);
    }
    sb.Append(")");
  }

  #endregion

  #region Tokenizer

  class Tokenizer : TokenizerBase {
    /// <inheritdoc/>
    protected override string StringQuotes => "'";

    /// <inheritdoc/>
    protected override string StopSymbols => "()";

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
