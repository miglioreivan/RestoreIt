# OggettoRestaurato.cs

## Descrizione
Componente contrassegno ("Tag Script") applicato dinamicamente o staticamente a un oggetto per certificarne il completamento del restauro.

## Responsabilità
- Identificare univocamente i modelli 3D dei reperti che hanno completato con successo tutte le fasi di restauro.

## Funzionamento
Viene verificato da script come `DropZone_Interaction` e `PedestalDropZone` per consentire o impedire la posa: impedisce di rimettere un pezzo finito sui tavoli di lavoro e sblocca la possibilità di posizionarlo sui piedistalli del museo.
