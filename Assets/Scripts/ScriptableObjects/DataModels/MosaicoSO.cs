using UnityEngine;

/// <summary>
/// ScriptableObject che estende DatiOggettoSO per modellare le configurazioni specifiche di un mosaico.
/// Contiene i riferimenti ai prefab di rinforzo (garza, aerolam), le maschere di colla/resina e le relative soglie.
/// </summary>
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
    
    [Tooltip("Percentuale minima di resina richiesta per completare la fase di resinatura (es. 0.70 = 70%).")]
    [SerializeField] [Range(0.1f, 1f)] private float sogliaCompletamentoResina = 0.70f;

    /// <summary>
    /// La texture2D in bianco e nero usata come maschera di pittura per la colla del mosaico.
    /// </summary>
    public Texture2D  MascheraCollaMosaico => mascheraCollaMosaico;

    /// <summary>
    /// La texture2D in bianco e nero usata come maschera di pittura per la resina del mosaico.
    /// </summary>
    public Texture2D  MascheraResinaMosaico => mascheraResinaMosaico;

    /// <summary>
    /// Percentuale normalizzata (0-1) di colla necessaria a considerare la fase completata.
    /// </summary>
    public float      SogliaCompletamentoColla => sogliaCompletamentoColla;

    /// <summary>
    /// Percentuale normalizzata (0-1) di resina necessaria a considerare la fase completata.
    /// </summary>
    public float      SogliaCompletamentoResina => sogliaCompletamentoResina;
}
