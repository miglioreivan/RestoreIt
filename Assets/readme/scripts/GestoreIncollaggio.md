# GestoreIncollaggio.cs

**Percorso**: `Assets/Scripts/Restoration/GestoreIncollaggio.cs`
**Macroarea**: [Restoration Core](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/macroareas/RestorationCore.md)

---

## Scopo

Gestisce il minigioco di stesura della colla sulle crepe dell'anfora assemblata tramite pittura UV su texture 2D guidata da una maschera di incollaggio.

---

## Analisi Logica e Funzionale

### 1. Preparazione dell'Anfora e dei Collider
All'avvio della fase:
- **Reparenting dell'anfora**: Recupera il modello assemblato persistente dal tavolo (`tavoloCorrente.anforaAssemblata`) e lo sposta sotto la postazione di incollaggio.
- **Configurazione dei Collider**: Disabilita tutti i collider del modello guida (ghost) ed assicura che ciascuno dei pezzi possieda un `MeshCollider` attivo. Senza di esso il raycast UV di Unity non calcola `hit.textureCoord` e il disegno fallirebbe.

### 2. Inizializzazione della Texture e della Maschera
Lo script legge la maschera di incollaggio associata al reperto:
- Genera a runtime una texture scrivibile (`collaTextureInstance`) inizializzata con pixel trasparenti.
- Scansiona la maschera: ogni pixel con componente rossa superiore a 128 viene marcato come "richiesto". Incrementa il contatore `totPixelCollaNecessari`.

### 3. Pittura UV a Runtime
Durante il trascinamento del mouse sinistro sul modello:
- Un raycast intercetta le coordinate UV `(x, y)` sul `MeshCollider`.
- Il metodo `PitturaColla` trasforma le UV in pixel della texture e colora di bianco opaco quelli richiesti e non ancora dipinti.
- Ricalcola la progressione normalizzata:

$$\text{progressioneColla} = \frac{\text{pixelCollaDipinti}}{\text{totPixelCollaNecessari}}$$

### 4. Sequenza di Completamento e Sostituzione del Reperto
Al superamento della soglia di completamento:
1. **Effetto di Consolidamento**: Vibrazione oscillatoria casuale di breve durata per simulare la saldatura della colla.
2. **Sostituzione con il Modello Pulito**: Distrugge l'anfora assemblata a cocci e la sostituisce con il prefab del reperto intero (`prefabAnforaIntera`).
3. **Cleanup del Tavolo**: Distrugge il vecchio `vaschettaGameObject` del tavolo, lo azzera a `null`, e assegna la nuova `anforaIntera` a `tavoloCorrente.anforaAssemblata`. Questo garantisce che il [RestoreManager](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/RestoreManager.md) abiliti il collider esattamente sul reperto finale e non su un contenitore ormai vuoto.
4. **Configurazione Materiali e Raccolta**: Imposta i parametri shader a zero, aggiunge il tag [OggettoRestaurato](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/OggettoRestaurato.md) e configura [PickUp_Interaction](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/PickUp_Interaction.md) affinché il giocatore possa raccogliere e portare il reperto al museo.
