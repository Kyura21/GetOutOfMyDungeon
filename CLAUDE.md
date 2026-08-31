# Get Out Of My Dungeon (GOOMD)

Gioco Unity 3D: ibrido biliardo/golf in un dungeon. Colpisci un teschio-palla con
una stecca a forma di braccio scheletrico; il teschio esplode al contatto con i nemici.

- Unity **6000.3.21f1**, URP, **new Input System** (`Mouse.current`, `Keyboard.current`)
- Progetto Unity: `GOOMD/GetOutOfMyDungeon/` (annidato: la root del repo e' `UnityTest/`)
- **Repository git** dal 2026-08-31. `git` non e' nel PATH: usa quello di Fork,
  `C:\Users\PC\AppData\Local\Fork\gitInstance\2.50.1\cmd\git.exe`.
  L'utente lavora con **Fork** come client grafico.
- `Library/` (2 GB), `Logs/`, `UserSettings/`, `.vs/` e i progetti IDE sono ignorati:
  Unity li rigenera. Gli asset veri pesano 0,3 MB, quindi **niente Git LFS** per ora.
- I `.meta` vanno sempre committati insieme all'asset: legano i GUID ai riferimenti
  in scena. Cancellarne uno rompe i collegamenti nell'Inspector.
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
- `Level/GameManager.cs` — singleton. Stato `Playing/Won/Lost`, `Power`, `Damage`,
  `fallY` come rete di sicurezza. **Due vie di vittoria indipendenti** (OR): gli
  obiettivi in `AreLevelObjectivesMet()` (nemici + puzzle + soglia danni, in AND)
  oppure la buca. `LastWinReason` dice quale, servira' al punteggio.
  `hasObjectives`, calcolato in `Start()`, evita che un livello senza obiettivi
  configurati si vinca da solo al primo frame.
- `Level/WinHole.cs` — la buca da biliardo. Imbucare **vince da solo**, senza
  bisogno di aver ucciso o risolto niente (cambia solo il punteggio).
  Non chiama `Win()`: setta `IsPotted` e lascia decidere al `GameManager`.
- `Level/HoleOpener.cs` — oggetto da colpire per aprire la buca. E' l'unica cosa
  che la apre: **non** e' legato all'esplosione, cosi' si puo' fare una pacifist
  run. `WinHole.requiredOpeners` vuoto = buca aperta da subito.
- `Level/KillZone.cs` — trigger di morte (lava). Riconosce il teschio da
  `SkullIntegrity`, non da tag.
- `Level/PuzzleTrigger.cs` — segnaposto: qualsiasi meccanismo chiama `Solve()`.

## Trappola da ricordare

`OnCollisionEnter` scatta **dopo** che il solver ha risolto l'urto: `rb.linearVelocity`
li' dentro e' gia' la velocita' di rimbalzo. Per pesare un urto sull'angolo d'impatto
serve la velocita' in ingresso, salvata a fine `FixedUpdate` (`lastVelocity` in
`SkullRolling` e `SkullIntegrity`). Non usare la velocita' letta nel callback.

## Condizioni di vittoria / sconfitta (decise dall'utente)

Gioco **a livelli**. Due modi di vincere, alternativi:

- **Obiettivi**: uccidere i nemici + risolvere i puzzle + raggiungere la soglia
  di `requiredDamage`, tutti in AND (ognuno disattivabile).
- **Buca**: imbucare il teschio nella `WinHole`, che si apre colpendo gli
  `HoleOpener` sparsi nel livello. Vale **da sola**, anche a nemici vivi: e' la
  pacifist run. Cambia il punteggio finale, non l'esito.

**Perdi**: integrita' del teschio a zero (si logora rotolando, **non** subisce
danno dalle esplosioni) oppure caduta fuori mappa (`KillZone` / `fallY`).

Due contatori distinti, da non confondere:

- **Power** — valuta, dai nemici uccisi (`EnemyHealth.powerReward`), da spendere
  in potenziamenti. Non e' mai un obiettivo. Sistema di acquisto non progettato.
- **Damage** — punteggio distruzione stile modalita' Crash di Burnout
  (`damageScorePerHP` sugli HP tolti davvero + `damageScoreOnKill`). Puo' essere
  un obiettivo tramite `requiredDamage`. Per ora lo alimentano solo i nemici:
  quando ci saranno oggetti distruttibili, chiamano `GameManager.AddDamage()`.

## Setup Unity ancora da fare dall'utente

1. Un collider in trigger con `WinHole` dove va la buca
2. Uno o piu' oggetti con `HoleOpener` da colpire, elencati in
   `WinHole.requiredOpeners` (se lasci l'array vuoto la buca e' aperta da subito)

Gia' in scena: `GameManager`, `KillZone`, `SkullRolling` + `SkullIntegrity` +
`SkullExplosion` sullo `Skull_player`, `SkullAimController`, `CameraController`,
un `EnemyHealth`.

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
