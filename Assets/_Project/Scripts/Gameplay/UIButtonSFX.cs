using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(AudioSource))]
public class UIButtonSFX : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] AudioClip hover;
    [SerializeField] AudioClip click;

    AudioSource _src;

    void Awake()
    {
        _src = GetComponent<AudioSource>();
        _src.playOnAwake = false; _src.loop = false; _src.spatialBlend = 0f; // 2D
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hover) _src.PlayOneShot(hover);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (click) _src.PlayOneShot(click);
    }

    // Se preferir usar o OnClick do Button no Inspector:
    public void PlayClick() { if (click) _src.PlayOneShot(click); }
    public void PlayHover() { if (hover) _src.PlayOneShot(hover); }
}
