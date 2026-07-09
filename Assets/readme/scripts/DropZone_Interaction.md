# DropZone_Interaction.cs

**Percorso**: `Assets/Scripts/Interaction/DropZone_Interaction.cs`
**Macroarea**: [Player & World Interaction](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/macroareas/PlayerInteraction.md)

---

## Scopo

Gestisce gli slot di posizionamento situati sopra i banchi di lavoro (Workbench). Consente al giocatore di posare il reperto trasportato in mano sul tavolo per avviare la procedura di restauro.

---

## Analisi Logica e Funzionale

### 1. Filtro degli Oggetti Accettati (`canInteract`)
Implementa l'interfaccia [IInteractable](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/Interaction/IInteractable.cs). La validazione controlla che:
- Il giocatore abbia un oggetto in mano.
- L'oggetto **non** sia già restaurato: la verifica cerca il componente `OggettoRestaurato` sull'oggetto in mano, nel suo parent e nei suoi figli (`GetComponent`, `GetComponentInParent`, `GetComponentInChildren`).
- Il tavolo sia attualmente vuoto (`tavoloCorrente.oggettoCorrente == null`).
- L'oggetto in mano sia compatibile con la tipologia accettata dal tavolo (`tipoAccettato`):
  - `SoloAnfore`: Accetta solo oggetti con dati di tipo `VaschettaSO`.
  - `SoloMosaici`: Accetta solo oggetti con dati di tipo `MosaicoSO`.
  - `Qualsiasi`: Accetta qualsiasi reperto.

### 2. Logica di Rilascio e Configurazione del Tavolo (`StartInteraction`)
Quando il giocatore posa l'oggetto premendo `[E]`:
1. **Aggiornamento Dati Tavolo**: Passa il riferimento del GameObject e del ScriptableObject dati al `tavoloCorrente` richiamando `PosaOggetto(oggetto)`.
2. **Svuotamento Mano**: Richiama `manoGiocatore.SvuotaMano()`, che azzera l'inventario e notifica l'evento `OnInventarioAggiornato`.
3. **Calcolo della Scala e Reparenting**: Memorizza la scala globale originale dell'oggetto, lo riparenta sotto `puntoRelease`, azzera posizione e rotazione locali, e calcola la nuova scala locale compensando quella del tavolo.
4. **Disattivazione dei Collider**: Disabilita ricorsivamente tutti i collider dell'oggetto e dei suoi figli per evitare conflitti fisici con i minigiochi di restauro.
5. **Disattivazione della DropZone**: Disabilita il collider della stessa DropZone per impedire nuovi rilasci durante la sessione.
6. **Sollevamento Evento**: Innesca `onReleaseEvent` per notificare al [RestoreManager](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/RestoreManager.md) la presenza di un oggetto sul banco.
