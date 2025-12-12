// Timberborn Custom Tools
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using System.Linq;
using Bindito.Core;
using ConfigurableToolGroups.Services;
using ConfigurableToolGroups.UI;
using IgorZ.CustomTools.Tools;
using IgorZ.TimberDev.Utils;
using Timberborn.BottomBarSystem;
using Timberborn.ToolButtonSystem;
using Timberborn.ToolSystem;
using UnityDev.Utils.LogUtilsLite;
using UnityEngine.UIElements;

namespace IgorZ.CustomTools.Core;

abstract class AbstractLayoutElement(
    CustomToolsService customToolsService, ModdableToolGroupButtonFactory groupButtonFactory)
    : CustomBottomBarElement {

  const string RedClass = "bottom-bar-button--red";

  #region CustomBottomBarElement implementation

  /// <inheritdoc/>
  public override IEnumerable<BottomBarElement> GetElements() {
    return customToolsService.CustomGroupSpecs
        .Where(x => string.Equals(x.Layout, Layout, StringComparison.CurrentCultureIgnoreCase))
        .Select(customGroupSpec => CreateGroup(customGroupSpec, null).ToBottomBarElement());
  }

  #endregion

  #region Implementation

  protected abstract string Layout { get; }

  static readonly string[] AllowedLayouts = ["left", "middle", "right"];
  IContainer _container;

  [Inject]
  public void InjectDependencies(IContainer container) {
    _container = container;
  }

  ModdableToolGroupButton CreateGroup(CustomToolGroupSpec customGroupSpec, ModdableToolGroupButton parent) {
    var toolGroupSpec = customGroupSpec.GetSpec<ToolGroupSpec>();
    if (toolGroupSpec == null) {
      throw new InvalidOperationException($"Missing ToolGroupSpec on custom group: {customGroupSpec}");
    }
    if (!AllowedLayouts.Contains(customGroupSpec.Layout.ToLower())) {
      throw new InvalidOperationException($"Unknown layout: {customGroupSpec.Layout}");
    }
    var groupId = toolGroupSpec.Id;
    ToolButtonColor? toolColor = customGroupSpec.Style.ToLower() switch {
        "blue" =>  ToolButtonColor.Blue,
        "green" => ToolButtonColor.Green,
        "red" => null,
        _ => throw new InvalidOperationException($"Unknown tool group style: {customGroupSpec.Style}"),
    };
    var groupButton = groupButtonFactory.Create(toolGroupSpec, parent, toolColor ?? ToolButtonColor.Blue);
    // FIXME: One day, have it handled by the Moddable Groups.
    if (!toolColor.HasValue) {
      var buttonWrapper = groupButton.Root.Q<VisualElement>("ToolGroupButtonWrapper");
      if (buttonWrapper != null) {
        buttonWrapper.RemoveFromClassList(ToolGroupButtonFactory.BlueClass);
        buttonWrapper.AddToClassList(RedClass);
      } else {
        DebugEx.Warning("Cannot adjust style to RED on group button {0}", groupId);
      }
    }
    var childrenToolSpecs = customToolsService.CustomToolSpecs
        .Where(x => x.GroupId == toolGroupSpec.Id).ToList();
    DebugEx.Info("Created custom group: {0}, childrenTools={1}", toolGroupSpec.Id, childrenToolSpecs.Count);

    foreach (var customToolSpec in childrenToolSpecs) {
      var toolType = ReflectionsHelper.GetType(customToolSpec.Type, typeof(AbstractCustomTool));
      var toolInstance = (AbstractCustomTool)_container.GetInstance(toolType);
      toolInstance.InitializeTool(customToolSpec);
      DebugEx.Info("Created tool '{0}' for group '{1}", toolType, toolGroupSpec.Id);
      groupButton.AddChildTool(toolInstance, toolInstance.ToolSpec.Icon);
    }

    return groupButton;
  }

  #endregion
}
