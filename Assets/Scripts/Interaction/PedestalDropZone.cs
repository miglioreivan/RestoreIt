using UnityEngine;
using UnityEngine.Events;

public class PedestalDropZone : MonoBehaviour, IInteractable
{
    public enum TipoOggettoAccettato
    {
        Qualsiasi,
        SoloAnfore,
        SoloMosaici
    }

    [Header("Dati")]
    [SerializeField] private InventarioManoSO manoGiocatore;
    [SerializeField] private Transform puntoRelease;

    [Header("Tipo di Oggetto Accettato")]
    [SerializeField] private TipoOggettoAccettato tipoAccettato = TipoOggettoAccettato.Qualsiasi;

    [Header("Eventi")]
    [SerializeField] private VoidEventChannelSO onReleaseEvent;
    [SerializeField] private UnityEventDatiOggetto eventsOnRelease;

    private GameObject lastReleasedItem;

    private void Awake()
    {
        if (manoGiocatore == null)
            Debug.LogError($"Componente manoGiocatore non assegnato nell'Inspector su {gameObject.name}.");
    }

    public bool canInteract()
    {
        if (manoGiocatore == null || manoGiocatore.currentGO == null)
            return false;

        GameObject go = manoGiocatore.currentGO;

        // Verifica che l'oggetto in mano sia stato restaurato
        bool isRestored = go.GetComponent<OggettoRestaurato>() != null ||
                          go.GetComponentInParent<OggettoRestaurato>() != null ||
                          go.GetComponentInChildren<OggettoRestaurato>() != null;
        if (!isRestored)
            return false;

        // Determina se l'oggetto è un'anfora o un mosaico tramite controlli incrociati su tipo, nome dati e nome del GameObject
        bool isAnfora = false;
        bool isMosaico = false;

        DatiOggettoSO oggetto = manoGiocatore.oggettoCorrente;
        if (oggetto != null)
        {
            if (oggetto is VaschettaSO) isAnfora = true;
            else if (oggetto is MosaicoSO) isMosaico = true;
            else if (oggetto.nomeOggetto.ToLower().Contains("anfora")) isAnfora = true;
            else if (oggetto.nomeOggetto.ToLower().Contains("mosaico")) isMosaico = true;
        }

        // Controllo di fallback sul nome del GameObject fisico
        if (!isAnfora && !isMosaico)
        {
            string goName = go.name.ToLower();
            if (goName.Contains("anfora")) isAnfora = true;
            else if (goName.Contains("mosaico")) isMosaico = true;
        }

        switch (tipoAccettato)
        {
            case TipoOggettoAccettato.SoloAnfore:
                return isAnfora;
            case TipoOggettoAccettato.SoloMosaici:
                return isMosaico;
            case TipoOggettoAccettato.Qualsiasi:
            default:
                return true;
        }
    }

    public string GetInteractionText()
    {
        if (manoGiocatore == null || manoGiocatore.currentGO == null)
            return "Non hai nessun oggetto da posizionare.";

        if (!canInteract())
        {
            if (tipoAccettato == TipoOggettoAccettato.SoloAnfore)
                return "Questo piedistallo accetta solo anfore restaurate.";
            if (tipoAccettato == TipoOggettoAccettato.SoloMosaici)
                return "Questo piedistallo accetta solo mosaici restaurati.";

            return "Questo oggetto non può essere posizionato sul piedistallo.";
        }

        return "[E] Posiziona sul piedistallo";
    }

    public void StartInteraction()
    {
        if (!canInteract()) return;

        lastReleasedItem = manoGiocatore.currentGO;
        GameObject go = manoGiocatore.currentGO;

        manoGiocatore.SvuotaMano();

        // Memorizzazione della scala globale per prevenire distorsioni dimensionali dopo il reparenting
        Vector3 targetWorldScale = go.transform.lossyScale;

        go.transform.SetParent(puntoRelease, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        // Compensazione della scala in base alle dimensioni globali del nuovo genitore
        if (puntoRelease != null)
        {
            Vector3 parentLossyScale = puntoRelease.lossyScale;
            go.transform.localScale = new Vector3(
                parentLossyScale.x != 0 ? targetWorldScale.x / parentLossyScale.x : targetWorldScale.x,
                parentLossyScale.y != 0 ? targetWorldScale.y / parentLossyScale.y : targetWorldScale.y,
                parentLossyScale.z != 0 ? targetWorldScale.z / parentLossyScale.z : targetWorldScale.z
            );
        }
        else
        {
            go.transform.localScale = targetWorldScale;
        }

        foreach (var col in go.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        // Disattivazione definitiva dell'interazione sul piedistallo dopo il posizionamento
        if (TryGetComponent(out Collider pedestalCollider))
            pedestalCollider.enabled = false;

        onReleaseEvent?.RaiseEvent();

        if (eventsOnRelease != null)
            eventsOnRelease.Invoke(lastReleasedItem);

        OnObjectPlaced?.Invoke(this);

        Debug.Log($"Oggetto {go.name} posizionato sul piedistallo {gameObject.name}.");
    }

    public bool HasObject => lastReleasedItem != null;
    public event System.Action<PedestalDropZone> OnObjectPlaced;
}
