using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class UnityEventDatiOggetto : UnityEvent<GameObject> { }

public class DropZone_Interaction : MonoBehaviour, IInteractable
{
    [Header("Dati")]
    [SerializeField] private InventarioManoSO manoGiocatore;
    [SerializeField] private Transform puntoRelease;
    
    [Header("Eventi")]
    [SerializeField] private VoidEventChannelSO onReleaseEvent;
    [SerializeField] private UnityEventDatiOggetto eventsOnRelease;
    
    private GameObject lastReleasedItem;

    public bool canInteract()
    {
        return manoGiocatore.currentGO != null;
    }

    public string GetInteractionText()
    {
        if (canInteract()) 
            return "[E] Rilascia";
        else 
            return "Non hai un oggetto da rilasciare.";
    }
    
    public void StartInteraction()
    {
        if (manoGiocatore.oggettoCorrente == null) return;
        if (onReleaseEvent == null) return;
        
        lastReleasedItem = manoGiocatore.currentGO;
        GameObject go = manoGiocatore.currentGO;
        manoGiocatore.oggettoCorrente = null;
        manoGiocatore.currentGO = null;
        
        go.transform.SetParent(puntoRelease, false);
        go.transform.localPosition = Vector3.zero;
        
        onReleaseEvent.RaiseEvent();
        
        if(eventsOnRelease != null)
            eventsOnRelease.Invoke(lastReleasedItem);
        enabled = false;
    }
}