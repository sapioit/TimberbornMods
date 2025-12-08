// Timberborn Mod: SmartPower
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using IgorZ.SmartPower.Core;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockingSystem;
using Timberborn.EnterableSystem;
using Timberborn.MechanicalSystem;

namespace IgorZ.SmartPower.PowerConsumers;

/// <summary>
/// Component that extends the <see cref="MechanicalBuilding"/> behavior to conserve energy when powered attraction has
/// no attendees.
/// </summary>
public sealed class SmartPoweredAttraction : BaseComponent, IAwakableComponent, IAdjustablePowerInput {

  #region IAdjustablePowerInput implementation

  // Don't set it to 0 as it may disable the network.
  const int NoAttendeesPowerConsumption = 1;  // hp

  /// <inheritdoc/>
  public int UpdateAndGetPowerInput() {
    if (_mechanicalBuilding.ConsumptionDisabled || _blockableObject && !_blockableObject.IsUnblocked) {
      if (_powerInputLimiter) {
        _powerInputLimiter.SetDesiredPower(-1);
      }
      return 0;
    }
    var newInput = _enterable.NumberOfEnterersInside == 0 ? NoAttendeesPowerConsumption : _nominalPowerInput;
    if (_powerInputLimiter) {
      _powerInputLimiter.SetDesiredPower(newInput);
    }
    return newInput;
  }

  #endregion

  #region Implementation

  MechanicalBuilding _mechanicalBuilding;
  BlockableObject _blockableObject;
  Enterable _enterable;
  PowerInputLimiter _powerInputLimiter;

  int _nominalPowerInput;

  /// <inheritdoc/>
  public void Awake() {
    _mechanicalBuilding = GetComponent<MechanicalBuilding>();
    _blockableObject = GetComponent<BlockableObject>();
    _enterable = GetComponent<Enterable>();
    _powerInputLimiter = GetComponent<PowerInputLimiter>();
    _nominalPowerInput = GetComponent<MechanicalNodeSpec>().PowerInput;
  }

  #endregion
}
