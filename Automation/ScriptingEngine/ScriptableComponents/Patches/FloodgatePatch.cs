// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using HarmonyLib;
using IgorZ.Automation.AutomationSystem;
using Timberborn.WaterBuildings;
using HeightChangeTracker = IgorZ.Automation.ScriptingEngine.ScriptableComponents.Components.FloodgateScriptableComponent.HeightChangeTracker;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming

namespace IgorZ.Automation.ScriptingEngine.ScriptableComponents.Patches;

[HarmonyPatch(typeof(Floodgate), nameof(Floodgate.Height), MethodType.Setter)]
static class FloodgatePatch {
  static void Postfix(bool __runOriginal, Floodgate __instance) {
    if (!__runOriginal) {
      return;  // The other patches must follow the same style to properly support the skip logic!
    }
    var automationBehavior = __instance.GetComponent<AutomationBehavior>();
    if (!automationBehavior) {
      return;
    }
    if (automationBehavior.TryGetDynamicComponent<HeightChangeTracker>(out var automationTracker)) {
      automationTracker.OnHeighChanged();
    }
  }
}
