using UnityEngine;
[ExecuteAlways]
public class WebBundle : MonoBehaviour
{
    public Transform anchorTop, hangingObject;
    public Material webMat;
    public int strands = 4;          // quantidade de fios
    public float baseWidth = 0.02f;  // espessura do fio principal
    public float spread = 0.02f;     // “abertura” entre fios (em metros)
    public float jitter = 0.004f;    // variação leve
    LineRenderer[] lrs;

    void OnValidate() { strands = Mathf.Max(1, strands); }
    void LateUpdate()
    {
        if (!anchorTop || !hangingObject || !webMat) return;
        if (lrs == null || lrs.Length != strands)
        { // cria/ajusta quantidade
            foreach (Transform c in transform) DestroyImmediate(c.gameObject);
            lrs = new LineRenderer[strands];
            for (int i = 0; i < strands; i++)
            {
                var go = new GameObject("strand_" + i);
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.material = webMat;
                lr.alignment = LineAlignment.View;
                lr.numCornerVertices = 0; lr.numCapVertices = 0;
                lrs[i] = lr;
            }
        }

        Vector3 a = anchorTop.position, b = hangingObject.position;
        Vector3 dir = (b - a).normalized;
        // base ortonormal (right/up) perpendicular ao fio
        Vector3 anyUp = (Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.9f) ? Vector3.right : Vector3.up;
        Vector3 right = Vector3.Normalize(Vector3.Cross(dir, anyUp));
        Vector3 up = Vector3.Normalize(Vector3.Cross(right, dir));

        for (int i = 0; i < strands; i++)
        {
            float t = (i + 0.5f) / strands * Mathf.PI * 2f;
            Vector3 radial = (Mathf.Cos(t) * right + Mathf.Sin(t) * up) * spread;
            Vector3 j = Random.insideUnitSphere * jitter; // leve aleatório
            var lr = lrs[i];
            lr.widthMultiplier = Mathf.Lerp(baseWidth, baseWidth * 0.5f, i / (float)(strands - 1));
            lr.SetPosition(0, a + radial + j);
            lr.SetPosition(1, b + radial - j);
        }
    }
}
