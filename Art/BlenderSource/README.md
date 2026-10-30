# Logic Circuit Kit

The 500 × 330 mm circuit board is authored in Blender 5.2. Its source is `Art/BlenderSource/LogicCircuitKit.blend`; the reproducible generator is `Tools/Blender/build_logic_circuit.py`. From the project root, regenerate the Unity model with:

```powershell
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup --python "Tools/Blender/build_logic_circuit.py"
```

The script exports `Assets/Resources/AR/LogicCircuitKit.fbx`. Unity loads that model for every gate. `GateCircuitVisual` adds touch targets to both metal toggle switches, updates the red/blue input wires and gate label, and turns the output bulb, filament, green output wire, and point light on or off from the selected gate's truth result.
