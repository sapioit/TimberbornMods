// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Text.RegularExpressions;
using IgorZ.Automation.ScriptingEngine.Core;

namespace IgorZ.Automation.ScriptingEngine.Expressions;

sealed class SymbolExpr : IExpression {

  /// <summary>Symbol name.</summary>
  public readonly string Value;

  /// <inheritdoc/>
  public string Describe() {
    throw new NotImplementedException();
  }

  /// <inheritdoc/>
  public void VisitNodes(Action<IExpression> visitorFn) {
    visitorFn(this);
  }

  /// <inheritdoc/>
  public override string ToString() {
    return $"{GetType().Name}#{Value}";
  }

  public static SymbolExpr Create(string value) => new(value);

  SymbolExpr(string value) {
    CheckName(value);
    Value = value;
  }

  static readonly Regex SymbolRegex = new(@"^(?!\.)([A-Za-z][A-Za-z0-9]+\.?)*([A-Za-z][A-Za-z0-9]*)$");

  /// <summary>
  /// Verifies if the name consists of alphanumeric symbols and doesn't have leading or trailing dots.
  /// </summary>
  /// <exception cref="ScriptError.ParsingError">if the name doesn't pass the check</exception>
  public static void CheckName(string value) {
    if (!SymbolRegex.IsMatch(value)) {
      throw new ScriptError.ParsingError("Bad symbol name: " + value);
    }
  }
}
