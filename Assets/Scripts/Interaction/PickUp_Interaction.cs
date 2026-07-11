using UnityEngine;

/// <summary>
/// Gestisce la meccanica di raccolta di un oggetto interattivo nel mondo,
/// inserendolo nell'inventario mano del giocatore e ripulendo lo stato del tavolo se necessario.
/// </summary>
public class PickUp_Interaction : MonoBehaviour, IInteractable
{
    [SerializeField] private DatiOggettoSO datiOggetto;
    [SerializeField] private InventarioManoSO manoGiocatore;

    [Header("Restauro / Tavolo (Opzionale)")]
    [SerializeField] private TavoloSO tavoloCorrente;

    [Header("Audio")]
    [SerializeField] private SoundEffect pickupSound;

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
        bool wasRestored = RestorationUtils.IsOggettoRestaurato(oggettoDaRaccogliere);

        if (wasRestored && oggettoDaRaccogliere.GetComponent<OggettoRestaurato>() == null)
        {
            oggettoDaRaccogliere.AddComponent<OggettoRestaurato>();
            RestoreLogger.Log($"Componente OggettoRestaurato propagato direttamente a {oggettoDaRaccogliere.name} durante la raccolta.");
        }

        manoGiocatore.ImpostaOggetto(datiOggetto, oggettoDaRaccogliere);

        RestorationUtils.ReparentPreservingScale(oggettoDaRaccogliere.transform, manoGiocatore.puntoMano);

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

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning($"[PickUp_Interaction] '{gameObject.name}': AudioManager.Instance è null! Assicurati che un GameObject con AudioManager esista nella scena.");
        }
        else if (pickupSound.clip == null)
        {
            Debug.LogWarning($"[PickUp_Interaction] '{gameObject.name}': pickupSound.clip non è assegnato nell'Inspector. Nessun suono verrà riprodotto.");
        }
        else
        {
            AudioManager.Instance.Play2D(pickupSound);
        }
    }
}
