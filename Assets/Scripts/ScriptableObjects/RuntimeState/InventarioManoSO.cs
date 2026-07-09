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

    // Imposta entrambi i valori in un colpo solo e notifica l'evento
    public void ImpostaOggetto(DatiOggettoSO dati, GameObject go)
    {
        oggettoCorrente = dati;
        currentGO = go;
        OnInventarioAggiornato?.Invoke();
    }

    // Svuota la mano e notifica l'evento
    public void SvuotaMano()
    {
        oggettoCorrente = null;
        currentGO = null;
        OnInventarioAggiornato?.Invoke();
    }

    private void OnEnable()
    {
        oggettoCorrente = null;
        currentGO = null;
        puntoMano = null;
    }
}