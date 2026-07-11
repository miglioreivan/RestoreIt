using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Il manager centrale responsabile del flusso di restauro di un reperto sul banco da lavoro.
/// Gestisce la macchina a stati delle fasi, i close-up della telecamera e lo sblocco dell'interazione del giocatore.
/// </summary>
public class RestoreManager : MonoBehaviour, IInteractable
{
    /// <summary>
    /// Struttura di mappatura che associa una fase di restauro definita tramite ScriptableObject
    /// al relativo GameObject di gameplay in scena ed all'inquadratura target della telecamera.
    /// </summary>
    [System.Serializable]
    public struct FaseMapping
    {
        /// <summary> Lo ScriptableObject di definizione della fase. </summary>
        public FaseRestauroSO faseSO;
        /// <summary> Il GameObject in scena contenente lo script e gli elementi grafici della fase. </summary>
        public GameObject     faseGameObject;
        /// <summary> La posizione ed orientamento target della camera durante questa fase. </summary>
        public Transform      targetCamera;
    }

    [Header("Impostazioni Generali")]
    [SerializeField] private FirstPersonController player;
    [SerializeField] private float         transitionDuration = 1.0f;
    [SerializeField] private GameObject    canvas;
    [SerializeField] private TMPro.TextMeshProUGUI istruzioniText;
    [SerializeField] private GameObject    testoDaDisattivare;
    [SerializeField] private UnityEngine.UI.Slider progressSlider;

    [Header("Stato Tavolo")]
    [SerializeField] private TavoloSO tavoloCorrente;
    public TavoloSO TavoloCorrente => tavoloCorrente;

    [Header("Fasi del Restauro")]
    [SerializeField] private List<FaseMapping> fasiMappate = new List<FaseMapping>();

    private Camera         playerCamera;
    private Vector3        startCameraPosition;
    private Quaternion     startCameraRotation;
    private Transform      startCameraParent;
    private bool           isRestoring           = false;
    private bool           isRestorationComplete = false;
    private FaseMapping    faseMappingCorrente;
    private Coroutine      activeTransitionCoroutine;
    private SuggerimentoMano suggerimentoMano;
    private FaseRestauroSO ultimaFaseAttiva;

    // Riferimento al gestore di fase corrente che implementa IRestorationPhase
    private IRestorationPhase faseCorrentePhaseable;

    // Unity Lifecycle

    private void Awake()
    {
        playerCamera = Camera.main;
        if (player == null)
        {
            player = FindFirstObjectByType<FirstPersonController>();
        }
    }

    private void Start()
    {
        if (!ValidaConfigurazione()) return;

        InitCollider();
        DisattivaFasi();

        suggerimentoMano = FindFirstObjectByType<SuggerimentoMano>();

        Transform searchRoot = transform.parent != null ? transform.parent : transform;
        RestoreLogger.Log($"Gerarchia di {searchRoot.name}:");
        StampaGerarchiaEComponenti(searchRoot, 1);
    }

    private void OnEnable()
    {
        if (tavoloCorrente == null) return;
        tavoloCorrente.OnOggettoPosato  += OnOggettoPosato;
        tavoloCorrente.OnFaseCambiata   += OnFaseCambiata;
        tavoloCorrente.OnTavoloSvuotato += OnTavoloSvuotato;
        RestoreLogger.Log("Sottoscritto agli eventi del tavolo di lavoro.");
    }

    private void OnDisable()
    {
        if (tavoloCorrente == null) return;
        tavoloCorrente.OnOggettoPosato  -= OnOggettoPosato;
        tavoloCorrente.OnFaseCambiata   -= OnFaseCambiata;
        tavoloCorrente.OnTavoloSvuotato -= OnTavoloSvuotato;
    }

    private void Update()
    {
        if (isRestoring && faseCorrentePhaseable != null && ultimaFaseAttiva != null)
        {
            float prog = faseCorrentePhaseable.Progression;
            if (prog >= 0f)
            {
                // Aggiorna lo Slider (se assegnato)
                if (progressSlider != null)
                {
                    progressSlider.gameObject.SetActive(true);
                    progressSlider.value = prog;
                }

                if (istruzioniText != null)
                {
                    istruzioniText.text = $"{ultimaFaseAttiva.DescrizioneFase} ({Mathf.RoundToInt(prog * 100f)}%)";
                }
            }
            else
            {
                // Nascondi lo Slider se la fase corrente non supporta la percentuale
                if (progressSlider != null)
                {
                    progressSlider.gameObject.SetActive(false);
                }

                if (istruzioniText != null)
                {
                    istruzioniText.text = ultimaFaseAttiva.DescrizioneFase;
                }
            }
        }
    }

    // Validazione

    private bool ValidaConfigurazione()
    {
        if (tavoloCorrente == null)
        {
            Debug.LogError($"Componente tavoloCorrente non assegnato nell'Inspector su {gameObject.name}.");
            enabled = false;
            return false;
        }
        if (canvas == null)
        {
            Debug.LogError($"Componente canvas non assegnato nell'Inspector su {gameObject.name}.");
            enabled = false;
            return false;
        }
        if (player == null)
        {
            Debug.LogError($"Componente player non assegnato nell'Inspector su {gameObject.name}.");
            enabled = false;
            return false;
        }
        if (playerCamera == null)
        {
            Debug.LogError($"Telecamera principale non trovata nella scena per {gameObject.name}.");
            enabled = false;
            return false;
        }
        if (fasiMappate == null || fasiMappate.Count == 0)
        {
            Debug.LogError($"Nessuna mappatura di fase configurata su {gameObject.name}.");
            enabled = false;
            return false;
        }
        for (int i = 0; i < fasiMappate.Count; i++)
        {
            FaseMapping m = fasiMappate[i];
            if (m.faseSO == null)
            {
                Debug.LogError($"Mappatura di fase alla posizione {i} non assegnata.");
                enabled = false;
                return false;
            }
            if (m.faseGameObject == null)
            {
                Debug.LogError($"Oggetto di fase non assegnato per {m.faseSO.name}.");
                enabled = false;
                return false;
            }
            if (m.targetCamera == null)
            {
                Debug.LogError($"Telecamera di destinazione non assegnata per {m.faseSO.name}.");
                enabled = false;
                return false;
            }
        }
        if (istruzioniText == null)
        {
            Debug.LogWarning($"Campo di testo per le istruzioni non assegnato nell'Inspector su {gameObject.name}.");
        }
        return true;
    }

    // Gestione Stato Tavolo

    private void OnOggettoPosato(DatiOggettoSO oggetto)
    {
        RestoreLogger.Log($"Oggetto posato sul tavolo: {(oggetto != null ? oggetto.name : "nessuno")}.");
        ImpostaCollider(oggetto != null);
        
        if (oggetto != null)
        {
            isRestorationComplete = false;

            // Rimozione di eventuali residui di garze o aerolam istanziati in sessioni precedenti
            if (tavoloCorrente != null && tavoloCorrente.vaschettaGameObject != null)
            {
                GameObject vaschettaGO = tavoloCorrente.vaschettaGameObject;
                List<GameObject> cloniDaDistruggere = new List<GameObject>();

                foreach (Transform child in vaschettaGO.transform)
                {
                    if (child.name.Contains("(Clone)") || child.name.Contains("Garza"))
                    {
                        cloniDaDistruggere.Add(child.gameObject);
                    }
                }

                foreach (GameObject clone in cloniDaDistruggere)
                {
                    RestoreLogger.Log($"Rimosso residuo di una sessione precedente {clone.name} su {vaschettaGO.name}.");
                    Destroy(clone);
                }
            }
        }
    }

    private void OnFaseCambiata(FaseRestauroSO fase)
    {
        RestoreLogger.Log($"Fase del tavolo modificata in {(fase != null ? fase.name : "nessuna")}.");
        
        ultimaFaseAttiva = fase;

        AttivaFase(fase);
    }

    private void OnTavoloSvuotato()
    {
        // Pulizia dei GameObject e delle texture di runtime prima che il SO nulli i riferimenti.
        // Il SO emette questo evento PRIMA di nullare, quindi qui i valori sono ancora validi.
        if (tavoloCorrente != null)
        {
            if (tavoloCorrente.vaschettaGameObject != null)
            {
                Destroy(tavoloCorrente.vaschettaGameObject);
            }
            if (tavoloCorrente.anforaAssemblata != null)
            {
                Destroy(tavoloCorrente.anforaAssemblata);
            }
            if (tavoloCorrente.collaTextureMosaico != null)
            {
                Destroy(tavoloCorrente.collaTextureMosaico);
            }
        }

        isRestorationComplete = false;
        ImpostaCollider(false);
    }

    /// <summary>
    /// Completa formalmente il processo di restauro del reperto.
    /// Riabilita i collider fisici sull'oggetto restaurato per consentirne la raccolta ed esegue l'auto-exit.
    /// </summary>
    public void CompletaRestauro()
    {
        isRestorationComplete = true;
        ImpostaCollider(false);
        if (istruzioniText != null)
        {
            istruzioniText.text = "Restauro completato!";
            istruzioniText.gameObject.SetActive(true);
        }

        // Riabilitazione del collider dell'oggetto restaurato per renderlo raccoglibile
        GameObject objRestaurato = tavoloCorrente.vaschettaGameObject != null ? tavoloCorrente.vaschettaGameObject : tavoloCorrente.anforaAssemblata;
        if (objRestaurato != null)
        {
            if (objRestaurato.GetComponent<OggettoRestaurato>() == null)
            {
                objRestaurato.AddComponent<OggettoRestaurato>();
                RestoreLogger.Log($"Componente OggettoRestaurato aggiunto a {objRestaurato.name}.");
            }

            Collider col = objRestaurato.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
                RestoreLogger.Log($"Collider riabilitato su {objRestaurato.name}.");
            }
        }

        // Uscita automatica per ripristinare la mobilità del giocatore
        StartCoroutine(AutoExitRestoration(1.5f));
    }

    // IInteractable

    /// <summary>
    /// Verifica se il giocatore può iniziare l'interazione con la postazione di restauro.
    /// </summary>
    /// <returns>True se non è già in corso un restauro.</returns>
    public bool canInteract()
    {
        bool can = !isRestoring;
        RestoreLogger.Log($"Stato interazione impostato a {can}.");
        return can;
    }

    /// <summary>
    /// Ritorna il testo contestuale da visualizzare nell'HUD dell'interazione del giocatore.
    /// </summary>
    public string GetInteractionText() => "[E] Usa postazione di restauro";

    /// <summary>
    /// Avvia la sessione di restauro, disabilita il movimento FPS del player ed effettua il close-up della camera sul tavolo.
    /// </summary>
    public void StartInteraction()
    {
        RestoreLogger.Log("Interazione di restauro avviata.");
        if (!canInteract()) return;

        isRestoring = true;
        player.enabled = false;
        player.NascondiTestoInterazione();
        ImpostaCollider(false);

        if (testoDaDisattivare != null)
        {
            testoDaDisattivare.SetActive(false);
        }

        startCameraParent   = playerCamera.transform.parent;
        startCameraPosition = playerCamera.transform.position;
        startCameraRotation = playerCamera.transform.rotation;
        playerCamera.transform.SetParent(null, true);

        canvas.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        FaseRestauroSO faseIniziale = tavoloCorrente.faseCorrente ?? fasiMappate[0].faseSO;
        if (tavoloCorrente.faseCorrente == null)
            tavoloCorrente.faseCorrente = faseIniziale;

        AttivaFase(faseIniziale);
    }

    /// <summary>
    /// Interrompe o conclude l'interazione, ripristinando il movimento del giocatore e riportando la telecamera in prima persona.
    /// </summary>
    public void StopInteraction()
    {
        if (!isRestoring) return;
        isRestoring = false;

        RestoreLogger.Log("Interazione di restauro interrotta.");
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        if (testoDaDisattivare != null)
        {
            testoDaDisattivare.SetActive(true);
        }

        if (progressSlider != null)
        {
            progressSlider.gameObject.SetActive(false);
        }

        if (istruzioniText != null)
        {
            istruzioniText.text = "";
            if (suggerimentoMano == null)
            {
                istruzioniText.gameObject.SetActive(false);
            }
        }

        DisattivaFasi();
        ultimaFaseAttiva = null;
        StartCameraTransition(startCameraPosition, startCameraRotation, startCameraParent, restorePlayer: true);
    }

    // Logica Fasi

    private void AttivaFase(FaseRestauroSO fase)
    {
        if (fase == null) return;
        RestoreLogger.Log($"Attivazione della fase {fase.name}.");
        ultimaFaseAttiva = fase;

        if (!TrovaMappatura(fase, out FaseMapping mapping))
        {
            Debug.LogWarning($"Fase {fase.name} non presente nelle fasi mappate.");
            return;
        }

        faseMappingCorrente = mapping;

        foreach (var m in fasiMappate)
        {
            if (m.faseGameObject != null)
                m.faseGameObject.SetActive(m.faseGameObject == mapping.faseGameObject);
        }

        if (istruzioniText != null)
        {
            istruzioniText.text = fase.DescrizioneFase;
            if (suggerimentoMano == null)
            {
                istruzioniText.gameObject.SetActive(!string.IsNullOrEmpty(fase.DescrizioneFase));
            }
            else
            {
                istruzioniText.gameObject.SetActive(true);
            }
        }

        StartCameraTransition(mapping.targetCamera.position, mapping.targetCamera.rotation, null, restorePlayer: false);
    }

    private bool TrovaMappatura(FaseRestauroSO fase, out FaseMapping result)
    {
        foreach (var m in fasiMappate)
        {
            if (m.faseSO == fase)
            {
                result = m;
                return true;
            }
        }
        result = default;
        return false;
    }

    private void DisattivaFasi()
    {
        // Disiscrizione dall'evento della fase corrente prima di disattivare tutto
        SottoscriviOnPhaseCompleted(null);

        foreach (var m in fasiMappate)
        {
            if (m.faseGameObject != null)
                m.faseGameObject.SetActive(false);
        }
    }

    // Transizione Camera

    private void StartCameraTransition(Vector3 targetPos, Quaternion targetRot, Transform parent, bool restorePlayer)
    {
        if (activeTransitionCoroutine != null)
        {
            StopCoroutine(activeTransitionCoroutine);
        }
        activeTransitionCoroutine = StartCoroutine(TransitionCamera(targetPos, targetRot, parent, restorePlayer));
    }

    private IEnumerator TransitionCamera(Vector3 targetPos, Quaternion targetRot, Transform parent, bool restorePlayer)
    {
        RestoreLogger.Log("Avvio transizione della telecamera.");
        float      elapsed  = 0f;
        Vector3    startPos = playerCamera.transform.position;
        Quaternion startRot = playerCamera.transform.rotation;

        while (elapsed < transitionDuration)
        {
            float t = elapsed / transitionDuration;
            playerCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
            playerCamera.transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        playerCamera.transform.SetParent(parent, true);
        playerCamera.transform.position = targetPos;
        playerCamera.transform.rotation = targetRot;
        RestoreLogger.Log("Transizione della telecamera completata.");

        if (restorePlayer)
        {
            player.enabled = true;
            if (!isRestorationComplete) ImpostaCollider(true);
            isRestoring = false;
            RestoreLogger.Log("Posizione e controllo del giocatore ripristinati.");

            // Aggiorna il testo del suggerimento mano al termine del restauro/transizione
            if (suggerimentoMano != null)
            {
                suggerimentoMano.AggiornaSuggerimento();
            }

            yield break;
        }

        GameObject faseGO = faseMappingCorrente.faseGameObject;
        if (faseGO == null) yield break;

        IRestorationPhaseManager phaseManager = faseGO.GetComponentInChildren<IRestorationPhaseManager>(true);
        if (phaseManager != null)
        {
            RestoreLogger.Log($"Notifica di transizione completata inviata a {phaseManager.GetType().Name} su {faseGO.name}.");
            phaseManager.CameraTransitionCompleted();
        }
        else
        {
            Debug.LogWarning($"Nessun gestore compatibile trovato per la fase {faseMappingCorrente.faseSO?.name} in {faseGO.name}.");
        }

        // Sottoscrizione all'evento IRestorationPhase (parallelo al sistema legacy)
        SottoscriviOnPhaseCompleted(faseGO);
    }

    /// <summary>
    /// Sottoscrive o disiscrisce dal gestore di fase che implementa IRestorationPhase.
    /// Passare null per disiscriversi senza sottoscrivere a una nuova fase.
    /// </summary>
    private void SottoscriviOnPhaseCompleted(GameObject nuovaFaseGO)
    {
        // Disiscrizione dalla fase precedente
        if (faseCorrentePhaseable != null)
        {
            faseCorrentePhaseable.OnPhaseCompleted -= OnCurrentPhaseCompleted;
            faseCorrentePhaseable = null;
        }

        if (nuovaFaseGO == null) return;

        // Iscrizione alla nuova fase (se implementa IRestorationPhase)
        IRestorationPhase nuovaFase = nuovaFaseGO.GetComponentInChildren<IRestorationPhase>(true);
        if (nuovaFase != null)
        {
            faseCorrentePhaseable = nuovaFase;
            faseCorrentePhaseable.OnPhaseCompleted += OnCurrentPhaseCompleted;
            RestoreLogger.Log($"[RestoreManager] Iscritto a OnPhaseCompleted di {nuovaFase.GetType().Name}.");
        }
    }

    private void OnCurrentPhaseCompleted(bool hasNextPhase)
    {
        RestoreLogger.Log($"[RestoreManager] OnPhaseCompleted ricevuto da {faseCorrentePhaseable?.GetType().Name} (hasNextPhase: {hasNextPhase}).");

        if (!hasNextPhase)
        {
            RestoreLogger.Log("[RestoreManager] Nessuna fase successiva configurata. Completamento del restauro.");
            CompletaRestauro();
        }
    }

    // Collider

    private void ImpostaCollider(bool attivo)
    {
        Collider col = TrovaCollider();
        if (col != null)
        {
            col.enabled = attivo;
            RestoreLogger.Log($"Collider {(attivo ? "abilitato" : "disabilitato")}.");
        }
    }

    private void InitCollider()
    {
        bool haOggetto = tavoloCorrente.oggettoCorrente != null;
        Collider col = TrovaCollider();
        if (col != null)
        {
            col.enabled = haOggetto;
            RestoreLogger.Log($"Stato collider inizializzato a {col.enabled}.");
        }
        else
        {
            Debug.LogWarning($"Nessun collider trovato su {gameObject.name} o sui relativi elementi genitori.");
        }
    }

    private Collider TrovaCollider() => GetComponent<Collider>();

    // Debug

    private void StampaGerarchiaEComponenti(Transform t, int livello)
    {
        string indent = new string('-', livello * 2) + " ";
        foreach (Transform child in t)
        {
            Component[] components = child.GetComponents<Component>();
            string compList = "";
            foreach (var c in components)
                compList += c == null ? "[null] " : $"[{c.GetType().Name}] ";
            RestoreLogger.Log($"{indent}{child.name} {(child.gameObject.activeSelf ? "attivo" : "inattivo")} con componenti {compList}");
            StampaGerarchiaEComponenti(child, livello + 1);
        }
    }

    private IEnumerator AutoExitRestoration(float delay)
    {
        yield return new WaitForSeconds(delay);
        StopInteraction();
    }
}
