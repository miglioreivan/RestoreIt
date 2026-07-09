# GestoreIncollaggioMosaico.cs

Gestisce il minigioco di posa della colla o resina consolidante sulle tessere del mosaico tramite pittura UV a runtime. Include un sistema di scansione del viewport per prevenire il soft-lock del gioco sulle aree non visibili del modello.

---

## 📖 Analisi Logica e Funzionale

### 1. Viewport Scanning per Calcolo dei Pixel (`InizializzaMappaPixelColla`)
A differenza delle anfore tridimensionali, il mosaico presenta una superficie piana o semi-curva racchiusa in una cornice. Alcune parti della texture UV possono essere occluse o uscire dai margini inquadrati dalla camera.
Per evitare che i pixel non visibili rimangano non dipinti impedendo il superamento del minigioco, lo script esegue una **scansione spaziale preliminare del viewport**:
- Proietta una griglia discreta (512x512) di raggi geometrici dalla camera verso la scena.
- Se colpisce il mosaico, ricava la coordinata UV.
- Converte la coordinata UV in pixel reali e la registra in una mappa di booleani `pixelRaggiungibili`.
- Nella scansione della maschera di colla, un pixel viene conteggiato come richiesto (`totPixelCollaNecessari++`) **solamente se** è marcato come attivo nella maschera originale **E** risulta visibile/raggiungibile all'interno della mappa.

### 2. Wrap delle Coordinate UV (`WrapUV`)
Alcuni materiali o mesh di mosaico presentano coordinate UV che si ripetono (Tiling) o che escono dall'intervallo standard $[0, 1]$.
Per evitare errori di scrittura fuori dai limiti dell'array della texture, lo script implementa una funzione di avvolgimento matematico (Wrap) delle coordinate UV tramite operatore modulo implicito (sottraendo la parte intera inferiore):
```csharp
private Vector2 WrapUV(Vector2 uv)
{
    uv.x = uv.x - Mathf.Floor(uv.x);
    uv.y = uv.y - Mathf.Floor(uv.y);
    return uv;
}
```
Questo normalizza qualsiasi valore UV all'interno dell'intervallo $[0, 1)$, consentendo il corretto calcolo dell'indice dei pixel in memoria.

### 3. Pittura UV e Sincronizzazione dei Materiali
Durante il disegno, lo script:
- Calcola le coordinate pixel UV corrette nel raggio d'azione del pennello.
- Scrive i pixel bianchi nella texture virtuale `collaTextureInstance`.
- Invoca `SincronizzaTextureColla` per passare a tutti i renderer del mosaico la texture aggiornata, mappata sul parametro shader `_TextureCollaDipingibile`.

### 4. Salvataggio della Texture per la Fase Successiva
Al completamento dell'incollaggio (es. 90% della resina posata):
- Lo script esegue l'effetto di vibrazione del mosaico per simulare il consolidamento.
- Modifica i parametri globali dello shader per abilitare la visualizzazione della pittura (`_mostraPittura = 1`).
- A differenza dell'anfora (che distrugge l'anfora a cocci), il mosaico deve mantenere la colla visibile durante la fase successiva (applicazione garze). Per questo motivo, il metodo `RimuoviMappaColla()` verifica se la fase successiva è attiva e, in tal caso, conserva in memoria la texture per passarla al minigioco delle garze.
