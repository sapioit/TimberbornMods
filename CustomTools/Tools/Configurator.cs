// Timberborn Utils
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using Bindito.Core;

namespace IgorZ.CustomTools.Tools;

[Context("Game")]
[Context("MapEditor")]
sealed class Configurator : IConfigurator {
  public void Configure(IContainerDefinition containerDefinition) {
    containerDefinition.Bind<DebugFinishNowTool>().AsSingleton();
    containerDefinition.Bind<PauseTool>().AsSingleton();
    containerDefinition.Bind<ResumeTool>().AsSingleton();
  }
}
