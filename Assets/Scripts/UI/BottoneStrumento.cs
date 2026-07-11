using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Componente UI applicato a un bottone per impostare e comunicare la selezione di un determinato
/// strumento di restauro (es. pennello colla, spazzola) tramite canali di eventi basati su ScriptableObject.
/// </summary>
public class BottoneStrumento : MonoBehaviour
{
    [SerializeField] private StrumentoRestauroSO loStrumentoDiQuestoBottone;
    [SerializeField] private StrumentoEventChannelSO canaleCambioStrumento;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => 
        {
            canaleCambioStrumento.RaiseEvent(loStrumentoDiQuestoBottone);
        });
    }
}
