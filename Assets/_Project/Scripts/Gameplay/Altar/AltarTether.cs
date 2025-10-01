using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class AltarTether : MonoBehaviour
{
    [Header("Alvo (slot do altar)")]
    public Transform target;

    [Header("Movimento ao alvo")]
    public float moveSpeed = 4f;
    public bool alignRotation = true;
    public float stopDistance = 0.002f;

    [Header("Estabilização")]
    [Tooltip("Força cinemático sem gravidade enquanto ancorado no altar (não segurado).")]
    public bool forceKinematicWhileTethered = true;
    [Tooltip("Desliga ao sair do trigger do altar.")]
    public bool releaseOnExit = false;

    [Header("Efeitos")]
    public float bobAmplitude = 0.02f;
    public float bobFrequency = 0.6f;
    public float spinDegreesPerSec = 35f;
    public Vector3 spinAxis = Vector3.up;

    Rigidbody _rb;
    XRGrabInteractable _grab;
    bool _isGrabbed;
    bool _forcingActive = false;
    bool _prefKinematic, _prefUseGravity; // estado padrão do prefab
    bool _prevKinematic, _prevUseGravity; // estado antes de forçar
    float _t0;
    Coroutine _deferReleaseCo;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _grab = GetComponent<XRGrabInteractable>();
        _t0 = Random.value * 10f;

        if (_grab)
        {
            _grab.selectEntered.AddListener(OnSelectEntered);
            _grab.selectExited.AddListener(OnSelectExited);
        }

        // Guarda defaults do prefab (fora do altar)
        if (_rb)
        {
            _prefKinematic = _rb.isKinematic;
            _prefUseGravity = _rb.useGravity;
        }
    }

    void OnDestroy()
    {
        if (_grab)
        {
            _grab.selectEntered.RemoveListener(OnSelectEntered);
            _grab.selectExited.RemoveListener(OnSelectExited);
        }
    }

    void OnEnable() { UpdatePhysicsState(); }
    void OnDisable() { RestorePhysicsIfForced(); }

    public void SetTarget(Transform t, float speed, bool alignRot)
    {
        target = t;
        moveSpeed = speed;
        alignRotation = alignRot;
        UpdatePhysicsState();
    }

    public void ClearTarget()
    {
        target = null;
        UpdatePhysicsState();
    }

    void OnSelectEntered(SelectEnterEventArgs _)
    {
        _isGrabbed = true;

        // Enquanto estiver na mão, NUNCA force física nem target.
        target = null;                 // evita reancorar no mesmo frame
        RestorePhysicsToPrefab();      // deixa como o prefab define
        _forcingActive = false;
    }

    void OnSelectExited(SelectExitEventArgs _)
    {
        _isGrabbed = false;

        // Espera 1 frame: dá tempo do Magnet recolocar o target se o item estiver no trigger.
        if (_deferReleaseCo != null) StopCoroutine(_deferReleaseCo);
        _deferReleaseCo = StartCoroutine(CoDeferredUpdate());
    }

    IEnumerator CoDeferredUpdate()
    {
        yield return null; // 1 frame
        UpdatePhysicsState(); // agora já sabemos se há target (no altar) ou não
    }

    void UpdatePhysicsState()
    {
        if (!_rb) return;

        bool shouldForce = forceKinematicWhileTethered && target && !_isGrabbed;

        if (shouldForce)
        {
            if (!_forcingActive)
            {
                _prevKinematic = _rb.isKinematic;
                _prevUseGravity = _rb.useGravity;
                _forcingActive = true;
            }
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }
        else
        {
            RestorePhysicsIfForced();
            // Fora do altar (sem target) ou na mão: garante defaults do prefab
            if (!target || _isGrabbed) RestorePhysicsToPrefab();
        }
    }

    void RestorePhysicsIfForced()
    {
        if (!_rb || !_forcingActive) return;
        _rb.isKinematic = _prevKinematic;
        _rb.useGravity = _prevUseGravity;
        _forcingActive = false;
    }

    void RestorePhysicsToPrefab()
    {
        if (!_rb) return;
        _rb.isKinematic = _prefKinematic;
        _rb.useGravity = _prefUseGravity;
    }

    void LateUpdate()
    {
        if (!target || _isGrabbed) return;

        Vector3 basePos = target.position;

        if (bobAmplitude > 0f && bobFrequency > 0f)
        {
            float y = Mathf.Sin((Time.time + _t0) * (Mathf.PI * 2f) * bobFrequency) * bobAmplitude;
            basePos += Vector3.up * y;
        }

        Vector3 cur = transform.position;
        float dist = Vector3.Distance(cur, basePos);

        if (dist > stopDistance)
        {
            float k = 1f - Mathf.Exp(-moveSpeed * Time.deltaTime);
            Vector3 next = Vector3.Lerp(cur, basePos, k);
            if (_rb && !_rb.isKinematic) _rb.MovePosition(next);
            else transform.position = next;
        }
        else
        {
            transform.position = basePos;
        }

        Quaternion targetRot = alignRotation ? target.rotation : transform.rotation;

        if (spinDegreesPerSec != 0f && spinAxis.sqrMagnitude > 0f)
        {
            targetRot = Quaternion.AngleAxis(spinDegreesPerSec * Time.deltaTime, spinAxis.normalized) * targetRot;
        }

        if (_rb && !_rb.isKinematic) _rb.MoveRotation(targetRot);
        else transform.rotation = targetRot;
    }
}