using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class PianoKeyboard : MonoBehaviour
{
    [Serializable]
    public struct KeyBinding
    {
        public string keyName; // ex.: C4, D#4
        public XRBaseInteractable key; // objeto da tecla
        public AudioClip clip; // som da tecla
        [Range(0.5f, 2f)] public float pitch; // 1 = original
        [Range(0f, 1f)] public float volumeOverride; // 0 = usa volume global
    }

    [Header("Teclas")]
    public KeyBinding[] keys;

    [Header("Áudio Global")]
    public AudioMixerGroup mixerGroup;
    [Range(0f, 1f)] public float volume = 1f;
    public bool spatialize3D = true;
    [Min(0.1f)] public float maxDistance = 15f;

    AudioSource source;
    readonly Dictionary<XRBaseInteractable, int> map = new();

    void Reset()
    {
        var s = GetComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = false;
        s.spatialBlend = 1f;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.minDistance = 0.1f;
        s.maxDistance = 15f;
    }

    void Awake()
    {
        source = GetComponent<AudioSource>();
        ApplySourceDefaults();
    }

    void OnEnable()
    {
        SubscribeAll();
    }

    void OnDisable()
    {
        UnsubscribeAll();
        map.Clear();
    }

    void ApplySourceDefaults()
    {
        if (!source) return;
        source.outputAudioMixerGroup = mixerGroup;
        source.spatialBlend = spatialize3D ? 1f : 0f;
        source.volume = volume;
        source.maxDistance = maxDistance;
    }

    void SubscribeAll()
    {
        UnsubscribeAll();
        map.Clear();
        if (keys == null) return;

        for (int i = 0; i < keys.Length; i++)
        {
            var k = keys[i];
            if (k.key == null || k.clip == null) continue;

            if (!map.ContainsKey(k.key))
            {
                map.Add(k.key, i);
                // prefira um caminho. se quiser Activate, adicione aqui também.
                k.key.selectEntered.AddListener(OnKeyPressed);
            }
        }
    }

    void UnsubscribeAll()
    {
        foreach (var pair in map)
        {
            var ib = pair.Key;
            if (ib != null) ib.selectEntered.RemoveListener(OnKeyPressed);
        }
    }

    void OnKeyPressed(SelectEnterEventArgs args)
    {
        if (!source) return;
        var key = args.interactableObject as XRBaseInteractable;
        if (key == null) return;
        if (!map.TryGetValue(key, out int idx)) return;

        var binding = keys[idx];
        source.pitch = binding.pitch <= 0f ? 1f : Mathf.Clamp(binding.pitch, 0.5f, 2f);
        var vol = binding.volumeOverride > 0f ? binding.volumeOverride : volume;
        ApplySourceDefaults();
        source.PlayOneShot(binding.clip, vol);
    }

    [ContextMenu("Auto-Populate from children")]
    void AutoPopulate()
    {
        var found = new List<KeyBinding>();
        foreach (Transform child in transform)
        {
            var ib = child.GetComponent<XRBaseInteractable>();
            if (ib == null) continue;
            found.Add(new KeyBinding
            {
                keyName = child.name,
                key = ib,
                clip = null, // preencha manualmente depois
                pitch = 1f,
                volumeOverride = 0f
            });
        }
        keys = found.ToArray();
    }
}
