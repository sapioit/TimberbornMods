// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IgorZ.Automation.ScriptingEngine.Core;

namespace IgorZ.Automation.ScriptingEngine.Expressions;

class MathOperator : AbstractOperator, IValueExpr {
  const string AddOperatorName = "add";
  const string SubOperatorName = "sub";
  const string MulOperatorName = "mul";
  const string DivOperatorName = "div";
  const string ModOperatorName = "mod";
  const string MinOperatorName = "min";
  const string MaxOperatorName = "max";
  const string RoundOperatorName = "round";
  const string NegateOperatorName = "neg";

  public enum OpType {
      Add, Subtract, Multiply, Divide, Modulus, Min, Max, Round, Negate,
  }

  public readonly OpType OperatorType;

  /// <inheritdoc/>
  public ScriptValue.TypeEnum ValueType => ScriptValue.TypeEnum.Number;

  /// <inheritdoc/>
  public Func<ScriptValue> ValueFn { get; }

  /// <inheritdoc/>
  public override string Describe() {
    return Name switch {
        AddOperatorName => $"({Operands.Select(x => x.Describe()).Aggregate((a, b) => a + " + " + b)})",
        SubOperatorName => $"({Operands[0].Describe()} - {Operands[1].Describe()})",
        MulOperatorName => $"{Operands[0].Describe()} × {Operands[1].Describe()}",
        DivOperatorName => $"{Operands[0].Describe()} ÷ {Operands[1].Describe()}",
        ModOperatorName => $"{Operands[0].Describe()} % {Operands[1].Describe()}",
        MinOperatorName or MaxOperatorName => $"{Name}({string.Join(", ", Operands.Select(x => x.Describe()))})",
        RoundOperatorName => $"Round({Operands[0].Describe()})",
        NegateOperatorName => $"-({Operands[0].Describe()})",
        _ => throw new InvalidDataException("Unknown operator: " + Name),
    };
  }

  public static MathOperator CreateAdd(IList<IExpression> arguments) => new(OpType.Add, arguments, 2, -1);
  public static MathOperator CreateSubtract(IList<IExpression> arguments) => new(OpType.Subtract, arguments, 2, 2);
  public static MathOperator CreateMultiply(IList<IExpression> arguments) => new(OpType.Multiply, arguments, 2, 2);
  public static MathOperator CreateDivide(IList<IExpression> arguments) => new(OpType.Divide, arguments, 2, 2);
  public static MathOperator CreateModulus(IList<IExpression> arguments) => new(OpType.Modulus, arguments, 2, 2);
  public static MathOperator CreateMin(IList<IExpression> arguments) => new(OpType.Min, arguments, 2, -1);
  public static MathOperator CreateMax(IList<IExpression> arguments) => new(OpType.Max, arguments, 2, -1);
  public static MathOperator CreateRound(IList<IExpression> arguments) => new(OpType.Round, arguments, 1, 1);
  public static MathOperator CreateNegate(IList<IExpression> arguments) => new(OpType.Negate, arguments, 1, 1);

  /// <inheritdoc/>
  public override string ToString() {
    return $"{GetType().Name}({OperatorType})";
  }

  static readonly string[] Names = ["add", "sub", "mul", "div", "mod", "min", "max", "round", "neg" ];

  MathOperator(OpType opType, IList<IExpression> operands, int minArgs, int maxArgs) : base(Names[(int)opType], operands) {
    OperatorType = opType;
    AsserNumberOfOperandsRange(minArgs, maxArgs);
    for (var i = 0; i < operands.Count; i++) {
      var op = Operands[i];
      if (op is not IValueExpr { ValueType: ScriptValue.TypeEnum.Number } result) {
        throw new ScriptError.ParsingError($"Operand #{i + 1} must be a numeric value, found: {op}");
      }
    }
    if (operands is not IList<IValueExpr> args) {
      throw new InvalidOperationException("Operands must be of type IValueExpr, but got " + operands.GetType());
    }
    ValueFn = opType switch {
        OpType.Add => () => args.Select(x => x.ValueFn()).Aggregate((a, b) => a + b),
        OpType.Subtract => () => args[0].ValueFn() - args[1].ValueFn(),
        OpType.Multiply => () => args[0].ValueFn() * args[1].ValueFn(),
        OpType.Divide => () => args[0].ValueFn() / args[1].ValueFn(),
        OpType.Modulus => () => ScriptValue.FromFloat(args[0].ValueFn().AsFloat % args[1].ValueFn().AsFloat),
        OpType.Min => () => args.Select(x => x.ValueFn()).Min(),
        OpType.Max => () => args.Select(x => x.ValueFn()).Max(),
        OpType.Round => () => ScriptValue.FromInt(args[0].ValueFn().AsInt),
        OpType.Negate => () => ScriptValue.Of(-args[0].ValueFn().AsNumber),
        _ => throw new ArgumentOutOfRangeException(nameof(opType), opType, null),
    };
  }
}
