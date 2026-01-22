// Timberborn Mod: Automation
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;

namespace IgorZ.Automation.ScriptingEngineUI;

sealed class CopyRuleButtonProvider : IEditorButtonProvider {

  const string CopyRuleBtnBtnLocKey = "IgorZ.Automation.Scripting.Editor.CopyRuleBtn";

  /// <inheritdoc/>
  public string CreateRuleBtnLocKey => null;

  /// <inheritdoc/>
  public string RuleRowBtnLocKey => CopyRuleBtnBtnLocKey;

  /// <inheritdoc/>
  public void OnRuleRowBtnAction(RuleRow ruleRow) {
    var newRow = CreateRuleAfterRowFn(ruleRow);
    newRow.ConditionExpression = ruleRow.ConditionExpression;
    newRow.ActionExpression = ruleRow.ActionExpression;
    newRow.SwitchToViewMode();
  }

  /// <inheritdoc/>
  public bool IsRuleRowBtnEnabled(RuleRow ruleRow) {
    return ruleRow.ConditionExpression != null && ruleRow.ActionExpression != null;
  }

  public Func<RuleRow, RuleRow> CreateRuleAfterRowFn;
}
