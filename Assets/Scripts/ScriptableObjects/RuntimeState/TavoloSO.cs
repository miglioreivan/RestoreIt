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
        oggettoCorrente = null;
        vaschettaCorrente = null;
        vaschettaGameObject = null;
        faseCorrente = null;
        anforaAssemblata = null;
        if (collaTextureMosaico != null)
        {
            Destroy(collaTextureMosaico);
            collaTextureMosaico = null;
        }
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
        if (anforaAssemblata != null)
        {
            Destroy(anforaAssemblata);
            anforaAssemblata = null;
        }
        if (collaTextureMosaico != null)
        {
            Destroy(collaTextureMosaico);
            collaTextureMosaico = null;
        }
        oggettoCorrente = null;
        vaschettaCorrente = null;
        faseCorrente = null;
        OnTavoloSvuotato?.Invoke();
    }
}
