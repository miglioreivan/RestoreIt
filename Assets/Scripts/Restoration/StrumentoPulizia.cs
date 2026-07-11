using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Vector2 = UnityEngine.Vector2;

/// <summary>
/// Minigioco di pulizia del fango basato sulla pittura di una texture di maschera a runtime.
/// Rileva l'interazione del mouse con raycast ed aggiorna la percentuale dei pixel visibili puliti.
/// </summary>
public class StrumentoPulizia : MonoBehaviour, IRestorationPhaseManager, IRestorationPhase
{
    /// <summary> Evento sollevato al completamento della fase di pulizia. </summary>
    public event System.Action<bool> OnPhaseCompleted;

    /// <summary> Grado di progressione corrente normalizzato (0.0f a 1.0f). </summary>
    public float Progression => progression;
    [Header("Impostazioni Telecamera e Raggio")]
    [SerializeField] private Camera cameraRestauro;
    [SerializeField] private LayerMask layerRestauro;

    [Header("Impostazioni Pennello/Spugna")]
    [SerializeField] private Texture2D mascheraSporcoOriginale;
    [SerializeField] private int rangePaintbrush = 30;
    [SerializeField] private Texture2D cursorTexture;
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;
    [SerializeField] private SoundEffect cleaningSound;
    private bool wasBrushingLastFrame = false;

    [Header("Progressione Restauro")]
    [SerializeField] private TavoloSO tavoloCorrente;
    [SerializeField] private FaseRestauroSO faseSuccessiva;
    [SerializeField] private float progression = 0f;
    [SerializeField] [Range(0.1f, 1f)] private float sogliaCompletamentoPulizia = 0.95f;
    [SerializeField] private UnityEvent<VaschettaSO> eventoPuliziaCompletata;

    [System.Serializable]
    public class UnityEventDatiRestauro : UnityEvent<DatiOggettoSO> { }
    [SerializeField] private UnityEventDatiRestauro eventoRestauroCompletato;



    private Texture2D textureInstance;
    private Color32[] texture32;

    private int totPixel = 0;
    private int textureWidth = 1024;
    private int textureHeight = 1024;

    private int pixelPainted = 0;
    private bool minigiocoFinito = true;

    private readonly int idMascheraSporco = Shader.PropertyToID("_MascheraSporco");
    private readonly int idMostraTerra   = Shader.PropertyToID("_mostraTerra");
    private readonly int idMostraColla   = Shader.PropertyToID("_mostraColla");
    private readonly int idMostraPittura = Shader.PropertyToID("_mostraPittura");

    private MaterialPropertyBlock propBlock;

    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();

        if (layerRestauro.value == 0)
        {
            layerRestauro = LayerMask.GetMask("Restauro");
            RestoreLogger.Log($"Layer di restauro vuoto. Impostato automaticamente a Restauro con valore {layerRestauro.value}.");
        }

        if (cameraRestauro == null)
        {
            cameraRestauro = GetComponentInChildren<Camera>(true);
            if (cameraRestauro == null)
            {
                cameraRestauro = GetComponentInParent<Camera>(true);
            }
        }

        InizializzaMascheraSporco();
    }

    private void OnDestroy()
    {
        if (textureInstance != null)
        {
            Destroy(textureInstance);
            textureInstance = null;
        }
    }

    private void InizializzaMascheraSporco()
    {
        if (mascheraSporcoOriginale != null)
        {
            // Se esiste già una textureInstance precedente (es. da un restauro precedente dello stesso banco), la distrugge per evitare leak
            if (textureInstance != null)
            {
                Destroy(textureInstance);
                textureInstance = null;
            }

            // Crea una copia virtuale non compressa a runtime usando RenderTexture e Graphics.Blit
            // per generare una copia perfettamente leggibile e scrivibile.
            textureInstance = RestorationUtils.CopiaTextureInRGBA32(mascheraSporcoOriginale);
            
            if (textureInstance != null)
            {
                textureWidth = textureInstance.width;
                textureHeight = textureInstance.height;
                texture32 = textureInstance.GetPixels32();
                RestoreLogger.Log("Nuova copia RGBA32 della maschera dello sporco creata correttamente.");
            }
            else
            {
                Debug.LogError("Impossibile creare una copia leggibile della maschera dello sporco.");
            }
        }
    }

    public void CameraTransitionCompleted()
    {
        RestoreLogger.Log($"Avvio del minigioco di pulizia su {gameObject.name}.");
        CountVisiblePixel();
        SetMouseCursor();
        IniziaMinigame();
    }

    public void CountVisiblePixel()
    {
        RestoreLogger.Log("Calcolo dei pixel visibili avviato.");
        InizializzaMascheraSporco();
        ConfiguraCollidersMosaico(true);
        SincronizzaRenderers();

        totPixel = 0;
        if (texture32 == null)
        {
            Debug.LogError("I dati dei pixel della texture dello sporco non sono validi.");
            return;
        }

        bool[] pixelRaggiungibili = new bool[texture32.Length];

        int risoluzioneScansione = 512;
        int raggioPrecisioneTelecamera = Mathf.CeilToInt((float)Mathf.Max(textureWidth, textureHeight) / risoluzioneScansione);

        int raycastLanciati = 0;
        int raycastColpiti = 0;

        for (int x = 0; x <= risoluzioneScansione; x++)
        {
            for (int y = 0; y <= risoluzioneScansione; y++)
            {
                float viewportX = (float)x / risoluzioneScansione;
                float viewportY = (float)y / risoluzioneScansione;

                Ray ray = cameraRestauro.ViewportPointToRay(new UnityEngine.Vector3(viewportX, viewportY, 0));
                raycastLanciati++;

                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerRestauro))
                {
                    raycastColpiti++;
                    if (hit.collider is MeshCollider)
                    {
                        if (hit.collider.TryGetComponent(out Renderer rend))
                        {
                            Material[] sharedMats = rend.sharedMaterials;
                            int submeshIndex = GetSubMeshIndex(hit.collider as MeshCollider, hit.triangleIndex);
                            if (submeshIndex >= 0 && submeshIndex < sharedMats.Length)
                            {
                                Material hitMat = sharedMats[submeshIndex];
                                if (hitMat != null)
                                {
                                    bool daModificare = hitMat.HasProperty(idMascheraSporco);
                                    if (daModificare)
                                    {
                                        Vector2 uv = RestorationUtils.WrapUV(hit.textureCoord);
                                        int pixelX = (int)(uv.x * textureWidth);
                                        int pixelY = (int)(uv.y * textureHeight);
                                        SegnaAreaVisibile(pixelX, pixelY, pixelRaggiungibili, raggioPrecisioneTelecamera);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        for (int i = 0; i < texture32.Length; i++)
        {
            Color32 p = texture32[i];
            if (pixelRaggiungibili[i] && (p.r > 12 || p.g > 12 || p.b > 12))
                totPixel++;
        }

        RestoreLogger.Log($"Scansione pixel completata. Pixel sporchi visibili rilevati: {totPixel}.");
    }
    private void SincronizzaRenderers()
    {
        RestoreLogger.Log("Sincronizzazione dei renderer avviata.");
        if (tavoloCorrente == null)
        {
            Debug.LogError("Tavolo di lavoro non valido durante la sincronizzazione.");
            return;
        }
        if (tavoloCorrente.vaschettaGameObject == null)
        {
            Debug.LogError("Oggetto della vaschetta non valido durante la sincronizzazione.");
            return;
        }

        GameObject activeObj = tavoloCorrente.vaschettaGameObject;
        RestoreLogger.Log($"Sincronizzazione dell'oggetto attivo {activeObj.name}.");

        List<Renderer> list = new List<Renderer>();
        list.AddRange(activeObj.GetComponentsInChildren<Renderer>(true));

        Transform current = activeObj.transform.parent;
        while (current != null)
        {
            Transform planeT = current.Find("Plane");
            if (planeT != null)
            {
                Renderer r = planeT.GetComponent<Renderer>();
                if (r != null)
                {
                    list.Add(r);
                    RestoreLogger.Log($"Trovato Plane del tavolo {current.name} e aggiunto alla sincronizzazione.");
                    break;
                }
            }
            if (current.parent == null) break;
            current = current.parent;
        }

        RestoreLogger.Log($"Trovati {list.Count} renderer da elaborare per la pulizia.");
        int renderersModificati = 0;

        foreach (Renderer rend in list)
        {
            int layerDelRenderer = rend.gameObject.layer;
            int layerMaskValue = layerRestauro.value;
            bool layerMatches = (layerMaskValue & (1 << layerDelRenderer)) != 0;

            if (!layerMatches) continue;

            bool daModificare = false;
            foreach (var mat in rend.sharedMaterials)
            {
                if (mat != null && mat.HasProperty(idMascheraSporco))
                {
                    daModificare = true;
                    break;
                }
            }

            if (daModificare)
            {
                rend.GetPropertyBlock(propBlock);
                if (textureInstance != null)
                {
                    propBlock.SetTexture(idMascheraSporco, textureInstance);
                }
                propBlock.SetFloat(idMostraTerra, 1f);
                propBlock.SetFloat(idMostraColla, 0f);
                propBlock.SetFloat(idMostraPittura, 0f);
                rend.SetPropertyBlock(propBlock);
                renderersModificati++;
            }
        }

        RestoreLogger.Log($"Sincronizzazione completata. Renderer modificati: {renderersModificati}.");
    }

    private void ImpostaFaseFinePuliziaSuMateriali()
    {
        RestoreLogger.Log("Applicazione dei materiali di fine pulizia.");
        
        if (tavoloCorrente == null || tavoloCorrente.vaschettaGameObject == null)
        {
            Debug.LogError("Tavolo di lavoro o oggetto della vaschetta non validi per la fine della pulizia.");
            return;
        }

        GameObject activeObj = tavoloCorrente.vaschettaGameObject;
        RestoreLogger.Log($"Fine pulizia impostata su {activeObj.name}.");

        List<Renderer> list = new List<Renderer>();
        list.AddRange(activeObj.GetComponentsInChildren<Renderer>(true));

        Transform current = activeObj.transform.parent;
        while (current != null)
        {
            Transform planeT = current.Find("Plane");
            if (planeT != null)
            {
                Renderer r = planeT.GetComponent<Renderer>();
                if (r != null)
                {
                    list.Add(r);
                    RestoreLogger.Log($"Fine pulizia applicata a Plane di {current.name}.");
                    break;
                }
            }
            if (current.parent == null) break;
            current = current.parent;
        }

        int modificati = 0;

        foreach (Renderer rend in list)
        {
            int layerDelRenderer = rend.gameObject.layer;
            int layerMaskValue = layerRestauro.value;
            bool layerMatches = (layerMaskValue & (1 << layerDelRenderer)) != 0;

            if (!layerMatches) continue;

            bool daModificare = false;
            foreach (var mat in rend.sharedMaterials)
            {
                if (mat != null && mat.HasProperty(idMascheraSporco))
                {
                    daModificare = true;
                    break;
                }
            }

            if (daModificare)
            {
                rend.GetPropertyBlock(propBlock);
                propBlock.SetFloat(idMostraTerra, 0f);
                propBlock.SetFloat(idMostraColla, 1f);
                propBlock.SetFloat(idMostraPittura, 1f);
                rend.SetPropertyBlock(propBlock);
                modificati++;
            }
        }
        RestoreLogger.Log($"Materiali di fine pulizia applicati su {modificati} renderer.");
    }

    private void SegnaAreaVisibile(int centroX, int centroY, bool[] mappa, int raggioCalcolo)
    {
        for (int x = -raggioCalcolo; x <= raggioCalcolo; x++)
        {
            for (int y = -raggioCalcolo; y <= raggioCalcolo; y++)
            {
                int targetX = centroX + x;
                int targetY = centroY + y;

                if (targetX >= 0 && targetX < textureWidth && targetY >= 0 && targetY < textureHeight)
                    mappa[targetY * textureWidth + targetX] = true;
            }
        }
    }

    private void Update()
    {
        if (minigiocoFinito)
        {
            if (wasBrushingLastFrame)
            {
                StopCleaningSound();
            }
            return;
        }
        if (Mouse.current == null) return;

        bool isPressing = Mouse.current.leftButton.isPressed;
        bool hasHit = false;

        if (isPressing)
        {
            Vector2 posizioneMouse = Mouse.current.position.ReadValue();
            Ray raggio = cameraRestauro.ScreenPointToRay(posizioneMouse);
            if (Physics.Raycast(raggio, out RaycastHit hitInfo, Mathf.Infinity, layerRestauro))
            {
                hasHit = true;
            }
        }

        if (isPressing && hasHit)
        {
            UseBrush();
            StartCleaningSound();
        }
        else
        {
            StopCleaningSound();
        }
    }

    private void StartCleaningSound()
    {
        if (wasBrushingLastFrame) return;
        wasBrushingLastFrame = true;
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning($"[StrumentoPulizia] '{gameObject.name}': AudioManager.Instance è null! Impossibile avviare il loop di pulizia.");
        }
        else if (cleaningSound.clip == null)
        {
            Debug.LogWarning($"[StrumentoPulizia] '{gameObject.name}': cleaningSound.clip non è assegnato nell'Inspector. Nessun suono verrà riprodotto.");
        }
        else
        {
            AudioManager.Instance.StartLoop(cleaningSound, "CleaningLoop");
        }
    }

    private void StopCleaningSound()
    {
        if (!wasBrushingLastFrame) return;
        wasBrushingLastFrame = false;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopLoop("CleaningLoop", 0.15f);
        }
    }
    private void UseBrush()
    {
        if (cameraRestauro == null || textureInstance == null)
        {
            return;
        }

        Vector2 posizioneMouse = Mouse.current.position.ReadValue();
        Ray raggio = cameraRestauro.ScreenPointToRay(posizioneMouse);
        
        if (Physics.Raycast(raggio, out RaycastHit hitInfo, Mathf.Infinity, layerRestauro))
        {
            if (hitInfo.collider is MeshCollider mc)
            {
                Vector2 coordinataUV = RestorationUtils.WrapUV(hitInfo.textureCoord);
                int pixelCentroX = (int)(coordinataUV.x * textureWidth);
                int pixelCentroY = (int)(coordinataUV.y * textureHeight);
                
                PitturaPixel(pixelCentroX, pixelCentroY);
            }
            else
            {
                Debug.LogWarning($"Il collider {hitInfo.collider.name} non è un MeshCollider. Coordinate UV non disponibili.");
            }
        }
    }

    private void PitturaPixel(int centroX, int centroY)
    {
        bool textureModificata = false;

        // Calcola il raggio in pixel basato su una dimensione di riferimento di 1024
        // per mantenere la dimensione coerente visivamente e indipendente dalla risoluzione della texture.
        float scala = (float)Mathf.Max(textureWidth, textureHeight) / 1024f;
        float raggioPixel = rangePaintbrush * scala;
        float raggioPixelSqr = raggioPixel * raggioPixel;

        int rangeLimit = Mathf.Max(1, Mathf.CeilToInt(raggioPixel));

        for (int x = -rangeLimit; x <= rangeLimit; x++)
        {
            for (int y = -rangeLimit; y <= rangeLimit; y++)
            {
                if (x * x + y * y <= raggioPixelSqr)
                {
                    int targetX = centroX + x;
                    int targetY = centroY + y;

                    if (targetX >= 0 && targetX < textureWidth && targetY >= 0 && targetY < textureHeight)
                    {
                        int indice = targetY * textureWidth + targetX;
                        Color32 p = texture32[indice];

                        // Pulizia del pixel se il colore supera la soglia minima di tolleranza dello sporco
                        if (p.r > 12 || p.g > 12 || p.b > 12)
                        {
                            texture32[indice] = new Color32(0, 0, 0, 255);
                            pixelPainted++;
                            textureModificata = true;
                        }
                    }
                }
            }
        }

        if (textureModificata)
        {
            textureInstance.SetPixels32(texture32);
            textureInstance.Apply();

            progression = RestorationUtils.CalcolaProgressione(pixelPainted, totPixel);

#if UNITY_EDITOR
            RestoreLogger.Log($"Progresso pulizia: {progression * 100f:F0}%.");
#endif

            if (progression >= sogliaCompletamentoPulizia)
            {
                RestoreLogger.Log("Pulizia completata con successo.");
                minigiocoFinito = true;
                progression = 1f;
                ResetMouseCursor();
                
                ImpostaFaseFinePuliziaSuMateriali();

                ConfiguraCollidersMosaico(false);

                StartCoroutine(SequenzaCompletamentoPulizia());
            }
        }
    }

    private System.Collections.IEnumerator SequenzaCompletamentoPulizia()
    {
        if (tavoloCorrente != null && tavoloCorrente.vaschettaGameObject != null)
        {
            yield return RestorationUtils.VibraOggetto(tavoloCorrente.vaschettaGameObject, 0.6f, 0.012f);
        }

        tavoloCorrente?.AvanzaFase(faseSuccessiva);
        eventoPuliziaCompletata?.Invoke(tavoloCorrente?.vaschettaCorrente);
        eventoRestauroCompletato?.Invoke(tavoloCorrente?.oggettoCorrente);
        OnPhaseCompleted?.Invoke(faseSuccessiva != null);
    }

    public void SetMouseCursor()
    {
        Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
    }

    public void ResetMouseCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    public void IniziaMinigame()
    {
        pixelPainted = 0;
        progression = 0f;
        minigiocoFinito = false;
        RestoreLogger.Log("Minigioco di pulizia attivato.");
    }


    private int GetSubMeshIndex(MeshCollider meshCollider, int triangleIndex)
    {
        if (meshCollider == null || meshCollider.sharedMesh == null) return -1;

        Mesh mesh = meshCollider.sharedMesh;
        int triangleCounter = 0;

        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            triangleCounter += (int)mesh.GetIndexCount(i) / 3;
            if (triangleIndex < triangleCounter)
                return i;
        }

        return -1;
    }

    private void ConfiguraCollidersMosaico(bool restoreMode)
    {
        if (tavoloCorrente == null || tavoloCorrente.vaschettaGameObject == null) return;
        GameObject obj = tavoloCorrente.vaschettaGameObject;

        if (restoreMode)
        {
            if (obj.TryGetComponent<PickUp_Interaction>(out var pickup))
            {
                pickup.enabled = false;
            }
            else
            {
                var childPickup = obj.GetComponentInChildren<PickUp_Interaction>(true);
                if (childPickup != null) childPickup.enabled = false;
            }

            // Configurazione dei collider per abilitare solo MeshCollider (necessari per raycast UV)
            foreach (var c in obj.GetComponentsInChildren<Collider>(true))
            {
                if (c is MeshCollider)
                {
                    c.enabled = true;
                }
                else
                {
                    c.enabled = false;
                }
            }
        }
        else
        {
            // Ripristino dell'interazione e riattivazione di tutti i collider
            if (obj.TryGetComponent<PickUp_Interaction>(out var pickup))
            {
                pickup.enabled = true;
            }
            else
            {
                var childPickup = obj.GetComponentInChildren<PickUp_Interaction>(true);
                if (childPickup != null) childPickup.enabled = true;
            }

            foreach (var c in obj.GetComponentsInChildren<Collider>(true))
            {
                c.enabled = true;
            }
        }
    }
    

}
