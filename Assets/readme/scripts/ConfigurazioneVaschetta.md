# ConfigurazioneVaschetta.cs

## Descrizione
Contiene l'elenco ordinato e configurato dei pezzi fisici presenti nella vaschetta di restauro per le anfore.

## Responsabilità
- Mantenere il riferimento ordinato dei pezzi dell'anfora per la fase di assemblaggio.

## Funzionamento
Viene utilizzato come contenitore di dati ("data holder") attaccato al GameObject della vaschetta in scena. Il `GestoreAssemblaggio` legge questa lista per sapere quali pezzi il giocatore deve assemblare e in quale ordine.

## Proprietà Chiave
- `pezziOrdinati` (`List<GameObject>`): Lista ordinata dei pezzi dell'anfora pronti per essere trascinati e snapsullati.

## Dipendenze
- Nessuna dipendenza diretta da altri script. Utilizzato da [GestoreAssemblaggio](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/Restoration/GestoreAssemblaggio.cs).
