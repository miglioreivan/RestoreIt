using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Componente di supporto applicato al GameObject della vaschetta per memorizzare
/// la lista ordinata dei singoli frammenti 3D dell'anfora da posizionare.
/// </summary>
public class ConfigurazioneVaschetta : MonoBehaviour
{
    /// <summary>
    /// Lista ordinata dei frammenti 3D presenti all'interno della vaschetta.
    /// </summary>
    [Header("Configurazione Pezzi")]
    public List<GameObject> pezziOrdinati = new List<GameObject>();
}
