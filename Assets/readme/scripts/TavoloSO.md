# TavoloSO.cs

## Descrizione
ScriptableObject persistente che memorizza lo stato corrente di un tavolo da restauro (Workbench).

## Responsabilità
- Memorizzare l'oggetto logico e la vaschetta fisica correntemente posati sul tavolo.
- Gestire e notificare il cambio della fase di restauro attiva sul tavolo.
- Offrire eventi globali a cui script esterni (es. `RestoreManager`) si registrano per allinearsi allo stato del tavolo.

## Funzionamento
- **Posa**: Quando un oggetto viene rilasciato sul tavolo, `PosaOggetto` memorizza le referenze e scatena `OnOggettoPosato`.
- **Fasi**: Coordina il flusso delle fasi tramite `AvanzaFase`, scatenando `OnFaseCambiata`.
- **Svuotamento**: All'invocazione di `SvuotaTavolo`, distrugge gli oggetti fisici di lavoro registrati, azzera le variabili e invoca `OnTavoloSvuotato` per ripristinare il tavolo a uno stato pulito.

## Proprietà Chiave
- `oggettoCorrente` (`DatiOggettoSO`): Dati dell'oggetto in lavorazione.
- `vaschettaCorrente` (`VaschettaSO`): Cast dei dati dell'oggetto a `VaschettaSO` (se applicabile).
- `vaschettaGameObject` (`GameObject`): Il modello fisico della vaschetta o del mosaico posato sul tavolo.
- `faseCorrente` (`FaseRestauroSO`): La fase di restauro attualmente attiva.
- `anforaAssemblata` (`GameObject`): Riferimento persistente all'anfora assemblata a metà restauro.

## Dipendenze
- [DatiOggettoSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/ScriptableObjects/DataModels/DatiOggettoSO.cs)
- [VaschettaSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/ScriptableObjects/DataModels/VaschettaSO.cs)
- [FaseRestauroSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/ScriptableObjects/DataModels/FaseRestauroSO.cs)
