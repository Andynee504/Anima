using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MusicZone : MonoBehaviour
{
    [Header("Detecção")]
    [SerializeField] string playerTag = "Player"; // tag do player

    [Header("Áudio")]
    [SerializeField] AudioSource source; // Arraste um AudioSource opcional
    [SerializeField] AudioClip music; // Arraste a musica aqui
    [SerializeField] bool loop = true; // Manter em loop
    [SerializeField] float fadeInSeconds = 0f; // Fade In
    [SerializeField] float fadeOutSeconds = 0f; // Fade Out
    [SerializeField] bool stopOnExit = true; // Parar quando sair do trigger

    float _targetVolume = 0.2f; // volume desejado
    Coroutine _fadeRoutine; // rotina atual de fade

    void Reset()
    {
        // garantir que o collider seja trigger
        var col = GetComponent<Collider>(); col.isTrigger = true; // forca Trigger
    }

    void Awake()
    {
        if (!source) source = GetComponent<AudioSource>(); // tenta pegar no mesmo GO se ausente?
        if (!source) source = gameObject.AddComponent<AudioSource>(); // (Debug) cria se nao existir

        source.playOnAwake = false; // nao tocar ao iniciar
        source.loop = loop; // loop on/off
        source.clip = music; // define a musica
        source.spatialBlend = 0.5f; // 2d === 0f ; 3d === 1f
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return; // confirma condicao de player
        if (!source || !music) return; // sem audio, sem roger, sem festa

        // aplicat clip/loop caso altere em runtime
        source.clip = music; source.loop = loop; // atualiza propriedades
        StartFadeIn(); // inicia fade-in (ou play direto)
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (!source) return;

        if (stopOnExit) StartFadeOut(); // faz fade-out e para
    }

    // ---- Fades --------------------------------------------------------------

    void StartFadeIn()
    {
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine); // cancela fade anterior
        _fadeRoutine = StartCoroutine(FadeIn()); // inicia novo fade-in
    }

    void StartFadeOut()
    {
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine); // cancela fade anterior
        _fadeRoutine = StartCoroutine(FadeOut()); // inicia fade-out
    }

    IEnumerator FadeIn()
    {
        float dur = Mathf.Max(0f, fadeInSeconds); // duracao minima 0
        float startVol = 0f; // comeca silencioso
        float endVol = _targetVolume; // objetivo
        source.volume = 0f; // zera volume
        if (!source.isPlaying) source.Play(); // toca se ainda nao estiver tocando

        if (dur == 0f) { source.volume = endVol; yield break; } // sem fade

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime; // acumula tempo
            source.volume = Mathf.Lerp(startVol, endVol, t / dur); // interpola
            yield return null; // espera proximo frame
        }
        source.volume = endVol; // garante volume final
    }

    IEnumerator FadeOut()
    {
        float dur = Mathf.Max(0f, fadeOutSeconds); // duracao minima 0
        float startVol = source.volume; // volume atual
        float endVol = 0f; // objetivo

        if (dur == 0f) { source.volume = 0f; source.Stop(); yield break; } // sem fade

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime; // acumula tempo
            source.volume = Mathf.Lerp(startVol, endVol, t / dur); // interpola
            yield return null; // proximo frame
        }
        source.volume = 0f; // zera volume
        source.Stop(); // para a musica
    }
}
