using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Componente di debug per il monitoraggio visivo del flusso delle fasi di restauro.
/// Attacca questo script allo stesso GameObject del RestoreManager.
/// Funziona solo nell'Editor Unity e non genera alcun overhead in Build.
/// </summary>
[AddComponentMenu("RestoreIt/Debug/Flusso Restauro Debug")]
public class FlussoRestauroDebug : MonoBehaviour
{
    [Header("Configurazione Debug")]
    [SerializeField] private TavoloSO tavoloCorrente;
    [SerializeField] private bool mostraInSceneView = true;
    [SerializeField] private Color coloreAttivo   = new Color(0.2f, 1f, 0.4f, 1f);
    [SerializeField] private Color coloreInattivo = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    private string ultimaFaseRegistrata;
    private System.Collections.Generic.List<string> storicoFasi = new System.Collections.Generic.List<string>();

    private void Awake()
    {
        if (tavoloCorrente == null)
        {
            RestoreManager rm = GetComponent<RestoreManager>();
            if (rm != null)
            {
                tavoloCorrente = rm.TavoloCorrente;
            }
        }
    }

    private void OnEnable()
    {
        if (tavoloCorrente == null)
        {
            RestoreManager rm = GetComponent<RestoreManager>();
            if (rm != null)
            {
                tavoloCorrente = rm.TavoloCorrente;
            }
        }

        if (tavoloCorrente != null)
        {
            tavoloCorrente.OnFaseCambiata   += OnFaseCambiata;
            tavoloCorrente.OnOggettoPosato  += OnOggettoPosato;
            tavoloCorrente.OnTavoloSvuotato += OnTavoloSvuotato;
        }
    }

    private void OnDisable()
    {
        if (tavoloCorrente != null)
        {
            tavoloCorrente.OnFaseCambiata   -= OnFaseCambiata;
            tavoloCorrente.OnOggettoPosato  -= OnOggettoPosato;
            tavoloCorrente.OnTavoloSvuotato -= OnTavoloSvuotato;
        }
    }

    private void OnFaseCambiata(FaseRestauroSO fase)
    {
        string nomeFase = fase != null ? fase.name : "null";
        ultimaFaseRegistrata = nomeFase;
        storicoFasi.Add($"[{System.DateTime.Now:HH:mm:ss}] Fase → {nomeFase}");
        if (storicoFasi.Count > 20) storicoFasi.RemoveAt(0);
        Debug.Log($"[FlussoRestauroDebug] Fase cambiata → {nomeFase}");
    }

    private void OnOggettoPosato(DatiOggettoSO oggetto)
    {
        string nome = oggetto != null ? oggetto.name : "null";
        storicoFasi.Add($"[{System.DateTime.Now:HH:mm:ss}] Oggetto posato: {nome}");
        if (storicoFasi.Count > 20) storicoFasi.RemoveAt(0);
        Debug.Log($"[FlussoRestauroDebug] Oggetto posato → {nome}");
    }

    private void OnTavoloSvuotato()
    {
        storicoFasi.Add($"[{System.DateTime.Now:HH:mm:ss}] Tavolo svuotato");
        if (storicoFasi.Count > 20) storicoFasi.RemoveAt(0);
        ultimaFaseRegistrata = null;
        Debug.Log("[FlussoRestauroDebug] Tavolo svuotato.");
    }

    private void OnGUI()
    {
        if (!Application.isPlaying || !mostraInSceneView) return;

        // Trova la nostra posizione tra tutte le finestre di debug attive nella scena per evitare sovrapposizioni
        FlussoRestauroDebug[] tuttiIDebug = FindObjectsByType<FlussoRestauroDebug>(FindObjectsSortMode.None);
        int mioIndex = 0;
        for (int i = 0; i < tuttiIDebug.Length; i++)
        {
            if (tuttiIDebug[i] == this)
            {
                mioIndex = i;
                break;
            }
        }

        GUIStyle box   = new GUIStyle(GUI.skin.box)   { fontSize = 11 };
        GUIStyle label = new GUIStyle(GUI.skin.label) { fontSize = 11 };

        float w = 340f;
        // Allinea sul bordo destro dello schermo (Screen.width) e affianca verso sinistra
        float x = Screen.width - (mioIndex + 1) * w - 10f - mioIndex * 15f;
        float y = 10f;

        GUI.color = Color.black * 0.6f;
        GUI.Box(new Rect(x - 4, y - 4, w + 8, Mathf.Min(storicoFasi.Count, 20) * 18 + 56), "", box);
        GUI.color = Color.white;

        // Intestazione
        GUI.color = coloreAttivo;
        string titolo = $"▶ {gameObject.name.ToUpper()} DEBUG";
        GUI.Label(new Rect(x, y, w, 20), titolo, label);
        y += 22;

        // Fase corrente
        GUI.color = string.IsNullOrEmpty(ultimaFaseRegistrata) ? coloreInattivo : coloreAttivo;
        string faseDisplay = string.IsNullOrEmpty(ultimaFaseRegistrata) ? "— nessuna —" : ultimaFaseRegistrata;
        GUI.Label(new Rect(x, y, w, 18), $"Fase corrente: {faseDisplay}", label);
        y += 20;

        // Storico
        GUI.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        foreach (string voce in storicoFasi)
        {
            GUI.Label(new Rect(x, y, w, 16), voce, label);
            y += 16;
        }
        GUI.color = Color.white;
    }
}
#endif
