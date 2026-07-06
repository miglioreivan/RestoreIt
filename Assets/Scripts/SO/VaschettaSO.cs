using UnityEngine;
using UnityEngine.Serialization;

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

    // Proprietà standard PascalCase
    public GameObject PrefabAnfora      => _prefabAnfora;
    public GameObject PrefabPezzi       => _prefabPezzi;
    public GameObject PrefabAnforaIntera => _prefabAnforaIntera;
    public Texture2D  MascheraCollaUnica => _mascheraCollaUnica;

    // Proprietà backward-compatibility camelCase
    public GameObject prefabAnfora      => PrefabAnfora;
    public GameObject prefabPezzi       => PrefabPezzi;
    public GameObject prefabAnforaIntera => PrefabAnforaIntera;
    public Texture2D  mascheraCollaUnica => MascheraCollaUnica;

    [Header("Incollaggio")]
    [Tooltip("Percentuale minima di colla richiesta per completare la fase di incollaggio (es. 0.70 = 70%).")]
    [SerializeField] [Range(0.1f, 1f)] private float sogliaCompletamentoColla = 0.70f;
    public float SogliaCompletamentoColla => sogliaCompletamentoColla;
}
