// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IgorZ.Automation.ScriptingEngine.Core;
using IgorZ.Automation.ScriptingEngine.Parser;
using IgorZ.Automation.ScriptingEngine.ScriptableComponents;
using Timberborn.BaseComponentSystem;

namespace IgorZ.Automation.ScriptingEngine.Expressions;

class GetPropertyOperator : AbstractOperator, IValueExpr {
  /// <inheritdoc/>
  public override string Describe() {
    var symbol = (Operands[0] as SymbolExpr)!.Value;
    if (IsList) {
      return Operands.Count == 1 ? $"Count({symbol})" : $"GetElement({symbol}, {Operands[0].Describe()})";
    }
    return $"ValueOf({symbol})";
  }

  public enum OpType {
    GetString,
    GetNumber,
  }

  public readonly OpType OperatorType;

  /// <inheritdoc/>
  public ScriptValue.TypeEnum ValueType { get; }
  /// <inheritdoc/>
  public Func<ScriptValue> ValueFn { get; }

  /// <summary>Tells if this operator accesses a list property.</summary>
  public bool IsList { get; }

  public static GetPropertyOperator CreateGetNumber(ParserBase.Context context, IList<IExpression> operands) =>
      new(OpType.GetNumber, context, operands);
  public static GetPropertyOperator CreateGetString(ParserBase.Context context, IList<IExpression> operands) =>
      new(OpType.GetString, context, operands);

  GetPropertyOperator(OpType opType, ParserBase.Context context, IList<IExpression> operands)
      : base(opType == OpType.GetNumber ? "getnum" : "getstr", operands) {
    OperatorType = opType;
    ValueType = opType switch {
        OpType.GetNumber => ScriptValue.TypeEnum.Number,
        OpType.GetString => ScriptValue.TypeEnum.String,
        _ => throw new ArgumentOutOfRangeException(nameof(opType), opType, null),
    };
    AsserNumberOfOperandsRange(1, -1);
    if (Operands[0] is not SymbolExpr symbol) {
      throw new ScriptError.ParsingError("Expected a symbol: " + Operands[0]);
    }
    var parts = symbol.Value.Split('.');
    if (parts.Length != 2) {
      throw new ScriptError.ParsingError("Bad property name: " + Operands[0]);
    }

    var propValueFn = context.ScriptingService.GetPropertySource(symbol.Value, context.ScriptHost);
    if (propValueFn == null) {
      var componentName = parts[0];
      var component = GetComponentByName(context.ScriptHost, componentName);
      if (!component) {
        throw new ScriptError.BadStateError(context.ScriptHost, $"Component {componentName} not found");
      }
      var propertyName = parts[1];
      var property = component.GetType().GetProperty(propertyName);
      if (property == null) {
        throw new ScriptError.ParsingError($"Property {propertyName} not found on component {componentName}");
      }
      propValueFn = () => property.GetValue(component)
          ?? (property.PropertyType == typeof(string) ? "NULL" : Activator.CreateInstance(property.PropertyType));
    }

    var listObject = propValueFn();
    var listVal = GetAsList(listObject);
    if (listVal != null) {
      IsList = true;
      if (operands.Count == 1) {
        if (ValueType != ScriptValue.TypeEnum.Number) {
          throw new ScriptError.ParsingError("The list type counter cannot be accessed as string");
        }
        propValueFn = () => GetAsList(listObject).Count;
      } else {
        AsserNumberOfOperandsExact(2);
        if (Operands[1] is not IValueExpr { ValueType: ScriptValue.TypeEnum.Number } indexExpr) {
          throw new ScriptError.ParsingError("Second operand must be a numeric value, found: " + Operands[1]);
        }
        propValueFn = () => {
          var list = GetAsList(listObject);
          var index = indexExpr.ValueFn().AsInt;
          if (index < 0 || index >= operands.Count) {
            throw new ScriptError.ValueOutOfRange($"Index {index} is out of range: [{0}; {list.Count})");
          }
          return list[index];
        };
      }
    }

    var propType = propValueFn().GetType();
    ValueFn = ValueType switch {
        ScriptValue.TypeEnum.Number when propType == typeof(int) => () => ScriptValue.FromInt((int)propValueFn()),
        ScriptValue.TypeEnum.Number when propType == typeof(float) => () => ScriptValue.FromFloat((float)propValueFn()),
        ScriptValue.TypeEnum.Number when propType == typeof(bool) => () => ScriptValue.FromBool((bool)propValueFn()),
        ScriptValue.TypeEnum.Number => throw new ScriptError.ParsingError(
            $"Property {symbol.Value} is of incompatible type: {propType}"),
        ScriptValue.TypeEnum.String when propType == typeof(string) => () => ScriptValue.Of((string)propValueFn()),
        ScriptValue.TypeEnum.String => throw new ScriptError.ParsingError(
            $"Property {symbol.Value} is of incompatible type: {propType}"),
        _ => throw new ArgumentOutOfRangeException(nameof(ValueType), ValueType, null),
    };
  }

  static BaseComponent GetComponentByName(BaseComponent baseComponent, string name) {
    if (name == "Inventory") {
      // Special case: the buildings can have more than one inventory. 
      return InventoryScriptableComponent.GetInventory(baseComponent, throwIfNotFound: false);
    }
    var components = baseComponent.AllComponents.OfType<BaseComponent>();
    return components.FirstOrDefault(x => x.GetType().Name == name);
  }

  /// <summary>Converts an object to a list. The object must implement the GetEnumerator method.</summary>
  static IList GetAsList(object value) {
    if (value is string) {
      return null;  // Strings are enumerable, but they aren't lists.
    }
    var getEnumeratorMethod = value.GetType().GetMethod("GetEnumerator", BindingFlags.Public | BindingFlags.Instance);
    if (getEnumeratorMethod == null) {
      return null;
    }
    var enumerator = getEnumeratorMethod.Invoke(value, null);
    if (enumerator is not IEnumerator enumeratorObj) {
      return null;
    }
    var list = new List<object>();
    while (enumeratorObj.MoveNext()) {
      list.Add(enumeratorObj.Current);
    }
    if (list.Count <= 0) {
      return list;
    }
    // The list must contain trivial types, and be sorted (for the repeatable outcome).
    var sampleValue = list[0];
    if (GoodTypes.All(x => !x.IsAssignableFrom(sampleValue.GetType()))) {
      // The list values aren't trivial. We can't handle them.
      return null;
    }
    list.Sort();
    return list;
  }

  static readonly List<Type> GoodTypes = [
      typeof(string),
      typeof(int),
      typeof(float),
      typeof(bool),
  ];
}
