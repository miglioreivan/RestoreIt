# FirstPersonController.cs

Gestisce la fisica del movimento tridimensionale del giocatore in prima persona, la rotazione della visuale, l'interazione con gli elementi di scena e il puntamento HUD.

---

## 📖 Analisi Logica e Funzionale

### 1. Sistema di Input e Movimento
Utilizza il nuovo **Input System** di Unity. Legge le azioni configurate per ricavare i vettori di movimento:
- Legge l'azione `Move` (WASD/levetta) per determinare la direzione di movimento locale rispetto all'orientamento del giocatore:
  $$\mathbf{M} = (\text{transform.forward} \times \text{input.y}) + (\text{transform.right} \times \text{input.x})$$
- Applica la velocità corretta a seconda che l'azione `Sprint` sia attiva o meno.
- Gestisce la gravità accumulando l'accelerazione verticale nel tempo (`verticalVelocity += gravity * Time.deltaTime`).
- Muove il giocatore richiamando il metodo `characterController.Move(M * Time.deltaTime)`.

### 2. Calcolo dello Scivolamento sulle Superfici Inclinate (Slope Sliding)
Per impedire al giocatore di scalare pareti o pendenze ripide, lo script implementa il calcolo di scivolamento fisico nel metodo `HandleMovement`:
- Intercetta la normale della superficie toccata (`hitNormal`) tramite `OnControllerColliderHit`.
- Calcola la pendenza angolare:
  $$\theta = \text{Vector3.Angle(Vector3.up, hitNormal)}$$
- Se $\theta$ è maggiore del limite massimo (`slopeLimit`) configurato sul Character Controller, calcola la direzione di discesa ortogonale proiettando la gravità lungo il piano inclinato tramite il doppio prodotto vettoriale:
  $$\mathbf{D}_{\text{slide}} = \text{Vector3.Cross}(\text{Vector3.Cross(Vector3.up, hitNormal)}, \text{hitNormal}).\text{normalized}$$
- Sovrascrive il vettore di spostamento orizzontale muovendo il giocatore lungo $\mathbf{D}_{\text{slide}}$ moltiplicata per `slideSpeed`.

### 3. Rotazione Telecamera (Mouse Look)
Legge l'azione `Look` per ruotare la visuale:
- Ruota il corpo del giocatore sull'asse verticale Y in base allo spostamento orizzontale del mouse.
- Ruota la telecamera sull'asse X (in alto/in basso) in base allo spostamento verticale, limitando l'angolo (`Mathf.Clamp`) entro un intervallo predefinito (`upDownRange`, default di $80^{\circ}$) per evitare che la telecamera si ribalti completamente.

### 4. Raycast Continuo e Rilevamento delle Interazioni (`CheckForInteractable`)
Nel metodo `Update`, lo script spara costantemente un raggio invisibile (Raycast) in avanti a partire dal centro della telecamera:
- Il raggio si estende per una lunghezza massima pari a `interactionRange`.
- Cerca collisioni esclusivamente con gli oggetti appartenenti a `interactableLayer`.
- Se colpisce un oggetto che implementa l'interfaccia [IInteractable](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/Interaction/IInteractable.cs), ed esso si trova in uno stato valido per l'interazione (`canInteract() == true`):
  - Ricava il testo di suggerimento (`GetInteractionText()`) e lo visualizza nell'HUD 2D a schermo.
  - Se il giocatore preme il pulsante `Interact` (tasto `[E]`), avvia il metodo `StartInteraction()` dell'interfaccia.
- Se non colpisce nulla o l'oggetto non implementa l'interfaccia, disattiva il testo HUD a schermo.
