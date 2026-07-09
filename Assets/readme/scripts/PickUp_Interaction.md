# PickUp_Interaction.cs

**Percorso**: `Assets/Scripts/Interaction/PickUp_Interaction.cs`
**Macroarea**: [Player & World Interaction](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/macroareas/PlayerInteraction.md)

---

## Scopo

Componente applicato ai reperti archeologici e alle vaschette in scena. Consente al giocatore di raccoglierli e stringerli in mano, gestendo il reparenting fisico ed il passaggio logico dei dati all'inventario.

---

## Analisi Logica e Funzionale

### 1. Requisiti di Interazione (`canInteract`)
Implementa l'interfaccia [IInteractable](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/Interaction/IInteractable.cs). L'interazione di raccolta è valida solo se la mano del giocatore è libera:
- Verifica che `manoGiocatore.oggettoCorrente` sia nullo e che `manoGiocatore.currentGO` sia nullo.
- Se la mano è occupata, l'HUD mostra un messaggio di errore ("Hai già un oggetto in mano!").

### 2. Logica di Raccolta e Reparenting (`StartInteraction`)
Quando l'utente preme `[E]` per raccogliere l'oggetto:
1. **Propagazione del Tag di Restauro**: Prima del cambio parent, lo script verifica se l'oggetto (o qualsiasi parte della sua gerarchia originale) possiede il tag `OggettoRestaurato`. In caso positivo, garantisce che il componente sia aggiunto direttamente all'istanza fisica raccolta (`this.gameObject`). Questo garantisce la corretta rilevabilità dello stato di restauro da parte del piedistallo museale anche se l'interazione viene avviata da un sotto-elemento.
2. **Registrazione nell'Inventario**: Chiama `manoGiocatore.ImpostaOggetto(datiOggetto, this.gameObject)`, che assegna i dati all'inventario e notifica immediatamente l'evento `OnInventarioAggiornato` a tutti gli ascoltatori (es. [SuggerimentoMano](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/SuggerimentoMano.md)).
3. **Memorizzazione della Scala Originale**: Prima di cambiare genitore, registra la scala globale attuale (`transform.lossyScale`) per evitare distorsioni dimensionali.
4. **Reparenting**: Collega l'oggetto sotto il punto mano del giocatore, azzera posizione e rotazione locali.
5. **Compensazione della Scala**: Calcola la scala locale corretta dividendo la scala globale salvata per la scala del nuovo genitore, preservando le proporzioni nel mondo 3D.
6. **Disattivazione dei Collider**: Disabilita ricorsivamente tutti i collider dell'oggetto e dei suoi figli (`GetComponentsInChildren<Collider>()`) per impedire che urtino l'ambiente durante il trasporto.
7. **Notifica Events e Tavolo**: Innesca l'evento di interazione sul ScriptableObject dati e notifica il tavolo corrente per liberare lo slot.
