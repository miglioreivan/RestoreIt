using System;
using UnityEngine;

/// <summary>
/// ScriptableObject che persiste lo stato dell'inventario in mano al giocatore.
/// Gestisce l'oggetto corrente, il suo GameObject associato e la notifica degli eventi di aggiornamento.
/// </summary>
[CreateAssetMenu(fileName = "NuovaMano", menuName = "ScriptableObjects/InventarioMano")]
public class InventarioManoSO : ScriptableObject
{
    /// <summary>
    /// I dati dell'oggetto attualmente raccolto e tenuto in mano dal giocatore.
    /// </summary>
    [Tooltip("L'oggetto raccolto dal giocatore")]
    public DatiOggettoSO oggettoCorrente;

    /// <summary>
    /// Il GameObject fisico istanziato associato all'oggetto tenuto in mano.
    /// </summary>
    [Tooltip("Il modello 3D fisico da spostare")]
    public GameObject currentGO;

    /// <summary>
    /// Il punto Transform di ancoraggio della mano del giocatore.
    /// </summary>
    [Tooltip("Il Transform della mano dove posizionare il modello")]
    public Transform puntoMano;

    /// <summary>
    /// Evento sollevato ogni volta che il contenuto dell'inventario o lo stato dell'oggetto in mano cambia.
    /// </summary>
    public event Action OnInventarioAggiornato;

    /// <summary>
    /// Indica se l'oggetto correntemente in mano è già stato restaurato.
    /// </summary>
    public bool IsRestored { get; private set; }

    /// <summary>
    /// Imposta i dati e il GameObject dell'oggetto in mano in un'unica operazione e solleva l'evento di aggiornamento.
    /// </summary>
    /// <param name="dati">I dati dell'oggetto.</param>
    /// <param name="go">Il GameObject associato.</param>
    public void ImpostaOggetto(DatiOggettoSO dati, GameObject go)
    {
        oggettoCorrente = dati;
        currentGO = go;
        IsRestored = RestorationUtils.IsOggettoRestaurato(go);
        
        int count = OnInventarioAggiornato != null ? OnInventarioAggiornato.GetInvocationList().Length : 0;
        RestoreLogger.Log($"[ManoSO] ImpostaOggetto chiamato. Oggetto: {(dati != null ? dati.nomeOggetto : "null")}, GO: {(go != null ? go.name : "null")}. Notifica a {count} ascoltatori.");
        
        OnInventarioAggiornato?.Invoke();
    }

    /// <summary>
    /// Svuota l'inventario in mano al giocatore, azzerando i riferimenti e sollevando l'evento di aggiornamento.
    /// </summary>
    public void SvuotaMano()
    {
        RestoreLogger.Log("[ManoSO] SvuotaMano chiamato.");
        oggettoCorrente = null;
        currentGO = null;
        IsRestored = false;
        
        int count = OnInventarioAggiornato != null ? OnInventarioAggiornato.GetInvocationList().Length : 0;
        RestoreLogger.Log($"[ManoSO] Stato mano azzerato. Notifica a {count} ascoltatori.");
        
        OnInventarioAggiornato?.Invoke();
    }

    private void OnEnable()
    {
        oggettoCorrente = null;
        currentGO = null;
        puntoMano = null;
        IsRestored = false;
    }
}
