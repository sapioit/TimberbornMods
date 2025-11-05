// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;

namespace IgorZ.Automation.ScriptingEngine.Expressions;

interface IExpression {
  /// <summary>Visits all nodes in the expression tree and applies the visitor function to each node.</summary>
  public void VisitNodes(Action<IExpression> visitorFn);
}
