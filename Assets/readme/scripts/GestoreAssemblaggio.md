# GestoreAssemblaggio.cs

**Percorso**: `Assets/Scripts/Restoration/GestoreAssemblaggio.cs`
**Macroarea**: [Restoration Core](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/macroareas/RestorationCore.md)

---

## Scopo

Gestisce il minigioco di assemblaggio tridimensionale (puzzle 3D). Il giocatore afferra i cocci dell'anfora sparsi nella vaschetta e li posiziona nella collocazione spaziale corretta sopra il modello guida (ghost).

---

## Analisi Logica e Funzionale

### 1. Inizializzazione della Scena
All'avvio della fase:
- **Generazione del Modello Guida (Ghost)**: Spawna l'anfora intera al centro del tavolo. Tutti i collider del ghost vengono disabilitati per non intercettare i raycast del giocatore sui cocci.
- **Configurazione dei Cocci**: Legge la lista ordinata dei cocci da [ConfigurazioneVaschetta](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/ConfigurazioneVaschetta.md) e memorizza per ciascuno posizione locale, rotazione locale e scala locale corrette che dovranno assumere sul modello assemblato.

### 2. Algoritmo di Drag & Drop 3D su Piano Ortogonale
Quando il giocatore tiene premuto il tasto sinistro del mouse sopra un frammento:
1. **Disaccoppiamento dal Parent**: Il cocco viene scollegato temporaneamente dalla vaschetta (`SetParent(null, true)`) per evitare deformazioni geometriche (shearing/skewing) causate dalla scala non uniforme della vaschetta stessa. Il parent originale viene salvato in `parentBeforeDrag` per essere ripristinato in caso di rilascio senza snap.
2. **Calcolo della Profondità ($Z$) del Target**:
   $$\text{targetDepth} = \text{Vector3.Dot}(\text{targetWorldPos} - \text{cameraPos}, \text{cameraForward})$$
3. **Definizione del Piano di Scorrimento**: Crea un piano geometrico (`Plane`) parallelo alla telecamera e posizionato alla profondità calcolata.
4. **Spostamento dell'Oggetto**: Ad ogni frame, proietta un raggio dal cursore sul piano virtuale e sposta il frammento sul punto di collisione.

### 3. Correzione della Scala Durante il Drag
Con il parent impostato a `null`, `localScale` coincide con la scala globale. La scala viene applicata direttamente come prodotto della scala mondiale del ghost per la scala locale originale del cocco, eliminando gli artefatti di shearing:

```csharp
Vector3 targetWorldScale = Vector3.Scale(ghostAnfora.transform.lossyScale, pezzoTrascinato.originalLocalScale);
pezzoTrascinato.gameObject.transform.localScale = targetWorldScale;
```

### 4. Logica di Allineamento e Snap
Durante il trascinamento, lo script calcola in tempo reale la distanza tra il frammento e il suo punto target:
$$\text{distanza} = \text{Vector3.Distance}(\text{frammento.position}, \text{targetWorldPos})$$

Se la distanza scende sotto `snapDistance`, il pezzo viene agganciato, riparentato sotto l'anfora assemblata e bloccato nelle trasformazioni originali registrate all'avvio.

Se rilasciato **senza** snap, il parent originale (`parentBeforeDrag`) viene ripristinato prima di reimpostare posizione, rotazione e scala iniziali, riportando il cocco nella vaschetta senza distorsioni.

### 5. Completamento del Puzzle
Quando tutti i cocci presentano `isSnapped = true`:
- L'anfora assemblata viene assegnata a `tavoloCorrente.anforaAssemblata` per la persistenza nelle fasi successive.
- Lo script avanza alla fase successiva o completa il restauro.
