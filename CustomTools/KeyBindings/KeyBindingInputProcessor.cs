// Timberborn Custom Tools
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using System.Linq;
using IgorZ.CustomTools.Core;
using IgorZ.CustomTools.Tools;
using IgorZ.TimberDev.Utils;
using Timberborn.InputSystem;
using Timberborn.KeyBindingSystem;
using Timberborn.SingletonSystem;
using Timberborn.ToolSystem;
using UnityDev.Utils.LogUtilsLite;

namespace IgorZ.CustomTools.KeyBindings;

/// <summary>The main controller class for the keybidings halding.</summary>
/// <remarks>
/// The only usage of this class for the client code would be consuming events via <see cref="ConsumeKeyBinding"/>.
/// </remarks>
public class KeyBindingInputProcessor(
    CustomToolsService customToolsService, EventBus eventBus, InputService inputService)
    : IPostLoadableSingleton, IInputProcessor {

  /// <summary>Specifies that the binding is consumed and the further bindings check should be stopped.</summary>
  /// <remarks>
  /// Only one ID can be consumed during the same tick. Noramlly, this methid is called from the listeners of
  /// <see cref="CustomToolKeyBindingEvent"/>.
  /// </remarks>
  /// <param name="keyBindingId"></param>
  public void ConsumeKeyBinding(string keyBindingId) {
    _blockOnBindingId = keyBindingId;
  }

  #region IPostLoadableSingleton implementation

  /// <inheritdoc/>
  public void PostLoad() {
    // Need to be called after all the other processors to be able to block the modifiers.
    PressedKeyBindings.Clear();
    _blockOnBindingId = null;
    inputService.AddInputProcessor(this);
  }

  #endregion

  #region IInputProcessor implementation

  /// <inheritdoc/>
  public bool ProcessInput() {
    if (PressedKeyBindings.Count > 0) {
      for (var i = PressedKeyBindings.Count - 1; i >= 0; i--) {
        // Execute the first matched binding and ignore all others. They will be blocked anyway.
        if (ProcessBindingPress(PressedKeyBindings[i])) {
          break;
        }
      }
      PressedKeyBindings.Clear();
    }
    if (_blockOnBindingId == null) {
      return false;
    }
    if (inputService.IsKeyDown(_blockOnBindingId)
        || inputService.IsKeyHeld(_blockOnBindingId) || inputService.IsKeyLongHeld(_blockOnBindingId)) {
      return true;
    }
    _blockOnBindingId = null;
    return false;
  }

  #endregion

  #region Implementation

  internal static readonly List<KeyBinding> PressedKeyBindings = [];
  string _blockOnBindingId;

  bool ProcessBindingPress(KeyBinding pressedKeyBinding) {
    var bindingSpec = pressedKeyBinding._keyBindingDefinition.KeyBindingSpec;
    var customToolSpec = bindingSpec.GetSpec<CustomToolSpec>();
    if (customToolSpec != null) {
      var customToolInstance = (AbstractCustomTool)StaticBindings.DependencyContainer.GetInstance(
          ReflectionsHelper.GetType(customToolSpec.Type, typeof(AbstractCustomTool)));
      customToolsService.SelectTool(customToolInstance);
      _blockOnBindingId = pressedKeyBinding.Id;
      return true;
    }

    var customToolBindingSpec = bindingSpec.GetSpec<CustomToolBindingSpec>();
    if (customToolBindingSpec == null) {
      return false;
    }
    _blockOnBindingId = pressedKeyBinding.Id;
    ITool toolToSelect = null;
    if (customToolBindingSpec.Type != null) {
      toolToSelect = (ITool)StaticBindings.DependencyContainer.GetInstance(
          ReflectionsHelper.GetType(customToolBindingSpec.Type, typeof(ITool), needDefaultConstructor: false));
    } else if (customToolBindingSpec.BlockObjectBlueprint != null) {
      if (!customToolsService.BlockObjectTools.TryGetValue(
              customToolBindingSpec.BlockObjectBlueprint, out var blockObjectTool)) {
        var allBlueprints = customToolsService.BlockObjectTools.Keys.OrderBy(x => x);
        DebugEx.Warning("All known BlockObjects blueprints:\n{0}", DebugEx.C2S(allBlueprints, separator: "\n"));
        throw new InvalidOperationException($"Cannot find tool for blueprint: {customToolBindingSpec.BlockObjectBlueprint}");
      }
      toolToSelect = blockObjectTool;
    }
    if (toolToSelect != null) {
      customToolsService.SelectTool(toolToSelect);
    } else {
      var bindingEvent = new CustomToolKeyBindingEvent { KeyBinding = pressedKeyBinding };
      eventBus.Post(bindingEvent);
    }
    return true;
  }

  #endregion
}
