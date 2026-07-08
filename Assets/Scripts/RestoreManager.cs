using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RestoreManager : MonoBehaviour, IInteractable
{
    [System.Serializable]
    public struct FaseMapping
    {
        public FaseRestauroSO faseSO;
        public GameObject     faseGameObject;
        public Transform      targetCamera;
    }

    [Header("Impostazioni Generali")]
    [SerializeField] private MonoBehaviour player;
    [SerializeField] private float         transitionDuration = 1.0f;
    [SerializeField] private GameObject    canvas;
    [SerializeField] private TMPro.TextMeshProUGUI istruzioniText;

    [Header("Stato Tavolo")]
    [SerializeField] private TavoloSO tavoloCorrente;

    [Header("Fasi del Restauro")]
    [SerializeField] private List<FaseMapping> fasiMappate = new List<FaseMapping>();

    private Camera         playerCamera;
    private Vector3        startCameraPosition;
    private Quaternion     startCameraRotation;
    private Transform      startCameraParent;
    private bool           isRestoring           = false;
    private bool           isRestorationComplete = false;
    private FaseMapping    faseMappingCorrente;

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

        Transform searchRoot = transform.parent != null ? transform.parent : transform;
        Debug.Log($"=== [RestoreManager] Gerarchia '{searchRoot.name}' ===");
        StampaGerarchiaEComponenti(searchRoot, 1);
        Debug.Log("====================================================");
    }

    private void OnEnable()
    {
        if (tavoloCorrente == null) return;
        tavoloCorrente.OnOggettoPosato  += OnOggettoPosato;
        tavoloCorrente.OnFaseCambiata   += OnFaseCambiata;
        tavoloCorrente.OnTavoloSvuotato += OnTavoloSvuotato;
        Debug.Log("[RestoreManager] Sottoscritto agli eventi di tavoloCorrente.");
    }

    private void OnDisable()
    {
        if (tavoloCorrente == null) return;
        tavoloCorrente.OnOggettoPosato  -= OnOggettoPosato;
        tavoloCorrente.OnFaseCambiata   -= OnFaseCambiata;
        tavoloCorrente.OnTavoloSvuotato -= OnTavoloSvuotato;
    }

    // Validazione

    private bool ValidaConfigurazione()
    {
        if (tavoloCorrente == null)
        {
            Debug.LogError($"[RestoreManager] '{gameObject.name}': tavoloCorrente non assegnato nell'Inspector.");
            enabled = false;
            return false;
        }
        if (canvas == null)
        {
            Debug.LogError($"[RestoreManager] '{gameObject.name}': canvas non assegnato nell'Inspector.");
            enabled = false;
            return false;
        }
        if (player == null)
        {
            Debug.LogError($"[RestoreManager] '{gameObject.name}': player non assegnato nell'Inspector.");
            enabled = false;
            return false;
        }
        if (playerCamera == null)
        {
            Debug.LogError($"[RestoreManager] '{gameObject.name}': Camera.main non trovata nella scena. Assicurarsi che la camera principale abbia il tag 'MainCamera'.");
            enabled = false;
            return false;
        }
        if (fasiMappate == null || fasiMappate.Count == 0)
        {
            Debug.LogError($"[RestoreManager] '{gameObject.name}': nessuna FaseMapping configurata. " +
                           "Aggiungi almeno una fase (FaseRestauroSO + GameObject + Camera) nell'Inspector.");
            enabled = false;
            return false;
        }
        for (int i = 0; i < fasiMappate.Count; i++)
        {
            FaseMapping m = fasiMappate[i];
            if (m.faseSO == null)
            {
                Debug.LogError($"[RestoreManager] FaseMapping[{i}]: faseSO non assegnato.");
                enabled = false;
                return false;
            }
            if (m.faseGameObject == null)
            {
                Debug.LogError($"[RestoreManager] FaseMapping[{i}] ('{m.faseSO.name}'): faseGameObject non assegnato.");
                enabled = false;
                return false;
            }
            if (m.targetCamera == null)
            {
                Debug.LogError($"[RestoreManager] FaseMapping[{i}] ('{m.faseSO.name}'): targetCamera non assegnata.");
                enabled = false;
                return false;
            }
        }
        if (istruzioniText == null)
        {
            Debug.LogWarning($"[RestoreManager] '{gameObject.name}': istruzioniText non assegnato nell'Inspector. Non verranno mostrate le istruzioni delle fasi.");
        }
        return true;
    }

    // Gestione Stato Tavolo

    private void OnOggettoPosato(DatiOggettoSO oggetto)
    {
        Debug.Log($"[RestoreManager] OnOggettoPosato: {(oggetto != null ? oggetto.name : "NULL")}");
        ImpostaCollider(oggetto != null);
        
        if (oggetto != null)
        {
            isRestorationComplete = false;

            // Pulizia di eventuali residui di garze o aerolam incollati da sessioni precedenti
            if (tavoloCorrente != null && tavoloCorrente.vaschettaGameObject != null)
            {
                GameObject vaschettaGO = tavoloCorrente.vaschettaGameObject;
                List<GameObject> cloniDaDistruggere = new List<GameObject>();

                foreach (Transform child in vaschettaGO.transform)
                {
                    if (child.name.Contains("(Clone)") || child.name.Contains("Garza") || child.name.Contains("Aerolam"))
                    {
                        cloniDaDistruggere.Add(child.gameObject);
                    }
                }

                foreach (GameObject clone in cloniDaDistruggere)
                {
                    Debug.Log($"[RestoreManager] Trovato residuo di una sessione precedente: '{clone.name}' su '{vaschettaGO.name}'. Rimozione in corso.");
                    Destroy(clone);
                }
            }
        }
    }

    private void OnFaseCambiata(FaseRestauroSO fase)
    {
        Debug.Log($"[RestoreManager] OnFaseCambiata: {(fase != null ? fase.name : "NULL")}");
        AttivaFase(fase);
    }

    private void OnTavoloSvuotato()
    {
        isRestorationComplete = false;
        ImpostaCollider(false);
    }

    public void CompletaRestauro()
    {
        isRestorationComplete = true;
        ImpostaCollider(false);
        if (istruzioniText != null)
        {
            istruzioniText.text = "Restauro completato!";
            istruzioniText.gameObject.SetActive(true);
        }

        // Riattiva il collider dell'oggetto restaurato per permettere il pickup
        GameObject objRestaurato = tavoloCorrente.vaschettaGameObject != null ? tavoloCorrente.vaschettaGameObject : tavoloCorrente.anforaAssemblata;
        if (objRestaurato != null)
        {
            if (objRestaurato.GetComponent<OggettoRestaurato>() == null)
            {
                objRestaurato.AddComponent<OggettoRestaurato>();
                Debug.Log($"[RestoreManager] Aggiunto componente OggettoRestaurato a '{objRestaurato.name}'.");
            }

            Collider col = objRestaurato.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
                Debug.Log($"[RestoreManager] Riattivato collider su '{objRestaurato.name}' per renderlo raccoglibile.");
            }
        }

        // Uscita automatica dopo un breve ritardo per ripristinare il controllo del player
        StartCoroutine(AutoExitRestoration(1.5f));
    }

    // IInteractable

    public bool canInteract()
    {
        bool can = !isRestoring;
        Debug.Log($"[RestoreManager] canInteract: {can}");
        return can;
    }

    public string GetInteractionText() => "[E] Usa postazione di restauro";

    public void StartInteraction()
    {
        Debug.Log("[RestoreManager] StartInteraction.");
        if (!canInteract()) return;

        isRestoring = true;
        player.enabled = false;
        ImpostaCollider(false);

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

    public void StopInteraction()
    {
        Debug.Log("[RestoreManager] StopInteraction.");
        canvas.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        if (istruzioniText != null)
        {
            istruzioniText.text = "";
            istruzioniText.gameObject.SetActive(false);
        }

        DisattivaFasi();
        StartCoroutine(TransitionCamera(startCameraPosition, startCameraRotation, startCameraParent, restorePlayer: true));
    }

    // Logica Fasi

    private void AttivaFase(FaseRestauroSO fase)
    {
        if (fase == null) return;
        Debug.Log($"[RestoreManager] AttivaFase: {fase.name}");

        if (!TrovaMappatura(fase, out FaseMapping mapping))
        {
            Debug.LogWarning($"[RestoreManager] Fase '{fase.name}' non presente in fasiMappate.");
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
            istruzioniText.gameObject.SetActive(!string.IsNullOrEmpty(fase.DescrizioneFase));
        }

        StartCoroutine(TransitionCamera(mapping.targetCamera.position, mapping.targetCamera.rotation, null, restorePlayer: false));
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
        foreach (var m in fasiMappate)
        {
            if (m.faseGameObject != null)
                m.faseGameObject.SetActive(false);
        }
    }

    // Transizione Camera

    private IEnumerator TransitionCamera(Vector3 targetPos, Quaternion targetRot, Transform parent, bool restorePlayer)
    {
        Debug.Log($"[RestoreManager] TransitionCamera avviata. restorePlayer: {restorePlayer}");
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
        Debug.Log("[RestoreManager] Transizione camera completata.");

        if (restorePlayer)
        {
            player.enabled = true;
            if (!isRestorationComplete) ImpostaCollider(true);
            isRestoring = false;
            Debug.Log("[RestoreManager] Player ripristinato.");
            yield break;
        }

        GameObject faseGO = faseMappingCorrente.faseGameObject;
        if (faseGO == null) yield break;

        StrumentoPulizia strumento = faseGO.GetComponentInChildren<StrumentoPulizia>(true);
        if (strumento != null)
        {
            Debug.Log($"[RestoreManager] Avvio minigame pulizia su '{strumento.gameObject.name}'.");
            strumento.CountVisiblePixel();
            strumento.SetMouseCursor();
            strumento.IniziaMinigame();
            yield break;
        }

        GestoreAssemblaggio gestore = faseGO.GetComponentInChildren<GestoreAssemblaggio>(true);
        if (gestore != null)
        {
            Debug.Log("[RestoreManager] Notifica GestoreAssemblaggio: transizione completata.");
            gestore.CameraTransitionCompleted();
            yield break;
        }

        GestoreIncollaggio gestoreIncollaggio = faseGO.GetComponentInChildren<GestoreIncollaggio>(true);
        if (gestoreIncollaggio != null)
        {
            Debug.Log("[RestoreManager] Notifica GestoreIncollaggio: transizione completata.");
            gestoreIncollaggio.CameraTransitionCompleted();
            yield break;
        }

        GestoreIncollaggioMosaico gestoreIncollaggioMosaico = faseGO.GetComponentInChildren<GestoreIncollaggioMosaico>(true);
        if (gestoreIncollaggioMosaico != null)
        {
            Debug.Log("[RestoreManager] Notifica GestoreIncollaggioMosaico: transizione completata.");
            gestoreIncollaggioMosaico.CameraTransitionCompleted();
            yield break;
        }

        GestoreGarze gestoreGarze = faseGO.GetComponentInChildren<GestoreGarze>(true);
        if (gestoreGarze != null)
        {
            Debug.Log("[RestoreManager] Notifica GestoreGarze: transizione completata.");
            gestoreGarze.CameraTransitionCompleted();
            yield break;
        }

        GestoreRotazioneMosaico gestoreRotazione = faseGO.GetComponentInChildren<GestoreRotazioneMosaico>(true);
        if (gestoreRotazione != null)
        {
            Debug.Log("[RestoreManager] Notifica GestoreRotazioneMosaico: transizione completata.");
            gestoreRotazione.CameraTransitionCompleted();
            yield break;
        }

        GestoreRimozioneGarza gestoreRimozione = faseGO.GetComponentInChildren<GestoreRimozioneGarza>(true);
        if (gestoreRimozione != null)
        {
            Debug.Log("[RestoreManager] Notifica GestoreRimozioneGarza: transizione completata.");
            gestoreRimozione.CameraTransitionCompleted();
            yield break;
        }

        Debug.LogWarning($"[RestoreManager] Fase '{faseMappingCorrente.faseSO?.name}': nessun StrumentoPulizia, GestoreAssemblaggio, GestoreIncollaggio, GestoreIncollaggioMosaico, GestoreGarze, GestoreRotazioneMosaico o GestoreRimozioneGarza trovato in '{faseGO.name}'.");
    }

    // Collider

    private void ImpostaCollider(bool attivo)
    {
        Collider col = TrovaCollider();
        if (col != null)
        {
            col.enabled = attivo;
            Debug.Log($"[RestoreManager] Collider {(attivo ? "attivato" : "disattivato")}.");
        }
    }

    private void InitCollider()
    {
        bool haOggetto = tavoloCorrente.oggettoCorrente != null;
        Collider col = TrovaCollider();
        if (col != null)
        {
            col.enabled = haOggetto;
            Debug.Log($"[RestoreManager] Collider inizializzato. Enabled: {col.enabled}");
        }
        else
        {
            Debug.LogWarning($"[RestoreManager] '{gameObject.name}': nessun Collider trovato su questo GO ne sul parent.");
        }
    }

    private Collider TrovaCollider()
    {
        if (TryGetComponent<Collider>(out var col)) return col;
        if (transform.parent != null && transform.parent.TryGetComponent<Collider>(out var parentCol)) return parentCol;
        return null;
    }

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
            Debug.Log($"{indent}'{child.name}' (Attivo: {child.gameObject.activeSelf}) -> {compList}");
            StampaGerarchiaEComponenti(child, livello + 1);
        }
    }

    private IEnumerator AutoExitRestoration(float delay)
    {
        yield return new WaitForSeconds(delay);
        StopInteraction();
    }
}
