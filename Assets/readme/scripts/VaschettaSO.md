# VaschettaSO.cs

## Descrizione
Estensione ScriptableObject di `DatiOggettoSO` contenente le proprietà e i prefabbricati necessari per l'intero ciclo di assemblaggio e incollaggio di un'Anfora.

## Responsabilità
- Conservare i riferimenti ai prefab tridimensionali dell'anfora per le varie fasi (pezzi divisi, ghost assemblato, anfora intera finale).
- Archiviare la maschera di incollaggio delle crepe.
- Configurare i parametri di progressione per il minigioco di incollaggio.

## Proprietà Chiave
- `prefabAnfora` (`GameObject`): Modello ghost dell'anfora assemblata.
- `prefabPezzi` (`GameObject`): Modello contenente i pezzi separati dell'anfora da allineare.
- `prefabAnforaIntera` (`GameObject`): Modello 3D dell'anfora restaurata e intatta da caricare al completamento.
- `mascheraCollaUnica` (`Texture2D`): Maschera in scala di grigi che mappa la posizione delle crepe da coprire di colla.
- `sogliaCompletamentoColla` (`float`): Percentuale di incollaggio richiesta per completare la fase (default: 70%).

## Dipendenze
- Eredita da [DatiOggettoSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/ScriptableObjects/DataModels/DatiOggettoSO.cs)
