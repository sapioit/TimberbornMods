// Timberborn Custom Tools
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System.Collections.Generic;
using Bindito.Core;
using Timberborn.BottomBarSystem;
using Timberborn.TemplateCollectionSystem;
using UnityDev.Utils.LogUtilsLite;

// ReSharper disable once CheckNamespace
namespace IgorZ.CustomTools.Core;

[Context("Game")]
class Configurator : IConfigurator {

  class BottomBarModuleProvider : IProvider<BottomBarModule> {
    readonly BottomBarElementsProvider _bottomBarElementsProvider;

    public BottomBarModuleProvider(BottomBarElementsProvider bottomBarElementsProvider) {
      _bottomBarElementsProvider = bottomBarElementsProvider;
    }

    public BottomBarModule Get() {
      //FIXME: handle layout settings
      BottomBarModule.Builder builder = new BottomBarModule.Builder();
      //builder.AddLeftSectionElement(_timberDevButtonProvider, 60);
      builder.AddMiddleSectionElements(_bottomBarElementsProvider);
      return builder.Build();
    }
  }

  public void Configure(IContainerDefinition containerDefinition) {
    containerDefinition.Bind<BottomBarElementsProvider>().AsSingleton();
    containerDefinition.MultiBind<BottomBarModule>().ToProvider<BottomBarModuleProvider>().AsSingleton();
    containerDefinition.MultiBind<ITemplateCollectionIdProvider>().To<CommonTemplateCollectionIdProvider>().AsSingleton();
  }
}

class CommonTemplateCollectionIdProvider : ITemplateCollectionIdProvider {
  const string CollectionId = "BottomBar.CustomTools";

  public IEnumerable<string> GetTemplateCollectionIds() {
    //FIXME: check if really needed
    DebugEx.Warning("*** called PROVIDER");
    yield return CollectionId;
  }
}