// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using IgorZ.Automation.ScriptingEngine.Parser;

namespace IgorZ.Automation.ScriptingEngine.Expressions;

abstract class BoolOperator(string name, IList<IExpression> operands) : AbstractOperator(name, operands) {
  public Func<bool> Execute { get; protected init; }
}
