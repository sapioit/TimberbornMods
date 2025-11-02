// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using Bindito.Core;
using IgorZ.Automation.ScriptingEngine.ScriptableComponents.Components;

namespace IgorZ.Automation.ScriptingEngine.ScriptableComponents;

// ReSharper disable once UnusedType.Global
[Context("Game")]
sealed class Configurator : IConfigurator {
  public void Configure(IContainerDefinition containerDefinition) {
    containerDefinition.Bind<SignalDispatcher>().AsTransient();
  }
}
