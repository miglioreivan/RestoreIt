using UnityEngine;

public class PickUp_Interaction : MonoBehaviour, IInteractable
{
    [SerializeField] private DatiOggettoSO datiOggetto; // Tipo di oggetto assegnato (SO)
    [SerializeField] private InventarioManoSO manoGiocatore; // SO Inventario
    [SerializeField] private VoidEventChannelSO onPickUp; // tipo di Azione

    public bool canInteract()
    {
        return manoGiocatore.oggettoCorrente ==  null;
    }

    public string GetInteractionText()
    {
        if (canInteract())
        {
            return "[E] Raccogli" + datiOggetto.nomeOggetto;
        }
        else
        {
            return "Hai già un oggetto in mnao!";
        }
    }
    
    public void StartInteraction()
    {
        if (!canInteract()) return;
        
        manoGiocatore.oggettoCorrente = datiOggetto;
        manoGiocatore.currentGO = this.gameObject;
        
        transform.SetParent(manoGiocatore.puntoMano, false);
        transform.localPosition = Vector3.zero;
        if (TryGetComponent(out Collider col))
            col.enabled = false;
        
        datiOggetto.EseguiInterazione();
    }
}
