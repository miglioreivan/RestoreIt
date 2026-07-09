# GestoreEsposizione.cs

## Descrizione
Monitora i piedistalli espositivi nella galleria del museo per determinare quando tutti i reperti sono stati posizionati correttamente, innescando gli eventi conclusivi.

## Responsabilità
- Registrare gli eventi di posizionamento da ciascun piedistallo (`PedestalDropZone`).
- Verificare se tutti i piedistalli configurati sono occupati da un oggetto restaurato.
- Lanciare eventi globali di fine partita o sblocchi di gioco al completamento dell'esposizione.

## Funzionamento
Lo script ascolta l'evento `OnObjectPlaced` di ogni piedistallo configurato. Ogni volta che viene posato un oggetto, controlla la lista completa dei piedistalli. Se tutti risultano occupati, imposta `completato` su true e lancia l'evento `onEsposizioneCompletata` e i canali di eventi associati.

## Proprietà Chiave
- `piedistalli` (`List<PedestalDropZone>`): L'elenco dei piedistalli espositivi da tenere sotto controllo.
- `onEsposizioneCompletata` (`UnityEvent`): Evento generato quando tutti i piedistalli sono occupati.
- `onEsposizioneCompletataChannels` (`List<VoidEventChannelSO>`): Canali di eventi aggiuntivi per notificare sistemi esterni.

## Dipendenze
- [PedestalDropZone](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/Interaction/PedestalDropZone.cs)
- [VoidEventChannelSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/ScriptableObjects/Events/VoidEventChannelSO.cs)
