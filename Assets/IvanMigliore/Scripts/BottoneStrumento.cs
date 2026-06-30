using UnityEngine;
using UnityEngine.UI;

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