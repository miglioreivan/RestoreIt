using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Gestisce la visualizzazione del pannello informativo del museo 2D, mostrando
/// i dettagli del reperto esposto e controllando lo stato del cursore e il movimento del giocatore.
/// </summary>
public class GestoreCanvasMuseo : MonoBehaviour
{
    [Header("Componenti UI")]
    [Tooltip("Il pannello principale del canvas da mostrare/nascondere")]
    [SerializeField] private GameObject pannelloCanvas;

    [Tooltip("Immagine di visualizzazione dell'oggetto")]
    [SerializeField] private Image immagineOggetto;

    [Header("Giocatore")]
    [Tooltip("Riferimento al controller del giocatore (trovato automaticamente all'inizio se lasciato vuoto)")]
    [SerializeField] private FirstPersonController playerController;

    /// <summary>
    /// Ritorna true se il pannello del Canvas del museo è attualmente aperto.
    /// </summary>
    public bool IsCanvasAttivo => pannelloCanvas != null && pannelloCanvas.activeSelf;

    private void Start()
    {
        // Assicurati che il canvas sia disattivato all'avvio
        if (pannelloCanvas != null)
        {
            pannelloCanvas.SetActive(false);
        }

        // Trova il FirstPersonController nella scena se non assegnato
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<FirstPersonController>();
        }
    }

    private void Update()
    {
        // Consente di chiudere il canvas premendo Escape o E con il nuovo Input System
        if (IsCanvasAttivo)
        {
            if (Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame))
            {
                ChiudiCanvas();
            }
        }
    }

    /// <summary>
    /// Mostra il Canvas compilando i dati forniti dall'oggetto esposto.
    /// </summary>
    /// <param name="dati">I dati ScriptableObject dell'oggetto esposto.</param>
    public void MostraOggetto(DatiOggettoMuseoSO dati)
    {
        if (dati == null)
        {
            Debug.LogWarning("[GestoreCanvasMuseo] Chiamata a MostraOggetto con dati null!");
            return;
        }

        // Attiva il Canvas
        if (pannelloCanvas != null)
        {
            pannelloCanvas.SetActive(true);
        }

        // Imposta e mostra l'immagine solo se presente
        if (immagineOggetto != null)
        {
            if (dati.immagineOggetto != null)
            {
                immagineOggetto.sprite = dati.immagineOggetto;
                immagineOggetto.gameObject.SetActive(true);
            }
            else
            {
                immagineOggetto.gameObject.SetActive(false);
            }
        }

        // Disabilita il controller del giocatore e nasconde il testo di interazione dell'HUD
        if (playerController != null)
        {
            playerController.enabled = false;
            playerController.NascondiTestoInterazione();
        }

        // Sblocca e mostra il cursore
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Chiude il Canvas e ripristina lo stato del giocatore.
    /// </summary>
    public void ChiudiCanvas()
    {
        if (pannelloCanvas != null)
        {
            pannelloCanvas.SetActive(false);
        }

        // Riabilita il controller del giocatore e riaccende il testo di interazione
        if (playerController != null)
        {
            playerController.enabled = true;
            playerController.MostraTestoInterazione();
        }

        // Blocca e nasconde nuovamente il cursore per il gameplay standard
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
