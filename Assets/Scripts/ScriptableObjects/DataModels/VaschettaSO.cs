using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// ScriptableObject che modella i dati di configurazione specifici di un reperto di tipo vaschetta o anfora.
/// Contiene i riferimenti ai prefab delle varie fasi del restauro e la soglia di completamento.
/// </summary>
[CreateAssetMenu(fileName = "NuovaVaschetta", menuName = "ScriptableObjects/Vaschetta")]
public class VaschettaSO : DatiOggettoSO
{
    [Header("Anfora")]
    [FormerlySerializedAs("prefabAnfora")]
    [SerializeField] private GameObject _prefabAnfora;
    
    [FormerlySerializedAs("prefabPezzi")]
    [SerializeField] private GameObject _prefabPezzi;
    
    [Tooltip("Il prefab dell'anfora intera e sana (da caricare alla fine dell'incollaggio).")]
    [FormerlySerializedAs("prefabAnforaIntera")]
    [SerializeField] private GameObject _prefabAnforaIntera;
    
    [Header("Maschera Colla")]
    [Tooltip("La maschera della colla B/N unica per tutta l'anfora.")]
    [FormerlySerializedAs("mascheraCollaUnica")]
    [SerializeField] private Texture2D _mascheraCollaUnica;

    /// <summary>
    /// Il prefab dell'anfora parziale semitrasparente (modello guida).
    /// </summary>
    public GameObject PrefabAnfora      => _prefabAnfora;

    /// <summary>
    /// Il prefab contenente tutti i singoli frammenti separati dell'anfora.
    /// </summary>
    public GameObject PrefabPezzi       => _prefabPezzi;

    /// <summary>
    /// Il prefab del modello sano e ripristinato dell'anfora.
    /// </summary>
    public GameObject PrefabAnforaIntera => _prefabAnforaIntera;

    /// <summary>
    /// La texture2D in bianco e nero usata come maschera di pittura per la colla.
    /// </summary>
    public Texture2D  MascheraCollaUnica => _mascheraCollaUnica;

    // Proprietà backward-compatibility camelCase
    public GameObject prefabAnfora      => PrefabAnfora;
    public GameObject prefabPezzi       => PrefabPezzi;
    public GameObject prefabAnforaIntera => PrefabAnforaIntera;
    public Texture2D  mascheraCollaUnica => MascheraCollaUnica;

    [Header("Incollaggio")]
    [Tooltip("Percentuale minima di colla richiesta per completare la fase di incollaggio (es. 0.70 = 70%).")]
    [SerializeField] [Range(0.1f, 1f)] private float sogliaCompletamentoColla = 0.70f;

    /// <summary>
    /// Percentuale normalizzata (0-1) di colla necessaria a considerare la fase completata.
    /// </summary>
    public float SogliaCompletamentoColla => sogliaCompletamentoColla;
}
