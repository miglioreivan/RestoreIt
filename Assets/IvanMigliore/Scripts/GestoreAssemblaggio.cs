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
    [SerializeField] private Camera cameraRestauro;

    [Header("Spawn Anfora Centrale")]
    [SerializeField] private Transform puntoSpawnAnfora;

    [Header("Parametri Assemblaggio")]
    [SerializeField] private float snapDistance = 0.08f;
    [SerializeField] private float snapAngle = 25f;
    [SerializeField] private float velocitaRotazione = 100f;

    [Header("Eventi")]
    public UnityEvent onAssemblaggioCompletato;
    
    [Header("Configurazioni Pennello e Soglie")]
    [SerializeField] private int rangePennelloColla = 15;
    [SerializeField] [Range(0.1f, 1f)] private float sogliaCompletamentoColla = 0.70f;
    private string nomeProprietaMascheraColla = "_mascheraColla";
    private string nomeProprietaCollaDipingibile = "_Colla";

    private enum StatoAssemblaggio
    {
        PosizionamentoPezzi,
        IncollaggioBordi,
        Completato
    }
    private StatoAssemblaggio statoCorrente = StatoAssemblaggio.PosizionamentoPezzi;

    private GameObject ghostAnfora;
    private List<PezzoInfo> listaPezzi = new List<PezzoInfo>();
    private bool isAssemblaggioActive = false;

    // Stato di Drag & Drop
    private PezzoInfo pezzoTrascinato;
    private Plane dragPlane;
    private Vector3 dragOffset;
    private Vector3 positionBeforeDrag;
    private Quaternion rotationBeforeDrag;
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
    private Texture2D mascheraCollaUnica;
    private int idProprietaMascheraColla;
    private int idProprietaCollaDipingibile;
    private int idMostraTerra;
    private int idMostraColla;
    private int idMostraPittura;

    private void Awake()
    {
        idProprietaMascheraColla = Shader.PropertyToID(nomeProprietaMascheraColla);
        idProprietaCollaDipingibile = Shader.PropertyToID(nomeProprietaCollaDipingibile);
        idMostraTerra = Shader.PropertyToID("_mostraTerra");
        idMostraColla = Shader.PropertyToID("_mostraColla");
        idMostraPittura = Shader.PropertyToID("_mostraPittura");
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

            mascheraCollaUnica = tavoloCorrente.vaschettaCorrente.mascheraCollaUnica != null 
                ? tavoloCorrente.vaschettaCorrente.mascheraCollaUnica 
                : config.mascheraCollaUnica;
            ConfiguraMaterialiAnfora();

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
        statoCorrente = StatoAssemblaggio.PosizionamentoPezzi;

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
        }
    }

    private void TerminaAssemblaggio()
    {
        isAssemblaggioActive = false;
        pezzoTrascinato = null;
        RimuoviMappaColla();

        if (ghostAnfora != null)
        {
            Destroy(ghostAnfora);
            ghostAnfora = null;
        }

        listaPezzi.Clear();
        statoCorrente = StatoAssemblaggio.PosizionamentoPezzi;
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

        if (statoCorrente == StatoAssemblaggio.PosizionamentoPezzi)
        {
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
                                
                                // Salva la posizione e la rotazione iniziale per poter fare il fallback in caso di mancato snap
                                positionBeforeDrag = pezzoTrascinato.gameObject.transform.position;
                                rotationBeforeDrag = pezzoTrascinato.gameObject.transform.localRotation;

                                // Azzera la rotazione locale (imposta 0,0,0 nell'Inspector sotto localRotation)
                                pezzoTrascinato.gameObject.transform.localRotation = Quaternion.identity;

                                // Calcola la posizione target specifica del pezzo nel mondo
                                Vector3 targetWorldPos = ghostAnfora.transform.TransformPoint(pezzoTrascinato.originalLocalPos);

                                // Calcola la profondità di questo target specifico rispetto alla camera
                                float targetDepth = Vector3.Dot(targetWorldPos - cameraRestauro.transform.position, cameraRestauro.transform.forward);
                                
                                // Crea il piano di drag perpendicolare alla camera, posizionato leggermente davanti al target specifico del pezzo (es. 0.05 unità più vicino)
                                // Questo assicura che il pezzo sia sempre renderizzato SOPRA (davanti) al suo target specifico e che non ci sia parallasse!
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

                                Debug.Log($"[GestoreAssemblaggio] Inizio drag del pezzo '{pezzoTrascinato.gameObject.name}' a profondità target: {targetDepth - 0.05f}");
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

                        // Mantiene la rotazione locale a 0,0,0 durante tutto il movimento
                        pezzoTrascinato.gameObject.transform.localRotation = Quaternion.identity;

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
                        Debug.Log($"[GestoreAssemblaggio] Rilascio senza snap. Ritorno di '{pezzoTrascinato.gameObject.name}' nella vaschetta.");
                    }
                    pezzoTrascinato = null;
                }
            }
        }
        else if (statoCorrente == StatoAssemblaggio.IncollaggioBordi)
        {
            if (isLeftPressed)
            {
                Ray ray = cameraRestauro.ScreenPointToRay(mousePos);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                {
                    if (ghostAnfora != null && hit.collider.transform.IsChildOf(ghostAnfora.transform))
                    {
                        PitturaColla(hit.textureCoord);
                    }
                }
            }
        }
    }

    private void PitturaColla(Vector2 uv)
    {
        if (mascheraCollaUnica == null || collaTextureInstance == null) return;

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

                Debug.Log($"[GestoreAssemblaggio] Progresso Applicazione Colla: {pixelCollaDipinti}/{totPixelCollaNecessari} ({progressioneColla * 100f:F1}%)");

                if (progressioneColla >= sogliaCompletamentoColla)
                {
                    ControllaCompletamento();
                }
            }
        }
    }


    private void IniziaFaseIncollaggioFinale()
    {
        if (mascheraCollaUnica == null)
        {
            Debug.LogError("[GestoreAssemblaggio] Maschera colla unica non assegnata!");
            ControllaCompletamento();
            return;
        }

        statoCorrente = StatoAssemblaggio.IncollaggioBordi;
        Debug.Log("[GestoreAssemblaggio] Tutti i pezzi sono stati posizionati! Inizia la fase di incollaggio dei bordi.");

        // Disabilita tutti i collider originali di ghostAnfora che non appartengono ai pezzi posizionati dal giocatore
        if (ghostAnfora != null)
        {
            foreach (var col in ghostAnfora.GetComponentsInChildren<Collider>(true))
            {
                bool isPezzoSnapped = false;
                foreach (var p in listaPezzi)
                {
                    if (p.gameObject == col.gameObject)
                    {
                        isPezzoSnapped = true;
                        break;
                    }
                }
                if (!isPezzoSnapped)
                {
                    col.enabled = false;
                    Debug.Log($"[GestoreAssemblaggio] Disabilitato collider originale ghost '{col.gameObject.name}'");
                }
            }
        }

        // Riabilita e configura i collider dei pezzi posizionati dal giocatore come MeshCollider per il raycast UV
        foreach (var p in listaPezzi)
        {
            if (p.gameObject != null)
            {
                Collider col = p.gameObject.GetComponent<Collider>();
                if (col != null)
                {
                    if (!(col is MeshCollider))
                    {
                        Destroy(col);
                        col = p.gameObject.AddComponent<MeshCollider>();
                    }
                    col.enabled = true;
                    Debug.Log($"[GestoreAssemblaggio] Riabilitato MeshCollider su pezzo snapped '{p.gameObject.name}'");
                }
                else
                {
                    col = p.gameObject.AddComponent<MeshCollider>();
                    col.enabled = true;
                    Debug.Log($"[GestoreAssemblaggio] Aggiunto e abilitato MeshCollider su pezzo snapped '{p.gameObject.name}'");
                }
                p.collider = col;
            }
        }

        Texture2D mascheraLeggibile = CopiaTextureCompatibile(mascheraCollaUnica);
        if (mascheraLeggibile == null)
        {
            Debug.LogError("[GestoreAssemblaggio] Impossibile creare una copia leggibile di mascheraCollaUnica!");
            ControllaCompletamento();
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
            coloreTextureColla[i] = new Color32(0, 0, 0, 0);
            if (pixelMaschera[i].r > 128)
            {
                pixelCollaNecessari[i] = true;
                totPixelCollaNecessari++;
            }
        }

        collaTextureInstance.SetPixels32(coloreTextureColla);
        collaTextureInstance.Apply();
        SincronizzaTextureColla(collaTextureInstance);

        Destroy(mascheraLeggibile);
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
        if (ghostAnfora == null) return;

        foreach (var r in ghostAnfora.GetComponentsInChildren<Renderer>())
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
        if (ghostAnfora == null) return;

        Debug.Log("[GestoreAssemblaggio] ConfiguraMaterialiAnfora — SetGlobalFloat: _mostraTerra=0, _mostraColla=1, _mostraPittura=1");
        Shader.SetGlobalFloat(idMostraTerra,   0f);
        Shader.SetGlobalFloat(idMostraColla,   1f);
        Shader.SetGlobalFloat(idMostraPittura, 1f);

        if (mascheraCollaUnica == null)
        {
            Debug.LogWarning("[GestoreAssemblaggio] ConfiguraMaterialiAnfora — mascheraCollaUnica è NULL, skip SetTexture _mascheraColla");
            return;
        }

        foreach (var r in ghostAnfora.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = r.materials;
            bool modified = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null && mats[i].HasProperty(idProprietaMascheraColla))
                {
                    mats[i].SetTexture(idProprietaMascheraColla, mascheraCollaUnica);
                    Debug.Log($"[GestoreAssemblaggio] SetTexture '_mascheraColla' su renderer '{r.name}' mat[{i}]='{mats[i].name}' con texture '{mascheraCollaUnica.name}'");
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

            if (pezzo.collider != null)
                pezzo.collider.enabled = false;

            pezzoTrascinato = null;
            Debug.Log($"[GestoreAssemblaggio] Pezzo '{pezzo.gameObject.name}' posizionato correttamente!");

            // Verifica se tutti i pezzi sono posizionati
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
                IniziaFaseIncollaggioFinale();
            }
        }
    }

    public void CameraTransitionCompleted()
    {
        cameraTransitionFinished = true;
        Debug.Log("[GestoreAssemblaggio] Camera transition completed. Drag and drop is now enabled.");
    }

    private void ControllaCompletamento()
    {
        if (statoCorrente == StatoAssemblaggio.IncollaggioBordi && progressioneColla >= sogliaCompletamentoColla)
        {
            StartCoroutine(SequenzaCompletamento());
        }
    }

    private System.Collections.IEnumerator SequenzaCompletamento()
    {
        statoCorrente = StatoAssemblaggio.Completato;
        isAssemblaggioActive = false;

        // 1. Vibrazione della mesh dell'anfora
        float duration = 0.6f;
        float magnitude = 0.012f;
        Vector3 originalPos = ghostAnfora != null ? ghostAnfora.transform.position : Vector3.zero;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (ghostAnfora != null)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                float z = Random.Range(-1f, 1f) * magnitude;
                ghostAnfora.transform.position = originalPos + new Vector3(x, y, z);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (ghostAnfora != null)
        {
            ghostAnfora.transform.position = originalPos;
        }

        // 2. Sostituzione visiva con il vaso intero (pulito, senza colla e senza terra)
        Debug.Log("[GestoreAssemblaggio] Completamento - Disattivazione colla e terra, sostituzione con il vaso intero.");

        // Imposta globalmente _mostraColla=0, _mostraTerra=0, _mostraPittura=1
        Shader.SetGlobalFloat(idMostraTerra, 0f);
        Shader.SetGlobalFloat(idMostraColla, 0f);
        Shader.SetGlobalFloat(idMostraPittura, 1f);

        // Rimuovi la mappa della colla
        RimuoviMappaColla();

        GameObject prefabIntero = tavoloCorrente.vaschettaCorrente.prefabAnforaIntera;
        if (prefabIntero != null && ghostAnfora != null)
        {
            Vector3 pos = ghostAnfora.transform.position;
            Quaternion rot = ghostAnfora.transform.rotation;
            Vector3 scale = ghostAnfora.transform.localScale;
            Transform parent = ghostAnfora.transform.parent;

            // Distrugge la Geometry Collection e tutti i pezzi snapped sotto di essa
            Destroy(ghostAnfora);
            ghostAnfora = null;

            // Spawna il prefab dell'anfora intera al suo posto
            GameObject anforaIntera = Instantiate(prefabIntero, pos, rot, parent);
            anforaIntera.name = prefabIntero.name;
            anforaIntera.transform.localScale = Vector3.one;

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
            Debug.LogWarning("[GestoreAssemblaggio] prefabAnforaIntera non assegnato nel VaschettaSO. Eseguo il fallback nascondendo gli outline.");

            // Disattiva le mesh originali del ghost (le sagome trasparenti)
            if (ghostAnfora != null)
            {
                foreach (Transform child in ghostAnfora.transform)
                {
                    bool isPezzoSnapped = false;
                    foreach (var p in listaPezzi)
                    {
                        if (p.gameObject == child.gameObject)
                        {
                            isPezzoSnapped = true;
                            break;
                        }
                    }

                    if (!isPezzoSnapped)
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }

            // Rimuovi l'outline (secondo materiale) da tutti i pezzi snapped
            foreach (var p in listaPezzi)
            {
                if (p.gameObject != null)
                {
                    Renderer r = p.gameObject.GetComponent<Renderer>();
                    if (r != null && r.materials.Length > 1)
                    {
                        r.materials = new Material[] { r.materials[0] };
                    }
                }
            }
        }

        // Attesa finale per dare feedback visivo
        yield return new WaitForSeconds(0.4f);

        // 3. Ferma l'interazione nel RestoreManager (sblocca il player, ripristina la camera)
        RestoreManager manager = FindFirstObjectByType<RestoreManager>();
        if (manager != null)
        {
            manager.StopInteraction();
        }
        else
        {
            Debug.LogWarning("[GestoreAssemblaggio] RestoreManager non trovato nella scena al completamento!");
        }

        // 4. Notifica il completamento
        onAssemblaggioCompletato?.Invoke();
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
