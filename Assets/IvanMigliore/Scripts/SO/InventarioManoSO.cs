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

    // Resetta i riferimenti runtime ogni volta che l'asset viene caricato/abilitato.
    // Questo previene MissingReferenceException e stato sporcato nell'Editor
    // dopo che si esce dalla modalità Play o si cambia scena.
    private void OnEnable()
    {
        oggettoCorrente = null;
        currentGO = null;
        puntoMano = null;
    }
}