// Timberborn Custom Tools
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bindito.Core;
using IgorZ.CustomTools.Tools;
using IgorZ.TimberDev.Utils;
using Timberborn.BlockObjectTools;
using Timberborn.BlueprintSystem;
using Timberborn.Common;
using Timberborn.SingletonSystem;
using Timberborn.ToolSystem;
using UnityDev.Utils.LogUtilsLite;

namespace IgorZ.CustomTools.Core;

/// <summary>Basic service for the custom tools functionality.</summary>
public sealed class CustomToolsService(
    ISpecService specService, IContainer container, ToolService toolService, ToolGroupService toolGroupService)
    : ILoadableSingleton, IPostLoadableSingleton {

  #region API

  /// <summary>All custom tool groups.</summary>
  public ImmutableArray<CustomToolGroupSpec> CustomGroupSpecs { get; private set; }

  /// <summary>All custom tools.</summary>
  public ImmutableArray<CustomToolSpec> CustomToolSpecs { get; private set; }

  /// <summary>Mapping of blockobject tools to the blueprint name that they place.</summary>
  public ImmutableDictionary<string, BlockObjectTool> BlockObjectTools { get; private set; }

  /// <summary>Activates the custom tool.</summary>
  public void SelectTool(AbstractCustomTool tool) {
    SelectTool(tool, tool.ToolSpec.GroupId);
  }

  /// <summary>Activates the generic game tool.</summary>
  /// <remarks>
  /// The relevant group will be looked up via <see cref="ToolGroupService"/>. If group association is not found, then
  /// the tool will be activated without activating teh group.
  /// </remarks>
  public void SelectTool(ITool tool) {
    SelectTool(tool, toolGroupService._assignedToolGroups.GetOrDefault(tool)?.Id);
  }

  /// <summary>Activates the generic game tool.</summary>
  /// <remarks>
  /// The group ID will be used to activate the relevant group in the bottom bar. It's not required to be the group that
  /// holds the tool. If group ID is not provided, then no group activation is done.
  /// </remarks>
  public void SelectTool(ITool tool, string groupId) {
    if (tool is BlockObjectTool blockObjectTool) {
      DebugEx.Info("Activating BlockObjectTool tool: tool={0}, groupId={1}",
                   blockObjectTool.Template.Blueprint.Name, groupId);
    } else {
      DebugEx.Info("Activating tool: tool={0}, groupId={1}", tool, groupId);
    }
    if (groupId != null) {
      var toolGroupSpec = toolGroupService.GetGroup(groupId);
      toolGroupService.EnterToolGroup(toolGroupSpec);
    }
    toolService.SwitchTool(tool);
  }

  #endregion

  #region ILoadableSingleton implementation

  static readonly string[] AllowedLayouts = ["left", "middle", "right"];

  /// <inheritdoc/>
  public void Load() {
    var hasLoadErrors = false;

    // Load and verify the group specs.
    CustomGroupSpecs = specService.GetSpecs<CustomToolGroupSpec>().ToImmutableArray();
    DebugEx.Info("Loaded {0} custom tool group specs", CustomGroupSpecs.Length);
    var groupIds = new HashSet<string>();
    foreach (var groupSpec in CustomGroupSpecs) {
      if (groupSpec.ParentGroupId == null
          && (groupSpec.Layout == null || !AllowedLayouts.Contains(groupSpec.Layout.ToLower()))) {
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

  #region IPostLoadableSingleton implementation

  /// <inheritdoc/>
  public void PostLoad() {
    var blockObjectTools = new Dictionary<string, BlockObjectTool>();
    foreach (var tool in toolGroupService._assignedToolGroups.Keys) {
      if (tool is BlockObjectTool blockObjectTool) {
        var blueprintName = blockObjectTool.Template.Blueprint.Name;
        if (!blockObjectTools.TryAdd(blueprintName, blockObjectTool)) {
          DebugEx.Warning("Duplicate blueprint name: {0}, blockObjectTool={1}", blueprintName, blockObjectTool);
        }
      }
    }
    BlockObjectTools = blockObjectTools.ToImmutableDictionary();
  }

  #endregion
}
