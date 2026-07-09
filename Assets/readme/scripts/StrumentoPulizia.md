# StrumentoPulizia.cs

Gestisce il minigioco di pulizia del fango o dello sporco superficiale accumulato sui reperti archeologici tramite un algoritmo di pittura UV su texture 2D a runtime.

---

## 📖 Analisi Logica e Funzionale

### 1. Duplicazione della Maschera in Scrittura
All'avvio della fase, lo script non modifica direttamente la texture dello sporco definita nell'Asset (poiché sovrascriverebbe permanentemente il file di progetto). 
Crea invece una copia runtime scrivibile della maschera dello sporco tramite Render Texture:
```csharp
RenderTexture rt = RenderTexture.GetTemporary(sorgente.width, sorgente.height, 0);
Graphics.Blit(sorgente, rt);
Texture2D nuovaTexture = new Texture2D(sorgente.width, sorgente.height, TextureFormat.RGBA32, false);
nuovaTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
nuovaTexture.Apply();
```
La nuova texture risultante (`textureInstance`) viene assegnata a tutti i materiali del reperto in sostituzione di quella originale.

### 2. Algoritmo di Scansione dei Pixel Visibili (`CountVisiblePixel`)
Una mesh 3D complessa (come un'anfora) presenta zone d'ombra, curve e facce posteriori che non sono inquadrate dalla telecamera di restauro. Se calcolassimo lo sporco totale basandoci sull'intera maschera, il giocatore non potrebbe mai raggiungere il 100% di pulizia, generando un soft-lock.
Il metodo `CountVisiblePixel` risolve questo problema eseguendo una **scansione preliminare del viewport**:
- Proietta una griglia discreta (es. 512x512) di raggi geometrici dalla camera verso la scena.
- Rileva le collisioni sul layer `Restauro`. Se il raggio colpisce un `MeshCollider` valido, estrae la coordinata UV locale (`hit.textureCoord`).
- Moltiplica le coordinate UV per la risoluzione della texture ricavando le coordinate pixel reali.
- Contrassegna tali pixel (e l'area circostante) come "raggiungibili" in un array di booleani `pixelRaggiungibili`.
- Calcola `totPixel` contando solo i pixel che presentano colore sporco (componenti RGB > 12) nella maschera originaria **E** che sono marcati come raggiungibili.

### 3. Pittura UV del Cursore (`UseBrush` e `PitturaPixel`)
Durante il trascinamento del mouse a sinistra premuto:
- Il raycast individua la collisione e ne estrae la coordinata UV.
- Il metodo `PitturaPixel` esegue un cerchio di scansione (di raggio pari a `rangePaintbrush`) centrato sul pixelUV colpito.
- Per ogni pixel all'interno del cerchio che presenta ancora sporco (RGB > 12), sovrascrive un colore nero opaco (`0, 0, 0, 255`) nella texture virtuale, incrementando il contatore `pixelPainted` ed impostando la variabile di modifica.
- Se la texture è stata modificata, applica i pixel e ricalcola il rapporto normalizzato `progression`:
  $$\text{progression} = \frac{\text{pixelPainted}}{\text{totPixel}}$$
- Se `progression` supera la soglia di completamento (es. 95%), disattiva lo strumento, azzera la visualizzazione dello sporco sul materiale impostando il parametro `_mostraTerra` a `0`, e notifica al tavolo l'avanzamento della fase.
