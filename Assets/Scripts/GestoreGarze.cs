using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections;

public class GestoreGarze : MonoBehaviour
{
    [Header("Tavolo")]
    [SerializeField] private TavoloSO tavoloCorrente;
    [SerializeField] private FaseRestauroSO triggerGarze; // La fase associata a questo script (es. FaseGarze)
    [SerializeField] private Camera cameraRestauro;
    [SerializeField] private RestoreManager restoreManager;
    [SerializeField] private FaseRestauroSO faseSuccessiva; // Fase successiva (facoltativa)
    [SerializeField] private bool usaAerolam = false; // Se true, spawna il prefabAerolam invece del prefabGarza

    [Header("Punti di Spawn e Target")]
    [SerializeField] private Transform puntoSpawnGarza;  // Dove la garza viene spawnata per essere presa
    [SerializeField] private Transform puntoTargetGarza; // Dove la garza deve essere posizionata sul mosaico

    [Header("Parametri di Snap")]
    [SerializeField] private float snapDistance = 0.5f;
    [SerializeField] private float snapAngle = 15f;

    [Header("Grafica Cursore")]
    [SerializeField] private Texture2D cursorDragTexture;
    [SerializeField] private Vector2 cursorDragHotspot = Vector2.zero;

    [Header("Eventi")]
    [SerializeField] private UnityEvent onGarzaApplicata;

    private MosaicoSO mosaicoCorrente;
    private GameObject garzaIstanza;
    private bool isGarzeActive = false;
    private bool cameraTransitionFinished = false;

    // Stato Drag
    private bool isDragging = false;
    private Plane dragPlane;
    private Vector3 dragOffset;
    private Vector3 positionBeforeDrag;
    private Quaternion rotationBeforeDrag;
    private Vector3 scaleBeforeDrag;
    private Collider garzaCollider;
    private bool isSnapped = false;

    private void OnEnable()
    {
        if (tavoloCorrente != null)
        {
            tavoloCorrente.OnFaseCambiata += OnFaseCambiata;
            // Se siamo già nella fase corretta all'abilitazione
            if (tavoloCorrente.faseCorrente == triggerGarze)
            {
                Debug.Log("[GestoreGarze] Rilevata fase corretta 'FaseGarze' all'abilitazione!");
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
        Debug.Log($"[GestoreGarze] OnFaseCambiata: fase cambiata a = {(fase != null ? fase.name : "null")}, fase attesa = {(triggerGarze != null ? triggerGarze.name : "null")}");
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
        Debug.Log("[GestoreGarze] Camera transition completed. Gauze drag and drop is now enabled.");
    }

    private void IniziaFaseGarze()
    {
        cameraTransitionFinished = false;
        isSnapped = false;
        
        if (tavoloCorrente == null || tavoloCorrente.oggettoCorrente == null)
        {
            Debug.LogError("[GestoreGarze] tavoloCorrente o oggettoCorrente è nullo!");
            return;
        }

        mosaicoCorrente = tavoloCorrente.oggettoCorrente as MosaicoSO;
        if (mosaicoCorrente == null)
        {
            Debug.LogError("[GestoreGarze] L'oggetto corrente sul tavolo non è un MosaicoSO!");
            return;
        }

        GameObject prefabDaSpawnare = usaAerolam ? mosaicoCorrente.prefabAerolam : mosaicoCorrente.prefabGarza;
        if (prefabDaSpawnare == null)
        {
            Debug.LogError($"[GestoreGarze] Prefab da spawnare (usaAerolam: {usaAerolam}) non assegnato nel MosaicoSO '{mosaicoCorrente.name}'!");
            return;
        }

        if (puntoSpawnGarza == null || puntoTargetGarza == null)
        {
            Debug.LogError("[GestoreGarze] puntoSpawnGarza o puntoTargetGarza non assegnati nell'Inspector!");
            return;
        }

        TerminaFaseGarze(); // Sicurezza

        // Spawna la garza/aerolam al punto di spawn
        garzaIstanza = Instantiate(prefabDaSpawnare, puntoSpawnGarza);
        garzaIstanza.transform.localPosition = Vector3.zero;
        garzaIstanza.transform.localRotation = Quaternion.identity;

        // Mantieni le proporzioni del prefab tenendo conto della scala del parent puntoSpawnGarza
        Vector3 parentSpawnScale = puntoSpawnGarza.lossyScale;
        Vector3 targetPrefabScale = prefabDaSpawnare.transform.localScale;
        garzaIstanza.transform.localScale = new Vector3(
            parentSpawnScale.x != 0 ? targetPrefabScale.x / parentSpawnScale.x : targetPrefabScale.x,
            parentSpawnScale.y != 0 ? targetPrefabScale.y / parentSpawnScale.y : targetPrefabScale.y,
            parentSpawnScale.z != 0 ? targetPrefabScale.z / parentSpawnScale.z : targetPrefabScale.z
        );

        // Assicurati che abbia un collider per il drag
        garzaCollider = garzaIstanza.GetComponent<Collider>();
        if (garzaCollider == null)
        {
            garzaCollider = garzaIstanza.AddComponent<BoxCollider>();
            Debug.Log("[GestoreGarze] Aggiunto BoxCollider temporaneo alla garza per il drag.");
        }
        garzaCollider.enabled = true;

        isGarzeActive = true;
        Debug.Log($"[GestoreGarze] Spawno la garza '{garzaIstanza.name}' nel puntoSpawnGarza.");
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
                // Inizia drag
                Ray ray = cameraRestauro.ScreenPointToRay(mousePos);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                {
                    if (hit.collider.gameObject == garzaIstanza || hit.collider.transform.IsChildOf(garzaIstanza.transform))
                    {
                        isDragging = true;
                        positionBeforeDrag = garzaIstanza.transform.position;
                        rotationBeforeDrag = garzaIstanza.transform.localRotation;
                        scaleBeforeDrag = garzaIstanza.transform.localScale;

                        // Calcola profondità rispetto al punto target
                        float targetDepth = Vector3.Dot(puntoTargetGarza.position - cameraRestauro.transform.position, cameraRestauro.transform.forward);
                        Vector3 planePoint = cameraRestauro.transform.position + cameraRestauro.transform.forward * (targetDepth - 0.02f);
                        dragPlane = new Plane(-cameraRestauro.transform.forward, planePoint);

                        // Calcola offset di trascinamento
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

                        Debug.Log("[GestoreGarze] Inizio drag della garza.");
                    }
                }
            }
            else
            {
                // Trascina
                Ray ray = cameraRestauro.ScreenPointToRay(mousePos);
                if (dragPlane.Raycast(ray, out float enter))
                {
                    Vector3 targetPos = ray.GetPoint(enter) + dragOffset;
                    garzaIstanza.transform.position = targetPos;
                    // Mantieni la rotazione allineata a quella del target
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
                    // Torna al punto di spawn
                    garzaIstanza.transform.position = positionBeforeDrag;
                    garzaIstanza.transform.localRotation = rotationBeforeDrag;
                    garzaIstanza.transform.localScale = scaleBeforeDrag;
                    Debug.Log("[GestoreGarze] Rilascio senza snap. Ritorno della garza al punto di spawn.");
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

            // Posiziona esattamente sul target
            garzaIstanza.transform.SetParent(puntoTargetGarza, false);
            garzaIstanza.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            garzaIstanza.transform.localRotation = Quaternion.identity;

            // Mantieni le proporzioni originali del prefab tenendo conto della scala di puntoTargetGarza
            Vector3 parentTargetScale = puntoTargetGarza.lossyScale;
            GameObject prefabOriginale = usaAerolam ? mosaicoCorrente.prefabAerolam : mosaicoCorrente.prefabGarza;
            Vector3 targetPrefabScale = prefabOriginale != null ? prefabOriginale.transform.localScale : Vector3.one;
            garzaIstanza.transform.localScale = new Vector3(
                parentTargetScale.x != 0 ? targetPrefabScale.x / parentTargetScale.x : targetPrefabScale.x,
                parentTargetScale.y != 0 ? targetPrefabScale.y / parentTargetScale.y : targetPrefabScale.y,
                parentTargetScale.z != 0 ? targetPrefabScale.z / parentTargetScale.z : targetPrefabScale.z
            );

            // Rendi la garza figlia del mosaico mantenendo la posizione globale corretta
            if (tavoloCorrente != null && tavoloCorrente.vaschettaGameObject != null)
            {
                garzaIstanza.transform.SetParent(tavoloCorrente.vaschettaGameObject.transform, true);
                Debug.Log($"[GestoreGarze] Garza imparentata con successo al mosaico: '{tavoloCorrente.vaschettaGameObject.name}'");
            }

            if (garzaCollider != null)
            {
                garzaCollider.enabled = false;
            }

            Debug.Log("[GestoreGarze] Garza posizionata con successo (Snap riuscito)!");
            onGarzaApplicata?.Invoke();

            StartCoroutine(SequenzaCompletamento());
        }
    }

    private IEnumerator SequenzaCompletamento()
    {
        yield return new WaitForSeconds(1.0f);

        // Nascondi la colla sui materiali del mosaico e pulisci la texture temporanea
        if (tavoloCorrente != null && tavoloCorrente.vaschettaGameObject != null)
        {
            int idMostraColla = Shader.PropertyToID("_mostraColla");
            foreach (var r in tavoloCorrente.vaschettaGameObject.GetComponentsInChildren<Renderer>())
            {
                Material[] mats = r.materials;
                bool modified = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null && mats[i].HasProperty(idMostraColla))
                    {
                        mats[i].SetFloat(idMostraColla, 0f);
                        modified = true;
                    }
                }
                if (modified)
                    r.materials = mats;
            }

            if (tavoloCorrente.collaTextureMosaico != null)
            {
                Destroy(tavoloCorrente.collaTextureMosaico);
                tavoloCorrente.collaTextureMosaico = null;
                Debug.Log("[GestoreGarze] collaTextureMosaico distrutta con successo al completamento della fase garze.");
            }
        }

        // Avanza alla fase successiva o completa
        if (tavoloCorrente != null && faseSuccessiva != null)
        {
            Debug.Log($"[GestoreGarze] Avanzo alla fase successiva: {faseSuccessiva.name}");
            tavoloCorrente.AvanzaFase(faseSuccessiva);
        }
        else if (restoreManager != null)
        {
            Debug.Log("[GestoreGarze] Nessuna fase successiva. Completo il restauro.");
            restoreManager.CompletaRestauro();
        }
    }
}
