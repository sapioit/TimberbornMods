// Timberborn Custom Tools
// Author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using System;
using System.Collections.Generic;
using Bindito.Core;
using Timberborn.AreaSelectionSystem;
using Timberborn.BlockObjectTools;
using Timberborn.BlockSystem;
using Timberborn.ConstructionGuidelines;
using Timberborn.ConstructionMode;
using Timberborn.Coordinates;
using Timberborn.GameFactionSystem;
using Timberborn.InputSystem;
using Timberborn.TemplateSystem;
using Timberborn.UISound;

namespace IgorZ.CustomTools.Tools;

/// <summary>The base class to the BlockObject tools that can place different templates.</summary>
/// <typeparam name="T">the mode control that defines which template will be sued to place the object.</typeparam>
public abstract class AbstractMultiTemplateBlockObjectTool<T> 
    : AbstractCustomTool, IInputProcessor, IConstructionModeEnabler, IBlockObjectGridTool where T: Enum {

  const string BlockObjectPlacedSoundName = "UI.BlockObjectPlaced";

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
  public override void Enter() {
    _inputService.AddInputProcessor(this);
  }

  /// <inheritdoc/>
  public override void Exit() {
    _inputService.RemoveInputProcessor(this);
    _previewPlacer.HideAllPreviews();
    _areaPicker.Reset();
    _placedAnythingThisFrame = false;
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

  PlaceableBlockObjectSpec _template;
  PreviewPlacer _previewPlacer;
  bool _placedAnythingThisFrame;

  /// <summary>Has to be public for the inject to work!</summary>
  [Inject]
  public void InjectDependencies(
      InputService inputService, TemplateNameMapper templateNameMapper,
      PreviewPlacerFactory previewPlacerFactory, BlockObjectPlacerService blockObjectPlacerService,
      AreaPicker areaPicker, UISoundController uiSoundController, PreviewPlacement previewPlacement,
      FactionService factionService) {
    _inputService = inputService;
    _templateNameMapper = templateNameMapper;
    _previewPlacerFactory = previewPlacerFactory;
    _blockObjectPlacerService = blockObjectPlacerService;
    _areaPicker = areaPicker;
    _uiSoundController = uiSoundController;
    _previewPlacement = previewPlacement;
    _factionService = factionService;
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
    foreach (var placement in buildableCoordinates) {
      blockObjectPlacer.Place(spec, placement, _ => {});
      _placedAnythingThisFrame = true;
    }
    if (_placedAnythingThisFrame) {
      _uiSoundController.PlaySound(BlockObjectPlacedSoundName);
    } else {
      _uiSoundController.PlayCantDoSound();
    }
  }

  #endregion
}
