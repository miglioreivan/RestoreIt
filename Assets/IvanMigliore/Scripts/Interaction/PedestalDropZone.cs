using UnityEngine;
using UnityEngine.Events;

public class PedestalDropZone : MonoBehaviour, IInteractable
{
    [Header("Dati")]
    [SerializeField] private InventarioManoSO manoGiocatore;
    [SerializeField] private Transform puntoRelease;

    [Header("Eventi")]
    [SerializeField] private VoidEventChannelSO onReleaseEvent;
    [SerializeField] private UnityEventDatiOggetto eventsOnRelease;

    private GameObject lastReleasedItem;

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
                Debug.LogWarning($"[PedestalDropZone] '{gameObject.name}': manoGiocatore è null e FirstPersonController non trovato nella scena.");
            }
        }
    }

    public bool canInteract()
    {
        if (manoGiocatore == null || manoGiocatore.currentGO == null)
            return false;

        GameObject go = manoGiocatore.currentGO;

        // Se ha il componente OggettoRestaurato, è sicuramente un oggetto restaurato
        if (go.GetComponent<OggettoRestaurato>() != null)
            return true;

        // Se è una vaschetta, NON è un oggetto restaurato e non può essere posizionato sul piedistallo
        bool isVaschetta = go.GetComponent<ConfigurazioneVaschetta>() != null || 
                           go.GetComponentInChildren<ConfigurazioneVaschetta>() != null ||
                           (manoGiocatore.oggettoCorrente is VaschettaSO);

        if (isVaschetta)
            return false;

        // Safe fallback basato sul nome, escludendo le vaschette
        bool hasAnforaName = go.name.ToLower().Contains("anfora") ||
                            (manoGiocatore.oggettoCorrente != null && manoGiocatore.oggettoCorrente.nomeOggetto.ToLower().Contains("anfora"));

        return hasAnforaName;
    }

    public string GetInteractionText()
    {
        if (manoGiocatore == null || manoGiocatore.currentGO == null)
            return "Non hai nessun oggetto da posizionare.";

        if (!canInteract())
            return "Questo oggetto non può essere posizionato sul piedistallo (solo oggetti restaurati).";

        return "[E] Posiziona sul piedistallo";
    }

    public void StartInteraction()
    {
        if (!canInteract()) return;

        lastReleasedItem = manoGiocatore.currentGO;
        GameObject go = manoGiocatore.currentGO;

        // Rilascia l'oggetto dalla mano
        manoGiocatore.oggettoCorrente = null;
        manoGiocatore.currentGO = null;

        // Posiziona l'oggetto sul piedistallo
        go.transform.SetParent(puntoRelease, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        // Disabilita il collider dell'oggetto posizionato per bloccarlo
        if (go.TryGetComponent(out Collider objCollider))
            objCollider.enabled = false;

        // Disabilita il collider del piedistallo stesso in modo che non si possa più interagire ("si blocca e basta")
        if (TryGetComponent(out Collider pedestalCollider))
            pedestalCollider.enabled = false;

        onReleaseEvent?.RaiseEvent();

        if (eventsOnRelease != null)
            eventsOnRelease.Invoke(lastReleasedItem);

        OnObjectPlaced?.Invoke(this);

        Debug.Log($"[PedestalDropZone] Oggetto '{go.name}' posizionato sul piedistallo e bloccato.");
    }

    public bool HasObject => lastReleasedItem != null;
    public event System.Action<PedestalDropZone> OnObjectPlaced;
}
