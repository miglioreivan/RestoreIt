using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Canale Cambio Strumento")]
public class StrumentoEventChannelSO : ScriptableObject
{
    public event UnityAction<StrumentoRestauroSO> OnStrumentoCambiato;

    public void RaiseEvent(StrumentoRestauroSO nuovoStrumento)
    {
        if (OnStrumentoCambiato != null)
            OnStrumentoCambiato.Invoke(nuovoStrumento);
    }
}
