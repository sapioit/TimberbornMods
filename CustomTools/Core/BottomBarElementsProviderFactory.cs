// Timberborn Custom Tools
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using System.Linq;
using Bindito.Core;
using IgorZ.CustomTools.Tools;
using IgorZ.TimberDev.Utils;
using Timberborn.AssetSystem;
using Timberborn.BottomBarSystem;
using Timberborn.SingletonSystem;
using Timberborn.TemplateCollectionSystem;
using Timberborn.ToolButtonSystem;
using Timberborn.ToolSystem;
using UnityDev.Utils.LogUtilsLite;
using UnityEngine;

namespace IgorZ.CustomTools.Core;

sealed class BottomBarElementsProviderFactory(
    ToolGroupButtonFactory toolGroupButtonFactory,
    ToolGroupService toolGroupService,
    ToolButtonFactory toolButtonFactory,
    ToolButtonService toolButtonService,
    IContainer container,
    IAssetLoader assetLoader,
    TemplateCollectionService templateCollectionService) : ILoadableSingleton {

  const string RedClass = "bottom-bar-button--red";

  #region ILoadableSingleton implementation

  public void Load() {
    var customToolSpecs = templateCollectionService.AllTemplates
        .Select(blueprint => blueprint.GetSpec<CustomToolSpec>())
        .Where(x => x != null).OrderBy(spec => spec.Order)
        .ToList();
    DebugEx.Info("Loaded {0} custom tool blueprints", customToolSpecs.Count());
    var newGroupButtons = new List<ToolGroupButton>();
    foreach (var customToolSpec in customToolSpecs) {
      if (GetOrCreateGroupButton(customToolSpec.GroupId, out var toolGroupButton)) {
        newGroupButtons.Add(toolGroupButton);
      }
      var toolType = ReflectionsHelper.GetType(customToolSpec.Type, typeof(AbstractCustomTool));
      var toolInstance = (AbstractCustomTool)container.GetInstance(toolType);
      toolInstance.InitializeTool(customToolSpec);
      var toolImage = assetLoader.Load<Sprite>(customToolSpec.Icon);
      var toolButton = toolButtonFactory.Create(toolInstance, toolImage, toolGroupButton.ToolButtonsElement);
      toolGroupService.AssignToGroup(toolGroupButton._toolGroup, toolButton.Tool);
      toolGroupButton.AddTool(toolButton);
      DebugEx.Info("Created tool '{0}' in group '{1}'", customToolSpec.Blueprint.Name, customToolSpec.GroupId);
    }
    _newGroupButtons = newGroupButtons.OrderBy(x => x._toolGroup.GetSpec<CustomToolGroupSpec>().Order).ToList();
  }

  #endregion

  #region API

  public void SetProviders(BottomBarModule.Builder builder) {
    builder.AddLeftSectionElement(new BottomBarElementsProvider(() => {
      return _newGroupButtons.Where(x => x._toolGroup.GetSpec<CustomToolGroupSpec>().Layout.ToLower() == "left");
    }), 1000);
    builder.AddMiddleSectionElements(new BottomBarElementsProvider(() => {
      return _newGroupButtons.Where(x => x._toolGroup.GetSpec<CustomToolGroupSpec>().Layout.ToLower() == "middle");
    }));
    builder.AddRightSectionElement(new BottomBarElementsProvider(() => {
      return _newGroupButtons.Where(x => x._toolGroup.GetSpec<CustomToolGroupSpec>().Layout.ToLower() == "right");
    }));
  }

  #endregion

  #region Implementation

  List<ToolGroupButton> _newGroupButtons;

  record BottomBarElementsProvider(Func<IEnumerable<ToolGroupButton>> GetButtonsFn) : IBottomBarElementsProvider {
    public IEnumerable<BottomBarElement> GetElements() {
      return GetButtonsFn().Select(x => BottomBarElement.CreateMultiLevel(x.Root, x.ToolButtonsElement));
    }
  }

  bool GetOrCreateGroupButton(string groupId, out ToolGroupButton toolGroupButton) {
    toolGroupButton = toolButtonService._toolGroupButtons.SingleOrDefault(x => x._toolGroup.Id == groupId);
    if (toolGroupButton != null) {
      return false;
    }
    var groupSpec = toolGroupService.GetGroup(groupId);
    var customGroupSpec = groupSpec.GetSpec<CustomToolGroupSpec>();
    if (customGroupSpec == null) {
      throw new InvalidOperationException($"Can't find custom tools group spec with id: {groupId}");
    }
    if (customGroupSpec.Layout.ToLower() is not "left" and not "middle" and not "right") {
      throw new InvalidOperationException($"Unknown layout in group '{groupId}': {customGroupSpec.Layout}");
    }
    var style = customGroupSpec.Style.ToLower();
    toolGroupButton = style switch {
        "blue" => toolGroupButtonFactory.CreateBlue(groupSpec),
        "green" => toolGroupButtonFactory.CreateGreen(groupSpec),
        "red" => toolGroupButtonFactory.Create(groupSpec, RedClass),
        _ => throw new InvalidOperationException($"Unknown group style name in group '{groupId}': {style}"),
    };
    DebugEx.Info("Created custom tool group: {0}", groupSpec.Id);
    return true;
  }

  #endregion
}
