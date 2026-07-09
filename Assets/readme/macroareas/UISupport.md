# Macroarea: UI & System Utilities (Interfaccia Utente e Menu)

Questa macroarea gestisce tutti gli elementi visivi 2D di supporto al gameplay, le schermate di caricamento/menu principale e i pulsanti dinamici utilizzati per cambiare gli strumenti di restauro a runtime.

---

## 🛠️ Gli Script della Macroarea

1. **[BottoneStrumento](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/BottoneStrumento.md)**: Gestisce i pulsanti dell'HUD di restauro (quando premuti, sollevano l'evento sul canale `StrumentoEventChannelSO` per notificare a tutti i minigiochi quale strumento è stato equipaggiato).
2. **[Menu](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/Menu.md)**: Controlla le schermate del menu principale, del menu di pausa, la transizione tra le scene e la chiusura dell'applicazione.
3. **[ConfigurazioneVaschetta](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/ConfigurazioneVaschetta.md)**: Script di supporto applicato alla vaschetta fisica 3D per contenere la lista dei singoli cocci in ordine gerarchico, permettendo al gestore dell'assemblaggio di leggerli ordinatamente.
4. **[SuggerimentoMano](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/SuggerimentoMano.md)**: Mostra un testo HUD contestuale reagendo all'evento `OnInventarioAggiornato` dell'inventario. Cambia il suggerimento in base a tre stati: mano vuota, oggetto da restaurare in mano, oggetto già restaurato in mano.

---

## 🔄 Il Flusso di Cambio Strumento (Tool Switching)

Durante la pulizia e l'incollaggio, il giocatore può scegliere tra diversi strumenti (pennello grande, pennello piccolo, colla, resina, ecc.). Il flusso di selezione è gestito in modo disaccoppiato:

1. **Il Clic del Giocatore**: L'utente clicca sull'icona di uno strumento nel Canvas UI.
2. **Raise Event**: Lo script `BottoneStrumento` associato a quel pulsante intercetta il clic e richiama:
   ```csharp
   strumentoEventChannel.RaiseEvent(datiStrumento);
   ```
3. **Ricezione dell'Evento**: I minigiochi attivi (come `StrumentoPulizia` o `GestoreIncollaggio`) registrano il cambio di strumento:
   - Aggiornano il raggio d'azione del pennello (`rangePaintbrush` o `rangePennelloColla`) leggendo il parametro dal ScriptableObject `strumento`.
   - Modificano il cursore visibile a schermo richiamando `Cursor.SetCursor(strumento.cursorTexture, ...)` per fornire un feedback visivo immediato (es. il cursore si trasforma in una spatola o in un flacone di colla).

Questo sistema consente di aggiungere nuovi strumenti semplicemente creando un nuovo file Asset `StrumentoRestauroSO` nell'Editor di Unity ed associandolo ad un nuovo pulsante UI, senza dover modificare il codice dei minigiochi.
