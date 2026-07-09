# Macroarea: ScriptableObjects & Data Model (Modello Dati ed Eventi)

Questa macroarea descrive l'infrastruttura dati del progetto. In **RestoreIt**, i dati di configurazione e lo stato runtime delle stazioni di lavoro e dei canali di comunicazione sono interamente ospitati da **ScriptableObjects**. Questo approccio riduce le dipendenze rigide tra scene ed elementi di gioco, facilitando i test e la manutenzione.

---

## 📂 Gli Script della Macroarea

I ScriptableObjects del progetto si dividono in tre categorie principali:

### 1. Canali Eventi (Event Channels)
- **[VoidEventChannelSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/VoidEventChannelSO.md)**: Canale eventi generico privo di parametri. Utilizzato per notificare completamenti, reset di stato o attivazioni globali (es. quando l'esposizione è completata).
- **[StrumentoEventChannelSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/StrumentoEventChannelSO.md)**: Canale eventi specifico per il passaggio di parametri legati agli strumenti di restauro (`StrumentoRestauroSO`), utilizzato per coordinare l'interfaccia utente dei banchi da lavoro.

### 2. Configurazione e Dati dei Reperti (Data Model Statico)
- **[DatiOggettoSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/DatiOggettoSO.md)**: Classe base che definisce le informazioni identificative di ciascun reperto (nome visualizzato, eventi di interazione).
- **[VaschettaSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/VaschettaSO.md)**: Estende `DatiOggettoSO`. Contiene tutti i riferimenti statici delle anfore (prefab dei cocci disassemblati, prefab del modello assemblato con crepe, prefab del modello pulito finale, texture delle maschere di fango e colla).
- **[MosaicoSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/MosaicoSO.md)**: Estende `DatiOggettoSO`. Contiene le informazioni di configurazione per i mosaici (prefab del mosaico sporco, prefab delle garze/Aerolam e maschere di colla/resina).
- **[StrumentoRestauroSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/StrumentoRestauroSO.md)**: Configurazione degli strumenti del workbench (nome dello strumento, raggio d'azione, cursore grafico personalizzato ed eventi associati).
- **[FaseRestauroSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/FaseRestauroSO.md)**: Definisce le fasi del restauro. Contiene la descrizione testuale da visualizzare nell'HUD (es. "Rimuovi lo sporco superficiale") e riferimenti di ordinamento.

### 3. Stato Dinamico (Data Model a Runtime)
- **[InventarioManoSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/InventarioManoSO.md)**: Stato persistente di ciò che il giocatore stringe in mano. Mantiene il riferimento logico (`DatiOggettoSO`), il riferimento al GameObject fisico e il `Transform` del punto mano.
- **[TavoloSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/TavoloSO.md)**: Rappresenta lo stato runtime di un tavolo da lavoro. Mantiene traccia del reperto attualmente depositato, della fase di restauro corrente, delle texture virtuali temporanee create a runtime (es. la texture della colla dipinta dal giocatore) e notifica i cambi di fase tramite l'evento delegato `OnFaseCambiata`.

---

## 🔄 Disaccoppiamento tramite Event Channels (SO Architecture)

Nelle architetture tradizionali, se un componente UI desidera attivare un minigioco, deve possedere un riferimento diretto (hard reference) al manager di quel minigioco. Questo rende i sistemi rigidi e difficili da testare isolatamente.

L'architettura ad **Event Channels** risolve questo problema introducendo un ScriptableObject come intermediario (Broker):
1. Lo ScriptableObject dell'evento (es. `cambiaStrumentoChannel`) risiede come risorsa (Asset) all'interno del progetto.
2. Il componente UI (`BottoneStrumento`) possiede una referenza all'Asset e, quando cliccato, invoca il metodo `RaiseEvent(strumento)`.
3. Il manager del minigioco (`StrumentoPulizia`) possiede una referenza allo stesso Asset e si sottoscrive all'evento nel metodo `OnEnable`. Quando l'evento viene sollevato, esegue il suo codice interno.

Questo significa che l'interfaccia UI e il manager di gioco non si conoscono a vicenda. Se rimuoviamo la UI dalla scena, il gioco non si rompe (semplicemente l'evento non viene sollevato). Se rimuoviamo il minigioco, la UI continua a funzionare senza generare eccezioni di tipo `NullReferenceException`.

---

## 📈 Configurazione Statica vs Istanze a Runtime

Una distinzione fondamentale da comprendere è la differenza tra i dati configurati negli Asset e gli oggetti istanziati nella scena di gioco:
- **Configurazioni Statiche** (`VaschettaSO`, `MosaicoSO`): Sono risorse di sola lettura create nell'Editor di Unity. Contengono i riferimenti ai prefab originali dei reperti e le texture delle maschere caricate dal comparto artistico. Non devono essere modificate a runtime per evitare di sovrascrivere i file di progetto.
- **Istanze Runtime** (`collaTextureMosaico` su `TavoloSO` o `textureInstance` su `StrumentoPulizia`): Sono oggetti generati dinamicamente in memoria RAM durante l'esecuzione del gioco (es. duplicando una maschera tramite `Graphics.Blit` su una `Texture2D` temporanea). Queste texture temporanee raccolgono le pennellate dell'utente e vengono distrutte al termine della fase o quando il tavolo viene svuotato per liberare memoria.
