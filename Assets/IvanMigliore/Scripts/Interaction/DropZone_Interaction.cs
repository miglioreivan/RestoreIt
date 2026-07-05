using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class UnityEventDatiOggetto : UnityEvent<GameObject> { }

public class DropZone_Interaction : MonoBehaviour, IInteractable
{
    [Header("Dati")]
    [SerializeField] private InventarioManoSO manoGiocatore;
    [SerializeField] private Transform puntoRelease;
    [SerializeField] private TavoloSO tavoloCorrente;

    private void Awake()
    {
        if (manoGiocatore == null)
        {
            FirstPersonController controller = FindFirstObjectByType<FirstPersonController>();
            if (controller != null)
            {
                manoGiocatore = controller.Inventario;
            }
            else
            {
                Debug.LogWarning($"[DropZone_Interaction] '{gameObject.name}': manoGiocatore è null e FirstPersonController non trovato nella scena.");
            }
        }
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
        return manoGiocatore.currentGO != null && (tavoloCorrente == null || tavoloCorrente.vaschettaCorrente == null);
    }

    public string GetInteractionText()
    {
        if (manoGiocatore.currentGO == null)
            return "Non hai un oggetto da rilasciare.";
        
        if (tavoloCorrente != null && tavoloCorrente.vaschettaCorrente != null)
            return "Il tavolo deve essere vuoto per posare una nuova vaschetta.";

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

        if (tavoloCorrente != null && manoGiocatore.oggettoCorrente is VaschettaSO vaschetta)
        {
            tavoloCorrente.vaschettaGameObject = go;
            tavoloCorrente.PosaVaschetta(vaschetta);
        }

        manoGiocatore.oggettoCorrente = null;
        manoGiocatore.currentGO = null;

        go.transform.SetParent(puntoRelease, false);
        go.transform.localPosition = Vector3.zero;

        if (go.TryGetComponent(out Collider objCollider))
            objCollider.enabled = false;

        if (TryGetComponent(out Collider dropZoneCollider))
            dropZoneCollider.enabled = false;

        onReleaseEvent?.RaiseEvent();

        if (eventsOnRelease != null)
            eventsOnRelease.Invoke(lastReleasedItem);
    }
}