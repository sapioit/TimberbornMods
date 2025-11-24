using System;
using Timberborn.Persistence;
using Timberborn.WorldPersistence;

namespace TestParser.Stubs.Game;

class SingletonLoaderStub : ISingletonLoader {
  public IObjectLoader GetSingleton(SingletonKey key) {
    throw new NotImplementedException();
  }
  public bool TryGetSingleton(SingletonKey key, out IObjectLoader objectLoader) {
    throw new NotImplementedException();
  }
}
