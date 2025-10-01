using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class DoorTimedLockOnExit : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] SlidingDoorController door;

    [Header("Temporizador")]
    [SerializeField] float lockSeconds = 10f;

    [Header("Filtro de ator")]
    [SerializeField] LayerMask actorLayers = ~0; // aceita qualquer layer por padrao
    [SerializeField] string requiredTag = "Player";

    // Estado
    readonly HashSet<Transform> _actorsInside = new HashSet<Transform>();
    Coroutine _lockCo;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
        if (!door) door = GetComponentInParent<SlidingDoorController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsActor(other)) return;
        _actorsInside.Add(GetActorRoot(other));
        // Entrar NAO inicia temporizador; apenas registra presença
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsActor(other)) return;
        _actorsInside.Remove(GetActorRoot(other));

        // So dispara quando o ULTIMO sair e nao houver timer rolando
        if (_actorsInside.Count == 0 && _lockCo == null)
            _lockCo = StartCoroutine(LockRoutine());
    }

    IEnumerator LockRoutine()
    {
        if (door)
        {
            door.SetMode(SlidingDoorController.Mode.LockedClosed);
            door.Close();
        }

        yield return new WaitForSeconds(lockSeconds);

        if (door)
        {
            door.SetMode(SlidingDoorController.Mode.Auto);
            // Se player estiver no trigger quando terminar, abre agora
            if (door.HasPresence) door.Open();
        }
        _lockCo = null;
    }

    // ---- utilitários ----
    bool IsActor(Collider other)
    {
        bool layerOk = (actorLayers.value & (1 << other.gameObject.layer)) != 0;
        if (!layerOk) return false;

        if (string.IsNullOrEmpty(requiredTag)) return true;
        return other.CompareTag(requiredTag) || other.transform.root.CompareTag(requiredTag);
    }

    Transform GetActorRoot(Collider other)
    {
        if (other.attachedRigidbody) return other.attachedRigidbody.transform.root;
        return other.transform.root;
    }
}
