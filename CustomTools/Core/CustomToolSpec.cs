// Timberborn Custom Tools
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using Timberborn.BlueprintSystem;

namespace IgorZ.CustomTools.Core;

public record CustomToolSpec : ComponentSpec {
  [Serialize]
  public string GroupId { get; init; }
  [Serialize]
  public string Type { get; init; }
  [Serialize]
  public int Order { get; init; }
  [Serialize]
  public string Icon { get; init; }
  [Serialize]
  public string DisplayNameLocKey { get; init; }
  [Serialize]
  public string DescriptionLocKey { get; init; }
}
