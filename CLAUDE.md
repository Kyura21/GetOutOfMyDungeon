# Get Out Of My Dungeon (GOOMD)

Gioco Unity 3D: **action puzzle** con un golf in un dungeon dentro. Controlli uno
scheletro **in terza persona**; la sua testa e' la pallina. La colpisci con una mazza
scegliendo **direzione, angolo e potenza**, e la testa esplode al contatto con i nemici.
Il personaggio puo' anche menare con la mazza a mano, ma e' molto meno efficace: **la
palla resta l'arma vera**.

Era un ibrido biliardo/golf: una stecca, tiri sempre rasi, nessun personaggio (la camera
volava libera). Cambiato dall'utente il **2026-10-04** in due passaggi:

1. **stecca → mazza da golf**, perche' con tiri solo rasi non si possono costruire livelli
   con verticalita': un tiro raso non scavalca niente;
2. **camera libera → personaggio in terza persona**, per mescolare il golf alla modalita'
   Crash di Burnout e farne un action puzzle.

Il modello di riferimento per i comandi e' **Super Battle Golf**. Attenzione alle fonti:
la voce Wikipedia **non descrive il sistema di tiro** (parla solo del multiplayer), e i
comandi li sappiamo da una guida di terze parti. Quello che conta e' quello che l'utente
ha visto nel gioco e ha descritto.

**`Docs/GetOutOfMyDungeon/Storico.md`** raccoglie le scelte provate e scartate con il loro motivo. Leggilo
prima di "correggere" qualcosa che sembra strano: spesso e' strano per un motivo.

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
- La documentazione vive in un **vault di Obsidian**, `Docs/GetOutOfMyDungeon/`, creato
  dall'utente. Si collegano fra loro con i wikilink `[[Nota]]`. Tre note:
  - **`GOOMD.md`** e' la nota di apertura: cos'e' il gioco, i comandi, l'indice.
  - **`Taratura.md`** (era `Docs/TUNING.md`) e' la wiki di taratura: ogni parametro
    esposto negli script, cosa fa, il valore attuale in scena, i legami fra valori e i
    conti di bilanciamento. Va aggiornata quando si aggiunge o rinomina un campo pubblico.
  - **`Storico.md`** (era `Docs/STORICO.md`) e' il registro delle scelte scartate, con il
    motivo. Va aggiornato quando una strada viene abbandonata o quando l'utente corregge
    una direzione.
  `CLAUDE.md` resta nella radice del repo, fuori dal vault, perche' e' il file che
  Claude Code legge da solo.
- L'utente vuole **spiegazioni in italiano semplice**: frasi corte, soggetto-verbo-oggetto,
  niente giri di parole ne' frasi a effetto. Vale per la documentazione e per le risposte.

## Scena: `Assets/Scenes/SampleScene.unity`

| GameObject | Componenti rilevanti |
|---|---|
| `Game_Controller` | `SkullAimController` |
| `Main Camera` | `CameraController` |
| `Skull_player` | `SkullExplosion` + `SkullRolling` + `SkullIntegrity` + `SkullHoming`, SphereCollider (physic mat `Skull_player`), Rigidbody |
| `Player` | capsula + CapsuleCollider + Rigidbody + `PlayerController` + `PlayerSwing` |
| `Player_model` (figlio di `Player`) | la parte visibile, collegata a `PlayerController.visual`: si inclina nel tuffo |
| `Skeletal_arm` (sotto `Player_model`) | la mazza, `PlayerSwing.clubPivot`. Ha un CapsuleCollider che `PlayerSwing` **spegne** all'avvio |
| `club_head` (figlio di `Skeletal_arm`) | la testa della mazza, `PlayerSwing.clubHead`. Ha una SphereCollider, spenta anche lei |
| `Mazza a mira` (figlio di `Player`) | la posa di mira, `PlayerSwing.addressPose`: copia della mazza, renderer spento, niente collider |
| `Game_Manager` | `GameManager` |
| `HUD` (Canvas) | `HUDController` — generato da `GOOMD > Crea HUD in scena` |
| `Shot_Camera`, `Shot_Camera_Rig` | la telecronaca, `ShotCamera` sul rig |
| `Enemy`, `Enemy (1)`, `Enemy (2)` | cubi, tag `enemy`, `EnemyHealth`, BoxCollider + Rigidbody. `EnemyStun` lo aggiunge `EnemyHealth` all'avvio se manca |
| `Win_hole` | `WinHole`, CapsuleCollider in trigger |
| `Hole_opener`, `Hole_opener (1)` | `HoleOpener`, collegati in `WinHole.requiredOpeners` |
| `Lava` | `KillZone` |
| `wall`, `Plane`, `Test_World`, `Global Volume`, `Directional Light` | ambiente |

`Arrow` (la freccia di mira) e' stata cancellata dalla scena il 2026-10-13.

Prefab in `Assets/Prefabs/`: `Enemies/Enemy_Absorber`, `Enemies/Enemy_Explosive`
(tag `enemy`, `maxHP` 3). `VFX/` la crea `GOOMD > Crea VFX esplosione`.

**`Reference/`** (radice del repo) contiene gli screen con cui l'utente ha spiegato la mira
e lo swing: frame di Super Battle Golf e screen della scena con i disegni sopra. Guardali
prima di toccare postura, pose o arco dello swing.

## Script (`Assets/Script/`)

- `Player/PlayerController.cs` — il personaggio in terza persona: una capsula con la mazza
  in mano. WASD relativo alla camera, **shift** corre, **alt** si abbassa e rallenta (la
  `CapsuleCollider` si accorcia tenendo i piedi dov'erano), **spazio** salta, **ctrl** si
  tuffa (a terra o in aria: e' la schivata; in volo non si sterza, atterrati ci si rialza
  per `getUpTime` andando piano; si inclina solo `visual`, il collider resta dritto).
  **Tasto destro TENUTO = modalita' tiro**, lasciato = si esce; se resta premuto dopo un
  tiro, finita la frustata si rientra da soli. Due modi: `Free` e `Shooting`.
  **Nessun controllo sulla palla** per entrare in mira: si entra sempre (non a mezz'aria,
  in tuffo o rialzandosi), cosa colpire lo decide il giocatore.
  In mira il corpo sta **di fianco al tiro** (`stanceAngle` 90, come un golfista: deciso
  dall'utente) e si sposta in modo che la testa della mazza cada su un **ancoraggio**
  fissato davanti a dove guardava: e' il "si sposta a sinistra e la mazza diventa
  protagonista". Girando la mira gli gira attorno. **WASD in mira** (anche in carica)
  sposta l'ancoraggio a piccoli passi (`aimStepSpeed`), come sul green. Rientrando in mira
  gia' in postura l'ancoraggio resta dove sta la mazza (`AnchorHere`), altrimenti il corpo
  rifaceva il passo di lato.
  **Il corpo non viene mai spostato a mano e resta sempre dinamico**: anche in mira si
  muove dando una velocita', cosi' i muri lo fermano. Un piazzamento automatico attorno
  alla palla c'era e lo trascinava dietro la palla in volo: vedi lo Storico.
  **Dopo il tiro torna libero appena finisce la frustata**, senza aspettare la palla.
  Mette il Rigidbody in `Interpolate` in `Awake()`, per la camera incollata. Dice alla
  camera quando si mira (`CameraController.Aiming`).
  **Il corpo passa attraverso la palla** (`ignoreBall`: `Physics.IgnoreCollision` in
  `Start()` fra i collider del corpo e quelli della palla). Prima la spostava camminandole
  addosso o mettendosi in postura. Voluto anche che una palla che rotola lo attraversi:
  la si raccogliera' con E (vedi "La prossima funzionalita'").
- `Player/PlayerSwing.cs` — **l'unico script che muove la mazza**, e ne fa due usi.
  **Swing da golf** in mira: la mazza va nella **posa di mira** (`addressPose`, davanti
  alla faccia, testa a terra: e' partenza e punto d'impatto), caricando sale lungo un
  **arco calcolato** verso destra, al rilascio ripassa sul punto e sale a sinistra nel
  **follow-through** (si scrive cosi', non tradurlo), che scala con la carica; poi rientra
  ripercorrendo l'arco al contrario (`recoverTime`). L'arco ruota attorno a `swingPivot`,
  un punto **dietro il collo** (deciso dall'utente: non riportarlo sulle spalle), su un
  piano **inclinato** calcolato dalla posa di mira (contiene pivot -> testa della mazza e
  la linea delle spalle): la testa sale e gira attorno alla capsula insieme. Salendo il
  raggio si stringe (`topRadius`). `backswingAngle` positivo, `followThroughAngle`
  negativo. E' un segnaposto per un'animazione vera.
  **Sfera di colpo**: sotto la testa della mazza (`clubHead`) nella posa di mira,
  appoggiata a terra, raggio `strikeRadius`. **Tutto quello che la tocca viene colpito**,
  palla e nemici insieme. Conta il **contatto** (`IsInStrikeZone(Collider)` con
  `ClosestPoint`), non il centro: una palla grande tocca la mazza col bordo. Non si vede
  in gioco, solo come gizmo.
  **La mazza non ha collider**: `Start()` spegne quelli della mazza e dei figli. Era figlia
  del Rigidbody del corpo e spingeva via la palla dalla sfera prima del colpo.
  Le pose sono oggetti vuoti figli del personaggio, creati dal menu del componente
  (tasto destro sul titolo di `PlayerSwing`): `GOOMD - 1. Salva posa di riposo`, `2. Salva
  posa di mira`, `3. Rimetti la mazza a riposo`, `Crea testa della mazza`. Leggono la
  posizione attuale di `Skeletal_arm`.
  Fuori dalla mira e' il **colpo a mano** col tasto sinistro: portata corta (`range`,
  `arc`), cooldown.
  **La mazza non fa danno: stordisce** (deciso dall'utente il 2026-10-13, la palla deve
  restare l'arma vera). Vale per tutto quello che la mazza colpisce senza essere la palla:
  colpo a mano e nemici presi dallo swing. `HitTarget` chiama `EnemyStun.Stun(impulso,
  moltiplicatore)`: lo swing al 100% o oltre stordisce piu' a lungo
  (`perfectStunMultiplier`, `overchargeStunMultiplier`, scelti da `OnShotReleased` in base
  alla `PowerZone`). Tolti `damage` e `knockWrecks`. **La mazza non provoca l'incidente di
  corsia**: lo provoca solo la testa, ed e' voluto.
- `Player/SkullAimController.cs` — FSM `Idle → Aiming → Powering → Idle`. Lo stato `Shot`
  (aspettare che la palla si fermi) **non esiste piu'**: bloccava il personaggio. Chi deve
  sapere che e' partito un tiro legge il contatore `BallHits`. Tiro da golf: la
  **direzione parte da dove guarda il personaggio** (`BeginAim(facing)`) e ruota verso
  dove guarda la camera (`aimAlignSpeed`); resta viva anche in carica (**non bloccarla**:
  deciso dall'utente).
  **L'angolo si sceglie sempre** (in `Update`, in ogni stato tranne `Powering`): rotella
  (`minLoftAngle`..`maxLoftAngle`, normalizzato 0-1, misurato in **giri di rotella** via
  `loftPerRevolution` / `notchesPerRevolution`) oppure **tasti numerici 1-9**, riga in alto
  e tastierino (`loftPresets`, frazioni del ventaglio). Non si azzera entrando in mira;
  `startLoftRatio` vale solo a inizio livello. **In carica l'angolo e' bloccato**: per
  cambiarlo si esce dalla mira (deciso dall'utente). Premendo il tasto sinistro l'angolo e'
  confermato e **parte la carica**; si tira **rilasciando**. `CancelAim()` esce senza
  tirare (il destro lasciato annulla anche in carica: voluto, per i cambi improvvisi).
  **La carica va fino al 110%**: `chargeTime` = secondi per il **100%**, `overchargeMax`
  (0.1) = quanto oltre, allo stesso ritmo. Quattro zone, `PowerZone { Normal, Weak,
  Perfect, Overcharge }`, `ZoneOf(carica)`:
  debole fino a `weakTolerance` (potenza fissa `weakPower`), normale proporzionale,
  **perfetta** a 100% ± `perfectTolerance` (`perfectMultiplier`), **oltre**
  (`overchargeMultiplier`, la palla piu' veloce) = **palla a ricerca** se c'e' un nemico
  nel cono (`SkullHoming`), altrimenti tiro dritto a quella potenza. E' una prova di
  tempismo: tenendo premuto si supera il perfetto. Rinominati con `FormerlySerializedAs`
  (i valori in scena restano): `zoneWidth`→`weakTolerance`, `bottomZonePower`→`weakPower`,
  `bottomZoneMultiplier`→`weakMultiplier`, `topZoneMultiplier`→`perfectMultiplier`.
  `PowerRatio` e' diventato `ChargeRatio` (1 = 100%), con `MaxChargeRatio` e
  `CurrentZone`; `LastShotHoming` dice se l'ultimo tiro e' partito a ricerca.
  **Con angolo > 0 il teschio vola**. L'angolo e' solo "quanto in alto voglio tirare":
  **non e' un compromesso da bilanciare con un costo**, vedi lo Storico.
  **Il verso delle dipendenze e' uno solo: mouse → camera → tiro.** La camera non deve mai
  dipendere da `ShotDirection`. `ShotDirection` e' **orizzontale**; la direzione vera del
  colpo e' `LaunchDirection`.
  Lo `Shoot()` **azzera la velocita' della palla** prima dell'impulso.
  La palla parte solo se tocca la sfera di colpo (`BallInReach`, chiesto a
  `PlayerSwing`: un numero solo, non due che divergono). I nemici nella sfera li colpisce
  `PlayerSwing.OnShotReleased`.
  **Traiettoria a quadratini** (creati da codice, layer Ignore Raycast, niente collider),
  sempre visibile in mira: una per la palla e una per ogni nemico nella sfera (con la sua
  spinta, `PushImpulse`), oppure una dalla sfera se e' vuota. I quadratini (`maxDots`) si
  dividono fra le traiettorie. La freccia di mira non c'e' piu'. In carica oltre il 100%
  con un nemico agganciato la traiettoria della palla e' la curva della ricerca.
  **Non muove la mazza**: quella e' di `PlayerSwing`.
- `Player/SkullHoming.cs` — **la palla a ricerca**, il tiro oltre il 100%. Sulla testa
  (se manca lo aggiunge `SkullAimController.Start`). Bersaglio: il nemico vivo piu' vicino
  nel cono (`range`, `coneAngle`); quasi alla pari (`tieDistance`) vince il piu' allineato
  al tiro. Mira a `aimHeight` dell'altezza del nemico (verso la testa). La curva parte
  `arcAngle` gradi sopra la linea dritta e la **velocita' si adatta** per chiuderla sul
  nemico: a velocita' fissa l'arco non si sceglie (ci sono solo due parabole, una quasi
  dritta e una altissima). `arcAngle` 0 = velocita' del colpo e parabola bassa.
  **In volo la palla e' guidata, non libera**: a ogni passo le si da' la velocita' per
  arrivare al punto della curva, con un'accelerazione `easeIn` (parte lenta, arriva
  velocissima: chiesto dall'utente). Toglie la gravita' che il motore aggiungera' nel
  passo. La fine della curva viene portata sul nemico di adesso (`steering`): segue chi
  cammina e copre il fuori portata (45 gradi). Si spegne al primo urto (dopo
  `ignoreHitsFor`), se il bersaglio muore, o a fine curva. Gizmo di portata e cono.
  Storia delle tre versioni nello Storico.
- `Camera/CameraController.cs` — terza persona a un **offset fisso** (`offset`, scelto
  dall'utente), nessuno stato. Il mouse gira la camera, non il personaggio: lo yaw gira
  l'offset attorno a lui, il pitch gira **solo lo sguardo** e non sposta la camera. Quando
  il personaggio si gira per camminare la camera non lo segue. **In mira** scivola su
  `aimOffset`, piu' vicina, in `aimBlendTime`: glielo dice `PlayerController` con
  `Aiming`. Si piazza in `LateUpdate`. Puntatore catturato, e si avvicina se fra lei e il
  personaggio c'e' un muro. **Non sa niente del gioco**: non legge il tiro ne' lo stato
  del livello. Senza `player` collegato non si muove.
- `Camera/ShotCamera.cs` — la telecronaca del tiro, soluzione dell'utente al problema dei
  **due pezzi del giocatore** che si allontanano. Una seconda camera segue la testa in volo
  e il suo quadro viene disegnato in una finestrella dell'HUD, come le camere che seguono
  la pallina in televisione. Si apre quando un colpo prende davvero la testa
  (sale `aim.BallHits`: a vuoto non c'e' niente da raccontare). Si chiude quando la testa
  e' andata oltre `hideDistance` e poi il corpo l'ha raggiunta — quindi anche dopo che la
  palla si e' fermata, mentre ci cammini verso: serve anche a ritrovarla. Se la testa non
  si allontana mai (colpo debole), si chiude quando si ferma. **Non chiudere per sola
  vicinanza**: al colpo il corpo e' sempre vicino, e la finestra si chiudeva nello stesso
  frame in cui si apriva.
  **E' una `RawImage` nel Canvas, non uno SpriteRenderer**: e' la RawImage che sa disegnare
  una `RenderTexture` in uno spazio di schermo. La `RenderTexture` e' creata a runtime
  (nessun asset) e liberata in `OnDestroy()`, perche' una creata col `new` non la raccoglie
  il GC. La seconda camera resta **spenta** a finestra chiusa.
  Il generatore dell'HUD crea anche `Shot_Camera` e `Shot_Camera_Rig`.
- `Player/SkullExplosion.cs` — esplode su collisione con tag `enemy`, danno **ad area**
  via `Physics.OverlapSphere`. Danneggia solo gli `EnemyHealth`: il teschio non si
  fa male da solo, per costruzione. `ResetExplosion()` viene chiamata da `Shoot()`,
  altrimenti esploderebbe una volta sola per partita.
- `Player/SkullRolling.cs` — frenata scriptata. **PhysX non modella l'attrito di
  rotolamento**, quindi una sfera scivola all'infinito a prescindere dal physic
  material: serve per forza codice. Damping solo orizzontale + `stopThreshold` che
  azzera del tutto. `IsStopped` e' la fonte unica della soglia, letta anche da
  `ShotCamera`. Da quando il teschio vola **tutto qui dentro vuole sapere se
  tocca terra**: `IsGrounded` arriva dalla normale dei contatti (un muro ha normale
  orizzontale e non conta, altrimenti strisciarci rimetterebbe in funzione l'attrito di
  rotolamento) oppure dal Rigidbody addormentato, che non riceve piu' `OnCollisionStay`.
  L'azzeramento sotto soglia vale **solo a terra**: in volo la velocita' passa per lo
  zero in cima alla parabola e il teschio si congelava a mezz'aria.
  **Mentre `SkullHoming.IsHoming` non fa niente**: la partenza lenta della ricerca veniva
  presa per una palla che si ferma e azzerata.
- `Player/SkullIntegrity.cs` — HP del teschio, si consumano **per metro percorso**,
  con moltiplicatore crescente man mano che si rovina. In volo il moto orizzontale
  continua, quindi il costo passa per `airWearMultiplier` (0 = volare e' gratis):
  altrimenti un metro in aria costerebbe come un metro strisciato, che non e'
  rotolamento. **E' la leva che decide se l'angolo e' una scelta o un gesto obbligato.**
- `Enemy/EnemyHealth.cs` — HP impostabili, registro statico `All` / `AliveCount`.
  `deathBehaviour`: `Destroy` / `Deactivate` / **`Wreck`** (il corpo resta in scena).
  Ottimizzato per le folle: `AliveCount` e' un contatore mantenuto, non una
  scansione (lo leggono ogni frame GameManager e HUD); il registro si toglie in
  tempo costante scambiando l'ultimo elemento; il flash rosso usa un
  `MaterialPropertyBlock` (niente materiale istanziato per nemico) dentro una
  coroutine, quindi **non c'e' nessun `Update` sui nemici**. In `Awake()` aggiunge
  `EnemyStun` se manca (e c'e' un Rigidbody).
- `Enemy/EnemyStun.cs` — **lo stordimento della mazza**: niente danno. `Stun(impulso,
  moltiplicatore)` rende il corpo dinamico, lo spinge e lo ribalta (`tumbleSpin`): e' il
  "ragdoll" del cubo, il punto dove accendere quello vero quando ci saranno modelli con le
  ossa. Coroutine (niente `Update`): a terra `stunDuration` x moltiplicatore, poi aspetta
  di andare sotto `settleSpeed` (al massimo `maxExtraWait` in piu'), si raddrizza in
  `getUpTime` da kinematic e torna come era (`wasKinematic`). `IsDown` lo leggono
  `EnemyPatrol` e `EnemyLaneSpawner`. Se muore o diventa rottame mentre e' giu' si
  interrompe e resta alla fisica (`CanStun`).
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
  alla morte, cosi' l'esplosione lo spedisce via. Stordito (`EnemyStun.IsDown`, letto in
  `Start` e non in `Awake`) si ferma; rialzato torna sul percorso camminando.
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
  `crashRecovery`. Un nemico stordito dalla mazza (`Runner.stun.IsDown`) tiene il suo
  posto in fila e chi sta dietro frena; rialzato, `Rejoin` lo riporta in corsia
  camminando prima di riprendere la marcia (senza, `Move` lo teletrasportava).
- `Level/GameManager.cs` — singleton. Stato `Playing/Won/Lost`, `Power`, `Damage`,
  `Kills`, `EnemiesAtStart`, `maxWrecks`, `fallY` come rete di sicurezza.
  **Due vie di vittoria indipendenti** (OR): gli obiettivi in
  `AreLevelObjectivesMet()` (nemici tutti e/o N + puzzle + soglia danni, in AND)
  oppure la buca. `LastWinReason` dice quale, servira' al punteggio.
  `hasObjectives`, calcolato in `Start()`, evita che un livello senza obiettivi
  configurati si vinca da solo al primo frame.
- `UI/HUDController.cs` — HUD uGUI: barre di integrita' e potenza, contatori
  Danni/Power/uccisioni, pannelli di fine livello. Solo visualizzazione: legge
  `GameManager.State` ogni frame invece di agganciarsi a `onWin`/`onLose`, cosi'
  non c'e' niente da ricollegare e gli `UnityEvent` restano liberi. Tutti i
  riferimenti sono opzionali. La barra di potenza piena vale `MaxChargeRatio` (110%), il
  numero mostra la carica vera, il colore cambia in zona perfetta e oltre, tre strisce
  (`weakBand`/`perfectBand`/`overchargeBand`, create da `GOOMD > Rifai barra potenza`)
  segnano le zone. L'indicatore dell'angolo si vede **sempre**.
- Editor (`*/Editor/`, fuori dalla build): `UI/Editor/HUDBuilder.cs` genera la
  gerarchia dell'HUD gia' cablata, `Enemy/Editor/ExplosionVFXBuilder.cs` genera un
  prefab di esplosione a particelle **mesh** (niente texture ne' trasparenze: in
  URP sono la via piu' breve a un effetto invisibile o rosa). Entrambi dal menu
  `GOOMD`.
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

Seconda trappola: **un collider su un figlio del personaggio fa parte del suo corpo
fisico.** La mazza aveva un CapsuleCollider (e `club_head` una SphereCollider): in posa di
mira la punta spingeva via la palla prima del colpo. `PlayerSwing` li spegne all'avvio.
Qualunque cosa si metta in mano al personaggio (anche gli oggetti presi, quando ci
saranno) va trattata allo stesso modo.

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

## Il personaggio e i suoi due pezzi

Deciso dall'utente il 2026-10-04. Il personaggio e' uno **scheletro in due pezzi**: il
**corpo**, che cammina e tiene la mazza, e la **testa**, che fa da pallina.

I comandi completi:

| Tasto | Cosa fa |
|---|---|
| WASD | si muove, relativo alla camera. In modalita' tiro: piccoli passi |
| Shift | corre |
| Alt | si abbassa e rallenta |
| Spazio | salta |
| Ctrl | tuffo, a terra o in aria (la schivata) |
| mouse | gira la camera, e la camera da' la direzione del tiro (puntatore catturato) |
| tasto destro | **tenuto**: modalita' tiro. Lasciato: si esce, anche in carica |
| tasto sinistro | colpo di mazza a mano (stordisce, niente danno). In modalita' tiro: tieni premuto per caricare (fino al 110%), rilascia per tirare |
| rotella | angolo del tiro, sempre tranne in carica. Un giro = 25% del ventaglio |
| 1-9 (riga in alto e tastierino) | angoli fissi del tiro (`loftPresets`) |
| E | **da fare**: prende la testa vicina / la posa sul punto di colpo |

### La prossima funzionalita': prendere gli oggetti

Annunciata dall'utente il 2026-10-12 come **meccanica centrale**. Il personaggio puo'
**prendere degli oggetti**. Esempi fatti dall'utente:

- prendere la **testa** (la palla) e **rimetterla al suo posto**, sulle spalle;
- se ce l'ha in testa, **farla cadere** per poterla colpire;
- mettere **altre cose al posto della testa**, da valutare.

E' anche la risposta al vecchio punto aperto "il gioco e' recuperare la testa".

**Detto dall'utente il 2026-10-13:**

- il tasto e' **E**;
- se la palla e' **vicina** (anche se rotola verso di lui), E la **raccoglie**: va "in
  testa", sulle spalle, e lo segue;
- E di nuovo la **posa a terra gia' sul punto di colpo** che avrebbe entrando in mira in
  quel momento. **Non semplicemente davanti**: deve essere nella posizione che avrebbe se
  entrasse in mira subito (cioe' l'ancoraggio di `PlayerSwing.BeginStance`);
- fuori da questi casi la palla gli **passa attraverso** (`ignoreBall`, gia' fatto):
  altrimenti la si sposta senza volerlo.

**Domande fatte il 2026-10-13, in attesa di risposta.** Accanto a ognuna la proposta;
l'utente deve confermare o correggere prima di scrivere il codice:

1. Testa in spalla + tasto destro (mira): la posa da sola sul punto di colpo ed entra in
   mira (proposta), oppure la mira resta bloccata finche' non la posa con E?
2. Con la testa in spalla: cammina, corre, salta, colpo a mano normali; il **tuffo la fa
   cadere** dove si trova (proposta). O il tuffo la tiene?
3. La testa in spalla tocca un nemico: **non esplode e non si consuma** (proposta).
4. Lava o caduta fuori mappa con la testa in spalla: **si perde** (proposta).
5. Raggio di raccolta: un raggio attorno al corpo, tarabile (proposta: 1,5 m).
6. Una palla in volo (tiro appena partito, palla a ricerca) si prende al volo con E se
   passa nel raggio? (proposta: si').
7. Dove sta la testa: campo `headSocket`, un oggetto vuoto sopra le spalle di
   `Player_model` messo dall'utente; vuoto = in cima alla capsula (proposta).
8. Il punto di colpo finisce dentro un muro: la posa davanti ai piedi (proposta).

Ancora da chiedere in seguito: cosa altro si puo' prendere, cosa cambia con un altro
oggetto al posto della testa.

L'idea della **camera legata ai due pezzi** e' stata spiegata e fatta: e' la finestra
della telecronaca (`ShotCamera`). La camera principale resta sempre sul corpo.

## Il modello di riferimento: Burnout 3, modalita' Crash

Deciso dall'utente, vale per **tutto** il design delle collisioni: lo schianto e'
l'obiettivo, non un fallimento. Ogni cosa contro cui il teschio si schianta deve
reagire in modo suo — fare punti danno, respingerlo, rallentarlo, esplodere,
innescare qualcos'altro — e la varieta' di quelle reazioni e' il gioco.

Conseguenze pratiche:

- Il punteggio **Damage** e' il punteggio principale della modalita'. Per ora lo
  alimentano solo i nemici (`EnemyHealth`, `EnemyExplosive`, `EnemyWreck`): quando
  ci saranno oggetti di scena distruttibili dovranno chiamare
  `GameManager.AddDamage()`.
- **Il corpo colpito resta in gioco** (`EnemyWreck`): rotola, ingombra e danneggia
  chi urta. E' il principio delle auto distrutte di Burnout, che restano in strada
  a innescare la carambola. Un nemico che sparisce e basta e' un'occasione persa.
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

### Stati / potenziamenti del teschio

Deciso dall'utente. Il teschio deve poter assumere degli stati che ne cambiano le
proprieta' e il comportamento all'impatto. Esempi fatti: **ghiaccio**, **fuoco**,
**rimbalzo** molto elevato, **acido**, **crea tornado** dove impatta.

Due vie di attivazione, entrambe previste:

1. raccogliendo oggetti nel livello (le torce di cui sopra)
2. acquistandoli con **Power** da un menu dei potenziamenti, ancora da progettare

L'utente sta ancora decidendo i dettagli. Vincolo emerso progettandolo: gli
esempi coprono **quattro aree diverse** — fisica (rimbalzo, ghiaccio), integrita',
danno (acido) e reazione all'impatto (tornado) — quindi la struttura va fatta in
modo che uno stato possa intervenire su tutte e quattro, non su un parametro solo.

Due contatori distinti, da non confondere:

- **Power** — valuta, dai nemici uccisi (`EnemyHealth.powerReward`), da spendere
  in potenziamenti. Non e' mai un obiettivo. Sistema di acquisto non progettato.
- **Damage** — punteggio distruzione stile modalita' Crash di Burnout
  (`damageScorePerHP` sugli HP tolti davvero + `damageScoreOnKill`). Puo' essere
  un obiettivo tramite `requiredDamage`. Per ora lo alimentano solo i nemici:
  quando ci saranno oggetti distruttibili, chiamano `GameManager.AddDamage()`.

## Come si lavora qui

**Non c'e' nessuna CLI Unity disponibile: non posso compilare, ne' far girare il
gioco.** Non dare mai per buono il codice nuovo: scrivilo, dillo esplicitamente e
chiedi conferma all'utente, che compila e prova in editor. Finora ha sempre
riportato l'esito, spesso segnalando comportamenti sbagliati piu' che errori.

**La taratura la fa l'utente.** I valori nel C# sono solo i default di creazione:
comandano quelli serializzati in scena e nei prefab. Quando un comportamento
sbagliato dipende da un numero, indica il campo e lascia fare a lui.

Aggiorna **`Docs/GetOutOfMyDungeon/Taratura.md`** quando aggiungi o rinomini un campo
pubblico: e' la wiki che l'utente usa per tarare. **Non a ogni passo**: durante un giro di
modifiche tieni la lista di cosa trascrivere e aggiorna Taratura, Storico, `GOOMD.md` e
questo file tutti insieme quando l'utente chiude il giro.

## Dove siamo rimasti — 2026-10-13

Il 2026-10-12 si e' rifinito il controller (camera, mira, postura, swing, traiettoria,
salto, tuffo). Il 2026-10-13, su richiesta e su prova dell'utente:

- **angolo sempre selezionabile**, con angoli fissi sui tasti 1-9 (riga in alto e
  tastierino); bloccato solo in carica;
- **la mazza stordisce invece di fare danno** (`EnemyStun`), e non provoca l'incidente;
- **carica fino al 110%**: colpo perfetto al 100%, oltre = **palla a ricerca**
  (`SkullHoming`), rifatta tre volte sulle prove dell'utente fino all'arco scelto e
  all'accelerazione;
- **il corpo passa attraverso la palla**;
- barra della potenza con zone e colori; documenti aggiornati, con l'elenco rapido dei
  parametri (Taratura cap. 1-bis) chiesto dall'utente.

Il dettaglio di ogni scelta scartata e' nello Storico.

**Lo stato.** Compila, confermato dall'utente fino alla palla a ricerca con l'arco
(`arcAngle`), provata e approvata: "l'arco va benissimo". **L'ultima modifica non e'
ancora stata compilata ne' provata**: la palla a ricerca guidata lungo la curva con
`easeIn` (parte lenta, arriva veloce) e `SkullRolling` che non interviene mentre la palla
cerca. Va confermata alla ripresa.

**I campi nuovi non sono tarati**: l'utente fara' una sessione di taratura dedicata a
tutti i parametri (compresa `Bounciness` di `Base_ground`). Lui tara anche
`perfectTolerance`, `stanceMoveSpeed`, `stanceSnap`.

**Si lavora cosi'**: l'utente manda screen in `Reference/` e descrive cosa non va. Guarda
gli screen prima di rispondere. Quando una descrizione e' ambigua, chiedi: due volte una
lettura sbagliata dell'immagine ha fatto rifare il lavoro (la postura, lo swing).

**I documenti si aggiornano in un colpo solo a fine giro**, non a ogni passo: chiesto
dall'utente.

### Alla ripresa

1. Chiedi l'esito della palla a ricerca con `easeIn` (compila? parte lenta e arriva
   veloce? colpisce ancora i nemici in movimento?).
2. Chiedi le **risposte alle 8 domande su "prendere gli oggetti"** (sezione "La prossima
   funzionalita'"), poi progetta e scrivi.

### In editor (lo fa l'utente, quando vuole)

- `Bounciness` di `Base_ground` e' 0: con i tiri alti la palla atterra e si pianta. Rinviato
  dall'utente alla sessione di taratura. Vedi Taratura cap. 13.

Decisi e chiusi, da non riaprire: la direzione del tiro **resta viva durante la carica**;
**l'angolo invece e' bloccato in carica**; `swingPivot` sta **dietro il collo**;
`stanceAngle` resta **90**; la mazza **non fa danno**; l'incidente lo provoca **solo la
testa**; il vecchio `zoneWidth` ora e' `weakTolerance` e lo tara l'utente.

## Prossimi passi (in ordine)

1. **Prendere gli oggetti** — la nuova meccanica centrale, vedi sopra "La prossima
   funzionalita'". Tasto e comportamento base gia' detti; mancano le risposte alle 8
   domande.
2. **Rilavorare la testa, i suoi stati e gli effetti** — annunciato dall'utente come uno
   dei prossimi passi. Il progetto degli stati e i suoi vincoli sono sopra ("Stati /
   potenziamenti del teschio"). E' il presupposto delle torce **e** del negozio.
3. **`Destructible`, il componente base dei props** — si rompe sopra una velocita'
   minima e chiama `GameManager.AddDamage()`. Oggi il punteggio Danni lo alimentano
   **solo i nemici**, e in una modalita' Crash e' meta' del punteggio che manca.
   Casse e forzieri sono la sua versione piu' semplice, il boulder una variante che
   infligge invece di subire. Lo swing li dovra' colpire come i nemici (sfera di colpo).
4. **Negozio dei potenziamenti** — il Power si accumula ma non si spende da
   nessuna parte. Serve dopo gli stati, perche' e' quello che vende.
5. **Puzzle veri** — `PuzzleTrigger` e' solo l'aggancio: nessun meccanismo lo chiama.
6. **Restart di livello** — gli script hanno gia' i metodi (`ResetIntegrity`,
   `ResetHealth`, `ResetExplosion`, `ResetHole`, `ResetOpener`, `ResetPuzzle`,
   `ResetPatrol`, `ResetWreck`) ma **nessuno li chiama**: l'unico restart e'
   `HUDController.RestartLevel()`, che ricarica la scena.
7. **Stile dell'HUD** — funziona ma sono rettangoli pieni e font di default.
8. **Pooling dei nemici nello spawner** — solo quando la cadenza salira' molto.
   Vedi `Docs/GetOutOfMyDungeon/Taratura.md` cap. 13-bis, con la trappola degli ascoltatori a runtime
   da rimuovere a ogni riuso.

Aperti minori:

- I nemici non reagiscono alla posizione del teschio: pattugliano e basta.
- Nessun suono, da nessuna parte.
- `Collision Detection` e' Discrete su tutti i Rigidbody: col teschio a 20 m/s
  sono 40 cm per passo di fisica, quindi puo' attraversare un bersaglio stretto. Col volo
  peggiora, perche' ora c'e' anche un pavimento da attraversare cadendo dall'alto. Con la
  palla a ricerca peggiora ancora: oltre il 100% la palla va a 32 m/s, e con `easeIn` 2
  arriva sul nemico al doppio della velocita' media. Se la palla attraversa i nemici
  invece di esplodere, il primo sospetto e' questo (`Continuous Dynamic` sulla palla).
- La telecronaca non segue le parabole: quota fissa sopra la testa, su un tiro alto
  inquadra il cielo.
- Nessuna animazione sul personaggio: `PlayerController.IsRunning` / `IsCrouching` /
  `IsGrounded` / `IsDiving` / `IsGettingUp` e `PlayerSwing.IsSwinging` sono pubblici
  proprio per attaccarci un Animator. Lo swing e' un arco calcolato, da sostituire con
  un'animazione.
