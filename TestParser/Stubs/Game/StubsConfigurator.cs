using Bindito.Core;
using Timberborn.Localization;
using Timberborn.SingletonSystem;
using Timberborn.WorldPersistence;

namespace TestParser.Stubs.Game;

class StubsConfigurator : IConfigurator {
  public void Configure(IContainerDefinition containerDefinition) {
    containerDefinition.Bind<ILoc>().To<LocStub>().AsSingleton();
    containerDefinition.Bind<EventBus>().AsSingleton();
    containerDefinition.Bind<ISingletonLoader>().To<SingletonLoaderStub>().AsSingleton();
  }
}
