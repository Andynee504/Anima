using UnityEngine;
using Unity.XR.CoreUtils; // XROrigin

public class XRTurnYawLimiter : MonoBehaviour
{
    public XROrigin xrOrigin;              // arraste o XR Origin aqui (ou deixa vazio p/ auto)
    public Transform reference;            // opcional: referência p/ âncora (ex.: um Empty no chão)
    [Range(5f, 120f)] public float maxYawDegrees = 45f; // limite p/ cada lado

    float _anchorYawY;

    void Awake()
    {
        if (!xrOrigin) xrOrigin = GetComponent<XROrigin>();
    }

    void OnEnable() { ResetAnchor(); }

    public void ResetAnchor()
    {
        // usa a referência se existir; senão, a rotação atual do XR Origin
        _anchorYawY = (reference ? reference.eulerAngles.y : xrOrigin.transform.eulerAngles.y);
    }

    void LateUpdate()
    {
        var t = xrOrigin.transform;
        float currentY = t.eulerAngles.y;

        // diferença assinada entre o yaw atual e a âncora
        float delta = Mathf.DeltaAngle(_anchorYawY, currentY);

        // restringe dentro do ±limite
        float clamped = Mathf.Clamp(delta, -maxYawDegrees, maxYawDegrees);

        if (!Mathf.Approximately(clamped, delta))
        {
            var e = t.eulerAngles;
            e.y = _anchorYawY + clamped;
            t.eulerAngles = e; // aplica o clamp
        }
    }
}
