using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class WebBetweenAreas : MonoBehaviour
{
    [Header("Areas (use BoxCollider as bounds)")]
    public Transform areaA;            // ex.: topo / teto / esquerda
    public Transform areaB;            // ex.: base / móvel / direita

    [Header("Look")]
    [Min(1)] public int strands = 12;
    public float width = 0.02f;        // espessura por fio
    public float jitter = 0.01f;       // “tremidinha” no meio do fio
    public float spreadEnd = 0.00f;    // alarga as pontas (0 = reta)
    public Material webMat;

    [Header("Collision Projection")]
    public bool projectToSurface = true;
    public LayerMask surfaceMask = ~0;
    public float surfaceOffset = 0.005f; // afastamento do ponto da superfície

    LineRenderer[] pool;

    void OnValidate()
    {
        strands = Mathf.Max(1, strands);
        width = Mathf.Max(0.0005f, width);
        jitter = Mathf.Max(0f, jitter);
        surfaceOffset = Mathf.Max(0f, surfaceOffset);
    }

    void LateUpdate()
    {
        if (!areaA || !areaB || !webMat) return;

        var boxA = areaA.GetComponent<BoxCollider>();
        var boxB = areaB.GetComponent<BoxCollider>();
        if (!boxA || !boxB) return; // precisa de BoxCollider em cada área

        EnsurePool(strands);

        for (int i = 0; i < strands; i++)
        {
            // 1) Ponto aleatório dentro do retângulo da área A
            Vector3 localA = new Vector3(
                (Random.value - 0.5f) * boxA.size.x,
                (Random.value - 0.5f) * boxA.size.y,
                +0.5f * boxA.size.z // face “frontal” do box
            );
            Vector3 start = areaA.TransformPoint(localA);

            // 2) Ponto aleatório dentro do retângulo da área B
            Vector3 localB = new Vector3(
                (Random.value - 0.5f) * boxB.size.x,
                (Random.value - 0.5f) * boxB.size.y,
                -0.5f * boxB.size.z // face “frontal” oposta
            );
            Vector3 end = areaB.TransformPoint(localB);

            // 3) Projeta os pontos na superfície (evita teia “entrar” no objeto)
            if (projectToSurface)
            {
                // Projeta start em direção a B
                Vector3 dirAB = (end - start).normalized;
                if (Physics.Raycast(start - dirAB * 0.05f, dirAB, out var hitA, 2f, surfaceMask, QueryTriggerInteraction.Ignore))
                    start = hitA.point - dirAB * surfaceOffset;

                // Projeta end em direção a A
                Vector3 dirBA = -dirAB;
                if (Physics.Raycast(end - dirBA * 0.05f, dirBA, out var hitB, 2f, surfaceMask, QueryTriggerInteraction.Ignore))
                    end = hitB.point - dirBA * surfaceOffset;
            }

            // 4) Pequeno “S” no meio (jitter controlado + spread nas pontas)
            Vector3 mid = (start + end) * 0.5f;
            Vector3 seg = (end - start);
            Vector3 anyUp = Mathf.Abs(Vector3.Dot(seg.normalized, Vector3.up)) > 0.9f ? Vector3.right : Vector3.up;
            Vector3 right = Vector3.Normalize(Vector3.Cross(seg.normalized, anyUp));
            Vector3 up = Vector3.Normalize(Vector3.Cross(right, seg.normalized));

            float ang = Random.value * Mathf.PI * 2f;
            Vector3 jitterDir = Mathf.Cos(ang) * right + Mathf.Sin(ang) * up;
            Vector3 midOff = jitterDir * jitter;

            Vector3 startOff = Vector3.Lerp(Vector3.zero, jitterDir * jitter, spreadEnd);
            Vector3 endOff = Vector3.Lerp(Vector3.zero, -jitterDir * jitter, spreadEnd);

            // 5) Configura o LineRenderer (3 pontos -> curva simples)
            var lr = pool[i];
            if (lr.positionCount != 3) lr.positionCount = 3;
            lr.SetPosition(0, start + startOff);
            lr.SetPosition(1, mid + midOff);
            lr.SetPosition(2, end + endOff);
            lr.widthMultiplier = width;
        }
    }

    void EnsurePool(int count)
    {
        if (pool != null && pool.Length == count) return;

        // limpa os antigos
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var ch = transform.GetChild(i);
            if (ch.name.StartsWith("strand_"))
#if UNITY_EDITOR
                if (!Application.isPlaying) DestroyImmediate(ch.gameObject); else Destroy(ch.gameObject);
#else
                DestroyImmediate(ch.gameObject);
#endif
        }

        pool = new LineRenderer[count];
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("strand_" + i);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.material = webMat;
            lr.alignment = LineAlignment.View;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.numCornerVertices = 0;
            lr.numCapVertices = 0;
            lr.textureMode = LineTextureMode.Stretch;
            lr.positionCount = 3;
            pool[i] = lr;
        }
    }
}
