using UnityEngine;

public class PickUp_Interaction : MonoBehaviour, IInteractable
{
    [SerializeField] private DatiOggettoSO datiOggetto; // Tipo di oggetto assegnato (SO)

    public void StartInteraction()
    {
        if (datiOggetto != null)
        {
            datiOggetto.EseguiInterazione();
        }
        
        Destroy(gameObject);
    }
}
