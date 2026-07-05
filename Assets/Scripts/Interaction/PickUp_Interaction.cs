using UnityEngine;

public class PickUp_Interaction : MonoBehaviour, IInteractable
{
    [SerializeField] private DatiOggettoSO datiOggetto;
    [SerializeField] private InventarioManoSO manoGiocatore;
    [SerializeField] private VoidEventChannelSO onPickUp;

    [Header("Restauro / Tavolo (Opzionale)")]
    [SerializeField] private TavoloSO tavoloCorrente;

    private void Awake()
    {
        if (manoGiocatore == null)
        {
            FirstPersonController controller = FindFirstObjectByType<FirstPersonController>();
            if (controller != null)
            {
                manoGiocatore = controller.Inventario;
            }
            else
            {
                Debug.LogWarning($"[PickUp_Interaction] '{gameObject.name}': manoGiocatore è null e FirstPersonController non trovato nella scena.");
            }
        }
    }

    public void ImpostaTavolo(TavoloSO tavolo)
    {
        tavoloCorrente = tavolo;
    }

    public bool canInteract()
    {
        return manoGiocatore.oggettoCorrente == null && manoGiocatore.currentGO == null;
    }

    public string GetInteractionText()
    {
        if (canInteract())
        {
            string nome = datiOggetto != null ? datiOggetto.nomeOggetto : gameObject.name;
            return "[E] Raccogli " + nome;
        }
        else
        {
            return "Hai già un oggetto in mano!";
        }
    }

    public void StartInteraction()
    {
        if (!canInteract()) return;

        manoGiocatore.oggettoCorrente = datiOggetto;
        manoGiocatore.currentGO = this.gameObject;

        transform.SetParent(manoGiocatore.puntoMano, false);
        transform.localPosition = Vector3.zero;
        if (TryGetComponent(out Collider col))
            col.enabled = false;

        if (datiOggetto != null)
        {
            datiOggetto.EseguiInterazione();
        }

        if (tavoloCorrente != null)
        {
            tavoloCorrente.SvuotaTavolo();
        }
    }
}
