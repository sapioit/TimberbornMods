// Timberborn Mod: SmartPower
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using Bindito.Core;
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

  WalkerPoweredGeneratorSettings _settings;
  BlockableObject _blockableObject;

  [Inject]
  public void InjectDependencies(WalkerPoweredGeneratorSettings settings) {
    _settings = settings;
  }

  public override void Awake() {
    ShowFloatingIcon = _settings.ShowFloatingIcon.Value;
    SuspendDelayedAction = SmartPowerService.GetTimeDelayedAction(_settings.SuspendDelayMinutes.Value);
    ResumeDelayedAction = SmartPowerService.GetTimeDelayedAction(_settings.ResumeDelayMinutes.Value);
    base.Awake();

    _blockableObject = GetComponent<BlockableObject>();
  }

  #endregion
}
