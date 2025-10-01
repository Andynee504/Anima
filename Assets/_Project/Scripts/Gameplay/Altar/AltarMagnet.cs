using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Collider))]
public class AltarMagnet : MonoBehaviour
{
    [Header("Âncoras/Slots (ordem = prioridade)")]
    public Transform[] anchorSlots;

    [Header("Atração")]
    [Tooltip("Quão rápido o item vai para o slot.")]
    public float moveSpeed = 4f;
    [Tooltip("Alinha rotação do item ao slot?")]
    public bool alignRotation = true;

    [Header("Filtro")]
    [Tooltip("Apenas camadas físicas aceitas (opcional).")]
    public LayerMask itemLayers = ~0;

    // mapeia item -> slotIndex
    private readonly Dictionary<Transform, int> _occupied = new();

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true; // altar deve ter trigger
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & itemLayers.value) == 0) return;

        var itemTF = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;

        var grab = itemTF.GetComponent<XRGrabInteractable>();
        if (!grab) return;

        // não magnetizar se está na mão
        if (grab.isSelected) return;

        var tether = itemTF.GetComponent<AltarTether>();
        if (!tether) tether = itemTF.gameObject.AddComponent<AltarTether>();

        int slot = FindFreeSlot();
        if (slot < 0) return;

        _occupied[itemTF] = slot;

        tether.enabled = true;
        tether.SetTarget(anchorSlots[slot], moveSpeed, alignRotation);
    }

    void OnTriggerExit(Collider other)
    {
        var itemTF = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;

        if (_occupied.TryGetValue(itemTF, out int slot))
            _occupied.Remove(itemTF);

        var tether = itemTF.GetComponent<AltarTether>();
        if (tether)
        {
            tether.ClearTarget();      // limpa alvo; AltarTether decide física no próximo frame
            if (tether.releaseOnExit) tether.enabled = false;
        }
    }

    int FindFreeSlot()
    {
        if (anchorSlots == null || anchorSlots.Length == 0) return -1;

        for (int i = 0; i < anchorSlots.Length; i++)
        {
            bool busy = false;
            foreach (var kv in _occupied)
            {
                if (kv.Value == i) { busy = true; break; }
            }
            if (!busy) return i;
        }
        return -1;
    }

    // Liberar slot quando item for selecionado (opcional, se quiser registrar via evento)
    public void NotifyItemSelected(Transform item)
    {
        if (_occupied.ContainsKey(item)) _occupied.Remove(item);
    }
}
