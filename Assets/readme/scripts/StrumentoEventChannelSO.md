# StrumentoEventChannelSO.cs

## Descrizione
ScriptableObject Event Channel utilizzato per notificare a livello globale il cambio dello strumento attivo durante il restauro.

## Responsabilità
- Consentire una comunicazione disaccoppiata (Pattern Observer) tra i bottoni della UI e i minigiochi di restauro.

## Funzionamento
I bottoni richiamano `RaiseEvent` passando il nuovo `StrumentoRestauroSO`. I componenti di restauro (es. `StrumentoPulizia`) si registrano all'evento `OnStrumentoCambiato` per aggiornare il cursore visivo e il raggio di azione del pennello.
