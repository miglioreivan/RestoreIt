# GestoreFrecceRestauro.cs

## Descrizione
Gestisce la visualizzazione di indicatori grafici (frecce tridimensionali fluttuanti) per suggerire al giocatore dove posare gli oggetti sui tavoli da lavoro.

## Responsabilità
- Mostrare o nascondere le frecce in base all'interattività della zona di rilascio (`DropZone_Interaction`).
- Applicare un effetto di fluttuazione sinusoidale verticale alle frecce attive.
- Ruotare le frecce verso il giocatore (effetto Billboard).

## Funzionamento
Nel metodo `Update`, lo script esamina ciascun indicatore registrato. Se la rispettiva `DropZone_Interaction` indica che il giocatore può posare l'oggetto (cioè ha l'oggetto corretto in mano e il tavolo è vuoto), attiva le frecce corrispondenti. Calcola poi una posizione verticale modificata con `Mathf.Sin` per ciascuna freccia per creare l'effetto galleggiamento e orienta le frecce verso la telecamera principale (`LookRotation`).

## Proprietà Chiave
- `indicatori` (`List<IndicatoreTavolo>`): Struttura che associa una dropzone a una lista di modelli di frecce visive (`arrowVisuals`).
- `floatSpeed` / `floatAmplitude`: Velocità e altezza dell'oscillazione.
- `lookAtPlayer` / `lookAtPlayerYOnly`: Controlli di rotazione billboard (escludendo o meno l'asse Y per evitare inclinazioni innaturali).

## Dipendenze
- [InventarioManoSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/ScriptableObjects/RuntimeState/InventarioManoSO.cs)
- [DropZone_Interaction](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/Interaction/DropZone_Interaction.cs)
