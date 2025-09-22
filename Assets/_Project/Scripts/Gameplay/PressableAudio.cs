using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class PressableAudio : MonoBehaviour
{
    public enum TriggerMode { OnSelect, OnActivate }

    [Header("Disparo")]
    public TriggerMode trigger = TriggerMode.OnSelect;

    [Header("Áudio")]
    public AudioClip pressClip;
    public AudioMixerGroup mixerGroup;
    [Range(0f, 1f)] public float volume = 1f;
    public bool spatialize3D = true;
    [Min(0.1f)] public float maxDistance = 10f;

    XRBaseInteractable interactable;
    AudioSource source;

    void Reset()
    {
        var s = GetComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = false;
        s.spatialBlend = 1f;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.minDistance = 0.1f;
        s.maxDistance = 10f;
    }

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        if (!interactable) Debug.LogError("[PressableAudio] Requer XRBaseInteractable.");
        source = GetComponent<AudioSource>();
        ApplySourceDefaults();
    }

    void OnEnable()
    {
        if (interactable == null) return;

        if (trigger == TriggerMode.OnSelect)
            interactable.selectEntered.AddListener(OnPressedSelect);
        else
            interactable.activated.AddListener(OnPressedActivate);
    }

    void OnDisable()
    {
        if (interactable == null) return;

        if (trigger == TriggerMode.OnSelect)
            interactable.selectEntered.RemoveListener(OnPressedSelect);
        else
            interactable.activated.RemoveListener(OnPressedActivate);
    }

    void ApplySourceDefaults()
    {
        if (!source) return;
        source.outputAudioMixerGroup = mixerGroup;
        source.spatialBlend = spatialize3D ? 1f : 0f;
        source.volume = volume;
        source.maxDistance = maxDistance;
    }

    void Play()
    {
        if (!pressClip || !source) return;
        ApplySourceDefaults();
        source.PlayOneShot(pressClip, volume);
    }

    void OnPressedSelect(SelectEnterEventArgs _) => Play();
    void OnPressedActivate(ActivateEventArgs _) => Play();
}
