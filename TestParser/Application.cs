using System;
using System.Collections.Generic;
using Bindito.Core;
using IgorZ.Automation.AutomationSystem;
using IgorZ.Automation.ScriptingEngine.Core;
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

  readonly List<string> _testSamples = [
      // "1 * (2 / 3)",
      // "1 * 2 / 3",
      // "(1 * 2) / 3",
      //"1 % 2 % 3",
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
      "Signals.Set('yellow', 12)",
  ];

  IContainer _container;

  void Run() {
    RegisterComponents();
    var pyParser = _container.GetInstance<PythonSyntaxParser>();
    var lispParser = _container.GetInstance<LispSyntaxParser>();
    var describer = _container.GetInstance<ExpressionDescriber>();
    var behavior = new AutomationBehavior();
    foreach (var testFormula in _testSamples) {
      Console.WriteLine("Test: " + testFormula);
      var result = pyParser.Parse(testFormula, behavior);
      var expression = result.ParsedExpression;
      if (expression != null) {
        Console.WriteLine("pyParser.Decompile: " + pyParser.Decompile(expression));
        Console.WriteLine("lispParser.Decompile: " + lispParser.Decompile(expression));
        Console.WriteLine("Describe: " + describer.DescribeExpression(expression));
        Console.WriteLine("Expression: " + expression);
      } else {
        Console.WriteLine(result.LastScriptError);
      }
      Console.WriteLine();
    }
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
