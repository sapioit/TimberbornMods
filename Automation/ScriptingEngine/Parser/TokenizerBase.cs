// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace IgorZ.Automation.ScriptingEngine.Parser;

/// <summary>A simple common expression tokenizer.</summary>
/// <remarks>
/// <p>
/// Tokenizer is a stateless object that can be re-used for tokenizing different inputs. Use <see cref="Tokenize"/>
/// method to split the input into tokens. To create a tokenizer for the specific syntax, inherit
/// <see cref="TokenizerBase"/> and provide the abstract members in the descendants.
/// </p>
/// <p>
/// In a normal case, every token has to be terminated by a symbol from <see cref="StopSymbols"/> or by EOF. The leading
/// space symbols are always stripped, but the space symbol still needs to be present in the stop symbols.
/// </p>
/// <p>Below is the order of consuming tokens. The first match is the winner.
/// <list type="number">
/// <item>
/// Special syntax <see cref="StopSymbolsKeywords"/>. Such keywords can contain stop symbols. If such a keyword ends
/// with a stop symbol, then this will be the token break. Otherwise, a normal stop symbol is required at the end to
/// separate the next token. The type of such matches is <see cref="Token.Type.Keyword"/>.
/// </item>
/// <item>
/// Stop symbols, listed in <see cref="StopSymbols"/>. They separate tokens from each other, but they are also tokens.
/// Each such symbol is captured as <see cref="Token.Type.StopSymbol"/>. The space symbol must always be
/// present there, and it has a special handling: it separates tokens, but it doesn't produce a token by itself. Also,
/// if it's repeated multiple times in a row, all the instances are removed. The "normal" stop symbol will produce a
/// sequence of tokens if repeated.
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
public abstract class TokenizerBase {

  #region Inheritables

  /// <summary>Symbols that quote a string literal.</summary>
  /// <remarks>The opening and closing symbols must be the same.</remarks>
  protected abstract string StringQuotes { get; }

  /// <summary>Symbols that should be present at the end of the keywords and identifiers.</summary>
  /// <remarks>These symbols themselves can get captured as tokens, unless they are a part of keyword.</remarks>
  /// <seealso cref="StopSymbolsKeywords"/>
  protected abstract string StopSymbols { get; }

  /// <summary>Strings that consist of non-stop symbols.</summary>
  /// <remarks>Keywords are strings reserved by the parser. They must end at EOF or a stop symbol.</remarks>
  protected abstract HashSet<string> Keywords { get; }

  /// <summary>Special kind of keywords that can contain stop symbols.</summary>
  /// <remarks>
  /// The check for such keywords is much less efficient that for the regular keywords, so keep this list short.
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
  /// <exception cref="TokenizerException">if cannot properly parse the tokens.</exception>
  public Queue<Token> Tokenize(string input) {
    var currentPos = 0;
    var tokens = new Queue<Token>();
    while (currentPos < input.Length) {
      // Skip the leading spaces.
      while (currentPos < input.Length && input[currentPos] == ' ') {
        currentPos++;
      }
      if (currentPos >= input.Length) {
        break;
      }
      var symbol = input[currentPos];

      // Keyword that contain stop symbols. Not performance efficient.
      string stopSymbolsKeyword = null;
      var symbolsLeft = input.Length - currentPos;
      foreach (var testKeyword in StopSymbolsKeywords) {
        if (testKeyword.Length > symbolsLeft) {
          continue;
        }
        var testInput = input.Substring(currentPos, testKeyword.Length);
        if (testInput == testKeyword) {
          stopSymbolsKeyword = testKeyword;
          break;
        }
      }
      if (stopSymbolsKeyword != null) {
        var startPos = currentPos;
        currentPos += stopSymbolsKeyword.Length;
        if (!StopSymbols.Contains(stopSymbolsKeyword[^1])) {
          CheckTokenTerminated(input, startPos, currentPos);
        }
        tokens.Enqueue(new Token(stopSymbolsKeyword, Token.Type.Keyword, startPos, currentPos));
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
            if (currentPos >= input.Length || input[currentPos] != closingSymbol) {
              throw new TokenizerException("Bad string literal escaping", currentPos - 1, currentPos); 
            }
            symbol = input[currentPos++];
          } 
          literal.Append(symbol);
        }
        if (!properlyTerminated) {
          throw new TokenizerException("Unterminated string literal", startPos, currentPos);
        }
        CheckTokenTerminated(input, startPos, currentPos);
        tokens.Enqueue(new Token(literal.ToString(), Token.Type.StringLiteral, startPos, currentPos));
        continue;
      }

      // Capture a normal token, that consists of non-stop symbols and ends te EOF or a stop symbol.
      var tokenStartPos = currentPos;
      while (currentPos < input.Length && !StopSymbols.Contains(input[currentPos])) {
        currentPos++;
      }
      var tokenString = input.Substring(tokenStartPos, currentPos - tokenStartPos);

      // Capture a numeric constant value.
      if (tokenString[0] is >= '0' and <= '9' or '-') {
        if (!float.TryParse(tokenString, out _)) {
          throw new TokenizerException($"Not a valid number: '{tokenString}'", tokenStartPos, currentPos);
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
      if (!_identifierRegexp.IsMatch(tokenString)) {
        throw new TokenizerException($"Invalid identifier: {tokenString}", tokenStartPos, currentPos);
      }
      CheckTokenTerminated(input, tokenStartPos, currentPos);
      tokens.Enqueue(new Token(tokenString, Token.Type.Identifier, tokenStartPos, currentPos));
    }
    return tokens;
  }

  #endregion

  #region Implementation

  readonly Regex _identifierRegexp = new("^([a-zA-Z][a-zA-Z0-9]*)(.[a-zA-Z0-9]+)*$");

  void CheckTokenTerminated(string input, int startPos, int endPos) {
    if (endPos < input.Length && !StopSymbols.Contains(input[endPos])) {
      throw new TokenizerException(
          $"Expected stop symbol at token end: {input.Substring(startPos, endPos - startPos)}", startPos, endPos);
    }
  }

  #endregion
}
