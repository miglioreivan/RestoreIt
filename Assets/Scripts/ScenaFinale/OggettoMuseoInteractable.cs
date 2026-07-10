using UnityEngine;

public class OggettoMuseoInteractable : MonoBehaviour, IInteractable
{
    [Header("Dati Oggetto Esposto")]
    [Tooltip("I dati (nome, descrizione, immagine) associati a questo specifico oggetto")]
    [SerializeField] private DatiOggettoMuseoSO datiOggetto;

    [Header("Riferimenti")]
    [Tooltip("Il gestore del canvas del museo (trovato automaticamente se lasciato vuoto)")]
    [SerializeField] private GestoreCanvasMuseo gestoreCanvas;

    private void Start()
    {
        // Se non è stato assegnato il gestore del canvas, cercalo nella scena
        if (gestoreCanvas == null)
        {
            gestoreCanvas = FindFirstObjectByType<GestoreCanvasMuseo>();
        }
    }

    /// <summary>
    /// Avvia l'interazione aprendo il canvas informativo.
    /// </summary>
    public void StartInteraction()
    {
        if (gestoreCanvas != null)
        {
            gestoreCanvas.MostraOggetto(datiOggetto);
        }
        else
        {
            Debug.LogError($"[OggettoMuseoInteractable] GestoreCanvasMuseo non trovato sulla scena per {gameObject.name}!");
        }
    }

    /// <summary>
    /// Ritorna il testo di interazione visualizzato nell'HUD del giocatore.
    /// </summary>
    public string GetInteractionText()
    {
        if (datiOggetto != null && !string.IsNullOrEmpty(datiOggetto.nomeOggetto))
        {
            return $"Premi [E] per esaminare {datiOggetto.nomeOggetto}";
        }
        return "Premi [E] per esaminare l'oggetto";
    }

    /// <summary>
    /// Determina se l'oggetto è attualmente interagibile.
    /// Impedisce l'interazione multipla se l'UI è già aperta.
    /// </summary>
    public bool canInteract()
    {
        if (gestoreCanvas != null && gestoreCanvas.IsCanvasAttivo)
        {
            return false;
        }
        return true;
    }
}
