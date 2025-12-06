// Timberborn Custom Tools
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using Timberborn.BlueprintSystem;

namespace IgorZ.CustomTools.Core;

public record CustomToolGroupSpec : ComponentSpec {
  /// <summary>Color style of the group.</summary>
  /// <remarks>Values: "red", "blue", "green". Case-insensitive.</remarks>
  [Serialize]
  public string Style { get; init; }

  /// <summary>The tool order in the tools group. It is defined in the specification.</summary>
  [Serialize]
  public int Order { get; init; }

  /// <summary>The tool's group position in the bottom bar.</summary>
  /// <remarks>Values: "left", "middle", "right"</remarks>
  [Serialize]
  public string Layout { get; init; }
}
