# VoidEventChannelSO.cs

## Descrizione
ScriptableObject Event Channel generico per eventi senza parametri (Void).

## Responsabilità
- Consentire notifiche globali disaccoppiate tra sistemi eterogenei (es. completamento fasi, raccolta oggetti, fine partita).

## Funzionamento
Implementa un semplice delegato `UnityAction` chiamato `OnEventRaised`. I sistemi sorgente richiamano `RaiseEvent()`, che a sua volta invoca tutti i listener registrati, riducendo l'accoppiamento diretto tra le classi.
