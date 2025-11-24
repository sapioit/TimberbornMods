using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Timberborn.BaseComponentSystem;
using Timberborn.Common;
using UnityDev.Utils.LogUtilsLite;
using UnityEngine;

namespace TestParser.Stubs;

// ReSharper disable Unity.IncorrectMonoBehaviourInstantiation
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
// ReSharper disable InconsistentNaming

static class PatchStubs {
  static readonly string PatchId = typeof(PatchStubs).AssemblyQualifiedName;

  public static void Apply() {
    var harmony = new Harmony(PatchId);
    harmony.PatchAll();
  }
}

[HarmonyPatch(typeof(BaseComponent), nameof(BaseComponent.AllComponents), MethodType.Getter)]
static class BaseComponentPatch_AllComponents {
  static readonly List<Component> TestComponents = [new Foobar()];

  static bool Prefix(out ReadOnlyList<Component> __result) {
    __result = new ReadOnlyList<Component>(TestComponents);
    return false;
  }
}

[HarmonyPatch(typeof(UnityEngine.Object), "IsNativeObjectAlive")]
static class UnityObject_IsNativeObjectAlive {
  static bool Prefix(out bool __result) {
    __result = true;
    return false;
  }
}
