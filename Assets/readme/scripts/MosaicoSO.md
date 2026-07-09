# MosaicoSO.cs

## Descrizione
Estensione ScriptableObject di `DatiOggettoSO` contenente tutti i parametri e i prefabbricati necessari per il restauro di uno specifico Mosaico.

## Responsabilità
- Archiviare i prefab di supporto e restauro (Garza, Aerolam) dedicati a questo mosaico.
- Contenere le texture di maschera per il minigioco di incollaggio (colla frontale e resina posteriore).
- Configurare le soglie di tolleranza e completamento specifiche per il mosaico.

## Proprietà Chiave
- `prefabGarza` / `prefabAerolam` (`GameObject`): I modelli 3D dei materiali di rinforzo da spawnare nelle rispettive fasi.
- `mascheraCollaMosaico` (`Texture2D`): Mappa in scala di grigi che definisce le zone in cui stendere la colla sul fronte.
- `mascheraResinaMosaico` (`Texture2D`): Mappa in scala di grigi che definisce le zone in cui stendere la resina sul retro.
- `sogliaCompletamentoColla` (`float`): Percentuale di pixel dipinti necessaria per completare l'incollaggio del mosaico.

## Dipendenze
- Eredita da [DatiOggettoSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/ScriptableObjects/DataModels/DatiOggettoSO.cs)
