using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
public class OrbitalMotion : MonoBehaviour
{
    [Header("Orbit (principal)")]
    public Transform center;
    public float radius = 10f;                  // m
    public float angularSpeedDeg = 20f;         // graus/s
    public Vector3 axis = Vector3.up;           // eixo da órbita
    public float bobAmplitude = 0.3f;           // oscilação ao longo do eixo
    public float bobFrequency = 0.2f;           // Hz

    [Header("Despawn")]
    public float despawnDistance = 60f;         // distância do centro para destruir

    [Header("Recapture (opcional)")]
    public bool recaptureWhenNear = true;
    public float recaptureSpeedThreshold = 0.6f;
    public float recaptureMaxDistanceFromIdeal = 1.2f;

    [Header("Grab")]
    public bool enableGrab = true;              // desative para “matar” o grab sem remover componente

    [Header("Self Spin")]
    public float selfSpinDegPerSec = 45f;       // rotação própria (graus/s)
    public Vector3 selfSpinAxis = Vector3.up;   // eixo do self-spin
    public bool randomizeSelfSpinAxisOnInit = true;          // randomiza eixo ao nascer
    [Range(0f, 1f)] public float selfSpinSpeedJitterPct = 0.10f; // ±10%
    public bool randomizeSelfSpinDirection = true;           // inverte direção aleatoriamente

    [Header("Local Orbit (epicycle)")]
    public float localOrbitRadius = 0.0f;       // 0 = desligado
    public float localOrbitSpeedDeg = 0.0f;     // graus/s

    [Header("Desync por instância")]
    public bool randomizeAxisOnInit = true;                    // randomiza eixo no Init (se Manager não setar)
    [Range(0f, 360f)] public float startAngleJitterDeg = 180f; // fase inicial
    [Range(0f, 0.5f)] public float speedJitterPct = 0.08f;     // ±8%
    public bool randomizeBaseDir = true;                       // direção base única

    [HideInInspector] public OrbitManager manager;

    // ---- internos ----
    Rigidbody rb;
    XRGrabInteractable grab;
    float angleDeg;                 // ângulo atual na órbita principal
    float bobPhase;                 // fase do bob
    bool orbitEnabled = true;
    Vector3 orbitBaseDir;           // base perpendicular única por instância

    // epiciclo
    float localAngle;
    Vector3 localBaseDir;

    // visibilidade/apresentação
    Renderer[] _renderers;
    Collider[] _colliders;
    Coroutine _fadeCo;
    float _currentAlpha = 1f;

    float GroupSpeed => manager ? manager.SpeedMultiplier : 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();
        if (!grab) grab = GetComponentInChildren<XRGrabInteractable>(true);

        _renderers = GetComponentsInChildren<Renderer>(true);
        _colliders = GetComponentsInChildren<Collider>(true);

        if (grab)
        {
            grab.enabled = enableGrab;
            grab.selectEntered.AddListener(OnSelectEntered);
            grab.selectExited.AddListener(OnSelectExited);
        }
    }

    public void InitFromManager(OrbitManager m, Transform orbitCenter, float startAngleDeg, float setRadius, float setAngularSpeedDeg, float setDespawnDistance)
    {
        manager = m;
        center = orbitCenter;
        angleDeg = startAngleDeg;
        radius = setRadius;
        angularSpeedDeg = setAngularSpeedDeg;
        despawnDistance = setDespawnDistance;
        bobPhase = Random.Range(0f, Mathf.PI * 2f);

        // kinematic para posicionamento limpo
        rb.isKinematic = true;
        orbitEnabled = true;

        // 1) Randomiza eixo primeiro — MAS só se ninguém setou um eixo válido antes
        if (randomizeAxisOnInit)
        {
            if (axis.sqrMagnitude < 0.9f) // considera “não definido”
            {
                axis = Random.onUnitSphere;
                if (axis.sqrMagnitude < 1e-4f) axis = Vector3.up;
                axis.Normalize();
            }
        }

        // 2) Normal do eixo atual
        Vector3 nAxis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;

        // 3) Base perpendicular única para esta instância (órbita principal)
        if (randomizeBaseDir)
        {
            var any = Random.onUnitSphere;
            orbitBaseDir = Vector3.Cross(nAxis, any);
            if (orbitBaseDir.sqrMagnitude < 1e-4f) orbitBaseDir = Vector3.right;
            orbitBaseDir.Normalize();
        }
        else
        {
            orbitBaseDir = Vector3.Cross(nAxis, Vector3.right);
            if (orbitBaseDir.sqrMagnitude < 1e-4f) orbitBaseDir = Vector3.Cross(nAxis, Vector3.forward);
            orbitBaseDir.Normalize();
        }

        // 4) Base local para o epiciclo
        localBaseDir = Vector3.Cross(nAxis, Random.insideUnitSphere);
        if (localBaseDir.sqrMagnitude < 1e-4f) localBaseDir = Vector3.right;
        localBaseDir.Normalize();
        localAngle = Random.Range(0f, 360f);

        // 5) RANDOMIZAÇÃO DO SELF SPIN
        if (randomizeSelfSpinAxisOnInit)
        {
            selfSpinAxis = Random.onUnitSphere;
            if (selfSpinAxis.sqrMagnitude < 1e-4f) selfSpinAxis = Vector3.up;
            selfSpinAxis.Normalize();
        }
        if (selfSpinDegPerSec != 0f && selfSpinSpeedJitterPct > 0f)
        {
            selfSpinDegPerSec *= Random.Range(1f - selfSpinSpeedJitterPct, 1f + selfSpinSpeedJitterPct);
        }
        if (randomizeSelfSpinDirection && selfSpinDegPerSec != 0f && Random.value < 0.5f)
        {
            selfSpinDegPerSec = -selfSpinDegPerSec;
        }

        // 6) Jitters ANTES da 1ª colocação
        angleDeg += Random.Range(-startAngleJitterDeg, startAngleJitterDeg);
        angularSpeedDeg *= Random.Range(1f - speedJitterPct, 1f + speedJitterPct);

        // 7) Posiciona já com tudo randomizado
        UpdateOrbitTransform(0f, force: true);
    }

    void Update()
    {
        if (!center) return;

        if (orbitEnabled && rb.isKinematic)
        {
            UpdateOrbitTransform(Time.deltaTime);
        }

        // Despawn
        float dist = Vector3.Distance(transform.position, center.position);
        if (dist > despawnDistance)
        {
            manager?.RequestRespawnAndDestroy(this);
        }
    }

    void UpdateOrbitTransform(float dt, bool force = false)
    {
        if (dt > 0f || force)
        {
            angleDeg += angularSpeedDeg * GroupSpeed * dt;
            localAngle += localOrbitSpeedDeg * dt;
        }

        Vector3 nAxis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;

        // posição na órbita principal
        Quaternion rot = Quaternion.AngleAxis(angleDeg, nAxis);
        Vector3 radial = rot * orbitBaseDir * radius;

        // bob ao longo do eixo
        float bob = Mathf.Sin((Time.time + bobPhase) * (Mathf.PI * 2f) * bobFrequency) * bobAmplitude;

        // epiciclo (mini-translação circular local)
        Vector3 localOffset = Vector3.zero;
        if (localOrbitRadius > 0f && localOrbitSpeedDeg != 0f)
        {
            Quaternion lrot = Quaternion.AngleAxis(localAngle, nAxis);
            localOffset = lrot * localBaseDir * localOrbitRadius;
        }

        Vector3 targetPos = center.position + radial + nAxis * bob + localOffset;
        transform.position = targetPos;

        // self spin
        if (selfSpinDegPerSec != 0f)
        {
            transform.Rotate(selfSpinAxis, selfSpinDegPerSec * dt, Space.Self);
        }
    }

    void OnSelectEntered(SelectEnterEventArgs _)
    {
        orbitEnabled = false;
        rb.isKinematic = false;
    }

    void OnSelectExited(SelectExitEventArgs _)
    {
        if (recaptureWhenNear) StartCoroutine(TryRecaptureRoutine());
    }

    IEnumerator TryRecaptureRoutine()
    {
        yield return new WaitForSeconds(0.2f);
        float t = 0f;
        while (t < 6f)
        {
            t += Time.deltaTime;
            if (!center) yield break;

            Vector3 nAxis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;

            Vector3 fromCenter = transform.position - center.position;
            Vector3 planeProj = Vector3.ProjectOnPlane(fromCenter, nAxis);

            if (planeProj.sqrMagnitude > 0.01f)
            {
                float currentRadius = planeProj.magnitude;
                float radiusDelta = Mathf.Abs(currentRadius - radius);

                float speed = rb.linearVelocity.magnitude; // Unity 6000
                if (speed < recaptureSpeedThreshold && radiusDelta < recaptureMaxDistanceFromIdeal)
                {
                    // reanexa em órbita
                    float signed = Vector3.SignedAngle(orbitBaseDir, planeProj.normalized, nAxis);

                    angleDeg = signed;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                    orbitEnabled = true;
                    yield break;
                }
            }
            yield return null;
        }
    }

    // --- Visibilidade com Fade ---
    public void SetPresentationVisible(bool visible, bool disableCollisionToo = false, float fadeSeconds = 0.25f)
    {
        // interação XR (grab)
        if (grab) grab.enabled = visible && enableGrab;

        // colisão (opcional)
        if (disableCollisionToo && _colliders != null)
            for (int i = 0; i < _colliders.Length; i++) _colliders[i].enabled = visible;

        // fade visual
        if (_renderers == null || _renderers.Length == 0)
            return;

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeRoutine(visible ? 1f : 0f, fadeSeconds));
    }

    IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        if (duration <= 0f)
        {
            ApplyAlphaToAll(targetAlpha);
            _currentAlpha = targetAlpha;
            yield break;
        }

        float start = _currentAlpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float a = Mathf.Lerp(start, targetAlpha, Mathf.SmoothStep(0f, 1f, k));
            ApplyAlphaToAll(a);
            yield return null;
        }
        ApplyAlphaToAll(targetAlpha);
        _currentAlpha = targetAlpha;
    }

    void ApplyAlphaToAll(float a)
    {
        for (int r = 0; r < _renderers.Length; r++)
        {
            var rend = _renderers[r];
            var mats = rend.materials; // instancia materiais (cópia)
            for (int m = 0; m < mats.Length; m++)
            {
                var mat = mats[m];
                if (!mat) continue;

                if (mat.HasProperty("_BaseColor"))
                {
                    var c = mat.GetColor("_BaseColor"); c.a = a; mat.SetColor("_BaseColor", c);
                }
                else if (mat.HasProperty("_Color"))
                {
                    var c = mat.GetColor("_Color"); c.a = a; mat.SetColor("_Color", c);
                }
            }
        }
        // Importante: para o fade funcionar, os materiais devem estar em modo Transparent/Fade.
    }

    void OnDestroy()
    {
        if (grab)
        {
            grab.selectEntered.RemoveListener(OnSelectEntered);
            grab.selectExited.RemoveListener(OnSelectExited);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!center) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center.position, radius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center.position, despawnDistance);
    }
#endif
}
