// Timberborn Custom Tools
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using HarmonyLib;
using Timberborn.ModManagerScene;

// ReSharper disable UnusedType.Global

namespace IgorZ.CustomTools.Patches;

sealed class PatchStarter : IModStarter {
  public void StartMod(IModEnvironment modEnvironment) {
    new Harmony("IgorZ.CustomTools").PatchAll();
  }
}
