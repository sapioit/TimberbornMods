// Timberborn Mod: SmartPower
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using IgorZ.SmartPower.Settings;
using Timberborn.BlockingSystem;

namespace IgorZ.SmartPower.PowerGenerators;

sealed class SmartWalkerPoweredGenerator : PowerOutputBalancer {

  #region PowerOutputBalancer overrides

  /// <inheritdoc/>
  protected override void Suspend() {
    base.Suspend();
    _blockableObject.Block(this);
  }

  /// <inheritdoc/>
  protected override void Resume() {
    base.Resume();
    _blockableObject.Unblock(this);
  }

  #endregion

  #region Implementation

  BlockableObject _blockableObject;

  public override void Awake() {
    ShowFloatingIcon = WalkerPoweredGeneratorSettings.ShowFloatingIcon;
    SuspendDelayedAction = SmartPowerService.GetTimeDelayedAction(WalkerPoweredGeneratorSettings.SuspendDelayMinutes);
    ResumeDelayedAction = SmartPowerService.GetTimeDelayedAction(WalkerPoweredGeneratorSettings.ResumeDelayMinutes);
    base.Awake();

    _blockableObject = GetComponent<BlockableObject>();
  }

  #endregion
}
