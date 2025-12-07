// Timberborn Custom Tools
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using HarmonyLib;
using IgorZ.CustomTools.Tools;
using Timberborn.BlockObjectToolsUI;
using Timberborn.ToolSystem;

// ReSharper disable UnusedType.Global
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local

namespace IgorZ.CustomTools.Patches;

[HarmonyPatch(typeof(BlockObjectToolWarningPanel))]
public static class BlockObjectToolWarningPanelPatch {
  [HarmonyPostfix]
  [HarmonyPatch(nameof(BlockObjectToolWarningPanel.UpdateSingleton))]
  static void ShowCustomToolWarning(BlockObjectToolWarningPanel __instance, ToolService ____toolService) {
    if (____toolService.ActiveTool is AbstractCustomTool activeTool) {
      __instance.UpdateText(activeTool.GetWarningText());
    }
  }
}
