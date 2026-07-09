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
        AggiornaSuggerimento();
    }

    private void OnEnable()
    {
        if (inventario != null)
            inventario.OnInventarioAggiornato += AggiornaSuggerimento;

        // Sincronizza subito il testo con lo stato attuale dell'inventario
        AggiornaSuggerimento();
    }

    private void OnDisable()
    {
        if (inventario != null)
            inventario.OnInventarioAggiornato -= AggiornaSuggerimento;
    }

    private void AggiornaSuggerimento()
    {
        if (testoSuggerimento == null || inventario == null)
            return;

        if (inventario.currentGO == null)
        {
            testoSuggerimento.text = testoManoVuota;
            return;
        }

        // Controlla se l'oggetto in mano è già stato restaurato
        GameObject go = inventario.currentGO;
        bool isRestored = go.GetComponent<OggettoRestaurato>() != null ||
                          go.GetComponentInParent<OggettoRestaurato>() != null ||
                          go.GetComponentInChildren<OggettoRestaurato>() != null;

        testoSuggerimento.text = isRestored ? testoOggettoFinito : testoManoPiena;
    }
}
