using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class GestoreIncollaggio : MonoBehaviour
{
    [Header("Tavolo")]
    [SerializeField] private TavoloSO tavoloCorrente;
    [SerializeField] private FaseRestauroSO triggerIncollaggio; // La fase associata a questo script (es. FaseColla)
    [SerializeField] private Camera cameraRestauro;
    [SerializeField] private RestoreManager restoreManager;

    [Header("Spawn Anfora Centrale")]
    [SerializeField] private Transform puntoSpawnAnfora;

    [Header("Eventi")]
    [SerializeField] private UnityEvent onIncollaggioCompletato;

    [Header("Grafica Cursore Applicazione Colla")]
    [SerializeField] private Texture2D cursorGlueTexture;
    [SerializeField] private Vector2 cursorGlueHotspot = Vector2.zero;

    [Header("Configurazioni Pennello")]
    [SerializeField] private int rangePennelloColla = 15;
    [SerializeField] private LayerMask layerRestauro;

    [Header("Rotazione Anfora")]
    [SerializeField] private float velocitaRotazione = 100f;

    private string nomeProprietaMascheraColla = "_mascheraColla";
    private string nomeProprietaCollaDipingibile = "_Colla";

    private GameObject anforaAssemblata;
    private bool isIncollaggioActive = false;
    private bool cameraTransitionFinished = false;

    // Stato della colla
    private Texture2D collaTextureInstance;
    private Color32[] coloreTextureColla;
    private bool[] pixelCollaNecessari;
    private bool[] pixelCollaMappati;

    [Header("Debug Incollaggio (Sola Lettura)")]
    [SerializeField] private int totPixelCollaNecessari;
    [SerializeField] private int pixelCollaDipinti;
    [SerializeField] [Range(0f, 1f)] private float progressioneColla = 0f;
    public float ProgressioneColla => progressioneColla;

    private float SogliaCompletamentoColla
    {
        get
        {
            if (tavoloCorrente != null && tavoloCorrente.vaschettaCorrente != null)
                return tavoloCorrente.vaschettaCorrente.SogliaCompletamentoColla;
            return 0.70f; // Fallback
        }
    }

    private Texture2D mascheraCollaUnica;
    private int idProprietaMascheraColla;
    private int idProprietaCollaDipingibile;
    private int idMostraTerra;
    private int idMostraColla;
    private int idMostraPittura;

    private void Awake()
    {
        idProprietaMascheraColla    = Shader.PropertyToID(nomeProprietaMascheraColla);
        idProprietaCollaDipingibile = Shader.PropertyToID(nomeProprietaCollaDipingibile);
        idMostraTerra    = Shader.PropertyToID("_mostraTerra");
        idMostraColla    = Shader.PropertyToID("_mostraColla");
        idMostraPittura  = Shader.PropertyToID("_mostraPittura");

        if (layerRestauro.value == 0)
        {
            layerRestauro = LayerMask.GetMask("Restauro");
            Debug.Log($"[GestoreIncollaggio] layerRestauro vuoto. Impostato automaticamente al layer 'Restauro' (valore: {layerRestauro.value}).");
        }

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
            if (tavoloCorrente.faseCorrente == triggerIncollaggio)
            {
                Debug.Log($"[GestoreIncollaggio] Rilevata fase corretta '{triggerIncollaggio.name}' all'abilitazione!");
                IniziaIncollaggio();
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
        Debug.Log($"[GestoreIncollaggio] OnFaseCambiata: fase cambiata a = {(fase != null ? fase.name : "null")}, fase attesa = {(triggerIncollaggio != null ? triggerIncollaggio.name : "null")}");
        if (fase != triggerIncollaggio)
        {
            TerminaIncollaggio();
            return;
        }

        IniziaIncollaggio();
    }

    private void IniziaIncollaggio()
    {
        Debug.Log($"[GestoreIncollaggio] IniziaIncollaggio avviato - vaschettaCorrente: {(tavoloCorrente != null && tavoloCorrente.vaschettaCorrente != null ? tavoloCorrente.vaschettaCorrente.name : "null")}");
        cameraTransitionFinished = false;
        if (tavoloCorrente == null || tavoloCorrente.vaschettaCorrente == null)
        {
            Debug.LogError("[GestoreIncollaggio] tavoloCorrente o vaschettaCorrente è nullo!");
            return;
        }

        if (puntoSpawnAnfora == null)
        {
            Debug.LogError("[GestoreIncollaggio] puntoSpawnAnfora non assegnato nell'Inspector!");
            return;
        }

        TerminaIncollaggio();

        // 1. Recupera l'anfora assemblata persistente da tavoloCorrente
        anforaAssemblata = tavoloCorrente.anforaAssemblata;
        if (anforaAssemblata == null)
        {
            Debug.LogError("[GestoreIncollaggio] anforaAssemblata è NULL in tavoloCorrente! Non c'è nessun vaso da incollare.");
            return;
        }

        Debug.Log($"[GestoreIncollaggio] Trovata anforaAssemblata '{anforaAssemblata.name}'. Riposizionamento e associazione al puntoSpawnAnfora.");
        
        // Reparenting dell'anfora per spostarla sotto la FaseColla
        anforaAssemblata.transform.SetParent(puntoSpawnAnfora, true);
        anforaAssemblata.transform.localPosition = Vector3.zero;
        anforaAssemblata.transform.localRotation = Quaternion.identity;

        // Configura i collider dei pezzi e del ghost
        GameObject vaschettaGO = tavoloCorrente.vaschettaGameObject;
        if (vaschettaGO == null)
        {
            Debug.LogError("[GestoreIncollaggio] vaschettaGameObject è NULL in tavoloCorrente!");
            return;
        }

        ConfigurazioneVaschetta config = vaschettaGO.GetComponent<ConfigurazioneVaschetta>();
        if (config == null)
        {
            Debug.LogError($"[GestoreIncollaggio] Manca il componente ConfigurazioneVaschetta sulla vaschetta '{vaschettaGO.name}'!");
            return;
        }

        List<GameObject> pezziOrdinati = config.pezziOrdinati;
        Debug.Log($"[GestoreIncollaggio] Trovati {pezziOrdinati.Count} pezzi ordinati nella vaschetta per configurare i colliders.");

        // Disabilita tutti i collider originali di ghostAnfora che non sono i pezzi posizionati dal giocatore
        int colliderGhostDisabilitati = 0;
        foreach (var col in anforaAssemblata.GetComponentsInChildren<Collider>(true))
        {
            bool isPezzoSnapped = false;
            foreach (var goPezzo in pezziOrdinati)
            {
                if (goPezzo != null && (col.gameObject == goPezzo || col.transform.IsChildOf(goPezzo.transform)))
                {
                    isPezzoSnapped = true;
                    break;
                }
            }
            if (!isPezzoSnapped)
            {
                col.enabled = false;
                colliderGhostDisabilitati++;
            }
        }
        Debug.Log($"[GestoreIncollaggio] Disabilitati {colliderGhostDisabilitati} collider originali del ghost.");

        // Riabilita e configura i collider dei pezzi posizionati dal giocatore come MeshCollider per il raycast UV
        int collidersPezziConfigurati = 0;
        foreach (var goPezzo in pezziOrdinati)
        {
            if (goPezzo != null)
            {
                Collider col = goPezzo.GetComponent<Collider>();
                if (col != null)
                {
                    if (!(col is MeshCollider))
                    {
                        Destroy(col);
                        col = goPezzo.AddComponent<MeshCollider>();
                    }
                    col.enabled = true;
                }
                else
                {
                    col = goPezzo.AddComponent<MeshCollider>();
                    col.enabled = true;
                }
                collidersPezziConfigurati++;
            }
        }
        Debug.Log($"[GestoreIncollaggio] Configurati {collidersPezziConfigurati} MeshColliders sui pezzi assemblati per raycasting UV.");

        // 2. Configura i materiali e la texture della colla
        mascheraCollaUnica = tavoloCorrente.vaschettaCorrente.mascheraCollaUnica;
        ConfiguraMaterialiAnfora();

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

            SincronizzaTextureColla(collaTextureInstance);
            
            // Inizializza i pixel necessari leggendo la maschera
            InizializzaMappaPixelColla();
        }
        else
        {
            Debug.LogWarning("[GestoreIncollaggio] Attenzione: mascheraCollaUnica è NULL!");
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
            Debug.LogError("[GestoreIncollaggio] Impossibile creare una copia leggibile di mascheraCollaUnica!");
            return;
        }

        int width = collaTextureInstance.width;
        int height = collaTextureInstance.height;

        pixelCollaNecessari = new bool[width * height];
        pixelCollaMappati = new bool[width * height];
        totPixelCollaNecessari = 0;
        pixelCollaDipinti = 0;
        progressioneColla = 0f;

        Color32[] pixelMaschera = mascheraLeggibile.GetPixels32();

        for (int i = 0; i < pixelMaschera.Length; i++)
        {
            if (pixelMaschera[i].r > 128)
            {
                pixelCollaNecessari[i] = true;
                totPixelCollaNecessari++;
            }
        }

        Destroy(mascheraLeggibile);
        Debug.Log($"[GestoreIncollaggio] Mappa pixel inizializzata. Totale pixel colla richiesti: {totPixelCollaNecessari}");
    }

    private void TerminaIncollaggio()
    {
        isIncollaggioActive = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        RimuoviMappaColla();

        if (anforaAssemblata != null)
        {
            Debug.Log($"[GestoreIncollaggio] TerminaIncollaggio - Distruggo anforaAssemblata '{anforaAssemblata.name}'");
            Destroy(anforaAssemblata);
            anforaAssemblata = null;
            
            if (tavoloCorrente != null)
            {
                tavoloCorrente.anforaAssemblata = null;
            }
        }
    }

    private void Update()
    {
        if (!isIncollaggioActive || !cameraTransitionFinished) return;

        GestisciRotazione();
        GestisciPittura();
    }

    private void GestisciRotazione()
    {
        if (anforaAssemblata == null) return;

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
            anforaAssemblata.transform.Rotate(Vector3.up, inputRot * velocitaRotazione * Time.deltaTime, Space.Self);
        }
    }

    private void GestisciPittura()
    {
        if (cameraRestauro == null || Mouse.current == null) return;

        if (Mouse.current.leftButton.isPressed)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = cameraRestauro.ScreenPointToRay(mousePos);

            // Eseguiamo il raycast ufficiale con il filtro layermask
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerRestauro))
            {
                if (anforaAssemblata != null && (hit.collider.gameObject == anforaAssemblata || hit.collider.transform.IsChildOf(anforaAssemblata.transform)))
                {
                    PitturaColla(hit.textureCoord);
                }
                else
                {
                    string hitName = hit.collider != null ? hit.collider.name : "null";
                    string hitParent = hit.collider != null && hit.collider.transform.parent != null ? hit.collider.transform.parent.name : "nessuno";
                    Debug.LogWarning($"[GestoreIncollaggio] Raycast ha colpito '{hitName}' (Parent: '{hitParent}'), ma non appartiene all'anfora assemblata!");
                }
            }
            else
            {
                // Lanciamo un raycast senza layer filter per debug se non colpisce nulla
                if (Physics.Raycast(ray, out RaycastHit hitDebug, Mathf.Infinity))
                {
                    int hitLayer = hitDebug.collider.gameObject.layer;
                    string hitLayerName = LayerMask.LayerToName(hitLayer);
                    Debug.LogWarning($"[GestoreIncollaggio-Debug] Raycast ha mancato il layerRestauro ma ha colpito '{hitDebug.collider.name}' al layer '{hitLayerName}' (valore: {hitLayer}). Verifica il LayerMask di GestoreIncollaggio!");
                }
            }
        }
    }

    private void PitturaColla(Vector2 uv)
    {
        if (mascheraCollaUnica == null || collaTextureInstance == null || pixelCollaNecessari == null) return;

        int width = collaTextureInstance.width;
        int height = collaTextureInstance.height;

        int pixelX = (int)(uv.x * width);
        int pixelY = (int)(uv.y * height);

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

                Debug.Log($"[GestoreIncollaggio] Progresso Applicazione Colla: {pixelCollaDipinti}/{totPixelCollaNecessari} ({progressioneColla * 100f:F1}%)");

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

    private System.Collections.IEnumerator SequenzaCompletamento()
    {
        isIncollaggioActive = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        // 1. Vibrazione della mesh dell'anfora
        float duration = 0.6f;
        float magnitude = 0.012f;
        Vector3 originalPos = anforaAssemblata != null ? anforaAssemblata.transform.position : Vector3.zero;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (anforaAssemblata != null)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                float z = Random.Range(-1f, 1f) * magnitude;
                anforaAssemblata.transform.position = originalPos + new Vector3(x, y, z);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (anforaAssemblata != null)
        {
            anforaAssemblata.transform.position = originalPos;
        }

        // 2. Sostituzione visiva con il vaso intero (pulito, senza colla e senza terra)
        Debug.Log("[GestoreIncollaggio] Completamento - Disattivazione colla e terra, sostituzione con il vaso intero.");

        // Imposta globalmente _mostraColla=0, _mostraTerra=0, _mostraPittura=1
        Shader.SetGlobalFloat(idMostraTerra, 0f);
        Shader.SetGlobalFloat(idMostraColla, 0f);
        Shader.SetGlobalFloat(idMostraPittura, 1f);

        // Rimuovi la mappa della colla
        RimuoviMappaColla();

        GameObject prefabIntero = tavoloCorrente.vaschettaCorrente.prefabAnforaIntera;
        if (prefabIntero != null && anforaAssemblata != null)
        {
            Vector3 pos = anforaAssemblata.transform.position;
            Quaternion rot = anforaAssemblata.transform.rotation;
            Vector3 scale = anforaAssemblata.transform.localScale;
            Transform parent = anforaAssemblata.transform.parent;

            // Distrugge la Geometry Collection o l'anfora assemblata temporanea
            Destroy(anforaAssemblata);
            anforaAssemblata = null;

            // Spawna il prefab dell'anfora intera al suo posto
            GameObject anforaIntera = Instantiate(prefabIntero, pos, rot, parent);
            anforaIntera.name = prefabIntero.name;
            anforaIntera.transform.localScale = Vector3.one;

            // Aggiunge la componente tag OggettoRestaurato per identificarla come restaurata
            if (anforaIntera.GetComponent<OggettoRestaurato>() == null)
            {
                anforaIntera.AddComponent<OggettoRestaurato>();
            }

            // Configura PickUp_Interaction per svuotare il tavolo al raccoglimento
            if (anforaIntera.TryGetComponent<PickUp_Interaction>(out var pickUp))
            {
                pickUp.ImpostaTavolo(tavoloCorrente);
            }
            else
            {
                var childPickUp = anforaIntera.GetComponentInChildren<PickUp_Interaction>();
                if (childPickUp != null)
                {
                    childPickUp.ImpostaTavolo(tavoloCorrente);
                }
            }

            // Configura i materiali dell'anfora intera per sicurezza
            foreach (var r in anforaIntera.GetComponentsInChildren<Renderer>())
            {
                Material[] mats = r.materials;
                bool modified = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null)
                    {
                        if (mats[i].HasProperty(idMostraTerra))
                        {
                            mats[i].SetFloat(idMostraTerra, 0f);
                            modified = true;
                        }
                        if (mats[i].HasProperty(idMostraColla))
                        {
                            mats[i].SetFloat(idMostraColla, 0f);
                            modified = true;
                        }
                        if (mats[i].HasProperty(idMostraPittura))
                        {
                            mats[i].SetFloat(idMostraPittura, 1f);
                            modified = true;
                        }
                    }
                }
                if (modified)
                {
                    r.materials = mats;
                }
            }
        }
        else
        {
            Debug.LogWarning("[GestoreIncollaggio] prefabAnforaIntera non assegnato nel VaschettaSO.");
        }

        // Attesa finale per dare feedback visivo
        yield return new WaitForSeconds(0.4f);

        // 3. Ferma l'interazione nel RestoreManager (sblocca il player, ripristina la camera)
        if (restoreManager != null)
        {
            restoreManager.CompletaRestauro();
            restoreManager.StopInteraction();
        }
        else
        {
            Debug.LogWarning("[GestoreIncollaggio] RestoreManager non trovato.");
        }

        // 4. Notifica il completamento
        onIncollaggioCompletato?.Invoke();
    }

    private void RimuoviMappaColla()
    {
        if (collaTextureInstance != null)
        {
            Destroy(collaTextureInstance);
            collaTextureInstance = null;
        }
        SincronizzaTextureColla(null);
    }

    private void SincronizzaTextureColla(Texture2D texture)
    {
        if (anforaAssemblata == null) return;

        foreach (var r in anforaAssemblata.GetComponentsInChildren<Renderer>())
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

    private void ConfiguraMaterialiAnfora()
    {
        if (anforaAssemblata == null) return;

        Debug.Log("[GestoreIncollaggio] ConfiguraMaterialiAnfora — SetGlobalFloat: _mostraTerra=0, _mostraColla=1, _mostraPittura=1");
        Shader.SetGlobalFloat(idMostraTerra,   0f);
        Shader.SetGlobalFloat(idMostraColla,   1f);
        Shader.SetGlobalFloat(idMostraPittura, 1f);

        if (mascheraCollaUnica == null)
        {
            Debug.LogWarning("[GestoreIncollaggio] ConfiguraMaterialiAnfora — mascheraCollaUnica è NULL");
            return;
        }

        foreach (var r in anforaAssemblata.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = r.materials;
            bool modified = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null && mats[i].HasProperty(idProprietaMascheraColla))
                {
                    mats[i].SetTexture(idProprietaMascheraColla, mascheraCollaUnica);
                    modified = true;
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
        Debug.Log("[GestoreIncollaggio] Camera transition completed. Glue painting is now enabled.");
    }
}
