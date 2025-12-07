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

  /// <summary>The tool group button order in case of there are multiple groups in the layout.</summary>
  /// <remarks>
  /// Used to order group buttons in case of there are multiple in the same layout. Duplicates are allowed. The custom
  /// group buttons are always added at the end of the stock buttons list.
  /// </remarks>
  [Serialize]
  public int Order { get; init; }

  /// <summary>The tool's group position in the bottom bar.</summary>
  /// <remarks>Values: "left", "middle", "right"</remarks>
  [Serialize]
  public string Layout { get; init; }
}
