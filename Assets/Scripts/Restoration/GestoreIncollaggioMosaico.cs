using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections;

public class GestoreIncollaggioMosaico : MonoBehaviour, IRestorationPhaseManager, IRestorationPhase
{
    public event System.Action<bool> OnPhaseCompleted;
    [Header("Progressione Restauro")]
    [SerializeField] private TavoloSO tavoloCorrente;
    [SerializeField] private FaseRestauroSO triggerIncollaggio;
    [SerializeField] private Camera cameraRestauro;
    [SerializeField] private FaseRestauroSO faseSuccessiva;
    [SerializeField] private bool usaResina = false; // Se true, usa la mascheraResinaMosaico invece di mascheraCollaMosaico

    [Header("Input e Cursore")]
    [SerializeField] private Texture2D cursorGlueTexture;
    [SerializeField] private Vector2 cursorGlueHotspot = Vector2.zero;
    [SerializeField] private int rangePennelloColla = 25;
    [SerializeField] private LayerMask layerRestauro;
    [SerializeField] private Texture2D textureVisivaColla; // La texture visiva (es. colla o resina) da applicare al materiale

    [Header("Rotazione (Opzionale)")]
    [SerializeField] private float velocitaRotazione = 90f;

    [Header("Eventi")]
    [SerializeField] private UnityEvent onIncollaggioCompletato;

    [Header("Audio")]
    [SerializeField] private SoundEffect glueSound;
    [SerializeField] private SoundEffect resinSound;
    private bool wasPaintingLastFrame = false;

    private MosaicoSO mosaicoCorrente;
    private GameObject mosaicoGO;
    private Texture2D collaTextureInstance;
    private Texture2D mascheraCollaUnica;

    private Color32[] coloreTextureColla;
    private bool[] pixelCollaNecessari;
    private bool[] pixelCollaMappati;

    [Header("Soglia di Completamento")]
    [Tooltip("Percentuale minima richiesta per completare la fase di incollaggio/resinatura (es. 0.70 = 70%). Se impostata a 0, usa la soglia definita nel ScriptableObject.")]
    [SerializeField] [Range(0f, 1f)] private float sogliaCompletamentoOverride = 0f;

    [Header("Debug Incollaggio (Sola Lettura)")]
    [SerializeField] private int totPixelCollaNecessari;
    [SerializeField] private int pixelCollaDipinti;
    [SerializeField] [Range(0f, 1f)] private float progressioneColla = 0f;
    public float ProgressioneColla => progressioneColla;

    private bool isIncollaggioActive = false;
    private bool cameraTransitionFinished = false;

    private readonly int idProprietaMascheraColla = Shader.PropertyToID("_mascheraColla");
    private readonly int idProprietaCollaDipingibile = Shader.PropertyToID("_Colla");
    private readonly int idMostraTerra   = Shader.PropertyToID("_mostraTerra");
    private readonly int idMostraColla   = Shader.PropertyToID("_mostraColla");
    private readonly int idMostraPittura = Shader.PropertyToID("_mostraPittura");

    private float SogliaCompletamentoColla
    {
        get
        {
            if (sogliaCompletamentoOverride > 0f)
                return sogliaCompletamentoOverride;

            if (mosaicoCorrente != null)
            {
                return usaResina ? mosaicoCorrente.SogliaCompletamentoResina : mosaicoCorrente.SogliaCompletamentoColla;
            }
            return 0.70f;
        }
    }

    private MaterialPropertyBlock propBlock;

    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();

        if (layerRestauro.value == 0)
        {
            layerRestauro = LayerMask.GetMask("Restauro");
            Debug.Log($"Layer di restauro vuoto. Impostato automaticamente a Restauro con valore {layerRestauro.value}.");
        }

        if (cameraRestauro == null)
        {
            cameraRestauro = GetComponentInChildren<Camera>(true);
            if (cameraRestauro == null)
                cameraRestauro = GetComponentInParent<Camera>(true);
            if (cameraRestauro == null)
                cameraRestauro = Camera.main;
        }

        // Rimossa ricerca legacy del RestoreManager per decoupling
    }

    private void OnEnable()
    {
        if (tavoloCorrente != null)
        {
            tavoloCorrente.OnFaseCambiata += OnFaseCambiata;
            
            if (tavoloCorrente.faseCorrente == triggerIncollaggio)
            {
                Debug.Log($"Rilevata fase di incollaggio {triggerIncollaggio.name} all'attivazione.");
                IniziaIncollaggio();
            }
        }
    }

    private void OnDisable()
    {
        if (tavoloCorrente != null)
        {
            tavoloCorrente.OnFaseCambiata -= OnFaseCambiata;
        }
        TerminaIncollaggio();
    }

    private void OnFaseCambiata(FaseRestauroSO nuovaFase)
    {
        if (nuovaFase == triggerIncollaggio)
        {
            Debug.Log($"Rilevata fase di incollaggio {triggerIncollaggio.name}.");
            IniziaIncollaggio();
        }
        else
        {
            TerminaIncollaggio();
        }
    }

    private void IniziaIncollaggio()
    {
        Debug.Log("Avvio del processo di incollaggio del mosaico.");
        cameraTransitionFinished = false;

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

        mosaicoGO = tavoloCorrente.vaschettaGameObject;
        if (mosaicoGO == null)
        {
            Debug.LogError("GameObject del mosaico non trovato nel tavolo di lavoro.");
            return;
        }

        TerminaIncollaggio();

        // Disattivazione temporanea dell'interazione sul mosaico durante l'incollaggio
        PickUp_Interaction pickup = mosaicoGO.GetComponentInChildren<PickUp_Interaction>(true);
        if (pickup != null)
        {
            pickup.enabled = false;
            Debug.Log("Componente PickUp_Interaction temporaneamente disabilitato sul mosaico.");
        }

        // Configurazione dei collider per abilitare solo MeshCollider (necessari per raycast UV)
        Collider[] colliders = mosaicoGO.GetComponentsInChildren<Collider>(true);
        int meshCollidersAbilitati = 0;
        int altriColliderDisabilitati = 0;

        foreach (var c in colliders)
        {
            if (c is MeshCollider)
            {
                c.enabled = true;
                meshCollidersAbilitati++;
            }
            else
            {
                c.enabled = false;
                altriColliderDisabilitati++;
            }
        }
        Debug.Log($"Configurati {meshCollidersAbilitati} MeshCollider e disabilitati {altriColliderDisabilitati} altri collider sul mosaico.");

        // Inizializzazione della texture virtuale per la pittura della colla
        mascheraCollaUnica = usaResina ? mosaicoCorrente.MascheraResinaMosaico : mosaicoCorrente.MascheraCollaMosaico;
        if (mascheraCollaUnica == null)
        {
            Debug.Log("Maschera colla non trovata nel mosaico, tentativo di recupero dal materiale.");
            foreach (var r in mosaicoGO.GetComponentsInChildren<Renderer>())
            {
                foreach (var mat in r.sharedMaterials)
                {
                    if (mat != null && mat.HasProperty(idProprietaMascheraColla))
                    {
                        var tex = mat.GetTexture(idProprietaMascheraColla) as Texture2D;
                        if (tex != null)
                        {
                            mascheraCollaUnica = tex;
                            Debug.Log($"Maschera colla recuperata correttamente dal materiale {mat.name}.");
                            break;
                        }
                    }
                }
                if (mascheraCollaUnica != null) break;
            }
        }
        ConfiguraMaterialiMosaico();

        if (mascheraCollaUnica != null)
        {
            int w = mascheraCollaUnica.width;
            int h = mascheraCollaUnica.height;

            collaTextureInstance = new Texture2D(w, h, TextureFormat.RGBA32, false);
            coloreTextureColla = new Color32[w * h];

            for (int i = 0; i < coloreTextureColla.Length; i++)
            {
                coloreTextureColla[i] = new Color32(0, 0, 0, 0);
            }

            collaTextureInstance.SetPixels32(coloreTextureColla);
            collaTextureInstance.Apply();

            if (tavoloCorrente != null)
            {
                tavoloCorrente.collaTextureMosaico = collaTextureInstance;
            }

            SincronizzaTextureColla(collaTextureInstance);
            
            // Scansione della maschera di incollaggio per determinare la mappa dei pixel richiesti
            InizializzaMappaPixelColla();
        }
        else
        {
            Debug.LogWarning("Maschera colla non assegnata.");
        }

        isIncollaggioActive = true;

        if (cursorGlueTexture != null)
        {
            Cursor.SetCursor(cursorGlueTexture, cursorGlueHotspot, CursorMode.Auto);
        }
    }

    private void InizializzaMappaPixelColla()
    {
        if (mascheraCollaUnica == null) return;

        Texture2D mascheraLeggibile = RestorationUtils.CopiaTextureInRGBA32(mascheraCollaUnica);
        if (mascheraLeggibile == null)
        {
            Debug.LogError("Impossibile creare una copia leggibile della maschera colla.");
            return;
        }

        int width = collaTextureInstance.width;
        int height = collaTextureInstance.height;

        pixelCollaNecessari = new bool[width * height];
        pixelCollaMappati = new bool[width * height];
        totPixelCollaNecessari = 0;
        pixelCollaDipinti = 0;
        progressioneColla = 0f;

        // Scansione spaziale dal viewport per marcare quali pixel UV sono effettivamente visibili per evitare soft-lock
        bool[] pixelRaggiungibili = new bool[width * height];
        int risoluzioneScansione = 512;
        int raggioPrecisioneTelecamera = Mathf.CeilToInt((float)width / risoluzioneScansione);

        for (int x = 0; x <= risoluzioneScansione; x++)
        {
            for (int y = 0; y <= risoluzioneScansione; y++)
            {
                float viewportX = (float)x / risoluzioneScansione;
                float viewportY = (float)y / risoluzioneScansione;

                Ray ray = cameraRestauro.ViewportPointToRay(new UnityEngine.Vector3(viewportX, viewportY, 0));

                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerRestauro))
                {
                    if (hit.collider is MeshCollider)
                    {
                        if (mosaicoGO != null && (hit.collider.gameObject == mosaicoGO || hit.collider.transform.IsChildOf(mosaicoGO.transform)))
                        {
                            Vector2 uv = RestorationUtils.WrapUV(hit.textureCoord);
                            int pixelX = (int)(uv.x * width);
                            int pixelY = (int)(uv.y * height);

                            for (int dx = -raggioPrecisioneTelecamera; dx <= raggioPrecisioneTelecamera; dx++)
                            {
                                for (int dy = -raggioPrecisioneTelecamera; dy <= raggioPrecisioneTelecamera; dy++)
                                {
                                    int targetX = pixelX + dx;
                                    int targetY = pixelY + dy;

                                    if (targetX >= 0 && targetX < width && targetY >= 0 && targetY < height)
                                    {
                                        pixelRaggiungibili[targetY * width + targetX] = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        Color32[] pixelMaschera = mascheraLeggibile.GetPixels32();

        for (int i = 0; i < pixelMaschera.Length; i++)
        {
            // Un pixel è richiesto se è marcato attivo nella maschera e visibile sul mesh del mosaico
            if (pixelMaschera[i].r > 128 && pixelRaggiungibili[i])
            {
                pixelCollaNecessari[i] = true;
                totPixelCollaNecessari++;
            }
        }

        Destroy(mascheraLeggibile);
        Debug.Log($"Mappa pixel del mosaico inizializzata con {totPixelCollaNecessari} pixel richiesti.");
    }

    private void TerminaIncollaggio()
    {
        isIncollaggioActive = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        RimuoviMappaColla();
    }

    private void Update()
    {
        if (!isIncollaggioActive || !cameraTransitionFinished || collaTextureInstance == null)
        {
            if (wasPaintingLastFrame)
            {
                StopGlueSound();
            }
            return;
        }

        GestisciRotazione();
        
        bool isPressing = Mouse.current != null && Mouse.current.leftButton.isPressed;
        bool hasHit = false;

        if (isPressing && cameraRestauro != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = cameraRestauro.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerRestauro))
            {
                if (mosaicoGO != null && (hit.collider.gameObject == mosaicoGO || hit.collider.transform.IsChildOf(mosaicoGO.transform)))
                {
                    hasHit = true;
                }
            }
        }

        if (isPressing && hasHit)
        {
            GestisciPittura();
            StartGlueSound();
        }
        else
        {
            StopGlueSound();
        }
    }

    private void StartGlueSound()
    {
        if (wasPaintingLastFrame) return;
        wasPaintingLastFrame = true;
        
        SoundEffect effectToPlay = usaResina ? resinSound : glueSound;
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning($"[GestoreIncollaggioMosaico] '{gameObject.name}': AudioManager.Instance è null! Impossibile avviare il loop colla.");
        }
        else if (effectToPlay.clip == null)
        {
            string nomeClip = usaResina ? "resinSound" : "glueSound";
            Debug.LogWarning($"[GestoreIncollaggioMosaico] '{gameObject.name}': {nomeClip}.clip non è assegnato nell'Inspector. Nessun suono verrà riprodotto.");
        }
        else
        {
            AudioManager.Instance.StartLoop(effectToPlay, "MosaicoGlueLoop");
        }
    }

    private void StopGlueSound()
    {
        if (!wasPaintingLastFrame) return;
        wasPaintingLastFrame = false;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopLoop("MosaicoGlueLoop", 0.15f);
        }
    }

    private void GestisciRotazione()
    {
        if (mosaicoGO == null) return;

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
            mosaicoGO.transform.Rotate(Vector3.up, inputRot * velocitaRotazione * Time.deltaTime, Space.Self);
        }
    }

    private void GestisciPittura()
    {
        if (cameraRestauro == null || Mouse.current == null) return;

        if (Mouse.current.leftButton.isPressed)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = cameraRestauro.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerRestauro))
            {
                if (mosaicoGO != null && (hit.collider.gameObject == mosaicoGO || hit.collider.transform.IsChildOf(mosaicoGO.transform)))
                {
                    PitturaColla(hit.textureCoord);
                }
                else
                {
                    Debug.LogWarning($"Il raycast ha colpito {(hit.collider != null ? hit.collider.name : "un oggetto")} che non corrisponde al mosaico.");
                }
            }
            else
            {
                if (Physics.Raycast(ray, out RaycastHit hitDebug, Mathf.Infinity))
                {
                    int hitLayer = hitDebug.collider.gameObject.layer;
                    string hitLayerName = LayerMask.LayerToName(hitLayer);
                    Debug.LogWarning($"Il raycast ha colpito {hitDebug.collider.name} sul layer {hitLayerName} anziché sul layer di restauro.");
                }
                else
                {
                    Debug.LogWarning("Il raycast non ha colpito alcun oggetto.");
                }
            }
        }
    }

    private void PitturaColla(Vector2 uv)
    {
        if (mascheraCollaUnica == null || collaTextureInstance == null || pixelCollaNecessari == null) return;

        int width = collaTextureInstance.width;
        int height = collaTextureInstance.height;

        Vector2 wrappedUV = RestorationUtils.WrapUV(uv);
        int pixelX = (int)(wrappedUV.x * width);
        int pixelY = (int)(wrappedUV.y * height);

        bool textureModificata = false;

        // Calcola il raggio in pixel basato su una dimensione di riferimento di 1024
        // per mantenere la dimensione coerente visivamente e indipendente dalla risoluzione della texture.
        float scala = (float)Mathf.Max(width, height) / 1024f;
        float raggioPixel = rangePennelloColla * scala;
        float raggioPixelSqr = raggioPixel * raggioPixel;

        int rangeLimit = Mathf.Max(1, Mathf.CeilToInt(raggioPixel));

        for (int x = -rangeLimit; x <= rangeLimit; x++)
        {
            for (int y = -rangeLimit; y <= rangeLimit; y++)
            {
                if (x * x + y * y <= raggioPixelSqr)
                {
                    int targetX = pixelX + x;
                    int targetY = pixelY + y;

                    if (targetX >= 0 && targetX < width && targetY >= 0 && targetY < height)
                    {
                        int indice = targetY * width + targetX;

                        if (pixelCollaNecessari[indice] && !pixelCollaMappati[indice])
                        {
                            pixelCollaMappati[indice] = true;
                            pixelCollaDipinti++;
                            textureModificata = true;
                            coloreTextureColla[indice] = new Color32(255, 255, 255, 255);
                        }
                    }
                }
            }
        }

        if (textureModificata)
        {
            collaTextureInstance.SetPixels32(coloreTextureColla);
            collaTextureInstance.Apply();

            if (totPixelCollaNecessari > 0)
            {
                float rapporto = (float)pixelCollaDipinti / totPixelCollaNecessari;
                progressioneColla = Mathf.Clamp01(rapporto);

#if UNITY_EDITOR
                Debug.Log($"Progresso colla: {progressioneColla * 100f:F0}%.");
#endif

                if (progressioneColla >= SogliaCompletamentoColla)
                {
                    ControllaCompletamento();
                }
            }
        }
    }

    private void ControllaCompletamento()
    {
        if (progressioneColla >= SogliaCompletamentoColla && isIncollaggioActive)
        {
            StartCoroutine(SequenzaCompletamento());
        }
    }

    private IEnumerator SequenzaCompletamento()
    {
        isIncollaggioActive = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        // Effetto visivo di vibrazione del mosaico per simulare il consolidamento
        yield return RestorationUtils.VibraOggetto(mosaicoGO, 0.6f, 0.012f);

        Debug.Log("Completamento della fase di incollaggio del mosaico.");

        Shader.SetGlobalFloat(idMostraTerra, 0f);
        Shader.SetGlobalFloat(idMostraColla, 1f);
        Shader.SetGlobalFloat(idMostraPittura, 1f);

        if (mosaicoGO != null)
        {
            // Aggiorna le proprietà dei materiali del mosaico per riflettere il completamento dell'incollaggio usando MaterialPropertyBlock
            foreach (var r in mosaicoGO.GetComponentsInChildren<Renderer>())
            {
                r.GetPropertyBlock(propBlock);
                propBlock.SetFloat(idMostraTerra, 0f);
                propBlock.SetFloat(idMostraColla, 1f);
                propBlock.SetFloat(idMostraPittura, 1f);
                propBlock.SetTexture(idProprietaCollaDipingibile, collaTextureInstance);
                r.SetPropertyBlock(propBlock);
            }

            if (tavoloCorrente != null)
            {
                tavoloCorrente.vaschettaGameObject = mosaicoGO;
            }
        }
        else
        {
            Debug.LogWarning("Oggetto del mosaico non valido.");
        }

        yield return new WaitForSeconds(0.4f);

        if (faseSuccessiva != null)
        {
            Debug.Log($"Avanzamento alla fase successiva: {faseSuccessiva.name}.");
            tavoloCorrente?.AvanzaFase(faseSuccessiva);
        }
        else
        {
            Debug.Log("Nessuna fase successiva configurata. Il completamento del restauro è ora gestito dal RestoreManager tramite l'evento OnPhaseCompleted.");
        }

        onIncollaggioCompletato?.Invoke();
        OnPhaseCompleted?.Invoke(faseSuccessiva != null);
    }

    private void RimuoviMappaColla()
    {
        // Salvataggio della texture della colla in previsione della fase successiva di applicazione delle garze
        if (tavoloCorrente != null && tavoloCorrente.faseCorrente == faseSuccessiva && faseSuccessiva != null)
        {
            Debug.Log("Salvataggio della texture della colla per la fase successiva.");
            return;
        }

        if (collaTextureInstance != null)
        {
            Destroy(collaTextureInstance);
            collaTextureInstance = null;
        }
        
        if (mosaicoGO != null)
        {
            foreach (var r in mosaicoGO.GetComponentsInChildren<Renderer>())
            {
                r.GetPropertyBlock(propBlock);
                propBlock.SetTexture(idProprietaCollaDipingibile, Texture2D.blackTexture);
                r.SetPropertyBlock(propBlock);
            }
        }
    }

    private void SincronizzaTextureColla(Texture2D texture)
    {
        if (mosaicoGO == null) return;

        foreach (var r in mosaicoGO.GetComponentsInChildren<Renderer>())
        {
            bool daModificare = false;
            foreach (var mat in r.sharedMaterials)
            {
                if (mat != null && mat.HasProperty(idProprietaCollaDipingibile))
                {
                    daModificare = true;
                    break;
                }
            }

            if (daModificare)
            {
                r.GetPropertyBlock(propBlock);
                propBlock.SetTexture(idProprietaCollaDipingibile, texture != null ? texture : Texture2D.blackTexture);
                r.SetPropertyBlock(propBlock);
            }
        }
    }

    private void ConfiguraMaterialiMosaico()
    {
        if (mosaicoGO == null) return;

        Shader.SetGlobalFloat(idMostraTerra,   0f);
        Shader.SetGlobalFloat(idMostraColla,   1f);
        Shader.SetGlobalFloat(idMostraPittura, 1f);

        int idProprietaTextureColla = Shader.PropertyToID("_TextureColla");

        foreach (var r in mosaicoGO.GetComponentsInChildren<Renderer>())
        {
            bool daModificare = false;
            foreach (var mat in r.sharedMaterials)
            {
                if (mat != null && (mat.HasProperty(idProprietaMascheraColla) || mat.HasProperty(idProprietaTextureColla) || mat.HasProperty(idProprietaCollaDipingibile)))
                {
                    daModificare = true;
                    break;
                }
            }

            if (daModificare)
            {
                r.GetPropertyBlock(propBlock);
                if (mascheraCollaUnica != null)
                {
                    propBlock.SetTexture(idProprietaMascheraColla, mascheraCollaUnica);
                }
                if (textureVisivaColla != null)
                {
                    propBlock.SetTexture(idProprietaTextureColla, textureVisivaColla);
                }
                if (collaTextureInstance != null)
                {
                    propBlock.SetTexture(idProprietaCollaDipingibile, collaTextureInstance);
                }
                r.SetPropertyBlock(propBlock);
            }
        }
    }



    public void CameraTransitionCompleted()
    {
        cameraTransitionFinished = true;
        Debug.Log("Transizione telecamera completata. Applicazione colla abilitata.");
    }


}
