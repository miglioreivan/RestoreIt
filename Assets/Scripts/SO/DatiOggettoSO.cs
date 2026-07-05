using UnityEngine;

[CreateAssetMenu(fileName = "NuovoDatoOggetto", menuName = "ScriptableObjects/DatiOggettoAvanzato")]
public class DatiOggettoSO : ScriptableObject
{
    public string nomeOggetto;

    [Header("Comunicazione")]
    [Tooltip("Inserisci qui il Canale Eventi per notificare altri sistemi (es. UI)")]
    [SerializeField] private VoidEventChannelSO raccogliOggetto;

    public virtual void EseguiInterazione()
    {
        Debug.Log($"Hai raccolto {nomeOggetto}!");
        if (raccogliOggetto) raccogliOggetto.RaiseEvent();
    }
}