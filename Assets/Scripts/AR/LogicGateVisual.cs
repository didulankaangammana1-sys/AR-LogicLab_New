
using UnityEngine;

public class LogicGateVisual : MonoBehaviour
{
    [SerializeField] private Renderer outputLed;

    private Material ledMaterial;

    void Awake()
    {
        if (outputLed == null)
        {
            Debug.LogError("Output LED is not assigned.");
            return;
        }

        ledMaterial = outputLed.material;
        SetOutput(false);
    }

    public void SetOutput(bool value)
    {
        if (ledMaterial == null) return;

        Color color = value
            ? new Color(0.13f, 0.90f, 0.35f)
            : new Color(0.30f, 0.35f, 0.42f);

        if (ledMaterial.HasProperty("_BaseColor"))
            ledMaterial.SetColor("_BaseColor", color);
        else
            ledMaterial.color = color;

        if (ledMaterial.HasProperty("_EmissionColor"))
        {
            ledMaterial.EnableKeyword("_EMISSION");

            ledMaterial.SetColor(
                "_EmissionColor",
                value ? color * 1.5f : Color.black
            );
        }
    }
}
