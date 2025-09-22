using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Events;

public class PanicRadialUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] GameObject radialRoot; // Canvas root
    [SerializeField] Transform controller; // Right Controller (transform)
    [SerializeField] RectTransform ringRect;  // RectTransform do RingBG (ou do Canvas)
    [SerializeField] Image sliceLeft; // Teleport
    [SerializeField] Image sliceRight; // Desabilitar efeitos
    [SerializeField] RectTransform dot; // indicador
    [SerializeField] InputActionReference holdAction; // <XRController>{RightHand}/secondaryButton (B) ou grip
    [SerializeField] GameObject[] blockIfVisible; // PanelMain/PanelPause/PanelSettings

    [Header("Tuning")]
    [Range(0f, 1f)] public float deadzone = 0.15f; // inclinação mínima
    public float dotRadius = 220f; // em px, Canvas 800x800

    [Header("Events")]
    public UnityEvent onTeleport; // dispara ao soltar em ESQUERDA
    public UnityEvent onDisableFx; // dispara ao soltar em DIREITA

    int current = 0; // 0=none, 1=left, 2=right
    bool active;

    void OnEnable()
    {
        if (holdAction)
        {
            holdAction.action.performed += OnHold;
            holdAction.action.canceled += OnRelease;
            holdAction.action.Enable();
        }
        Show(false);
    }
    void OnDisable()
    {
        if (holdAction)
        {
            holdAction.action.performed -= OnHold;
            holdAction.action.canceled -= OnRelease;
            holdAction.action.Disable();
        }
    }

    void OnHold(InputAction.CallbackContext _)
    {
        if (!CanShow()) return;
        Show(true);
    }
    void OnRelease(InputAction.CallbackContext _)
    {
        if (!active) return;
        // confirma seleção
        if (current == 1) onTeleport?.Invoke();
        else if (current == 2) onDisableFx?.Invoke();
        Show(false);
    }

    void Show(bool on)
    {
        active = on;
        if (radialRoot) radialRoot.SetActive(on);
        current = 0;
        if (sliceLeft) sliceLeft.color = SetA(sliceLeft.color, 0.35f);
        if (sliceRight) sliceRight.color = SetA(sliceRight.color, 0.35f);
        if (dot) dot.anchoredPosition = Vector2.zero;
    }

    void Update()
    {
        if (!active || !controller || !ringRect) return;

        // Vetor de inclinação do controle, projetado no plano do Canvas
        Vector3 planeN = ringRect.transform.forward; // normal do Canvas
        Vector3 v = Vector3.ProjectOnPlane(controller.up * -1f, planeN); // ponta na direção do "mergulho"
        Vector3 local = ringRect.transform.InverseTransformDirection(v);
        Vector2 dir = new Vector2(local.x, local.y);
        float mag = dir.magnitude;

        // Atualiza ponto
        Vector2 p = (mag > 1f ? dir.normalized : dir) * dotRadius;
        if (dot) dot.anchoredPosition = p;

        // Seleção por hemisfério, com deadzone
        int sel = 0;
        if (mag > deadzone)
        {
            sel = (dir.x < 0f) ? 1 : 2; // esquerda = Teleport, direita = DisableFx
        }
        if (sel != current)
        {
            current = sel;
            // feedback visual
            if (sliceLeft) sliceLeft.color = SetA(sliceLeft.color, sel == 1 ? 0.90f : 0.35f);
            if (sliceRight) sliceRight.color = SetA(sliceRight.color, sel == 2 ? 0.90f : 0.35f);
        }
    }

    bool CanShow()
    {
        if (blockIfVisible == null) return true;
        foreach (var go in blockIfVisible) if (go && go.activeInHierarchy) return false;
        return true;
    }

    static Color SetA(Color c, float a) { c.a = a; return c; }
}
