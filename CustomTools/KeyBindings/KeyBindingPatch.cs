// Timberborn Custom Tools
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using HarmonyLib;
using IgorZ.CustomTools.Core;
using Timberborn.KeyBindingSystem;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local
namespace IgorZ.CustomTools.KeyBindings;

[HarmonyPatch(typeof(KeyBinding), nameof(KeyBinding.IsDown), MethodType.Setter)]
static class KeyBindingPatch {
  static void Prefix(bool __runOriginal, KeyBinding __instance, bool value) {
    if (!__runOriginal) {
      return;  // The other patches must follow the same style to properly support the skip logic!
    }
    if (value && !__instance.IsDown) {
      KeyBindingInputProcessor.PressedKeyBindings.Add(__instance);
    }
  }
}

