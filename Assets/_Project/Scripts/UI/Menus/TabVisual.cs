using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TabVisual : MonoBehaviour
{
    [SerializeField] Toggle toggle; // arraste o Toggle do próprio objeto
    [SerializeField] Image fill; // filho Fill
    [SerializeField] TMP_Text label; // filho Label
    [SerializeField] string goldHex = "#FFE179";
    [SerializeField] string darkHex = "#171616";
    Color gold, dark;

    void Awake()
    {
        ColorUtility.TryParseHtmlString(goldHex, out gold); // #FFE179
        ColorUtility.TryParseHtmlString(darkHex, out dark); // #171616
        Apply(toggle && toggle.isOn); // estado inicial
        if (toggle) toggle.onValueChanged.AddListener(Apply); // escuta mudanças
    }

    void Apply(bool on)
    {
        if (fill) fill.color = new Color(gold.r, gold.g, gold.b, on ? 1f : 0f); // alpha 1/0
        if (label) label.color = on ? dark : gold; // texto conforme estado
    }
}
