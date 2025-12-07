// Timberborn Custom Tools
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System.Text;
using Bindito.Core;
using IgorZ.CustomTools.Core;
using Timberborn.CoreUI;
using Timberborn.Localization;
using Timberborn.ToolSystem;
using Timberborn.ToolSystemUI;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global

namespace IgorZ.CustomTools.Tools;

/// <summary>Base class for all custom tools.</summary>
public abstract class AbstractCustomTool : IDevModeTool, IToolDescriptor {

  #region API

  /// <summary>Returns text to show in the block object tool warning area.</summary>
  /// <remarks>
  /// This text can change and the changes will be reflected in UI immediately. Empty string or null value will hide the
  /// warning.
  /// </remarks>
  public virtual string GetWarningText() => null;

  /// <summary>Shortcut to <see cref="ILoc"/>.</summary>
  protected ILoc Loc { get; private set; }

  /// <summary>The spec of the tool.</summary>
  /// <remarks>
  /// It can be used to extract more spec from the tools blueprint. E.g. <c>ToolSpec.GetSpec&lt;MyDataSpec&gt;()</c>.
  /// </remarks>
  protected CustomToolSpec ToolSpec { get; private set; }

  /// <summary>
  /// The localized text to present as the tool caption. If not overriden, then the string from the
  /// <see cref="AbstractCustomTool.ToolSpec"/> is used.
  /// </summary>
  /// <remarks>If <c>null</c> or empty, then the title will not be shown.</remarks>
  protected virtual string DescriptionTitleLoc => Loc.T(ToolSpec.DisplayNameLocKey);

  /// <summary>
  /// The localized text to present as the tool description. If not overriden, then the string from the
  /// <see cref="AbstractCustomTool.ToolSpec"/> is used.
  /// </summary>
  /// <remarks>If <c>null</c> or empty, then the description will not be shown.</remarks>
  protected virtual string DescriptionMainSectionLoc => Loc.T(ToolSpec.DescriptionLocKey);

  /// <summary>The localized option text that is presented at the bottom of the main stuff.</summary>
  /// <remarks>This value should be set in the <see cref="Initialize"/> method and don't change after that.</remarks>
  protected string DescriptionHintSection = null;

  /// <summary>
  /// The visual elements to add to via <see cref="ToolDescription.Builder.AddExternalSection"/>. It can be <c>null</c>.
  /// </summary>
  /// <remarks>This value should be set in the <see cref="Initialize"/> method and don't change after that.</remarks>
  protected VisualElement[] DescriptionExternalSections = null;

  /// <summary>
  /// The visual elements to add to via <see cref="ToolDescription.Builder.AddSection(VisualElement)"/>.
  /// It can be <c>null</c>.
  /// </summary>
  /// <remarks>This value should be set in the <see cref="Initialize"/> method and don't change after that.</remarks>
  protected VisualElement[] DescriptionVisualSections = null;

  /// <summary>Extra localized strings to add to the description as "bullets".</summary>
  /// <remarks>This value should be set in the <see cref="Initialize"/> method and don't change after that.</remarks>
  /// <seealso cref="SpecialStrings.RowStarter"/>
  protected string[] DescriptionBullets = null;

  /// <summary>Tells if any of the shift keys is held.</summary>
  protected bool IsShiftHeld => Keyboard.current.shiftKey.isPressed;

  /// <summary>Tells if any of the control keys is held.</summary>
  protected bool IsCtrlHeld => Keyboard.current.ctrlKey.isPressed;

  /// <summary>Tells if any of the alt keys is held.</summary>
  protected bool IsAltHeld => Keyboard.current.altKey.isPressed;

  /// <summary>Initializes the tool. Do all logic here instead of the constructor.</summary>
  protected virtual void Initialize() {
  }

  #endregion

  #region IDevModeTool implementation

  /// <inheritdoc/>
  public abstract void Enter();

  /// <inheritdoc/>
  public abstract void Exit();

  /// <inheritdoc/>
  public bool IsDevMode => ToolSpec.DevMode;

  #endregion

  #region IToolDescriptor implementation

  /// <inheritdoc/>
  public ToolDescription DescribeTool() {
    var description =
        new ToolDescription.Builder(!string.IsNullOrEmpty(DescriptionTitleLoc) ? Loc.T(DescriptionTitleLoc) : null);
    var descriptionText = new StringBuilder();
    if (!string.IsNullOrEmpty(DescriptionMainSectionLoc)) {
      descriptionText.Append(DescriptionMainSectionLoc);
    }
    if (DescriptionBullets != null) {
      foreach (var descriptionBullet in DescriptionBullets) {
        descriptionText.Append("\n" + SpecialStrings.RowStarter + descriptionBullet);
      }
    }
    description.AddSection(descriptionText.ToString());
    if (DescriptionVisualSections != null) {
      foreach (var visualSection in DescriptionVisualSections) {
        description.AddSection(visualSection);
      }
    }
    if (DescriptionHintSection != null) {
      description.AddPrioritizedSection(DescriptionHintSection);
    }
    if (DescriptionExternalSections != null) {
      foreach (var externalSection in DescriptionExternalSections) {
        description.AddExternalSection(externalSection);
      }
    }
    return description.Build();
  }

  #endregion

  #region Implementation

  /// <summary>Injects the dependencies. It has to be public to work.</summary>
  [Inject]
  public void InjectDependencies(ILoc loc) {
    Loc = loc;
  }

  internal void InitializeTool(CustomToolSpec toolSpec) {
    ToolSpec = toolSpec;
    Initialize();
  }

  #endregion
}