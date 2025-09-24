using UnityEngine;
using System.Collections;

public class SlidingDoorController : MonoBehaviour
{
    public enum Mode { Auto, LockedClosed, Manual }

    [Header("Refs")]
    [SerializeField] Transform leftPanel;
    [SerializeField] Transform rightPanel;

    [Header("Movimento")]
    [SerializeField] float slideDistance = 0.8f;          // quanto cada folha entra no batente
    [SerializeField] Vector3 leftDirection = Vector3.left;
    [SerializeField] Vector3 rightDirection = Vector3.right;
    [SerializeField] float openDuration = 0.45f;
    [SerializeField] float closeDuration = 0.45f;
    [SerializeField] AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Automação")]
    [SerializeField] Mode mode = Mode.Auto;
    [SerializeField] float autoCloseDelay = 1.0f;

    // estado
    public bool IsOpen { get; private set; }
    public bool HasPresence => _presenceCount > 0;

    Vector3 _lClosed, _rClosed, _lOpen, _rOpen;
    int _presenceCount = 0;
    Coroutine _moveCo;
    Coroutine _delayCloseCo;

    void Reset()
    {
        // tenta auto-preencher
        if (leftPanel == null && transform.Find("LeftPanel") != null) leftPanel = transform.Find("LeftPanel");
        if (rightPanel == null && transform.Find("RightPanel") != null) rightPanel = transform.Find("RightPanel");
    }

    void Awake()
    {
        if (leftPanel == null || rightPanel == null)
        {
            Debug.LogError("[SlidingDoorController] Falta vincular LeftPanel/RightPanel.", this);
            enabled = false; return;
        }

        // guarda posições fechadas
        _lClosed = leftPanel.localPosition;
        _rClosed = rightPanel.localPosition;

        // calcula posições abertas
        _lOpen = _lClosed + leftDirection.normalized * slideDistance;
        _rOpen = _rClosed + rightDirection.normalized * slideDistance;

        // garante Rigidbody cinemático no pai
        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        CloseInstant();
    }

    // ===== API chamada pelo sensor (ou por outros sistemas) =====
    public void OnActorEnter()
    {
        _presenceCount++;
        if (mode != Mode.Auto) return;
        Open();
    }

    public void OnActorExit()
    {
        _presenceCount = Mathf.Max(0, _presenceCount - 1);
        if (mode != Mode.Auto) return;
        if (_presenceCount == 0) CloseAfterDelay(autoCloseDelay);
    }

    public void SetMode(Mode m)
    {
        mode = m;
        if (mode == Mode.LockedClosed) Close();
    }

    public void LockForSeconds(float seconds)
    {
        StopCoroutineSafe(ref _delayCloseCo);
        StartCoroutine(LockRoutine(seconds));
    }

    // ===== Comandos diretos =====
    public void Open()
    {
        if (mode == Mode.LockedClosed) return;
        StopCoroutineSafe(ref _delayCloseCo);
        MoveTo(true, openDuration);
    }

    public void Close()
    {
        MoveTo(false, closeDuration);
    }

    public void CloseAfterDelay(float delay)
    {
        StopCoroutineSafe(ref _delayCloseCo);
        _delayCloseCo = StartCoroutine(CloseDelayRoutine(delay));
    }

    public void CloseInstant()
    {
        StopCoroutineSafe(ref _moveCo);
        leftPanel.localPosition = _lClosed;
        rightPanel.localPosition = _rClosed;
        IsOpen = false;
    }

    // ===== Internos =====
    IEnumerator CloseDelayRoutine(float d)
    {
        yield return new WaitForSeconds(d);
        if (_presenceCount == 0 && mode == Mode.Auto) Close();
    }

    IEnumerator LockRoutine(float seconds)
    {
        var old = mode;
        mode = Mode.LockedClosed;
        Close();
        yield return new WaitForSeconds(seconds);
        mode = old == Mode.LockedClosed ? Mode.Auto : old;
        if (mode == Mode.Auto && _presenceCount > 0) Open();
    }

    void MoveTo(bool open, float duration)
    {
        StopCoroutineSafe(ref _moveCo);
        _moveCo = StartCoroutine(MoveRoutine(open, duration));
    }

    IEnumerator MoveRoutine(bool open, float duration)
    {
        IsOpen = open;
        Vector3 lFrom = leftPanel.localPosition;
        Vector3 rFrom = rightPanel.localPosition;
        Vector3 lTo = open ? _lOpen : _lClosed;
        Vector3 rTo = open ? _rOpen : _rClosed;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, duration);
            float k = curve.Evaluate(Mathf.Clamp01(t));
            leftPanel.localPosition = Vector3.LerpUnclamped(lFrom, lTo, k);
            rightPanel.localPosition = Vector3.LerpUnclamped(rFrom, rTo, k);
            yield return null;
        }
        leftPanel.localPosition = lTo;
        rightPanel.localPosition = rTo;
        _moveCo = null;
    }

    void StopCoroutineSafe(ref Coroutine co)
    {
        if (co != null) { StopCoroutine(co); co = null; }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (leftPanel && rightPanel)
        {
            Gizmos.color = Color.cyan;
            var lClosed = Application.isPlaying ? _lClosed : leftPanel.localPosition;
            var rClosed = Application.isPlaying ? _rClosed : rightPanel.localPosition;
            var lOpen = lClosed + (leftDirection == Vector3.zero ? Vector3.left : leftDirection.normalized) * slideDistance;
            var rOpen = rClosed + (rightDirection == Vector3.zero ? Vector3.right : rightDirection.normalized) * slideDistance;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawLine(lClosed, lOpen);
            Gizmos.DrawLine(rClosed, rOpen);
        }
    }
#endif
}
