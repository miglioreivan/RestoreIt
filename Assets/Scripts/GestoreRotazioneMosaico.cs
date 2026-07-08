using UnityEngine;
using System.Collections;

public class GestoreRotazioneMosaico : MonoBehaviour
{
    [Header("Tavolo e Fasi")]
    [SerializeField] private TavoloSO tavoloCorrente;
    [SerializeField] private FaseRestauroSO triggerRotazione;
    [SerializeField] private FaseRestauroSO faseSuccessiva;

    [Header("Parametri Rotazione")]
    [SerializeField] private float durataRotazione = 1.5f;
    [SerializeField] private Vector3 angoliRotazione = new Vector3(180f, 0f, 0f); // Sottosopra (es. flip X di 180)

    private bool isRotazioneAttiva = false;

    private void OnEnable()
    {
        if (tavoloCorrente != null)
        {
            tavoloCorrente.OnFaseCambiata += OnFaseCambiata;
            
            if (tavoloCorrente.faseCorrente == triggerRotazione)
            {
                IniziaRotazione();
            }
        }
    }

    private void OnDisable()
    {
        if (tavoloCorrente != null)
        {
            tavoloCorrente.OnFaseCambiata -= OnFaseCambiata;
        }
    }

    private void OnFaseCambiata(FaseRestauroSO nuovaFase)
    {
        if (nuovaFase == triggerRotazione)
        {
            IniziaRotazione();
        }
    }

    private bool cameraTransitionFinished = false;

    private void IniziaRotazione()
    {
        isRotazioneAttiva = false;
        cameraTransitionFinished = false;
        Debug.Log("[GestoreRotazioneMosaico] IniziaRotazione chiamato. In attesa del completamento della transizione camera...");
    }

    public void CameraTransitionCompleted()
    {
        if (isRotazioneAttiva || cameraTransitionFinished) return;
        cameraTransitionFinished = true;

        GameObject mosaicoGO = tavoloCorrente != null ? tavoloCorrente.vaschettaGameObject : null;
        if (mosaicoGO == null)
        {
            Debug.LogError("[GestoreRotazioneMosaico] Nessun mosaico GameObject sul tavolo da ruotare!");
            AvanzaFase();
            return;
        }

        StartCoroutine(EseguiRotazione(mosaicoGO));
    }

    private IEnumerator EseguiRotazione(GameObject targetGO)
    {
        isRotazioneAttiva = true;
        Debug.Log("[GestoreRotazioneMosaico] Avvio rotazione sottosopra del mosaico...");

        Quaternion startRot = targetGO.transform.rotation;
        // Calcola la rotazione finale sommando la rotazione locale desiderata
        Quaternion targetRot = startRot * Quaternion.Euler(angoliRotazione);

        float elapsed = 0f;
        while (elapsed < durataRotazione)
        {
            if (targetGO == null) break;
            
            float t = elapsed / durataRotazione;
            // Usiamo un'interpolazione smooth (es. SmoothStep) per rendere il movimento fluido ed elegante
            t = Mathf.SmoothStep(0f, 1f, t);
            
            targetGO.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (targetGO != null)
        {
            targetGO.transform.rotation = targetRot;
        }

        Debug.Log("[GestoreRotazioneMosaico] Rotazione completata!");
        isRotazioneAttiva = false;
        
        yield return new WaitForSeconds(0.5f); // Breve pausa per enfasi visiva
        AvanzaFase();
    }

    private void AvanzaFase()
    {
        if (tavoloCorrente != null && faseSuccessiva != null)
        {
            Debug.Log($"[GestoreRotazioneMosaico] Avanzo alla fase successiva: {faseSuccessiva.name}");
            tavoloCorrente.AvanzaFase(faseSuccessiva);
        }
        else
        {
            Debug.LogWarning("[GestoreRotazioneMosaico] Nessuna fase successiva configurata.");
        }
    }
}
