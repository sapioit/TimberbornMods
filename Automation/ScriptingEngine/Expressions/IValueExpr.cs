// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;

namespace IgorZ.Automation.ScriptingEngine.Expressions;

interface IValueExpr : IExpression {
   public ScriptValue.TypeEnum ValueType { get; }
   public Func<ScriptValue> ValueFn { get; }
}
