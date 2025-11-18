using System;

namespace IgorZ.Automation.Settings;

public class ScriptEngineSettings {
  public static bool CheckArgumentValues => true;
  public static bool CheckOptionsArguments => true;
  public static int SignalExecutionStackSize => throw new NotImplementedException();
}
