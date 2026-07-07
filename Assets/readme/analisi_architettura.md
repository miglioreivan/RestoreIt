# Analisi Architetturale & Ottimizzazioni

## Panoramica

Il progetto è un **adventure/puzzle game in Unity** ben strutturato, con un sistema di interazioni polimorfico, un quest system e un inventario. L'architettura di base è solida, ma ci sono diversi pattern problematici che nel tempo portano a bug difficili da tracciare, accoppiamento eccessivo e difficoltà di estensione.

---

## 🔴 Problemi Critici

### 1. `WorldItem` viola il **Single Responsibility Principle**

`WorldItem` fa troppe cose contemporaneamente: è un `IInteractable`, gestisce la logica di ogni tipo di interazione (`HandleRaccogli`, `HandleLeggi`, `HandleDialogo`, `HandleUsaLascia`) **e** gestisce il proprio ciclo di vita (destroy, task check).

```
❌ PROBLEMA: WorldItem agisce come un mega-dispatcher interno
```

**Soluzione → delegare completamente alle `BaseInteraction`**

`WorldItem` dovrebbe essere solo un **contenitore di metadati** (ID, prompt, priorità), delegando **tutta** la logica a `InteractionHandler`. I metodi `Handle*` vanno spostati nelle rispettive classi `*Interaction`.

```
WorldItem           = Dati + identità (ID, prompt, priorità)
InteractionHandler  = Seleziona la migliore interazione
BaseInteraction     = Esegue la logica specifica
```

---

### 2. `InventoryManager` fa troppe cose (**God Class**)

`InventoryManager` contiene:
- Gestione lista item (`Add`, `Remove`, `Has`, `Get`)
- Logica di uso/scarto item (`UseItem`, `DiscardItem`)
- Spawn di item nel mondo (`SpawnDroppedItem`)
- Apertura UI di lettura (`OpenReadingUI`)

Questo accoppia il manager con `InventoryUI`, `ReadableItemData`, `PlayerInteractor` e la fisica del mondo.

**Soluzione → separazione delle responsabilità:**

| Nuova Classe | Responsabilità |
|---|---|
| `InventoryManager` | Solo lista item: Add/Remove/Has/Get |
| `ItemUsageHandler` | Logica use/discard, chiama i servizi necessari |
| `WorldItemSpawner` | Spawn fisico nel mondo |

---

### 3. `UIManager` mischia responsabilità UI eterogenee

`UIManager` gestisce contemporaneamente:
- HUD della quest (titolo + step)
- Reading panel (con cursor lock / input block)
- Notification system con fade e coroutine

Ogni pannello ha lifecycle diverso e dipendenze diverse. Se una coroutine di notifica fallisce, può influenzare la lettura.

**Soluzione → uno script per pannello:**

| Classe | Responsabilità |
|---|---|
| `QuestHUDController` | Aggiorna HUD quest/task |
| `ReadingPanelController` | Gestisce lettura con cursor lock |
| `NotificationController` | Notifiche con fade/coroutine |
| `UIManager` | Coordina i controller, nessuna logica diretta |

---

## 🟡 Problemi Moderati

### 4. Singleton con `Instance` come campo pubblico

`GameManager`, `QuestManager`, `InventoryManager`, `UIManager`, `InteractionUI` espongono tutti `Instance` come campo pubblico `-->`. In Unity questo è un anti-pattern: l'istanza può essere sovrascritta accidentalmente dall'Inspector o da altri script.

**Soluzione → proprietà con setter privato:**

```csharp
// ❌ Prima
public static GameManager Instance;

// ✅ Dopo
public static GameManager Instance { get; private set; }

private void Awake() {
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
}
```

---

### 5. `DodecahedronPuzzle` è fortemente accoppiato al player

`DodecahedronPuzzle` contiene riferimenti diretti a `FPSController` e `FPSCamera` e manipola la camera/input del player durante il puzzle. Questo:
- Rompe il principio di inversione delle dipendenze
- Rende impossibile testare il puzzle senza un player completo
- Crea codice duplicato (camera snap/restore ripetuto altrove)

**Soluzione → un servizio `CameraOverrideService`:**

```
DodecahedronPuzzle --eventi--> CameraOverrideService
CameraOverrideService gestisce: capture/restore camera + lock input
```

`DodecahedronPuzzle` lancia solo `OnPuzzleActivated(puzzleID)` e `OnPuzzleDeactivated()`.

---

### 6. `QuestManager` trova i task per ID con ricerca lineare

```csharp
- FindRuntimeTask(taskID:string) : TaskRuntimeState
```

Internamente usa `List<TaskRuntimeState>`, il che implica una ricerca `O(n)` ogni volta. Con poche quest va bene, ma se le quest crescono:

**Soluzione → `Dictionary<string, TaskRuntimeState>`**

```csharp
// ❌ Prima
private List<TaskRuntimeState> runtimeTasks;
private TaskRuntimeState FindRuntimeTask(string id) => runtimeTasks.FirstOrDefault(t => t.Config.taskID == id);

// ✅ Dopo
private Dictionary<string, TaskRuntimeState> runtimeTaskMap;
private TaskRuntimeState FindRuntimeTask(string id) => runtimeTaskMap.TryGetValue(id, out var t) ? t : null;
```

---

### 7. `ReadableSceneInteraction` duplica `completionType` da `BaseInteraction`

```
BaseInteraction o-> "completionType" InteractionType
ReadableSceneInteraction o-> "completionType" InteractionType   ← DUPLICATO
```

`ReadableSceneInteraction` ridichara `completionType` che già esiste nel padre. In C# questo fa **shadowing** del campo base, il che causa bug sottili se il polimorfismo usa il campo della base.

**Soluzione:** rimuovere la ridichiarazione in `ReadableSceneInteraction` e usare `base.completionType` (o una proprietà protetta nel padre).

---

### 8. `PlayerInteractor` espone due campi camera (`playerCamera` + `PlayerCamera`)

```
PlayerInteractor --> "playerCamera" Camera
PlayerInteractor --> "PlayerCamera" Camera
```

Ci sono due riferimenti alla stessa camera (uno privato, uno come proprietà pubblica). Questo è confusionario e potrebbe causare desync se uno dei due non viene aggiornato.

**Soluzione:** un solo campo `[SerializeField] private Camera _playerCamera` con proprietà pubblica `public Camera PlayerCamera => _playerCamera`.

---

## 🟢 Ottimizzazioni di Qualità

### 9. Introdurre `IQuestService` e `IInventoryService`

Attualmente `QuestManager` e `InventoryManager` sono singleton concreti. Qualsiasi classe che li usa è accoppiata all'implementazione. Per testabilità e mockabilità:

```csharp
public interface IQuestService {
    void LoadQuest(QuestData quest);
    TaskRuntimeState GetCurrentRuntimeTask();
    bool IsCurrentTaskOfType(InteractionType type);
    // ...
}

public interface IInventoryService {
    bool AddItem(ItemData item);
    bool HasItem(string itemID);
    // ...
}
```

`QuestManager` e `InventoryManager` implementano le interfacce. Gli script che le usano dipendono dall'interfaccia, non dalla classe concreta.

---

### 10. `ItemData` con flag booleani vs. polimorfismo

```
+ isPickable : bool
+ isUsable   : bool
+ isDroppable: bool
```

Tre flag booleani che controllano il comportamento. Questo tende a crescere (`isReadable`, `isKey`, `isPuzzlePart`...) producendo una classe piena di `if`. La gerarchia `ItemData → KeyItemData → ReadableItemData` è già un passo nella direzione giusta, ma le flag devono stare nelle sottoclassi, non nel base.

**Soluzione → virtual properties:**

```csharp
public class ItemData : ScriptableObject {
    public virtual bool IsPickable => true;
    public virtual bool IsUsable   => false;
    public virtual bool IsDroppable => true;
}

public class KeyItemData : ItemData {
    public override bool IsUsable => true;
    public override bool IsDroppable => false;
}
```

---

### 11. `FPSController` con troppi stati bool paralleli

```
- isSprinting, isCrouching, isGrounded
- wasMovingLastFrame, wasSprintingLastFrame
- isCurrentlyMoving
```

Sei variabili booleane per lo stato del player. Questo è fragile: se dimentichi di aggiornarne una, lo stato diventa incoerente.

**Soluzione → enum `PlayerMovementState`:**

```csharp
private enum MovementState { Idle, Walking, Sprinting, Crouching }
private MovementState _currentState;
```

Transizioni esplicite, niente stati paralleli incoerenti.

---

## Riepilogo Priorità

| # | Problema | Impatto | Difficoltà |
|---|---|---|---|
| 1 | `WorldItem` God Dispatcher | 🔴 Alto | Media |
| 2 | `InventoryManager` God Class | 🔴 Alto | Alta |
| 3 | `UIManager` responsabilità miste | 🔴 Alto | Media |
| 4 | Singleton con campo pubblico | 🟡 Medio | Bassa |
| 5 | `DodecahedronPuzzle` accoppiato a player | 🟡 Medio | Media |
| 6 | Ricerca task O(n) | 🟡 Medio | Bassa |
| 7 | `completionType` duplicato | 🟡 Medio | Bassa |
| 8 | Due camera fields in `PlayerInteractor` | 🟡 Medio | Bassa |
| 9 | Mancano `IQuestService`/`IInventoryService` | 🟢 Qualità | Media |
| 10 | Flag bool in `ItemData` | 🟢 Qualità | Media |
| 11 | Stati bool paralleli in `FPSController` | 🟢 Qualità | Bassa |

---

> [!TIP]
> Inizia dai problemi 4, 6, 7, 8: sono cambi piccoli, a basso rischio e migliorano subito la leggibilità e la robustezza senza richiedere refactoring strutturali.

> [!IMPORTANT]
> I problemi 1, 2, 3 (God Class/Dispatcher) sono quelli che causeranno più bug e rigidità nel lungo termine. Pianificali come refactoring dedicati, non come micro-fix.
