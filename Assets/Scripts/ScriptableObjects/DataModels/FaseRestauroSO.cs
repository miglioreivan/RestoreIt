using UnityEngine;

[CreateAssetMenu(fileName = "NuovaFase", menuName = "ScriptableObjects/Fasi Restauro")]
public class FaseRestauroSO : ScriptableObject
{
    [Header("Guida Giocatore")]
    [TextArea(3, 10)]
    [SerializeField] private string descrizioneFase;

    public string DescrizioneFase => descrizioneFase;
}