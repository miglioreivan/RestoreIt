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
            Debug.LogError($"[DropZone_Interaction] '{gameObject.name}': manoGiocatore non assegnato nell'Inspector.");
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
            Debug.Log($"[DropZone_Interaction] '{gameObject.name}': Tavolo svuotato, riabilitato collider.");
        }
    }

    public bool canInteract()
    {
        if (manoGiocatore == null || manoGiocatore.currentGO == null)
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
            Debug.LogWarning($"[DropZone_Interaction] '{gameObject.name}': onReleaseEvent non assegnato nell'Inspector.");

        lastReleasedItem = manoGiocatore.currentGO;
        GameObject go = manoGiocatore.currentGO;

        if (tavoloCorrente != null && manoGiocatore.oggettoCorrente != null)
        {
            tavoloCorrente.vaschettaGameObject = go;
            tavoloCorrente.PosaOggetto(manoGiocatore.oggettoCorrente);
        }

        manoGiocatore.oggettoCorrente = null;
        manoGiocatore.currentGO = null;

        // Salva la scala globale originale prima del cambio di parent per evitare distorsioni
        Vector3 targetWorldScale = go.transform.lossyScale;

        go.transform.SetParent(puntoRelease, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        // Ricalcola la scala locale in base alla scala globale del nuovo parent
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

        if (go.TryGetComponent(out Collider objCollider))
            objCollider.enabled = false;

        if (TryGetComponent(out Collider dropZoneCollider))
            dropZoneCollider.enabled = false;

        onReleaseEvent?.RaiseEvent();

        if (eventsOnRelease != null)
            eventsOnRelease.Invoke(lastReleasedItem);
    }
}