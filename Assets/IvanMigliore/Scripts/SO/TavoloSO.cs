using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NuovoTavolo", menuName = "ScriptableObjects/Stato/Tavolo")]
public class TavoloSO : ScriptableObject
{
    public VaschettaSO vaschettaCorrente;
    public GameObject vaschettaGameObject;
    public FaseRestauroSO faseCorrente;

    public event Action<VaschettaSO> OnVaschettaPosata;
    public event Action<FaseRestauroSO> OnFaseCambiata;
    public event Action OnTavoloSvuotato;

    private void OnEnable()
    {
        vaschettaCorrente = null;
        vaschettaGameObject = null;
        faseCorrente = null;
    }

    public void PosaVaschetta(VaschettaSO vaschetta)
    {
        vaschettaCorrente = vaschetta;
        OnVaschettaPosata?.Invoke(vaschetta);
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
        vaschettaCorrente = null;
        faseCorrente = null;
        OnTavoloSvuotato?.Invoke();
    }
}
