using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class AltarItem : XRGrabInteractable
{
    [Header("Lock / Interação")]
    [Tooltip("Permite hover, mas impede que o objeto seja pego (Select).")]
    public bool bloquearRetirada = false;

    [Tooltip("Desativa totalmente a interação (sem hover/sem Select).")]
    public bool desativarInteracao = false;

    [Header("Vínculo com o Altar")]
    [Tooltip("Script que mantém o item 'preso' ao altar (levitação/ímã/anchor). Será desativado no primeiro pickup, se marcado.")]
    public MonoBehaviour altarVinculoScript;

    [Tooltip("Ao pegar pela primeira vez, desativa o script acima para o altar não interagir mais com este item.")]
    public bool desativarVinculoAoPegar = true;

    private bool _foiPegoAlgumaVez = false;
    private InteractionLayerMask _layersOriginais;
    private bool _capturouLayers = false;

    protected override void OnEnable()
    {
        base.OnEnable();
        if (!_capturouLayers)
        {
            _layersOriginais = interactionLayers;
            _capturouLayers = true;
        }

        // Se nao possui referencia, procura no proprio item
        if (!altarVinculoScript)
            altarVinculoScript = GetComponent<AltarTether>();

        AplicarEstadoInteracao();
    }

    // Se trocar em runtime (UI/debug), chamar estes setters:
    public void SetBloquearRetirada(bool valor) { bloquearRetirada = valor; AplicarEstadoInteracao(); }
    public void SetDesativarInteracao(bool valor) { desativarInteracao = valor; AplicarEstadoInteracao(); }

    private void AplicarEstadoInteracao()
    {
        // Desativar tudo: tira o item do "sistema" de interação (sem hover/sem select)
        var none = (InteractionLayerMask)0;
        interactionLayers = desativarInteracao ? none : _layersOriginais;
        // Observacao: manteremos colliders e o componente ativos (assim o item continua visivel/animado/etc).
    }

    public override bool IsSelectableBy(IXRSelectInteractor interactor)
    {
        if (desativarInteracao) return false; // congelado total
        if (bloquearRetirada) return false; // permite hover, mas impede Select
        return base.IsSelectableBy(interactor);
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        if (!_foiPegoAlgumaVez)
        {
            _foiPegoAlgumaVez = true;

            // Depois que saiu do altar pela primeira vez, altar não manda mais nele
            if (desativarVinculoAoPegar && altarVinculoScript)
                altarVinculoScript.enabled = false;
        }
    }
}
