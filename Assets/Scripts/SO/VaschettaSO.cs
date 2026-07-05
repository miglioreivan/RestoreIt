using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NuovaVaschetta", menuName = "ScriptableObjects/Vaschetta")]
public class VaschettaSO : DatiOggettoSO
{
    [Header("Anfora")]
    public GameObject prefabAnfora;
    public GameObject prefabPezzi;
    [Tooltip("Il prefab dell'anfora intera e sana (da caricare alla fine dell'incollaggio).")]
    public GameObject prefabAnforaIntera;
    
    [Header("Maschera Colla")]
    [Tooltip("La maschera della colla B/N unica per tutta l'anfora.")]
    public Texture2D mascheraCollaUnica;
}
