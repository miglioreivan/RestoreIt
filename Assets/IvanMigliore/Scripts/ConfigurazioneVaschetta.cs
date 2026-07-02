using UnityEngine;
using System.Collections.Generic;

public class ConfigurazioneVaschetta : MonoBehaviour
{
    [Header("Maschera Colla")]
    [Tooltip("La maschera della colla B/N unica per tutta l'anfora.")]
    public Texture2D mascheraCollaUnica;

    [Header("Configurazione Pezzi (Ordinati dal primo all'ultimo)")]
    [Tooltip("Trascina qui i pezzi nell'ordine esatto di posizionamento dal basso verso l'alto.")]
    public List<GameObject> pezziOrdinati = new List<GameObject>();
}
