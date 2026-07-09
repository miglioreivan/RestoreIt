using UnityEngine;

[CreateAssetMenu(fileName = "NuovoMosaico", menuName = "ScriptableObjects/Mosaico")]
public class MosaicoSO : DatiOggettoSO
{
    [Header("Fase Garze")]
    [Tooltip("Il prefabbricato della garza da applicare su questo mosaico")]
    public GameObject prefabGarza;
    [Tooltip("Il prefabbricato di Aerolam da applicare sul retro di questo mosaico")]
    public GameObject prefabAerolam;

    [Header("Fase Incollaggio")]
    [Tooltip("La maschera della colla B/N unica per tutto il mosaico.")]
    [SerializeField] private Texture2D mascheraCollaMosaico;
    [Tooltip("La maschera della resina B/N per il retro del mosaico.")]
    [SerializeField] private Texture2D mascheraResinaMosaico;
    [Tooltip("Percentuale minima di colla richiesta per completare la fase di incollaggio (es. 0.70 = 70%).")]
    [SerializeField] [Range(0.1f, 1f)] private float sogliaCompletamentoColla = 0.70f;

    public Texture2D  MascheraCollaMosaico => mascheraCollaMosaico;
    public Texture2D  MascheraResinaMosaico => mascheraResinaMosaico;
    public float      SogliaCompletamentoColla => sogliaCompletamentoColla;
}
