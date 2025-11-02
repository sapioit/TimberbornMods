// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;

namespace IgorZ.Automation.ScriptingEngine.Parser;

/// <summary>Tokenizer error.</summary>
public sealed class TokenizerException(string error, int startPos, int endPos)
    : Exception($"{error}, at {startPos}-{endPos-1}") {
  /// <summary>Short error message.</summary>
  public string Error { get; } = error;
  /// <summary>The token start position in the tokenazier's input.</summary>
  public int StartPos { get; } = startPos;
  /// <summary>The token end position in the tokenazier's input.</summary>
  public int EndPos { get; } = endPos;
}
