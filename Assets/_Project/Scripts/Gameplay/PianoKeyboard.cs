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
        public string keyName;                   // ex.: C4, D#4
        public XRBaseInteractable key;          // objeto da tecla

        // NOVO — reprodução com sustain
        public AudioClip attackClip;            // início curto
        public AudioClip sustainLoopClip;       // trecho que loopa enquanto segura
        public AudioClip releaseClip;           // cauda ao soltar

        // LEGADO — fallback (one-shot). Se ALR vazios, usa 'clip'
        public AudioClip clip;

        [Range(0.5f, 2f)] public float pitch;   // 1 = original
        [Range(0f, 1f)] public float volumeOverride; // 0 = usa volume global
    }

    [Header("Teclas")]
    public KeyBinding[] keys;

    [Header("Áudio Global")]
    public AudioMixerGroup mixerGroup;
    [Range(0f, 1f)] public float volume = 1f;
    public bool spatialize3D = true;
    [Min(0.1f)] public float maxDistance = 15f;

    // === Campos do script original ===
    AudioSource source; // global (fallback p/ one-shot)
    readonly Dictionary<XRBaseInteractable, int> map = new();

    // === NOVO: runtime por tecla ===
    class RuntimeKey
    {
        public AudioSource oneShot;  // attack/release (ou clip legado)
        public AudioSource loop;     // sustain (loop)
        public bool held;
    }
    readonly Dictionary<XRBaseInteractable, RuntimeKey> runtime = new();

    // ---------- MANTIDOS DO SEU SCRIPT ----------
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
        SubscribeAll();            // mantém o fluxo original
        BuildRuntimeSources();     // prepara os audios por tecla (corrigido)
    }

    void OnDisable()
    {
        UnsubscribeAll();
        map.Clear();

        foreach (var rk in runtime.Values)
        {
            if (rk.oneShot) rk.oneShot.Stop();
            if (rk.loop) rk.loop.Stop();
        }
        runtime.Clear();
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
            if (k.key == null) continue;

            if (!map.ContainsKey(k.key))
            {
                map.Add(k.key, i);

                // MANTIDO do original
                k.key.selectEntered.AddListener(OnKeyPressed);

                // NOVO — suporte a hold (Select e Activate)
                k.key.selectExited.AddListener(OnKeyReleased);
                k.key.activated.AddListener(OnKeyActivated);
                k.key.deactivated.AddListener(OnKeyDeactivated);
            }
        }
    }

    void UnsubscribeAll()
    {
        foreach (var pair in map)
        {
            var ib = pair.Key;
            if (ib == null) continue;

            // MANTIDO do original
            ib.selectEntered.RemoveListener(OnKeyPressed);

            // NOVO
            ib.selectExited.RemoveListener(OnKeyReleased);
            ib.activated.RemoveListener(OnKeyActivated);
            ib.deactivated.RemoveListener(OnKeyDeactivated);
        }
    }

    // ---------- LEGADO: compatível com seu fluxo atual ----------
    // Dispara nota com o evento SelectEnter (ex.: Poke). Agora redireciona para NoteOn.
    void OnKeyPressed(SelectEnterEventArgs args)
    {
        var key = args.interactableObject as XRBaseInteractable;
        NoteOn(key, preferOneShotIfLegacy: true);
    }

    // ---------- NOVOS handlers ----------
    void OnKeyReleased(SelectExitEventArgs args)
    {
        var key = args.interactableObject as XRBaseInteractable;
        NoteOff(key);
    }

    void OnKeyActivated(ActivateEventArgs args)
    {
        var key = args.interactableObject as XRBaseInteractable;
        NoteOn(key, preferOneShotIfLegacy: false); // em Activate, assume “tocar e segurar”
    }

    void OnKeyDeactivated(DeactivateEventArgs args)
    {
        var key = args.interactableObject as XRBaseInteractable;
        NoteOff(key);
    }

    // ---------- Lógica de nota ----------
    void NoteOn(XRBaseInteractable key, bool preferOneShotIfLegacy)
    {
        if (key == null) return;
        if (!map.TryGetValue(key, out int idx)) return;

        var binding = keys[idx];
        var rk = GetOrCreateRuntime(key);

        if (rk.held) return;
        rk.held = true;

        float pit = Mathf.Clamp(binding.pitch <= 0f ? 1f : binding.pitch, 0.5f, 2f);
        float vol = binding.volumeOverride > 0f ? binding.volumeOverride : volume;

        bool hasALR = binding.attackClip || binding.sustainLoopClip || binding.releaseClip;

        if (hasALR)
        {
            // ATTACK
            if (binding.attackClip)
            {
                rk.oneShot.pitch = pit;
                rk.oneShot.PlayOneShot(binding.attackClip, vol);
            }
            // SUSTAIN LOOP
            if (binding.sustainLoopClip)
            {
                rk.loop.clip = binding.sustainLoopClip;
                rk.loop.pitch = pit;
                rk.loop.volume = vol;
                rk.loop.loop = true;
                rk.loop.Play();
            }
        }
        else
        {
            // Fallback LEGADO: usa 'clip' one-shot
            if (binding.clip)
            {
                if (preferOneShotIfLegacy)
                {
                    ApplySourceDefaults();
                    source.pitch = pit;
                    source.PlayOneShot(binding.clip, vol);
                    rk.held = false; // não há sustain
                }
                else
                {
                    // Em “Activate” segurado, simula segurar repetindo discretamente
                    rk.loop.clip = binding.clip;
                    rk.loop.pitch = pit;
                    rk.loop.volume = vol;
                    rk.loop.loop = true;
                    rk.loop.Play();
                }
            }
            else
            {
                rk.held = false;
            }
        }
    }

    void NoteOff(XRBaseInteractable key)
    {
        if (key == null) return;
        if (!map.TryGetValue(key, out int idx)) return;

        var binding = keys[idx];
        var rk = GetOrCreateRuntime(key);
        if (!rk.held) return;
        rk.held = false;

        // Para sustain (se houver)
        if (rk.loop && rk.loop.isPlaying) rk.loop.Stop();

        // RELEASE tail (se houver)
        if (binding.releaseClip)
        {
            float pit = Mathf.Clamp(binding.pitch <= 0f ? 1f : binding.pitch, 0.5f, 2f);
            float vol = binding.volumeOverride > 0f ? binding.volumeOverride : volume;
            rk.oneShot.pitch = pit;
            rk.oneShot.PlayOneShot(binding.releaseClip, vol);
        }
    }

    RuntimeKey GetOrCreateRuntime(XRBaseInteractable key)
    {
        if (runtime.TryGetValue(key, out var rk) && rk.oneShot && rk.loop) return rk;

        rk = new RuntimeKey();
        rk.oneShot = CreateChildSource(key.transform, key.name + "_OneShot", loop: false);
        rk.loop = CreateChildSource(key.transform, key.name + "_Loop", loop: true);
        runtime[key] = rk;
        return rk;
    }

    AudioSource CreateChildSource(Transform parent, string name, bool loop)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.outputAudioMixerGroup = mixerGroup;
        s.spatialBlend = spatialize3D ? 1f : 0f;
        s.maxDistance = maxDistance;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.volume = volume;
        return s;
    }

    // NOVO — prepara (ou reaproveita) os AudioSources por tecla na ativação
    void BuildRuntimeSources()
    {
        runtime.Clear();
        if (keys == null) return;

        foreach (var kb in keys)
        {
            if (!kb.key) continue;
            var rk = new RuntimeKey();
            rk.oneShot = CreateChildSource(kb.key.transform, kb.key.name + "_OneShot", loop: false);
            rk.loop = CreateChildSource(kb.key.transform, kb.key.name + "_Loop", loop: true);
            runtime[kb.key] = rk;
        }
    }

    // ---------- MANTIDO (ContextMenu) ----------
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
                // novos campos ficam nulos por padrão
                attackClip = null,
                sustainLoopClip = null,
                releaseClip = null,
                clip = null, // legado
                pitch = 1f,
                volumeOverride = 0f
            });
        }
        keys = found.ToArray();
    }
}
