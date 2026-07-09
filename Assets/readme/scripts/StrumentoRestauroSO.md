# StrumentoRestauroSO.cs

## Descrizione
ScriptableObject che contiene le proprietà descrittive e grafiche di uno strumento di restauro (es. Spugna, Bisturi).

## Responsabilità
- Memorizzare l'aspetto visivo del cursore del mouse associato allo strumento.
- Definire l'hotspot di click e il raggio d'azione di pulizia.

## Proprietà Chiave
- `cursoreCustom` (`Texture2D`): Immagine che sostituisce il puntatore standard del mouse.
- `hotspot` (`Vector2`): Il pixel esatto dell'immagine che esegue il click (es. la punta dello strumento).
- `raggioAzione` (`int`): Raggio di influenza (in pixel) applicato durante la pulizia o pittura.
