// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using IgorZ.Automation.ScriptingEngine.Core;

namespace IgorZ.Automation.ScriptingEngine.Parser;

/// <summary>A simple common expression tokenizer.</summary>
/// <remarks>
/// <p>
/// Tokenizer is a stateless object that can be re-used for tokenizing different inputs. Use <see cref="Tokenize"/>
/// method to split the input into tokens. To create a tokenizer for the specific syntax, inherit
/// <see cref="TokenizerBase"/> and provide the abstract members in the descendants.
/// </p>
/// <p>
/// In a normal case, every token has to be terminated by a symbol from <see cref="StopSymbols"/>,
/// <see cref="Whitespaces"/> or by EOF. The leading whitespaces are stripped.
/// </p>
/// <p>Below is the order of consuming tokens. The first match is the winner.
/// <list type="number">
/// <item>
/// The leading symbols from <see cref="Whitespaces"/> are skipped until the first symbols that is not in this list. In
/// all the other means, the whitespaces are considered to <i>regular stop symbols</i> down below of this list.
/// </item>
/// <item>
/// Special syntax <see cref="StopSymbolsKeywords"/>. Such keywords can contain stop symbols (but not whitespaces!). If
/// such a keyword ends with a stop symbol, then this will be the token break. Otherwise, a normal stop symbol is
/// required at the end to separate the next token, and it will become a token itself. The type of such matches is
/// <see cref="Token.Type.Keyword"/>.
/// </item>
/// <item>
/// Stop symbols, listed in <see cref="StopSymbols"/>. They separate tokens from each other, but they are also tokens.
/// Each such symbol is captured as <see cref="Token.Type.StopSymbol"/>.
/// </item>
/// <item>
/// If a token, wrapped by stop symbols, starts and ends with <see cref="StringQuotes"/> symbols, then it's captured as
/// a <see cref="Token.Type.StringLiteral"/> type. Inside the quoted literal, the quotation symbol can be escaped with
/// "\" symbol. The value of such token is the literal itself, without the quotes.
/// </item>
/// <item>
/// If a token, wrapped by stop symbols, starts with a digit or symbol '-', then it's a
/// <see cref="Token.Type.NumericValue"/> token. The value is not checked to be a parsable numeric value. Note, that if
/// symbol '-' is in the stop symbols or keywords, then it will become a separate token. 
/// </item>
/// <item>
/// If a token, wrapped by stop symbols, matches any of <see cref="Keywords"/>, then it's a
/// <see cref="Token.Type.Keyword"/> token. In a common syntax, most of the tokens fall into this category.
/// </item>
/// <item>
/// As a last chance, the token, wrapped by stop symbols, is checked for being a <see cref="Token.Type.Identifier"/>.
/// This must be a continues string that starts from a letter and can contain letters, digits and dots (but not one
/// after another).
/// </item>
/// <item>
/// There is no "unexpected token" case. If nothing matched, then it's an "invalid identifier" case.
/// </item>
/// </list>
/// </p>
/// </remarks>
public abstract partial class TokenizerBase {

  #region Inheritables

  /// <summary>Symbols that quote a string literal.</summary>
  /// <remarks>The opening and closing symbols must be the same.</remarks>
  protected abstract string StringQuotes { get; }

  /// <summary>Symbols that break the input to tokens and becomes tokens themselves.</summary>
  /// <seealso cref="StopSymbolsKeywords"/>
  protected abstract string StopSymbols { get; }

  /// <summary>Stop symbols that break the input to tokens, but don't become tokens themselves.</summary>
  protected virtual string Whitespaces { get; } = " \r\n\t";

  /// <summary>Strings that consist of non-stop symbols.</summary>
  /// <remarks>Keywords are strings, reserved by the parser. They must end at EOF or a stop symbol.</remarks>
  protected abstract HashSet<string> Keywords { get; }

  /// <summary>Special kind of keywords that can contain stop symbols.</summary>
  /// <remarks>
  /// <see cref="StopSymbols"/> can be a part of such keywords, but not the <see cref="Whitespaces"/>! The check for
  /// such keywords is much less efficient than for the regular keywords, so try to keep this list short.
  /// </remarks>
  protected abstract string[] StopSymbolsKeywords { get; }

  #endregion

  #region API

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

  /// <summary>Splits the input and returns thу tokens.</summary>
  /// <exception cref="ScriptError.ParsingError">if cannot properly parse the tokens.</exception>
  public Queue<Token> Tokenize(string input) {
    // No matter how many comments are left, this will be forgotten anyway. The longer strings must come before the
    // shorter ones to prevent partial match!
    _sortedStopSymbolsKeywords ??= StopSymbolsKeywords.OrderByDescending(x => x.Length).ToArray();

    var currentPos = 0;
    var tokens = new Queue<Token>();
    while (currentPos < input.Length) {
      // Skip the leading whitespaces.
      while (currentPos < input.Length && Whitespaces.Contains(input[currentPos])) {
        currentPos++;
      }
      if (currentPos >= input.Length) {
        break;
      }
      var symbol = input[currentPos];

      // Keyword that contain stop symbols. Not performance efficient.
      var symbolsLeft = input.Length - currentPos;
      string testInput = null;
      var matchFound = false;
      foreach (var testKeyword in _sortedStopSymbolsKeywords) {
        if (testKeyword.Length > symbolsLeft) {
          continue;
        }
        var endPos = currentPos + testKeyword.Length;
        if (!StopSymbols.Contains(testKeyword[^1])
            && endPos < input.Length && !StopSymbols.Contains(input[endPos]) && !Whitespaces.Contains(input[endPos])) {
          continue; // Partial match.
        }
        if (testInput == null || testInput.Length != testKeyword.Length) {
          testInput = input.Substring(currentPos, testKeyword.Length);
        }
        if (testInput == testKeyword) {
          matchFound = true;
          break;
        }
      }
      if (matchFound) {
        var startPos = currentPos;
        currentPos += testInput.Length;
        tokens.Enqueue(new Token(testInput, Token.Type.Keyword, startPos, currentPos));
        continue;
      }

      // Stop symbols.
      if (StopSymbols.Contains(symbol)) {
        tokens.Enqueue(new Token(symbol.ToString(), Token.Type.StopSymbol, currentPos, currentPos + 1));
        currentPos++;
        continue;
      }

      // Capture a string literal.
      // It's a special case since string literal can contain stop symbols.
      if (StringQuotes.Contains(symbol)) {
        var startPos = currentPos;
        var closingSymbol = symbol;
        var properlyTerminated = false;
        currentPos++;
        var literal = new StringBuilder();
        while (currentPos < input.Length) {
          symbol = input[currentPos++];
          if (symbol == closingSymbol) {
            properlyTerminated = true;
            break;
          }
          if (symbol == '\\') {
            if (currentPos >= input.Length || input[currentPos] != closingSymbol && input[currentPos] != '\\') {
              throw new ScriptError.ParsingError($"Bad string literal escaping at {currentPos - 1}"); 
            }
            symbol = input[currentPos++];
          } 
          literal.Append(symbol);
        }
        if (!properlyTerminated) {
          throw new ScriptError.ParsingError($"Unterminated string literal at {startPos}-{currentPos - 1}");
        }
        CheckTokenTerminated(input, startPos, currentPos);
        tokens.Enqueue(new Token(literal.ToString(), Token.Type.StringLiteral, startPos, currentPos));
        continue;
      }

      // Capture a normal token, that consists of non-stop symbols and ends at EOF or a stop symbol.
      var tokenStartPos = currentPos;
      while (currentPos < input.Length
             && !StopSymbols.Contains(input[currentPos]) && !Whitespaces.Contains(input[currentPos])) {
        currentPos++;
      }
      var tokenString = input.Substring(tokenStartPos, currentPos - tokenStartPos);

      // Capture a numeric constant value.
      if (tokenString[0] is >= '0' and <= '9' or '-') {
        if (!float.TryParse(tokenString, out _)) {
          throw new ScriptError.ParsingError($"Not a valid number '{tokenString}' at {tokenStartPos}-{currentPos - 1}");
        }
        CheckTokenTerminated(input, tokenStartPos, currentPos);
        tokens.Enqueue(new Token(tokenString, Token.Type.NumericValue, tokenStartPos, currentPos));
        continue;
      }

      // Capture keywords
      if (Keywords.Contains(tokenString)) {
        CheckTokenTerminated(input, tokenStartPos, currentPos);
        tokens.Enqueue(new Token(tokenString, Token.Type.Keyword, tokenStartPos, currentPos));
        continue;
      }

      // Capture identifier.
      if (!IdentifierRegexp().IsMatch(tokenString)) {
        throw new ScriptError.ParsingError($"Invalid identifier '{tokenString}' at {tokenStartPos}-{currentPos-1}");
      }
      CheckTokenTerminated(input, tokenStartPos, currentPos);
      tokens.Enqueue(new Token(tokenString, Token.Type.Identifier, tokenStartPos, currentPos));
    }
    return tokens;
  }

  /// <summary>Returns a quoted and properly escaped string.</summary>
  /// <param name="input">The plain string to quote.</param>
  /// <param name="quoteSymbol">
  /// Symbol to use for quotes. If not provided, then the first symbol from <see cref="StringQuotes"/> will be used.
  /// </param>
  public string EscapeString(string input, char? quoteSymbol = null) {
    var quote = quoteSymbol ?? StringQuotes[0];
    input = input.Replace(@"\", @"\\");
    for (var i = StringQuotes.Length - 1; i >= 0; i--) {
      var symbol = StringQuotes[i];
      if (symbol == quote) {
        input = input.Replace(symbol.ToString(), $"\\{symbol}");
      }
    }
    return $"{quote}{input}{quote}";
  }

  #endregion

  #region Implementation

  [GeneratedRegex("^([a-zA-Z][a-zA-Z0-9]*)(.[a-zA-Z0-9]+)*$")]
  private static partial Regex IdentifierRegexp();

  string[] _sortedStopSymbolsKeywords;

  void CheckTokenTerminated(string input, int startPos, int endPos) {
    if (endPos >= input.Length || StopSymbols.Contains(input[endPos]) || Whitespaces.Contains(input[endPos])) {
      return;
    }
    var tokenString = input.Substring(startPos, endPos - startPos);
    throw new ScriptError.ParsingError($"Expected stop symbol at token end: {tokenString} at {startPos}-{endPos-1}");
  }

  #endregion
}
