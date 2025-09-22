using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class InteractableAudio : MonoBehaviour
{
    [Header("Eventos (checkbox)")]
    public bool playOnHoverEnter = false;
    public bool playOnGrab = true;
    public bool playWhileHeldLoop = false;
    public bool playOnRelease = true;
    public bool alsoOnActivate = false; // opcional

    [Header("Clips")]
    public AudioClip hoverEnterClip;
    public AudioClip grabClip;
    public AudioClip holdLoopClip;
    public AudioClip releaseClip;

    [Header("Áudio Base")]
    public AudioMixerGroup mixerGroup; // arraste SFX se tiver
    public bool spatialize3D = true;
    [Range(0f, 1f)] public float volume = 1f;
    [Min(0.1f)] public float maxDistance = 12f;

    XRGrabInteractable grab;
    AudioSource source;

    void Reset()
    {
        var s = GetComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = false;
        s.spatialBlend = 1f;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.minDistance = 0.1f;
        s.maxDistance = 12f;
    }

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        if (!grab) Debug.LogError("[InteractableAudio] Requer XRGrabInteractable.");
        source = GetComponent<AudioSource>();
        ApplySourceDefaults();
    }

    void OnEnable()
    {
        if (grab == null) return;
        grab.hoverEntered.AddListener(OnHoverEntered);
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
        grab.activated.AddListener(OnActivated);
    }

    void OnDisable()
    {
        if (grab == null) return;
        grab.hoverEntered.RemoveListener(OnHoverEntered);
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
        grab.activated.RemoveListener(OnActivated);
        StopHoldLoop();
    }

    void ApplySourceDefaults()
    {
        if (!source) return;
        source.outputAudioMixerGroup = mixerGroup;
        source.spatialBlend = spatialize3D ? 1f : 0f;
        source.volume = volume;
        source.maxDistance = maxDistance;
    }

    void OnHoverEntered(HoverEnterEventArgs _)
    {
        if (!playOnHoverEnter || !hoverEnterClip) return;
        ApplySourceDefaults();
        source.PlayOneShot(hoverEnterClip, volume);
    }

    void OnGrab(SelectEnterEventArgs _)
    {
        ApplySourceDefaults();

        if (playOnGrab && grabClip)
            source.PlayOneShot(grabClip, volume);

        if (playWhileHeldLoop && holdLoopClip)
            StartHoldLoop();
    }

    void OnRelease(SelectExitEventArgs _)
    {
        if (playWhileHeldLoop) StopHoldLoop();

        if (playOnRelease && releaseClip)
        {
            ApplySourceDefaults();
            source.PlayOneShot(releaseClip, volume);
        }
    }

    void OnActivated(ActivateEventArgs _)
    {
        if (!alsoOnActivate) return;
        // reaproveita o "grab" como feedback de activate
        if (grabClip)
        {
            ApplySourceDefaults();
            source.PlayOneShot(grabClip, volume);
        }
    }

    void StartHoldLoop()
    {
        if (!source || !holdLoopClip) return;
        source.clip = holdLoopClip;
        source.loop = true;
        ApplySourceDefaults();
        source.Play();
    }

    void StopHoldLoop()
    {
        if (!source) return;
        if (source.loop) source.Stop();
        source.loop = false;
        source.clip = null;
    }
}
