using UnityEngine;

public class PickUp_Interaction : MonoBehaviour, IInteractable
{
    [SerializeField] private DatiOggettoSO datiOggetto;
    [SerializeField] private InventarioManoSO manoGiocatore;

    [Header("Restauro / Tavolo (Opzionale)")]
    [SerializeField] private TavoloSO tavoloCorrente;

    private void Awake()
    {
        if (manoGiocatore == null)
            Debug.LogError($"[PickUp_Interaction] '{gameObject.name}': manoGiocatore non assegnato nell'Inspector.");
    }

    public void ImpostaTavolo(TavoloSO tavolo)
    {
        tavoloCorrente = tavolo;
    }

    public bool canInteract()
    {
        return manoGiocatore.oggettoCorrente == null && manoGiocatore.currentGO == null;
    }

    public string GetInteractionText()
    {
        if (canInteract())
        {
            string nome = datiOggetto != null ? datiOggetto.nomeOggetto : gameObject.name;
            return "[E] Raccogli " + nome;
        }
        else
        {
            return "Hai già un oggetto in mano!";
        }
    }

    public void StartInteraction()
    {
        if (!canInteract()) return;

        manoGiocatore.oggettoCorrente = datiOggetto;
        manoGiocatore.currentGO = this.gameObject;

        // Salva la scala globale originale prima del cambio di parent per evitare distorsioni
        Vector3 targetWorldScale = transform.lossyScale;

        transform.SetParent(manoGiocatore.puntoMano, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // Ricalcola la scala locale in base alla scala globale del nuovo parent
        if (manoGiocatore.puntoMano != null)
        {
            Vector3 parentLossyScale = manoGiocatore.puntoMano.lossyScale;
            transform.localScale = new Vector3(
                parentLossyScale.x != 0 ? targetWorldScale.x / parentLossyScale.x : targetWorldScale.x,
                parentLossyScale.y != 0 ? targetWorldScale.y / parentLossyScale.y : targetWorldScale.y,
                parentLossyScale.z != 0 ? targetWorldScale.z / parentLossyScale.z : targetWorldScale.z
            );
        }
        else
        {
            transform.localScale = targetWorldScale;
        }

        if (TryGetComponent(out Collider col))
            col.enabled = false;

        if (datiOggetto != null)
        {
            datiOggetto.EseguiInterazione();
        }

        if (tavoloCorrente != null)
        {
            tavoloCorrente.SvuotaTavolo();
        }
    }
}
