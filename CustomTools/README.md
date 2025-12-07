# Timberborn: Custom Tools

This mod offers new QoL tools in the bottom bar, and also provides API for the modders to quickly create own tools.

## For the players: Built-in tools

![Custom tools bottom bar button](https://raw.githubusercontent.com/ihsoft/TimberbornMods/refs/heads/timberborn-1.0/CustomTools/Workshop/Showcase/GroupButtonDemo.png)

### Immediate finish of incomplete buildings

This tool is only visible when the dev mode is activated (Shift+Alt+Z). Select multiple incomplete buildings and have
them all instantly built. A handy tool when testing stuff.

![Immediate finish of incomplete buildings](https://raw.githubusercontent.com/ihsoft/TimberbornMods/refs/heads/timberborn-1.0/CustomTools/Workshop/Showcase/FinishNowToolDemo.png)

### Pause buildings in the selected range

Select multiple buildings that need to stop. Use SHIFT to "lock" selection to a certain buildings type. A handy tool to
pause a set of buildings when micromanaging the colony.

![Pause buildings in the selected range](https://raw.githubusercontent.com/ihsoft/TimberbornMods/refs/heads/timberborn-1.0/CustomTools/Workshop/Showcase/PauseBuildingToolDemo.png)

### Resume buildings in the selected range

Select multiple buildings that need to resume working. Use SHIFT to "lock" selection to a certain buildings type. A
handy tool to resume a set of buildings when micromanaging the colony.

![Resume buildings in the selected range](https://raw.githubusercontent.com/ihsoft/TimberbornMods/refs/heads/timberborn-1.0/CustomTools/Workshop/Showcase/ResumeBuildingToolDemo.png)

## For the modders: API to create own tools

The game's approach of creating tools requires a pretty big code efforts. If your mod only need "a button" in the bottom
bar, you can use this mod to save your efforts:

1. Make a simple class that inherits
   [`AbstractCustomTool`](https://github.com/ihsoft/TimberbornMods/blob/timberborn-1.0/CustomTools/Tools/AbstractCustomTool.cs)
   or one of its descendants.
2. Create a blueprint that defines the appearance of your button. At the very least, add
   [`CustomToolSpec`](https://github.com/ihsoft/TimberbornMods/blob/8704467e2e08885f47f8b4cce06ed01912e48672/CustomTools/Core/CustomToolSpec.cs)
   to the blueprint. You can add more specs to control the tool behavior or pass extra data to your class.
3. Use the Timberborn blueprint patching system to update
   [`TemplateCollection`](https://github.com/ihsoft/TimberbornMods/blob/8704467e2e08885f47f8b4cce06ed01912e48672/CustomTools/Mod/Blueprints/TemplateCollections/TemplateCollection.BottomBar.CustomTools.blueprint.json).
   This will let `CustomTools` mod know about your new tool.
4. Optionally. Create your own group button in the bar to attach the tools to. See how it's done in
   [this blueprint example](https://github.com/ihsoft/TimberbornMods/blob/8704467e2e08885f47f8b4cce06ed01912e48672/CustomTools/Mod/Blueprints/ToolGroups/ToolGroup.CustomTools.blueprint.json).

Tools examples:
* [`DebugFinishNowTool`](https://github.com/ihsoft/TimberbornMods/blob/timberborn-1.0/CustomTools/Tools/DebugFinishNowTool.cs).
  It's a very basic tool that picks up a set of block objects from the map and preforms actions on them. The blueprint
  for it is located [here](https://github.com/ihsoft/TimberbornMods/blob/8704467e2e08885f47f8b4cce06ed01912e48672/CustomTools/Mod/Blueprints/Tools/Tool.CustomTools.DebugFinishNowTool.blueprint.json).
* [`PauseTool`](https://github.com/ihsoft/TimberbornMods/blob/timberborn-1.0/CustomTools/Tools/PauseTool.cs). A more
  advanced example of using the "locking selection tool". This tool can selectively pick the objects.