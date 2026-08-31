# Get Out Of My Dungeon (GOOMD)

Gioco Unity 3D: ibrido biliardo/golf in un dungeon. Colpisci un teschio-palla con
una stecca a forma di braccio scheletrico; il teschio esplode al contatto con i nemici.

- Unity **6000.3.21f1**, URP, **new Input System** (`Mouse.current`, `Keyboard.current`)
- Progetto Unity: `GOOMD/GetOutOfMyDungeon/`
- **Non e' un repository git.** Niente storia dei commit: lo stato del lavoro vive qui.
- L'utente parla **italiano**: rispondi sempre in italiano. Commenti nel codice in
  italiano senza accenti (per evitare problemi di encoding nei file .cs).

## Scena: `Assets/Scenes/SampleScene.unity`

| GameObject | Componenti rilevanti |
|---|---|
| `Game_Controller` | `SkullAimController` |
| `Main Camera` | `CameraController` |
| `Skull_player` | `SkullExplosion`, SphereCollider (physic mat `Skull_player`), Rigidbody |
| `Skeletal_arm` | la stecca (cue) |
| `Arrow` | root vuoto, figlio `Directional_arrow` con `Animation` legacy + `Pointing.anim` (loop da solo) |
| `Enemy` | cubo, tag `enemy`, BoxCollider + Rigidbody |
| `Plane`, `Test_World`, `Global Volume`, `Directional Light` | ambiente |

## Script (`Assets/Script/`)

- `Player/SkullAimController.cs` — FSM `Idle → Aiming → Powering → Shot`.
  Mira **relativa** (delta del mouse su `targetYaw`, smussata con `Mathf.LerpAngle`),
  **non** raycast: un raycast dalla camera si auto-alimenterebbe, visto che la camera
  in Aim si posiziona proprio in base alla direzione di tiro → giravano entrambi
  all'impazzata. Non tornare a puntare un punto sul terreno.
- `Camera/CameraController.cs` — FSM `Explore / Aim / Follow`. In Aim orbita dietro
  la palla dal lato della stecca (`-ShotDirection`), gia' in posizione al primo frame.
- `Player/SkullExplosion.cs` — esplode su collisione con tag `enemy`, danno **ad area**
  via `Physics.OverlapSphere`. Danneggia solo gli `EnemyHealth`: il teschio non si
  fa male da solo, per costruzione. `ResetExplosion()` viene chiamata da `Shoot()`,
  altrimenti esploderebbe una volta sola per partita.
- `Player/SkullRolling.cs` — frenata scriptata. **PhysX non modella l'attrito di
  rotolamento**, quindi una sfera scivola all'infinito a prescindere dal physic
  material: serve per forza codice. Damping solo orizzontale + `stopThreshold` che
  azzera del tutto. `IsStopped` e' la fonte unica della soglia, letta anche da
  `SkullAimController`.
- `Player/SkullIntegrity.cs` — HP del teschio, si consumano **per metro percorso**,
  con moltiplicatore crescente man mano che si rovina.
- `Enemy/EnemyHealth.cs` — HP impostabili, registro statico `All` / `AliveCount`
  (aggiornato in `OnEnable`/`OnDisable`), flash rosso sul colpo, `powerReward`.
- `Level/GameManager.cs` — singleton. Stato `Playing/Won/Lost`, currency `Power`,
  condizioni di vittoria in AND, `fallY` come rete di sicurezza.
- `Level/KillZone.cs` — trigger di morte (lava). Riconosce il teschio da
  `SkullIntegrity`, non da tag.
- `Level/PuzzleTrigger.cs` — segnaposto: qualsiasi meccanismo chiama `Solve()`.

## Trappola da ricordare

`OnCollisionEnter` scatta **dopo** che il solver ha risolto l'urto: `rb.linearVelocity`
li' dentro e' gia' la velocita' di rimbalzo. Per pesare un urto sull'angolo d'impatto
serve la velocita' in ingresso, salvata a fine `FixedUpdate` (`lastVelocity` in
`SkullRolling` e `SkullIntegrity`). Non usare la velocita' letta nel callback.

## Condizioni di vittoria / sconfitta (decise dall'utente)

- **Vinci**: uccidere i nemici + risolvere i puzzle + guadagnare Power
  (currency interna, da spendere in potenziamenti — sistema non ancora progettato).
- **Perdi**: rottura del teschio (che **non** subisce danno dalle esplosioni, ma si
  logora rotolando) oppure caduta in una zona fuori mappa (lago di lava).

## Setup Unity ancora da fare dall'utente

1. `SkullRolling` + `SkullIntegrity` sullo `Skull_player`
2. Un GameObject con `GameManager` (puo' essere `Game_Controller`)
3. Un collider in trigger con `KillZone` dove va la lava

**Niente e' mai stato compilato in Unity in queste sessioni** (nessuna CLI Unity
disponibile): chiedi conferma che compili prima di dare per buono il codice nuovo.

## Aperti

- Taratura di `wearPerMeter` (1 su 100 di integrita' = 100 m di rotolamento a livello:
  molto stretto, da provare)
- `explosionVFX` e `deathVFX` non assegnati
- Nessuna UI per HP / integrita' / Power
- Sistema di potenziamenti da progettare
- Puzzle veri da progettare (`PuzzleTrigger` e' solo l'aggancio)
- Nemici senza AI: per ora bersagli fermi
