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

    private void Start()
    {
        textureInstance = Instantiate(mascheraSporcoOriginale);
        textureRes = textureInstance.width;
        texture32 = textureInstance.GetPixels32();
    }

    /// <summary>
    /// Scansiona il viewport con una griglia 512x512 di raycast per determinare
    /// quali pixel della texture di sporco sono visibili dalla camera.
    /// Il raggio di marcatura è calcolato dinamicamente per garantire copertura
    /// continua senza buchi in UV space: raggio = ceil(textureRes / risoluzione).
    /// </summary>
    public void CountVisiblePixel()
    {
        SincronizzaRenderers();

        totPixel = 0;
        bool[] pixelRaggiungibili = new bool[texture32.Length];

        int risoluzioneScansione = 512;
        int raggioPrecisioneTelecamera = Mathf.CeilToInt((float)textureRes / risoluzioneScansione);

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
            if (pixelRaggiungibili[i] && texture32[i].a > 12)
                totPixel++;
        }

        Debug.Log($"[StrumentoPulizia] Pixel sporchi visibili dalla camera: {totPixel}");
    }

    /// <summary>
    /// Assegna la texture istanza (modificabile a runtime) a tutti i renderer
    /// nel layer di restauro che hanno la proprietà _MascheraSporco.
    /// Usa sharedMaterials per leggere e materials per scrivere, evitando
    /// di istanziare inutilmente materiali che non richiedono la modifica.
    /// </summary>
    private void SincronizzaRenderers()
    {
        Renderer[] tuttiIRenderer = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

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
                    instanceMats[i].SetTexture(idMascheraSporco, textureInstance);
                    modified = true;
                }
            }

            if (modified && instanceMats != null)
                rend.materials = instanceMats;
        }
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
        if (minigiocoFinito || Mouse.current == null || !Mouse.current.leftButton.isPressed) return;

        UseBrush();
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
                        if (targetMat.GetTexture(idMascheraSporco) != textureInstance)
                        {
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

                        if (texture32[indice].a > 12)
                        {
                            texture32[indice] = new Color32(0, 0, 0, 0);
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

            if (totPixel > 0)
                progression = Mathf.Clamp01((float)pixelPainted / totPixel);

            if (progression >= 0.95f)
            {
                minigiocoFinito = true;
                progression = 1f;
                ResetMouseCursor();
                tavoloCorrente?.AvanzaFase(faseSuccessiva);
                eventoPuliziaCompletata?.Invoke(tavoloCorrente?.vaschettaCorrente);
            }
        }
    }

    public void SetMouseCursor()
    {
        Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
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
    }

    public void TerminaMinigame()
    {
        minigiocoFinito = true;
    }


    /// <summary>
    /// Risale alla submesh colpita dal raycast a partire dal triangleIndex globale.
    /// Unity non espone direttamente l'indice di submesh nel RaycastHit,
    /// quindi si scorrono i conteggi di triangoli di ciascuna submesh cumulativamente.
    /// Richiede che la mesh abbia Read/Write Enabled.
    /// </summary>
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