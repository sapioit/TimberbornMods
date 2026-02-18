// Timberborn Custom Tools
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using Bindito.Core;
using IgorZ.CustomTools.KeyBindings;
using Timberborn.AreaSelectionSystem;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockObjectTools;
using Timberborn.BlockSystem;
using Timberborn.ConstructionGuidelines;
using Timberborn.ConstructionMode;
using Timberborn.Coordinates;
using Timberborn.EntitySystem;
using Timberborn.GameFactionSystem;
using Timberborn.InputSystem;
using Timberborn.SingletonSystem;
using Timberborn.TemplateSystem;
using Timberborn.UISound;
using UnityDev.Utils.LogUtilsLite;

namespace IgorZ.CustomTools.Tools;

/// <summary>The base class to the BlockObject tools that can place different templates.</summary>
/// <remarks>
/// This tool also supports the Undo ability. The block objects that were placed by the tool <i>before</i> existing it,
/// can be undone. The undo key binding is constant: <see cref="UndoPlacementsKeyBinding"/>. Due to the tool description
/// is not a dynamic thing, the primary binding is fixed to "Ctrl+Z". This is used in the tool description. A secondary
/// binding can be defined, but the primary one cannot be changed.
/// </remarks>
/// <typeparam name="T">the mode control that defines which template will be used to place the object.</typeparam>
public abstract class AbstractMultiTemplateBlockObjectTool<T> 
    : AbstractCustomTool, IInputProcessor, IConstructionModeEnabler, IBlockObjectGridTool where T: Enum {

  const string BlockObjectPlacedSoundName = "UI.BlockObjectPlaced";
  const string UndoPlacementsKeyBinding = "IgorZ-CustomTools-Undo";
  const string UndoHintLocKey = "IgorZ.CustomTools.BlockObjectTool.UndoHint"; 

  #region API

  /// <summary>Returns the placeable spec for the mode.</summary>
  protected abstract PlaceableBlockObjectSpec GetTemplateForMode(T mode);

  /// <summary>Returns the mode in which the tool needs to run in.</summary>
  /// <remarks>
  /// This is a high frequency method. It is called every video frame. As long as the mode doesn't change between the
  /// frames, the logic is cheap. If the mode is changed, then there will be stuff updated.
  /// </remarks>
  /// <seealso cref="GetTemplateForMode"/>
  protected abstract T GetCurrentMode();

  /// <summary>The current mode of this tool.</summary>
  /// <remarks>
  /// You can't change it directly. If the mode needs to be changed, it must be reported from
  /// <see cref="GetCurrentMode"/>.
  /// </remarks>
  protected T CurrentMode {
    get => _currentMode;
    private set {
      if (value.Equals(_currentMode) && _previewPlacer != null) {
        return;
      }
      _currentMode = value;
      _previewPlacer?.HideAllPreviews();
      _template = GetTemplateForMode(_currentMode);
      _previewPlacer = _previewPlacerFactory.Create(_template);
    }
  }
  T _currentMode;

  /// <summary>Returns a localized display name string for the template.</summary>
  protected string GetTemplateDisplayName(ComponentSpec template) {
    return Loc.T(template.GetSpec<LabeledEntitySpec>().DisplayNameLocKey);
  }

  /// <summary>A shortcut to the templates service.</summary>
  /// <param name="name">The full name of the template.</param>
  protected PlaceableBlockObjectSpec GetTemplate(string name) {
    return _templateNameMapper.GetTemplate(name).GetSpec<PlaceableBlockObjectSpec>();
  }

  /// <summary>A shortcut to the templates service, but without requiring the faction suffix.</summary>
  /// <remarks>
  /// This method will do two lookups: for the name "as-is" and for the name with the current faction ID as suffix. Use
  /// it when the name is the same for the factions, and you don't want to create separate setups.
  /// </remarks>
  /// <param name="name">The full name of the template with or <i>without</i> the faction ID suffix.</param>
  protected PlaceableBlockObjectSpec GetTemplateNoFaction(string name) {
    if (_templateNameMapper.TryGetTemplate(name, out var template)) {
      return template.GetSpec<PlaceableBlockObjectSpec>();
    }
    name += $".{_factionService.Current.Id}";
    return _templateNameMapper.GetTemplate(name).GetSpec<PlaceableBlockObjectSpec>();
  }

  #endregion

  #region IInputProcessor implementation

  /// <inheritdoc/>
  public virtual bool ProcessInput() {
    CurrentMode = GetCurrentMode();
    return _areaPicker.PickBlockObjectArea(
        _template, _previewPlacement.Orientation, _previewPlacement.FlipMode, PreviewCallback, Place);
  }

  #endregion

  #region AbstractCustomTool implementation

  /// <inheritdoc/>
  protected override void Initialize() {
    DescriptionBullets = [Loc.T(UndoHintLocKey)];
  }

  /// <inheritdoc/>
  public override string GetWarningText() {
    return _previewPlacer.WarningText;
  }

  /// <inheritdoc/>
  public override void Enter() {
    _inputService.AddInputProcessor(this);
    _eventBus.Register(this);
  }

  /// <inheritdoc/>
  public override void Exit() {
    _inputService.RemoveInputProcessor(this);
    _previewPlacer.HideAllPreviews();
    _areaPicker.Reset();
    _placedAnythingThisFrame = false;
    _placementHistory.Clear();
    _eventBus.Unregister(this);
  }

  #endregion

  #region Implementation

  InputService _inputService;
  TemplateNameMapper _templateNameMapper;
  PreviewPlacerFactory _previewPlacerFactory;
  BlockObjectPlacerService _blockObjectPlacerService;
  UISoundController _uiSoundController;
  PreviewPlacement _previewPlacement;
  AreaPicker _areaPicker;
  FactionService _factionService;
  EventBus _eventBus;
  EntityService _entityService;

  PlaceableBlockObjectSpec _template;
  PreviewPlacer _previewPlacer;
  bool _placedAnythingThisFrame;
  readonly Stack<List<BaseComponent>> _placementHistory = [];

  /// <summary>Has to be public for the inject to work!</summary>
  [Inject]
  public void InjectDependencies(
      InputService inputService, TemplateNameMapper templateNameMapper,
      PreviewPlacerFactory previewPlacerFactory, BlockObjectPlacerService blockObjectPlacerService,
      AreaPicker areaPicker, UISoundController uiSoundController, PreviewPlacement previewPlacement,
      FactionService factionService, EventBus eventBus, EntityService entityService) {
    _inputService = inputService;
    _templateNameMapper = templateNameMapper;
    _previewPlacerFactory = previewPlacerFactory;
    _blockObjectPlacerService = blockObjectPlacerService;
    _areaPicker = areaPicker;
    _uiSoundController = uiSoundController;
    _previewPlacement = previewPlacement;
    _factionService = factionService;
    _eventBus = eventBus;
    _entityService = entityService;
  }

  void PreviewCallback(IEnumerable<Placement> placements) {
    if (_placedAnythingThisFrame) { 
      _placedAnythingThisFrame = false; 
    } else {
      ShowPreviews(placements);
    }
  }

  void ShowPreviews(IEnumerable<Placement> placements) {
    _previewPlacer.ShowPreviews(placements);
  }

  void Place(IEnumerable<Placement> placements) {
    _placedAnythingThisFrame = false;
    var buildableCoordinates = _previewPlacer.GetBuildableCoordinates(placements);
    var spec = _template.GetSpec<BlockObjectSpec>();
    var blockObjectPlacer = _blockObjectPlacerService.GetMatchingPlacer(spec);
    var historyRecord = new List<BaseComponent>();
    foreach (var placement in buildableCoordinates) {
      blockObjectPlacer.Place(spec, placement, component => historyRecord.Add(component));
      _placedAnythingThisFrame = true;
    }
    _placementHistory.Push(historyRecord);
    if (_placedAnythingThisFrame) {
      _uiSoundController.PlaySound(BlockObjectPlacedSoundName);
    } else {
      _uiSoundController.PlayCantDoSound();
    }
  }

  /// <summary>Listens for the undo keybinding.</summary>
  [OnEvent]
  public void OnCustomToolEvent(CustomToolKeyBindingEvent keyBindingEvent) {
    if (_placementHistory.Count == 0 || keyBindingEvent.KeyBinding.Id != UndoPlacementsKeyBinding) {
      return;
    }
    var sequence = _placementHistory.Pop();
    DebugEx.Info("Undoing {0} placements...", sequence.Count);
    foreach (var component in sequence) {
      _entityService.Delete(component);
    }
  }

  #endregion
}
