# PedestalDropZone.cs

**Percorso**: `Assets/Scripts/Interaction/PedestalDropZone.cs`
**Macroarea**: [Player & World Interaction](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/macroareas/PlayerInteraction.md)

---

## Scopo

Gestisce gli slot di posizionamento sopra i piedistalli espositivi della galleria del museo. Accetta e blocca esclusivamente i reperti che hanno completato con successo tutte le fasi di restauro, con la possibilità di limitare ogni piedistallo a un tipo di oggetto specifico (anfore o mosaici).

---

## Analisi Logica e Funzionale

### 1. Distinzione del Tipo di Oggetto Accettato
Ogni piedistallo espone nell'Inspector un campo `tipoAccettato` di tipo enum `TipoOggettoAccettato`:

| Valore | Comportamento |
| :--- | :--- |
| `Qualsiasi` | Accetta qualsiasi reperto restaurato |
| `SoloAnfore` | Accetta solo oggetti con dati di tipo `VaschettaSO` |
| `SoloMosaici` | Accetta solo oggetti con dati di tipo `MosaicoSO` |

### 2. Validazione del Reperto Restaurato (`canInteract`)
Implementa l'interfaccia [IInteractable](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/Interaction/IInteractable.cs). La validazione controlla che:
- Il giocatore abbia un oggetto in mano.
- L'oggetto sia effettivamente restaurato: la ricerca del componente `OggettoRestaurato` avviene sull'oggetto stesso, nel suo parent e nei suoi figli per coprire qualsiasi struttura gerarchica (inclusa la propagazione del tag gestita all'atto della raccolta).
- I dati o il GameObject fisico siano compatibili con il `tipoAccettato` del piedistallo. La verifica è resa estremamente robusta tramite controlli incrociati sul tipo di ScriptableObject (`VaschettaSO` per anfore, `MosaicoSO` per mosaici), sulla presenza delle parole chiave "anfora" o "mosaico" all'interno del nome del reperto (`nomeOggetto`) o, come fallback di sicurezza, all'interno del nome del GameObject fisico trasportato (`go.name`).

Il testo HUD di feedback è contestuale al tipo di piedistallo: *"Questo piedistallo accetta solo anfore restaurate."* oppure *"Questo piedistallo accetta solo mosaici restaurati."*

### 3. Logica di Posa ed Esposizione (`StartInteraction`)
Quando il giocatore posiziona l'oggetto premendo `[E]`:
1. **Rilascio della Mano**: Chiama `manoGiocatore.SvuotaMano()`, che azzera l'inventario e notifica l'evento `OnInventarioAggiornato`.
2. **Reparenting ed Eliminazione Distorsioni**: Memorizza la scala globale, riparenta sotto `puntoRelease`, e applica la compensazione di scala locale.
3. **Disattivazione dei Collider**: Disabilita ricorsivamente tutti i collider dell'oggetto esposto e dei suoi figli per evitare collisioni fisiche con il giocatore nella galleria.
4. **Blocco del Piedistallo**: Disabilita il collider del piedistallo per impedire ulteriori interazioni.
5. **Notifica del Completamento**: Solleva gli eventi di rilascio e invoca il delegato `OnObjectPlaced` per informare il [GestoreEsposizione](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/GestoreEsposizione.md).
