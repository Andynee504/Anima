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

    [Header("Stars (Layer 1)")]
    [Range(0f, 1f)] public float starDensity = 0.45f;
    [Range(0f, 3f)] public float starBrightness = 1.2f;
    [Range(0.0005f, .01f)] public float starSize = 0.0030f;
    [Range(0.0001f, .01f)] public float starSoftness = 0.0015f;
    [Range(0f, 1f)] public float twinkleAmount = 0.30f;
    [Range(0f, 8f)] public float twinkleSpeed = 1.6f;
    [Range(64f, 2048f)] public float starTile = 1024f;

    [Header("Stars (Layer 2 - opcional)")]
    public bool useSecondLayer = true;
    [Range(0f, 1f)] public float starDensity2 = 0.25f;
    [Range(0f, 3f)] public float starBrightness2 = 0.8f;
    [Range(0.0005f, .01f)] public float starSize2 = 0.0022f;
    [Range(64f, 4096f)] public float starTile2 = 2048f;

    [Header("Nebula")]
    [Range(0f, 1f)] public float nebulaIntensity = 0.25f;
    [Range(0.2f, 6f)] public float nebulaScale = 1.2f;
    [Range(0.5f, 3f)] public float nebulaContrast = 1.6f;

    [Header("Glints")]
    [Range(0f, 1f)] public float glintAmount = 0.12f;
    [Range(0f, 8f)] public float glintSpeed = 1.5f;

    [Header("Global")]
    [Range(0f, 1f)] public float fade = 1f;
    public Color tint = Color.white;

    [Header("Editor")]
    public bool liveUpdate = false; // marque se quiser aplicar em tempo real no Editor

    void OnValidate() { if (liveUpdate) Apply(); }
    void OnEnable() { Apply(); }

    public void Apply()
    {
        if (!domeMat) return;

        // Toggles
        domeMat.SetFloat("_StarsOn", starsOn ? 1f : 0f);
        domeMat.SetFloat("_NebulaOn", nebulaOn ? 1f : 0f);
        domeMat.SetFloat("_GlintsOn", glintsOn ? 1f : 0f);

        // Stars (Layer 1)
        domeMat.SetFloat("_StarDensity", starDensity);
        domeMat.SetFloat("_StarBrightness", starBrightness);
        domeMat.SetFloat("_StarSize", starSize);
        domeMat.SetFloat("_StarSoftness", starSoftness);
        domeMat.SetFloat("_TwinkleAmount", twinkleAmount);
        domeMat.SetFloat("_TwinkleSpeed", twinkleSpeed);
        domeMat.SetFloat("_StarTile", starTile);

        // Stars (Layer 2)
        float l2 = useSecondLayer ? 1f : 0f; // habilita por densidade/brightness
        domeMat.SetFloat("_StarDensity2", l2 > 0f ? starDensity2 : 0f);
        domeMat.SetFloat("_StarBrightness2", l2 > 0f ? starBrightness2 : 0f);
        domeMat.SetFloat("_StarSize2", l2 > 0f ? starSize2 : 0.001f);
        domeMat.SetFloat("_StarTile2", l2 > 0f ? starTile2 : 512f);

        // Nebula
        domeMat.SetFloat("_NebulaIntensity", nebulaIntensity);
        domeMat.SetFloat("_NebulaScale", nebulaScale);
        domeMat.SetFloat("_NebulaContrast", nebulaContrast);

        // Glints
        domeMat.SetFloat("_GlintAmount", glintAmount);
        domeMat.SetFloat("_GlintSpeed", glintSpeed);

        // Global
        domeMat.SetFloat("_Fade", fade);
        domeMat.SetColor("_Tint", tint);
    }

    // Exemplo de APIs públicas para menus/UI
    public void ToggleStars(bool on) { starsOn = on; Apply(); }
    public void ToggleNebula(bool on) { nebulaOn = on; Apply(); }
    public void ToggleGlints(bool on) { glintsOn = on; Apply(); }
}
