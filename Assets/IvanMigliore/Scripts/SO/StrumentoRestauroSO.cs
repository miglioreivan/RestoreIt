using UnityEngine;

[CreateAssetMenu(fileName = "NuovoStrumento", menuName = "ScriptableObjects/Strumento Restauro")]
public class StrumentoRestauroSO : ScriptableObject
{
    [Header("Grafica Cursore")]
    [Tooltip("L'immagine che sostituirà il puntatore del mouse")]
    public Texture2D cursoreCustom;
    
    [Tooltip("Il punto esatto in pixel dell'immagine che compie il click (es. la punta del pennello)")]
    public Vector2 hotspot;

    [Header("Impostazioni Pulizia")]
    [Tooltip("Quanto è grande l'area pulita da questo strumento")]
    public int raggioAzione = 10;
}