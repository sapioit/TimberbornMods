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
       "\"te'st\" == 'te\\'st'",
       "'te\"st' == \"te\\\"st\"",
       @"'te\\st' == 'te\\st'",
       "'test' != 'test2'",
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
       "concat(1, '-test-', 2) == '1-test-2'",
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
      "(1 + 2) + 3 + 4",
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
      "getnum()",
      "getstr()",
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

    // TestOneStatement("1 + 2 + 3 + 4", out var reports);
    // Console.Write(string.Join("\n", reports));

    RunGoodScriptSamples(_goodScriptSamples, showErrorsOnly);
    RunGoodScriptSamples(_equationTests, showErrorsOnly);
    RunGoodScriptSamples(_multiArgumentOperatorsTests, showErrorsOnly);
    // RunBadScriptSamples(_badScriptSamples, showErrorsOnly);
  }

  void RunGoodScriptSamples(IList<string> samples, bool showErrorsOnly = false) {
    Console.WriteLine("Samples that must pass:");
    var success = 0;
    foreach (var testFormula in samples) {
      var testPassed = TestOneStatement(testFormula, out var reports);
      if (testPassed) {
        success++;
      }
      if (!testPassed || !showErrorsOnly) {
        Console.WriteLine(string.Join("\n", reports));
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

  bool TestOneStatement(string input, out List<string> reports) {
    var res = true;
    reports = new List<string>();
    var pyParser = _container.GetInstance<PythonSyntaxParser>();
    var lispParser = _container.GetInstance<LispSyntaxParser>();
    var behavior = new AutomationBehavior();
    reports.Add($"Testing: {input}");
    var result = pyParser.Parse(input, behavior);
    if (result.ParsedExpression == null) {
      reports.Add($"  * Failed to parse input as Python: {result.LastScriptError}");
      return false;
    }

    var decompiled1 = pyParser.Decompile(result.ParsedExpression);
    reports.Add($"Decompiled Python: {decompiled1}");
    result = pyParser.Parse(decompiled1, behavior);
    if (result.LastScriptError != null) {
      reports.Add($"  * Failed to parse decompiled Python: {result.LastScriptError}");
      return false;
    }
    var decompiled2 = pyParser.Decompile(result.ParsedExpression);
    if (decompiled1 != decompiled2) {
      reports.Add($"  * ERROR: {decompiled1} is not {decompiled2}");
    }

    decompiled1 = lispParser.Decompile(result.ParsedExpression);
    reports.Add($"Decompiled Lisp: {decompiled1}");
    result = lispParser.Parse(decompiled1, behavior);
    if (result.LastScriptError != null) {
      reports.Add($"  * Failed to parse decompiled Python: {result.LastScriptError}");
      return false;
    }
    decompiled2 = lispParser.Decompile(result.ParsedExpression);
    if (decompiled1 != decompiled2) {
      reports.Add($"  * ERROR: {decompiled1} is not {decompiled2}");
      res = false;
    }

    if (result.ParsedExpression is BoolOperator boolOperator) {
      try {
        if (!boolOperator.Execute()) {
          reports.Add($"  * Boolean operator executed to FALSE");
          res = false;
        }
      } catch (ScriptError e) {
        reports.Add($"  * Failed executing boolean operator: {e.Message}");
        res = false;
      }
    }

    var describer = _container.GetInstance<ExpressionDescriber>();
    try {
      var description = describer.DescribeExpression(result.ParsedExpression);
      reports.Add($"  * Description: {description}");
    } catch (Exception e) {
      reports.Add($"  * Failed making description: {e.Message}");
    }
    return res;
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
