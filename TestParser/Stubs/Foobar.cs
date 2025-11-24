using System.Collections.Generic;
using Timberborn.BaseComponentSystem;
using Timberborn.Common;

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
  public List<string> strList => ["one", "two"];
  public List<int> numList => [1, 2];
  public IEnumerable<int> numEnumerable => [1, 2];
  public ReadOnlyHashSet<int> numReadOnlyHashSet => new([1, 2]);  // Timberborn specific type.
}
