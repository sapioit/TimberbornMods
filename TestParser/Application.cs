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
using TestParser.Stubs.Game;

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
       "\"te'st\" == 'test'",
       "'te\"st' == 'test'",
       @"'te\\st' == 'test'",
       "'test' != 'test'",
       "-1.",
       "12.",
       "123.0",
       "-123.0",
       "123.-10",  // 123.0 - 10
       "( 12 * 1 - 2 ) * 3 + 3 / 2 / ( 32 + 4 ) * max(12, 13, 14) / min(1,2,3)",
       "max(12, 13, 14)/ (min ((1-2)-3,2,3) / Signals.Var1)",
       "max(12, 13, 14)/ (min (1-(2-3),2,3) / Signals.Var2)",
       "100 >= -200",
       "Signals.Set('yellow', 12)",
       "concat(1, '-test', 2) == '1-test-2'",
       "getnum('Foobar.numInt')",
       "getnum('Foobar.numFloat')",
       "getnum('Foobar.strList')",
       "getnum('Foobar.boolFalse')",
  ];

  readonly List<string> _equationTests = [
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
      "round(1.01) == 1",
      "round(1.61) == 2",
      "min(1,2,3) == 1",
      "max(1,2,3) == 3",
      "-1 == (0 - 1)",
      "getstr('Foobar.str') == 'test'",
      "getstr('Foobar.strOverridden') == 'overridden'",
      "getnum('Foobar.numInt') == 123",
      "getnum('Foobar.numFloat') == 123.33",
      "getnum('Foobar.strList') == 2",
      "getstr('Foobar.strList', 1) == 'two'",
      "getnum('Foobar.numList', 1) == 2",
      "getnum('Foobar.boolFalse') == 0",
      "getnum('Foobar.boolTrue') == 1",
  ];

  readonly List<string> _badScriptSamples = [
      "1 + ()",
      "1 + ((1)",
      "1 + (1))",
      "1 + (1 + 2 + )",
      "1 + (1 + 2 + 3",
      "1 + (1 + 2 3)",
      "(1 + 2 ())",
      "'test' > 'test'",
      "'test' == 123",
      @"'te\st' == 'test'",
      "\"te\\st\" == 'test'",
      "'te\\\"st' == 'test'",
      "\"te\\'st\" == 'test'",
      "-.01",
      "01.abc",
      "Signals.1Var",
      "Signals.Set(1, 2 3)",
      "Signals.Set(1, 2, 3",
      "Signals.Set 1, 2, 3)",
      "max(12, 13, 14)/ (min (1-(2-3),2,3) / Test.Var1) + (Signals.Set(\"yellow1\", 34))",
      "(12 * 1 - 2) * 3 + 3 / 2 / (32 + 4) * 7 + \"te'st\" + loh.loh<=1",
      "getnum('test')",  // Bad name format. Must be: foo.bar.
      "getstr('Foobar.numInt')",
      "getnum('Foobar.str)",
      "getstr('Foobar.strList')",
      "getnum('foobar.numList', 2)",  // Index out of range.
      "concat()",
      "min(1)",
      "max(2)",
      "round()",
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
    PatchStubs.Apply();
    var behavior = new AutomationBehavior();

    RunGoodScriptSamples(showErrorsOnly: true);
    RunBadScriptSamples(showErrorsOnly: true);
    RunMathExpressions(showErrorsOnly: true);
  }

  void RunMathExpressions(bool showErrorsOnly = false) {
    var pyParser = _container.GetInstance<PythonSyntaxParser>();
    var behavior = new AutomationBehavior();
    Console.WriteLine("Equations must pass:");
    var success = 0;
    foreach (var testFormula in _equationTests) {
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
    Console.WriteLine($"{success} of {_equationTests.Count} test samples passed\n");
  }

  void RunGoodScriptSamples(bool showErrorsOnly = false) {
    var pyParser = _container.GetInstance<PythonSyntaxParser>();
    var lispParser = _container.GetInstance<LispSyntaxParser>();
    var behavior = new AutomationBehavior();
    Console.WriteLine("Samples that must pass:");
    var success = 0;
    foreach (var testFormula in _goodScriptSamples) {
      var result = pyParser.Parse(testFormula, behavior);
      if (result.LastScriptError != null) {
        Console.WriteLine("[FAIL] Pattern: " + testFormula);
        Console.WriteLine($"  * Python parse error: {result.LastError}");
        continue;
      }
      var decompile =  pyParser.Decompile(result.ParsedExpression);
      result = pyParser.Parse(decompile, behavior);
      if (result.LastScriptError != null) {
        Console.WriteLine("[FAIL] Pattern: " + testFormula);
        Console.WriteLine($"  * Python decompiled parse error: {decompile} => {result.LastError}");
        continue;
      }
      decompile = lispParser.Decompile(result.ParsedExpression);
      result = lispParser.Parse(decompile, behavior);
      if (result.LastScriptError != null) {
        Console.WriteLine("[FAIL] Pattern: " + testFormula);
        Console.WriteLine($"  * Lisp decompiled parse error: {decompile} => {result.LastError}");
        continue;
      }
      success++;
      if (!showErrorsOnly) {
        Console.WriteLine("[PASS] Pattern: " + testFormula);
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
    scriptingService.RegisterScriptable(_container.GetInstance<FoobarScriptingComponent>());
  }

  class ComponentsConfigurator : IConfigurator {
    public void Configure(IContainerDefinition containerDefinition) {
      containerDefinition.Bind<ExpressionDescriber>().AsSingleton();
      containerDefinition.Bind<SignalDispatcher>().AsSingleton();
      containerDefinition.Bind<ScriptingService>().AsSingleton();
      containerDefinition.Bind<SignalsScriptableComponent>().AsSingleton();
      containerDefinition.Bind<FoobarScriptingComponent>().AsSingleton();
    }
  }
}
