namespace IgorZ.Automation.Settings;

public class ScriptEditorSettings {
  public enum ScriptSyntax {
    Python,
    Lisp,
  }

  public static ScriptSyntax DefaultScriptSyntax { get; private set; } = ScriptSyntax.Python;
}
