using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class GestoreRimozioneGarza : MonoBehaviour
{
    [Header("Tavolo")]
    [SerializeField] private TavoloSO tavoloCorrente;
    [SerializeField] private FaseRestauroSO triggerRimozione; // La fase associata a questo script (es. FaseRimozioneGarza)
    [SerializeField] private Camera cameraRestauro;
    [SerializeField] private RestoreManager restoreManager;
    [SerializeField] private FaseRestauroSO faseSuccessiva;

    [Header("Cerca Oggetto")]
    [Tooltip("La parola chiave da cercare nei figli del mosaico per identificare l'oggetto da rimuovere.")]
    [SerializeField] private string nomeOggettoDaCercare = "Garza";

    [Header("Parametri Interazione")]
    [SerializeField] private LayerMask layerRestauro; // Il layer Restauro per il raycast
    [SerializeField] private Texture2D cursorClickTexture; // Cursore personalizzato per il click
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;

    private bool isActive = false;
    private bool cameraTransitionFinished = false;
    private GameObject oggettoDaRimuovere;

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
        Debug.Log($"[GestoreRimozioneGarza] Camera transition completed. Clicca sull'oggetto '{nomeOggettoDaCercare}' per rimuoverlo.");
    }

    private void IniziaFaseRimozione()
    {
        isActive = true;
        cameraTransitionFinished = false;
        oggettoDaRimuovere = null;

        if (tavoloCorrente == null || tavoloCorrente.vaschettaGameObject == null)
        {
            Debug.LogError("[GestoreRimozioneGarza] Tavolo corrente o oggetto vaschetta NULL!");
            return;
        }

        // Cerca l'oggetto tra i figli del mosaico
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
            Debug.LogWarning($"[GestoreRimozioneGarza] Nessun oggetto con '{nomeOggettoDaCercare}' nel nome trovato tra i figli del mosaico. Salto la fase.");
            StartCoroutine(AvanzaFaseRitardato());
            return;
        }

        // Assicuriamoci che l'oggetto abbia un collider per poter essere cliccato
        if (oggettoDaRimuovere.GetComponent<Collider>() == null)
        {
            // Aggiungiamo un BoxCollider o MeshCollider
            if (oggettoDaRimuovere.GetComponentInChildren<MeshFilter>() != null)
            {
                oggettoDaRimuovere.AddComponent<MeshCollider>();
            }
            else
            {
                oggettoDaRimuovere.AddComponent<BoxCollider>();
            }
            Debug.Log($"[GestoreRimozioneGarza] Aggiunto collider temporaneo a '{oggettoDaRimuovere.name}' per consentire il click.");
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
                // Se abbiamo colpito l'oggetto da rimuovere o un suo figlio
                if (hit.collider.gameObject == oggettoDaRimuovere || hit.collider.transform.IsChildOf(oggettoDaRimuovere.transform))
                {
                    RimuoviOggetto();
                }
            }
        }
    }

    private void RimuoviOggetto()
    {
        Debug.Log($"[GestoreRimozioneGarza] Clic su '{oggettoDaRimuovere.name}' rilevato. Rimozione e passaggio alla fase successiva.");
        Destroy(oggettoDaRimuovere);
        oggettoDaRimuovere = null;
        
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
