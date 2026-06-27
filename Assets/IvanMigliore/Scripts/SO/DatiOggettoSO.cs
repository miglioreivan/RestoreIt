using UnityEngine;

[CreateAssetMenu(fileName = "NuovoDatoOggetto", menuName = "ScriptableObjects/DatiOggettoAvanzato")]
public class DatiOggettoSO : ScriptableObject
{
    public string nomeOggetto;

    [Header("Comunicazione")]
    [Tooltip("Inserisci qui il Canale Eventi per notificare altri sistemi (es. UI)")]
    [SerializeField]  private VoidEventChannelSO raccogliOggetto;

    // La logica viene eseguita qui, mantenendo pulito l'oggetto fisico
    public virtual void EseguiInterazione()
    {
        Debug.Log($"Hai raccolto {nomeOggetto}!");
        if(raccogliOggetto != null) raccogliOggetto.RaiseEvent();
    }
}