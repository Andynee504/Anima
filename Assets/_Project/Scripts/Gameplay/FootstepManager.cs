using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FootstepManager : MonoBehaviour
{
    [Header("Input (XR Move)")]
    [SerializeField] InputActionReference moveAction; // Vector2 do locomotion
    [SerializeField, Range(0f, 1f)] float inputThreshold = 0.15f; // só considera "andar" acima disso

    [Header("Perfis")]
    [SerializeField] FootstepProfile defaultProfile;
    FootstepProfile _activeProfile;

    [Header("Áudio")]
    [SerializeField] AudioSource oneShotSource; // 2D ou levemente 3D no pé
    [SerializeField] bool spatialize = false;

    CharacterController _cc;
    Vector3 _lastRootPos;
    float _stepTimer = 0f;
    float _stepDistanceAccum = 0f;
    const float kMinSpeedForSteps = 0.05f; // m/s

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (!oneShotSource) oneShotSource = gameObject.AddComponent<AudioSource>();
        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.spatialBlend = spatialize ? 1f : 0f;

        _activeProfile = defaultProfile;
        _lastRootPos = transform.position;
    }

    void OnEnable()  { moveAction?.action.Enable(); }
    void OnDisable() { moveAction?.action.Disable(); }

    void Update()
    {
        if (_activeProfile == null || _activeProfile.clips == null || _activeProfile.clips.Length == 0) return;

        // 1) É caminhada? (ignora olhar/strafe de cabeça)
        Vector2 move = moveAction ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        bool isWalkingInput = move.sqrMagnitude >= inputThreshold * inputThreshold;

        // 2) Velocidade do root (CharacterController) e chão
        float speed = new Vector3(_cc.velocity.x, 0, _cc.velocity.z).magnitude;
        bool grounded = _cc.isGrounded;

        // 3) Se não está andando de verdade, reseta o cronômetro devagar
        if (!isWalkingInput || !grounded || speed < kMinSpeedForSteps)
        {
            _lastRootPos = transform.position;
            return;
        }

        // 4) Acumula distância horizontal
        Vector3 now = transform.position;
        Vector3 delta = now - _lastRootPos; delta.y = 0;
        _stepDistanceAccum += delta.magnitude;
        _lastRootPos = now;

        // 5) Intervalo escalado pela velocidade (mais rápido, menor intervalo)
        float scale = _activeProfile.speedToInterval.Evaluate(speed);
        float interval = Mathf.Max(0.05f, _activeProfile.baseInterval * scale);

        _stepTimer += Time.deltaTime;
        if (_stepTimer >= interval)
        {
            _stepTimer = 0f;
            _stepDistanceAccum = 0f;
            PlayFootstep(_activeProfile);
        }
    }

    void PlayFootstep(FootstepProfile p)
    {
        var clip = p.clips[Random.Range(0, p.clips.Length)];
        oneShotSource.volume = Random.Range(p.volumeJitter.x, p.volumeJitter.y);
        oneShotSource.pitch  = Random.Range(p.pitchJitter.x,  p.pitchJitter.y);
        oneShotSource.PlayOneShot(clip);
    }

    // Chamado por zonas (abaixo)
    public void ApplyProfile(FootstepProfile profile)   => _activeProfile = profile ? profile : defaultProfile;
    public void ClearProfile(FootstepProfile fallback)  => _activeProfile = fallback ? fallback : defaultProfile;
}
