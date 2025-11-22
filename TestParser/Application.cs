using System;
using System.Collections;
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
       "Foobar.EmptyAction()",
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
      "getnum('Foobar.numInt') == 123",
      "getnum('Foobar.numFloat') == 123.33",
      "getnum('Foobar.strList') == 2",
      "getstr('Foobar.strList', 1) == 'two'",
      "getnum('Foobar.numList', 1) == 2",
      "getnum('Foobar.boolFalse') == 0",
      "getnum('Foobar.boolTrue') == 1",
  ];

  readonly List<string> _multiArgumentOperatorsTests = [
      "1 + 2 + (3 + 4)",
      "1 + 2 + (3 - 4)",
      "1 == 1 and 2 == 2 and (3 == 3 or 4 == 4)",
      "1 == 1 or 2 == 2 or 3 == 3 and 4 == 4",
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
      "getstr('Foobar.numInt') == 'foo'",  // getstr auto detects value type.
      "getnum('Foobar.str') == 1",  // getnum auto detects value type.
      "getstr('Foobar.strList')",
      "getnum('foobar.numList', 2)",  // Index out of range.
      "getnum('foobar.numList', 1, 1)",
      "getnum(1)",
      "getnum(Signals.Var1)", // must be a string literal
      //FIXME: try non-value index or non-constant name.
      "concat()",
      "min(1)",
      "max(2)",
      "round()",
  ];

  IContainer _container;

  void Run() {
    const bool showErrorsOnly = true;
    RegisterComponents();
    PatchStubs.Apply();

    //TestOneStatement("getnum(Signals.Var1)");

    RunGoodScriptSamples(_goodScriptSamples, showErrorsOnly);
    RunBadScriptSamples(_badScriptSamples, showErrorsOnly);
    RunEquationTests(_equationTests, showErrorsOnly);
    RunMultiArgumentOperatorsTests(_multiArgumentOperatorsTests, showErrorsOnly);
  }

  void RunEquationTests(IList<string> samples, bool showErrorsOnly = false) {
    var pyParser = _container.GetInstance<PythonSyntaxParser>();
    var lispParser = _container.GetInstance<LispSyntaxParser>();
    var behavior = new AutomationBehavior();
    Console.WriteLine("Equations must pass:");
    var success = 0;
    foreach (var testFormula in samples) {
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
      try {
        pyParser.Decompile(result.ParsedExpression);
        lispParser.Decompile(result.ParsedExpression);
      } catch (Exception) {
        Console.WriteLine("[EXCEPTION] Pattern: " + testFormula);
        throw;
      }
      success++;
      if (!showErrorsOnly) {
        Console.WriteLine("[PASS] Pattern: " + testFormula);
      }
    }
    Console.WriteLine($"{success} of {samples.Count} test samples passed\n");
  }

  void RunMultiArgumentOperatorsTests(IList<string> samples, bool showErrorsOnly = false) {
    var pyParser = _container.GetInstance<PythonSyntaxParser>();
    var lispParser = _container.GetInstance<LispSyntaxParser>();
    var behavior = new AutomationBehavior();
    Console.WriteLine("Multi-argument operators must pass:");
    var success = 0;
    foreach (var testFormula in samples) {
      var result = pyParser.Parse(testFormula, behavior);
      if (result.ParsedExpression == null) {
        Console.WriteLine("[FAIL] Pattern: " + testFormula);
        Console.WriteLine("  * Error: " + result.LastError);
        continue;
      }
      if (result.ParsedExpression is not AbstractOperator abstractOperator) {
        Console.WriteLine("[FAIL] Pattern: " + testFormula);
        Console.WriteLine("  * Not an operator: " + result.ParsedExpression);
        continue;
      }
      if (abstractOperator.Operands.Count < 3) {
        Console.WriteLine("[FAIL] Pattern: " + testFormula);
        Console.WriteLine($"  * Too few operands: {abstractOperator}, operands={abstractOperator.Operands}");
        continue;
      }
      try {
        pyParser.Decompile(result.ParsedExpression);
        lispParser.Decompile(result.ParsedExpression);
      } catch (Exception) {
        Console.WriteLine("[EXCEPTION] Pattern: " + testFormula);
        throw;
      }
      success++;
      if (!showErrorsOnly) {
        Console.WriteLine("[PASS] Pattern: " + testFormula);
      }
    }
    Console.WriteLine($"{success} of {samples.Count} test samples passed\n");
  }

  void RunGoodScriptSamples(IList<string> samples, bool showErrorsOnly = false) {
    var pyParser = _container.GetInstance<PythonSyntaxParser>();
    var lispParser = _container.GetInstance<LispSyntaxParser>();
    var behavior = new AutomationBehavior();
    Console.WriteLine("Samples that must pass:");
    var success = 0;
    foreach (var testFormula in samples) {
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
    Console.WriteLine($"{success} of {samples.Count} test samples passed\n");
  }

  void RunBadScriptSamples(IList<string> samples, bool showErrorsOnly = false) {
    var pyParser = _container.GetInstance<PythonSyntaxParser>();
    var behavior = new AutomationBehavior();
    Console.WriteLine("Samples that must fail:");
    var success = 0;
    foreach (var testFormula in samples) {
      var result = pyParser.Parse(testFormula, behavior);
      if (result.LastScriptError == null) {
        Console.WriteLine("[FAIL] Pattern: " + testFormula);
        Console.WriteLine("  * Didn't fail: " + result.ParsedExpression);
        continue;
      }
      success++;
      if (!showErrorsOnly) {
        Console.WriteLine("[PASS] Pattern: " + testFormula);
      }
    }
    Console.WriteLine($"{success} of {samples.Count} test samples passed\n");
  }

  void TestOneStatement(string input) {
    var pyParser = _container.GetInstance<PythonSyntaxParser>();
    var lispParser = _container.GetInstance<LispSyntaxParser>();
    var behavior = new AutomationBehavior();
    Console.WriteLine("Testing: " + input);
    var result = pyParser.Parse(input, behavior);
    if (result.ParsedExpression == null) {
      throw result.LastScriptError;
    }
    if (result.ParsedExpression is BoolOperator boolOperator && !boolOperator.Execute()) {
      Console.WriteLine("[FAIL] Pattern: " + input);
      Console.WriteLine("  * Boolean operator executed to FALSE");
      return;
    }
    Console.WriteLine("Decompiled Python: " + pyParser.Decompile(result.ParsedExpression));
    Console.WriteLine("Decompiled Lisp: " + lispParser.Decompile(result.ParsedExpression));
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
