using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Componente UI che visualizza suggerimenti contestuali per guidare il giocatore
/// reagendo reattivamente all'evento OnInventarioAggiornato dell'inventario (senza polling in Update).
/// </summary>
public class SuggerimentoMano : MonoBehaviour
{
    [Header("Riferimenti")]
    [SerializeField] private InventarioManoSO inventario;
    [SerializeField] private TextMeshProUGUI testoSuggerimento;

    [Header("Testi")]
    [SerializeField] private string testoManoVuota     = "Raccogli un oggetto da restaurare";
    [SerializeField] private string testoManoPiena     = "Porta l'oggetto al tavolo di restauro";
    [SerializeField] private string testoOggettoFinito = "Esponi l'oggetto nel museo";

    private void Start()
    {
        RestoreLogger.Log("[SuggerimentoMano] Start chiamato.");
        AggiornaSuggerimento();
    }

    private void OnEnable()
    {
        if (inventario != null)
        {
            inventario.OnInventarioAggiornato += AggiornaSuggerimento;
            RestoreLogger.Log("[SuggerimentoMano] Sottoscrizione all'evento OnInventarioAggiornato eseguita.");
        }
        else
        {
            Debug.LogError("[SuggerimentoMano] InventarioManoSO non assegnato nell'Inspector.");
        }

        // Sincronizza subito il testo con lo stato attuale dell'inventario
        AggiornaSuggerimento();
    }

    private void OnDisable()
    {
        if (inventario != null)
        {
            inventario.OnInventarioAggiornato -= AggiornaSuggerimento;
            RestoreLogger.Log("[SuggerimentoMano] Annullamento sottoscrizione all'evento OnInventarioAggiornato eseguita.");
        }
    }

    public void AggiornaSuggerimento()
    {
        if (testoSuggerimento == null)
        {
            Debug.LogError("[SuggerimentoMano] Riferimento TextMeshProUGUI testoSuggerimento non assegnato.");
            return;
        }

        if (inventario == null)
        {
            Debug.LogError("[SuggerimentoMano] Riferimento InventarioManoSO inventario è null.");
            return;
        }

        if (inventario.currentGO == null)
        {
            testoSuggerimento.text = testoManoVuota;
            RestoreLogger.Log($"[SuggerimentoMano] Inventario vuoto. Testo UI impostato a: \"{testoManoVuota}\"");
            return;
        }

        // Controlla se l'oggetto in mano è già stato restaurato
        bool IsRestored = inventario.IsRestored;

        string testoScelto = IsRestored ? testoOggettoFinito : testoManoPiena;
        testoSuggerimento.text = testoScelto;
        
        RestoreLogger.Log($"[SuggerimentoMano] Oggetto in mano: {inventario.currentGO.name}. Restaurato: {IsRestored}. Testo UI impostato a: \"{testoScelto}\"");
    }

    public void ApriMuseo()
    {
        SceneManager.LoadScene(2);
    }
}
