using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections;

public class GestoreIncollaggioMosaico : MonoBehaviour
{
    [Header("Progressione Restauro")]
    [SerializeField] private TavoloSO tavoloCorrente;
    [SerializeField] private FaseRestauroSO triggerIncollaggio;
    [SerializeField] private Camera cameraRestauro;
    [SerializeField] private RestoreManager restoreManager;
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

    private MosaicoSO mosaicoCorrente;
    private GameObject mosaicoGO;
    private Texture2D collaTextureInstance;
    private Texture2D mascheraCollaUnica;

    private Color32[] coloreTextureColla;
    private bool[] pixelCollaNecessari;
    private bool[] pixelCollaMappati;

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
            if (mosaicoCorrente != null)
                return mosaicoCorrente.SogliaCompletamentoColla;
            return 0.70f;
        }
    }

    private void Awake()
    {
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

        if (restoreManager == null)
        {
            restoreManager = GetComponentInParent<RestoreManager>(true);
            if (restoreManager == null && transform.parent != null)
                restoreManager = transform.parent.GetComponentInChildren<RestoreManager>(true);
        }
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

        Texture2D mascheraLeggibile = CopiaTextureCompatibile(mascheraCollaUnica);
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
                            Vector2 uv = WrapUV(hit.textureCoord);
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
        if (!isIncollaggioActive || !cameraTransitionFinished) return;

        GestisciRotazione();
        GestisciPittura();
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

        Vector2 wrappedUV = WrapUV(uv);
        int pixelX = (int)(wrappedUV.x * width);
        int pixelY = (int)(wrappedUV.y * height);

        bool textureModificata = false;

        for (int x = -rangePennelloColla; x <= rangePennelloColla; x++)
        {
            for (int y = -rangePennelloColla; y <= rangePennelloColla; y++)
            {
                if (x * x + y * y <= rangePennelloColla * rangePennelloColla)
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
            SincronizzaTextureColla(collaTextureInstance);

            if (totPixelCollaNecessari > 0)
            {
                float rapporto = (float)pixelCollaDipinti / totPixelCollaNecessari;
                progressioneColla = Mathf.Clamp01(rapporto);

                Debug.Log($"Progresso colla: {progressioneColla * 100f:F0}%.");

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
        float duration = 0.6f;
        float magnitude = 0.012f;
        Vector3 originalPos = mosaicoGO != null ? mosaicoGO.transform.position : Vector3.zero;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (mosaicoGO != null)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                float z = Random.Range(-1f, 1f) * magnitude;
                mosaicoGO.transform.position = originalPos + new Vector3(x, y, z);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (mosaicoGO != null)
        {
            mosaicoGO.transform.position = originalPos;
        }

        Debug.Log("Completamento della fase di incollaggio del mosaico.");

        Shader.SetGlobalFloat(idMostraTerra, 0f);
        Shader.SetGlobalFloat(idMostraColla, 1f);
        Shader.SetGlobalFloat(idMostraPittura, 1f);

        if (mosaicoGO != null)
        {
            // Aggiorna le proprietà dei materiali del mosaico per riflettere il completamento dell'incollaggio
            foreach (var r in mosaicoGO.GetComponentsInChildren<Renderer>())
            {
                Material[] mats = r.materials;
                bool modified = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null)
                    {
                        if (mats[i].HasProperty(idMostraTerra)) { mats[i].SetFloat(idMostraTerra, 0f); modified = true; }
                        if (mats[i].HasProperty(idMostraColla)) { mats[i].SetFloat(idMostraColla, 1f); modified = true; }
                        if (mats[i].HasProperty(idMostraPittura)) { mats[i].SetFloat(idMostraPittura, 1f); modified = true; }
                        if (mats[i].HasProperty(idProprietaCollaDipingibile)) { mats[i].SetTexture(idProprietaCollaDipingibile, collaTextureInstance); modified = true; }
                    }
                }
                if (modified)
                {
                    r.materials = mats;
                }
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
            Debug.Log("Nessuna fase successiva configurata. Restauro completato.");
            if (restoreManager != null)
            {
                restoreManager.CompletaRestauro();
            }
        }

        onIncollaggioCompletato?.Invoke();
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
        SincronizzaTextureColla(null);
    }

    private void SincronizzaTextureColla(Texture2D texture)
    {
        if (mosaicoGO == null) return;

        foreach (var r in mosaicoGO.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = r.materials;
            bool modified = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null && mats[i].HasProperty(idProprietaCollaDipingibile))
                {
                    mats[i].SetTexture(idProprietaCollaDipingibile, texture);
                    modified = true;
                }
            }
            if (modified)
            {
                r.materials = mats;
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
            Material[] mats = r.materials;
            bool modified = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null)
                {
                    if (mats[i].HasProperty(idProprietaMascheraColla) && mascheraCollaUnica != null)
                    {
                        mats[i].SetTexture(idProprietaMascheraColla, mascheraCollaUnica);
                        modified = true;
                    }
                    if (textureVisivaColla != null && mats[i].HasProperty(idProprietaTextureColla))
                    {
                        mats[i].SetTexture(idProprietaTextureColla, textureVisivaColla);
                        modified = true;
                    }
                }
            }
            if (modified)
                r.materials = mats;
        }
    }

    private Texture2D CopiaTextureCompatibile(Texture2D sorgente)
    {
        if (sorgente == null) return null;

        RenderTexture rt = RenderTexture.GetTemporary(
            sorgente.width, 
            sorgente.height, 
            0, 
            RenderTextureFormat.Default, 
            RenderTextureReadWrite.Linear
        );

        Graphics.Blit(sorgente, rt);

        RenderTexture precedenteActive = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D nuovaTexture = new Texture2D(sorgente.width, sorgente.height, TextureFormat.RGBA32, false);
        nuovaTexture.name = sorgente.name + "_Leggibile";
        
        nuovaTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        nuovaTexture.Apply();

        RenderTexture.active = precedenteActive;
        RenderTexture.ReleaseTemporary(rt);

        return nuovaTexture;
    }

    public void CameraTransitionCompleted()
    {
        cameraTransitionFinished = true;
        Debug.Log("Transizione telecamera completata. Applicazione colla abilitata.");
    }

    private Vector2 WrapUV(Vector2 uv)
    {
        uv.x = uv.x - Mathf.Floor(uv.x);
        uv.y = uv.y - Mathf.Floor(uv.y);
        return uv;
    }
}
