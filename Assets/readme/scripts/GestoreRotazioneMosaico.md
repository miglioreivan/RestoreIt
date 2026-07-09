# GestoreRotazioneMosaico.cs

## Descrizione
Gestisce l'animazione di rotazione automatica (flip / capovolgimento) del mosaico sul workbench.

## Responsabilità
- Eseguire una rotazione controllata e fluida del mosaico (es. di 180 gradi per esporre il retro).
- Gestire le tempistiche con una coroutine interpolata.
- Avanzare alla fase successiva al termine dell'animazione.

## Funzionamento
All'inizio della fase (e una volta completata la transizione della telecamera), lo script avvia la coroutine `EseguiRotazione`. Questa prende il GameObject del mosaico presente sul tavolo e ne interpola la rotazione da quella corrente a quella finale (es. ruotato di 180 gradi sull'asse X) usando `Quaternion.Slerp` combinato con `Mathf.SmoothStep` per un effetto elegante e smorzato. Al termine della rotazione, attende una breve pausa di enfasi visiva e cambia la fase del tavolo.

## Proprietà Chiave
- `durataRotazione` (`float`): Il tempo in secondi impiegato per completare il flip.
- `angoliRotazione` (`Vector3`): L'angolo espresso in gradi di cui ruotare il mosaico (default: 180, 0, 0).

## Dipendenze
- [TavoloSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/ScriptableObjects/RuntimeState/TavoloSO.cs)
- [FaseRestauroSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/ScriptableObjects/DataModels/FaseRestauroSO.cs)
