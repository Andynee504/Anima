using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FootstepZone : MonoBehaviour
{
    [SerializeField] string playerTag = "Player";
    [SerializeField] FootstepProfile zoneProfile;      // ex.: passos na grama
    [SerializeField] FootstepProfile fallbackToDefault; // geralmente deixe vazio (manager usa default)

    void Reset() { GetComponent<Collider>().isTrigger = true; }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        var manager = other.GetComponentInChildren<FootstepManager>();
        if (manager) manager.ApplyProfile(zoneProfile);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        var manager = other.GetComponentInChildren<FootstepManager>();
        if (manager) manager.ClearProfile(fallbackToDefault);
    }
}
