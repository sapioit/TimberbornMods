// Timberborn Custom Tools
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using System.Linq;
using Timberborn.BlockSystem;

namespace IgorZ.CustomTools.Tools;

sealed class FourTemplatesBlockObjectTool
    : AbstractMultiTemplateBlockObjectTool<FourTemplatesBlockObjectTool.ModeType> {

  const string ShiftDescriptionHintLocKey = "IgorZ.CustomTools.FourTemplatesBlockObjectTool.ShiftDescriptionHint";
  const string CtrlDescriptionHintLocKey = "IgorZ.CustomTools.FourTemplatesBlockObjectTool.CtrlDescriptionHint";
  const string AltDescriptionHintLocKey = "IgorZ.CustomTools.FourTemplatesBlockObjectTool.AltDescriptionHint";

  public enum ModeType {
    NoModifier,
    ShiftModifier,
    CtrlModifier,
    AltModifier,
  }

  FourTemplatesToolSpec _fourTemplatesToolSpec;

  protected override void Initialize() {
    base.Initialize();
    _fourTemplatesToolSpec = ToolSpec.GetSpec<FourTemplatesToolSpec>();
    if (_fourTemplatesToolSpec == null) {
      throw new Exception($"FourTemplatesToolSpec not found on: {ToolSpec.Id}");
    }
    var bullets = new List<string>();
    if (_fourTemplatesToolSpec.ShiftModifierTemplate != null) {
      bullets.Add(
          Loc.T(ShiftDescriptionHintLocKey, GetTemplateDisplayName(GetTemplateForMode(ModeType.ShiftModifier))));
    }
    if (_fourTemplatesToolSpec.CtrlModifierTemplate != null) {
      bullets.Add(Loc.T(CtrlDescriptionHintLocKey, GetTemplateDisplayName(GetTemplateForMode(ModeType.CtrlModifier))));
    }
    if (_fourTemplatesToolSpec.AltModifierTemplate != null) {
      bullets.Add(Loc.T(AltDescriptionHintLocKey, GetTemplateDisplayName(GetTemplateForMode(ModeType.AltModifier))));
    }
    DescriptionBullets = DescriptionBullets == null ? bullets.ToArray() : bullets.Concat(DescriptionBullets).ToArray();
    CurrentMode = ModeType.NoModifier;
  }

  /// <inheritdoc/>
  protected override PlaceableBlockObjectSpec GetTemplateForMode(ModeType mode) {
    var templateName = mode switch {
        ModeType.ShiftModifier => _fourTemplatesToolSpec.ShiftModifierTemplate,
        ModeType.AltModifier => _fourTemplatesToolSpec.AltModifierTemplate,
        ModeType.CtrlModifier => _fourTemplatesToolSpec.CtrlModifierTemplate,
        ModeType.NoModifier => _fourTemplatesToolSpec.NoModifierTemplate,
        _ => throw new InvalidOperationException($"Unknown mode {mode}"),
    };
    if (templateName is null) {
      throw new InvalidOperationException($"No template set for mode: {mode}");
    }
    return _fourTemplatesToolSpec.FactionNeutral ? GetTemplateNoFaction(templateName) : GetTemplate(templateName);
  }

  /// <inheritdoc/>
  protected override ModeType GetCurrentMode() {
    if (IsShiftHeld && _fourTemplatesToolSpec.ShiftModifierTemplate != null) {
      return ModeType.ShiftModifier;
    }
    if (IsAltHeld && _fourTemplatesToolSpec.AltModifierTemplate != null) {
      return ModeType.AltModifier;
    }
    if (IsCtrlHeld && _fourTemplatesToolSpec.CtrlModifierTemplate != null) {
      return ModeType.CtrlModifier;
    }
    return ModeType.NoModifier;
  }
}
