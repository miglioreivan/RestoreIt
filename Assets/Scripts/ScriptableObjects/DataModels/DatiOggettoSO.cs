using UnityEngine;

/// <summary>
/// Classe base ScriptableObject che rappresenta i metadati generici di un oggetto o reperto archeologico.
/// </summary>
[CreateAssetMenu(fileName = "NuovoDatoOggetto", menuName = "ScriptableObjects/DatiOggettoAvanzato")]
public class DatiOggettoSO : ScriptableObject
{
    /// <summary>
    /// Il nome identificativo dell'oggetto.
    /// </summary>
    [Tooltip("Nome visualizzato del reperto.")]
    public string nomeOggetto;

    [Header("Comunicazione")]
    [Tooltip("Inserisci qui il Canale Eventi per notificare altri sistemi (es. UI)")]
    [SerializeField] private VoidEventChannelSO raccogliOggetto;

    /// <summary>
    /// Esegue l'interazione associata alla raccolta dell'oggetto e solleva l'evento sul canale dedicato.
    /// </summary>
    public virtual void EseguiInterazione()
    {
        RestoreLogger.Log($"Oggetto {nomeOggetto} raccolto.");
        if (raccogliOggetto) raccogliOggetto.RaiseEvent();
    }
}
