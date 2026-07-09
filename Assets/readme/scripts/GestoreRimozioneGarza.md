# GestoreRimozioneGarza.cs

## Descrizione
Gestisce la fase in cui il giocatore deve rimuovere la garza protettiva dal mosaico dopo che questo è stato consolidato e girato.

## Responsabilità
- Cercare la garza presente come figlio del mosaico e dotarla di un collider temporaneo per renderla interattiva.
- Rilevare il click del mouse sulla garza tramite raycast selettivo.
- Distruggere l'oggetto garza e aggiornare lo shader del mosaico per nascondere la pittura provvisoria.
- Avanzare la fase di restauro.

## Funzionamento
All'avvio della fase, lo script effettua una ricerca gerarchica tra i figli del mosaico in scena cercando un GameObject il cui nome contiene "Garza". Se lo trova, gli assegna un `BoxCollider` o `MeshCollider`. Quando l'utente ci clicca sopra (utilizzando un raycast filtrato sul layer "Restauro"), lo script elimina la garza, imposta la proprietà shader `_mostraPittura` a `0` su tutti i materiali del mosaico per pulire la visualizzazione, e avanza di fase.

## Proprietà Chiave
- `nomeOggettoDaCercare` (`string`): Parola chiave gerarchica (default: "Garza").
- `layerRestauro` (`LayerMask`): Layer usato per filtrare i click del mouse.
- `cursorClickTexture` (`Texture2D`): Cursore personalizzato che indica l'azione di rimozione (es. mano o pinzette).

## Dipendenze
- [TavoloSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/ScriptableObjects/RuntimeState/TavoloSO.cs)
- [RestoreManager](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/Restoration/RestoreManager.cs)
