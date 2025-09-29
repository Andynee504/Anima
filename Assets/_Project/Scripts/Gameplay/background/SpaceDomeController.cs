using UnityEngine;

[DisallowMultipleComponent]
public class SpaceDomeController : MonoBehaviour
{
    [Header("Material")]
    public Material domeMat; // instância própria (Material, não shared)

    [Header("Toggles")]
    public bool starsOn = true;
    public bool nebulaOn = false;
    public bool glintsOn = false;

    [Header("Stars")]
    [Range(0f, 1f)] public float starDensity = 0.35f;
    [Range(0f, 2f)] public float starBrightness = 1.0f;
    [Range(0f, 1f)] public float twinkleAmount = 0.35f;
    [Range(0f, 5f)] public float twinkleSpeed = 1.2f;

    [Header("Nebula")]
    [Range(0f, 1f)] public float nebulaIntensity = 0.25f;
    [Range(0.1f, 4f)] public float nebulaScale = 1.0f;
    [Range(0.5f, 3f)] public float nebulaContrast = 1.4f;

    [Header("Glints")]
    [Range(0f, 1f)] public float glintAmount = 0.15f;
    [Range(0f, 5f)] public float glintSpeed = 1.5f;

    [Header("Global")]
    [Range(0f, 1f)] public float fade = 1f;
    public Color tint = Color.white;

    void OnValidate() { Apply(); }
    void Start() { Apply(); }

    public void Apply()
    {
        if (!domeMat) return;

        domeMat.SetFloat("_StarsOn", starsOn ? 1f : 0f);
        domeMat.SetFloat("_NebulaOn", nebulaOn ? 1f : 0f);
        domeMat.SetFloat("_GlintsOn", glintsOn ? 1f : 0f);

        domeMat.SetFloat("_StarDensity", starDensity);
        domeMat.SetFloat("_StarBrightness", starBrightness);
        domeMat.SetFloat("_TwinkleAmount", twinkleAmount);
        domeMat.SetFloat("_TwinkleSpeed", twinkleSpeed);

        domeMat.SetFloat("_NebulaIntensity", nebulaIntensity);
        domeMat.SetFloat("_NebulaScale", nebulaScale);
        domeMat.SetFloat("_NebulaContrast", nebulaContrast);

        domeMat.SetFloat("_GlintAmount", glintAmount);
        domeMat.SetFloat("_GlintSpeed", glintSpeed);

        domeMat.SetFloat("_Fade", fade);
        domeMat.SetColor("_Tint", tint);
    }

    // Exemplo de APIs públicas para menus
    public void ToggleStars(bool on) { starsOn = on; Apply(); }
    public void ToggleNebula(bool on) { nebulaOn = on; Apply(); }
    public void ToggleGlints(bool on) { glintsOn = on; Apply(); }
}
