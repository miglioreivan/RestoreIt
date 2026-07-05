using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Vector2 = UnityEngine.Vector2;

public class StrumentoPulizia : MonoBehaviour
{
    [Header("Impostazioni Telecamera e Raggio")]
    [SerializeField] private Camera cameraRestauro;
    [SerializeField] private LayerMask layerRestauro;

    [Header("Impostazioni Pennello/Spugna")]
    [SerializeField] private Texture2D mascheraSporcoOriginale;
    [SerializeField, Range(5, 15)] private int rangePaintbrush;
    [SerializeField] private Texture2D cursorTexture;
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;

    [Header("Progressione Restauro")]
    [SerializeField] private TavoloSO tavoloCorrente;
    [SerializeField] private FaseRestauroSO faseSuccessiva;
    public float progression = 0f;
    public UnityEvent<VaschettaSO> eventoPuliziaCompletata;

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
                                if (hitMat != null && hitMat.HasProperty(idMascheraSporco))
                                {
                                    Vector2 uv = hit.textureCoord;
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
        Renderer[] tuttiIRenderer = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Debug.Log($"[StrumentoPulizia] SincronizzaRenderers - Trovati {tuttiIRenderer.Length} renderer totali nella scena.");

        int renderersModificati = 0;

        foreach (Renderer rend in tuttiIRenderer)
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
                if (sharedMats[i] != null && sharedMats[i].HasProperty(idMascheraSporco))
                {
                    if (instanceMats == null) instanceMats = rend.materials;
                    instanceMats[i].SetTexture(idMascheraSporco, textureInstance);

                    if (instanceMats[i].HasProperty(idMostraTerra))
                        instanceMats[i].SetFloat(idMostraTerra, 1f);
                    if (instanceMats[i].HasProperty(idMostraColla))
                        instanceMats[i].SetFloat(idMostraColla, 0f);
                    if (instanceMats[i].HasProperty(idMostraPittura))
                        instanceMats[i].SetFloat(idMostraPittura, 0f);

                    Debug.Log($"[StrumentoPulizia] SincronizzaRenderers - Assegnata textureInstance a '{rend.name}' materiale[{i}]: '{sharedMats[i].name}'. Impostato _mostraTerra=1, _mostraColla=0, _mostraPittura=0");
                    modified = true;
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
        Renderer[] tuttiIRenderer = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int modificati = 0;

        foreach (Renderer rend in tuttiIRenderer)
        {
            if ((layerRestauro.value & (1 << rend.gameObject.layer)) == 0) continue;

            Material[] sharedMats = rend.sharedMaterials;
            Material[] instanceMats = null;
            bool modified = false;

            for (int i = 0; i < sharedMats.Length; i++)
            {
                if (sharedMats[i] != null && sharedMats[i].HasProperty(idMascheraSporco))
                {
                    if (instanceMats == null) instanceMats = rend.materials;

                    if (instanceMats[i].HasProperty(idMostraTerra))
                        instanceMats[i].SetFloat(idMostraTerra, 0f);
                    if (instanceMats[i].HasProperty(idMostraColla))
                        instanceMats[i].SetFloat(idMostraColla, 1f);
                    if (instanceMats[i].HasProperty(idMostraPittura))
                        instanceMats[i].SetFloat(idMostraPittura, 1f);

                    Debug.Log($"[StrumentoPulizia] ImpostaFaseFinePuliziaSuMateriali - Aggiornato '{rend.name}' materiale[{i}]: '{sharedMats[i].name}'. Impostato _mostraTerra=0, _mostraColla=1, _mostraPittura=1");
                    modified = true;
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
        Vector2 posizioneMouse = Mouse.current.position.ReadValue();
        Ray raggio = cameraRestauro.ScreenPointToRay(posizioneMouse);

        if (Physics.Raycast(raggio, out RaycastHit hitInfo, Mathf.Infinity, layerRestauro))
        {
            if (hitInfo.collider is MeshCollider && hitInfo.collider.TryGetComponent(out Renderer rend))
            {
                int submeshIndex = GetSubMeshIndex(hitInfo.collider as MeshCollider, hitInfo.triangleIndex);
                Material[] mats = rend.materials;
                if (submeshIndex >= 0 && submeshIndex < mats.Length)
                {
                    Material targetMat = mats[submeshIndex];
                    if (targetMat != null && targetMat.HasProperty(idMascheraSporco))
                    {
                        Texture currentTex = targetMat.GetTexture(idMascheraSporco);
                        if (currentTex != textureInstance)
                        {
                            Debug.LogWarning($"[StrumentoPulizia] UseBrush - Trovata texture discrepanza su '{rend.name}' materiale[{submeshIndex}]. Era '{currentTex?.name ?? "NULL"}', imposto '{textureInstance.name}'");
                            targetMat.SetTexture(idMascheraSporco, textureInstance);
                            rend.materials = mats;
                        }

                        Vector2 coordinataUV = hitInfo.textureCoord;
                        int pixelCentroX = (int)(coordinataUV.x * textureRes);
                        int pixelCentroY = (int)(coordinataUV.y * textureRes);

                        PitturaPixel(pixelCentroX, pixelCentroY);
                    }
                }
            }
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

            if (progression >= 0.95f)
            {
                Debug.Log("[StrumentoPulizia] Progresso raggiunto 95%! Completamento minigioco.");
                minigiocoFinito = true;
                progression = 1f;
                ResetMouseCursor();
                
                ImpostaFaseFinePuliziaSuMateriali();

                tavoloCorrente?.AvanzaFase(faseSuccessiva);
                eventoPuliziaCompletata?.Invoke(tavoloCorrente?.vaschettaCorrente);
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
}