using System;
using Timberborn.BaseComponentSystem;

namespace IgorZ.Automation.AutomationSystem;

public class AutomationService {
  public static int CurrentTick => 0;
  public static bool AutomationSystemReady => false; 

  public void RegisterTickable(Action<int> tickable) {
    throw new NotImplementedException();
  }
  public void UnregisterTickable(Action<int> tickable) {
    throw new NotImplementedException();
  }
}
