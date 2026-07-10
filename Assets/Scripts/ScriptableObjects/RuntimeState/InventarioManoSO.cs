using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NuovaMano", menuName = "ScriptableObjects/InventarioMano")]
public class InventarioManoSO : ScriptableObject
{
    [Tooltip("L'oggetto raccolto dal giocatore")]
    public DatiOggettoSO oggettoCorrente;

    [Tooltip("Il modello 3D fisico da spostare")]
    public GameObject currentGO;

    [Tooltip("Il Transform della mano dove posizionare il modello")]
    public Transform puntoMano;

    // Notifica gli ascoltatori ogni volta che il contenuto della mano cambia
    public event Action OnInventarioAggiornato;

    public bool isRestored { get; private set; }

    // Imposta entrambi i valori in un colpo solo e notifica l'evento
    public void ImpostaOggetto(DatiOggettoSO dati, GameObject go)
    {
        oggettoCorrente = dati;
        currentGO = go;
        isRestored = go != null && (
            go.GetComponent<OggettoRestaurato>() != null ||
            go.GetComponentInParent<OggettoRestaurato>() != null ||
            go.GetComponentInChildren<OggettoRestaurato>() != null
        );
        
        int count = OnInventarioAggiornato != null ? OnInventarioAggiornato.GetInvocationList().Length : 0;
        Debug.Log($"[ManoSO] ImpostaOggetto chiamato. Oggetto: {(dati != null ? dati.nomeOggetto : "null")}, GO: {(go != null ? go.name : "null")}. Notifica a {count} ascoltatori.");
        
        OnInventarioAggiornato?.Invoke();
    }

    // Svuota la mano e notifica l'evento
    public void SvuotaMano()
    {
        Debug.Log("[ManoSO] SvuotaMano chiamato.");
        oggettoCorrente = null;
        currentGO = null;
        isRestored = false;
        
        int count = OnInventarioAggiornato != null ? OnInventarioAggiornato.GetInvocationList().Length : 0;
        Debug.Log($"[ManoSO] Stato mano azzerato. Notifica a {count} ascoltatori.");
        
        OnInventarioAggiornato?.Invoke();
    }

    private void OnEnable()
    {
        oggettoCorrente = null;
        currentGO = null;
        puntoMano = null;
        isRestored = false;
    }
}