using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class GestoreAssemblaggio : MonoBehaviour
{
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
                Debug.Log($"[GestoreAssemblaggio] Rilevata fase corretta '{triggerAssemblaggio.name}' all'abilitazione!");
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
        Debug.Log($"[GestoreAssemblaggio] OnFaseCambiata: fase cambiata a = {(fase != null ? fase.name : "null")}, fase attesa = {(triggerAssemblaggio != null ? triggerAssemblaggio.name : "null")}");
        if (fase != triggerAssemblaggio)
        {
            TerminaAssemblaggio();
            return;
        }

        IniziaAssemblaggio();
    }

    private void IniziaAssemblaggio()
    {
        Debug.Log($"[GestoreAssemblaggio] IniziaAssemblaggio - vaschettaCorrente: {(tavoloCorrente.vaschettaCorrente != null ? tavoloCorrente.vaschettaCorrente.name : "null")}, vaschettaGameObject: {(tavoloCorrente.vaschettaGameObject != null ? tavoloCorrente.vaschettaGameObject.name : "null")}");
        cameraTransitionFinished = false;
        if (tavoloCorrente.vaschettaCorrente == null || tavoloCorrente.vaschettaGameObject == null) return;

        if (puntoSpawnAnfora == null)
        {
            Debug.LogError("[GestoreAssemblaggio] puntoSpawnAnfora non assegnato nell'Inspector!");
            return;
        }

        TerminaAssemblaggio();

        // 1. Spawna l'anfora intera (ghost)
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

        // 2. Raccogli e configura i pezzi presenti nella vaschetta fisica
        GameObject prefabPezzi = tavoloCorrente.vaschettaCorrente.prefabPezzi;
        if (prefabPezzi == null)
        {
            prefabPezzi = tavoloCorrente.vaschettaCorrente.prefabAnfora;
            Debug.Log($"[GestoreAssemblaggio] prefabPezzi era NULL su '{tavoloCorrente.vaschettaCorrente.name}'. Uso come fallback prefabAnfora: '{(prefabPezzi != null ? prefabPezzi.name : "null")}'");
        }
        GameObject vaschettaGO = tavoloCorrente.vaschettaGameObject;

        if (prefabPezzi != null && vaschettaGO != null)
        {
            ConfigurazioneVaschetta config = vaschettaGO.GetComponent<ConfigurazioneVaschetta>();
            if (config == null)
            {
                Debug.LogError($"[GestoreAssemblaggio] Manca il componente ConfigurazioneVaschetta sul GameObject della vaschetta '{vaschettaGO.name}'!");
                return;
            }

            Debug.Log($"[GestoreAssemblaggio] ConfigurazioneVaschetta trovata. Pezzi ordinati nella vaschetta da posizionare: {config.pezziOrdinati.Count}");

            foreach (var goPezzo in config.pezziOrdinati)
            {
                if (goPezzo == null) continue;

                Transform targetTransform = TrovaFiglioNelPrefab(prefabPezzi, goPezzo.name);
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
                        Debug.Log($"[GestoreAssemblaggio] Aggiunto MeshCollider a '{goPezzo.name}'");
                    }
                    pezzo.collider = col;

                    listaPezzi.Add(pezzo);
                    Debug.Log($"[GestoreAssemblaggio] Pezzo '{goPezzo.name}' caricato con successo. Posizione target locale: {pezzo.originalLocalPos}");
                }
                else
                {
                    Debug.LogWarning($"[GestoreAssemblaggio] Non è stato possibile trovare il pezzo '{goPezzo.name}' nel prefabPezzi/prefabAnfora '{prefabPezzi.name}'.");
                }
            }
        }
        else
        {
            Debug.LogError($"[GestoreAssemblaggio] Impossibile configurare i pezzi: prefabPezzi/prefabAnfora o vaschettaGO è NULL! prefabPezzi: {(prefabPezzi != null ? prefabPezzi.name : "null")}, vaschettaGO: {(vaschettaGO != null ? vaschettaGO.name : "null")}");
        }

        isAssemblaggioActive = true;
    }

    private void TerminaAssemblaggio()
    {
        isAssemblaggioActive = false;
        Debug.Log("[GestoreAssemblaggio] TerminaAssemblaggio - Pulizia e disattivazione.");
        if (pezzoTrascinato != null)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            pezzoTrascinato = null;
        }

        if (ghostAnfora != null)
        {
            if (tavoloCorrente != null && tavoloCorrente.anforaAssemblata == ghostAnfora)
            {
                Debug.Log("[GestoreAssemblaggio] TerminaAssemblaggio - ghostAnfora è registrato come anforaAssemblata persistente. NON lo distruggo.");
                ghostAnfora = null; // Rilascia il riferimento locale senza distruggerlo
            }
            else
            {
                Debug.Log("[GestoreAssemblaggio] TerminaAssemblaggio - Distruggo ghostAnfora locale.");
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
                    Debug.Log($"[GestoreAssemblaggio] Click rilevato su '{hit.collider.gameObject.name}' (Parent: '{hit.collider.transform.parent?.name ?? "Nessuno"}')");
                    bool trovato = false;
                    foreach (var pezzo in listaPezzi)
                    {
                        if (!pezzo.isSnapped && (hit.collider.gameObject == pezzo.gameObject || hit.collider.transform.IsChildOf(pezzo.gameObject.transform)))
                        {
                            pezzoTrascinato = pezzo;
                            
                            // Salva la posizione, rotazione e scala iniziale per poter fare il fallback in caso di mancato snap
                            positionBeforeDrag = pezzoTrascinato.gameObject.transform.position;
                            rotationBeforeDrag = pezzoTrascinato.gameObject.transform.localRotation;
                            scaleBeforeDrag = pezzoTrascinato.gameObject.transform.localScale;

                            // Calcola e applica la scala locale temporanea per il drag per evitare distorsioni causate da scale dei parent differenti
                            if (ghostAnfora != null)
                            {
                                Vector3 parentLossyScale = pezzoTrascinato.gameObject.transform.parent != null ? pezzoTrascinato.gameObject.transform.parent.lossyScale : Vector3.one;
                                Vector3 targetWorldScale = Vector3.Scale(ghostAnfora.transform.lossyScale, pezzoTrascinato.originalLocalScale);
                                pezzoTrascinato.gameObject.transform.localScale = new Vector3(
                                    parentLossyScale.x != 0 ? targetWorldScale.x / parentLossyScale.x : targetWorldScale.x,
                                    parentLossyScale.y != 0 ? targetWorldScale.y / parentLossyScale.y : targetWorldScale.y,
                                    parentLossyScale.z != 0 ? targetWorldScale.z / parentLossyScale.z : targetWorldScale.z
                                );
                            }

                            // Allinea la rotazione del pezzo nel mondo a quella target finale definita dal ghost
                            pezzoTrascinato.gameObject.transform.rotation = ghostAnfora.transform.rotation * pezzoTrascinato.originalLocalRot;
                            
                            // Calcola la posizione target specifica del pezzo nel mondo
                            Vector3 targetWorldPos = ghostAnfora.transform.TransformPoint(pezzoTrascinato.originalLocalPos);

                            // Calcola la profondità di questo target specifico rispetto alla camera
                            float targetDepth = Vector3.Dot(targetWorldPos - cameraRestauro.transform.position, cameraRestauro.transform.forward);
                            
                            // Crea il piano di drag perpendicolare alla camera, posizionato leggermente davanti al target specifico del pezzo (es. 0.05 unità più vicino)
                            Vector3 planePoint = cameraRestauro.transform.position + cameraRestauro.transform.forward * (targetDepth - 0.05f);
                            dragPlane = new Plane(-cameraRestauro.transform.forward, planePoint);

                            // Proietta la posizione iniziale del mouse sul piano di drag e sposta l'oggetto lì,
                            // centrando perfettamente il pivot del pezzo sotto il cursore del mouse
                            Ray rayStart = cameraRestauro.ScreenPointToRay(mousePos);
                            if (dragPlane.Raycast(rayStart, out float enterStart))
                            {
                                Vector3 pointOnPlane = rayStart.GetPoint(enterStart);
                                pezzoTrascinato.gameObject.transform.position = pointOnPlane;
                            }

                            // Imposta l'offset a zero in modo che l'oggetto rimanga perfettamente centrato rispetto al mouse
                            dragOffset = Vector3.zero;

                            Debug.Log($"[DEBUG SCALE] Dragging piece '{pezzoTrascinato.gameObject.name}', lossyScale = {pezzoTrascinato.gameObject.transform.lossyScale}");
                            Debug.Log($"[GestoreAssemblaggio] Inizio drag del pezzo '{pezzoTrascinato.gameObject.name}' a profondità target: {targetDepth - 0.05f}");
                            
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
                        Debug.LogWarning($"[GestoreAssemblaggio] Il collider colpito '{hit.collider.gameObject.name}' non corrisponde a nessun pezzo da assemblare (pezzi attesi: {listaPezzi.Count}).");
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

                    // Mantiene la rotazione target corretta durante tutto il movimento
                    pezzoTrascinato.gameObject.transform.rotation = ghostAnfora.transform.rotation * pezzoTrascinato.originalLocalRot;

                    VerificaSnap(pezzoTrascinato);
                }
            }
        }
        else
        {
            if (pezzoTrascinato != null)
            {
                // Se non è snapped, ritorna alla sua posizione originaria nella vaschetta
                if (!pezzoTrascinato.isSnapped)
                {
                    pezzoTrascinato.gameObject.transform.position = positionBeforeDrag;
                    pezzoTrascinato.gameObject.transform.localRotation = rotationBeforeDrag;
                    pezzoTrascinato.gameObject.transform.localScale = scaleBeforeDrag;
                    Debug.Log($"[GestoreAssemblaggio] Rilascio senza snap. Ritorno di '{pezzoTrascinato.gameObject.name}' nella vaschetta.");
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

        // Se il pezzo è quello trascinato, la sua rotazione è bloccata a identity, quindi lo snap si basa solo sulla distanza spaziale.
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

            Debug.Log($"[DEBUG SCALE] Snapped piece '{pezzo.gameObject.name}', lossyScale = {pezzo.gameObject.transform.lossyScale}");

            if (pezzo.collider != null)
                pezzo.collider.enabled = false;

            pezzoTrascinato = null;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            Debug.Log($"[GestoreAssemblaggio] Pezzo '{pezzo.gameObject.name}' posizionato correttamente!");

            // ...e verifica se tutti i pezzi sono posizionati
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
        isAssemblaggioActive = false;
        Debug.Log("[GestoreAssemblaggio] === [CompletaFaseAssemblaggio] Tutti i pezzi posizionati con successo! ===");
        onAssemblaggioCompletato?.Invoke();

        if (ghostAnfora != null)
        {
            Debug.Log($"[GestoreAssemblaggio] Reparenting di ghostAnfora '{ghostAnfora.name}' a NULL per mantenerlo attivo durante la transizione di fase.");
            ghostAnfora.transform.SetParent(null, true);
            
            if (tavoloCorrente != null)
            {
                tavoloCorrente.anforaAssemblata = ghostAnfora;
                Debug.Log($"[GestoreAssemblaggio] Salvato riferimento di ghostAnfora in tavoloCorrente.anforaAssemblata. Nome: '{tavoloCorrente.anforaAssemblata.name}'");
            }
        }
        else
        {
            Debug.LogError("[GestoreAssemblaggio] CompletaFaseAssemblaggio - ghostAnfora è NULL! Impossibile persistere l'anfora.");
        }

        if (tavoloCorrente != null && faseSuccessiva != null)
        {
            Debug.Log($"[GestoreAssemblaggio] Chiamata AvanzaFase per passare a: '{faseSuccessiva.name}'");
            tavoloCorrente.AvanzaFase(faseSuccessiva);
        }
        else
        {
            Debug.LogError($"[GestoreAssemblaggio] Impostazioni mancanti per avanzare alla fase successiva! tavoloCorrente: {tavoloCorrente != null}, faseSuccessiva: {faseSuccessiva != null}");
        }
    }

    public void CameraTransitionCompleted()
    {
        cameraTransitionFinished = true;
        Debug.Log("[GestoreAssemblaggio] Camera transition completed. Drag and drop is now enabled.");
    }

    private Transform TrovaFiglioNelPrefab(GameObject prefab, string nomeFiglio)
    {
        return CercaFiglioRicorsivo(prefab.transform, nomeFiglio);
    }

    private Transform CercaFiglioRicorsivo(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = CercaFiglioRicorsivo(parent.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }
}
