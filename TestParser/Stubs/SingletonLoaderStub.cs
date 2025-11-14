using System;
using System.Diagnostics.CodeAnalysis;
using Timberborn.Persistence;
using Timberborn.WorldPersistence;

namespace TestParser;

class SingletonLoaderStub : ISingletonLoader {
  public IObjectLoader GetSingleton(SingletonKey key) {
    throw new NotImplementedException();
  }
  public bool TryGetSingleton(SingletonKey key, [UnscopedRef] out IObjectLoader objectLoader) {
    throw new NotImplementedException();
  }
}
