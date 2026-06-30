using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class StrumentoPulizia : MonoBehaviour
{
    [Header("Impostazioni Telecamera e Raggio")]
    [SerializeField] private Camera cameraRestauro;
    [SerializeField] private LayerMask layerRestauro;
    
    [Header("Impostazioni Pennello/Spugna")]
    [SerializeField] private Texture2D mascheraSporcoOriginale;
    [SerializeField] private int raggioAzionePennello = 15;
    
    [Header("Progressione Restauro")]
    [Tooltip("Percentuale da 0.0 a 1.0 (ottima per le barre UI)")]
    public float percentualeCompletamento = 0f;
    public UnityEvent eventoPuliziaCompletata;

    // --- VARIABILI INTERNE OTTIMIZZATE ---
    private Texture2D mascheraSporcoIstanza;
    private Color32[] pixelsSporco; 
    
    private int textureWidth;
    private int textureHeight;
    private int pixelTotaliSporchi;
    private int pixelPulitiAttuali = 0;
    private bool minigiocoFinito = false;

    // Shader ID per le massime prestazioni
    private readonly int idMascheraSporco = Shader.PropertyToID("_MascheraSporco");

    private void Start()
    {
        // 1. Creiamo la copia virtuale della texture
        mascheraSporcoIstanza = Instantiate(mascheraSporcoOriginale);

        // 2. Estraiamo l'array in memoria UNA SOLA VOLTA per evitare cali di frame
        pixelsSporco = mascheraSporcoIstanza.GetPixels32();

        textureWidth = mascheraSporcoIstanza.width;
        textureHeight = mascheraSporcoIstanza.height;
        
        // 3. Contiamo SOLO i pixel che sono fisicamente sporchi all'avvio
        pixelTotaliSporchi = 0;
        for (int i = 0; i < pixelsSporco.Length; i++)
        {
            if (pixelsSporco[i].a > 12)
            {
                pixelTotaliSporchi++;
            }
        }
    }

    private void Update()
    {
        // Se il gioco è finito, se il mouse non c'è, o se non stiamo cliccando, ci fermiamo
        if (minigiocoFinito || Mouse.current == null || !Mouse.current.leftButton.isPressed) return;

        UseBrush();
    }

    private void UseBrush()
    {
        Vector2 posizioneMouse = Mouse.current.position.ReadValue();
        Ray raggio = cameraRestauro.ScreenPointToRay(posizioneMouse);

        if (Physics.Raycast(raggio, out RaycastHit hitInfo, Mathf.Infinity, layerRestauro))
        {
            // Il collider deve essere un MeshCollider
            if (!(hitInfo.collider is MeshCollider)) return; 

            // Assegniamo la texture clonata al Materiale usando il Renderer colpito
            if (hitInfo.collider.TryGetComponent(out Renderer rend))
            {
                if (rend.material.GetTexture(idMascheraSporco) != mascheraSporcoIstanza)
                {
                    rend.material.SetTexture(idMascheraSporco, mascheraSporcoIstanza);
                }
            }

            // Troviamo il punto UV
            Vector2 coordinataUV = hitInfo.textureCoord;

            int pixelCentroX = (int)(coordinataUV.x * textureWidth);
            int pixelCentroY = (int)(coordinataUV.y * textureHeight);

            PitturaPixel(pixelCentroX, pixelCentroY);
        }
    }

    private void PitturaPixel(int centroX, int centroY)
    {
        bool textureModificata = false;

        for (int x = -raggioAzionePennello; x <= raggioAzionePennello; x++)
        {
            for (int y = -raggioAzionePennello; y <= raggioAzionePennello; y++)
            {
                // Verifica circolare per avere un pennello tondo
                if (x * x + y * y <= raggioAzionePennello * raggioAzionePennello)
                {
                    int targetX = centroX + x;
                    int targetY = centroY + y;

                    // Evitiamo di uscire dai bordi della texture
                    if (targetX >= 0 && targetX < textureWidth && targetY >= 0 && targetY < textureHeight)
                    {
                        int indice = targetY * textureWidth + targetX;
                        
                        // Puliamo solo se c'è effettivamente dello sporco
                        if (pixelsSporco[indice].a > 12) 
                        {
                            pixelsSporco[indice] = new Color32(0, 0, 0, 0); 
                            pixelPulitiAttuali++;
                            textureModificata = true;
                        }
                    }
                }
            }
        }

        if (textureModificata)
        {
            // Applichiamo la modifica in un solo colpo alla GPU
            mascheraSporcoIstanza.SetPixels32(pixelsSporco);
            mascheraSporcoIstanza.Apply();

            if (pixelTotaliSporchi > 0)
            {
                // Ci assicuriamo di non sforare con i conteggi interi
                pixelPulitiAttuali = Mathf.Clamp(pixelPulitiAttuali, 0, pixelTotaliSporchi);

                // Calcoliamo la percentuale e la limitiamo rigorosamente tra 0 e 1
                float rapporto = (float)pixelPulitiAttuali / pixelTotaliSporchi;
                percentualeCompletamento = Mathf.Clamp(rapporto, 0f, 1f);
            }

            // Una volta raggiunto il 95% diamo per conclusa l'operazione
            if (percentualeCompletamento >= 0.95f)
            {
                minigiocoFinito = true;
                percentualeCompletamento = 1f; 
                
                // Ripristiniamo il cursore
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                
                // Lanciamo l'evento
                eventoPuliziaCompletata?.Invoke();
            }
        }
    }
}