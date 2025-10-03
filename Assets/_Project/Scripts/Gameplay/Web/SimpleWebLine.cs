using UnityEngine;

[ExecuteAlways]
public class SimpleWebLine : MonoBehaviour
{
    public Transform anchorTop;     // arraste AnchorTop
    public Transform hangingObject; // arraste HangingObject
    public LineRenderer lr;         // arraste o Line Renderer do próprio objeto
    public float width = 0.02f;     // espessura da teia

    void Reset() { lr = GetComponent<LineRenderer>(); }
    void OnValidate() { if (lr) lr.widthMultiplier = width; }

    void LateUpdate()
    {
        if (!lr || !anchorTop || !hangingObject) return;
        // garante 2 pontos e atualiza posições
        if (lr.positionCount != 2) lr.positionCount = 2;
        lr.SetPosition(0, anchorTop.position);
        lr.SetPosition(1, hangingObject.position);
        lr.widthMultiplier = width;
    }
}
