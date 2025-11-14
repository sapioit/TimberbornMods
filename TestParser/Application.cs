using System;
using System.Collections.Generic;
using Bindito.Core;
using IgorZ.Automation.AutomationSystem;
using IgorZ.Automation.ScriptingEngine.Core;
using IgorZ.Automation.ScriptingEngine.Expressions;
using IgorZ.Automation.ScriptingEngine.Parser;
using IgorZ.Automation.ScriptingEngine.ScriptableComponents.Components;
using IgorZ.Automation.ScriptingEngineUI;
using TestParser.Stubs;

namespace TestParser;

public class Application {
  public static void Main(string[] args) {
    var parser = new Application();
    parser.Run();
  }

  readonly Dictionary<string, string> _localizations = new();

  readonly List<string> _goodScriptSamples = [
       "1 * (2 / 3)",
       "1 * 2 / 3",
       "(1 * 2) / 3",
      "1 % 2 % 3",
       "1 % (2 % 3)",
       "(1 % 2) % 3",
       "(1 % 2) / 3",
       "1 % 2 / 3",
       "( 12 * 1 - 2 ) * 3 + 3 / 2 / ( 32 + 4 ) * 7",
       "\"te'st\" == 'te\\'st'",
       "'test' != 'test'",
       "( 12 * 1 - 2 ) * 3 + 3 / 2 / ( 32 + 4 ) * max(12, 13, 14) / min(1,2,3)",
       "max(12, 13, 14)/ (min ((1-2)-3,2,3) / Signals.Var1)",
       "max(12, 13, 14)/ (min (1-(2-3),2,3) / Signals.Var2)",
       "100 >= -200",
       "Signals.Set('yellow', 12)",
  ];

  readonly List<string> _mathTests = [
      "1.5 * (20 / -5) == -6.00",
      "1.5 * -(20 / -5) == 6.00",
      "-1.5 * -(20 / -5) == -6.00",
      "--20 / 10 / 2 == 1",
      "-(-20 / 10) / 2 == 1",
      "--20 / (10 / 2) == 4",
      "1 - 2 - 3 == -4",
      "(1 - 2) - 3 == -4",
      "1 - (2 - 3) == 2",
      "1 - 2 > -2",
      "1 - 2 >= -1",
      "1.001 == 1.00", // FIXME: probably fail on such constants?
      "1.006 == 1.01",
      "1 + 0.006 == 1.01",
      "1.003 + 0.003 == 1",
  ];

  readonly List<string> _badScriptSamples = [
      "1 + (1 + 2 + )",
      "1 + (1 + 2 + 3",
      "1 + (1 + 2 3)",
      "'test' > 'test'",
      "'test' == 123",
      "Signals.Set(1, 2 3)",
      "Signals.Set(1, 2, 3",
      "Signals.Set 1, 2, 3)",
      "max(12, 13, 14)/ (min (1-(2-3),2,3) / Test.Var1) + (Signals.Set(\"yellow1\", 34))",
      "(12 * 1 - 2) * 3 + 3 / 2 / (32 + 4) * 7 + \"te'st\" + loh.loh<=1",
  ];

  readonly List<string> _diffSamples = [
      // "Inventory.OutputGood.Water == 100 and Inventory.OutputGood.Water == 200 or Inventory.OutputGood.Water == 300",
      // "(Inventory.OutputGood.Water == 100 and Inventory.OutputGood.Water == 200 or Inventory.OutputGood.Water == 300)",
      // "test(Inventory.OutputGood.Water == 100 and Inventory.OutputGood.Water == 200 or Inventory.OutputGood.Water == 300)",
      // "Inventory.OutputGood.Water >= 100 and not (100 == 200 % 23 + 1 or 10 != 10)",
      //"Inventory.OutputGood.Water >= -100 and not 100 == 200 % 23 + 1 or 10 != 10 or Signals.Set('yellow', 12)",
      //"Signals.Set('yellow', 12)",
      "Signals.Set('yellow', 12)",
  ];

  IContainer _container;

  void Run() {
    RegisterComponents();
    RunGoodScriptSamples(showErrorsOnly: true);
    RunBadScriptSamples(showErrorsOnly: true);
    RunMathExpressions(showErrorsOnly: true);

    var pyParser = _container.GetInstance<PythonSyntaxParser>();
    var lispParser = _container.GetInstance<LispSyntaxParser>();
    var describer = _container.GetInstance<ExpressionDescriber>();
    var behavior = new AutomationBehavior();
    foreach (var testFormula in _diffSamples) {
      Console.WriteLine("Test: " + testFormula);
      var result = pyParser.Parse(testFormula, behavior);
      var expression = result.ParsedExpression;
      if (expression != null) {
        Console.WriteLine("pyParser.Decompile: " + pyParser.Decompile(expression));
        Console.WriteLine("lispParser.Decompile: " + lispParser.Decompile(expression));
        Console.WriteLine("Describe: " + describer.DescribeExpression(expression));
        Console.WriteLine("Expression: " + expression);
      } else {
        Console.WriteLine(result.LastError);
      }
      Console.WriteLine();
    }
  }

  void RunMathExpressions(bool showErrorsOnly = false) {
    var pyParser = _container.GetInstance<PythonSyntaxParser>();
    var behavior = new AutomationBehavior();
    Console.WriteLine("Math expressions must pass:");
    var success = 0;
    foreach (var testFormula in _mathTests) {
      var result = pyParser.Parse(testFormula, behavior);
      if (result.ParsedExpression == null) {
        Console.WriteLine("[FAIL] Pattern: " + testFormula);
        Console.WriteLine("  * Error: " + result.LastError);
        continue;
      }
      if (result.ParsedExpression is not BoolOperator boolOperator) {
        Console.WriteLine("[FAIL] Pattern: " + testFormula);
        Console.WriteLine("  * Not a boolean operator: " + result.ParsedExpression);
        continue;
      }
      if (!boolOperator.Execute()) {
        Console.WriteLine("[FAIL] Pattern: " + testFormula);
        Console.WriteLine("  * Evaluated to FALSE");
        continue;
      }
      success++;
      if (!showErrorsOnly) {
        Console.WriteLine("[PASS] Pattern: " + testFormula);
      }
    }
    Console.WriteLine($"{success} of {_mathTests.Count} test samples passed\n");
  }

  void RunGoodScriptSamples(bool showErrorsOnly = false) {
    var pyParser = _container.GetInstance<PythonSyntaxParser>();
    var behavior = new AutomationBehavior();
    Console.WriteLine("Samples that must pass:");
    var success = 0;
    foreach (var testFormula in _goodScriptSamples) {
      var result = pyParser.Parse(testFormula, behavior);
      if (result.LastScriptError == null) {
        var decompile =  pyParser.Decompile(result.ParsedExpression);
        result = pyParser.Parse(decompile, behavior);
        if (result.LastScriptError == null) {
          success++;
          if (showErrorsOnly) {
            continue;
          }
          Console.WriteLine("[PASS] Pattern: " + testFormula);
        } else {
          Console.WriteLine("[FAIL] Pattern: " + testFormula);
          Console.WriteLine("  * Decompiled: " + decompile);
        }
      } else {
        Console.WriteLine("[FAIL] Pattern: " + testFormula);
        Console.WriteLine("  * Error: " + result.LastError);
      }
    }
    Console.WriteLine($"{success} of {_goodScriptSamples.Count} test samples passed\n");
  }

  void RunBadScriptSamples(bool showErrorsOnly = false) {
    var pyParser = _container.GetInstance<PythonSyntaxParser>();
    var behavior = new AutomationBehavior();
    Console.WriteLine("Samples that must fail:");
    var success = 0;
    foreach (var testFormula in _badScriptSamples) {
      var result = pyParser.Parse(testFormula, behavior);
      if (result.LastScriptError != null) {
        success++;
        if (showErrorsOnly) {
          continue;
        }
        Console.WriteLine("[PASS] Pattern: " + testFormula);
      }
      Console.WriteLine("[FAIL] Pattern: " + testFormula);
      Console.WriteLine("  * Expression: " + result.ParsedExpression);
    }
    Console.WriteLine($"{success} of {_badScriptSamples.Count} test samples passed\n");
  }

  void RegisterComponents() {
    IConfigurator[] configurators = [
        new StubsConfigurator(),
        new IgorZ.Automation.ScriptingEngine.Parser.Configurator(),
        new ComponentsConfigurator(),
    ]; 
    _container = Bindito.Core.Bindito.CreateContainer(configurators);

    var scriptingService = _container.GetInstance<ScriptingService>();
    scriptingService.RegisterScriptable(_container.GetInstance<SignalsScriptableComponent>());
  }

  class ComponentsConfigurator : IConfigurator {
    public void Configure(IContainerDefinition containerDefinition) {
      containerDefinition.Bind<ExpressionDescriber>().AsSingleton();
      containerDefinition.Bind<SignalDispatcher>().AsSingleton();
      containerDefinition.Bind<ScriptingService>().AsSingleton();
      containerDefinition.Bind<SignalsScriptableComponent>().AsSingleton();
    }
  }
}
