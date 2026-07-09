# BottoneStrumento.cs

## Descrizione
Gestisce un pulsante dell'interfaccia utente (UI) per selezionare uno specifico strumento di restauro (es. bisturi, spugna, colla).

## Responsabilità
- Rilevare il click sul componente `Button` di Unity.
- Notificare il cambio dello strumento attivo inviando il relativo ScriptableObject tramite un canale di eventi.

## Funzionamento
All'avvio (`Start`), lo script recupera il componente `Button` sul GameObject corrente e aggiunge un listener all'evento `onClick`. Quando il pulsante viene cliccato, chiama `RaiseEvent()` sul canale ScriptableObject associato, passando il riferimento dello strumento configurato.

## Proprietà Chiave
- `loStrumentoDiQuestoBottone` (`StrumentoRestauroSO`): Lo strumento associato a questo bottone.
- `canaleCambioStrumento` (`StrumentoEventChannelSO`): Il canale di eventi ScriptableObject usato per comunicare il cambio di strumento.

## Dipendenze
- [StrumentoRestauroSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/ScriptableObjects/DataModels/StrumentoRestauroSO.cs)
- [StrumentoEventChannelSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/ScriptableObjects/Events/StrumentoEventChannelSO.cs)
