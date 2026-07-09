# RestoreManager.cs

**Percorso**: `Assets/Scripts/Restoration/RestoreManager.cs`
**Macroarea**: [Restoration Core](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/macroareas/RestorationCore.md)

---

## Scopo

Controllore centrale del banco da lavoro (Workbench). Agisce come macchina a stati sequenziale che coordina le transizioni della telecamera, disabilita il movimento del giocatore ed attiva i singoli minigiochi di restauro.

---

## Analisi Logica e Funzionale

### 1. Avvio dell'Interazione (`StartInteraction`)
Implementa l'interfaccia [IInteractable](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/Interaction/IInteractable.cs). Quando il giocatore inquadra la postazione e preme `[E]`, il metodo:
- Blocca il movimento disattivando il componente [FirstPersonController](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/Scripts/Player/FirstPersonController.cs).
- Disabilita il collider della postazione (`ImpostaCollider(false)`) per evitare rilevamenti doppi.
- Salva la posizione, la rotazione e il parent della telecamera per ripristinarli all'uscita.
- Sgancia la telecamera dal player (`SetParent(null, true)`) e avvia la transizione fluida verso il punto di lavoro.
- Attiva il canvas dell'interfaccia di restauro.

### 2. Transizione della Telecamera (`TransitionCamera`)
La coroutine calcola un'interpolazione lineare (`Vector3.Lerp` / `Quaternion.Lerp`) tra posizione di partenza e target. Al termine della transizione, individua automaticamente il minigioco attivo nella fase corrente e lo avvia richiamando `CameraTransitionCompleted()` o `IniziaMinigame()`.

```csharp
while (elapsed < transitionDuration)
{
    playerCamera.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / transitionDuration);
    playerCamera.transform.rotation = Quaternion.Lerp(startRot, targetRot, elapsed / transitionDuration);
    elapsed += Time.deltaTime;
    yield return null;
}
```

### 3. Gestione del Ciclo delle Fasi
Sottoscrivendosi all'evento `OnFaseCambiata` di [TavoloSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/TavoloSO.md), il manager:
- Disattiva il GameObject della fase precedente.
- Attiva il GameObject della nuova fase.
- Sposta la telecamera verso il nuovo punto di osservazione.
- Aggiorna le istruzioni testuali nell'HUD.

### 4. Completamento del Restauro (`CompletaRestauro`)
Quando il minigioco notifica il completamento dell'ultima fase:
- Aggiunge il componente `OggettoRestaurato` al reperto finito.
- Riabilita solo il collider direttamente sul GameObject del reperto (senza risalire al parent, per evitare interferenze con la scena).
- Avvia `AutoExitRestoration` per tornare automaticamente in prima persona dopo 1.5 secondi.

### 5. Uscita dall'Interazione (`StopInteraction`)
Il canvas dell'interfaccia di restauro **non viene disattivato** all'uscita, per permettere la visualizzazione di eventuali elementi UI persistenti (es. [SuggerimentoMano](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/SuggerimentoMano.md)). Viene ripristinato il cursore di sistema e avviata la transizione della telecamera verso la posizione originale del giocatore.

---

## Proprietà Chiave

- `tavoloCorrente` ([TavoloSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/TavoloSO.md)): ScriptableObject che persiste lo stato logico e runtime del banco da lavoro.
- `fasiMappate` (`List<FaseMapping>`): Mappa strutturata nell'Inspector che associa ogni fase al GameObject del minigioco e al target di inquadratura.
- `transitionDuration` (`float`): Durata in secondi della transizione della telecamera.
