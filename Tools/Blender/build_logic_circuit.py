import bpy
from mathutils import Vector
from pathlib import Path

ROOT_DIR = Path(__file__).resolve().parents[2]
BLEND_OUT = ROOT_DIR / "Art" / "BlenderSource" / "LogicCircuitKit.blend"
FBX_OUT = ROOT_DIR / "Assets" / "Resources" / "AR" / "LogicCircuitKit.fbx"

# A compact, classroom-scale logic circuit designed in Blender in meters.
# Blender Z is the board normal; FBX Y-up export maps that to Unity local Y.
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
for block in bpy.data.materials:
    bpy.data.materials.remove(block)

scene = bpy.context.scene
scene.unit_settings.system = "METRIC"
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 1200
scene.render.resolution_y = 900
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.world.use_nodes = True
scene.world.node_tree.nodes.get("Background").inputs["Color"].default_value = (0.018, 0.026, 0.04, 1)
scene.world.node_tree.nodes.get("Background").inputs["Strength"].default_value = 0.24
scene.view_settings.view_transform = "Standard"
scene.view_settings.exposure = 0.0
scene.view_settings.gamma = 1.0

def material(name, rgba, metallic=0.0, roughness=0.42, emission=None, emission_strength=0.0, transmission=0.0):
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = rgba
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = rgba
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if "Emission Color" in bsdf.inputs:
        bsdf.inputs["Emission Color"].default_value = emission or (0.0, 0.0, 0.0, 1.0)
    if "Emission Strength" in bsdf.inputs:
        bsdf.inputs["Emission Strength"].default_value = emission_strength
    if "Transmission Weight" in bsdf.inputs:
        bsdf.inputs["Transmission Weight"].default_value = transmission
    return mat

pcb = material("PCB | deep teal solder mask", (0.008, 0.046, 0.055, 1), 0.12, 0.42)
edge = material("PCB | graphite edge", (0.018, 0.029, 0.045, 1), 0.55, 0.28)
copper = material("Trace | warm copper", (0.72, 0.31, 0.105, 1), 0.78, 0.24)
silk = material("Silkscreen | warm ivory", (0.76, 0.88, 0.85, 1), 0.0, 0.4)
black = material("Switch and IC | black polymer", (0.018, 0.024, 0.035, 1), 0.15, 0.25)
chip_top = material("IC | slate ceramic", (0.075, 0.105, 0.13, 1), 0.12, 0.3)
steel = material("Switch | brushed steel", (0.54, 0.62, 0.69, 1), 0.88, 0.22)
gold = material("Contact | gold plated", (0.83, 0.56, 0.19, 1), 0.9, 0.18)
wire_red = material("Wire A | signal red", (0.78, 0.13, 0.12, 1), 0.22, 0.3)
wire_blue = material("Wire B | signal blue", (0.08, 0.36, 0.78, 1), 0.22, 0.3)
wire_out = material("Wire Y | signal amber", (0.92, 0.55, 0.11, 1), 0.28, 0.28)
indicator_off = material("Indicator | unlit", (0.06, 0.10, 0.12, 1), 0.1, 0.28)
indicator_on = material("Indicator | live", (0.08, 0.72, 0.47, 1), 0.05, 0.2, (0.05, 0.9, 0.5, 1), 1.2)
bulb_glass = material("Bulb | frosted glass", (0.95, 0.79, 0.40, 0.28), 0.0, 0.2, (1.0, 0.54, 0.12, 1), 0.05, 0.25)
filament = material("Bulb | tungsten filament", (0.33, 0.19, 0.07, 1), 0.25, 0.35, (0.8, 0.22, 0.035, 1), 0.15)
bulb_base = material("Bulb | ceramic socket", (0.06, 0.075, 0.09, 1), 0.18, 0.32)

root = bpy.data.objects.new("LogicCircuitKit", None)
bpy.context.collection.objects.link(root)
root.empty_display_type = "PLAIN_AXES"
root.empty_display_size = 0.035

def set_parent(obj, parent=root):
    obj.parent = parent
    obj.matrix_parent_inverse = parent.matrix_world.inverted()
    return obj

def apply_scale(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.select_set(False)

def bevel(obj, width=0.002, segments=3):
    if width <= 0:
        return
    mod = obj.modifiers.new("Machined edge radius", "BEVEL")
    mod.width = width
    mod.segments = segments
    mod.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.select_set(False)

def cube(name, location, dimensions, mat, radius=0.001, parent=root):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    apply_scale(obj)
    bevel(obj, min(radius, min(dimensions) * 0.28), 3)
    if mat:
        obj.data.materials.append(mat)
    set_parent(obj, parent)
    return obj

def cylinder(name, location, radius, depth, mat, direction=(0, 0, 1), vertices=40, parent=root, bevel_width=0.0007):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    obj = bpy.context.object
    obj.name = name
    direction = Vector(direction).normalized()
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(direction)
    apply_scale(obj)
    bevel(obj, bevel_width, 2)
    if mat:
        obj.data.materials.append(mat)
    for face in obj.data.polygons:
        face.use_smooth = len(face.vertices) == 4
    set_parent(obj, parent)
    return obj

def sphere(name, location, radius, mat, scale=None, parent=root, segments=32, rings=20):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=radius, location=location)
    obj = bpy.context.object
    obj.name = name
    if scale:
        obj.scale = scale
        apply_scale(obj)
    if mat:
        obj.data.materials.append(mat)
    for face in obj.data.polygons:
        face.use_smooth = True
    set_parent(obj, parent)
    return obj

def torus(name, location, major_radius, minor_radius, mat, parent=root, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(major_segments=40, minor_segments=10, major_radius=major_radius, minor_radius=minor_radius, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    if mat:
        obj.data.materials.append(mat)
    for face in obj.data.polygons:
        face.use_smooth = True
    set_parent(obj, parent)
    return obj

def empty(name, location, parent=root):
    obj = bpy.data.objects.new(name, None)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    obj.empty_display_type = "SPHERE"
    obj.empty_display_size = 0.012
    set_parent(obj, parent)
    return obj

def curve_mesh(name, points, mat, radius=0.0014, parent=root):
    data = bpy.data.curves.new(name + " path", "CURVE")
    data.dimensions = "3D"
    data.resolution_u = 2
    data.bevel_depth = radius
    data.bevel_resolution = 3
    spline = data.splines.new("POLY")
    spline.points.add(len(points) - 1)
    for target, point in zip(spline.points, points):
        target.co = (point[0], point[1], point[2], 1.0)
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target="MESH")
    obj = bpy.context.object
    obj.name = name
    obj.select_set(False)
    if mat:
        obj.data.materials.append(mat)
    set_parent(obj, parent)
    return obj

def text_mesh(name, body, location, size, mat, rotation=(0, 0, 0), parent=root):
    data = bpy.data.curves.new(name + " type", "FONT")
    data.body = body
    data.size = size
    data.extrude = 0.00028
    data.bevel_depth = 0.00008
    data.align_x = "CENTER"
    data.align_y = "CENTER"
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    obj.rotation_euler = rotation
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.convert(target="MESH")
    obj = bpy.context.object
    obj.name = name
    obj.select_set(False)
    if mat:
        obj.data.materials.append(mat)
    set_parent(obj, parent)
    return obj

# PCB: 500 x 330 mm, safe rounded corners, realistic edge, pads, and silk markings.
cube("CircuitBoard", (0, 0, 0.010), (0.50, 0.33, 0.020), edge, 0.012)
cube("CircuitBoardTop", (0, 0, 0.021), (0.488, 0.318, 0.006), pcb, 0.010)
for x in (-0.222, 0.222):
    for y in (-0.137, 0.137):
        cylinder("BoardStandoff", (x, y, 0.0242), 0.010, 0.004, gold, vertices=24)
        cylinder("BoardScrew", (x, y, 0.0265), 0.0038, 0.0018, steel, vertices=16)
        cube("BoardScrewSlot", (x, y, 0.0275), (0.0048, 0.0007, 0.00045), edge, 0.0002)

# Real toggle switch assemblies with pivots for live interaction in Unity.
board_top = 0.024
for suffix, sy, label in (("A", 0.090, "A"), ("B", -0.090, "B")):
    sx = -0.186
    assembly = empty("InputSwitch" + suffix + "_Interaction", (sx, sy, board_top))
    cube("InputSwitch" + suffix + "_Housing", (sx, sy, board_top + 0.008), (0.058, 0.064, 0.016), black, 0.004, assembly)
    cylinder("InputSwitch" + suffix + "_Mount", (sx, sy, board_top + 0.017), 0.020, 0.008, steel, vertices=40, parent=assembly)
    cylinder("InputSwitch" + suffix + "_Collar", (sx, sy, board_top + 0.022), 0.014, 0.006, gold, vertices=40, parent=assembly)
    pivot = empty("InputSwitch" + suffix + "_LeverPivot", (sx, sy, board_top + 0.027), assembly)
    start = (sx, sy, board_top + 0.027)
    end = (sx + 0.018, sy, board_top + 0.086)
    axis = Vector(end) - Vector(start)
    cylinder("InputSwitch" + suffix + "_Lever", tuple((Vector(start) + Vector(end)) * 0.5), 0.0034, axis.length, steel, tuple(axis), vertices=24, parent=pivot, bevel_width=0.0005)
    sphere("InputSwitch" + suffix + "_Knob", end, 0.008, black, scale=(1, 1, 1.18), parent=pivot)
    sphere("InputSwitch" + suffix + "_Indicator", (sx + 0.019, sy - 0.023, board_top + 0.018), 0.0043, indicator_off, parent=assembly, segments=20, rings=12)
    for dx in (-0.022, 0.022):
        for dy in (-0.025, 0.025):
            cylinder("InputSwitch" + suffix + "_Terminal", (sx + dx, sy + dy, board_top + 0.002), 0.0032, 0.003, gold, vertices=16, parent=assembly)
    text_mesh("Silkscreen_Input" + suffix, label, (sx, sy - 0.038, board_top + 0.003), 0.018, silk)

# DIP logic module, pins, and visible copper contact pads.
cube("GateSocket", (-0.012, 0, 0.029), (0.147, 0.125, 0.010), black, 0.006)
cube("GatePackage", (-0.012, 0, 0.047), (0.132, 0.103, 0.027), chip_top, 0.006)
cube("GatePackageInset", (-0.012, 0, 0.061), (0.092, 0.064, 0.0015), black, 0.003)
for side in (-1, 1):
    for index in range(5):
        py = -0.043 + index * 0.0215
        px = -0.012 + side * 0.074
        cube("GatePin", (px, py, 0.035), (0.018, 0.006, 0.005), gold, 0.0012)
        cube("GatePad", (px + side * 0.008, py, 0.0247), (0.018, 0.009, 0.0015), copper, 0.001)
text_mesh("GateMark", "LOGIC", (-0.012, 0, 0.0622), 0.016, silk)

# Insulated signal runs, raised above the board and routed between the real parts.
curve_mesh("WireInputA", [(-0.157, 0.090, 0.029), (-0.111, 0.090, 0.029), (-0.082, 0.056, 0.029), (-0.068, 0.036, 0.029)], wire_red, 0.0020)
curve_mesh("WireInputB", [(-0.157, -0.090, 0.029), (-0.111, -0.090, 0.029), (-0.082, -0.056, 0.029), (-0.068, -0.036, 0.029)], wire_blue, 0.0020)
curve_mesh("WireOutputY", [(0.044, 0, 0.029), (0.088, 0, 0.029), (0.125, 0, 0.029), (0.155, 0, 0.029)], wire_out, 0.0022)
for i in range(3):
    cube("TracePad", (-0.12 + i * 0.034, 0.09, 0.0248), (0.012, 0.012, 0.0015), copper, 0.001)
    cube("TracePad", (-0.12 + i * 0.034, -0.09, 0.0248), (0.012, 0.012, 0.0015), copper, 0.001)

# A warm-white incandescent style output bulb; globe and filament animate in Unity.
bx, by = 0.187, 0.0
cylinder("OutputBulbSocket", (bx, by, board_top + 0.010), 0.020, 0.020, bulb_base, vertices=40)
for ring_z in (board_top + 0.005, board_top + 0.010, board_top + 0.015):
    torus("OutputBulbThread", (bx, by, ring_z), 0.0185, 0.0016, steel)
cylinder("OutputBulbContact", (bx, by, board_top + 0.022), 0.012, 0.006, gold, vertices=32)
sphere("OutputBulbGlass", (bx, by, board_top + 0.050), 0.026, bulb_glass, scale=(1, 1, 1.12), segments=40, rings=24)
curve_mesh("OutputBulbFilament", [
    (bx - 0.007, by, board_top + 0.033),
    (bx - 0.004, by, board_top + 0.044),
    (bx + 0.004, by, board_top + 0.044),
    (bx + 0.007, by, board_top + 0.033)
], filament, 0.0011)
empty("OutputBulbLightAnchor", (bx, by, board_top + 0.048))

# Printed labels remain visible even before the runtime gate name is updated.
text_mesh("Silkscreen_Title", "LOGICLAB  |  5V", (0.012, -0.139, 0.0248), 0.014, silk)
text_mesh("Silkscreen_A", "INPUT A", (-0.187, 0.047, 0.0248), 0.0095, silk)
text_mesh("Silkscreen_B", "INPUT B", (-0.187, -0.133, 0.0248), 0.0095, silk)
text_mesh("Silkscreen_Y", "OUTPUT Y", (0.182, -0.048, 0.0248), 0.0095, silk)

# Studio setup belongs to the editable .blend source; FBX export omits cameras and lights.
bpy.ops.object.camera_add(location=(0.80, -0.92, 1.02))
camera = bpy.context.object
camera.name = "PreviewCamera"
direction = Vector((0, 0, 0.02)) - camera.location
camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
camera.data.type = "ORTHO"
camera.data.ortho_scale = 0.82
scene.camera = camera
for name, loc, energy, size in (
    ("Key", (0.1, -0.25, 0.9), 12, 0.7),
    ("Fill", (-0.6, 0.4, 0.6), 7, 0.6),
    ("Rim", (0.4, 0.7, 0.7), 9, 0.5),
):
    bpy.ops.object.light_add(type="AREA", location=loc)
    light = bpy.context.object
    light.name = "Preview" + name
    light.data.energy = energy
    light.data.shape = "DISK"
    light.data.size = size
    light.rotation_euler = (Vector((0, 0, 0.02)) - light.location).to_track_quat("-Z", "Y").to_euler()

BLEND_OUT.parent.mkdir(parents=True, exist_ok=True)
FBX_OUT.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND_OUT))

bpy.ops.object.select_all(action="DESELECT")
export_objects = [root] + list(root.children_recursive)
for obj in export_objects:
    if obj.type in {"EMPTY", "MESH"}:
        obj.select_set(True)
bpy.context.view_layer.objects.active = root
bpy.ops.export_scene.fbx(
    filepath=str(FBX_OUT),
    use_selection=True,
    object_types={"EMPTY", "MESH"},
    apply_scale_options="FBX_SCALE_ALL",
    axis_forward="-Z",
    axis_up="Y",
    use_mesh_modifiers=True,
    mesh_smooth_type="FACE",
    add_leaf_bones=False,
    bake_anim=False,
    path_mode="AUTO",
    embed_textures=False
)

print("LOGICLAB_BLENDER_OK")
print("BLEND=" + str(BLEND_OUT))
print("FBX=" + str(FBX_OUT))
print("MODEL_OBJECTS=" + str(len(export_objects)))
