// Timberborn Custom Tools
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bindito.Core;
using ConfigurableToolGroups.Services;
using IgorZ.CustomTools.Tools;
using IgorZ.TimberDev.Utils;
using Timberborn.BlueprintSystem;
using Timberborn.SingletonSystem;
using Timberborn.ToolSystem;
using UnityDev.Utils.LogUtilsLite;

namespace IgorZ.CustomTools.Core;

sealed class CustomToolsService(ISpecService specService, IContainer container)
    : ILoadableSingleton {

  #region API

  public ImmutableArray<CustomToolGroupSpec> CustomGroupSpecs { get; private set; }
  public ImmutableArray<CustomToolSpec> CustomToolSpecs { get; private set; }

  #endregion

  #region ILoadableSingleton implementation

  static readonly string[] AllowedLayouts = ["left", "middle", "right"];

  public void Load() {
    var hasLoadErrors = false;

    // Load and verify the group specs.
    CustomGroupSpecs = specService.GetSpecs<CustomToolGroupSpec>().ToImmutableArray();
    DebugEx.Info("Loaded {0} custom tool group specs", CustomGroupSpecs.Length);
    var groupIds = new HashSet<string>();
    foreach (var groupSpec in CustomGroupSpecs) {
      if (groupSpec.ParentGroupId == null && !AllowedLayouts.Contains(groupSpec.Layout.ToLower())) {
        DebugEx.Error("Group spec has illegal layout: {0}", groupSpec);
        hasLoadErrors = true;
      }
      if (groupSpec.ParentGroupId != null && groupSpec.Layout != null) {
        DebugEx.Warning("Layout specification ignored in group spec: {0}", groupSpec);
      }
      var toolGroupSpec = groupSpec.GetSpec<ToolGroupSpec>();
      if (toolGroupSpec == null) {
        DebugEx.Error("Custom group blueprint has no ToolGroupSpec: {0}", groupSpec.Blueprint.Name);
        hasLoadErrors = true;
        continue;
      }
      groupIds.Add(toolGroupSpec.Id);
    }
    var unknownGroupIdSpecs = CustomGroupSpecs
        .Where(groupSpec => groupSpec.ParentGroupId != null && !groupIds.Contains(groupSpec.ParentGroupId));
    foreach (var groupIdRef in unknownGroupIdSpecs) {
      DebugEx.Error("Unknown group ID in group spec: {0}", groupIdRef);
      hasLoadErrors = true;
    }

    // Load and verify the tool specs.
    CustomToolSpecs = specService.GetSpecs<CustomToolSpec>().ToImmutableArray();
    DebugEx.Info("Loaded {0} custom tool specs", CustomGroupSpecs.Length);
    foreach (var toolSpec in CustomToolSpecs) {
      if (toolSpec.GroupId == null) {
        DebugEx.Error("Custom tool spec doesn't have group ID: {0}", toolSpec);
        hasLoadErrors = true;
        continue;
      }
      if (!groupIds.Contains(toolSpec.GroupId)) {
        DebugEx.Error("Custom tool spec specifies unknown group ID: {0}", toolSpec);
        hasLoadErrors = true;
      }
      var toolType = ReflectionsHelper.GetType(toolSpec.Type, typeof(AbstractCustomTool), throwOnError: false);
      if (toolType == null) {
        DebugEx.Error("Custom tool spec requests unknown type: {0}", toolSpec);
        hasLoadErrors = true;
        continue;
      }
      var toolInstance = (AbstractCustomTool)container.GetInstance(toolType);
      if (toolInstance == null) {
        DebugEx.Error("Custom tool spec type is not instantiable (forgot Bind?): {0}", toolSpec);
        hasLoadErrors = true;
      }
    }

    // Don't go further if there are critical errors.
    if (hasLoadErrors) {
      throw new InvalidOperationException("Some CustomTools specs cannot be loaded!");
    }
  }

  #endregion
}
