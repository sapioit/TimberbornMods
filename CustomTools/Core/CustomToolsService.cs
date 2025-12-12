// Timberborn Custom Tools
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System.Collections.Immutable;
using System.Linq;
using Bindito.Core;
using ConfigurableToolGroups.Services;
using Timberborn.BlueprintSystem;
using Timberborn.SingletonSystem;
using UnityDev.Utils.LogUtilsLite;

namespace IgorZ.CustomTools.Core;

sealed class CustomToolsService(ISpecService specService)
    : ILoadableSingleton {

  #region API

  public ImmutableArray<CustomToolGroupSpec> CustomGroupSpecs { get; private set; }
  public ImmutableArray<CustomToolSpec> CustomToolSpecs { get; private set; }

  #endregion

  #region ILoadableSingleton implementation

  public void Load() {
    CustomGroupSpecs = specService.GetSpecs<CustomToolGroupSpec>().ToImmutableArray();
    DebugEx.Info("Loaded {0} custom tool group specs", CustomGroupSpecs.Length);
    CustomToolSpecs = specService.GetSpecs<CustomToolSpec>().ToImmutableArray();
    DebugEx.Info("Loaded {0} custom tool specs", CustomGroupSpecs.Length);
  }

  #endregion
}
