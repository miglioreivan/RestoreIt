# Menu.cs

## Descrizione
Semplice gestore per il menu di gioco iniziale.

## Responsabilità
- Consentire l'avvio del gioco caricando la scena principale.
- Consentire la chiusura dell'applicazione.

## Funzionamento
Espone due metodi pubblici richiamabili da eventi onClick dei pulsanti UI: `StartGame()`, che carica la scena all'indice 1 del Build Settings tramite `SceneManager`, e `Quit()`, che chiude l'eseguibile di gioco.

## Dipendenze
- Utilizza il namespace `UnityEngine.SceneManagement`.
