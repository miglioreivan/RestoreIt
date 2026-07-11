using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class GestoreAssemblaggio : MonoBehaviour, IRestorationPhaseManager, IRestorationPhase
{
    public event System.Action<bool> OnPhaseCompleted;
    [System.Serializable]
    public class PezzoInfo
    {
        public GameObject gameObject;
        public Vector3 originalLocalPos;
        public Quaternion originalLocalRot;
        public Vector3 originalLocalScale;
        public bool isSnapped;
        public Collider collider;
    }

    [Header("Tavolo")]
    [SerializeField] private TavoloSO tavoloCorrente;
    [SerializeField] private FaseRestauroSO triggerAssemblaggio;
    [SerializeField] private FaseRestauroSO faseSuccessiva; // Fase successiva per l'incollaggio (es. FaseColla)
    [SerializeField] private Camera cameraRestauro;
    [SerializeField] private RestoreManager restoreManager;

    [Header("Spawn Anfora Centrale")]
    [SerializeField] private Transform puntoSpawnAnfora;

    [Header("Parametri Assemblaggio")]
    [SerializeField] private float snapDistance = 0.08f;
    [SerializeField] private float snapAngle = 25f;
    [SerializeField] private float velocitaRotazione = 100f;

    [Header("Eventi")]
    [SerializeField] private UnityEvent onAssemblaggioCompletato;

    [Header("Grafica Cursore Drag & Drop")]
    [SerializeField] private Texture2D cursorDragTexture;
    [SerializeField] private Vector2 cursorDragHotspot = Vector2.zero;

    [Header("Audio")]
    [SerializeField] private SoundEffect snapSound;

    private GameObject ghostAnfora;
    private List<PezzoInfo> listaPezzi = new List<PezzoInfo>();
    private bool isAssemblaggioActive = false;

    // Stato di Drag & Drop
    private PezzoInfo pezzoTrascinato;
    private Plane dragPlane;
    private Vector3 dragOffset;
    private Vector3 positionBeforeDrag;
    private Quaternion rotationBeforeDrag;
    private Vector3 scaleBeforeDrag;
    private Transform parentBeforeDrag;
    private bool cameraTransitionFinished = false;

    private void Awake()
    {
        if (restoreManager == null)
        {
            restoreManager = GetComponentInParent<RestoreManager>(true);
            if (restoreManager == null && transform.parent != null)
            {
                restoreManager = transform.parent.GetComponentInChildren<RestoreManager>(true);
            }
        }

        if (cameraRestauro == null)
        {
            cameraRestauro = GetComponentInChildren<Camera>(true);
            if (cameraRestauro == null)
            {
                cameraRestauro = GetComponentInParent<Camera>(true);
            }
        }
    }

    private void OnEnable()
    {
        if (tavoloCorrente != null)
        {
            tavoloCorrente.OnFaseCambiata += OnFaseCambiata;

            // Cattura immediatamente la fase se è già quella corretta all'abilitazione del GameObject
            if (tavoloCorrente.faseCorrente == triggerAssemblaggio)
            {
                Debug.Log($"Rilevata fase di assemblaggio {triggerAssemblaggio.name} all'attivazione.");
                IniziaAssemblaggio();
            }
        }
    }

    private void OnDisable()
    {
        if (tavoloCorrente != null)
            tavoloCorrente.OnFaseCambiata -= OnFaseCambiata;
    }

    private void OnFaseCambiata(FaseRestauroSO fase)
    {
        Debug.Log($"Fase di restauro modificata in {(fase != null ? fase.name : "nessuna")}.");
        if (fase != triggerAssemblaggio)
        {
            TerminaAssemblaggio();
            return;
        }

        IniziaAssemblaggio();
    }

    private void IniziaAssemblaggio()
    {
        Debug.Log("Avvio del processo di assemblaggio dei pezzi.");
        cameraTransitionFinished = false;
        if (tavoloCorrente.vaschettaCorrente == null || tavoloCorrente.vaschettaGameObject == null) return;

        if (puntoSpawnAnfora == null)
        {
            Debug.LogError("Punto di spawn dell'anfora non assegnato nell'Inspector.");
            return;
        }

        TerminaAssemblaggio();

        // Istanziazione del modello dell'anfora in modalità semitrasparente (modello guida)
        GameObject prefabAnfora = tavoloCorrente.vaschettaCorrente.prefabAnfora;
        if (prefabAnfora != null)
        {
            ghostAnfora = Instantiate(prefabAnfora, puntoSpawnAnfora);
            ghostAnfora.transform.localPosition = Vector3.zero;
            ghostAnfora.transform.localRotation = Quaternion.identity;

            Vector3 parentScale = puntoSpawnAnfora.lossyScale;
            Vector3 targetScale = prefabAnfora.transform.localScale;
            ghostAnfora.transform.localScale = new Vector3(
                parentScale.x != 0 ? targetScale.x / parentScale.x : targetScale.x,
                parentScale.y != 0 ? targetScale.y / parentScale.y : targetScale.y,
                parentScale.z != 0 ? targetScale.z / parentScale.z : targetScale.z
            );

            Collider[] cols = ghostAnfora.GetComponentsInChildren<Collider>();
            if (cols.Length == 0)
            {
                foreach (var r in ghostAnfora.GetComponentsInChildren<MeshRenderer>())
                {
                    r.gameObject.AddComponent<MeshCollider>();
                }
            }
        }

        // Configurazione dei singoli pezzi presenti nella vaschetta
        GameObject prefabPezzi = tavoloCorrente.vaschettaCorrente.prefabPezzi;
        if (prefabPezzi == null)
        {
            prefabPezzi = tavoloCorrente.vaschettaCorrente.prefabAnfora;
            Debug.Log($"Utilizzo del prefab dell'anfora come fallback per i pezzi di {tavoloCorrente.vaschettaCorrente.name}.");
        }
        GameObject vaschettaGO = tavoloCorrente.vaschettaGameObject;

        if (prefabPezzi != null && vaschettaGO != null)
        {
            ConfigurazioneVaschetta config = vaschettaGO.GetComponent<ConfigurazioneVaschetta>();
            if (config == null)
            {
                Debug.LogError($"Componente ConfigurazioneVaschetta mancante sul GameObject della vaschetta {vaschettaGO.name}.");
                return;
            }

            Debug.Log($"Configurazione vaschetta caricata. Pezzi da posizionare: {config.pezziOrdinati.Count}.");

            foreach (var goPezzo in config.pezziOrdinati)
            {
                if (goPezzo == null) continue;

                Transform targetTransform = TrovaFiglioNelPrefab(prefabPezzi.transform, goPezzo.name);
                if (targetTransform != null)
                {
                    PezzoInfo pezzo = new PezzoInfo
                    {
                        gameObject = goPezzo,
                        originalLocalPos = targetTransform.localPosition,
                        originalLocalRot = targetTransform.localRotation,
                        originalLocalScale = targetTransform.localScale,
                        isSnapped = false
                    };

                    Collider col = goPezzo.GetComponent<Collider>();
                    if (col == null)
                    {
                        col = goPezzo.AddComponent<MeshCollider>();
                        Debug.Log($"Aggiunto MeshCollider all'oggetto {goPezzo.name}.");
                    }
                    pezzo.collider = col;

                    listaPezzi.Add(pezzo);
                    Debug.Log($"Pezzo {goPezzo.name} caricato con successo.");
                }
                else
                {
                    Debug.LogWarning($"Pezzo {goPezzo.name} non trovato nel prefab dell'anfora.");
                }
            }
        }
        else
        {
            Debug.LogError("Impossibile configurare i pezzi perché il prefab o la vaschetta non sono validi.");
        }

        isAssemblaggioActive = true;
    }

    private void TerminaAssemblaggio()
    {
        isAssemblaggioActive = false;
        Debug.Log("Termine dell'assemblaggio ed esecuzione della pulizia.");
        if (pezzoTrascinato != null)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            pezzoTrascinato = null;
        }

        if (ghostAnfora != null)
        {
            if (tavoloCorrente != null && tavoloCorrente.anforaAssemblata == ghostAnfora)
            {
                Debug.Log("L'anfora ghost è persistente e non viene distrutta.");
                ghostAnfora = null;
            }
            else
            {
                Debug.Log("Rimozione dell'anfora ghost locale.");
                Destroy(ghostAnfora);
                ghostAnfora = null;
            }
        }

        listaPezzi.Clear();
    }

    private void Update()
    {
        if (!isAssemblaggioActive) return;

        GestisciRotazione();
        GestisciDragAndDrop();
    }

    private void GestisciRotazione()
    {
        if (ghostAnfora == null) return;

        float inputRot = 0f;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.sKey.isPressed)
                inputRot = -1f;
            else if (Keyboard.current.dKey.isPressed)
                inputRot = 1f;
        }

        if (Mathf.Abs(inputRot) > 0.01f)
        {
            ghostAnfora.transform.Rotate(Vector3.up, inputRot * velocitaRotazione * Time.deltaTime, Space.Self);
        }
    }

    private void GestisciDragAndDrop()
    {
        if (cameraRestauro == null || Mouse.current == null || !cameraTransitionFinished) return;

        bool isLeftPressed = Mouse.current.leftButton.isPressed;
        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (isLeftPressed)
        {
            if (pezzoTrascinato == null)
            {
                Ray ray = cameraRestauro.ScreenPointToRay(mousePos);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                {
                    Debug.Log($"Selezionato l'elemento {hit.collider.gameObject.name}.");
                    bool trovato = false;
                    foreach (var pezzo in listaPezzi)
                    {
                        if (!pezzo.isSnapped && (hit.collider.gameObject == pezzo.gameObject || hit.collider.transform.IsChildOf(pezzo.gameObject.transform)))
                        {
                            pezzoTrascinato = pezzo;
                                                     // Memorizzazione dello stato di trasformazione iniziale per consentire il riposizionamento in caso di rilascio non valido
                            positionBeforeDrag = pezzoTrascinato.gameObject.transform.position;
                            rotationBeforeDrag = pezzoTrascinato.gameObject.transform.localRotation;
                            scaleBeforeDrag = pezzoTrascinato.gameObject.transform.localScale;
                            parentBeforeDrag = pezzoTrascinato.gameObject.transform.parent;

                            // Scollega temporaneamente dal parent durante il drag per evitare deformazioni da scala non uniforme (shearing)
                            pezzoTrascinato.gameObject.transform.SetParent(null, true);

                            // Calcolo e applicazione della scala corretta nel mondo (ora localScale = scala globale)
                            if (ghostAnfora != null)
                            {
                                Vector3 targetWorldScale = Vector3.Scale(ghostAnfora.transform.lossyScale, pezzoTrascinato.originalLocalScale);
                                pezzoTrascinato.gameObject.transform.localScale = targetWorldScale;
                            }

                            // Allinea la rotazione del pezzo nel mondo a quella finale del modello guida
                            pezzoTrascinato.gameObject.transform.rotation = ghostAnfora.transform.rotation * pezzoTrascinato.originalLocalRot;
                            
                            // Determinazione della posizione finale desiderata nel mondo
                            Vector3 targetWorldPos = ghostAnfora.transform.TransformPoint(pezzoTrascinato.originalLocalPos);

                            // Calcolo della profondità del target rispetto alla telecamera di restauro
                            float targetDepth = Vector3.Dot(targetWorldPos - cameraRestauro.transform.position, cameraRestauro.transform.forward);
                            
                            // Creazione del piano di drag perpendicolare alla vista e posizionato in prossimità del target
                            Vector3 planePoint = cameraRestauro.transform.position + cameraRestauro.transform.forward * (targetDepth - 0.05f);
                            dragPlane = new Plane(-cameraRestauro.transform.forward, planePoint);

                            // Proiezione della posizione del puntatore sul piano di drag per centrare l'oggetto
                            Ray rayStart = cameraRestauro.ScreenPointToRay(mousePos);
                            if (dragPlane.Raycast(rayStart, out float enterStart))
                            {
                                Vector3 pointOnPlane = rayStart.GetPoint(enterStart);
                                pezzoTrascinato.gameObject.transform.position = pointOnPlane;
                            }

                            dragOffset = Vector3.zero;

                            Debug.Log($"Inizio trascinamento del pezzo {pezzoTrascinato.gameObject.name}.");
                            
                            if (cursorDragTexture != null)
                            {
                                Cursor.SetCursor(cursorDragTexture, cursorDragHotspot, CursorMode.Auto);
                            }

                            trovato = true;
                            break;
                        }
                    }
                    if (!trovato)
                    {
                        Debug.LogWarning($"L'elemento selezionato {hit.collider.gameObject.name} non corrisponde a nessun pezzo da assemblare.");
                    }
                }
            }
            else
            {
                Ray ray = cameraRestauro.ScreenPointToRay(mousePos);
                if (dragPlane.Raycast(ray, out float enter))
                {
                    Vector3 targetPos = ray.GetPoint(enter) + dragOffset;
                    pezzoTrascinato.gameObject.transform.position = targetPos;

                    // Blocco della rotazione sull'orientamento target corretto durante lo spostamento
                    pezzoTrascinato.gameObject.transform.rotation = ghostAnfora.transform.rotation * pezzoTrascinato.originalLocalRot;

                    VerificaSnap(pezzoTrascinato);
                }
            }
        }
        else
        {
            if (pezzoTrascinato != null)
            {
                if (!pezzoTrascinato.isSnapped)
                {
                    pezzoTrascinato.gameObject.transform.SetParent(parentBeforeDrag, true);
                    pezzoTrascinato.gameObject.transform.position = positionBeforeDrag;
                    pezzoTrascinato.gameObject.transform.localRotation = rotationBeforeDrag;
                    pezzoTrascinato.gameObject.transform.localScale = scaleBeforeDrag;
                    Debug.Log($"Pezzo {pezzoTrascinato.gameObject.name} rilasciato senza snap, ritorno alla vaschetta.");
                }
                pezzoTrascinato = null;
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }
    }

    private void VerificaSnap(PezzoInfo pezzo)
    {
        if (ghostAnfora == null) return;

        Vector3 targetWorldPos = ghostAnfora.transform.TransformPoint(pezzo.originalLocalPos);
        Quaternion targetWorldRot = ghostAnfora.transform.rotation * pezzo.originalLocalRot;

        float dist = Vector3.Distance(pezzo.gameObject.transform.position, targetWorldPos);

        // Per l'oggetto trascinato lo snap avviene in base alla sola tolleranza di distanza spaziale
        bool match = false;
        if (pezzo == pezzoTrascinato)
        {
            match = (dist <= snapDistance);
        }
        else
        {
            float angle = Quaternion.Angle(pezzo.gameObject.transform.rotation, targetWorldRot);
            match = (dist <= snapDistance && angle <= snapAngle);
        }

        if (match)
        {
            pezzo.isSnapped = true;
            pezzo.gameObject.transform.SetParent(ghostAnfora.transform, false);
            
            pezzo.gameObject.transform.localPosition = pezzo.originalLocalPos;
            pezzo.gameObject.transform.localRotation = pezzo.originalLocalRot;
            pezzo.gameObject.transform.localScale = pezzo.originalLocalScale;

            if (pezzo.collider != null)
                pezzo.collider.enabled = false;

            pezzoTrascinato = null;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            Debug.Log($"Pezzo {pezzo.gameObject.name} posizionato correttamente.");

            if (AudioManager.Instance == null)
            {
                Debug.LogWarning($"[GestoreAssemblaggio] '{gameObject.name}': AudioManager.Instance è null! Assicurati che un GameObject con AudioManager esista nella scena.");
            }
            else if (snapSound.clip == null)
            {
                Debug.LogWarning($"[GestoreAssemblaggio] '{gameObject.name}': snapSound.clip non è assegnato nell'Inspector. Nessun suono verrà riprodotto.");
            }
            else
            {
                AudioManager.Instance.Play3D(snapSound, pezzo.gameObject.transform.position);
            }

            // Verifica del completamento del posizionamento di tutti i pezzi
            bool tuttiPosizionati = true;
            foreach (var p in listaPezzi)
            {
                if (!p.isSnapped)
                {
                    tuttiPosizionati = false;
                    break;
                }
            }

            if (tuttiPosizionati)
            {
                CompletaFaseAssemblaggio();
            }
        }
    }

    private void CompletaFaseAssemblaggio()
    {
        StartCoroutine(SequenzaCompletamentoAssemblaggio());
    }

    private System.Collections.IEnumerator SequenzaCompletamentoAssemblaggio()
    {
        isAssemblaggioActive = false;
        Debug.Log("Tutti i pezzi sono stati posizionati con successo.");
        onAssemblaggioCompletato?.Invoke();

        if (ghostAnfora != null)
        {
            Debug.Log($"Modifica parent di {ghostAnfora.name} a null per preservarlo nella transizione.");
            ghostAnfora.transform.SetParent(null, true);
            
            if (tavoloCorrente != null)
            {
                tavoloCorrente.anforaAssemblata = ghostAnfora;
                Debug.Log("Riferimento all'anfora assemblata memorizzato nel tavolo di lavoro.");
            }

            yield return RestorationUtils.VibraOggetto(ghostAnfora, 0.6f, 0.012f);
        }
        else
        {
            Debug.LogError("Impossibile persistere l'anfora perché l'oggetto ghost è nullo.");
        }

        if (tavoloCorrente != null && faseSuccessiva != null)
        {
            Debug.Log($"Avanzamento alla fase successiva: {faseSuccessiva.name}.");
            tavoloCorrente.AvanzaFase(faseSuccessiva);
        }
        else
        {
            Debug.LogError("Impostazioni incomplete per avanzare alla fase successiva.");
        }
        OnPhaseCompleted?.Invoke(faseSuccessiva != null);
    }

    public void CameraTransitionCompleted()
    {
        cameraTransitionFinished = true;
        Debug.Log("Transizione telecamera completata. Trascinamento abilitato.");
    }

    private Transform TrovaFiglioNelPrefab(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = TrovaFiglioNelPrefab(parent.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }
}
