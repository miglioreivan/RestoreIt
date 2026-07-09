# DatiOggettoSO.cs

## Descrizione
ScriptableObject base che descrive i dati identificativi di un oggetto interattivo o raccoglibile.

## Responsabilità
- Archiviare le informazioni base dell'oggetto (es. il nome).
- Offrire un punto di aggancio per notificare l'evento di raccolta ad altri sistemi (es. visualizzazione UI o quest).

## Metodi Chiave
- `EseguiInterazione()`: Metodo virtuale invocato quando l'oggetto viene raccolto. Esegue un debug e notifica il canale eventi `raccogliOggetto` (se assegnato).

## Proprietà Chiave
- `nomeOggetto` (`string`): Nome visualizzato dell'oggetto.
- `raccogliOggetto` (`VoidEventChannelSO`): Canale opzionale per notificare a livello globale la raccolta dell'oggetto.

## Dipendenze
- [VoidEventChannelSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/ScriptableObjects/Events/VoidEventChannelSO.cs)
