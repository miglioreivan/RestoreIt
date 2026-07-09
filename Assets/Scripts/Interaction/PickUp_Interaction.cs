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
            Debug.LogError($"Componente manoGiocatore non assegnato nell'Inspector su {gameObject.name}.");
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

        // Determina qual è l'oggetto effettivo da raccogliere.
        // Se siamo su un tavolo e questo oggetto è figlio della vaschetta o dell'anfora assemblata, raccogliamo il padre.
        GameObject oggettoDaRaccogliere = this.gameObject;
        if (tavoloCorrente != null)
        {
            if (tavoloCorrente.vaschettaGameObject != null && (this.gameObject == tavoloCorrente.vaschettaGameObject || transform.IsChildOf(tavoloCorrente.vaschettaGameObject.transform)))
            {
                oggettoDaRaccogliere = tavoloCorrente.vaschettaGameObject;
            }
            else if (tavoloCorrente.anforaAssemblata != null && (this.gameObject == tavoloCorrente.anforaAssemblata || transform.IsChildOf(tavoloCorrente.anforaAssemblata.transform)))
            {
                oggettoDaRaccogliere = tavoloCorrente.anforaAssemblata;
            }
        }

        // Se l'oggetto o la sua gerarchia originale è contrassegnata come restaurata, garantisce che il tag sia presente sull'oggetto raccolto
        bool wasRestored = oggettoDaRaccogliere.GetComponent<OggettoRestaurato>() != null ||
                           oggettoDaRaccogliere.GetComponentInParent<OggettoRestaurato>() != null ||
                           oggettoDaRaccogliere.GetComponentInChildren<OggettoRestaurato>() != null;

        if (wasRestored && oggettoDaRaccogliere.GetComponent<OggettoRestaurato>() == null)
        {
            oggettoDaRaccogliere.AddComponent<OggettoRestaurato>();
            Debug.Log($"Componente OggettoRestaurato propagato direttamente a {oggettoDaRaccogliere.name} durante la raccolta.");
        }

        manoGiocatore.ImpostaOggetto(datiOggetto, oggettoDaRaccogliere);

        // Memorizzazione della scala globale per evitare distorsioni dimensionali dopo il reparenting
        Vector3 targetWorldScale = oggettoDaRaccogliere.transform.lossyScale;

        oggettoDaRaccogliere.transform.SetParent(manoGiocatore.puntoMano, false);
        oggettoDaRaccogliere.transform.localPosition = Vector3.zero;
        oggettoDaRaccogliere.transform.localRotation = Quaternion.identity;

        // Compensazione della scala in base alle dimensioni globali del nuovo genitore
        if (manoGiocatore.puntoMano != null)
        {
            Vector3 parentLossyScale = manoGiocatore.puntoMano.lossyScale;
            oggettoDaRaccogliere.transform.localScale = new Vector3(
                parentLossyScale.x != 0 ? targetWorldScale.x / parentLossyScale.x : targetWorldScale.x,
                parentLossyScale.y != 0 ? targetWorldScale.y / parentLossyScale.y : targetWorldScale.y,
                parentLossyScale.z != 0 ? targetWorldScale.z / parentLossyScale.z : targetWorldScale.z
            );
        }
        else
        {
            oggettoDaRaccogliere.transform.localScale = targetWorldScale;
        }

        foreach (var col in oggettoDaRaccogliere.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        if (datiOggetto != null)
        {
            datiOggetto.EseguiInterazione();
        }

        if (tavoloCorrente != null)
        {
            if (tavoloCorrente.vaschettaGameObject == oggettoDaRaccogliere)
            {
                tavoloCorrente.vaschettaGameObject = null;
            }
            if (tavoloCorrente.anforaAssemblata == oggettoDaRaccogliere)
            {
                tavoloCorrente.anforaAssemblata = null;
            }
            tavoloCorrente.SvuotaTavolo();
        }
    }
}
