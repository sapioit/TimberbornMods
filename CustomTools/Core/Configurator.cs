// Timberborn Custom Tools
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System.Collections.Generic;
using Bindito.Core;
using Timberborn.BottomBarSystem;
using Timberborn.TemplateCollectionSystem;

// ReSharper disable once CheckNamespace
namespace IgorZ.CustomTools.Core;

[Context("Game")]
[Context("MapEditor")]
class Configurator : IConfigurator {

  class BottomBarModuleProvider(BottomBarElementsProviderFactory bottomBarElementsProviderFactory)
      : IProvider<BottomBarModule> {
    public BottomBarModule Get() {
      var builder = new BottomBarModule.Builder();
      bottomBarElementsProviderFactory.SetProviders(builder);
      return builder.Build();
    }
  }

  class CommonTemplateCollectionIdProvider : ITemplateCollectionIdProvider {
    const string CollectionId = "BottomBar.CustomTools";

    public IEnumerable<string> GetTemplateCollectionIds() {
      yield return CollectionId;
    }
  }

  public void Configure(IContainerDefinition containerDefinition) {
    containerDefinition.Bind<BottomBarElementsProviderFactory>().AsSingleton();
    containerDefinition.MultiBind<BottomBarModule>().ToProvider<BottomBarModuleProvider>().AsSingleton();
    containerDefinition.MultiBind<ITemplateCollectionIdProvider>()
        .To<CommonTemplateCollectionIdProvider>().AsSingleton();
  }
}
