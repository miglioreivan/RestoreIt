using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Vector2 = UnityEngine.Vector2;

public class StrumentoPulizia : MonoBehaviour
{
    [Header("Impostazioni Telecamera e Raggio")]
    [SerializeField] private Camera cameraRestauro;
    [SerializeField] private LayerMask layerRestauro;

    [Header("Impostazioni Pennello/Spugna")]
    [SerializeField] private Texture2D mascheraSporcoOriginale;
    [SerializeField] private int rangePaintbrush = 30;
    [SerializeField] private Texture2D cursorTexture;
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;

    [Header("Progressione Restauro")]
    [SerializeField] private TavoloSO tavoloCorrente;
    [SerializeField] private FaseRestauroSO faseSuccessiva;
    [SerializeField] private float progression = 0f;
    public float Progression => progression;
    [SerializeField] [Range(0.1f, 1f)] private float sogliaCompletamentoPulizia = 0.95f;
    [SerializeField] private UnityEvent<VaschettaSO> eventoPuliziaCompletata;

    [System.Serializable]
    public class UnityEventDatiRestauro : UnityEvent<DatiOggettoSO> { }
    [SerializeField] private UnityEventDatiRestauro eventoRestauroCompletato;



    private Texture2D textureInstance;
    private Color32[] texture32;

    private int totPixel = 0;
    private int textureRes = 1024;

    private int pixelPainted = 0;
    private bool minigiocoFinito = true;

    private readonly int idMascheraSporco = Shader.PropertyToID("_MascheraSporco");
    private readonly int idMostraTerra   = Shader.PropertyToID("_mostraTerra");
    private readonly int idMostraColla   = Shader.PropertyToID("_mostraColla");
    private readonly int idMostraPittura = Shader.PropertyToID("_mostraPittura");

    private void Awake()
    {
        if (cameraRestauro == null)
        {
            cameraRestauro = GetComponentInChildren<Camera>(true);
            if (cameraRestauro == null)
            {
                cameraRestauro = GetComponentInParent<Camera>(true);
            }
        }

        Debug.Log($"[StrumentoPulizia] Awake - mascheraSporcoOriginale: {(mascheraSporcoOriginale != null ? mascheraSporcoOriginale.name : "NULL")}");
        if (mascheraSporcoOriginale != null)
        {
            Debug.Log($"[StrumentoPulizia] Awake - Formato originale: {mascheraSporcoOriginale.format}, graphicsFormat: {mascheraSporcoOriginale.graphicsFormat}");
            
            // Crea una copia virtuale non compressa a runtime usando RenderTexture e Graphics.Blit
            // per generare una copia perfettamente leggibile e scrivibile.
            textureInstance = CopiaTextureInRGBA32(mascheraSporcoOriginale);
            
            if (textureInstance != null)
            {
                textureRes = textureInstance.width;
                texture32 = textureInstance.GetPixels32();
                Debug.Log($"[StrumentoPulizia] Awake - Copia RGBA32 creata con successo. Nome: {textureInstance.name}, Risoluzione: {textureRes}x{textureRes}, Pixel: {texture32.Length}");
            }
            else
            {
                Debug.LogError("[StrumentoPulizia] Awake - FALLITA la creazione della copia scrivibile via Blit!");
            }
        }
    }

    /// <summary>
    /// Crea una copia virtuale non compressa a runtime (textureInstance) usando
    /// RenderTexture e Graphics.Blit per generare una copia perfettamente leggibile e scrivibile.
    /// </summary>
    private Texture2D CopiaTextureInRGBA32(Texture2D sorgente)
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

    public void CountVisiblePixel()
    {
        Debug.Log("[StrumentoPulizia] CountVisiblePixel avviato.");
        ConfiguraCollidersMosaico(true);
        SincronizzaRenderers();

        totPixel = 0;
        if (texture32 == null)
        {
            Debug.LogError("[StrumentoPulizia] CountVisiblePixel - texture32 è NULL!");
            return;
        }

        bool[] pixelRaggiungibili = new bool[texture32.Length];

        int risoluzioneScansione = 512;
        int raggioPrecisioneTelecamera = Mathf.CeilToInt((float)textureRes / risoluzioneScansione);

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
                                        Vector2 uv = WrapUV(hit.textureCoord);
                                        int pixelX = (int)(uv.x * textureRes);
                                        int pixelY = (int)(uv.y * textureRes);
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

        Debug.Log($"[StrumentoPulizia] Scansione completata. Raycast lanciati: {raycastLanciati}, Colpiti: {raycastColpiti}. Pixel sporchi visibili totali calcolati: {totPixel}");
    }
    private void SincronizzaRenderers()
    {
        Debug.Log("[StrumentoPulizia] SincronizzaRenderers avviato.");
        if (tavoloCorrente == null)
        {
            Debug.LogError("[StrumentoPulizia] SincronizzaRenderers - tavoloCorrente è NULL!");
            return;
        }
        if (tavoloCorrente.vaschettaGameObject == null)
        {
            Debug.LogError("[StrumentoPulizia] SincronizzaRenderers - vaschettaGameObject è NULL!");
            return;
        }

        GameObject activeObj = tavoloCorrente.vaschettaGameObject;
        Debug.Log($"[StrumentoPulizia] SincronizzaRenderers - Oggetto attivo sul tavolo: '{activeObj.name}'");

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
                    Debug.Log($"[StrumentoPulizia] SincronizzaRenderers - Trovato Plane del tavolo '{current.name}', aggiunto alla lista.");
                    break;
                }
            }
            if (current.parent == null) break;
            current = current.parent;
        }

        Debug.Log($"[StrumentoPulizia] SincronizzaRenderers - Trovati {list.Count} renderer da controllare.");
        int renderersModificati = 0;

        foreach (Renderer rend in list)
        {
            int layerDelRenderer = rend.gameObject.layer;
            int layerMaskValue = layerRestauro.value;
            bool layerMatches = (layerMaskValue & (1 << layerDelRenderer)) != 0;

            Debug.Log($"[StrumentoPulizia-Debug-Sincronizza] Controllo Renderer '{rend.name}' (Layer: {LayerMask.LayerToName(layerDelRenderer)}, Compatibile: {layerMatches})");

            if (!layerMatches) continue;

            Material[] sharedMats = rend.sharedMaterials;
            Material[] instanceMats = null;
            bool modified = false;

            for (int i = 0; i < sharedMats.Length; i++)
            {
                if (sharedMats[i] != null)
                {
                    bool daModificare = sharedMats[i].HasProperty(idMascheraSporco);
                    Debug.Log($"[StrumentoPulizia-Debug-Sincronizza] Materiale '{sharedMats[i].name}' submesh {i} ha '_MascheraSporco'? {daModificare}");

                    if (daModificare)
                    {
                        if (instanceMats == null) instanceMats = rend.materials;
                        instanceMats[i].SetTexture(idMascheraSporco, textureInstance);

                        bool haMostraTerra = instanceMats[i].HasProperty(idMostraTerra);
                        bool haMostraColla = instanceMats[i].HasProperty(idMostraColla);
                        bool haMostraPittura = instanceMats[i].HasProperty(idMostraPittura);

                        Debug.Log($"[StrumentoPulizia-Debug-Sincronizza] Impostazione parametri su '{sharedMats[i].name}': haMostraTerra={haMostraTerra}, haMostraColla={haMostraColla}, haMostraPittura={haMostraPittura}");

                        if (haMostraTerra)
                            instanceMats[i].SetFloat(idMostraTerra, 1f);
                        if (haMostraColla)
                            instanceMats[i].SetFloat(idMostraColla, 0f);
                        if (haMostraPittura)
                            instanceMats[i].SetFloat(idMostraPittura, 0f);

                        modified = true;
                    }
                }
            }

            if (modified && instanceMats != null)
            {
                rend.materials = instanceMats;
                renderersModificati++;
            }
        }

        Debug.Log($"[StrumentoPulizia] SincronizzaRenderers completato. Renderer modificati: {renderersModificati}");
    }

    private void ImpostaFaseFinePuliziaSuMateriali()
    {
        Debug.Log("[StrumentoPulizia] ImpostaFaseFinePuliziaSuMateriali avviato.");
        
        if (tavoloCorrente == null || tavoloCorrente.vaschettaGameObject == null)
        {
            Debug.LogError("[StrumentoPulizia] ImpostaFaseFinePuliziaSuMateriali - tavoloCorrente o vaschettaGameObject è NULL!");
            return;
        }

        GameObject activeObj = tavoloCorrente.vaschettaGameObject;
        Debug.Log($"[StrumentoPulizia] ImpostaFaseFinePuliziaSuMateriali - Oggetto attivo sul tavolo: '{activeObj.name}'");

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
                    Debug.Log($"[StrumentoPulizia] ImpostaFaseFinePuliziaSuMateriali - Trovato Plane del tavolo '{current.name}', aggiunto alla lista.");
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

            Material[] sharedMats = rend.sharedMaterials;
            Material[] instanceMats = null;
            bool modified = false;

            for (int i = 0; i < sharedMats.Length; i++)
            {
                if (sharedMats[i] != null)
                {
                    bool daModificare = sharedMats[i].HasProperty(idMascheraSporco);
                    if (daModificare)
                    {
                        if (instanceMats == null) instanceMats = rend.materials;

                        bool haMostraTerra = instanceMats[i].HasProperty(idMostraTerra);
                        bool haMostraColla = instanceMats[i].HasProperty(idMostraColla);
                        bool haMostraPittura = instanceMats[i].HasProperty(idMostraPittura);

                        Debug.Log($"[StrumentoPulizia-Debug-FinePulizia] Impostazione parametri fine pulizia su '{sharedMats[i].name}': haMostraTerra={haMostraTerra}, haMostraColla={haMostraColla}, haMostraPittura={haMostraPittura}");

                        if (haMostraTerra)
                            instanceMats[i].SetFloat(idMostraTerra, 0f);
                        if (haMostraColla)
                            instanceMats[i].SetFloat(idMostraColla, 1f);
                        if (haMostraPittura)
                            instanceMats[i].SetFloat(idMostraPittura, 1f);

                        modified = true;
                    }
                }
            }

            if (modified && instanceMats != null)
            {
                rend.materials = instanceMats;
                modificati++;
            }
        }
        Debug.Log($"[StrumentoPulizia] ImpostaFaseFinePuliziaSuMateriali completato. Renderer aggiornati: {modificati}");
    }

    private void SegnaAreaVisibile(int centroX, int centroY, bool[] mappa, int raggioCalcolo)
    {
        for (int x = -raggioCalcolo; x <= raggioCalcolo; x++)
        {
            for (int y = -raggioCalcolo; y <= raggioCalcolo; y++)
            {
                int targetX = centroX + x;
                int targetY = centroY + y;

                if (targetX >= 0 && targetX < textureRes && targetY >= 0 && targetY < textureRes)
                    mappa[targetY * textureRes + targetX] = true;
            }
        }
    }

    private void Update()
    {
        if (minigiocoFinito) return;
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.isPressed)
        {
            UseBrush();
        }
    }

    private void UseBrush()
    {
        if (cameraRestauro == null)
        {
            Debug.LogError("[StrumentoPulizia] UseBrush fallito: cameraRestauro è NULL!");
            return;
        }

        Vector2 posizioneMouse = Mouse.current.position.ReadValue();
        Ray raggio = cameraRestauro.ScreenPointToRay(posizioneMouse);
        
        // Disegna il raggio nella Scene View per debug visivo (linea rossa che dura 1 secondo)
        Debug.DrawRay(raggio.origin, raggio.direction * 10f, Color.red, 1f);

        // Debug: proviamo prima a lanciare il raycast senza layermask per vedere cosa incontriamo sul percorso
        if (Physics.Raycast(raggio, out RaycastHit hitGenerico, Mathf.Infinity))
        {
            int layerDelRilevato = hitGenerico.collider.gameObject.layer;
            bool layerCompatibile = (layerRestauro.value & (1 << layerDelRilevato)) != 0;
            
            Debug.Log($"[StrumentoPulizia-Debug] Raycast Generico COLPITO: '{hitGenerico.collider.name}' al layer {LayerMask.LayerToName(layerDelRilevato)} (Compatibile con layerRestauro: {layerCompatibile})");
        }
        else
        {
            Debug.LogWarning("[StrumentoPulizia-Debug] Raycast Generico NON HA COLPITO NULLA nella scena!");
        }

        // Ora eseguiamo il raycast ufficiale con il filtro layermask
        if (Physics.Raycast(raggio, out RaycastHit hitInfo, Mathf.Infinity, layerRestauro))
        {
            Debug.Log($"[StrumentoPulizia-Debug] Raycast Ufficiale (layerRestauro) COLPITO: '{hitInfo.collider.name}'");

            if (!(hitInfo.collider is MeshCollider))
            {
                Debug.LogWarning($"[StrumentoPulizia-Debug] Il collider '{hitInfo.collider.name}' NON è un MeshCollider! La pulizia richiede necessariamente un MeshCollider per le coordinate UV.");
            }

            if (hitInfo.collider.TryGetComponent(out Renderer rend))
            {
                if (hitInfo.collider is MeshCollider mc)
                {
                    int submeshIndex = GetSubMeshIndex(mc, hitInfo.triangleIndex);
                    Material[] sharedMats = rend.sharedMaterials;
                    
                    Debug.Log($"[StrumentoPulizia-Debug] Trovato Renderer su '{rend.name}'. Submesh colpita: {submeshIndex}, Materiali totali: {sharedMats.Length}");

                    if (submeshIndex >= 0 && submeshIndex < sharedMats.Length)
                    {
                        Material sharedMat = sharedMats[submeshIndex];
                        if (sharedMat != null)
                        {
                            bool daModificare = sharedMat.HasProperty(idMascheraSporco);
                            Debug.Log($"[StrumentoPulizia-Debug] Materiale '{sharedMat.name}' possiede la proprietà '_MascheraSporco'? {daModificare}");

                            if (daModificare)
                            {
                                Material[] mats = rend.materials;
                                Material targetMat = mats[submeshIndex];
                                if (targetMat != null)
                                {
                                    Texture currentTex = targetMat.GetTexture(idMascheraSporco);
                                    if (currentTex != textureInstance)
                                    {
                                        Debug.LogWarning($"[StrumentoPulizia] UseBrush - Trovata texture discrepanza su '{rend.name}' materiale[{submeshIndex}]. Era '{currentTex?.name ?? "NULL"}', imposto '{textureInstance.name}'");
                                        targetMat.SetTexture(idMascheraSporco, textureInstance);
                                        rend.materials = mats;
                                    }

                                    Vector2 coordinataUV = WrapUV(hitInfo.textureCoord);
                                    int pixelCentroX = (int)(coordinataUV.x * textureRes);
                                    int pixelCentroY = (int)(coordinataUV.y * textureRes);
                                    
                                    // Log di debug con UV grezza e UV wrappata
                                    Debug.Log($"[StrumentoPulizia-Debug] Pitturazione UV Grezza: ({hitInfo.textureCoord.x:F3}, {hitInfo.textureCoord.y:F3}) -> UV Wrappata: ({coordinataUV.x:F3}, {coordinataUV.y:F3}) -> Pixel: ({pixelCentroX}, {pixelCentroY})");
                                    PitturaPixel(pixelCentroX, pixelCentroY);
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError($"[StrumentoPulizia-Debug] Submesh index {submeshIndex} fuori range per '{rend.name}' (sharedMaterials.Length = {sharedMats.Length})");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[StrumentoPulizia-Debug] Nessun Renderer trovato sul GameObject '{hitInfo.collider.name}'!");
            }
        }
        else
        {
            Debug.LogWarning("[StrumentoPulizia-Debug] Raycast Ufficiale (layerRestauro) non ha colpito nulla.");
        }
    }

    private void PitturaPixel(int centroX, int centroY)
    {
        bool textureModificata = false;
        int pixelCancellatiInQuestoColpo = 0;

        for (int x = -rangePaintbrush; x <= rangePaintbrush; x++)
        {
            for (int y = -rangePaintbrush; y <= rangePaintbrush; y++)
            {
                if (x * x + y * y <= rangePaintbrush * rangePaintbrush)
                {
                    int targetX = centroX + x;
                    int targetY = centroY + y;

                    if (targetX >= 0 && targetX < textureRes && targetY >= 0 && targetY < textureRes)
                    {
                        int indice = targetY * textureRes + targetX;
                        Color32 p = texture32[indice];

                        // Se il pixel è sporco (RGB o Alpha superiori a 12), lo puliamo
                        if (p.r > 12 || p.g > 12 || p.b > 12)
                        {
                            // Sostituiamo con un pixel nero opaco (0, 0, 0, 255) per le massime prestazioni a runtime
                            texture32[indice] = new Color32(0, 0, 0, 255);
                            pixelPainted++;
                            pixelCancellatiInQuestoColpo++;
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

            if (totPixel > 0)
                progression = Mathf.Clamp01((float)pixelPainted / totPixel);

            Debug.Log($"[StrumentoPulizia] PitturaPixel a ({centroX}, {centroY}) - Cancellati: {pixelCancellatiInQuestoColpo}. Progressi: {pixelPainted}/{totPixel} ({progression * 100:F1}%)");

            if (progression >= sogliaCompletamentoPulizia)
            {
                Debug.Log($"[StrumentoPulizia] Progresso raggiunto {sogliaCompletamentoPulizia * 100f:F1}%! Completamento minigioco.");
                minigiocoFinito = true;
                progression = 1f;
                ResetMouseCursor();
                
                ImpostaFaseFinePuliziaSuMateriali();

                ConfiguraCollidersMosaico(false);

                tavoloCorrente?.AvanzaFase(faseSuccessiva);
                eventoPuliziaCompletata?.Invoke(tavoloCorrente?.vaschettaCorrente);
                eventoRestauroCompletato?.Invoke(tavoloCorrente?.oggettoCorrente);
            }
        }
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
        Debug.Log("[StrumentoPulizia] IniziaMinigame - Minigioco attivato!");
    }

    public void TerminaMinigame()
    {
        minigiocoFinito = true;
        Debug.Log("[StrumentoPulizia] TerminaMinigame - Minigioco disattivato manualmente!");
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
            // Disabilita l'interazione pick-up
            if (obj.TryGetComponent<PickUp_Interaction>(out var pickup))
            {
                pickup.enabled = false;
            }
            else
            {
                var childPickup = obj.GetComponentInChildren<PickUp_Interaction>(true);
                if (childPickup != null) childPickup.enabled = false;
            }

            // Disabilita tutti i collider non MeshCollider e assicura che tutti i MeshCollider siano abilitati
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
            // Ripristina l'interazione e abilita tutti i collider
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
    
    private Vector2 WrapUV(Vector2 uv)
    {
        uv.x = uv.x - Mathf.Floor(uv.x);
        uv.y = uv.y - Mathf.Floor(uv.y);
        return uv;
    }
}