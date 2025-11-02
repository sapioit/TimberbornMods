// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

namespace IgorZ.Automation.ScriptingEngine.Parser;

/// <summary>A captured token. It has a string value and denotes its position in the input.</summary>
public record struct Token(string Value, Token.Type TokenType, int StartPos, int EndPos) {
  /// <summary>The matched type of the token.</summary>
  /// <remarks>It refers to the tokenizer settings, not the language syntax.</remarks>
  public enum Type {
    /// <summary>One of the predefined keywords.</summary>
    Keyword,

    /// <summary>A single stop symbol which separates the tokens.</summary>
    StopSymbol,

    /// <summary>A string literal. The starting and ending quotes are removed. All escapes expanded.</summary>
    StringLiteral,

    /// <summary>A string representation of a float number. It is NOT validated to be parsable.</summary>
    NumericValue,

    /// <summary>
    /// A string that starts with a letter and consists of letters, digits and dots (not repeated consequently).
    /// </summary>
    Identifier,
  }
}
