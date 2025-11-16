using System.Collections.Generic;
using Timberborn.BaseComponentSystem;

namespace TestParser.Stubs;

sealed class Foobar : BaseComponent {
  public override string ToString() {
    return "Foobar"; 
  }

  public int numInt => 123;
  public float numFloat => 123.33f;
  public bool boolFalse => false;
  public bool boolTrue => true;
  public string str => "test";
  public IList<string> strList => ["one", "two"];
  public IList<int> numList => [1, 2];
}
