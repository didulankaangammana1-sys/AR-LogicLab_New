using TMPro;
using UnityEngine;

/// <summary>
/// Shows a Blender-authored circuit kit and synchronizes switches, signal wires, and the output bulb.
/// </summary>
public sealed class GateCircuitVisual : MonoBehaviour
{
    private static readonly Color Cyan = new Color32(72, 220, 232, 255);
    private static readonly Color Green = new Color32(74, 232, 150, 255);
    private static readonly Color Off = new Color32(42, 57, 68, 255);
    private static readonly Color WireRed = new Color32(236, 83, 73, 255);
    private static readonly Color WireBlue = new Color32(62, 142, 245, 255);
    private static readonly Color Amber = new Color32(255, 176, 64, 255);
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();

    private Transform modelRoot;
    private Transform switchAAssembly;
    private Transform switchBAssembly;
    private Transform switchALever;
    private Transform switchBLever;
    private Collider switchACollider;
    private Collider switchBCollider;
    private Renderer switchAIndicator;
    private Renderer switchBIndicator;
    private Renderer inputWireA;
    private Renderer inputWireB;
    private Renderer outputWire;
    private Renderer bulbGlass;
    private Renderer bulbFilament;
    private Light bulbLight;
    private TextMesh gateLabel;
    private bool hasBlenderModel;

    public void Configure(LogicGateType gate, bool a, bool b)
    {
        EnsureCircuitModel();
        if (gateLabel != null) gateLabel.text = LogicGateDefinition.Name(gate);
        if (switchBCollider != null) switchBCollider.enabled = gate != LogicGateType.Not;
        SetInputs(a, b, LogicGateDefinition.Evaluate(gate, a, b));
    }

    public void SetInputs(bool a, bool b, bool output)
    {
        EnsureCircuitModel();

        if (switchALever != null)
            switchALever.localRotation = Quaternion.Euler(0f, 0f, a ? -27f : 27f);
        if (switchBLever != null)
            switchBLever.localRotation = Quaternion.Euler(0f, 0f, b ? -27f : 27f);

        SetColor(switchAIndicator, a ? Green : Off, a ? Green * 1.7f : Color.black);
        SetColor(switchBIndicator, b ? Green : Off, b ? Green * 1.7f : Color.black);
        SetColor(inputWireA, a ? WireRed : Off, a ? WireRed * 1.25f : Color.black);
        SetColor(inputWireB, b ? WireBlue : Off, b ? WireBlue * 1.25f : Color.black);
        SetColor(outputWire, output ? Green : Off, output ? Green * 1.8f : Color.black);
        SetColor(bulbFilament, output ? Amber : new Color(0.12f, 0.09f, 0.055f), output ? Amber * 2.6f : Color.black);
        SetColor(bulbGlass, output ? new Color(1f, 0.78f, 0.36f, 0.38f) : new Color(0.28f, 0.22f, 0.13f, 0.24f),
            output ? new Color(1f, 0.48f, 0.1f, 1f) * 1.2f : Color.black);

        if (bulbLight != null)
        {
            bulbLight.enabled = output;
            bulbLight.intensity = output ? 1.15f : 0f;
        }
    }

    /// <summary>Returns true when a screen ray hits one of the two large, touch-friendly switch targets.</summary>
    public bool TryGetInputSwitch(Ray ray, out bool inputA)
    {
        inputA = false;
        if (!hasBlenderModel) return false;

        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, 5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return false;

        Transform hitTransform = hit.collider.transform;
        if (switchAAssembly != null &&
            (hitTransform == switchAAssembly || hitTransform.IsChildOf(switchAAssembly)))
        {
            inputA = true;
            return true;
        }

        if (switchBAssembly != null &&
            (hitTransform == switchBAssembly || hitTransform.IsChildOf(switchBAssembly)))
        {
            inputA = false;
            return true;
        }

        return false;
    }

    private void EnsureCircuitModel()
    {
        if (modelRoot != null) return;

        GameObject circuitAsset = Resources.Load<GameObject>("AR/LogicCircuitKit");
        if (circuitAsset == null)
        {
            Debug.LogError("LogicLab Blender model is missing at Resources/AR/LogicCircuitKit.fbx. Using the safety fallback.");
            BuildSafetyFallback();
            return;
        }

        GameObject instance = Instantiate(circuitAsset, transform, false);
        instance.name = "Blender Logic Circuit Kit";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
        modelRoot = instance.transform;
        hasBlenderModel = true;

        switchAAssembly = FindChild(modelRoot, "InputSwitchA_Interaction");
        switchBAssembly = FindChild(modelRoot, "InputSwitchB_Interaction");
        switchALever = FindChild(modelRoot, "InputSwitchA_LeverPivot");
        switchBLever = FindChild(modelRoot, "InputSwitchB_LeverPivot");
        switchAIndicator = FindRenderer(modelRoot, "InputSwitchA_Indicator");
        switchBIndicator = FindRenderer(modelRoot, "InputSwitchB_Indicator");
        inputWireA = FindRenderer(modelRoot, "WireInputA");
        inputWireB = FindRenderer(modelRoot, "WireInputB");
        outputWire = FindRenderer(modelRoot, "WireOutputY");
        bulbGlass = FindRenderer(modelRoot, "OutputBulbGlass");
        bulbFilament = FindRenderer(modelRoot, "OutputBulbFilament");

        switchACollider = AddTouchCollider(switchAAssembly);
        switchBCollider = AddTouchCollider(switchBAssembly);

        Transform lightAnchor = FindChild(modelRoot, "OutputBulbLightAnchor");
        if (lightAnchor != null)
        {
            bulbLight = lightAnchor.gameObject.AddComponent<Light>();
            bulbLight.type = LightType.Point;
            bulbLight.color = new Color(1f, 0.54f, 0.18f);
            bulbLight.range = 0.36f;
            bulbLight.intensity = 0f;
            bulbLight.shadows = LightShadows.None;
            bulbLight.enabled = false;
        }

        GameObject labelObject = new GameObject("Runtime Gate Label");
        labelObject.transform.SetParent(modelRoot, false);
        labelObject.transform.localPosition = new Vector3(-0.012f, 0.066f, 0f);
        labelObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        gateLabel = labelObject.AddComponent<TextMesh>();
        gateLabel.text = "AND";
        gateLabel.fontSize = 64;
        gateLabel.characterSize = 0.018f;
        gateLabel.anchor = TextAnchor.MiddleCenter;
        gateLabel.alignment = TextAlignment.Center;
        gateLabel.color = Cyan;

        EnableEmission(switchAIndicator);
        EnableEmission(switchBIndicator);
        EnableEmission(inputWireA);
        EnableEmission(inputWireB);
        EnableEmission(outputWire);
        EnableEmission(bulbGlass);
        EnableEmission(bulbFilament);
    }

    private static Transform FindChild(Transform parent, string exactName)
    {
        if (parent == null) return null;
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
            if (children[i].name == exactName) return children[i];
        return null;
    }

    private static Renderer FindRenderer(Transform parent, string exactName)
    {
        Transform found = FindChild(parent, exactName);
        return found != null ? found.GetComponent<Renderer>() : null;
    }

    private static Collider AddTouchCollider(Transform target)
    {
        if (target == null) return null;

        BoxCollider box = target.GetComponent<BoxCollider>();
        if (box == null) box = target.gameObject.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.038f, 0f);
        box.size = new Vector3(0.078f, 0.108f, 0.078f);
        return box;
    }

    private static void EnableEmission(Renderer renderer)
    {
        if (renderer == null) return;
        Material[] materials = renderer.materials;
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] != null && materials[i].HasProperty(EmissionId))
                materials[i].EnableKeyword("_EMISSION");
        }
    }

    private void SetColor(Renderer renderer, Color color, Color emission)
    {
        if (renderer == null) return;
        properties.Clear();
        properties.SetColor(BaseColorId, color);
        properties.SetColor(ColorId, color);
        properties.SetColor(EmissionId, emission);
        renderer.SetPropertyBlock(properties);
    }

    private void BuildSafetyFallback()
    {
        hasBlenderModel = false;
        Material boardMat = MakeMaterial(new Color32(15, 43, 56, 255));
        Material componentMat = MakeMaterial(new Color32(27, 42, 56, 255));
        GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "Fallback circuit board";
        board.transform.SetParent(transform, false);
        board.transform.localScale = new Vector3(0.5f, 0.02f, 0.33f);
        board.GetComponent<Renderer>().sharedMaterial = boardMat;

        GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gate.name = "Fallback logic chip";
        gate.transform.SetParent(transform, false);
        gate.transform.localPosition = new Vector3(-0.01f, 0.047f, 0f);
        gate.transform.localScale = new Vector3(0.13f, 0.03f, 0.1f);
        gate.GetComponent<Renderer>().sharedMaterial = componentMat;

        TextMesh label = new GameObject("Fallback gate label").AddComponent<TextMesh>();
        label.transform.SetParent(transform, false);
        label.transform.localPosition = new Vector3(-0.01f, 0.068f, 0f);
        label.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        label.text = "AND";
        label.characterSize = 0.018f;
        label.anchor = TextAnchor.MiddleCenter;
        gateLabel = label;

        switchAAssembly = CreateFallbackSwitch("Fallback input A", new Vector3(-0.18f, 0.04f, 0.09f), componentMat);
        switchBAssembly = CreateFallbackSwitch("Fallback input B", new Vector3(-0.18f, 0.04f, -0.09f), componentMat);
        switchACollider = switchAAssembly.GetComponent<Collider>();
        switchBCollider = switchBAssembly.GetComponent<Collider>();

        GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulb.name = "Fallback output bulb";
        bulb.transform.SetParent(transform, false);
        bulb.transform.localPosition = new Vector3(0.19f, 0.05f, 0f);
        bulb.transform.localScale = Vector3.one * 0.052f;
        bulbGlass = bulb.GetComponent<Renderer>();
        bulbGlass.sharedMaterial = MakeMaterial(Off);

        bulbLight = bulb.AddComponent<Light>();
        bulbLight.type = LightType.Point;
        bulbLight.color = new Color(1f, 0.54f, 0.18f);
        bulbLight.range = 0.36f;
        bulbLight.intensity = 0f;
        bulbLight.shadows = LightShadows.None;
        bulbLight.enabled = false;
    }

    private Transform CreateFallbackSwitch(string objectName, Vector3 position, Material material)
    {
        GameObject group = new GameObject(objectName);
        group.transform.SetParent(transform, false);
        group.transform.localPosition = position;

        GameObject baseObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObject.transform.SetParent(group.transform, false);
        baseObject.transform.localScale = new Vector3(0.038f, 0.008f, 0.038f);
        baseObject.GetComponent<Renderer>().sharedMaterial = material;

        BoxCollider collider = group.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.035f, 0f);
        collider.size = new Vector3(0.078f, 0.108f, 0.078f);
        return group.transform;
    }

    private static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.color = color;
        if (material.HasProperty(BaseColorId)) material.SetColor(BaseColorId, color);
        if (material.HasProperty(EmissionId))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor(EmissionId, Color.black);
        }
        return material;
    }
}
