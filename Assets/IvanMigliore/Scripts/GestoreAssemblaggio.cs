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
    [SerializeField] private FaseRestauroSO faseCheTriggeraAssemblaggio;
    [SerializeField] private Camera cameraRestauro;

    [Header("Spawn Anfora Centrale")]
    [SerializeField] private Transform puntoSpawnAnfora;

    [Header("Parametri Assemblaggio")]
    [SerializeField] private float snapDistance = 0.08f;
    [SerializeField] private float snapAngle = 25f;
    [SerializeField] private float velocitaRotazione = 100f;

    [Header("Impostazioni Pennello Colla")]
    [SerializeField] private int rangePennelloColla = 10;
    [SerializeField] private string nomeProprietaMascheraColla = "_MascheraColla";
    [SerializeField] private string nomeProprietaCollaDipingibile = "_CollaDipingibile";
    [SerializeField] private Color32 coloreGuidaColla = new Color32(255, 255, 255, 45);
    [SerializeField] private Color32 coloreCollaApplicata = new Color32(245, 210, 60, 220);

    [Header("Eventi")]
    public UnityEvent onAssemblaggioCompletato;

    private GameObject ghostAnfora;
    private List<PezzoInfo> listaPezzi = new List<PezzoInfo>();
    private bool isAssemblaggioActive = false;
    private int currentPezzoIndex = 0;

    // Stato di Drag & Drop
    private PezzoInfo pezzoTrascinato;
    private Plane dragPlane;
    private Vector3 dragOffset;

    // Stato della colla
    private Texture2D collaTextureInstance;
    private Color32[] coloreTextureColla;
    private bool[] pixelCollaNecessari;
    private bool[] pixelCollaMappati;
    private int totPixelCollaNecessari;
    private int pixelCollaDipinti;
    private float progressioneColla = 0f;
    private bool isAreaGlued = false;
    private Texture2D mascheraCollaUnica;
    private int idProprietaMascheraColla;
    private int idProprietaCollaDipingibile;

    private void Awake()
    {
        idProprietaMascheraColla = Shader.PropertyToID(nomeProprietaMascheraColla);
        idProprietaCollaDipingibile = Shader.PropertyToID(nomeProprietaCollaDipingibile);
    }

    private void OnEnable()
    {
        if (tavoloCorrente != null)
            tavoloCorrente.OnFaseCambiata += OnFaseCambiata;
    }

    private void OnDisable()
    {
        if (tavoloCorrente != null)
            tavoloCorrente.OnFaseCambiata -= OnFaseCambiata;
    }

    private void OnFaseCambiata(FaseRestauroSO fase)
    {
        if (fase != faseCheTriggeraAssemblaggio)
        {
            TerminaAssemblaggio();
            return;
        }

        IniziaAssemblaggio();
    }

    private void IniziaAssemblaggio()
    {
        if (tavoloCorrente.vaschettaCorrente == null || tavoloCorrente.vaschettaGameObject == null) return;

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
        GameObject vaschettaGO = tavoloCorrente.vaschettaGameObject;

        if (prefabPezzi != null && vaschettaGO != null)
        {
            ConfigurazioneVaschetta config = vaschettaGO.GetComponent<ConfigurazioneVaschetta>();
            if (config == null)
            {
                Debug.LogError($"[GestoreAssemblaggio] Manca il componente ConfigurazioneVaschetta sul GameObject della vaschetta '{vaschettaGO.name}'!");
                return;
            }

            mascheraCollaUnica = config.mascheraCollaUnica;
            if (mascheraCollaUnica != null)
            {
                ImpostaMascheraCollaSuMateriali(mascheraCollaUnica);
            }

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
                    }
                    pezzo.collider = col;

                    listaPezzi.Add(pezzo);
                }
                else
                {
                    Debug.LogWarning($"[GestoreAssemblaggio] Non è stato possibile trovare il pezzo '{goPezzo.name}' nel prefabPezzi.");
                }
            }
        }

        currentPezzoIndex = 0;
        isAssemblaggioActive = true;

        if (mascheraCollaUnica != null)
        {
            InizializzaMappaCollaUnica(mascheraCollaUnica);
        }

        AggiornaPezzoAttivo();
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
        if (cameraRestauro == null || Mouse.current == null) return;

        bool isLeftPressed = Mouse.current.leftButton.isPressed;
        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (currentPezzoIndex >= listaPezzi.Count) return;
        PezzoInfo activePezzo = listaPezzi[currentPezzoIndex];

        if (isLeftPressed)
        {
            if (pezzoTrascinato == null)
            {
                Ray ray = cameraRestauro.ScreenPointToRay(mousePos);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                {
                    if (hit.collider.gameObject == activePezzo.gameObject)
                    {
                        if (currentPezzoIndex == 0 || isAreaGlued)
                        {
                            pezzoTrascinato = activePezzo;
                            dragPlane = new Plane(-cameraRestauro.transform.forward, hit.point);
                            dragOffset = pezzoTrascinato.gameObject.transform.position - hit.point;
                        }
                        else
                        {
                            Debug.Log("[GestoreAssemblaggio] Devi prima applicare la colla sull'anfora!");
                        }
                    }
                    else if (ghostAnfora != null && hit.collider.transform.IsChildOf(ghostAnfora.transform))
                    {
                        if (currentPezzoIndex > 0 && !isAreaGlued)
                        {
                            PitturaColla(hit.textureCoord);
                        }
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

                    VerificaSnap(pezzoTrascinato);
                }
            }
        }
        else
        {
            if (pezzoTrascinato != null)
            {
                pezzoTrascinato = null;
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
                            coloreTextureColla[indice] = coloreCollaApplicata;
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

                if (progressioneColla >= 0.90f && !isAreaGlued)
                {
                    isAreaGlued = true;
                    Debug.Log("[GestoreAssemblaggio] Area incollata! Ora puoi posizionare il pezzo.");
                }
            }
        }
    }

    private void InizializzaMappaCollaUnica(Texture2D maschera)
    {
        int width = maschera.width;
        int height = maschera.height;

        collaTextureInstance = new Texture2D(width, height, TextureFormat.RGBA32, false);
        coloreTextureColla = new Color32[width * height];

        for (int i = 0; i < coloreTextureColla.Length; i++)
        {
            coloreTextureColla[i] = new Color32(0, 0, 0, 0);
        }

        collaTextureInstance.SetPixels32(coloreTextureColla);
        collaTextureInstance.Apply();

        SincronizzaTextureColla(collaTextureInstance);
    }

    private void AttivaGuidaCollaPerPezzo(PezzoInfo pezzo)
    {
        if (mascheraCollaUnica == null || collaTextureInstance == null) return;

        MeshFilter mf = pezzo.gameObject.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            isAreaGlued = true;
            progressioneColla = 1f;
            return;
        }

        Mesh mesh = mf.sharedMesh;
        Vector2[] uvs = mesh.uv;
        int[] triangles = mesh.triangles;
        int width = collaTextureInstance.width;
        int height = collaTextureInstance.height;

        Color32[] pixelMaschera = mascheraCollaUnica.GetPixels32();

        pixelCollaNecessari = new bool[width * height];
        pixelCollaMappati = new bool[width * height];
        totPixelCollaNecessari = 0;
        pixelCollaDipinti = 0;
        isAreaGlued = false;
        progressioneColla = 0f;

        HashSet<int> pixelRilevati = new HashSet<int>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector2 uvA = uvs[triangles[i]];
            Vector2 uvB = uvs[triangles[i + 1]];
            Vector2 uvC = uvs[triangles[i + 2]];

            int minX = Mathf.FloorToInt(Mathf.Min(uvA.x, Mathf.Min(uvB.x, uvC.x)) * width);
            int maxX = Mathf.CeilToInt(Mathf.Max(uvA.x, Mathf.Max(uvB.x, uvC.x)) * width);
            int minY = Mathf.FloorToInt(Mathf.Min(uvA.y, Mathf.Min(uvB.y, uvC.y)) * height);
            int maxY = Mathf.CeilToInt(Mathf.Max(uvA.y, Mathf.Max(uvB.y, uvC.y)) * height);

            minX = Mathf.Clamp(minX, 0, width - 1);
            maxX = Mathf.Clamp(maxX, 0, width - 1);
            minY = Mathf.Clamp(minY, 0, height - 1);
            maxY = Mathf.Clamp(maxY, 0, height - 1);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    int index = y * width + x;
                    if (pixelRilevati.Contains(index)) continue;

                    Vector2 p = new Vector2((float)x / width, (float)y / height);
                    if (IsPointInTriangle(p, uvA, uvB, uvC))
                    {
                        pixelRilevati.Add(index);
                        if (pixelMaschera[index].r > 128)
                        {
                            pixelCollaNecessari[index] = true;
                            totPixelCollaNecessari++;

                            if (coloreTextureColla[index].a == 0)
                            {
                                coloreTextureColla[index] = coloreGuidaColla;
                            }
                        }
                    }
                }
            }
        }

        collaTextureInstance.SetPixels32(coloreTextureColla);
        collaTextureInstance.Apply();
        SincronizzaTextureColla(collaTextureInstance);

        Debug.Log($"[GestoreAssemblaggio] Guida colla attivata per '{pezzo.gameObject.name}'. Pixel necessari: {totPixelCollaNecessari}");
    }

    private void PulisciGuidaDopoSnap()
    {
        if (collaTextureInstance == null) return;

        for (int i = 0; i < coloreTextureColla.Length; i++)
        {
            if (coloreTextureColla[i].r == coloreGuidaColla.r &&
                coloreTextureColla[i].g == coloreGuidaColla.g &&
                coloreTextureColla[i].b == coloreGuidaColla.b &&
                coloreTextureColla[i].a == coloreGuidaColla.a)
            {
                coloreTextureColla[i] = new Color32(0, 0, 0, 0);
            }
        }

        collaTextureInstance.SetPixels32(coloreTextureColla);
        collaTextureInstance.Apply();
        SincronizzaTextureColla(collaTextureInstance);
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

    private void ImpostaMascheraCollaSuMateriali(Texture2D maskTexture)
    {
        if (ghostAnfora == null) return;

        foreach (var r in ghostAnfora.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = r.materials;
            bool modified = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null && mats[i].HasProperty(idProprietaMascheraColla))
                {
                    mats[i].SetTexture(idProprietaMascheraColla, maskTexture);
                    modified = true;
                }
            }
            if (modified)
            {
                r.materials = mats;
            }
        }
    }

    private void VerificaSnap(PezzoInfo pezzo)
    {
        if (ghostAnfora == null) return;

        Vector3 targetWorldPos = ghostAnfora.transform.TransformPoint(pezzo.originalLocalPos);
        Quaternion targetWorldRot = ghostAnfora.transform.rotation * pezzo.originalLocalRot;

        float dist = Vector3.Distance(pezzo.gameObject.transform.position, targetWorldPos);
        float angle = Quaternion.Angle(pezzo.gameObject.transform.rotation, targetWorldRot);

        if (dist <= snapDistance && angle <= snapAngle)
        {
            pezzo.isSnapped = true;
            pezzo.gameObject.transform.SetParent(ghostAnfora.transform, false);
            
            pezzo.gameObject.transform.localPosition = pezzo.originalLocalPos;
            pezzo.gameObject.transform.localRotation = pezzo.originalLocalRot;
            pezzo.gameObject.transform.localScale = pezzo.originalLocalScale;

            if (pezzo.collider != null)
                pezzo.collider.enabled = false;

            pezzoTrascinato = null;

            PulisciGuidaDopoSnap();

            currentPezzoIndex++;
            AggiornaPezzoAttivo();

            ControllaCompletamento();
        }
    }

    private void AggiornaPezzoAttivo()
    {
        if (currentPezzoIndex >= listaPezzi.Count) return;

        PezzoInfo activePezzo = listaPezzi[currentPezzoIndex];

        if (currentPezzoIndex == 0)
        {
            isAreaGlued = true;
            progressioneColla = 1f;
            Debug.Log($"[GestoreAssemblaggio] Posiziona il primo pezzo '{activePezzo.gameObject.name}' direttamente (senza colla).");
        }
        else
        {
            AttivaGuidaCollaPerPezzo(activePezzo);
        }
    }

    private void ControllaCompletamento()
    {
        bool completato = true;
        foreach (var pezzo in listaPezzi)
        {
            if (!pezzo.isSnapped)
            {
                completato = false;
                break;
            }
        }

        if (completato)
        {
            isAssemblaggioActive = false;
            RimuoviMappaColla();
            Debug.Log("[GestoreAssemblaggio] Assemblaggio completato con successo!");
            onAssemblaggioCompletato?.Invoke();
        }
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

    private bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float s = (a.x - c.x) * (p.y - c.y) - (a.y - c.y) * (p.x - c.x);
        float t = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);

        if ((s < 0) != (t < 0) && s != 0 && t != 0)
            return false;

        float d = (c.x - b.x) * (p.y - b.y) - (c.y - b.y) * (p.x - b.x);
        return d == 0 || (d < 0) == (s + t <= 0);
    }
}
