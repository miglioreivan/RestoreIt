using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NuovoTavolo", menuName = "ScriptableObjects/Stato/Tavolo")]
public class TavoloSO : ScriptableObject
{
    public DatiOggettoSO oggettoCorrente;
    public VaschettaSO vaschettaCorrente;
    public GameObject vaschettaGameObject;
    public FaseRestauroSO faseCorrente;
    [HideInInspector] public GameObject anforaAssemblata;
    [HideInInspector] public Texture2D collaTextureMosaico;

    public event Action<DatiOggettoSO> OnOggettoPosato;
    public event Action<FaseRestauroSO> OnFaseCambiata;
    public event Action OnTavoloSvuotato;

    private void OnEnable()
    {
        // Reset dei dati runtime ad ogni avvio di Play Mode.
        // La distruzione di eventuali texture precedenti è delegata agli ascoltatori di OnTavoloSvuotato.
        oggettoCorrente = null;
        vaschettaCorrente = null;
        vaschettaGameObject = null;
        faseCorrente = null;
        anforaAssemblata = null;
        collaTextureMosaico = null;
    }

    public void PosaOggetto(DatiOggettoSO oggetto)
    {
        oggettoCorrente = oggetto;
        vaschettaCorrente = oggetto as VaschettaSO;
        OnOggettoPosato?.Invoke(oggetto);
    }

    public void AvanzaFase(FaseRestauroSO prossima)
    {
        faseCorrente = prossima;
        OnFaseCambiata?.Invoke(prossima);
    }

    public void SvuotaTavolo()
    {
        // L'evento viene emesso PRIMA di nullare i riferimenti, in modo che
        // gli ascoltatori (es. RestoreManager) possano leggere i valori correnti
        // e chiamare Destroy() sugli oggetti di scena che devono essere rimossi.
        OnTavoloSvuotato?.Invoke();

        vaschettaGameObject = null;
        anforaAssemblata = null;
        collaTextureMosaico = null;
        oggettoCorrente = null;
        vaschettaCorrente = null;
        faseCorrente = null;
    }
}
