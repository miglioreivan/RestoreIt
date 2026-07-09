# GestoreGarze.cs

Gestisce il minigioco di prelievo, trascinamento 3D e posizionamento di una garza protettiva (o pannello di Aerolam) sulla superficie del mosaico per consolidarne la struttura.

---

## 📖 Analisi Logica e Funzionale

### 1. Istanziazione e Compensazione delle Scale locali
All'attivazione della fase:
- Spawna il prefab della garza (o dell'Aerolam, in base al flag `usaAerolam`) nel punto di origine (`puntoSpawnGarza`).
- Poiché il punto di spawn potrebbe avere una scala non uniforme impostata nell'Editor, lo script ricalcola e compensa immediatamente la scala locale dell'istanza per impedire deformazioni geometriche, dividendo la scala del prefab originale per la scala globale del parent di spawn:
  $$\text{scale} = \left( \frac{\text{prefab.Scale.x}}{\text{parent.lossyScale.x}}, \frac{\text{prefab.Scale.y}}{\text{parent.lossyScale.y}}, \frac{\text{prefab.Scale.z}}{\text{parent.lossyScale.z}} \right)$$

### 2. Spostamento 3D tramite Piano di Drag Ortogonale
Quando il giocatore clicca e trascina la garza:
1. **Calcolo della Profondità ($Z$)**: Definisce la coordinata $Z$ di scorrimento basandosi sulla posizione finale del punto target (`puntoTargetGarza`) rispetto alla telecamera di restauro:
   $$\text{targetDepth} = \text{Vector3.Dot}(\text{puntoTargetGarza.position} - \text{camera.position}, \text{camera.forward})$$
2. **Definizione del Piano di Drag**: Crea un piano ortogonale passante per $Z$ (`dragPlane`) parallelo alla visuale della camera.
3. **Raycast sul Piano**: Ad ogni movimento del mouse, proietta un raggio sul piano virtuale e aggiorna la posizione della garza. Allinea contemporaneamente la rotazione della garza con quella del target per facilitare l'operazione di posizionamento dell'utente.

### 3. Snap e Vincolo Gerarchico
Ad ogni frame del trascinamento, lo script calcola lo scostamento di distanza e angolo tra la garza ed il target del mosaico:
$$\text{distanza} = \text{Vector3.Distance}(\text{garza.position}, \text{puntoTargetGarza.position})$$
$$\text{angolo} = \text{Quaternion.Angle}(\text{garza.rotation}, \text{puntoTargetGarza.rotation})$$

Se le tolleranze sono rispettate ($\text{distanza} \le \text{snapDistance}$ ed $\text{angolo} \le \text{snapAngle}$):
- Lo script aggancia la garza impostando la posizione locale a zero rispetto a `puntoTargetGarza`.
- Ricalcola la scala locale in base alla scala globale del target per preservarne le proporzioni reali nel mondo.
- **Associazione al Reperto**: Riparenta la garza istanziata sotto il GameObject principale del mosaico (`tavoloCorrente.vaschettaGameObject.transform`), in modo che diventi un tutt'uno con il reperto e risponda correttamente a tutte le future rotazioni o spostamenti del tavolo.
- Disattiva il collider della garza per bloccarne definitivamente il movimento.

### 4. Sequenza di Completamento e Pulizia degli Shader
Una volta completato lo snap:
- Attende 1 secondo per dare un riscontro visivo del posizionamento.
- Cerca tutti i renderer del mosaico e imposta a `0` il valore del parametro shader `_mostraColla` per nascondere la colla che era stata stesa nella fase precedente.
- Distrugge la texture temporanea della colla (`collaTextureMosaico`) precedentemente creata in memoria per liberare RAM.
- Avanza alla fase successiva (es. rotazione del mosaico per lavorare sull'altro lato).
