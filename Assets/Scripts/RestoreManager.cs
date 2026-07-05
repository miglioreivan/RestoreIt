using UnityEngine;
using System.Collections;

public class RestoreManager : MonoBehaviour, IInteractable
{
    [Header("Close-Up 3D")]
    [SerializeField] private MonoBehaviour player;
    [SerializeField] private Transform target;
    [SerializeField] private float transitionDuration = 1.0f;
    [SerializeField] private GameObject canvas;

    [Header("Assegnazione Fasi")]
    [SerializeField] private GameObject fasePuliziaGO;
    [SerializeField] private GameObject faseAssemblaggioGO;

    [Header("Configurazione Restauro")]
    [SerializeField] private TavoloSO tavoloCorrente;
    [SerializeField] private Transform targetAssemblaggio;
    [SerializeField] private FaseRestauroSO faseAssemblaggio;

    private Camera playerCamera;
    private Vector3 startCameraPosition;
    private Quaternion startCameraRotation;
    private Transform startCameraParent;
    private bool isRestoring = false;
    private bool isRestorationComplete = false;

    private void Awake()
    {
        Debug.Log("[RestoreManager] Awake");
        playerCamera = Camera.main;
    }

    private void OnEnable()
    {
        Debug.Log("[RestoreManager] OnEnable");
        if (tavoloCorrente != null)
        {
            tavoloCorrente.OnVaschettaPosata += OnVaschettaPosata;
            tavoloCorrente.OnFaseCambiata += OnFaseCambiata;
            tavoloCorrente.OnTavoloSvuotato += OnTavoloSvuotato;
            Debug.Log("[RestoreManager] Sottoscritto agli eventi di tavoloCorrente.");
        }
        else
        {
            Debug.LogError("[RestoreManager] tavoloCorrente è NULL in OnEnable!");
        }
    }

    private void OnDisable()
    {
        Debug.Log("[RestoreManager] OnDisable");
        if (tavoloCorrente != null)
        {
            tavoloCorrente.OnVaschettaPosata -= OnVaschettaPosata;
            tavoloCorrente.OnFaseCambiata -= OnFaseCambiata;
            tavoloCorrente.OnTavoloSvuotato -= OnTavoloSvuotato;
        }
    }

    private void OnTavoloSvuotato()
    {
        isRestorationComplete = false;
        DisattivaColliderInterazione();
    }

    public void CompletaRestauro()
    {
        isRestorationComplete = true;
        DisattivaColliderInterazione();
    }

    private void DisattivaColliderInterazione()
    {
        if (TryGetComponent<Collider>(out var col))
        {
            col.enabled = false;
            Debug.Log("[RestoreManager] Disattivato collider tavolo.");
        }
        else if (transform.parent != null && transform.parent.TryGetComponent<Collider>(out var parentCol))
        {
            parentCol.enabled = false;
            Debug.Log("[RestoreManager] Disattivato collider parent tavolo.");
        }
    }

    private void AttivaColliderInterazione()
    {
        if (TryGetComponent<Collider>(out var col))
        {
            col.enabled = true;
            Debug.Log("[RestoreManager] Attivato collider tavolo.");
        }
        else if (transform.parent != null && transform.parent.TryGetComponent<Collider>(out var parentCol))
        {
            parentCol.enabled = true;
            Debug.Log("[RestoreManager] Attivato collider parent tavolo.");
        }
    }

    private void Start()
    {
        Debug.Log("[RestoreManager] Start");
        
        // Stampa la gerarchia completa a partire dal parent per capire esattamente cosa contiene il tavolo
        Transform searchRoot = transform.parent != null ? transform.parent : transform;
        Debug.Log($"=== DIAGNOSTICA GERARCHIA TAVOLO A PARTIRE DA '{searchRoot.name}' ===");
        StampaGerarchiaEComponenti(searchRoot, 1);
        Debug.Log("=================================================");

        if (TryGetComponent<Collider>(out var col))
        {
            bool hasVaschetta = (tavoloCorrente != null && tavoloCorrente.vaschettaCorrente != null);
            col.enabled = hasVaschetta;
            Debug.Log($"[RestoreManager] Collider inizializzato. Enabled: {col.enabled} (ha vaschetta: {hasVaschetta})");
        }
        else
        {
            if (transform.parent != null && transform.parent.TryGetComponent<Collider>(out var parentCol))
            {
                bool hasVaschetta = (tavoloCorrente != null && tavoloCorrente.vaschettaCorrente != null);
                parentCol.enabled = hasVaschetta;
                Debug.Log($"[RestoreManager] Collider del parent '{transform.parent.name}' inizializzato. Enabled: {parentCol.enabled}");
            }
            else
            {
                Debug.LogWarning("[RestoreManager] Nessun Collider trovato né su RestoreManager né su TavoloLavoro!");
            }
        }

        // Disattiva le fasi all'avvio del gioco
        ImpostaStatoFasePulizia(false);
        ImpostaStatoFaseAssemblaggio(false);
    }

    private void StampaGerarchiaEComponenti(Transform t, int livello)
    {
        string indent = new string('-', livello * 2) + " ";
        foreach (Transform child in t)
        {
            Component[] components = child.GetComponents<Component>();
            string compList = "";
            foreach (var c in components)
            {
                if (c == null) compList += "[Component mancante/null] ";
                else compList += $"[{c.GetType().Name}] ";
            }
            Debug.Log($"{indent} Figlio: '{child.name}' (Attivo: {child.gameObject.activeSelf}) -> Componenti: {compList}");
            
            StampaGerarchiaEComponenti(child, livello + 1);
        }
    }

    private void OnVaschettaPosata(VaschettaSO vaschetta)
    {
        Debug.Log($"[RestoreManager] OnVaschettaPosata - Vaschetta posata: {(vaschetta != null ? vaschetta.name : "NULL")}");
        
        if (vaschetta != null)
        {
            isRestorationComplete = false;
            AttivaColliderInterazione();
        }
        else
        {
            DisattivaColliderInterazione();
        }
    }

    private void OnFaseCambiata(FaseRestauroSO fase)
    {
        Debug.Log($"[RestoreManager] OnFaseCambiata - Nuova fase: {(fase != null ? fase.name : "NULL")}");
        if (fase == faseAssemblaggio && targetAssemblaggio != null)
        {
            ImpostaStatoFasePulizia(false);
            ImpostaStatoFaseAssemblaggio(true);

            StartCoroutine(TransitionCamera(targetAssemblaggio.position, targetAssemblaggio.rotation, null, false, false));
        }
    }

    public bool canInteract()
    {
        bool can = !isRestoring;
        Debug.Log($"[RestoreManager] canInteract chiamato: {can} (isRestoring: {isRestoring})");
        return can;
    }

    public string GetInteractionText()
    {
        return "[E] Usa postazione di restauro";
    }

    public void StartInteraction()
    {
        Debug.Log("[RestoreManager] StartInteraction richiesto.");
        if (!canInteract()) return;

        isRestoring = true;

        if (player != null)
        {
            player.enabled = false;
            Debug.Log("[RestoreManager] Disattivato script movimento player.");
        }
        
        DisattivaColliderInterazione();

        startCameraParent = playerCamera.transform.parent;
        startCameraPosition = playerCamera.transform.position;
        startCameraRotation = playerCamera.transform.rotation;

        playerCamera.transform.SetParent(null, true);

        canvas.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[RestoreManager] Attivo fase pulizia e disattivo fase assemblaggio.");
        ImpostaStatoFasePulizia(true);
        ImpostaStatoFaseAssemblaggio(false);

        Debug.Log("[RestoreManager] Avvio coroutine transizione camera.");
        StartCoroutine(TransitionCamera(target.position, target.rotation, null, true, false));
    }

    public void StopInteraction()
    {
        Debug.Log("[RestoreManager] StopInteraction richiesto.");
        canvas.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ImpostaStatoFasePulizia(false);
        ImpostaStatoFaseAssemblaggio(false);

        StartCoroutine(TransitionCamera(startCameraPosition, startCameraRotation, startCameraParent, false, true));
    }

    private IEnumerator TransitionCamera(Vector3 targetPos, Quaternion targetRot, Transform parent, bool startCleaning, bool restorePlayer)
    {
        Debug.Log($"[RestoreManager] TransitionCamera avviata. startCleaning: {startCleaning}, restorePlayer: {restorePlayer}");
        float elapsedTime = 0f;
        Vector3 startPos = playerCamera.transform.position;
        Quaternion startRot = playerCamera.transform.rotation;

        while (elapsedTime < transitionDuration)
        {
            playerCamera.transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / transitionDuration);
            playerCamera.transform.rotation = Quaternion.Lerp(startRot, targetRot, elapsedTime / transitionDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        playerCamera.transform.SetParent(parent, true);
        playerCamera.transform.position = targetPos;
        playerCamera.transform.rotation = targetRot;
        Debug.Log("[RestoreManager] TransitionCamera completata.");

        // Se la transizione era per la fase di assemblaggio, notifica il GestoreAssemblaggio per abilitare il drag & drop
        if (!startCleaning && !restorePlayer)
        {
            GestoreAssemblaggio gestore = null;
            if (faseAssemblaggioGO != null)
            {
                gestore = faseAssemblaggioGO.GetComponentInChildren<GestoreAssemblaggio>(true);
            }
            if (gestore == null)
            {
                Transform searchRoot = transform.parent != null ? transform.parent : transform;
                gestore = searchRoot.GetComponentInChildren<GestoreAssemblaggio>(true);
            }
            if (gestore != null)
            {
                gestore.CameraTransitionCompleted();
            }
            else
            {
                Debug.LogWarning("[RestoreManager] Transizione camera completata ma GestoreAssemblaggio non trovato!");
            }
        }

        if (startCleaning)
        {
            StrumentoPulizia sp = null;
            if (fasePuliziaGO != null)
            {
                sp = fasePuliziaGO.GetComponentInChildren<StrumentoPulizia>(true);
            }
            
            if (sp == null)
            {
                Transform searchRoot = transform.parent != null ? transform.parent : transform;
                sp = searchRoot.GetComponentInChildren<StrumentoPulizia>(true);
            }

            if (sp != null)
            {
                Debug.Log($"[RestoreManager] StrumentoPulizia trovato in '{sp.gameObject.name}'. Avvio il minigioco.");
                sp.CountVisiblePixel();
                sp.SetMouseCursor();
                sp.IniziaMinigame();
            }
            else
            {
                Debug.LogError("[RestoreManager] ERRORE: StrumentoPulizia NON trovato!");
            }
        }

        if (restorePlayer)
        {
            if (player != null) player.enabled = true;
            
            if (!isRestorationComplete)
            {
                AttivaColliderInterazione();
            }
            else
            {
                Debug.Log("[RestoreManager] Restauro completato. Non riattivo il collider delle interazioni di restauro.");
            }

            isRestoring = false;
            Debug.Log("[RestoreManager] Interazione terminata, ripristinato player.");
        }
    }

    private void ImpostaStatoFasePulizia(bool attiva)
    {
        if (fasePuliziaGO != null)
        {
            Debug.Log($"[RestoreManager] ImpostaStatoFasePulizia({attiva}) su GameObject da Inspector: '{fasePuliziaGO.name}'");
            fasePuliziaGO.SetActive(attiva);
            return;
        }

        Transform searchRoot = transform.parent != null ? transform.parent : transform;
        var sp = searchRoot.GetComponentInChildren<StrumentoPulizia>(true);
        if (sp != null)
        {
            if (sp.gameObject != gameObject)
            {
                Debug.Log($"[RestoreManager] ImpostaStatoFasePulizia({attiva}) su GameObject '{sp.gameObject.name}' (Fallback)");
                sp.gameObject.SetActive(attiva);
            }
            else
            {
                Debug.Log($"[RestoreManager] ImpostaStatoFasePulizia({attiva}) su script '{sp.name}' (Fallback)");
                sp.enabled = attiva;
            }
        }
        else
        {
            Debug.LogWarning($"[RestoreManager] ImpostaStatoFasePulizia({attiva}) - StrumentoPulizia non trovato sotto '{searchRoot.name}'!");
        }
    }

    private void ImpostaStatoFaseAssemblaggio(bool attiva)
    {
        if (faseAssemblaggioGO != null)
        {
            Debug.Log($"[RestoreManager] ImpostaStatoFaseAssemblaggio({attiva}) su GameObject da Inspector: '{faseAssemblaggioGO.name}'");
            faseAssemblaggioGO.SetActive(attiva);
            return;
        }

        Transform searchRoot = transform.parent != null ? transform.parent : transform;
        var ga = searchRoot.GetComponentInChildren<GestoreAssemblaggio>(true);
        if (ga != null)
        {
            if (ga.gameObject != gameObject)
            {
                Debug.Log($"[RestoreManager] ImpostaStatoFaseAssemblaggio({attiva}) su GameObject '{ga.gameObject.name}' (Fallback)");
                ga.gameObject.SetActive(attiva);
            }
            else
            {
                Debug.Log($"[RestoreManager] ImpostaStatoFaseAssemblaggio({attiva}) su script '{ga.name}' (Fallback)");
                ga.enabled = attiva;
            }
        }
        else
        {
            Debug.LogWarning($"[RestoreManager] ImpostaStatoFaseAssemblaggio({attiva}) - GestoreAssemblaggio non trovato sotto '{searchRoot.name}'!");
        }
    }
}