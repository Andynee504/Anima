using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorProximity : MonoBehaviour
{
    [SerializeField] SlidingDoorController door;
    [SerializeField] LayerMask actorLayers = ~0; // por padrão, qualquer layer
    [SerializeField] string requiredTag = "Player"; // opcional: deixe vazio para ignorar

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
        if (!door) door = GetComponentInParent<SlidingDoorController>();
    }

    bool IsActor(Collider other)
    {
        bool layerOk = (actorLayers.value & (1 << other.gameObject.layer)) != 0;
        bool tagOk = string.IsNullOrEmpty(requiredTag) || other.CompareTag(requiredTag);
        return layerOk && tagOk;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!door || !IsActor(other)) return;
        door.OnActorEnter();
    }

    void OnTriggerExit(Collider other)
    {
        if (!door || !IsActor(other)) return;
        door.OnActorExit();
    }
}
