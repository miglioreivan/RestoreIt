using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NuovoTavolo", menuName = "ScriptableObjects/Stato/Tavolo")]
public class TavoloSO : ScriptableObject
{
    public DatiOggettoSO oggettoCorrente;
    public VaschettaSO vaschettaCorrente;
    public GameObject vaschettaGameObject;
    public FaseRestauroSO faseCorrente;

    public event Action<DatiOggettoSO> OnOggettoPosato;
    public event Action<FaseRestauroSO> OnFaseCambiata;
    public event Action OnTavoloSvuotato;

    private void OnEnable()
    {
        oggettoCorrente = null;
        vaschettaCorrente = null;
        vaschettaGameObject = null;
        faseCorrente = null;
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
        if (vaschettaGameObject != null)
        {
            Destroy(vaschettaGameObject);
            vaschettaGameObject = null;
        }
        oggettoCorrente = null;
        vaschettaCorrente = null;
        faseCorrente = null;
        OnTavoloSvuotato?.Invoke();
    }
}
