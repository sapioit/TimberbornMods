using IgorZ.CustomTools.Core;
using IgorZ.CustomTools.Tools;
using Timberborn.ConstructionMode;
using UnityDev.Utils.LogUtilsLite;
using UnityEngine.UIElements;

namespace IgorZ.CustomTools.Test;

class TestTool : AbstractCustomTool, IConstructionModeEnabler {
  protected TestToolSpec ToolData => _toolData ??= ToolSpec.GetSpec<TestToolSpec>();
  TestToolSpec _toolData;

  public override void Enter() {
    DebugEx.Warning("*** Enter on spec: {0}", ToolData);
  }
  public override void Exit() {
    DebugEx.Warning("*** Exit on spec: {0}", ToolData);
  }

  protected override void Initialize() {
    base.Initialize();
    DescriptionHintSection = "hint-hint-hint";
    DescriptionBullets = ["one string", "two string"];
    DescriptionExternalSections = [
        new Label("test label"),
    ];
    DescriptionVisualSections = [
        new Label("test label 2"),
    ];
  }
}