using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
public class OrbitalMotion : MonoBehaviour
{
    [Header("Orbit")]
    public Transform center;
    public float radius = 10f; // meters
    public float angularSpeedDeg = 20f; // degrees/second
    public Vector3 axis = Vector3.up; // orbit axis (usually up)
    public float bobAmplitude = 0.3f; // vertical float
    public float bobFrequency = 0.2f; // Hz

    [Header("Despawn")]
    public float despawnDistance = 60f; // distance from center to destroy

    [Header("Recapture (optional)")]
    public bool recaptureWhenNear = true;
    public float recaptureSpeedThreshold = 0.6f;
    public float recaptureMaxDistanceFromIdeal = 1.2f; // how close to ideal orbit to re-attach

    [HideInInspector] public OrbitManager manager;

    Rigidbody rb;
    XRGrabInteractable grab;
    float angleDeg; // current angle along orbit
    float bobPhase; // for per-instance bobbing
    bool orbitEnabled = true;

    // Read group multiplier from manager (defaults to 1)
    float GroupSpeed => manager ? manager.SpeedMultiplier : 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();
        if (grab)
        {
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

        // Start orbiting kinematic for clean placement
        rb.isKinematic = true;
        orbitEnabled = true;

        // Place at initial position immediately
        UpdateOrbitTransform(0f, force: true);
    }

    void Update()
    {
        if (!center) return;

        if (orbitEnabled && rb.isKinematic)
        {
            UpdateOrbitTransform(Time.deltaTime);
        }

        // Despawn check (works in any state)
        float dist = Vector3.Distance(transform.position, center.position);
        if (dist > despawnDistance)
        {
            // Ask manager to replace and destroy this instance
            manager?.RequestRespawnAndDestroy(this);
        }
    }

    void UpdateOrbitTransform(float dt, bool force = false)
    {
        if (dt > 0f || force)
        {
            angleDeg += angularSpeedDeg * GroupSpeed * dt;
        }

        Vector3 nAxis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;

        // Build a perpendicular base vector to rotate around axis
        Vector3 baseDir = Vector3.Cross(nAxis, Vector3.right);
        if (baseDir.sqrMagnitude < 1e-4f) baseDir = Vector3.Cross(nAxis, Vector3.forward);
        baseDir.Normalize();

        // Position on circle
        Quaternion rot = Quaternion.AngleAxis(angleDeg, nAxis);
        Vector3 radial = rot * baseDir * radius;

        // Gentle bob along axis
        float bob = Mathf.Sin((Time.time + bobPhase) * (Mathf.PI * 2f) * bobFrequency) * bobAmplitude;

        Vector3 targetPos = center.position + radial + nAxis * bob;

        transform.position = targetPos;
        // Optional: face tangentially or keep world rotation. Here we keep rotation.
    }

    void OnSelectEntered(SelectEnterEventArgs _)
    {
        // Leave orbit and enable physics
        orbitEnabled = false;
        rb.isKinematic = false;
    }

    void OnSelectExited(SelectExitEventArgs _)
    {
        // Stay free-flying; optionally start a recapture watcher
        if (recaptureWhenNear) StartCoroutine(TryRecaptureRoutine());
    }

    IEnumerator TryRecaptureRoutine()
    {
        // Wait a moment to let the throw/impulse happen
        yield return new WaitForSeconds(0.2f);

        // Try for a few seconds to see if object settles near orbit path
        float t = 0f;
        while (t < 6f)
        {
            t += Time.deltaTime;
            if (!center) yield break;

            // Ideal orbit position at current radius angle (compute best angle from position)
            Vector3 nAxis = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;

            // Project vector from center onto orbit plane
            Vector3 fromCenter = transform.position - center.position;
            Vector3 planeProj = Vector3.ProjectOnPlane(fromCenter, nAxis);

            if (planeProj.sqrMagnitude > 0.01f)
            {
                float currentRadius = planeProj.magnitude;
                float radiusDelta = Mathf.Abs(currentRadius - radius);

                // Estimate tangential angle for smooth re-attach
                Vector3 baseDir = Vector3.Cross(nAxis, Vector3.right);
                if (baseDir.sqrMagnitude < 1e-4f) baseDir = Vector3.Cross(nAxis, Vector3.forward);
                baseDir.Normalize();

                // Compute signed angle from baseDir to planeProj around axis
                float signed = Vector3.SignedAngle(baseDir, planeProj.normalized, nAxis);

                // Check speed & closeness to ideal radius
                float speed = rb.linearVelocity.magnitude;
                if (speed < recaptureSpeedThreshold && radiusDelta < recaptureMaxDistanceFromIdeal)
                {
                    // Snap back to orbiting
                    angleDeg = signed;
                    rb.linearVelocity = Vector3.zero; // Unity 6000 API
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                    orbitEnabled = true;
                    yield break;
                }
            }

            yield return null;
        }
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
