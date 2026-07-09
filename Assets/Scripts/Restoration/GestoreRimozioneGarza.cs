using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class GestoreRimozioneGarza : MonoBehaviour
{
    [Header("Tavolo")]
    [SerializeField] private TavoloSO tavoloCorrente;
    [SerializeField] private FaseRestauroSO triggerRimozione;
    [SerializeField] private Camera cameraRestauro;
    [SerializeField] private RestoreManager restoreManager;
    [SerializeField] private FaseRestauroSO faseSuccessiva;

    [Header("Cerca Oggetto")]
    [Tooltip("La parola chiave da cercare nei figli del mosaico per identificare l'oggetto da rimuovere.")]
    [SerializeField] private string nomeOggettoDaCercare = "Garza";

    [Header("Parametri Interazione")]
    [SerializeField] private LayerMask layerRestauro;
    [SerializeField] private Texture2D cursorClickTexture;
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;

    private bool isActive = false;
    private bool cameraTransitionFinished = false;
    private GameObject oggettoDaRimuovere;

    private void Awake()
    {
        if (layerRestauro.value == 0)
        {
            layerRestauro = LayerMask.GetMask("Restauro");
            Debug.Log($"Layer di restauro vuoto. Impostato automaticamente a Restauro con valore {layerRestauro.value}.");
        }
    }

    private void OnEnable()
    {
        if (tavoloCorrente != null)
        {
            tavoloCorrente.OnFaseCambiata += OnFaseCambiata;
            if (tavoloCorrente.faseCorrente == triggerRimozione)
            {
                IniziaFaseRimozione();
            }
        }
    }

    private void OnDisable()
    {
        if (tavoloCorrente != null)
        {
            tavoloCorrente.OnFaseCambiata -= OnFaseCambiata;
        }
        TerminaFaseRimozione();
    }

    private void OnFaseCambiata(FaseRestauroSO fase)
    {
        if (fase != triggerRimozione)
        {
            TerminaFaseRimozione();
            return;
        }
        IniziaFaseRimozione();
    }

    public void CameraTransitionCompleted()
    {
        cameraTransitionFinished = true;
        Debug.Log($"Transizione telecamera completata. Cliccare su {nomeOggettoDaCercare} per rimuoverlo.");
    }

    private void IniziaFaseRimozione()
    {
        isActive = true;
        cameraTransitionFinished = false;
        oggettoDaRimuovere = null;

        if (tavoloCorrente == null || tavoloCorrente.vaschettaGameObject == null)
        {
            Debug.LogError("Tavolo corrente o oggetto vaschetta non validi.");
            return;
        }

        // Ricerca dell'oggetto target all'interno della gerarchia dei figli del mosaico
        Transform mosaicoTransform = tavoloCorrente.vaschettaGameObject.transform;
        foreach (Transform child in mosaicoTransform)
        {
            if (child.name.Contains(nomeOggettoDaCercare))
            {
                oggettoDaRimuovere = child.gameObject;
                break;
            }
        }
        if (oggettoDaRimuovere == null)
        {
            Debug.LogWarning($"Nessun oggetto con nome {nomeOggettoDaCercare} trovato tra i figli del mosaico. Salto la fase.");
            StartCoroutine(AvanzaFaseRitardato());
            return;
        }

        // Assegnazione di un collider temporaneo per rendere l'oggetto intercettabile tramite raycast
        if (oggettoDaRimuovere.GetComponent<Collider>() == null)
        {
            if (oggettoDaRimuovere.GetComponentInChildren<MeshFilter>() != null)
            {
                oggettoDaRimuovere.AddComponent<MeshCollider>();
            }
            else
            {
                oggettoDaRimuovere.AddComponent<BoxCollider>();
            }
            Debug.Log($"Aggiunto collider temporaneo a {oggettoDaRimuovere.name} per consentire il clic.");
        }

        if (cursorClickTexture != null)
        {
            Cursor.SetCursor(cursorClickTexture, cursorHotspot, CursorMode.Auto);
        }
    }

    private void TerminaFaseRimozione()
    {
        isActive = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private void Update()
    {
        if (!isActive || !cameraTransitionFinished || oggettoDaRimuovere == null) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = cameraRestauro.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerRestauro))
            {
                // Verifica della corrispondenza dell'oggetto colpito dal raycast
                if (hit.collider.gameObject == oggettoDaRimuovere || hit.collider.transform.IsChildOf(oggettoDaRimuovere.transform))
                {
                    RimuoviOggetto();
                }
            }
        }
    }

    private void RimuoviOggetto()
    {
        Debug.Log($"Rilevato clic su {oggettoDaRimuovere.name}. Rimozione dell'oggetto e avanzamento alla fase successiva.");
        Destroy(oggettoDaRimuovere);
        oggettoDaRimuovere = null;
        
        if (tavoloCorrente != null && tavoloCorrente.vaschettaGameObject != null)
        {
            int idMostraPittura = Shader.PropertyToID("_mostraPittura");
            foreach (var r in tavoloCorrente.vaschettaGameObject.GetComponentsInChildren<Renderer>())
            {
                Material[] mats = r.materials;
                bool modified = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null && mats[i].HasProperty(idMostraPittura))
                    {
                        mats[i].SetFloat(idMostraPittura, 0f);
                        modified = true;
                    }
                }
                if (modified)
                    r.materials = mats;
            }
            Debug.Log("Impostata la visualizzazione della pittura a zero su tutti i materiali del mosaico.");
        }

        StartCoroutine(AvanzaFaseRitardato());
    }

    private IEnumerator AvanzaFaseRitardato()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (tavoloCorrente != null && faseSuccessiva != null)
        {
            tavoloCorrente.AvanzaFase(faseSuccessiva);
        }
        else if (restoreManager != null)
        {
            restoreManager.CompletaRestauro();
        }
    }
}
