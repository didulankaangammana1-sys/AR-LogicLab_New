using TMPro;
using UnityEngine;

/// <summary>Builds a lightweight, readable circuit on the detected AR plane at runtime.</summary>
public sealed class GateCircuitVisual : MonoBehaviour
{
    private Renderer switchA;
    private Renderer switchB;
    private Renderer outputLed;
    private Renderer wireA;
    private Renderer wireB;
    private Renderer outputWire;
    private TextMesh gateLabel;
    private MaterialPropertyBlock properties;

    private static readonly Color Navy = new Color32(12, 24, 42, 255);
    private static readonly Color Cyan = new Color32(48, 214, 234, 255);
    private static readonly Color Green = new Color32(73, 225, 150, 255);
    private static readonly Color Off = new Color32(82, 102, 123, 255);

    public void Configure(LogicGateType gate, bool a, bool b)
    {
        if (gateLabel == null) BuildBoard();
        gateLabel.text = LogicGateDefinition.Name(gate);
        SetInputs(a, b, LogicGateDefinition.Evaluate(gate, a, b));
    }

    public void SetInputs(bool a, bool b, bool output)
    {
        SetColor(switchA, a ? Green : Off);
        SetColor(switchB, b ? Green : Off);
        SetColor(wireA, a ? Cyan : Off);
        SetColor(wireB, b ? Cyan : Off);
        SetColor(outputWire, output ? Green : Off);
        SetColor(outputLed, output ? Green : Off);
    }

    private void BuildBoard()
    {
        properties = new MaterialPropertyBlock();
        MakeCube("Circuit board", Vector3.zero, new Vector3(.44f, .018f, .29f), Navy);
        MakeCube("Gate body", new Vector3(0, .033f, 0), new Vector3(.16f, .04f, .12f), new Color32(23, 47, 72, 255));

        gateLabel = MakeLabel("Gate label", "AND", new Vector3(0, .058f, -.055f), .045f, Cyan);
        gateLabel.transform.localRotation = Quaternion.Euler(-90, 0, 0);

        switchA = MakeCube("Input A switch", new Vector3(-.17f, .04f, .075f), new Vector3(.045f, .025f, .045f), Off);
        switchB = MakeCube("Input B switch", new Vector3(-.17f, .04f, -.075f), new Vector3(.045f, .025f, .045f), Off);
        outputLed = MakeSphere("Output LED", new Vector3(.17f, .052f, 0), new Vector3(.043f, .025f, .043f), Off);
        wireA = MakeCube("Input wire A", new Vector3(-.09f, .028f, .075f), new Vector3(.10f, .008f, .009f), Off);
        wireB = MakeCube("Input wire B", new Vector3(-.09f, .028f, -.075f), new Vector3(.10f, .008f, .009f), Off);
        outputWire = MakeCube("Output wire", new Vector3(.09f, .028f, 0), new Vector3(.09f, .008f, .009f), Off);
        MakeLabel("Input A label", "A", new Vector3(-.17f, .055f, .105f), .025f, Color.white);
        MakeLabel("Input B label", "B", new Vector3(-.17f, .055f, -.045f), .025f, Color.white);
        MakeLabel("Output label", "Y", new Vector3(.17f, .075f, .04f), .025f, Color.white);
    }

    private Renderer MakeCube(string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = name;
        item.transform.SetParent(transform, false);
        item.transform.localPosition = position;
        item.transform.localScale = scale;
        Renderer renderer = item.GetComponent<Renderer>();
        renderer.sharedMaterial = MakeMaterial(color);
        return renderer;
    }

    private Renderer MakeSphere(string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        item.name = name;
        item.transform.SetParent(transform, false);
        item.transform.localPosition = position;
        item.transform.localScale = scale;
        Renderer renderer = item.GetComponent<Renderer>();
        renderer.sharedMaterial = MakeMaterial(color);
        return renderer;
    }

    private TextMesh MakeLabel(string name, string text, Vector3 position, float size, Color color)
    {
        GameObject item = new GameObject(name);
        item.transform.SetParent(transform, false);
        item.transform.localPosition = position;
        TextMesh label = item.AddComponent<TextMesh>();
        label.text = text;
        label.fontSize = 64;
        label.characterSize = size;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = color;
        return label;
    }

    private static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var material = new Material(shader) { color = color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * .35f);
        }
        return material;
    }

    private void SetColor(Renderer renderer, Color color)
    {
        if (renderer == null) return;
        renderer.GetPropertyBlock(properties);
        properties.SetColor("_BaseColor", color);
        properties.SetColor("_Color", color);
        properties.SetColor("_EmissionColor", color * .8f);
        renderer.SetPropertyBlock(properties);
    }
}
