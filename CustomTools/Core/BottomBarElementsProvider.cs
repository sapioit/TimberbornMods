// Timberborn Custom Tools
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System.Collections.Generic;
using System.Linq;
using Bindito.Core;
using IgorZ.CustomTools.Tools;
using IgorZ.TimberDev.Utils;
using Timberborn.AssetSystem;
using Timberborn.BottomBarSystem;
using Timberborn.TemplateCollectionSystem;
using Timberborn.ToolButtonSystem;
using Timberborn.ToolSystem;
using UnityDev.Utils.LogUtilsLite;
using UnityEngine;

namespace IgorZ.CustomTools.Core;

class BottomBarElementsProvider(
    ToolGroupButtonFactory toolGroupButtonFactory,
    ToolGroupService toolGroupService,
    ToolButtonFactory toolButtonFactory,
    ToolButtonService toolButtonService,
    IContainer container,
    IAssetLoader assetLoader,
    TemplateCollectionService templateCollectionService) : IBottomBarElementsProvider {

  public IEnumerable<BottomBarElement> GetElements() {
    // var blueprints = templateCollectionService.AllTemplates
    //     .Where(tmpl => tmpl.Specs.Any(x => x is CustomToolSpec))
    //     .OrderBy(blueprint => ((CustomToolSpec)blueprint.Specs.Single(x => x is CustomToolSpec)).Order);
    var blueprints = templateCollectionService.AllTemplates
        .Select(blueprint => blueprint.GetSpec<CustomToolSpec>())
        .Where(x => x != null)
        .OrderBy(spec => spec.Order);
    var newGroupButtons = new List<ToolGroupButton>();
    foreach (var spec in blueprints) {
      //FIXME
      DebugEx.Fine("Got template blueprint: {0}", spec.Blueprint.Name);
      foreach (var spec2 in spec.Blueprint.Specs) {
        DebugEx.Warning("*** blueprint spec: {0}", spec2);
      }

      if (GetOrCreateGroupButton(spec.GroupId, out var toolGroupButton)) {
        newGroupButtons.Add(toolGroupButton);
      }
      var toolType = ReflectionsHelper.GetType(spec.Type, typeof(AbstractCustomTool));
      var toolInstance = (AbstractCustomTool)container.GetInstance(toolType);
      toolInstance.InitializeTool(spec);
      var toolImage = assetLoader.Load<Sprite>(spec.Icon);
      var toolButton = toolButtonFactory.Create(toolInstance, toolImage, toolGroupButton.ToolButtonsElement);
      toolGroupService.AssignToGroup(toolGroupButton._toolGroup, toolButton.Tool);
      toolGroupButton.AddTool(toolButton);
    }
    foreach (var newGroupButton in newGroupButtons) {
      yield return BottomBarElement.CreateMultiLevel(newGroupButton.Root, newGroupButton.ToolButtonsElement);
    }
  }

  bool GetOrCreateGroupButton(string groupId, out ToolGroupButton toolGroupButton) {
    toolGroupButton = toolButtonService._toolGroupButtons.SingleOrDefault(x => x._toolGroup.Id == groupId);
    if (toolGroupButton != null) {
      return false;
    }
    toolGroupButton = toolGroupButtonFactory.CreateBlue(toolGroupService.GetGroup(groupId));
    return true;
  }
}
