using System;
using System.Collections.Generic;
using TestParser.Export;

namespace TestParser;

public class Application {
  public static void Main(string[] args) {
    var parser = new Application();
    parser.Run();
  }

  readonly List<string> _testSamples = [
      // "1 * (2 / 3)",
      // "1 * 2 / 3",
      // "(1 * 2) / 3",
      // "1 % 2 % 3",
      // "1 % (2 % 3)",
      // "(1 % 2) % 3",
      // "(1 % 2) / 3",
      // "1 % 2 / 3",
      // "( 12 * 1 - 2 ) * 3 + 3 / 2 / ( 32 + 4 ) * 7",
      // "(12 * 1 - 2) * 3 + 3 / 2 / (32 + 4) * 7 + \"te'st\" + loh.loh<=1",
      // "( 12 * 1 - 2 ) * 3 + 3 / 2 / ( 32 + 4 ) * max(12, 13, 14) / min(1,2,3)",
      // "max(12, 13, 14)/ (min (1-(2-3),2,3) / Test.Var1) + (Signals.Set(\"yellow1\", 34))",
      // "max(12, 13, 14)/ (min ((1-2)-3),2,3) / Test.Var1)",
      // "100 >= -200",
      "Inventory.OutputGood.Water == 100 and Inventory.OutputGood.Water == 200 or Inventory.OutputGood.Water == 300",
      "(Inventory.OutputGood.Water == 100 and Inventory.OutputGood.Water == 200 or Inventory.OutputGood.Water == 300)",
      "test(Inventory.OutputGood.Water == 100 and Inventory.OutputGood.Water == 200 or Inventory.OutputGood.Water == 300)",
  ];

  void Run() {
    var parser = new PythonSyntaxParser();
    foreach (var testFormula in _testSamples) {
      Console.WriteLine("Test: " + testFormula);
      var expression = parser.ParseExpression(testFormula);
      Console.WriteLine("Deconstruct: " + parser.DeconstructPython(expression));
      Console.WriteLine(expression);
      Console.WriteLine();
    }
  }
}
