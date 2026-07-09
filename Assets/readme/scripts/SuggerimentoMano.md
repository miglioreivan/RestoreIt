# SuggerimentoMano

**Percorso**: `Assets/Scripts/UI/SuggerimentoMano.cs`
**Macroarea**: [UI & System Utilities](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/macroareas/UISupport.md)

---

## Scopo

Mostra al giocatore un testo contestuale nell'HUD in base al contenuto della sua mano. Il testo cambia automaticamente ogni volta che l'inventario viene aggiornato, senza polling in `Update`, sfruttando il pattern ad evento offerto da `InventarioManoSO`.

---

## Dipendenze

- **[InventarioManoSO](file:///C:/Users/migli/Documents/Unity%20Projects/RestoreIt/Assets/readme/scripts/InventarioManoSO.md)**: Fornisce l'evento `OnInventarioAggiornato` e i campi `currentGO` e `oggettoCorrente`.
- **`OggettoRestaurato`**: Componente marker utilizzato per determinare se l'oggetto in mano è già stato restaurato.
- **`TextMeshProUGUI`**: Componente UI che visualizza il testo del suggerimento.

---

## Comportamento per Stato

| Stato della mano | Testo mostrato (default) |
| :--- | :--- |
| Mano vuota | *"Raccogli un oggetto da restaurare"* |
| Oggetto non restaurato in mano | *"Porta l'oggetto al tavolo di restauro"* |
| Oggetto restaurato in mano | *"Esponi l'oggetto nel museo"* |

Tutti e tre i testi sono campi serializzati modificabili dall'Inspector senza toccare il codice.

---

## Pattern Tecnico

Lo script si iscrive all'evento `OnInventarioAggiornato` in `OnEnable` e si disiscrive in `OnDisable`, garantendo una corretta gestione del ciclo di vita del componente senza memory leak.

```csharp
private void OnEnable()
{
    if (inventario != null)
        inventario.OnInventarioAggiornato += AggiornaSuggerimento;
}

private void OnDisable()
{
    if (inventario != null)
        inventario.OnInventarioAggiornato -= AggiornaSuggerimento;
}
```

La distinzione tra oggetto normale e restaurato avviene cercando `OggettoRestaurato` sul GameObject in mano e su tutta la sua gerarchia (parent e figli).

---

## Configurazione in Unity

1. Aggiungere il componente a un GameObject persistente nella Canvas principale.
2. Assegnare nell'Inspector:
   - **Inventario**: l'asset `InventarioManoSO` usato dal resto del progetto.
   - **Testo Suggerimento**: il componente `TextMeshProUGUI` dell'HUD.
3. Personalizzare opzionalmente i tre campi testo dall'Inspector.
