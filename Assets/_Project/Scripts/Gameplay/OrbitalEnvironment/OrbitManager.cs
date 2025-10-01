using System.Collections.Generic;
using UnityEngine;

public class OrbitManager : MonoBehaviour
{
    [Header("Scene Refs")]
    public Transform center;
    public Camera xrCamera;

    [Header("Prefabs & Weights")]
    public OrbitalMotion[] orbiterPrefabs; // variacoes
    public int[] weights; // opcional (ex.: 50,30,20)

    [Header("Pool / Counts")]
    public int targetCount = 8;

    [Header("Spawn Params (defaults)")]
    public Vector2 radiusRange = new Vector2(8f, 14f);
    public Vector2 speedDegRange = new Vector2(10f, 28f);
    public float despawnDistance = 60f;
    [Tooltip("Separação mínima em graus da direção da câmera para spawn 'fora da vista'")]
    public float spawnMinAngleFromViewDeg = 70f;

    [Header("Spherical Orbit")]
    public bool spherical = true;
    [Range(1, 12)] public int ringCount = 5;

    [Header("Speed Control")]
    [Range(0.05f, 5f)] public float SpeedMultiplier = 1f;

    [Header("Visibility")]
    public bool pauseWhenHidden = true; // pausa orbita quando esconder via SetAllVisible

    [Header("Extra participants")]
    public OrbitalMotion[] alsoAffect;

    readonly List<OrbitalMotion> actives = new();

    void Start()
    {
        if (!center || !xrCamera || orbiterPrefabs == null || orbiterPrefabs.Length == 0)
        {
            Debug.LogError("[OrbitManager] Missing refs or no prefabs.");
            return;
        }
        // primeira leva: distribuído (sem spawn-out-of-view)
        SpawnDistributed(targetCount);

        // Garante estado inicial oculto (caso a sala de visibilidade ainda nao tenha rodado)
        SetAllVisible(false, disableCollisionToo: true, fadeSeconds: 0f);
        if (alsoAffect != null)
        {
            foreach (var extra in alsoAffect)
            {
                if (extra) extra.SetPresentationVisible(false, true, 0f);
            }
        }
    }

    void Update()
    {
        // mantem a populacao estavel
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
        SpawnOne();
    }

    // ----------- Spawning -----------

    void SpawnDistributed(int count)
    {
        if (!spherical || ringCount <= 1)
        {
            // anel unico
            for (int i = 0; i < count; i++)
            {
                var prefab = PickPrefab();
                float t = (i + 0.5f) / Mathf.Max(1, count);
                float angleDeg = t * 360f + Random.Range(-6f, 6f);
                float radius = Random.Range(radiusRange.x, radiusRange.y);
                float speedDeg = Random.Range(speedDegRange.x, speedDegRange.y);

                var inst = Instantiate(prefab);
                inst.name = $"{prefab.name}_orbiter";

                // ja nasce invisivel (sem colisao, sem grab)
                inst.SetPresentationVisible(false, disableCollisionToo: true, fadeSeconds: 0f);

                // garantir que NAO vai sobrescrever o eixo no Init
                inst.randomizeAxisOnInit = false;
                inst.axis = Random.onUnitSphere.normalized;

                inst.InitFromManager(this, center, angleDeg, radius, speedDeg, despawnDistance);
                actives.Add(inst);
            }
            return;
        }

        // “esfera” — divide em varios aneis com eixos variados
        int perRing = Mathf.CeilToInt(count / (float)ringCount);
        int spawned = 0;

        for (int r = 0; r < ringCount && spawned < count; r++)
        {
            // eixo do anel r (quase uniforme via “Fibonacci sphere”)
            float k = (r + 0.5f) / ringCount; // 0..1
            float cos = 1f - 2f * k; // -1..1
            float sin = Mathf.Sqrt(Mathf.Max(0f, 1f - cos * cos));
            float phi = r * 2.39996323f; // ~137.5°
            Vector3 ringAxis = new Vector3(Mathf.Cos(phi) * sin, cos, Mathf.Sin(phi) * sin).normalized;
            if (ringAxis.sqrMagnitude < 1e-4f) ringAxis = Vector3.up;

            int inThisRing = Mathf.Min(perRing, count - spawned);
            for (int j = 0; j < inThisRing; j++)
            {
                var prefab = PickPrefab();
                float t = (j + 0.5f) / Mathf.Max(1, inThisRing);
                float angleDeg = t * 360f + Random.Range(-10f, 10f);
                float radius = Random.Range(radiusRange.x, radiusRange.y);
                float speedDeg = Random.Range(speedDegRange.x, speedDegRange.y);

                var inst = Instantiate(prefab);
                inst.name = $"{prefab.name}_orbiter";

                // ja nasce invisivel
                inst.SetPresentationVisible(false, disableCollisionToo: true, fadeSeconds: 0f);

                inst.randomizeAxisOnInit = false; // nao sobrescrever no Init
                inst.axis = ringAxis; // usa o eixo do anel

                inst.InitFromManager(this, center, angleDeg, radius, speedDeg, despawnDistance);
                actives.Add(inst);
                spawned++;
            }
        }
    }

    void SpawnOne()
    {
        var prefab = PickPrefab();
        float radius = Random.Range(radiusRange.x, radiusRange.y);
        float speedDeg = Random.Range(speedDegRange.x, speedDegRange.y);
        float angleDeg = PickAngleOutOfView();

        var inst = Instantiate(prefab);
        inst.name = $"{prefab.name}_orbiter";

        // ja nasce invisivel
        inst.SetPresentationVisible(false, disableCollisionToo: true, fadeSeconds: 0f);

        inst.randomizeAxisOnInit = false; // nao sobrescrever no Init

        if (spherical)
        {
            var ax = Random.onUnitSphere;
            if (ax.sqrMagnitude < 1e-4f) ax = Vector3.up;
            inst.axis = ax.normalized;
        }
        else
        {
            inst.axis = Random.onUnitSphere.normalized;
        }

        inst.InitFromManager(this, center, angleDeg, radius, speedDeg, despawnDistance);
        actives.Add(inst);
    }

    // ----------- Visibilidade -----------

    public void SetAllVisible(bool visible, bool disableCollisionToo = false, float fadeSeconds = 0.25f)
    {
        if (pauseWhenHidden)
        {
            // zera a velocidade global quando oculto; retoma (>=1) quando visivel
            SpeedMultiplier = visible ? Mathf.Max(SpeedMultiplier, 1f) : 0f;
        }

        for (int i = 0; i < actives.Count; i++)
        {
            var o = actives[i];
            if (!o) continue;
            o.SetPresentationVisible(visible, disableCollisionToo, fadeSeconds);
        }
        if (alsoAffect != null)
        {
            foreach (var extra in alsoAffect)
                if (extra) extra.SetPresentationVisible(visible, disableCollisionToo, fadeSeconds);
        }
    }

    // ----------- Utilitários -----------

    OrbitalMotion PickPrefab()
    {
        if (orbiterPrefabs == null || orbiterPrefabs.Length == 0)
            return null;

        if (weights == null || weights.Length != orbiterPrefabs.Length)
            return orbiterPrefabs[Random.Range(0, orbiterPrefabs.Length)];

        int total = 0;
        for (int i = 0; i < weights.Length; i++) total += Mathf.Max(0, weights[i]);
        if (total <= 0) return orbiterPrefabs[Random.Range(0, orbiterPrefabs.Length)];

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
        // projeta o forward da camera no plano Y (assumindo up global)
        Vector3 axis = Vector3.up;
        Vector3 camFwd = xrCamera.transform.forward;
        Vector3 planeFwd = Vector3.ProjectOnPlane(camFwd, axis).normalized;
        if (planeFwd.sqrMagnitude < 1e-4f) planeFwd = Vector3.forward;

        // baseDir consistente com OrbitalMotion (quando axis=up)
        Vector3 baseDir = Vector3.Cross(axis, Vector3.right);
        if (baseDir.sqrMagnitude < 1e-4f) baseDir = Vector3.Cross(axis, Vector3.forward);
        baseDir.Normalize();

        float camAngle = Vector3.SignedAngle(baseDir, planeFwd, axis);
        float jitter = Random.Range(-30f, 30f);
        float desired = camAngle + 180f + jitter;

        float sep = Mathf.Abs(Mathf.DeltaAngle(desired, camAngle));
        if (sep < spawnMinAngleFromViewDeg)
        {
            float push = Mathf.Sign(desired - camAngle) * (spawnMinAngleFromViewDeg - sep + 5f);
            desired += push;
        }
        return desired;
    }

    // API de velocidade global
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
