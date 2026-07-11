using System;
using UnityEngine;

/// <summary>
/// ScriptableObject che modella e persiste lo stato runtime di un singolo banco da lavoro (tavolo).
/// Contiene i riferimenti all'oggetto in restauro, la vaschetta attiva e la fase corrente.
/// </summary>
[CreateAssetMenu(fileName = "NuovoTavolo", menuName = "ScriptableObjects/Stato/Tavolo")]
public class TavoloSO : ScriptableObject
{
    /// <summary>
    /// I dati dell'oggetto attualmente posato sul tavolo.
    /// </summary>
    [Tooltip("L'oggetto correntemente posato sul tavolo.")]
    public DatiOggettoSO oggettoCorrente;

    /// <summary>
    /// I dati della vaschetta attiva se l'oggetto è una vaschetta/mosaico.
    /// </summary>
    [Tooltip("La vaschetta corrente se l'oggetto supporta questa modalità.")]
    public VaschettaSO vaschettaCorrente;

    /// <summary>
    /// Il GameObject della vaschetta istanziato fisicamente nella scena.
    /// </summary>
    [Tooltip("Il GameObject istanziato della vaschetta nella scena.")]
    public GameObject vaschettaGameObject;

    /// <summary>
    /// La fase di restauro correntemente attiva per l'oggetto sul tavolo.
    /// </summary>
    [Tooltip("La fase di restauro corrente.")]
    public FaseRestauroSO faseCorrente;

    /// <summary>
    /// L'anfora parzialmente o completamente assemblata a runtime (persistita durante la transizione).
    /// </summary>
    [HideInInspector] public GameObject anforaAssemblata;

    /// <summary>
    /// La texture generata a runtime per tracciare lo stato della colla del mosaico.
    /// </summary>
    [HideInInspector] public Texture2D collaTextureMosaico;

    /// <summary>
    /// Evento sollevato quando un oggetto viene posato sul tavolo di lavoro.
    /// </summary>
    public event Action<DatiOggettoSO> OnOggettoPosato;

    /// <summary>
    /// Evento sollevato quando la fase di restauro del tavolo viene modificata o avanzata.
    /// </summary>
    public event Action<FaseRestauroSO> OnFaseCambiata;

    /// <summary>
    /// Evento sollevato prima che i riferimenti del tavolo vengano azzerati (svuotamento).
    /// </summary>
    public event Action OnTavoloSvuotato;

    private void OnEnable()
    {
        // Reset dei dati runtime ad ogni avvio di Play Mode per evitare persistenza indesiderata.
        oggettoCorrente = null;
        vaschettaCorrente = null;
        vaschettaGameObject = null;
        faseCorrente = null;
        anforaAssemblata = null;
        collaTextureMosaico = null;
    }

    /// <summary>
    /// Posiziona un oggetto sul tavolo da lavoro e notifica tutti gli ascoltatori.
    /// </summary>
    /// <param name="oggetto">I dati dell'oggetto da posare sul tavolo.</param>
    public void PosaOggetto(DatiOggettoSO oggetto)
    {
        oggettoCorrente = oggetto;
        vaschettaCorrente = oggetto as VaschettaSO;
        OnOggettoPosato?.Invoke(oggetto);
    }

    /// <summary>
    /// Avanza la fase di restauro del tavolo alla fase successiva e notifica gli ascoltatori.
    /// </summary>
    /// <param name="prossima">La fase successiva da attivare.</param>
    public void AvanzaFase(FaseRestauroSO prossima)
    {
        faseCorrente = prossima;
        OnFaseCambiata?.Invoke(prossima);
    }

    /// <summary>
    /// Svuota il tavolo da lavoro. Emette l'evento prima di azzerare i riferimenti
    /// per consentire ai manager di scena di pulire i GameObject correlati.
    /// </summary>
    public void SvuotaTavolo()
    {
        OnTavoloSvuotato?.Invoke();

        vaschettaGameObject = null;
        anforaAssemblata = null;
        collaTextureMosaico = null;
        oggettoCorrente = null;
        vaschettaCorrente = null;
        faseCorrente = null;
    }
}
