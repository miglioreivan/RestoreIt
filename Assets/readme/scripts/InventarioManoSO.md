# InventarioManoSO.cs

**Percorso**: `Assets/Scripts/ScriptableObjects/RuntimeState/InventarioManoSO.cs`
**Macroarea**: [ScriptableObjects & Data Model](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/macroareas/DataModel.md)

---

## Scopo

ScriptableObject persistente che modella lo stato dell'inventario in mano al giocatore. Funge da punto di condivisione dati centrale tra il giocatore, le zone di prelievo e le zone di rilascio, disaccoppiando i sottosistemi.

---

## Proprietà Chiave

- `oggettoCorrente` (`DatiOggettoSO`): Riferimento ai dati logici dell'oggetto trasportato.
- `currentGO` (`GameObject`): Riferimento all'istanza fisica del modello in mano.
- `puntoMano` (`Transform`): Il transform agganciato sotto la camera del player dove l'oggetto viene posizionato.

---

## Evento e Metodi di Notifica

Per evitare polling in `Update` da parte dei componenti UI, `InventarioManoSO` espone un evento C# che notifica tutti gli ascoltatori ad ogni modifica dell'inventario:

- `OnInventarioAggiornato` (`event Action`): Viene lanciato ogni volta che il contenuto della mano cambia. Il componente [SuggerimentoMano](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/SuggerimentoMano.md) si iscrive a questo evento per aggiornare il testo dell'HUD in modo reattivo.
- `ImpostaOggetto(DatiOggettoSO dati, GameObject go)`: Imposta entrambi i campi `oggettoCorrente` e `currentGO` in un'unica chiamata e lancia l'evento. Usato da [PickUp_Interaction](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/PickUp_Interaction.md) alla raccolta dell'oggetto.
- `SvuotaMano()`: Azzera entrambi i campi e lancia l'evento. Usato da [DropZone_Interaction](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/DropZone_Interaction.md) e [PedestalDropZone](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/PedestalDropZone.md) al rilascio dell'oggetto.

All'abilitazione del gioco (`OnEnable`), azzera automaticamente tutte le referenze per evitare problemi di persistenza indesiderata tra sessioni di gioco in Editor.
