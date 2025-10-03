using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Footstep Profile")]
public class FootstepProfile : ScriptableObject
{
    [Header("Clipes de passo")]
    public AudioClip[] clips;

    [Header("Dinâmica")]
    [Tooltip("Intervalo base entre passos em segundos (em velocidade normal).")]
    public float baseInterval = 0.5f;
    [Tooltip("Multiplicador de ainda mais rápido ao correr (usa velocidade). 1 = mantém base.")]
    public AnimationCurve speedToInterval = AnimationCurve.Linear(0, 1, 4, 0.5f);

    [Header("Variação")]
    public Vector2 volumeJitter = new Vector2(0.9f, 1.0f);
    public Vector2 pitchJitter  = new Vector2(0.95f, 1.05f);
}
