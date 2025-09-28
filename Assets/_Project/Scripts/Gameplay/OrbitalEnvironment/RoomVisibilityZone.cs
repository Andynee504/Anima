using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RoomVisibilityZone : MonoBehaviour
{
    [Tooltip("Manager que controla os orbiters desta área")]
    public OrbitManager manager;

    [Header("Detecção do jogador")]
    public Transform player;             // arraste o XR Origin ou a Camera (CenterEye)
    public string playerTag = "Player";  // opcional
    public bool requireTag = false;

    [Header("Comportamento")]
    public bool disableCollisionWhenHidden = true;
    public float fadeInSeconds = 0.35f;
    public float fadeOutSeconds = 0.25f;

    Collider _col;

    void Reset()
    {
        _col = GetComponent<Collider>();
        _col.isTrigger = true;
    }

    void Awake()
    {
        _col = GetComponent<Collider>();
    }

    void Start()
    {
        if (!manager) return;

        // descobre player se não foi setado
        if (!player)
        {
            var cam = Camera.main;
            if (cam) player = cam.transform;
        }

        // Estado inicial: se o player JÁ está dentro do volume, mostra; senão, esconde
        bool inside = IsPlayerInsideAtStart();
        manager.SetAllVisible(inside, disableCollisionWhenHidden, inside ? fadeInSeconds : 0f);
    }

    bool IsPlayerInsideAtStart()
    {
        if (!player || !_col) return false;
        return _col.bounds.Contains(player.position); // bom para Box/Sphere; para Mesh não-convexo, use um Box como filho
    }

    bool IsPlayer(Collider other)
    {
        if (player && other.transform == player) return true;
        if (!requireTag && (other.name.Contains("XR Origin") || other.name.Contains("Camera"))) return true;
        if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag)) return true;
        if (other.GetComponent<CharacterController>()) return true;
        return false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!manager) return;
        if (!IsPlayer(other)) return;
        manager.SetAllVisible(true, disableCollisionWhenHidden, fadeInSeconds);
    }

    void OnTriggerExit(Collider other)
    {
        if (!manager) return;
        if (!IsPlayer(other)) return;
        manager.SetAllVisible(false, disableCollisionWhenHidden, fadeOutSeconds);
    }
}
