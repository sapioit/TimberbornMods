// Timberborn Utils
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using Bindito.Core;

// ReSharper disable once CheckNamespace
namespace IgorZ.CustomTools.Test;

[Context("Game")]
class Configurator : IConfigurator {
  public void Configure(IContainerDefinition containerDefinition) {
    containerDefinition.Bind<TestTool>().AsTransient();
  }
}
