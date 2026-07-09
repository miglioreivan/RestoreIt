using System;
using TMPro;
using UnityEngine;

// Mostra al giocatore un suggerimento contestuale in base allo stato della sua mano.
// Reagisce esclusivamente all'evento OnInventarioAggiornato, non in Update.
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
        Debug.Log("[SuggerimentoMano] Start chiamato.");
        AggiornaSuggerimento();
    }

    private void OnEnable()
    {
        if (inventario != null)
        {
            inventario.OnInventarioAggiornato += AggiornaSuggerimento;
            Debug.Log("[SuggerimentoMano] Sottoscrizione all'evento OnInventarioAggiornato eseguita.");
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
            Debug.Log("[SuggerimentoMano] Annullamento sottoscrizione all'evento OnInventarioAggiornato eseguita.");
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
            Debug.Log($"[SuggerimentoMano] Inventario vuoto. Testo UI impostato a: \"{testoManoVuota}\"");
            return;
        }

        // Controlla se l'oggetto in mano è già stato restaurato
        GameObject go = inventario.currentGO;
        bool isRestored = go.GetComponent<OggettoRestaurato>() != null ||
                          go.GetComponentInParent<OggettoRestaurato>() != null ||
                          go.GetComponentInChildren<OggettoRestaurato>() != null;

        string testoScelto = isRestored ? testoOggettoFinito : testoManoPiena;
        testoSuggerimento.text = testoScelto;
        
        Debug.Log($"[SuggerimentoMano] Oggetto in mano: {go.name}. Restaurato: {isRestored}. Testo UI impostato a: \"{testoScelto}\"");
    }
}
