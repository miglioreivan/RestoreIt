using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GestoreEsposizione : MonoBehaviour
{
    [Header("Piedistalli")]
    [Tooltip("Lista dei piedistalli da monitorare")]
    [SerializeField] private List<PedestalDropZone> piedistalli = new List<PedestalDropZone>();

    [Header("Eventi di Completamento")]
    [Tooltip("UnityEvent richiamato quando tutti i piedistalli sono occupati")]
    [SerializeField] private UnityEvent onEsposizioneCompletata;

    [Tooltip("Canali di eventi scriptable richiamati al completamento (opzionale)")]
    [SerializeField] private List<VoidEventChannelSO> onEsposizioneCompletataChannels = new List<VoidEventChannelSO>();

    private bool completato = false;

    private void OnEnable()
    {
        foreach (var pedestal in piedistalli)
        {
            if (pedestal != null)
            {
                pedestal.OnObjectPlaced += ControllaStatoEsposizione;
            }
        }
    }

    private void OnDisable()
    {
        foreach (var pedestal in piedistalli)
        {
            if (pedestal != null)
            {
                pedestal.OnObjectPlaced -= ControllaStatoEsposizione;
            }
        }
    }

    private void Start()
    {
        // Controllo iniziale se per caso sono già tutti pieni all'avvio
        ControllaStatoEsposizione(null);
    }

    private void ControllaStatoEsposizione(PedestalDropZone _)
    {
        if (completato) return;
        if (piedistalli == null || piedistalli.Count == 0) return;

        foreach (var pedestal in piedistalli)
        {
            if (pedestal == null || !pedestal.HasObject)
            {
                return; // Almeno un piedistallo non è occupato
            }
        }

        // Tutti i piedistalli sono occupati!
        completato = true;
        Debug.Log("[GestoreEsposizione] Tutti i piedistalli sono occupati. Esecuzione eventi di completamento.");
        
        onEsposizioneCompletata?.Invoke();

        foreach (var channel in onEsposizioneCompletataChannels)
        {
            if (channel != null)
            {
                channel.RaiseEvent();
            }
        }
    }
}
