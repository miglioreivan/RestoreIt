using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class UnityEventDatiOggetto : UnityEvent<GameObject> { }

public class DropZone_Interaction : MonoBehaviour, IInteractable
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
    [SerializeField] private TavoloSO tavoloCorrente;
    [SerializeField] private TipoOggettoAccettato tipoAccettato = TipoOggettoAccettato.Qualsiasi;

    private void Awake()
    {
        if (manoGiocatore == null)
            Debug.LogError($"Componente manoGiocatore non assegnato nell'Inspector su {gameObject.name}.");
    }

    [Header("Eventi")]
    [SerializeField] private VoidEventChannelSO onReleaseEvent;
    [SerializeField] private UnityEventDatiOggetto eventsOnRelease;

    private GameObject lastReleasedItem;

    private void OnEnable()
    {
        if (tavoloCorrente != null)
        {
            tavoloCorrente.OnTavoloSvuotato += OnTavoloSvuotato;
        }
    }

    private void OnDisable()
    {
        if (tavoloCorrente != null)
        {
            tavoloCorrente.OnTavoloSvuotato -= OnTavoloSvuotato;
        }
    }

    private void OnTavoloSvuotato()
    {
        if (TryGetComponent(out Collider dropZoneCollider))
        {
            dropZoneCollider.enabled = true;
            Debug.Log($"Tavolo svuotato, riabilitato il collider per {gameObject.name}.");
        }
    }

    public bool canInteract()
    {
        if (manoGiocatore == null || manoGiocatore.currentGO == null)
            return false;

        bool isRestored = manoGiocatore.currentGO.GetComponent<OggettoRestaurato>() != null ||
                          manoGiocatore.currentGO.GetComponentInParent<OggettoRestaurato>() != null ||
                          manoGiocatore.currentGO.GetComponentInChildren<OggettoRestaurato>() != null;
        if (isRestored)
            return false;

        if (tavoloCorrente != null && tavoloCorrente.oggettoCorrente != null)
            return false;

        DatiOggettoSO oggetto = manoGiocatore.oggettoCorrente;
        if (oggetto == null)
            return false;

        switch (tipoAccettato)
        {
            case TipoOggettoAccettato.SoloAnfore:
                return oggetto is VaschettaSO;
            case TipoOggettoAccettato.SoloMosaici:
                return oggetto is MosaicoSO;
            case TipoOggettoAccettato.Qualsiasi:
            default:
                return true;
        }
    }

    public string GetInteractionText()
    {
        if (manoGiocatore == null || manoGiocatore.currentGO == null)
            return "Non hai un oggetto da rilasciare.";

        bool isRestored = manoGiocatore.currentGO.GetComponent<OggettoRestaurato>() != null ||
                          manoGiocatore.currentGO.GetComponentInParent<OggettoRestaurato>() != null ||
                          manoGiocatore.currentGO.GetComponentInChildren<OggettoRestaurato>() != null;
        if (isRestored)
            return "Non puoi rimettere sul tavolo un oggetto già restaurato!";
        
        if (tavoloCorrente != null && tavoloCorrente.oggettoCorrente != null)
            return "Il tavolo deve essere vuoto per posare un nuovo oggetto.";

        DatiOggettoSO oggetto = manoGiocatore.oggettoCorrente;
        if (oggetto != null)
        {
            if (tipoAccettato == TipoOggettoAccettato.SoloAnfore && !(oggetto is VaschettaSO))
                return "Questo tavolo accetta solo anfore.";
            
            if (tipoAccettato == TipoOggettoAccettato.SoloMosaici && !(oggetto is MosaicoSO))
                return "Questo tavolo accetta solo mosaici.";

            return $"[E] Rilascia {oggetto.nomeOggetto}";
        }

        return "[E] Rilascia";
    }

    public void StartInteraction()
    {
        if (manoGiocatore.currentGO == null) return;
        if (!canInteract()) return;

        if (onReleaseEvent == null)
            Debug.LogWarning($"Evento onReleaseEvent non assegnato nell'Inspector su {gameObject.name}.");

        lastReleasedItem = manoGiocatore.currentGO;
        GameObject go = manoGiocatore.currentGO;

        if (tavoloCorrente != null && manoGiocatore.oggettoCorrente != null)
        {
            tavoloCorrente.vaschettaGameObject = go;
            tavoloCorrente.PosaOggetto(manoGiocatore.oggettoCorrente);
        }

        manoGiocatore.SvuotaMano();

        // Memorizzazione della scala globale per evitare distorsioni dimensionali dopo il reparenting
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

        if (TryGetComponent(out Collider dropZoneCollider))
            dropZoneCollider.enabled = false;

        onReleaseEvent?.RaiseEvent();

        if (eventsOnRelease != null)
            eventsOnRelease.Invoke(lastReleasedItem);
    }
}