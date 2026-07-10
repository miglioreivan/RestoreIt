using UnityEngine;

[CreateAssetMenu(fileName = "NuovoDatoOggettoMuseo", menuName = "ScriptableObjects/DatiOggettoMuseo")]
public class DatiOggettoMuseoSO : ScriptableObject
{
    [Header("Informazioni Oggetto")]
    [Tooltip("Nome dell'oggetto esposto nel museo")]
    public string nomeOggetto;

    [Tooltip("Descrizione storica o informativa dell'oggetto")]
    [TextArea(5, 12)]
    public string descrizione;

    [Header("Aspetto Visivo")]
    [Tooltip("Immagine dell'oggetto (opzionale, se null non verrà mostrata nell'UI)")]
    public Sprite immagineOggetto;
}
