# IInteractable.cs

## Descrizione
Interfaccia fondamentale per definire qualsiasi comportamento di interazione diretta da parte del giocatore in prima persona.

## Metodi Dichiarati
- `void StartInteraction()`: Codice eseguito nel momento esatto in cui il giocatore interagisce con l'oggetto.
- `string GetInteractionText()`: Ritorna il testo da visualizzare sull'HUD quando il giocatore inquadra l'oggetto (es. "[E] Raccogli anfora").
- `bool canInteract()`: Ritorna se l'interazione è attualmente valida e disponibile.

## Utilizzo
Qualsiasi script che richiede interazione da parte del raycast del `FirstPersonController` deve implementare questa interfaccia.
