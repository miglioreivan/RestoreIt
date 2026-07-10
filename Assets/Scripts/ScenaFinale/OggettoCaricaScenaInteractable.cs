using UnityEngine;
using UnityEngine.SceneManagement;

public class OggettoCaricaScenaInteractable : MonoBehaviour, IInteractable
{
    [Header("Impostazioni Scena")]
    [Tooltip("L'indice build della scena da caricare (es. 0 per tornare al Menu Principale)")]
    [SerializeField] private int indiceScena = 0;

    [Tooltip("Testo che compare sull'HUD del giocatore quando guarda l'oggetto")]
    [SerializeField] private string testoInterazione = "Premi [E] per uscire dal museo";

    /// <summary>
    /// Avvia l'interazione caricando direttamente la scena configurata.
    /// </summary>
    public void StartInteraction()
    {
        Debug.Log($"[OggettoCaricaScenaInteractable] Caricamento della scena con indice {indiceScena}...");
        SceneManager.LoadScene(indiceScena);
    }

    /// <summary>
    /// Ritorna il testo visualizzato nell'HUD del giocatore.
    /// </summary>
    public string GetInteractionText()
    {
        return testoInterazione;
    }

    /// <summary>
    /// Determina se l'oggetto è interagibile. Sempre true per il cambio scena.
    /// </summary>
    public bool canInteract()
    {
        return true;
    }
}
