using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NuovaVaschetta", menuName = "ScriptableObjects/Vaschetta")]
public class VaschettaSO : DatiOggettoSO
{
    [Header("Anfora")]
    public GameObject prefabAnfora;
    public GameObject prefabPezzi;
}
