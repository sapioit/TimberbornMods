using System;
using System.Collections.Generic;
using IgorZ.Automation.AutomationSystem;
using IgorZ.Automation.ScriptingEngine.Parser;

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
      "1 % 2 % 3",
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
      // "Inventory.OutputGood.Water == 100 and Inventory.OutputGood.Water == 200 or Inventory.OutputGood.Water == 300",
      // "(Inventory.OutputGood.Water == 100 and Inventory.OutputGood.Water == 200 or Inventory.OutputGood.Water == 300)",
      // "test(Inventory.OutputGood.Water == 100 and Inventory.OutputGood.Water == 200 or Inventory.OutputGood.Water == 300)",
      // "Inventory.OutputGood.Water >= 100 and not (100 == 200 % 23 + 1 or 10 != 10)",
      //"Inventory.OutputGood.Water >= -100 and not 100 == 200 % 23 + 1 or 10 != 10 or Signals.Set('yellow', 12)",
  ];

  void Run() {
    var parser = new PythonSyntaxParser();
    var behavior = new AutomationBehavior();
    foreach (var testFormula in _testSamples) {
      Console.WriteLine("Test: " + testFormula);
      var result = parser.Parse(testFormula, behavior);
      var expression = result.ParsedExpression;
      Console.WriteLine("Deconstruct: " + parser.Decompile(expression));
      Console.WriteLine(expression);
      Console.WriteLine();
    }
  }
}
