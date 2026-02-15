// Timberborn Custom Tools
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using IgorZ.CustomTools.Tools;
using Timberborn.InputSystem;
using Timberborn.SingletonSystem;
using Timberborn.ToolSystem;
using UnityDev.Utils.LogUtilsLite;

namespace IgorZ.CustomTools.KeyBindings;

class KeyBindingInputProcessor(
    DebugFinishNowTool debugFinishNowTool, PauseTool pauseTool, ResumeTool resumeTool,
    ToolService toolService, ToolGroupService toolGroupService, InputService inputService)
    : IPostLoadableSingleton, IInputProcessor {

  const string PauseToolKeyBindingId = "IgorZ-CustomTools-PauseTool";
  const string ResumeToolKeyBindingId = "IgorZ-CustomTools-ResumeTool";
  const string DebugFinishAllToolKeyBindingId = "IgorZ-CustomTools-DebugFinishNowTool";

  public void PostLoad() {
    // Need to eb called after all other processors to be able to block the modifiers.
    inputService.AddInputProcessor(this);
  }

  public bool ProcessInput() {
    return TrySelectingTool(PauseToolKeyBindingId, pauseTool.ToolSpec.GroupId, pauseTool)
        || TrySelectingTool(ResumeToolKeyBindingId, resumeTool.ToolSpec.GroupId, resumeTool)
        || TrySelectingTool(DebugFinishAllToolKeyBindingId, debugFinishNowTool.ToolSpec.GroupId, debugFinishNowTool);
  }

  bool TrySelectingTool(string bindingId, string groupId, ITool tool) {
    return HandleBindingPressed(bindingId, () => {
      DebugEx.Fine("Activating tool: groupId={0}, tool={1}", groupId, tool);
      var toolGroupSpec = toolGroupService.GetGroup(groupId);
      toolGroupService.EnterToolGroup(toolGroupSpec);
      toolService.SwitchTool(tool);
    });
  }

  bool HandleBindingPressed(string bindingId, Action actionFn) {
    if (inputService.IsKeyDown(bindingId)) {
      actionFn();
      return true;
    }
    // FIXME: It's a workaround. Consume the "similar" bindings to halt the bindings without the modifiers.
    if (inputService.IsKeyHeld(bindingId) || inputService.IsKeyLongHeld(bindingId)) {
      return true;
    }
    return false;
  }
}
