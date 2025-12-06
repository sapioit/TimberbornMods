using Timberborn.BlueprintSystem;

namespace IgorZ.CustomTools.Test;

public record TestToolSpec : ComponentSpec {
  [Serialize]
  public string TestData { get; init; }
}
