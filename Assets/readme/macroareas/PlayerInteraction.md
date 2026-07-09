# Macroarea: Player Interaction & World (Interazione ed Esplorazione)

Questa macroarea gestisce i sistemi di movimento del giocatore, il rilevamento degli oggetti interattivi tramite raycast, la raccolta fisica dei reperti (con reparenting nella mano) e la posa sui banchi da lavoro o sui piedistalli espositivi del museo.

---

## 🛠️ Gli Script della Macroarea

La logica dell'esplorazione e dell'interazione con il mondo di gioco è implementata nei seguenti script:
1. **[FirstPersonController](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/FirstPersonController.md)**: Gestisce gli input di movimento (WASD, corsa) e visuale del giocatore, l'HUD e il raycast continuo per rilevare gli elementi interattivi.
2. **[PickUp_Interaction](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/PickUp_Interaction.md)**: Componente applicato a reperti e vaschette per permettere al giocatore di raccoglierli (agganciandoli gerarchicamente al punto mano).
3. **[DropZone_Interaction](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/DropZone_Interaction.md)**: Componente applicato alle zone di rilascio dei tavoli di lavoro (Workbench) per consentire di depositare l'oggetto e avviare il restauro.
4. **[PedestalDropZone](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/PedestalDropZone.md)**: Slot di rilascio posizionati sopra i piedistalli espositivi del museo, adibiti ad accogliere esclusivamente i reperti restaurati.
5. **[GestoreEsposizione](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/GestoreEsposizione.md)**: Monitora lo stato di tutti i piedistalli della galleria per attivare eventi speciali o completare il livello quando tutti i reperti sono esposti.
6. **[IInteractable](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/IInteractable.md)**: Interfaccia C# implementata da tutti gli oggetti con cui il giocatore può interagire (porta i metodi per validare l'interazione, ottenere il testo descrittivo dell'HUD e iniziare l'azione).
7. **[OggettoRestaurato](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/OggettoRestaurato.md)**: Script di marcatura (tag logico) applicato a reperti completati.
8. **[GestoreFrecceRestauro](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/GestoreFrecceRestauro.md)**: Gestisce le frecce grafiche 3D fluttuanti posizionate sopra i banchi di lavoro per guidare visivamente il giocatore.

---

## 📈 Dettagli Logici e Fisici del Movimento FPS

Lo script `FirstPersonController` implementa un controllo in prima persona tramite il componente `CharacterController` di Unity. Comprende due moduli fisici importanti:

### 1. Calcolo dello Scivolamento sulle Pendenze (Slope Sliding)
Quando il giocatore si trova su una rampa o su una superficie troppo inclinata, la fisica di gioco deve impedirgli di arrampicarsi e farlo scivolare verso il basso.
Il controller esegue questa verifica nel metodo `HandleMovement` sfruttando i dati di collisione restituiti da `OnControllerColliderHit`:
1. Rileva la normale della superficie di contatto (`hitNormal`).
2. Calcola l'angolo di inclinazione rispetto alla verticale (`Vector3.up`):
   $$\theta = \text{Vector3.Angle(Vector3.up, hitNormal)}$$
3. Se l'angolo $\theta$ è superiore al limite configurato nel Character Controller (`slopeLimit`), il giocatore si trova in stato di scivolamento (`isSliding = true`).
4. Viene calcolata la direzione di scivolamento perpendicolare alla pendenza tramite il prodotto vettoriale (Cross Product):
   - Calcola l'asse orizzontale della pendenza:
     $$\mathbf{A}_{\text{horizontal}} = \text{Vector3.Cross(Vector3.up, hitNormal)}$$
   - Calcola la direzione di discesa lungo la normale:
     $$\mathbf{D}_{\text{slide}} = \text{Vector3.Cross}(\mathbf{A}_{\text{horizontal}}, \text{hitNormal})$$
5. I vettori di movimento laterale del giocatore vengono sovrascritti proiettando la velocità lungo $\mathbf{D}_{\text{slide}}$ moltiplicata per la velocità di scivolamento (`slideSpeed`).

---

## 📐 Formula Matematica di Compensazione locale delle Scale

Un problema tipico di Unity che causa la distorsione geometrica degli oggetti è il **reparenting sotto genitori che hanno una scala non uniforme** (es. `puntoMano` o `puntoRelease` del tavolo che hanno scale diverse da `(1, 1, 1)`).
Quando si esegue `transform.SetParent(newParent)`, Unity aggiorna la scala locale per preservare la dimensione globale originale dell'oggetto nel mondo 3D. Se il genitore ha una scala asimmetrica (es. `(1.5, 0.8, 1.2)`), l'oggetto si deformerà.

Per risolvere questo problema, gli script di interazione applicano la seguente formula matematica a runtime per ricalcolare la scala locale corretta in base alla scala globale del nuovo genitore:
$$\text{transform.localScale} = \left( \frac{\text{scalaMondoOriginale.x}}{\text{parentLossyScale.x}}, \frac{\text{scalaMondoOriginale.y}}{\text{parentLossyScale.y}}, \frac{\text{scalaMondoOriginale.z}}{\text{parentLossyScale.z}} \right)$$

Nel codice C# questa logica si traduce come segue:
```csharp
Vector3 targetWorldScale = transform.lossyScale;
transform.SetParent(nuovoParent, false);

if (nuovoParent != null)
{
    Vector3 parentLossyScale = nuovoParent.lossyScale;
    transform.localScale = new Vector3(
        parentLossyScale.x != 0 ? targetWorldScale.x / parentLossyScale.x : targetWorldScale.x,
        parentLossyScale.y != 0 ? targetWorldScale.y / parentLossyScale.y : targetWorldScale.y,
        parentLossyScale.z != 0 ? targetWorldScale.z / parentLossyScale.z : targetWorldScale.z
    );
}
else
{
    transform.localScale = targetWorldScale;
}
```
Questo garantisce che l'oggetto mantenga le esatte proporzioni geometriche originarie sia quando si trova in mano al giocatore, sia quando viene posato sui tavoli o sui piedistalli.

---

## 🔄 Ciclo di Rilevamento delle Interazioni

Il controller esegue ad ogni frame una scansione tramite raycast:
1. Spara un raggio dalla telecamera frontale del giocatore in direzione `cameraTransform.forward` per una lunghezza massima pari a `interactionRange`.
2. Il raycast è filtrato tramite la maschera `interactableLayer` per ignorare ostacoli o geometrie statiche e colpire solo gli oggetti interattivi.
3. Se colpisce un oggetto, tenta di recuperare il componente `IInteractable`:
   - Se presente e l'interazione è valida (`canInteract() == true`), l'HUD mostra la stringa di suggerimento (`GetInteractionText()`).
   - Se il giocatore preme il tasto `[E]`, viene invocato `StartInteraction()`, che avvia la raccolta, il rilascio o l'attivazione della postazione.
