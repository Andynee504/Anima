using System.Collections.Generic;
using UnityEngine;

public class OrbitManager : MonoBehaviour
{
    [Header("Scene Refs")]
    public Transform center;
    public Camera xrCamera;

    [Header("Prefabs & Weights")]
    public OrbitalMotion[] orbiterPrefabs;
    public int[] weights; // teste

    [Header("Pool / Counts")]
    public int targetCount = 8;

    [Header("Spawn Params (defaults)")]
    public Vector2 radiusRange = new Vector2(8f, 14f);
    public Vector2 speedDegRange = new Vector2(10f, 28f);
    public float despawnDistance = 60f;
    public float spawnMinAngleFromViewDeg = 60f; // > FOV/2 is safe

    [Header("Speed Control")]
    [Range(0.05f, 5f)] public float SpeedMultiplier = 1f;

    readonly List<OrbitalMotion> actives = new();

    void Start()
    {
        if (!center || !xrCamera || orbiterPrefabs == null || orbiterPrefabs.Length == 0)
        {
            Debug.LogError("[OrbitManager] Missing refs or no prefabs.");
            return;
        }

        // Top-up to target count
        for (int i = actives.Count; i < targetCount; i++) SpawnOne();
    }

    void Update()
    {
        // Mantem populacao estavel caso delecao manual
        if (actives.Count < targetCount)
        {
            int diff = targetCount - actives.Count;
            for (int i = 0; i < diff; i++) SpawnOne();
        }
    }

    public void RequestRespawnAndDestroy(OrbitalMotion who)
    {
        if (who)
        {
            actives.Remove(who);
            Destroy(who.gameObject);
        }
        // Immediately spawn a replacement
        SpawnOne();
    }

    void SpawnOne()
    {
        var prefab = PickPrefab();
        float radius = Random.Range(radiusRange.x, radiusRange.y);
        float speedDeg = Random.Range(speedDegRange.x, speedDegRange.y);

        // Pick an angle that's out of current view
        float angleDeg = PickAngleOutOfView();

        var inst = Instantiate(prefab);
        inst.name = $"{prefab.name}_orbiter";
        inst.InitFromManager(this, center, angleDeg, radius, speedDeg, despawnDistance);
        actives.Add(inst);
    }

    OrbitalMotion PickPrefab()
    {
        if (weights == null || weights.Length != orbiterPrefabs.Length)
        {
            int i = Random.Range(0, orbiterPrefabs.Length);
            return orbiterPrefabs[i];
        }

        int total = 0;
        for (int i = 0; i < weights.Length; i++) total += Mathf.Max(0, weights[i]);
        if (total <= 0)
        {
            int i = Random.Range(0, orbiterPrefabs.Length);
            return orbiterPrefabs[i];
        }

        int r = Random.Range(1, total + 1);
        int acc = 0;
        for (int i = 0; i < orbiterPrefabs.Length; i++)
        {
            acc += Mathf.Max(0, weights[i]);
            if (r <= acc) return orbiterPrefabs[i];
        }
        return orbiterPrefabs[orbiterPrefabs.Length - 1];
    }

    float PickAngleOutOfView()
    {
        // Project camera forward onto orbit plane and pick roughly behind it
        Vector3 axis = Vector3.up; // We assume up-axis orbits. If you vary per-orbiter, pass it in.
        Vector3 camFwd = xrCamera.transform.forward;
        Vector3 planeFwd = Vector3.ProjectOnPlane(camFwd, axis).normalized;

        if (planeFwd.sqrMagnitude < 1e-4f) planeFwd = Vector3.forward; // fallback

        // Build same baseDir used in OrbitalMotion
        Vector3 baseDir = Vector3.Cross(axis, Vector3.right);
        if (baseDir.sqrMagnitude < 1e-4f) baseDir = Vector3.Cross(axis, Vector3.forward);
        baseDir.Normalize();

        // Camera forward angle relative to baseDir
        float camAngle = Vector3.SignedAngle(baseDir, planeFwd, axis);

        // Spawn behind camera -> add 180 ± jitter, and also ensure out-of-view margin
        float jitter = Random.Range(-30f, 30f);
        float desired = camAngle + 180f + jitter;

        // Final nudge to guarantee min separation from view
        float sep = Mathf.Abs(Mathf.DeltaAngle(desired, camAngle));
        if (sep < spawnMinAngleFromViewDeg)
        {
            float push = Mathf.Sign(desired - camAngle) * (spawnMinAngleFromViewDeg - sep + 5f);
            desired += push;
        }
        return desired;
    }

    // Speed API
    public void SetSpeedMultiplier(float m) => SpeedMultiplier = Mathf.Max(0f, m);
    public void LerpSpeedMultiplierTo(float target, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(LerpSpeedRoutine(target, duration));
    }
    System.Collections.IEnumerator LerpSpeedRoutine(float target, float duration)
    {
        float start = SpeedMultiplier, t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = duration > 0f ? Mathf.Clamp01(t / duration) : 1f;
            SpeedMultiplier = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, k));
            yield return null;
        }
        SpeedMultiplier = target;
    }
}
