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
- **`Docs/TUNING.md`** e' la wiki di taratura: ogni parametro esposto negli script,
  cosa fa, il valore attuale in scena, i legami fra valori e i conti di bilanciamento.
  Va aggiornata quando si aggiunge o rinomina un campo pubblico.

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
- `Enemy/EnemyHealth.cs` — HP impostabili, registro statico `All` / `AliveCount`.
  `deathBehaviour`: `Destroy` / `Deactivate` / **`Wreck`** (il corpo resta in scena).
  Ottimizzato per le folle: `AliveCount` e' un contatore mantenuto, non una
  scansione (lo leggono ogni frame GameManager e HUD); il registro si toglie in
  tempo costante scambiando l'ultimo elemento; il flash rosso usa un
  `MaterialPropertyBlock` (niente materiale istanziato per nemico) dentro una
  coroutine, quindi **non c'e' nessun `Update` sui nemici**.
- `Enemy/EnemyWreck.cs` — al primo colpo il corpo esce dai binari e diventa un
  birillo: rotola, ingombra, spinge e **danneggia gli altri nemici urtandoli**.
  E' il pezzo che produce le carambole. Danneggia solo mentre e' libero,
  altrimenti il traffico si autodistruggerebbe negli ingorghi. Il numero di
  rottami contemporanei ha un tetto (`GameManager.maxWrecks`): e' il costo
  dominante quando la scena si riempie.
- `Enemy/EnemyArmor.cs` — corazza direzionale (cono attorno a `forward`) e/o soglia
  di velocita'. Il colpo bloccato **non fa esplodere** il teschio: rimbalza.
  Il controllo lo fa `SkullExplosion` prima di esplodere, non un `OnCollisionEnter`
  qui: l'ordine dei due callback sarebbe casuale.
- `Enemy/EnemyExplosive.cs` — esplode morendo e innesca i vicini. Si aggancia da
  solo a `onDied`. La catena si risolve nello stesso frame, `HasExploded` la chiude.
- `Enemy/EnemyAbsorber.cs` — mangia la velocita' del teschio nell'urto. Serve solo
  se `freezeSkullOnExplode` e' spento.
- `Enemy/EnemyPatrol.cs` — percorso su N waypoint. Rigidbody **kinematic** mosso con
  `MovePosition` (uno dinamico spostato a mano combatte col solver); torna dinamico
  alla morte, cosi' l'esplosione lo spedisce via.
- `Enemy/EnemyLaneSpawner.cs` — strada a N corsie da A a B, curvabile con
  `waypoints` intermedi (spline Catmull-Rom campionata in una spezzata, misurata
  per lunghezza d'arco cosi' la velocita' resta costante nelle curve). Per corsia:
  densita', verso di marcia, cadenza irregolare, velocita' con variazione per
  singolo nemico, budget, sacchetto pesato di tipi da cui pescare a ogni comparsa.
  Genera in `Awake()` per farsi contare da `GameManager.Start()`. Il budget conta
  i nemici che **passano**, non quelli vivi. Chi ha qualcuno davanti rallenta
  invece di attraversarlo (`minGap`/`slowdownGap`): niente compenetrazioni e
  ingorghi che si sciolgono da soli. Il traffico non rallenta per il teschio, di
  proposito. Uccidere un nemico provoca un **incidente**: gli altri della sua
  corsia escono dai binari, vengono spinti via e il traffico si ferma per
  `crashRecovery`.
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

- **Obiettivi**: uccidere tutti i nemici (`requireAllEnemiesDead`) e/o ucciderne N
  (`requiredKills`) + risolvere i puzzle + raggiungere la soglia di `requiredDamage`,
  tutti in AND (ognuno disattivabile). `Kills` e' un conto **cumulativo** sul
  `GameManager`, non `EnemiesAtStart - AliveCount`: con uno spawner attivo i nemici
  continuano ad arrivare. Con lo spawner si usa `requiredKills` e si spegne
  `requireAllEnemiesDead`, che non si soddisferebbe mai.
- **Buca**: imbucare il teschio nella `WinHole`, che si apre colpendo gli
  `HoleOpener` sparsi nel livello. Vale **da sola**, anche a nemici vivi: e' la
  pacifist run. Cambia il punteggio finale, non l'esito.

**Perdi**: integrita' del teschio a zero (si logora rotolando, **non** subisce
danno dalle esplosioni) oppure caduta fuori mappa (`KillZone` / `fallY`).

## Il modello di riferimento: Burnout 3, modalita' Crash

Deciso dall'utente, vale per **tutto** il design delle collisioni: lo schianto e'
l'obiettivo, non un fallimento. Ogni cosa contro cui il teschio si schianta deve
reagire in modo suo — fare punti danno, respingerlo, rallentarlo, esplodere,
innescare qualcos'altro — e la varieta' di quelle reazioni e' il gioco.

Conseguenze pratiche:

- Il punteggio **Damage** e' il punteggio principale della modalita'. Per ora lo
  alimentano solo i nemici (`EnemyHealth` ed `EnemyExplosive`): quando ci saranno
  oggetti di scena distruttibili dovranno chiamare `GameManager.AddDamage()`.
  **Un componente per i props distruttibili non esiste ancora ed e' il pezzo
  mancante piu' evidente.**
- Una reazione all'urto che non da' ne' punti ne' un vantaggio tattico e' fuori
  bersaglio: `EnemyArmor` respinge (e con `bounceKeep` > 1 rilancia), `EnemyAbsorber`
  rallenta, `EnemyExplosive` innesca la catena.
- Fermare il giocatore e basta e' la reazione peggiore: attenzione a
  `freezeSkullOnExplode`, che spento rende possibili le catene di schianti.

### Oggetti di scena gia' progettati (da costruire, in quest'ordine di idee)

Decisi dall'utente, non ancora implementati. Serviranno tutti un componente base
tipo `Destructible` che reagisce all'urto sopra una velocita' minima e chiama
`GameManager.AddDamage()`.

- **Boulder** — molto resistente, **danneggia il teschio** invece di subire, e a
  contatto lo fa esplodere. E' l'ostacolo che punisce lo schianto invece di
  premiarlo, quindi va gestito con `SkullIntegrity.ApplyWear` / `Break`.
- **Casse e forzieri** — si rompono e danno qualcosa. I forzieri in particolare
  danno **Power** (`GameManager.AddPower`), che finora arriva solo dai nemici:
  saranno la seconda fonte di valuta.
- **Torce e simili** — applicano uno **stato** al teschio (prende fuoco).

## Stati / potenziamenti del teschio (LA PROSSIMA COSA DA FARE)

Deciso dall'utente. Il teschio deve poter assumere degli stati che ne cambiano le
proprieta' e il comportamento all'impatto. Esempi fatti: **ghiaccio**, **fuoco**,
**rimbalzo** molto elevato, **acido**, **crea tornado** dove impatta.

Due vie di attivazione, entrambe previste:

1. raccogliendo oggetti nel livello (le torce di cui sopra)
2. acquistandoli con **Power** da un menu dei potenziamenti, ancora da progettare

L'utente sta ancora decidendo i dettagli, ma la **struttura degli stati va fatta
per prima**: e' il presupposto sia delle torce sia del negozio, ed e' oggi il
pezzo mancante piu' importante. Va progettata in modo che uno stato possa
toccare fisica (rimbalzo, attrito), integrita', danno e reazione all'impatto,
visto che gli esempi coprono tutte e quattro le cose.

Due contatori distinti, da non confondere:

- **Power** — valuta, dai nemici uccisi (`EnemyHealth.powerReward`), da spendere
  in potenziamenti. Non e' mai un obiettivo. Sistema di acquisto non progettato.
- **Damage** — punteggio distruzione stile modalita' Crash di Burnout
  (`damageScorePerHP` sugli HP tolti davvero + `damageScoreOnKill`). Puo' essere
  un obiettivo tramite `requiredDamage`. Per ora lo alimentano solo i nemici:
  quando ci saranno oggetti distruttibili, chiamano `GameManager.AddDamage()`.

## Setup Unity

Completo dal 2026-09-08. In scena: `GameManager`, `KillZone` (su `Lava`),
`SkullRolling` + `SkullIntegrity` + `SkullExplosion` sullo `Skull_player`,
`SkullAimController`, `CameraController`, tre `EnemyHealth`, `Win_hole` con
`WinHole` e due `Hole_opener` con `HoleOpener` (collegati in `requiredOpeners`).
Manca solo `PuzzleTrigger`, che non ha ancora un meccanismo vero dietro.

**Niente e' mai stato compilato in Unity in queste sessioni** (nessuna CLI Unity
disponibile): chiedi conferma che compili prima di dare per buono il codice nuovo.

## Aperti

- Taratura di `wearPerMeter` (1 su 100 di integrita' = 100 m di rotolamento a livello:
  molto stretto, da provare)
- `explosionVFX` e `deathVFX` non assegnati
- Sistema di potenziamenti da progettare
- Puzzle veri da progettare (`PuzzleTrigger` e' solo l'aggancio)
- Nessun prefab di nemico: gli archetipi sono componenti da comporre a mano,
  e `EnemyLaneSpawner` un prefab lo vuole per forza
- I nemici non reagiscono alla posizione del teschio: pattugliano e basta
