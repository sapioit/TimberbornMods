using IgorZ.Automation.ScriptingEngine.Expressions;

namespace IgorZ.Automation.ScriptingEngine.Parser;

abstract class ParserBaseStub {
  protected ExpressionContext CurrentContext => new ExpressionContext();

  protected abstract IExpression ProcessString(string input);
  public abstract string Decompile(IExpression expression);
}
