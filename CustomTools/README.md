# Timberborn: Custom Tools

This mod offers new quality-of-life tools in the bottom bar and also provides an API for modders to quickly create their own tools.

## For the players: Built-in tools

![Custom tools bottom bar button](https://raw.githubusercontent.com/ihsoft/TimberbornMods/refs/heads/timberborn-1.0/CustomTools/Workshop/Showcase/GroupButtonDemo.png)

### Immediate finish of incomplete buildings

This tool is only visible when the dev mode is activated (Shift + Alt + Z). Select multiple incomplete buildings and have
them instantly completed. A handy tool when testing or prototyping.

![Immediate finish of incomplete buildings](https://raw.githubusercontent.com/ihsoft/TimberbornMods/refs/heads/timberborn-1.0/CustomTools/Workshop/Showcase/FinishNowToolDemo.png)

### Pause buildings in the selected range

Select multiple buildings that need to be paused. Hold **SHIFT** to lock the selection to a specific building type.  
A useful tool for temporarily stopping groups of buildings during colony micromanagement.

![Pause buildings in the selected range](https://raw.githubusercontent.com/ihsoft/TimberbornMods/refs/heads/timberborn-1.0/CustomTools/Workshop/Showcase/PauseBuildingToolDemo.png)

### Resume buildings in the selected range

Select multiple buildings you want to resume. Hold **SHIFT** to lock the selection to a specific building type.  
Helps quickly bring groups of buildings back online during micromanagement.

![Resume buildings in the selected range](https://raw.githubusercontent.com/ihsoft/TimberbornMods/refs/heads/timberborn-1.0/CustomTools/Workshop/Showcase/ResumeBuildingToolDemo.png)

## For the modders: API to create your own tools

The game's native approach to creating tools requires a considerable amount of code.  
If your mod only needs “a button” in the bottom bar, you can use this mod to significantly reduce development effort:

1. Create a simple class that inherits from
   [`AbstractCustomTool`](https://github.com/ihsoft/TimberbornMods/blob/timberborn-1.0/CustomTools/Tools/AbstractCustomTool.cs)
   or one of its descendants.

2. Create a blueprint that defines the appearance of your button. At minimum, add
   [`CustomToolSpec`](https://github.com/ihsoft/TimberbornMods/blob/8704467e2e08885f47f8b4cce06ed01912e48672/CustomTools/Core/CustomToolSpec.cs).
   You can add additional specs to control behavior or provide extra data to your class.

3. Use Timberborn’s blueprint patching system to update
   [`TemplateCollection`](https://github.com/ihsoft/TimberbornMods/blob/8704467e2e08885f47f8b4cce06ed01912e48672/CustomTools/Mod/Blueprints/TemplateCollections/TemplateCollection.BottomBar.CustomTools.blueprint.json).
   This allows the **CustomTools** mod to recognize your new tool.

4. (Optional) Create your own group button in the bottom bar and attach your tools to it. See this example blueprint:
   [ToolGroup.CustomTools](https://github.com/ihsoft/TimberbornMods/blob/8704467e2e08885f47f8b4cce06ed01912e48672/CustomTools/Mod/Blueprints/ToolGroups/ToolGroup.CustomTools.blueprint.json)

### Tool examples

* [`DebugFinishNowTool`](https://github.com/ihsoft/TimberbornMods/blob/timberborn-1.0/CustomTools/Tools/DebugFinishNowTool.cs).
  A basic tool that selects a set of block objects on the map and performs actions on them.
  Its blueprint can be found [here](https://github.com/ihsoft/TimberbornMods/blob/8704467e2e08885f47f8b4cce06ed01912e48672/CustomTools/Mod/Blueprints/Tools/Tool.CustomTools.DebugFinishNowTool.blueprint.json).

* [`PauseTool`](https://github.com/ihsoft/TimberbornMods/blob/timberborn-1.0/CustomTools/Tools/PauseTool.cs).
  A more advanced example that uses selection locking to target specific object types.
