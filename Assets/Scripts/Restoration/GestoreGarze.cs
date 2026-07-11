using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections;

public class GestoreGarze : MonoBehaviour, IRestorationPhaseManager, IRestorationPhase
{
    public event System.Action<bool> OnPhaseCompleted;
    public float Progression => -1f;
    [Header("Tavolo")]
    [SerializeField] private TavoloSO tavoloCorrente;
    [SerializeField] private FaseRestauroSO triggerGarze;
    [SerializeField] private Camera cameraRestauro;
    [SerializeField] private FaseRestauroSO faseSuccessiva;
    [SerializeField] private bool usaAerolam = false;

    [Header("Punti di Spawn e Target")]
    [SerializeField] private Transform puntoSpawnGarza;
    [SerializeField] private Transform puntoTargetGarza;

    [Header("Parametri di Snap")]
    [SerializeField] private float snapDistance = 0.5f;
    [SerializeField] private float snapAngle = 15f;

    [Header("Grafica Cursore")]
    [SerializeField] private Texture2D cursorDragTexture;
    [SerializeField] private Vector2 cursorDragHotspot = Vector2.zero;

    [Header("Audio")]
    [SerializeField] private SoundEffect gauzeSound;
    [SerializeField] private SoundEffect aerolamSound;

    [Header("Eventi")]
    [SerializeField] private UnityEvent onGarzaApplicata;

    private MosaicoSO mosaicoCorrente;
    private GameObject garzaIstanza;
    private bool isGarzeActive = false;
    private bool cameraTransitionFinished = false;

    private bool isDragging = false;
    private Plane dragPlane;
    private Vector3 dragOffset;
    private Vector3 positionBeforeDrag;
    private Quaternion rotationBeforeDrag;
    private Vector3 scaleBeforeDrag;
    private Collider garzaCollider;
    private bool isSnapped = false;
    private MaterialPropertyBlock propBlock;

    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        if (tavoloCorrente != null)
        {
            tavoloCorrente.OnFaseCambiata += OnFaseCambiata;
            if (tavoloCorrente.faseCorrente == triggerGarze)
            {
                Debug.Log($"Rilevata fase di applicazione garze {triggerGarze.name} all'attivazione.");
                IniziaFaseGarze();
            }
        }
    }

    private void OnDisable()
    {
        if (tavoloCorrente != null)
        {
            tavoloCorrente.OnFaseCambiata -= OnFaseCambiata;
        }
        TerminaFaseGarze();
    }

    private void OnFaseCambiata(FaseRestauroSO fase)
    {
        Debug.Log($"Fase di restauro modificata in {(fase != null ? fase.name : "nessuna")}.");
        if (fase != triggerGarze)
        {
            TerminaFaseGarze();
            return;
        }

        IniziaFaseGarze();
    }

    public void CameraTransitionCompleted()
    {
        cameraTransitionFinished = true;
        Debug.Log("Transizione telecamera completata. Trascinamento garze abilitato.");
    }

    private void IniziaFaseGarze()
    {
        cameraTransitionFinished = false;
        isSnapped = false;
        
        if (tavoloCorrente == null || tavoloCorrente.oggettoCorrente == null)
        {
            Debug.LogError("Tavolo di lavoro o oggetto corrente non validi.");
            return;
        }

        mosaicoCorrente = tavoloCorrente.oggettoCorrente as MosaicoSO;
        if (mosaicoCorrente == null)
        {
            Debug.LogError("L'oggetto sul tavolo di lavoro non è un mosaico.");
            return;
        }

        GameObject prefabDaSpawnare = usaAerolam ? mosaicoCorrente.prefabAerolam : mosaicoCorrente.prefabGarza;
        if (prefabDaSpawnare == null)
        {
            Debug.LogError("Prefab da istanziare non configurato nel mosaico.");
            return;
        }

        if (puntoSpawnGarza == null || puntoTargetGarza == null)
        {
            Debug.LogError("Punti di spawn o target della garza non assegnati nell'Inspector.");
            return;
        }

        TerminaFaseGarze();

        garzaIstanza = Instantiate(prefabDaSpawnare, puntoSpawnGarza);
        garzaIstanza.transform.localPosition = Vector3.zero;
        garzaIstanza.transform.localRotation = Quaternion.identity;

        // Calcolo e assegnazione della scala locale per compensare le dimensioni globali del punto di spawn
        Vector3 parentSpawnScale = puntoSpawnGarza.lossyScale;
        Vector3 targetPrefabScale = prefabDaSpawnare.transform.localScale;
        garzaIstanza.transform.localScale = new Vector3(
            parentSpawnScale.x != 0 ? targetPrefabScale.x / parentSpawnScale.x : targetPrefabScale.x,
            parentSpawnScale.y != 0 ? targetPrefabScale.y / parentSpawnScale.y : targetPrefabScale.y,
            parentSpawnScale.z != 0 ? targetPrefabScale.z / parentSpawnScale.z : targetPrefabScale.z
        );

        // Assegnazione di un BoxCollider temporaneo se non presente, necessario per il trascinamento
        garzaCollider = garzaIstanza.GetComponent<Collider>();
        if (garzaCollider == null)
        {
            garzaCollider = garzaIstanza.AddComponent<BoxCollider>();
            Debug.Log("Aggiunto BoxCollider temporaneo alla garza per il trascinamento.");
        }
        garzaCollider.enabled = true;

        isGarzeActive = true;
        Debug.Log($"Garza {garzaIstanza.name} istanziata nel punto di spawn.");
    }

    private void TerminaFaseGarze()
    {
        isGarzeActive = false;
        isDragging = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        if (garzaIstanza != null)
        {
            if (!isSnapped)
            {
                Destroy(garzaIstanza);
                garzaIstanza = null;
            }
        }
    }

    private void Update()
    {
        if (!isGarzeActive || !cameraTransitionFinished || isSnapped) return;

        GestisciDragAndDrop();
    }

    private void GestisciDragAndDrop()
    {
        if (cameraRestauro == null || Mouse.current == null) return;

        bool isLeftPressed = Mouse.current.leftButton.isPressed;
        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (isLeftPressed)
        {
            if (!isDragging)
            {
                Ray ray = cameraRestauro.ScreenPointToRay(mousePos);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                {
                    if (hit.collider.gameObject == garzaIstanza || hit.collider.transform.IsChildOf(garzaIstanza.transform))
                    {
                        isDragging = true;
                        positionBeforeDrag = garzaIstanza.transform.position;
                        rotationBeforeDrag = garzaIstanza.transform.localRotation;
                        scaleBeforeDrag = garzaIstanza.transform.localScale;

                        // Calcolo della profondità spaziale rispetto al target per allineare il piano di trascinamento
                        float targetDepth = Vector3.Dot(puntoTargetGarza.position - cameraRestauro.transform.position, cameraRestauro.transform.forward);
                        Vector3 planePoint = cameraRestauro.transform.position + cameraRestauro.transform.forward * (targetDepth - 0.02f);
                        dragPlane = new Plane(-cameraRestauro.transform.forward, planePoint);

                        // Determinazione del punto di intersezione iniziale sul piano di drag
                        Ray rayStart = cameraRestauro.ScreenPointToRay(mousePos);
                        if (dragPlane.Raycast(rayStart, out float enterStart))
                        {
                            Vector3 pointOnPlane = rayStart.GetPoint(enterStart);
                            garzaIstanza.transform.position = pointOnPlane;
                        }
                        dragOffset = Vector3.zero;

                        if (cursorDragTexture != null)
                        {
                            Cursor.SetCursor(cursorDragTexture, cursorDragHotspot, CursorMode.Auto);
                        }

                        Debug.Log("Inizio trascinamento della garza.");
                    }
                }
            }
            else
            {
                Ray ray = cameraRestauro.ScreenPointToRay(mousePos);
                if (dragPlane.Raycast(ray, out float enter))
                {
                    Vector3 targetPos = ray.GetPoint(enter) + dragOffset;
                    garzaIstanza.transform.position = targetPos;
                    // Allineamento della rotazione dell'oggetto a quella della destinazione durante il drag
                    garzaIstanza.transform.rotation = puntoTargetGarza.rotation;

                    VerificaSnap();
                }
            }
        }
        else
        {
            if (isDragging)
            {
                isDragging = false;
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

                if (!isSnapped)
                {
                    // Riposizionamento dell'oggetto al punto di spawn originale in caso di rilascio non valido
                    garzaIstanza.transform.position = positionBeforeDrag;
                    garzaIstanza.transform.localRotation = rotationBeforeDrag;
                    garzaIstanza.transform.localScale = scaleBeforeDrag;
                    Debug.Log("Rilascio della garza senza snap, riposizionamento al punto di spawn.");
                }
            }
        }
    }

    private void VerificaSnap()
    {
        if (garzaIstanza == null || puntoTargetGarza == null) return;

        float dist = Vector3.Distance(garzaIstanza.transform.position, puntoTargetGarza.position);
        float angle = Quaternion.Angle(garzaIstanza.transform.rotation, puntoTargetGarza.rotation);

        if (dist <= snapDistance && angle <= snapAngle)
        {
            isSnapped = true;
            isDragging = false;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

            garzaIstanza.transform.SetParent(puntoTargetGarza, false);
            garzaIstanza.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            garzaIstanza.transform.localRotation = Quaternion.identity;

            // Calcolo e assegnazione della scala locale corretta in base alle dimensioni del target per evitare distorsioni
            Vector3 parentTargetScale = puntoTargetGarza.lossyScale;
            GameObject prefabOriginale = usaAerolam ? mosaicoCorrente.prefabAerolam : mosaicoCorrente.prefabGarza;
            Vector3 targetPrefabScale = prefabOriginale != null ? prefabOriginale.transform.localScale : Vector3.one;
            garzaIstanza.transform.localScale = new Vector3(
                parentTargetScale.x != 0 ? targetPrefabScale.x / parentTargetScale.x : targetPrefabScale.x,
                parentTargetScale.y != 0 ? targetPrefabScale.y / parentTargetScale.y : targetPrefabScale.y,
                parentTargetScale.z != 0 ? targetPrefabScale.z / parentTargetScale.z : targetPrefabScale.z
            );

            // Associazione gerarchica dell'oggetto al mosaico mantenendo invariata la posizione globale
            if (tavoloCorrente != null && tavoloCorrente.vaschettaGameObject != null)
            {
                garzaIstanza.transform.SetParent(tavoloCorrente.vaschettaGameObject.transform, true);
                Debug.Log("Garza associata correttamente al mosaico.");
            }

            if (garzaCollider != null)
            {
                garzaCollider.enabled = false;
            }

            Debug.Log("Garza posizionata correttamente sul mosaico.");
            onGarzaApplicata?.Invoke();

            SoundEffect effectToPlay = usaAerolam ? aerolamSound : gauzeSound;
            if (AudioManager.Instance == null)
            {
                Debug.LogWarning($"[GestoreGarze] '{gameObject.name}': AudioManager.Instance è null! Impossibile riprodurre il suono garza.");
            }
            else if (effectToPlay.clip == null)
            {
                string nomeClip = usaAerolam ? "aerolamSound" : "gauzeSound";
                Debug.LogWarning($"[GestoreGarze] '{gameObject.name}': {nomeClip}.clip non è assegnato nell'Inspector. Nessun suono verrà riprodotto.");
            }
            else
            {
                AudioManager.Instance.Play3D(effectToPlay, garzaIstanza.transform.position);
            }

            StartCoroutine(SequenzaCompletamento());
        }
    }

    private IEnumerator SequenzaCompletamento()
    {
        if (tavoloCorrente != null && tavoloCorrente.vaschettaGameObject != null)
        {
            yield return RestorationUtils.VibraOggetto(tavoloCorrente.vaschettaGameObject, 0.6f, 0.012f);
        }
        yield return new WaitForSeconds(0.4f);

        // Disattivazione della visualizzazione della colla nei renderer del mosaico usando MaterialPropertyBlock
        if (tavoloCorrente != null && tavoloCorrente.vaschettaGameObject != null)
        {
            int idMostraColla = Shader.PropertyToID("_mostraColla");
            foreach (var r in tavoloCorrente.vaschettaGameObject.GetComponentsInChildren<Renderer>())
            {
                bool daModificare = false;
                foreach (var mat in r.sharedMaterials)
                {
                    if (mat != null && mat.HasProperty(idMostraColla))
                    {
                        daModificare = true;
                        break;
                    }
                }

                if (daModificare)
                {
                    r.GetPropertyBlock(propBlock);
                    propBlock.SetFloat(idMostraColla, 0f);
                    r.SetPropertyBlock(propBlock);
                }
            }

            if (tavoloCorrente.collaTextureMosaico != null)
            {
                Destroy(tavoloCorrente.collaTextureMosaico);
                tavoloCorrente.collaTextureMosaico = null;
                Debug.Log("Texture colla del mosaico rimossa con successo al completamento.");
            }
        }

        // Avanza alla fase successiva o completa
        if (tavoloCorrente != null && faseSuccessiva != null)
        {
            Debug.Log($"Avanzamento alla fase successiva: {faseSuccessiva.name}.");
            tavoloCorrente.AvanzaFase(faseSuccessiva);
        }
        else
        {
            Debug.Log("Nessuna fase successiva configurata. Il completamento del restauro è ora gestito dal RestoreManager tramite l'evento OnPhaseCompleted.");
        }
        OnPhaseCompleted?.Invoke(faseSuccessiva != null);
    }
}
