using UnityEngine;
using Unity.XR.CoreUtils; // XROrigin

public class XRTurnYawLimiter : MonoBehaviour
{
    public XROrigin xrOrigin;              // arraste o XR Origin aqui (ou deixa vazio p/ auto)
    public Transform reference;            // opcional: refer�ncia p/ �ncora (ex.: um Empty no ch�o)
    [Range(5f, 120f)] public float maxYawDegrees = 45f; // limite p/ cada lado

    float _anchorYawY;

    void Awake()
    {
        if (!xrOrigin) xrOrigin = GetComponent<XROrigin>();
    }

    void OnEnable() { ResetAnchor(); }

    public void ResetAnchor()
    {
        // usa a refer�ncia se existir; sen�o, a rota��o atual do XR Origin
        _anchorYawY = (reference ? reference.eulerAngles.y : xrOrigin.transform.eulerAngles.y);
    }

    void LateUpdate()
    {
        var t = xrOrigin.transform;
        float currentY = t.eulerAngles.y;

        // diferen�a assinada entre o yaw atual e a �ncora
        float delta = Mathf.DeltaAngle(_anchorYawY, currentY);

        // restringe dentro do �limite
        float clamped = Mathf.Clamp(delta, -maxYawDegrees, maxYawDegrees);

        if (!Mathf.Approximately(clamped, delta))
        {
            var e = t.eulerAngles;
            e.y = _anchorYawY + clamped;
            t.eulerAngles = e; // aplica o clamp
        }
    }
}
