---
aliases:
  - TUNING
  - Wiki di taratura
tags:
  - goomd
  - taratura
---

# GOOMD — Wiki di taratura

Riferimento di tutti i parametri esposti negli script di *Get Out Of My Dungeon*:
dove stanno, cosa fanno, cosa succede se li muovi.

> Per **perché** una cosa è fatta così e non in un altro modo, vedi
> **[[Storico]]**: le scelte provate e scartate, con il motivo.

> **Regola d'oro**: i numeri scritti nel codice C# sono solo i *default di creazione*.
> Quando aggiungi un componente a un GameObject, Unity copia quei valori nella scena
> e da lì in poi **comanda la scena**, non il codice. Cambiare il default in un `.cs`
> **non** cambia gli oggetti già in scena.
> Le colonne "in scena" qui sotto riportano i valori attualmente salvati in
> `Assets/Scenes/SampleScene.unity` alla data di stesura (2026-09-08). I capitoli 2, 2-bis
> e 2-ter sono stati riletti sulla scena salvata il **2026-10-12**. I campi nuovi del
> **2026-10-13** (carica al 110%, palla a ricerca, stordimento) hanno solo il default:
> l'utente li tarerà in una sessione dedicata.

Legenda delle colonne:

- **Default** — valore nel sorgente C#, quello che prende un oggetto nuovo
- **In scena** — valore attuale in `SampleScene`, se diverso dal default è in **grassetto**

---

## 1. Mappa rapida: chi porta cosa

| GameObject in scena | Componenti da tarare |
|---|---|
| `Game_Controller` | `SkullAimController` — mira, **angolo**, potenza, traiettoria a quadratini |
| `Player` | `PlayerController` — movimento, corsa, salto, tuffo, postura in mira; `PlayerSwing` — swing da golf, punto di colpo, colpo a mano |
| `Player_model` (figlio di `Player`) | la parte visibile: si inclina nel tuffo (`PlayerController.visual`) |
| `Skeletal_arm` (sotto `Player_model`) | la mazza: nessuno script proprio, la muove `PlayerSwing`. Il suo collider viene **spento** all'avvio |
| `club_head` (figlio di `Skeletal_arm`) | la testa della mazza: da qui parte il punto di colpo |
| `Mazza a mira` (figlio di `Player`) | la posa di mira della mazza (`PlayerSwing.addressPose`). Renderer spento, nessun collider |
| `Main Camera` | `CameraController` — terza persona a offset fisso, si avvicina in mira |
| `Shot_Camera_Rig` (**generato**) | `ShotCamera` — la finestra con la telecronaca del tiro |
| `Skull_player` | `SkullExplosion`, `SkullRolling`, `SkullIntegrity`, `SkullHoming` (la palla a ricerca), Rigidbody, SphereCollider (+ physic material) |
| `Enemy`, `Enemy (1)`, `Enemy (2)` | `EnemyHealth` — HP, ricompense, punteggio danni; `EnemyStun` — lo stordimento della mazza (se manca lo aggiunge `EnemyHealth` all'avvio) |
| `Win_hole` | `WinHole` — buca di vittoria |
| `Hole_opener`, `Hole_opener (1)` | `HoleOpener` — bersagli che aprono la buca |
| `Game_Manager` | `GameManager` — obiettivi del livello, sconfitta, punteggi |
| `Lava` | `KillZone` — morte istantanea |
| `HUD` (Canvas) | `HUDController` — barre, contatori, pannelli di fine livello |

> La freccia di mira (`Arrow`) **non c'è più**: cancellata dalla scena il 2026-10-13.
> La direzione si legge dai quadratini della traiettoria.

---

## 1-bis. Elenco rapido: ogni parametro in poche parole

Una riga per campo, per script. Serve per tarare senza leggere tutto. Il dettaglio, i
legami fra i valori e i conti stanno nei capitoli dopo. I campi di riferimento (gli
oggetti da collegare) e gli eventi non ci sono: non si tarano.

### `SkullAimController` (su `Game_Controller`) — il tiro

| Campo | Cosa tara |
|---|---|
| `aimAlignSpeed` | quanto in fretta la mira passa da dove guarda il corpo a dove guarda la camera |
| `minLoftAngle` | l'angolo più basso, in gradi |
| `maxLoftAngle` | l'angolo più alto, in gradi |
| `startLoftRatio` | l'angolo a inizio livello |
| `loftPresets` | gli angoli dei tasti numerici 1-9, in frazione del ventaglio |
| `loftPerRevolution` | quanto cambia l'angolo in un giro di rotella |
| `notchesPerRevolution` | gli scatti della tua rotella in un giro |
| `loftSmooth` | quanto morbido si muove l'angolo |
| `showAimGizmo` | il gizmo del ventaglio |
| `maxPower` | la velocità della palla al 100% |
| `chargeTime` | i secondi per caricare al 100% |
| `overchargeMax` | quanto si può caricare oltre il 100% |
| `forceMultiplier` | moltiplica tutti i tiri |
| `weakTolerance` | fin dove la carica è un colpo debole |
| `weakPower` | la potenza del colpo debole |
| `weakMultiplier` | moltiplica il colpo debole |
| `perfectTolerance` | quanto è larga la zona del colpo perfetto attorno al 100% |
| `perfectMultiplier` | il boost di potenza del colpo perfetto |
| `overchargeMultiplier` | la potenza oltre il 100%: la palla più veloce |
| `showTrajectory` | i quadratini della traiettoria |
| `dotPrefab` / `dotMaterial` | l'aspetto dei quadratini |
| `dotSpacing` | la distanza fra i quadratini |
| `dotSize` | la grandezza dei quadratini |
| `maxDots` | quanti quadratini in tutto |
| `trajectorySteps` / `trajectoryStep` | quanto lunga è la traiettoria disegnata |
| `trajectoryMask` | cosa ferma la traiettoria disegnata |

### `SkullHoming` (su `Skull_player`) — la palla a ricerca

| Campo | Cosa tara |
|---|---|
| `range` | fin dove cerca un nemico |
| `coneAngle` | quanto è largo il cono di ricerca |
| `tieDistance` | sotto questa differenza di distanza vince il nemico più allineato |
| `aimHeight` | dove mira sul nemico: piedi o testa |
| `arcAngle` | quanto arco fa la palla |
| `easeIn` | quanto accelera: partenza lenta, arrivo veloce |
| `steering` | quanto segue il nemico che si sposta |
| `ignoreHitsFor` | per quanto, alla partenza, un urto non ferma la ricerca |
| `showGizmo` | il gizmo del cono |

### `PlayerController` (su `Player`) — il corpo

| Campo | Cosa tara |
|---|---|
| `ignoreBall` | il corpo passa attraverso la palla |
| `walkSpeed` | la velocità camminando |
| `runSpeed` | la velocità correndo |
| `crouchSpeed` | la velocità abbassato |
| `acceleration` | quanto in fretta parte e si ferma |
| `turnSpeed` | quanto in fretta si gira |
| `airControl` | quanto si sterza in aria |
| `crouchHeightScale` | quanto si abbassa |
| `crouchChangeSpeed` | quanto in fretta si abbassa |
| `stanceAngle` | di quanto il corpo è girato rispetto al tiro, in mira |
| `stanceMoveSpeed` | la velocità del passo di lato entrando in mira |
| `stanceSnap` | quanto è secco il passo di lato |
| `aimStepSpeed` | la velocità dei piccoli passi in mira |
| `jumpHeight` | l'altezza del salto |
| `groundMask` | cosa conta come terreno |
| `groundCheckDistance` | quanto sotto i piedi cerca il terreno |
| `maxGroundAngle` | oltre questa pendenza è un muro |
| `diveSpeed` | la velocità del tuffo |
| `diveUpSpeed` | quanto sale il tuffo |
| `diveTilt` | quanto si inclina nel tuffo |
| `tiltSpeed` | quanto in fretta si inclina |
| `getUpTime` | quanto dura il rialzarsi dopo il tuffo |
| `getUpSpeed` | la velocità mentre si rialza |

### `PlayerSwing` (su `Player`) — la mazza

| Campo | Cosa tara |
|---|---|
| `restPose` / `addressPose` | le pose della mazza: a riposo e in mira |
| `addressBlendSpeed` | quanto in fretta la mazza va in posa di mira |
| `swingPivot` | il punto attorno a cui gira lo swing |
| `backswingAngle` | quanto sale la mazza caricando |
| `followThroughAngle` | quanto sale la mazza dopo il colpo |
| `invertSwingSide` | carica a sinistra invece che a destra |
| `topRadius` | quanto si stringe l'arco salendo |
| `swingDuration` | quanto dura la frustata |
| `impactAt` | in che punto della frustata avviene il colpo |
| `recoverTime` | quanto dura il rientro della mazza |
| `returnSpeed` | quanto in fretta torna a riposo |
| `strikeReach` | quanto davanti al corpo sta il punto di colpo |
| `stanceDistance` | il punto di colpo se manca `clubHead` |
| `strikeRadius` | quanto è grande la sfera di colpo |
| `groundMask` | dove si appoggia il punto di colpo |
| `meleeAxis` | l'asse del colpo a mano |
| `range` / `arc` | la portata e la larghezza del colpo a mano |
| `cooldown` | la pausa fra due colpi |
| `meleeDuration` | quanto dura il colpo a mano |
| `hitAt` | in che punto del colpo a mano si prende il bersaglio |
| `meleeFrom` / `meleeTo` | da dove a dove gira la mazza nel colpo a mano |
| `knockForce` | quanto lontano vola il nemico colpito |
| `knockLift` | quanto in alto vola il nemico colpito |
| `perfectStunMultiplier` | lo stordimento in più con lo swing al 100% |
| `overchargeStunMultiplier` | lo stordimento in più con lo swing oltre il 100% |
| `hitMask` | cosa può colpire la mazza |
| `maxTargetsPerSwing` | quanti oggetti al massimo in un colpo |

### `CameraController` (su `Main Camera`)

| Campo | Cosa tara |
|---|---|
| `offset` | dove sta la camera rispetto al personaggio |
| `positionSmooth` | quanto la camera insegue in ritardo |
| `aimOffset` | dove sta la camera in mira |
| `aimBlendTime` | quanto dura il passaggio alla camera di mira |
| `lookSensitivity` | la sensibilità del mouse |
| `lookMinPitch` / `lookMaxPitch` | quanto si guarda in alto e in basso |
| `lockCursor` | cattura il puntatore |
| `collisionMask` | cosa avvicina la camera quando è in mezzo |
| `collisionPadding` | quanto la camera resta staccata dal muro |
| `minDistance` | quanto può avvicinarsi al personaggio |

### `ShotCamera` (su `Shot_Camera_Rig`) — la telecronaca

| Campo | Cosa tara |
|---|---|
| `followDistance` | quanto la camera sta dietro la testa |
| `followHeight` | quanto la camera sta in alto |
| `followLookHeight` | quanto sopra la testa guarda |
| `followSmooth` | quanto morbido insegue |
| `hideDistance` | da che distanza la finestra si può chiudere |
| `textureWidth` / `textureHeight` | la risoluzione della finestra |

### `SkullRolling` (su `Skull_player`) — il rotolamento

| Campo | Cosa tara |
|---|---|
| `rollingDamping` | quanto frena a terra |
| `stopThreshold` | sotto questa velocità si ferma |
| `airDamping` | quanto frena in aria |
| `groundNormalMin` | quanto in piano deve essere una superficie per contare come terreno |
| `groundGrace` | per quanto resta "a terra" dopo l'ultimo contatto |
| `collisionSpeedLoss` | quanta velocità perde in un urto |
| `minImpactSpeed` | sotto questa velocità l'urto non conta |

### `SkullIntegrity` (su `Skull_player`) — la vita della testa

| Campo | Cosa tara |
|---|---|
| `maxIntegrity` | la vita della testa |
| `wearPerMeter` | quanta vita costa un metro rotolato |
| `fragility` | quanto consuma di più a fine vita |
| `airWearMultiplier` | quanto costa un metro in volo |
| `wearPerImpact` | quanta vita costa un urto |
| `minImpactSpeed` | sotto questa velocità l'urto non costa |

### `SkullExplosion` (su `Skull_player`) — l'esplosione

| Campo | Cosa tara |
|---|---|
| `explosionVFX` / `destroyDelay` | l'effetto e quanto resta |
| `explosionForce` | quanto forte spinge |
| `explosionRadius` | quanto è grande |
| `upwardsModifier` | quanto spinge in alto |
| `affectedLayers` | cosa viene investito |
| `explosionDamage` | il danno ai nemici |
| `damageFalloff` | il danno cala con la distanza |
| `freezeSkullOnExplode` | la testa si ferma quando esplode |
| `hideSkullOnExplode` | la testa sparisce quando esplode |

### `EnemyHealth` (sui nemici)

| Campo | Cosa tara |
|---|---|
| `maxHP` | la vita del nemico |
| `powerReward` | quanto Power dà da morto |
| `damageScorePerHP` | i punti Danni per ogni HP tolto |
| `damageScoreOnKill` | i punti Danni in più alla morte |
| `deathBehaviour` | cosa succede al corpo da morto |
| `deathVFX` / `deathDelay` | l'effetto di morte e quando sparisce |
| `flashOnHit` / `flashColor` / `flashDuration` | il lampo quando viene colpito |

### `EnemyStun` (sui nemici) — lo stordimento della mazza

| Campo | Cosa tara |
|---|---|
| `stunDuration` | quanto resta a terra |
| `settleSpeed` | si rialza solo sotto questa velocità |
| `maxExtraWait` | il tempo massimo in più ad aspettare che si fermi |
| `tumbleSpin` | quanto si ribalta quando cade |
| `getUpTime` | quanto dura il rialzarsi |

### `EnemyWreck` (sui nemici) — il corpo come birillo

| Campo | Cosa tara |
|---|---|
| `knockOnDamage` | esce dai binari al primo danno |
| `knockOnDeath` | esce dai binari da morto |
| `impactDamage` | il danno che fa urtando altri nemici |
| `minImpactSpeed` | sotto questa velocità l'urto non fa danno |
| `impactCooldown` | la pausa fra due danni da urto |
| `impactScore` | i punti Danni per ogni urto |
| `lifetime` | quanto resta in scena |

### `EnemyArmor` — corazzato

| Campo | Cosa tara |
|---|---|
| `directional` | la corazza ha un lato |
| `armorFacing` | dove guarda la corazza |
| `armorHalfAngle` | quanto è larga la corazza |
| `minBreakSpeed` | la velocità minima per passare |
| `bounceKeep` | quanta velocità tiene la testa che rimbalza |
| `minBounceSpeed` | sotto questa velocità non rilancia |
| `blocksSplash` | para anche le esplosioni |

### `EnemyExplosive` — esplosivo

| Campo | Cosa tara |
|---|---|
| `radius` | quanto è grande l'esplosione |
| `damage` | il danno ai vicini |
| `force` / `upwardsModifier` | quanto e quanto in alto spinge |
| `affectedLayers` | cosa viene investito |
| `damageScore` | i punti Danni per l'esplosione |
| `explosionVFX` / `destroyDelay` | l'effetto e quanto resta |

### `EnemyAbsorber` — gelatinoso

| Campo | Cosa tara |
|---|---|
| `speedKeep` | quanta velocità tiene la testa |
| `minImpactSpeed` | sotto questa velocità non assorbe |
| `absorbAngular` | frena anche la rotazione |

### `EnemyPatrol` — pattuglia

| Campo | Cosa tara |
|---|---|
| `waypoints` / `mode` | il percorso e come lo ripete |
| `snapToFirstWaypoint` | parte sul primo punto |
| `speed` | la velocità |
| `waitAtWaypoint` | la pausa a ogni punto |
| `arriveThreshold` | quanto vicino conta come arrivato |
| `faceMovement` / `turnSpeed` | si gira verso dove va, e quanto in fretta |
| `stunOnHit` | la pausa quando incassa un danno |

### `EnemyLaneSpawner` — la strada a corsie

| Campo | Cosa tara |
|---|---|
| `pointA` / `waypoints` / `pointB` | il percorso della strada |
| `smoothPath` / `samplesPerSegment` | quanto è curva e liscia |
| `laneSpacing` / `centerLanes` | la distanza fra le corsie |
| `enemyPool` | i tipi di nemico e il loro peso |
| `faceTravelDirection` | i nemici guardano dove vanno |
| `respawnDelay` | l'attesa dopo un'uccisione |
| `startPopulated` | la strada parte piena |
| `totalSpawnBudget` | quanti nemici in tutto |
| `stopLaneOnCrash` / `crashOnHit` | l'incidente quando la testa colpisce |
| `crashImpulse` / `crashRadius` / `crashUpwards` | quanto forte l'incidente spinge |
| `crashDamage` | il danno dell'incidente |
| `crashRecovery` | quanto resta ferma la corsia |
| Per corsia: `enemyCount` | quanti nemici insieme |
| Per corsia: `direction` | il verso di marcia |
| Per corsia: `spawnInterval` / `intervalVariation` | la cadenza e quanto è irregolare |
| Per corsia: `speed` / `speedVariation` | la velocità e quanto varia |
| Per corsia: `minGap` / `slowdownGap` | la distanza di sicurezza |
| Per corsia: `spawnBudget` | quanti nemici in tutto sulla corsia |
| Per corsia: `phase` | lo sfasamento rispetto alle altre |
| Per corsia: `poolOverride` | i tipi di nemico solo di questa corsia |

### `GameManager` (su `Game_Manager`) — il livello

| Campo | Cosa tara |
|---|---|
| `requireAllEnemiesDead` | per vincere vanno uccisi tutti |
| `requiredKills` | quante uccisioni servono |
| `requiredDamage` | quanti punti Danni servono |
| `requiredPuzzles` | quali puzzle servono |
| `countDamageAfterWin` | i Danni contano anche a livello finito |
| `fallY` | sotto questa quota si perde |
| `maxWrecks` | quanti rottami al massimo insieme |

### Livello: buca, apri-buca, lava, puzzle

| Script | Campo | Cosa tara |
|---|---|---|
| `WinHole` | `requiredOpeners` / `requireAllOpeners` | cosa apre la buca |
| `WinHole` | `snapToCenter` / `freezeSkull` / `hideSkull` | cosa fa la testa imbucata |
| `HoleOpener` | `onlySkull` | lo apre solo la testa |
| `HoleOpener` | `minImpactSpeed` | la velocità minima per aprirlo |
| `HoleOpener` | `triggeredVFX` / `disableOnTriggered` | cosa fa quando si apre |
| `KillZone` | `reason` | il testo della sconfitta |
| `PuzzleTrigger` | `puzzleName` / `powerReward` | il nome e la ricompensa |

### `HUDController` (su `HUD`)

| Campo | Cosa tara |
|---|---|
| `integrityGradient` / `integrityFormat` | il colore e il testo della barra della vita |
| `integritySmooth` | quanto morbida scende la barra della vita |
| `lowIntegrityThreshold` / `lowIntegrityPulseSpeed` | quando e quanto lampeggia la vita bassa |
| `powerGradient` | il colore della barra di potenza |
| `showWhileAiming` | la barra di potenza si vede già in mira |
| `powerFormat` / `powerLabelFollowsFill` | il testo della potenza e se sale con la barra |
| `perfectColor` / `overchargeColor` | il colore della barra al 100% e oltre |
| `loftFormat` | il testo dell'angolo |
| `damageFormat` / `damageRollSpeed` | il testo dei Danni e quanto in fretta sale |
| `powerCurrencyFormat` | il testo del Power |
| `enemiesFormat` | il testo dei nemici rimasti |
| `statsFormat` | il testo di fine livello |

---

## 2. Il tiro — `SkullAimController`

`Assets/Script/Player/SkullAimController.cs` — su `Game_Controller`.

Macchina a stati `Idle → Aiming → Powering → Idle`. Dopo il rilascio si torna subito
`Idle`: non si aspetta che la palla si fermi. Il personaggio resta fermo solo per la
frustata della mazza (`PlayerSwing.swingDuration`), poi cammina di nuovo.

`BallHits` conta i colpi che hanno preso la testa. Lo legge `ShotCamera` per sapere quando
aprire la telecronaca.

Sequenza di input:

1. **tieni premuto il tasto destro**: sei in modalità tiro. Lo gestisce `PlayerController`
   (cap. 2-bis). Si entra **sempre**, anche con la palla lontana o in movimento.
2. in `Aiming`: la direzione del tiro **parte da dove guarda il personaggio** e ruota verso
   dove guarda la camera, a `aimAlignSpeed` gradi al secondo. Il **mouse** gira la camera.
   **WASD** fa piccoli passi per allineare la mazza alla palla.
3. **premi il tasto sinistro**: l'angolo è confermato e la carica parte. Arriva al 100% in
   `chargeTime` secondi e continua fino al 110%. I piccoli passi funzionano anche qui.
   **L'angolo in carica è bloccato**: per cambiarlo si esce dalla mira.
4. **rilascia il sinistro** per tirare. Cosa succede dipende dalla zona in cui hai
   rilasciato (vedi "Zone della carica").
5. **lascia il destro** in qualsiasi momento, anche in carica, per annullare e tornare a
   camminare. Serve per i cambi improvvisi durante il gioco.

**L'angolo si sceglie sempre**, anche camminando: con la rotella o con i tasti numerici
(riga in alto o tastierino). Si arriva in mira con l'angolo già pronto.

Se dopo il tiro il destro è ancora premuto, finita la frustata si rientra in mira da soli.

> La direzione del tiro arriva dalla camera. Prima era il contrario: la camera si
> posizionava dietro la palla in base alla direzione di tiro. Ora il verso è uno solo —
> il mouse gira la camera, la camera dà la direzione — e questo ha eliminato l'anello
> che si auto-alimentava. Vedi [[Storico]]. La direzione **resta viva durante la carica**:
> deciso dall'utente, non va bloccata.

> **Questo script non muove la mazza.** La mazza sta in mano al personaggio e la anima
> `PlayerSwing` (cap. 2-ter).

### Riferimenti (da collegare, non da tarare)

| Campo | In scena | Note |
|---|---|---|
| `ball` | `Skull_player` | la testa-palla |
| `ballRb` | Rigidbody di `Skull_player` | è quello che riceve l'impulso |
| `cameraTransform` | `Main Camera` | da qui arriva la direzione del tiro. Se lo lasci vuoto prende `Camera.main` |
| `swing` | `Player` | il `PlayerSwing`, che sa dove colpisce la mazza. Se vuoto lo cerca in scena. **Se non lo trova, il colpo prende la testa da qualunque distanza** |
| `skullExplosion` | vuoto | se vuoto lo pesca da `ball` in `Start()` — va bene lasciarlo così |
| `homing` | vuoto | la palla a ricerca. Se vuoto lo pesca da `ball`, e se `ball` non ce l'ha lo aggiunge con i default |

### Direzione

| Campo | Default | Cosa fa |
|---|---|---|
| `aimAlignSpeed` | 360 | Gradi al secondo con cui la direzione del tiro passa da dove guarda il personaggio a dove guarda la camera. `0` = salta subito sulla camera. |

### Angolo di tiro (rotella e tasti numerici)

L'angolo è semplicemente **quanto in alto vuoi tirare la palla**. A zero la palla scorre
raso terra. Al massimo fa un campanile. A metà parte tesa verso un punto davanti a te.

Si sceglie **in ogni momento tranne durante la carica**: camminando, fermo, in mira. Non
torna a zero entrando in mira: resta dove l'hai lasciato.

Dentro lo script l'angolo è un numero da 0 a 1, mappato sul ventaglio
`minLoftAngle → maxLoftAngle`. Così la corsa della rotella resta la stessa anche se
cambi gli estremi.

| Campo | Default | Cosa fa |
|---|---|---|
| `minLoftAngle` | 0 | Estremo basso del ventaglio, in gradi. A 0 il tiro è raso. |
| `maxLoftAngle` | 55 | Estremo alto, in gradi. **Clampato a 89** da `OnValidate`: a 90 la direzione di lancio diventa parallela a `Vector3.up`. |
| `startLoftRatio` | 0 | Angolo a inizio livello, in frazione del ventaglio. Da lì in poi l'angolo resta quello che sceglie il giocatore. |
| `loftPresets` | 0 / 0.25 / 0.45 / 0.7 / 1 | Gli angoli fissi dei tasti numerici: il primo valore va sul tasto 1, il secondo sul 2, fino al 9. Valgono sia la riga in alto sia il tastierino. Sono **frazioni del ventaglio**, non gradi: con `maxLoftAngle` 55, 0.45 = circa 25°. Più valori = più tasti attivi. |
| `loftPerRevolution` | 0.25 | Quanto cambia l'angolo in **un giro completo di rotella**, in frazione del ventaglio. Con 0.25 servono quattro giri per andare da un estremo all'altro. |
| `notchesPerRevolution` | 24 | Quanti scatti ha la tua rotella in un giro. 24 è il valore dei mouse più comuni. Se l'angolo si muove troppo o troppo poco, **questo è il numero da correggere**. |
| `loftSmooth` | 14 | Morbidezza con cui l'angolo mostrato raggiunge quello scelto. `0` = salta di scatto. |
| `showAimGizmo` | true | Accende il gizmo del ventaglio. |

L'indicatore dell'angolo nell'HUD si vede sempre: senza, lo si cambierebbe alla cieca.

> **Perché c'era un problema.** La prima versione contava gli scatti dividendo il valore
> dello scroll per 120, che è quanto manda Windows per uno scatto. Ma l'Input System non
> usa le stesse unità su tutti i sistemi: dove lo scroll arriva già in scatti (valore 1),
> quella divisione rendeva il movimento 120 volte più piccolo, e l'angolo sembrava
> immobile. Ora il numero fisso non c'è più: se il valore letto è grande viene convertito
> in scatti, se è piccolo è già in scatti.

#### Il gizmo del ventaglio

Il gizmo disegna tre cose:

- **in giallo** i due raggi dell'angolo minimo e massimo, con un arco che li unisce.
  Questa è la corsa della rotella.
- **in azzurro e in magenta** le due parabole estreme, calcolate a potenza piena e fermate
  dove incontrano un muro o il pavimento. Sono il tiro più raso e il tiro più alto
  possibili da quel punto. Servono a decidere quanto in alto puoi mettere una
  piattaforma: se sta fuori da quelle due curve, non ci si arriva.
- **in verde**, solo mentre il gioco gira, l'angolo scelto in quel momento e la parabola
  che ne esce.

### Potenza: si carica tenendo premuto

| Campo | Default | In scena | Cosa fa |
|---|---|---|---|
| `maxPower` | 20 | 20 | Potenza al **100%** della carica. Con `forceMultiplier = 1` e Rigidbody di massa 1, questo è **direttamente la velocità iniziale in m/s** della palla (impulso su massa 1 = variazione di velocità). È il numero più importante del gioco: decide quanto lontano arriva un colpo. |
| `chargeTime` | 1.2 | 1.2 | Quanti secondi di pressione servono per arrivare al **100%**. Corto = colpi forti facili e zona perfetta più stretta nel tempo; lungo = devi aspettare, e aspettare in mezzo ai nemici costa. **È la leva di ritmo del tiro.** |
| `overchargeMax` | 0.1 | — | Quanto si carica oltre il 100%. 0.1 = fino al 110%, raggiunto in `chargeTime × 1.1` secondi. Lì la carica si ferma. |
| `forceMultiplier` | 1 | 1 | Moltiplicatore finale sull'impulso. Comodo per scalare tutto il gioco senza toccare `maxPower`. |

La carica sale sempre allo stesso ritmo, anche oltre il 100%. Il colpo migliore è al
100%, non al massimo: tenendo premuto si supera il perfetto. **È una prova di tempismo.**

Quando il colpo parte, la velocità che la palla aveva viene **azzerata** prima
dell'impulso. Sommare l'impulso a una palla che si muoveva ancora darebbe tiri
imprevedibili.

### Cosa prende il colpo

Il colpo **non cerca la palla**. Prende quello che tocca la **sfera di colpo** sotto la
testa della mazza (cap. 2-ter, "Il punto di colpo"). Conta il **contatto**: un oggetto è
preso se il suo collider tocca la sfera, non se il suo centro ci cade dentro.

Tutto quello che tocca la sfera viene colpito **insieme**:

- **la palla** parte, con l'angolo e la potenza che hai scelto;
- **i nemici** vengono **storditi**, non danneggiati: cadono, volano lungo la direzione del
  tiro (`PlayerSwing.PushImpulse`) e poi si rialzano (`EnemyStun`, cap. 7). Al 100% e oltre
  restano giù più a lungo (`PlayerSwing.perfectStunMultiplier`, `overchargeStunMultiplier`);
- **se la sfera è vuota**, la mazza mulina a vuoto. Lo swing si vede comunque.

La mazza non provoca mai l'incidente di corsia: quello lo provoca solo la testa.

`BallInReach` dice se la palla tocca la sfera adesso. `LastShotHitBall` dice com'è andato
l'ultimo colpo. Lo legge `ShotCamera` (cap. 3-bis): un colpo a vuoto non apre la
telecronaca.

### Zone della carica

La carica (da 0 a 110%) è divisa in quattro zone. La zona in cui rilasci decide il colpo.

| Zona | Dove | La palla | Un nemico nella sfera |
|---|---|---|---|
| **Debole** | da 0 a `weakTolerance` | potenza fissa: `maxPower × weakPower × weakMultiplier` | stordito normale |
| **Normale** | in mezzo | potenza proporzionale alla carica | stordito normale |
| **Perfetta** | 100% ± `perfectTolerance` | `maxPower × perfectMultiplier` | stordito più a lungo |
| **Oltre** | sopra la perfetta, fino al 110% | `maxPower × overchargeMultiplier`, e **cerca il nemico** (cap. 2-quater) | stordito più a lungo |

| Campo | Default | In scena | Cosa fa |
|---|---|---|---|
| `weakTolerance` | 0.07 | 0.07 | Fin dove la carica è un colpo debole. 0 = zona spenta. Era `zoneWidth`: il valore è passato da solo. |
| `weakPower` | 0.12 | 0.12 | La potenza del colpo debole, in frazione di `maxPower`. Serve perché a carica quasi zero la forza sarebbe quasi zero e il tiro non partirebbe. Era `bottomZonePower`. |
| `weakMultiplier` | 1 | 1 | Moltiplica il colpo debole. Era `bottomZoneMultiplier`. |
| `perfectTolerance` | 0.03 | — | Metà larghezza della zona perfetta, attorno al 100%. Con 0.03 la zona va dal 97% al 103%. **Non può superare `overchargeMax`**, altrimenti la zona "oltre" non esisterebbe. |
| `perfectMultiplier` | 1.3 | 1.3 | Il boost del colpo perfetto. Era `topZoneMultiplier`. |
| `overchargeMultiplier` | 1.6 | — | La potenza oltre il 100%: la palla più veloce del gioco. **Tienilo sopra `perfectMultiplier`.** Vale anche quando non c'è un nemico da cercare: allora la palla parte dritta con l'angolo scelto. Con un bersaglio e `SkullHoming.arcAngle` > 0 la velocità la decide la curva, non questo numero. |

**Quanto dura la zona perfetta, nel tempo**: `2 × perfectTolerance × chargeTime` secondi.
Con 0.03 e 1.2 s sono **0,07 secondi**, circa 4 frame a 60 fps. Se è troppo difficile, alza
`perfectTolerance` o `chargeTime`.

Dopo il tiro, `LastShotZone` dice in quale zona si era fermata la carica,
`LastShotImpulse` con quanta forza è partita la palla, `LastShotHoming` se è partita a
ricerca. Serviranno al punteggio.

### Anteprima della traiettoria (quadratini)

Una fila di quadratini lungo la parabola, sempre girati verso la camera, come in Super
Battle Golf. Si vedono **sempre in mira**. Partono da:

- **la palla**, se tocca la sfera di colpo;
- **ogni nemico** che tocca la sfera, con la sua traiettoria (la spinta dei nemici non è
  quella della palla);
- **la sfera di colpo**, se è vuota: è il tiro che partirebbe se la palla fosse lì.

Con più oggetti nella sfera si vedono più traiettorie, e i quadratini si dividono fra loro.

| Campo | Default | Cosa fa |
|---|---|---|
| `showTrajectory` | true | Spento = niente quadratini. |
| `dotPrefab` | vuoto | Un prefab da usare come quadratino. Vuoto = un quadrato creato da codice. |
| `dotMaterial` | vuoto | Materiale del quadrato creato da codice. Vuoto = materiale di default. |
| `dotSpacing` | 0.4 | Metri fra un quadratino e l'altro, **misurati lungo la curva**. Per tempo si ammasserebbero in cima alla parabola, dove la palla rallenta. |
| `dotSize` | 0.12 | Lato del quadratino, in metri. |
| `maxDots` | 60 | Quadratini in tutto, divisi fra le traiettorie. Se una traiettoria si interrompe presto con più oggetti nella sfera, alzalo. Si legge all'avvio. |
| `trajectorySteps` | 80 | Punti simulati. Minimo 2. |
| `trajectoryStep` | 0.05 | Secondi simulati per punto. `steps × step` = **quanti secondi di volo** si vedono: 80 × 0,05 = 4 s. |
| `trajectoryMask` | tutto | Cosa ferma l'anteprima. |

Mentre scegli l'angolo l'anteprima disegna il tiro al 100% (colpo perfetto): ti dice fin
dove arriveresti col colpo migliore. Mentre carichi, segue la carica. **Oltre il 100%**, se
c'è un nemico agganciato, disegna la curva della palla a ricerca verso di lui: così vedi
chi stai mirando.

> **Attenzione a cosa promette.** L'anteprima calcola una parabola pulita e niente altro:
> non tiene conto di `airDamping`, dei rimbalzi, degli urti. Vale fino al punto in cui
> l'oggetto tocca qualcosa. Da lì in poi decide la fisica.

### Costanti non esposte (bisogna toccare il codice)

- **3 m** — lunghezza dei raggi del gizmo del ventaglio.
- **89°** — tetto duro su `maxLoftAngle`, imposto da `OnValidate`.
- **120** — il valore sopra il quale lo scroll viene considerato in unità di piattaforma
  e convertito in scatti.
- I quadratini stanno sul layer **Ignore Raycast** (2) e senza collider, così non fanno
  da ostacolo alla parabola che disegnano.

---

## 2-bis. Il personaggio — `PlayerController`

`Assets/Script/Player/PlayerController.cs` — su `Player`.

Comandi:

| Tasto | Cosa fa |
|---|---|
| WASD | si muove, nella direzione in cui guarda la camera. In mira: piccoli passi |
| Shift | corre |
| Alt | si abbassa e rallenta |
| Spazio | salta |
| Ctrl | tuffo, a terra o in aria. È la schivata |
| mouse | gira la camera |
| tasto destro | **tenuto**: modalità tiro. Lasciato: si esce, anche durante la carica |
| tasto sinistro | colpo di mazza: stordisce, non fa danno. In modalità tiro: tieni premuto per caricare, rilascia per tirare |
| rotella | angolo del tiro, sempre tranne durante la carica |
| 1-9 (riga in alto o tastierino) | angoli fissi del tiro (`SkullAimController.loftPresets`) |

Due modi:

- **Free** — si muove, salta, si tuffa e mena con la mazza.
- **Shooting** — sta in postura e fa lo swing. Comanda `SkullAimController`.

> **Il corpo non viene mai spostato a mano.** Il Rigidbody resta dinamico: anche in mira si
> muove dando una velocità, quindi i muri lo fermano. Viene messo in **Interpolate** in
> `Awake()`, per la camera incollata.

### Palla

| Campo | Default | Cosa fa |
|---|---|---|
| `ignoreBall` | true | Il corpo passa attraverso la palla. All'avvio spegne gli urti fra i collider del corpo e quelli della palla. Così camminandole addosso o mettendosi in postura non la sposta: la palla si muove solo col colpo. |

Conseguenza: anche una palla che rotola verso il corpo lo attraversa. È voluto. Quando ci
sarà "prendere gli oggetti", una palla vicina si raccoglierà con **E**.

### Velocità

| Campo | Default | Cosa fa |
|---|---|---|
| `walkSpeed` | 4 | Metri al secondo camminando. |
| `runSpeed` | 7.5 | Metri al secondo con shift. Il rapporto con `walkSpeed` decide se correre si sente: sotto 1,5 volte non si nota. |
| `crouchSpeed` | 2 | Metri al secondo abbassato, con alt. |
| `acceleration` | 25 | Quanto in fretta raggiunge la velocità voluta, in m/s². Alto = comandi immediati e un po' rigidi; basso = il personaggio slitta e sembra pesante. |
| `turnSpeed` | 14 | Quanto in fretta il corpo si gira verso la direzione in cui va. In mira si gira invece nella postura. |
| `airControl` | 0.4 | In aria, frazione di `acceleration`. 0 = in aria non si sterza, 1 = come a terra. |

Abbassato non si corre: altrimenti alt+shift darebbe una corsa accucciata.

### Abbassarsi (Alt)

| Campo | Default | Cosa fa |
|---|---|---|
| `crouchHeightScale` | 0.6 | Altezza della capsula da abbassato, in frazione di quella normale. Vale anche durante il tuffo e mentre ci si rialza. |
| `crouchChangeSpeed` | 10 | Quanto in fretta cambia l'altezza. `0` = cambia di scatto. |

> I piedi restano dove sono. Se si riducesse solo l'altezza, la capsula si stringerebbe
> attorno al proprio centro e il personaggio resterebbe sospeso per aria: per questo il
> centro viene abbassato di metà della riduzione.

### Modalità tiro: postura e passi

Tenendo premuto il destro si entra in mira. Si entra **sempre**: con la palla lontana, in
movimento o assente. Non si entra solo a mezz'aria, durante un tuffo e mentre ci si rialza.

Entrando in mira succede questo:

1. davanti a dove guarda il personaggio si fissa un **ancoraggio**, a
   `PlayerSwing.strikeReach` metri;
2. il corpo si gira **di fianco al tiro**, come un golfista (`stanceAngle`);
3. il corpo si sposta in modo che la testa della mazza, in posa di mira, cada
   sull'ancoraggio. È il "si sposta a sinistra e la mazza diventa protagonista";
4. girando la mira, il corpo **gira attorno all'ancoraggio**;
5. con **WASD** l'ancoraggio si sposta a piccoli passi, e il corpo lo segue. Anche durante
   la carica.

Se si rientra in mira subito dopo un tiro, senza aver camminato, l'ancoraggio resta dove
sta la mazza (`PlayerSwing.AnchorHere`): il corpo non rifà il passo di lato.

| Campo | Default | In scena | Cosa fa |
|---|---|---|---|
| `stanceAngle` | 90 | 90 | Di quanti gradi il corpo è girato rispetto al tiro. 90 = di fianco, guarda la palla e il tiro parte alla sua sinistra. |
| `stanceMoveSpeed` | 5 | 5 | Metri al secondo massimi per mettersi in posizione. |
| `stanceSnap` | 12 | 12 | Quanto in fretta ci arriva. Più alto = più secco. **Se il passo di lato entrando in mira sembra lento, alza questi due.** |
| `aimStepSpeed` | 1 | 1 | Metri al secondo dei piccoli passi con WASD in mira. |

### Salto (Spazio)

| Campo | Default | Cosa fa |
|---|---|---|
| `jumpHeight` | 1.2 | Altezza del salto in metri. La velocità di partenza è calcolata da qui: `v = radice(2 g h)`. |
| `groundMask` | tutto | Cosa conta come terreno. |
| `groundCheckDistance` | 0.15 | Quanto sotto i piedi si cerca il terreno. |
| `maxGroundAngle` | 50 | Pendenza oltre la quale una superficie è un muro: toccarla di lato non permette di saltare. |

`IsGrounded` arriva da una sfera lanciata verso il basso appena sopra i piedi. Per 0,15 s
dopo un salto il terreno si ignora: i piedi restano vicini al suolo per un paio di passi di
fisica e il controllo direbbe ancora "a terra".

### Tuffo (Ctrl) e rialzarsi

Il tuffo è la schivata. Ctrl lancia il corpo in avanti con un piccolo arco, sia da terra
sia a mezz'aria. Verso dove ci si sta muovendo; da fermi, verso dove si guarda. In volo
**non si sterza**: è un impegno, come ogni schivata. Toccata terra il personaggio si rialza
per `getUpTime` secondi, e intanto va piano.

| Campo | Default | Cosa fa |
|---|---|---|
| `diveSpeed` | 9 | Metri al secondo in orizzontale all'inizio del tuffo. |
| `diveUpSpeed` | 1.5 | Metri al secondo verso l'alto: decide quanto dura il tuffo da terra (circa 0,3 s con 1,5). |
| `diveTilt` | 75 | Gradi di inclinazione della parte visibile (`visual`). |
| `tiltSpeed` | 12 | Quanto in fretta si inclina. |
| `getUpTime` | 0.8 | Secondi per rialzarsi dopo l'atterraggio. Intanto non si salta, non si corre, non si mira. |
| `getUpSpeed` | 1.5 | Metri al secondo mentre ci si rialza. |
| `visual` | `Player_model` | La parte visibile che si corica. **Deve essere un figlio**: il collider resta dritto, perché una capsula coricata che si rialza a terra verrebbe sparata via dalla fisica. Vuoto = nessuna inclinazione. |

Durante il tuffo e mentre ci si rialza la capsula resta bassa (`crouchHeightScale`): la
schivata passa sotto le cose.

`IsGrounded`, `IsDiving` e `IsGettingUp` sono pubblici, insieme a `IsRunning` e
`IsCrouching`, per attaccarci un Animator.

### Riferimenti

| Campo | Note |
|---|---|
| `aim` | vuoto = lo cerca in scena |
| `swing` | vuoto = lo prende dallo stesso oggetto |
| `cameraTransform` | vuoto = `Camera.main`. Da qui prende anche il `CameraController`, per dirgli quando si mira |
| `capsule` | vuoto = la prende dallo stesso oggetto |

---

## 2-ter. La mazza — `PlayerSwing`

`Assets/Script/Player/PlayerSwing.cs` — su `Player`.

La mazza è un transform solo e la muove solo questo script. Fa due cose:

- **in modalità tiro**, lo swing da golf. In mira la mazza va in posa **davanti alla
  faccia**, con la testa a terra: è la posa di partenza ed è dove avviene il colpo.
  Caricando sale lungo un arco verso destra. Al rilascio ripassa sul punto di colpo e
  prosegue a sinistra (il **follow-through**), poi rientra.
- **fuori**, il colpo a mano col tasto sinistro.

> **La mazza non ha collider.** `Start()` spegne tutti i collider di `Skeletal_arm` e dei
> suoi figli (`club_head` ne ha uno). La mazza è figlia del Rigidbody del corpo, quindi un
> suo collider fa parte del corpo fisico: in posa di mira la punta spingeva via la palla
> prima del colpo. Il colpo è **logico** (la sfera di colpo), non fisico.

### Le pose

Una posa è un oggetto vuoto figlio di `Player`: dice dove sta la mazza in quel momento.
Si creano dal menu del componente: **tasto destro sul titolo di `Player Swing (Script)`**
nell'Inspector, oppure i **tre puntini** a destra del titolo. Le voci sono in fondo:

| Voce | Cosa fa |
|---|---|
| `GOOMD - 1. Salva posa di riposo` | crea `Pose_Rest` dove sta adesso `Skeletal_arm` |
| `GOOMD - 2. Salva posa di mira` | crea `Pose_Address` dove sta adesso `Skeletal_arm` |
| `GOOMD - 3. Rimetti la mazza a riposo` | riporta `Skeletal_arm` sulla posa di riposo |
| `GOOMD - Crea testa della mazza` | crea `Club_Head` dentro la mazza, da spostare sulla punta |

Si lavora così: sposti `Skeletal_arm` dove vuoi, salvi la posa, e la rimetti a riposo.

| Campo | In scena | Cosa fa |
|---|---|---|
| `clubPivot` | `Skeletal_arm` | La mazza. |
| `clubHead` | `club_head` | Figlio della mazza, sulla punta che tocca terra. Il punto di colpo sta sotto di lui. Vuoto = il punto sta `stanceDistance` davanti al corpo. |
| `restPose` | vuoto | La posa di riposo. Vuoto = dove sta la mazza all'avvio. Sistemato dall'utente il 2026-10-12: prima puntava sulla mazza stessa. |
| `addressPose` | `Mazza a mira` | La posa di mira, davanti alla faccia. Vuoto = la posa di riposo, che col corpo girato di 90° finisce lungo la linea di tiro. |
| `addressBlendSpeed` | 14 | Quanto in fretta la mazza va in posa entrando in mira. |

### Swing da golf (arco)

Lo swing è un **arco calcolato**, non un'animazione: la posa di mira ruotata attorno a
`swingPivot`. Il piano dell'arco non è un campo: si calcola dalla posa di mira. È il piano
**inclinato** che passa per la testa della mazza e per la linea delle spalle. Ruotando su
quel piano la testa sale e gira attorno alla capsula **insieme**, come una mazza vera, e
non passa mai sotto il corpo:

**basso davanti → alto a destra (carica) → basso davanti (colpo) → alto a sinistra
(follow-through)**

Riferimenti: `Reference/swing arc.png`, `0%.png`, `50%.png`, `100%.png`. Prima o poi lo
sostituirà un'animazione vera.

| Campo | Default | In scena | Cosa fa |
|---|---|---|---|
| `swingPivot` | (0, 0.5, −0.5) | (0, 0.5, −0.5) | Il centro dell'arco, nello spazio del personaggio: un punto **dietro il collo**. Non le spalle: le spalle non ruotano su se stesse, è la mazza che gira attorno a quel punto. |
| `backswingAngle` | 160 | **180** | Gradi lungo l'arco a carica piena, verso destra. **Positivo.** 90 = testa della mazza di lato all'altezza delle spalle, 180 = in alto dietro la testa. |
| `followThroughAngle` | −160 | −160 | Gradi a fine follow-through, verso sinistra. **Negativo**: positivo va dalla stessa parte della carica. Scala con la carica: un colpo piano ha un follow-through corto. |
| `invertSwingSide` | false | false | Se la carica va a sinistra invece che a destra. |
| `topRadius` | 0.5 | — | Quanto si stringe l'arco salendo: in cima il raggio (pivot → mazza) è moltiplicato per questo numero, come braccia e polsi che si piegano. 1 = arco largo quanto la mazza. |
| `swingDuration` | 0.3 | **0.25** | Durata della frustata al rilascio, dalla carica al follow-through. |
| `impactAt` | 0.4 | **0.5** | A che frazione della frustata la mazza ripassa sul punto di colpo. Prima la discesa, che parte piano e accelera; dopo il follow-through, che rallenta fino a fermarsi. |
| `recoverTime` | 0.4 | — | Finito il follow-through, secondi per tornare alla posa di mira **ripercorrendo l'arco al contrario**. In linea retta la mazza attraversava il corpo. |
| `returnSpeed` | 6 | 6 | Fuori da un colpo, quanto in fretta la mazza torna a riposo. `0` = di scatto. |

Il gizmo, selezionando `Player`, disegna senza entrare in Play:

- la mazza **rossa** in posa di mira, **gialla** in cima alla carica, **arancione** in
  cima al follow-through;
- la **strada della testa**: gialla per la carica, arancione per il follow-through;
- una **sfera azzurra**: il centro dell'arco;
- la **sfera verde**: la sfera di colpo.

### Il punto di colpo

Sotto la testa della mazza, nella posa di mira, appoggiata a terra, c'è una **sfera di
colpo** di raggio `strikeRadius`. **Tutto quello che la tocca viene colpito**: la palla,
i nemici. Conta il contatto col collider, non il centro dell'oggetto: una palla grande
tocca la mazza col bordo.

La sfera **non si vede in gioco**: è un dato del controller. Si vede come gizmo verde. Segue
il corpo: è dove la mazza colpirebbe adesso.

| Campo | Default | In scena | Cosa fa |
|---|---|---|---|
| `strikeReach` | 0.6 | **0.5** | A quanti metri davanti al personaggio si fissa l'ancoraggio entrando in mira. |
| `stanceDistance` | 0.7 | 0.7 | Solo senza `clubHead`: a che distanza davanti al corpo sta la sfera. |
| `strikeRadius` | 0.3 | 0.3 | Raggio della sfera di colpo. Più grande = più tolleranza nell'allinearsi. |
| `groundMask` | tutto | — | Dove appoggiare la sfera. Conta solo il terreno fermo, cioè i collider senza Rigidbody: altrimenti si appoggerebbe sopra la palla o un nemico. |

`IsInStrikeZone(collider)`, `CollectFlyingTargets` e `PushImpulse` sono pubblici: li usa
`SkullAimController` per sapere se prende la palla e per disegnare le traiettorie.

### Colpo a mano

Serve a rendere il gioco un action puzzle invece di un gioco di soli tiri: senza la testa
il personaggio non è disarmato. Ma **non fa danno**: **stordisce**. Il nemico colpito cade,
vola via e dopo un po' si rialza (`EnemyStun`, cap. 7). La palla resta l'unica arma.
Deciso dall'utente il 2026-10-13.

Vale per **tutto quello che la mazza colpisce senza essere la palla**: il colpo a mano e
i nemici presi dallo swing da golf. Lo swing al 100% o oltre li tiene giù più a lungo.

| Campo | Default | Cosa fa |
|---|---|---|
| `meleeAxis` | (1, 0, 0) | Asse locale del colpo a mano. (1,0,0) = dall'alto in basso. |
| `range` | 2.5 | Portata, misurata dal centro del personaggio. Solo per il colpo a mano. |
| `arc` | 110 | Ampiezza del ventaglio colpito davanti, in gradi. Solo per il colpo a mano. |
| `cooldown` | 0.55 | Secondi fra un colpo e il successivo. |
| `meleeDuration` | 0.3 | Durata dell'oscillazione. |
| `hitAt` | 0.45 | A che punto dell'oscillazione il colpo prende i bersagli. Li prende **una volta sola** per colpo. |
| `meleeFrom` | −70 | Gradi di partenza dell'arco. |
| `meleeTo` | 70 | Gradi di arrivo. |

### Spinta

| Campo | Default | Cosa fa |
|---|---|---|
| `knockForce` | 6 | Spinta data a chi viene colpito: quanto lontano vola lo stordito. |
| `knockLift` | 0.25 | Quanta parte della spinta va verso l'alto. |

`EnemyStun` rende il nemico fisico **prima** della spinta: un nemico in fila è kinematic, e
la spinta da sola non lo muoverebbe. Un nemico senza `EnemyStun` (non succede: lo aggiunge
`EnemyHealth`) verrebbe solo spinto, se è già fisico.

### Stordimento dello swing

| Campo | Default | Cosa fa |
|---|---|---|
| `perfectStunMultiplier` | 2 | Moltiplica `EnemyStun.stunDuration` per i nemici presi dallo swing al 100%. |
| `overchargeStunMultiplier` | 2 | Lo stesso, con lo swing oltre il 100%. |

Il colpo a mano stordisce sempre per la durata normale.

### Cosa può colpire

| Campo | Default | Cosa fa |
|---|---|---|
| `hitMask` | tutto | Quali layer vengono considerati. |
| `maxTargetsPerSwing` | 16 | Quanti bersagli al massimo in un colpo. È un buffer allocato una volta all'avvio, così un colpo non alloca niente. |

---

## 2-quater. La palla a ricerca — `SkullHoming`

`Assets/Script/Player/SkullHoming.cs` — su `Skull_player`. Se manca, lo aggiunge
`SkullAimController` all'avvio con i valori di default.

È il colpo **caricato oltre il 100%** che prende la palla. Al rilascio la palla aggancia un
nemico e gli va addosso seguendo una curva.

**Il bersaglio**: il nemico vivo più vicino dentro un cono davanti al tiro. Se due nemici
sono quasi alla stessa distanza, vince quello più allineato al tiro. Se nel cono non c'è
nessuno, la palla parte come un colpo normale a `overchargeMultiplier`.

**La curva**: una parabola che parte `arcAngle` gradi sopra la linea dritta verso il
nemico. La forma è la stessa a ogni distanza. La velocità si adatta per chiudere la curva
sul nemico.

**Il volo**: la palla non vola libera. Il codice la porta lungo la curva, dandole una
velocità a ogni passo di fisica. Resta un corpo fisico, quindi gli urti funzionano. Parte
lenta e arriva veloce (`easeIn`). La fine della curva viene spostata sul nemico di adesso,
quindi segue anche chi cammina. La guida si spegne al primo urto, se il nemico muore, o a
fine curva: da lì la palla continua libera con la velocità che ha.

| Campo | Default | Cosa fa |
|---|---|---|
| `range` | 25 | Fin dove cerca un nemico, in metri dalla palla. |
| `coneAngle` | 35 | Quanto è largo il cono, in gradi a destra e a sinistra del tiro. |
| `tieDistance` | 0.5 | Metri. Due nemici con meno di questa differenza di distanza contano uguali: vince il più allineato. |
| `aimHeight` | 0.75 | Dove mira sul nemico, in frazione della sua altezza. 0 = piedi, 0.5 = centro, 1 = cima. |
| `arcAngle` | 10 | L'arco: di quanti gradi la palla parte sopra la linea dritta. 5-10 = curva leggera, 20 e più = pallonetto. **0 = nessun arco scelto**: la palla parte alla velocità del colpo (`overchargeMultiplier`) sulla parabola più bassa, quasi dritta. |
| `easeIn` | 2 | Quanto accelera lungo la curva. 1 = velocità costante. 2 = parte da ferma e arriva al doppio della velocità media. Più alto = arrivo più violento. La durata del volo non cambia. |
| `steering` | 1 | Quanto la fine della curva segue il nemico. 1 = lo prende anche se si sposta. 0 = la curva resta quella del lancio. |
| `ignoreHitsFor` | 0.1 | Secondi dopo il lancio in cui un urto non spegne la guida: la palla parte da terra e la tocca ancora. |
| `showGizmo` | true | Il gizmo: un cerchio chiaro per `range`, due linee e un arco per il cono. In Play segue la direzione del tiro, fuori dal Play il forward della testa. |

> **Perché la velocità non è quella del colpo.** A velocità fissa esistono solo due
> parabole che arrivano su un punto: una bassa, quasi dritta, e una altissima. Non si può
> scegliere un arco in mezzo. Per avere l'arco scelto la velocità deve adattarsi. Vedi
> [[Storico]].

> **Legame.** Mentre la palla cerca, `SkullRolling` non la frena e non la ferma: la
> partenza lenta verrebbe presa per una palla che si sta fermando.

---

## 3. La camera — `CameraController`

`Assets/Script/Camera/CameraController.cs` — su `Main Camera`.

Terza persona, a un **offset fisso** dal personaggio. Resta sul personaggio anche quando
si tira, perché è lui che fa lo swing: in mira si avvicina soltanto (`aimOffset`).

Il mouse gira la camera, non il personaggio. Quando il personaggio si gira per camminare,
la camera resta dove l'ha messa il mouse.

Il mouse fa due cose diverse:

- **destra/sinistra (yaw)**: la camera gira attorno al personaggio, e l'offset gira con lei.
  Così resta sempre dietro, rispetto a dove guardi.
- **su/giù (pitch)**: gira solo lo sguardo. La camera **non si sposta**: resta all'offset.

La palla che vola non la segue questa camera. Se ne occupa `ShotCamera` (cap. 3-bis), che
apre una finestra nell'HUD con la telecronaca del tiro.

La direzione della camera è quella del mouse, e da qui `SkullAimController` prende la
direzione del tiro. Il verso delle dipendenze è uno solo: **mouse → camera → tiro**.

> Questo script non sa niente del gioco. Non legge il tiro, non legge lo stato del livello.
> Vuole solo un transform da seguire, e qualcuno che gli dica se si sta mirando. Prima
> leggeva lo stato del tiro per cambiare inquadratura: ora non serve più.

| Campo | Default | Cosa fa |
|---|---|---|
| `player` | **da collegare** | La capsula del personaggio. **Senza questo la camera non si muove.** |
| `offset` | (0, 2, −5) | Dove sta la camera rispetto al pivot del personaggio. **x** di lato (positivo = a destra, per una camera sopra la spalla), **y** in alto, **z** avanti/indietro (negativo = dietro). Gira solo con lo yaw. |
| `positionSmooth` | 0 | `0` = incollata all'offset. Sopra 0 la camera insegue in ritardo. Per una camera fissa lascialo a 0. **La rotazione non viene smussata** di proposito: un ritardo sulla rotazione si sente subito come comando impastato. |
| `lookSensitivity` | 0.15 | Gradi di rotazione per pixel di mouse. È la sensibilità del giocatore, candidata a finire in un menu opzioni. |
| `lookMinPitch` | −25 | Il limite dello sguardo **verso l'alto**, in gradi. Negativo = su. −25 vuol dire che alzi lo sguardo al massimo di 25° sopra l'orizzonte. |
| `lookMaxPitch` | 70 | Il limite dello sguardo **verso il basso**, in gradi. 70 vuol dire che abbassi lo sguardo fino a 70° sotto l'orizzonte. Resta sotto 90: a 90 esatti la camera guarda a picco e lo yaw diventa instabile. Con la camera fissa, guardando molto in basso il personaggio esce dal quadro: se succede, abbassa questo numero. |
| `lockCursor` | true | Cattura il puntatore del mouse. Viene liberato in `OnDisable()`, altrimenti resterebbe catturato e non si riuscirebbe più a usare l'editor. |

### In mira

In mira la camera scivola su un secondo offset, più vicino al personaggio. A dirle quando
si mira è `PlayerController`, con la proprietà `Aiming`: la camera continua a non leggere
niente del gioco.

| Campo | Default | Cosa fa |
|---|---|---|
| `aimOffset` | (0, 1.8, −2.8) | Stesse regole di `offset`. In mira il corpo sta a sinistra del punto di colpo: con x a 0 il corpo è al centro del quadro e la mazza alla sua destra, come in `Reference/start mira.png`. x positivo porta la mazza più verso il centro. |
| `aimBlendTime` | 0.25 | Secondi per passare da un offset all'altro, partendo e arrivando piano. `0` = di scatto. |

> La camera si piazza in `LateUpdate`, dopo che il personaggio si è mosso. Il Rigidbody del
> personaggio viene messo in **Interpolate** da `PlayerController.Awake()`: senza, una camera
> incollata fa vedere gli scatti della fisica a 50 Hz come un tremolio.
>
> Con `offset.x` diverso da 0 la camera sta di lato, ma la direzione del tiro resta il suo
> `forward`. Quindi il tiro parte **parallelo** a dove guardi, spostato di lato di `offset.x`.

### Urti col muro

In un dungeon la camera finirebbe dentro le pareti. Se fra il personaggio e la camera c'è
un ostacolo, la camera si avvicina fino a liberarsi.

| Campo | Default | Cosa fa |
|---|---|---|
| `collisionMask` | tutto | Cosa viene considerato ostacolo. Se la camera singhiozza avvicinandosi, togli da questa maschera i layer dei trigger e degli oggetti piccoli. |
| `collisionPadding` | 0.3 | Di quanto si stacca dal muro trovato, così non lo attraversa col piano di ritaglio. |
| `minDistance` | 1 | La distanza sotto cui non scende mai. Troppo bassa e finisci dentro il personaggio. |

Il controllo parte da sopra il personaggio, all'altezza `offset.y`, e va verso la camera.
Partendo dai piedi il pavimento stesso conterebbe come muro.

> **`distance` e `lookHeight` non esistono più** (2026-10-12): li sostituisce `offset`.
> I vecchi valori in scena si perdono, l'offset riparte da (0, 2, −5).

---

## 3-bis. La telecronaca del tiro — `ShotCamera`

`Assets/Script/Camera/ShotCamera.cs` — su `Shot_Camera_Rig`, creato da
**`GOOMD > Crea HUD in scena`**.

Una seconda camera segue la testa in volo, e quello che vede viene disegnato in una
finestrella dentro l'HUD. Come le camere che seguono la pallina in una partita di golf in
televisione.

Serve a risolvere un problema vero: il giocatore è fatto di **due pezzi che si allontanano**,
il corpo e la testa. La camera principale resta sempre dietro al corpo, quindi senza questa
finestra il tiro non si vedrebbe affatto.

Quando si apre e quando si chiude:

- **si apre** quando un colpo prende davvero la testa. Un colpo a vuoto non apre niente,
  perché non c'è nessun tiro da raccontare. La finestra guarda il contatore
  `SkullAimController.BallHits`: se è salito, un colpo ha preso la testa;
- **si chiude** quando la testa è andata oltre `hideDistance` e poi il corpo l'ha raggiunta
  (torna sotto `hideDistance`). A quel punto la testa si vede nell'inquadratura normale.
- **si chiude anche** se la testa non è mai andata oltre `hideDistance` e si è fermata: un
  colpo debole non ha niente da raccontare.

> Prima si chiudeva appena corpo e testa erano a meno di `hideDistance`. Ma al momento del
> colpo il corpo è sempre a portata di mazza, quindi vicinissimo: la finestra si chiudeva
> nello stesso frame in cui si apriva, e non si vedeva mai.

Quindi la finestra resta aperta **anche dopo che la palla si è fermata**, mentre cammini
verso la testa. È voluto: è anche il modo di ritrovarla.

### Riferimenti

| Campo | Collegare a | Note |
|---|---|---|
| `aim` | vuoto | lo cerca in scena |
| `ball` | vuoto | lo prende da `aim` |
| `player` | la capsula | **da collegare a mano.** Il generatore lo riempie solo se il personaggio è già in scena quando lo lanci |
| `shotCamera` | `Shot_Camera` | la camera della telecronaca, creata dal generatore |
| `display` | la `RawImage` nell'HUD | creata dal generatore |
| `frame` | il pannello `ShotCamera` nell'HUD | la cornice da accendere e spegnere |

### Inquadratura

| Campo | Default | Cosa fa |
|---|---|---|
| `followDistance` | 8 | Quanto la camera sta dietro alla testa. Più grande = più contesto, senso di velocità minore. |
| `followHeight` | 4 | Quanto sta in alto. |
| `followLookHeight` | 0.5 | Punto guardato sopra la testa. |
| `followSmooth` | 5 | Morbidezza dell'inseguimento. `0` = incollata. All'apertura si piazza comunque di scatto, senza arrivare da dove era prima. |
| `hideDistance` | 8 | A quanti metri dal corpo la finestra si chiude. **Va guardato insieme a `CameraController.offset`**: se la finestra si chiude quando la testa è ancora fuori dall'inquadratura principale, per un attimo non vedi né l'una né l'altra. |

La direzione da cui riprende resta **quella del tiro** per tutta la ripresa. Una camera che
segue da dietro lungo la linea del colpo si legge come una telecronaca; una che gira attorno
alla palla no.

### Risoluzione della finestra

| Campo | Default | Cosa fa |
|---|---|---|
| `textureWidth` | 480 | Larghezza in pixel della `RenderTexture`. |
| `textureHeight` | 270 | Altezza. 480×270 è 16:9. |

> **La `RenderTexture` viene creata a runtime**, quindi non serve nessun asset e non c'è
> niente da collegare. Viene liberata a mano in `OnDestroy()`: una `RenderTexture` creata
> col `new` non la raccoglie il garbage collector.

> **Non è uno SpriteRenderer** ma una `RawImage` dentro il Canvas. È la `RawImage` il
> componente che sa disegnare una `RenderTexture` in uno spazio di schermo; uno
> SpriteRenderer vive nel mondo 3D e andrebbe tenuto in posizione a mano davanti alla
> camera.

> **Se la finestra resta nera** in URP, aggiungi a `Shot_Camera` il componente
> *Universal Additional Camera Data*. Una camera creata da codice può non averlo.

La camera resta **spenta** quando la finestra è chiusa. Una camera accesa disegna la scena a
ogni frame anche se nessuno la guarda, e qui sarebbe il doppio del lavoro di rendering per
niente.

---

## 4. Il rotolamento — `SkullRolling`

`Assets/Script/Player/SkullRolling.cs` — su `Skull_player`.

> **Perché esiste**: PhysX **non modella l'attrito di rotolamento**. Una sfera su un piano
> scivola all'infinito qualunque physic material tu le metta. La frenata va scritta a mano.

> **Cambiato col tiro da golf.** Ora il teschio vola, e tutto qui dentro deve sapere se
> tocca terra o no. Il contatto si rileva dai callback di collisione, guardando la **normale**
> del contatto: un muro ha normale orizzontale e non conta come terreno, altrimenti
> strisciare contro una parete in volo rimetterebbe in funzione l'attrito di rotolamento.
> `IsGrounded` e `IsAirborne` sono esposti e li legge anche `SkullIntegrity`.

| Campo | Default | In scena | Cosa fa |
|---|---|---|---|
| `rollingDamping` | 0.15 | 0.15 | Attrito di rotolamento, in frazione di velocità persa al secondo. Agisce **solo sulle componenti orizzontali** e **solo a terra**. Alto = superficie ruvida/moquette, la palla si ferma subito; basso = ghiaccio. **Comanda la lunghezza dei tiri rasi più di ogni altro numero.** |
| `stopThreshold` | 0.4 | 0.4 | Sotto questa velocità (m/s) la palla viene azzerata di netto, velocità lineare e angolare — **ma solo se tocca terra**. Serve a non aspettare l'eternità che una sfera quasi ferma si fermi davvero. **Attenzione: questa soglia è anche la definizione di "palla ferma" per tutto il gioco** — `IsStopped` la espone e `ShotCamera` la legge per chiudere la telecronaca. Alzarla tronca i rotolamenti lenti; abbassarla dà rotolamenti lunghi. |
| `airDamping` | 0 | — | Attrito dell'aria: frazione di velocità **orizzontale** persa al secondo mentre il teschio vola. `0` = parabola pulita e prevedibile, identica all'anteprima. Alzandolo i tiri alti si accorciano e l'anteprima diventa ottimista. |
| `groundNormalMin` | 0.5 | — | Quanto deve essere rivolta in su la normale di un contatto per contare come terreno (0-1). 0.5 ≈ 60° di pendenza massima. Alzalo se il teschio "si appoggia" a pareti troppo inclinate; abbassalo se su una rampa ripida si comporta come se fosse in volo. |
| `groundGrace` | 0.12 | — | Per quanti secondi resta considerato "a terra" dopo l'ultimo contatto. Evita che su un terreno mosso l'attrito si accenda e spenga a intermittenza. Alzarlo troppo fa frenare il teschio nei primi istanti di volo. |
| `collisionSpeedLoss` | 0.12 | 0.35 | Frazione di velocità persa in un urto **perfettamente frontale** (range 0-1). Un urto di striscio perde molto meno, in proporzione al coseno dell'angolo d'impatto: così rotolare lungo un muro non frena. `0` disattiva del tutto la perdita e lascia fare a PhysX. **Si somma a quello che PhysX ha già tolto** (vedi cap. 13, Bounciness): tenerla alta significa frenare due volte. |
| `minImpactSpeed` | 1 | 1 | Sotto questa velocità d'impatto (m/s) l'urto non conta. Evita che i micro-contatti col pavimento fermino la palla. Se la palla "si incolla" toccando le pareti, alzalo. |

> **Mentre la palla cerca un nemico** (`SkullHoming.IsHoming`, cap. 2-quater) questo
> script non frena e non ferma. La palla a ricerca parte lenta: verrebbe presa per una palla
> che si sta fermando e azzerata.

---

## 5. L'integrità del teschio — `SkullIntegrity`

`Assets/Script/Player/SkullIntegrity.cs` — su `Skull_player`.

Gli HP del giocatore. Si consumano **percorrendo distanza**, non subendo colpi:
il teschio non si fa male con le proprie esplosioni, per costruzione.
A zero → `GameManager.Lose("Il teschio si e' rotto")`.

| Campo | Default | In scena | Cosa fa |
|---|---|---|---|
| `maxIntegrity` | 100 | 100 | Il totale. È solo un'unità di misura: quello che conta è il rapporto con `wearPerMeter`. Tienilo a 100 e ragiona in percentuale. |
| `wearPerMeter` | 1 | **0.5** | Integrità consumata per metro di moto **orizzontale**. Con 0,5 su 100 il budget nominale è 200 m, ma `fragility` lo accorcia (vedi sotto). **È il budget di movimento del livello**: decide quanti tiri lunghi ti puoi permettere. |
| `airWearMultiplier` | 0 | — | Quanto costa un metro percorso **in volo**, in frazione di quanto costa a terra. `0` = volare non consuma integrità, `1` = consuma come rotolare. Esiste perché il calcolo dell'usura guarda il moto orizzontale, e in volo quel moto continua: senza questo campo un metro in aria costerebbe come un metro strisciato sul pavimento, che non è la stessa cosa. **Lascialo a 0** se non hai un motivo preciso per cambiarlo: la scelta dell'angolo non deve essere un calcolo di convenienza (vedi [[Storico]]). |
| `fragility` | 1 | 1 | Quanto accelera l'usura mano a mano che il teschio si rovina. `0` = consumo costante. `1` = a integrità zero consuma il **doppio** che da nuovo. Non è una moltiplicazione lineare del budget: con `fragility = 1` il budget reale è circa il **69%** di quello nominale. |
| `wearPerImpact` | 0 | **0.25** | Costo di un urto frontale in punti di integrità, pesato sull'angolo d'impatto (di striscio costa quasi zero). Rende costoso sbattere contro i muri. |
| `minImpactSpeed` | 2 | 2 | Sotto questa velocità d'impatto (m/s) l'urto non consuma niente. **Attenzione**: questo campo si chiama come quello di `SkullRolling` ma è un'altra cosa — quello frena, questo consuma HP. Sono tarabili in modo indipendente di proposito. |

### Conti utili per la taratura

Con i valori attuali (`maxIntegrity 100`, `wearPerMeter 0.5`, `fragility 1`):

- **Budget reale ≈ 139 metri** di rotolamento per livello.
  Formula: `distanza = (maxIntegrity / wearPerMeter) * ln(1 + fragility) / fragility`.
  Con `fragility = 0` verrebbe esattamente 200 m.
- **Un tiro raso (angolo 0) a potenza massima percorre ≈ 80 m** in campo aperto.
  Formula approssimata: `distanza ≈ maxPower / (rollingDamping + linearDamping del Rigidbody)`
  = `20 / (0.15 + 0.1)` = 80 m. Nella realtà è meno: urti, dislivelli e attrito PhysX tolgono altro.

### Il tiro con angolo: gittata e altezza

Serve per **dimensionare la verticalità dei livelli**: quanto alta può stare una piattaforma
perché sia raggiungibile. Con massa 1 e `forceMultiplier = 1`, `maxPower` è la velocità
iniziale in m/s; `g` è 9,81.

- **Gittata in volo** (il "carry", prima del rimbalzo): `v² · sin(2θ) / g`
- **Altezza massima**: `(v · sin θ)² / (2g)`

Con `maxPower = 20`:

| Angolo | Carry | Altezza massima |
|---|---|---|
| 30° | ≈ 35 m | ≈ 5,1 m |
| **45°** (gittata massima) | **≈ 41 m** | ≈ 10,2 m |
| 55° (`maxLoftAngle` attuale) | ≈ 38 m | ≈ 13,7 m |

Col colpo perfetto (`perfectMultiplier = 1.3`, cioè 26 m/s):

| Angolo | Carry | Altezza massima |
|---|---|---|
| 45° | ≈ 69 m | ≈ 17,2 m |
| 55° | ≈ 65 m | ≈ 23,1 m |

> **Due avvertenze.** Primo: oltre i 45° si **perde** gittata e si **guadagna** altezza — è
> proprio questo che rende l'angolo una scelta tattica e non solo "più alto = meglio".
> Secondo: `linearDamping` del Rigidbody (0,1, cap. 13) smorza **tutti** gli assi, anche in
> volo, quindi i numeri reali sono un po' più bassi di questi. Il gizmo del ventaglio, che
> traccia la parabola pura, è ottimista per lo stesso motivo.

> **Conseguenza da tenere presente**: al momento un livello vale **poco meno di due tiri a
> tavoletta**. Se vuoi livelli da 5-6 tiri, la strada più pulita è abbassare `wearPerMeter`
> (a 0,2 il budget sale a ~350 m) oppure alzare `rollingDamping` per accorciare i tiri.
> Muovere `maxPower` invece cambia anche il feeling del colpo, non solo la durata.

---

## 6. L'esplosione — `SkullExplosion`

`Assets/Script/Player/SkullExplosion.cs` — su `Skull_player`.

Esplode al contatto con un collider **taggato `enemy`**. Danno ad area via
`Physics.OverlapSphere`: colpisce tutti gli `EnemyHealth` nel raggio, non solo quello urtato.
Si riarma a ogni tiro (`ResetExplosion()` chiamata da `Shoot()`), altrimenti esploderebbe
una volta sola per partita.

| Campo | Default | In scena | Cosa fa |
|---|---|---|---|
| `explosionVFX` | vuoto | vuoto | Prefab istanziato nel punto d'impatto. **Da assegnare.** |
| `destroyDelay` | 2 | 2 | Dopo quanti secondi il VFX viene distrutto. Deve coprire la durata dell'effetto, o si taglia a metà. |
| `explosionForce` | 700 | **25** | Forza dell'onda d'urto sui Rigidbody, applicata come `ForceMode.Impulse`. Su corpi di massa 1 è direttamente una spinta in m/s. Il default 700 era pensato per `ForceMode.Force`: **usa il valore della scena come riferimento**, non quello del codice. È puro spettacolo, non fa danno. |
| `explosionRadius` | 5 | **2.5** | Raggio dell'area colpita, in metri. Vale sia per il danno che per la spinta. Il gizmo arancione lo disegna quando `Skull_player` è selezionato. |
| `upwardsModifier` | 0.5 | **0.2** | Quanto la spinta è verso l'alto invece che radiale. Alto = i nemici volano in cielo; `0` = li spazza via orizzontalmente. |
| `affectedLayers` | tutto | tutto | Filtro dei layer investiti. Utile quando ci saranno oggetti scenici da non far volare. |
| `explosionDamage` | 1 | **2** | Danno inflitto a ogni `EnemyHealth` nel raggio. |
| `damageFalloff` | false | false | Se attivo, il danno cala con la distanza dal centro (minimo 1). Rende importante centrare bene, ma complica la lettura per il giocatore: tienilo spento finché non c'è una UI che lo comunichi. |
| `freezeSkullOnExplode` | true | true | Azzera la velocità del teschio all'esplosione. Spegnendolo il teschio prosegue attraverso l'esplosione e può innescare reazioni a catena. |
| `hideSkullOnExplode` | false | false | Nasconde il teschio. Attenzione: `ResetExplosion()` lo riattiva al tiro dopo, quindi non è una sparizione definitiva. |

> **Nota di bilanciamento attuale**: `explosionDamage = 2` contro `EnemyHealth.maxHP = 3`
> significa che **una singola esplosione non uccide un nemico**: lo lascia a 1 HP.
> Se l'intenzione è "un colpo, un nemico morto", servono `explosionDamage ≥ 3` oppure
> `maxHP = 2`. Se invece l'intenzione è costringere a due passaggi, va bene così —
> ma è bene che sia una scelta, non un caso.
>
> È la spiegazione più frequente di *"colpisco un nemico e non succede niente"*: non
> muore, quindi non viene restituito alla fisica, quindi non vola via e — se è su una
> corsia — continua indisturbato. Da qui `EnemyLaneSpawner.crashOnHit`, che fa scattare
> l'incidente sul colpo invece che sulla morte.

> **Se il teschio sembra attraversare i nemici**: il Rigidbody del teschio ha
> `Collision Detection = Discrete`. A 20 m/s con passo fisico di 0,02 s la palla avanza
> **40 cm per passo**, abbastanza da saltare a piè pari un nemico stretto. Metti
> **Continuous Dynamic** sul teschio (e **Continuous Speculative** sui nemici cinematici,
> che è l'unica modalità continua ammessa per loro).

> **Se l'esplosione non si vede**: `explosionVFX` non è assegnato. L'esplosione avviene
> lo stesso — danno, spinta, punteggio — ma non produce nessuna immagine, e con
> `freezeSkullOnExplode` acceso l'unico segnale è il teschio che si ferma di colpo.

---

## 7. I nemici — `EnemyHealth`

`Assets/Script/Enemy/EnemyHealth.cs` — su ogni `Enemy`.

Tiene un registro statico dei nemici vivi (`AliveCount`), che il `GameManager` legge
per l'obiettivo "uccidi tutti".

| Campo | Default | In scena | Cosa fa |
|---|---|---|---|
| `maxHP` | 3 | 3 | Punti vita. Confrontalo sempre con `SkullExplosion.explosionDamage` per sapere quante esplosioni servono. |
| `powerReward` | 1 | 1 | **Power** guadagnato uccidendolo: è la *valuta* per i potenziamenti futuri. Non è mai un obiettivo di livello. Qui vanno differenziati i nemici (un boss dà di più). |
| `damageScorePerHP` | 100 | 100 | Punteggio **Danni** per ogni HP effettivamente tolto. "Effettivamente" conta: un colpo da 5 su un nemico con 2 HP residui vale 2, non 5, così l'ultimo colpo non gonfia il punteggio. |
| `damageScoreOnKill` | 250 | 250 | Bonus Danni alla morte. Un nemico standard vale quindi `3 × 100 + 250 = 550` punti totali. |
| `deathVFX` | vuoto | vuoto | Prefab alla morte, distrutto dopo 2 s (costante nel codice). Puoi usare quello generato da `GOOMD > Crea VFX esplosione`. |
| `deathDelay` | 0 | **0.1** | Ritardo prima di sparire, per far vedere il VFX. Ignorato in modalità `Wreck`. |
| `deathBehaviour` | Destroy | Destroy | Cosa resta del nemico morto. **`Destroy`** = sparisce. **`Deactivate`** = resta spento in scena, pronto per essere riusato in un restart. **`Wreck`** = il corpo **rimane in gioco come ostacolo fisico**, ed è la modalità da usare per il traffico in stile Burnout. Vuole un `EnemyWreck` sull'oggetto, che è quello che lo rende dinamico e ne limita la durata. |
| `flashOnHit` | true | true | Lampeggio di colore al colpo. |
| `flashColor` | rosso | rosso | Colore del lampeggio. |
| `flashDuration` | 0.15 | 0.15 | Durata in secondi. Sotto 0,1 quasi non si vede; sopra 0,4 sembra un bug. |

Il flash usa un `MaterialPropertyBlock` e gira dentro una coroutine avviata al colpo:
niente materiale istanziato per nemico e nessun `Update` che scatti a vuoto sessanta volte
al secondo per ognuno. Sono le due cose che rendono sostenibili le folle.

### `EnemyWreck` — il corpo come birillo

`Assets/Script/Enemy/EnemyWreck.cs`. Richiede `Rigidbody`.

Al primo colpo incassato il nemico **esce dai binari** e diventa un corpo fisico libero:
rotola, ingombra, spinge gli altri e li **danneggia urtandoli**. È il pezzo che produce le
carambole — le auto distrutte di Burnout che restano in mezzo alla strada. Non serve che
muoia: vale anche da vivo.

| Campo | Default | Cosa fa |
|---|---|---|
| `knockOnDamage` | true | Basta un colpo incassato per liberarlo. È quasi sempre quello che vuoi. |
| `knockOnDeath` | true | Lo libera anche alla morte, per i casi in cui muore senza passare da un colpo (danno da esplosione a catena, `crashDamage`). |
| `impactDamage` | 1 | Danno che infligge ad **altri nemici** urtandoli. `0` = ingombra soltanto, senza far male. È il moltiplicatore della catena: alzalo e un solo tiro può azzerare una corsia. |
| `minImpactSpeed` | 4 | Sotto questa velocità d'urto è solo una spinta. Tienila alta abbastanza da non far danno mentre rotola pigramente addosso a qualcuno. |
| `impactCooldown` | 0.2 | Pausa fra un urto utile e il successivo, così un contatto strisciante non applica danno a ogni frame. |
| `impactScore` | 50 | Punti Danni per l'urto in sé. Il punteggio degli HP tolti lo aggiunge già `TakeDamage`: questo è il premio per il tamponamento, come in Burnout. |
| `lifetime` | -1 | Secondi di vita del rottame. **`-1` = resta finché il tetto globale lo consente**, ed è la scelta giusta se vuoi che i rottami siano ostacoli permanenti. |

> Un rottame **danneggia solo mentre è libero**: un nemico ancora sui binari non fa male a
> nessuno, altrimenti il traffico si autodistruggerebbe da solo negli ingorghi.

> **Il tetto ai rottami è `GameManager.maxWrecks`** (default 60, `0` = nessun limite).
> Superato, sparisce il più vecchio — che è anche quello più probabilmente lontano
> dall'azione. Senza tetto, una strada trafficata piena di rottami diventa in fretta un
> simulatore di fisica: è l'unico numero che davvero protegge il frame rate.

### `EnemyStun` — lo stordimento della mazza

`Assets/Script/Enemy/EnemyStun.cs`. Richiede `Rigidbody`.

È quello che fa la mazza a un nemico: **non toglie vita**. Il colpo lo butta giù: il corpo
diventa fisico, vola, si ribalta e rotola. Dopo `stunDuration` aspetta di essersi quasi
fermato, poi si raddrizza e torna a fare quello che faceva. Deciso dall'utente il
2026-10-13: la palla deve restare l'arma vera.

**Non va aggiunto a mano**: se un nemico ha `EnemyHealth` e un Rigidbody ma non ha
`EnemyStun`, lo aggiunge `EnemyHealth` all'avvio coi valori di default. Mettilo sul prefab
solo per tararlo.

| Campo | Default | Cosa fa |
|---|---|---|
| `stunDuration` | 2 | Secondi a terra prima di provare a rialzarsi. Lo swing al 100% o oltre lo moltiplica (`PlayerSwing.perfectStunMultiplier`, `overchargeStunMultiplier`). |
| `settleSpeed` | 0.5 | Si rialza solo quando va più piano di così, in m/s: non si rialza mentre rotola ancora. |
| `maxExtraWait` | 2 | Secondi in più al massimo ad aspettare che si fermi. Poi si rialza comunque. |
| `tumbleSpin` | 6 | Rotazione data dal colpo, in rad/s, per farlo ribaltare in avanti. 0 = viene solo spinto. |
| `getUpTime` | 0.5 | Secondi per tornare dritto. |

Colpito di nuovo mentre è a terra, il conto ricomincia. Se muore o diventa rottame
(`EnemyWreck`) mentre è a terra, lo stordimento si interrompe e il corpo resta alla fisica.

`IsDown` è vero a terra e mentre si rialza: lo leggono `EnemyPatrol` e `EnemyLaneSpawner`
per fermarsi. Eventi `onStunned` e `onRecovered` per suoni ed effetti.

> **Il ragdoll.** Il nemico oggi è un cubo, quindi il "ragdoll" è il cubo intero lasciato
> alla fisica. Con un modello con le ossa il ragdoll vero si accende nello stesso punto,
> `EnemyStun.Stun`.

---

## 7-bis. Gli archetipi di nemico

Componenti opzionali da aggiungere accanto a `EnemyHealth`. Sono combinabili:
un corazzato esplosivo che pattuglia è legittimo.

### `EnemyArmor` — corazzato direzionale e a soglia

`Assets/Script/Enemy/EnemyArmor.cs`

Due regole indipendenti, entrambe disattivabili. Un colpo bloccato **non fa esplodere il
teschio**: rimbalza come su una sponda e resta in gioco.

| Campo | Default | Cosa fa |
|---|---|---|
| `directional` | true | Attiva la corazza direzionale. |
| `armorFacing` | vuoto | Il transform il cui `forward` è il lato corazzato. Vuoto = il transform del nemico. Utile per corazzare un lato diverso da quello in cui guarda la mesh. |
| `armorHalfAngle` | 70 | Semiapertura del cono corazzato. 70° = tutto il fronte più i fianchi; 90° = mezzo nemico; 180° = invulnerabile. Il gizmo lo disegna quando lo selezioni. |
| `minBreakSpeed` | 0 | Sotto questa velocità d'impatto il colpo non passa **da nessun lato**. È l'archetipo "corazzato a soglia": costringe a spendere potenza, quindi integrità. `0` disattiva la regola. |
| `bounceKeep` | 0.9 | Quanta velocità conserva il teschio respinto. `1` = sponda perfetta, **>1 lo accelera** (bumper: si può usare per raggiungere zone lontane senza consumare integrità). |
| `minBounceSpeed` | 2 | Sotto questa velocità non riscrivo la velocità del teschio: rilanciarlo a bassa velocità lo farebbe scattare in modo innaturale. |
| `blocksSplash` | false | Se la corazza para anche l'onda d'urto che arriva dal suo lato. Spento, un esplosivo vicino uccide il corazzato aggirando la corazza — che è spesso la soluzione voluta del puzzle. |

> Il controllo è fatto da `SkullExplosion` **prima** di esplodere, non da un
> `OnCollisionEnter` di `EnemyArmor`. Se fosse qui, l'ordine dei due callback sarebbe
> casuale e ogni tanto il teschio esploderebbe lo stesso.

### `EnemyExplosive` — esplosivo a catena

`Assets/Script/Enemy/EnemyExplosive.cs`. Richiede `EnemyHealth`.

Morendo esplode a sua volta e innesca i vicini. Si aggancia da solo a `EnemyHealth.onDied`:
**non c'è niente da collegare nell'Inspector**.

| Campo | Default | Cosa fa |
|---|---|---|
| `radius` | 3 | Raggio dell'esplosione. Confrontalo con la distanza a cui piazzi i nemici: è quello che decide se la catena parte. |
| `damage` | 2 | Danno ai nemici nel raggio. |
| `force` | 20 | Spinta sui Rigidbody, come impulso. |
| `upwardsModifier` | 0.4 | Quanto la spinta va verso l'alto. |
| `damageScore` | 300 | Punteggio Danni per l'esplosione in sé. Il punteggio dei nemici colpiti lo aggiunge già `EnemyHealth.TakeDamage`, quindi **non contarlo due volte** qui. |
| `explosionVFX` / `destroyDelay` | vuoto / 2 | Come su `SkullExplosion`. |

La catena si risolve tutta **nello stesso frame**: ogni nemico esplode una volta sola
(`HasExploded`), quindi non può rimbalzare all'infinito. L'esplosione spinge anche il
teschio, se è nel raggio.

### `EnemyAbsorber` — gelatinoso

`Assets/Script/Enemy/EnemyAbsorber.cs`

Non ti ferma con gli HP ma con la fisica: si mangia gran parte della velocità del teschio.

| Campo | Default | Cosa fa |
|---|---|---|
| `speedKeep` | 0.25 | Frazione di velocità che **sopravvive** all'urto. 0,25 = ne resta un quarto. |
| `minImpactSpeed` | 1 | Sotto, non tocco niente: eviterei di incollare la palla al primo sfioramento. |
| `absorbAngular` | true | Frena anche la rotazione, altrimenti il teschio continua a filare visivamente. |

> Ha senso solo con **`SkullExplosion.freezeSkullOnExplode` spento**: se il teschio si
> ferma comunque a ogni esplosione, l'assorbimento non si vede.

### `EnemyPatrol` — pattugliamento su N punti

`Assets/Script/Enemy/EnemyPatrol.cs`. Richiede `Rigidbody`.

| Campo | Default | Cosa fa |
|---|---|---|
| `waypoints` | vuoto | I punti del percorso, in ordine. Oggetti vuoti va benissimo. **Non devono essere figli del nemico**: si muoverebbero con lui e il percorso scapperebbe in avanti — lo script te lo segnala con un warning. |
| `mode` | PingPong | `Loop` (torna al primo), `PingPong` (avanti e indietro), `Once` (si ferma in fondo). |
| `snapToFirstWaypoint` | true | Si posiziona esattamente sul primo punto in `Start()`. |
| `speed` | 2 | Metri al secondo. |
| `waitAtWaypoint` | 0.5 | Pausa a ogni punto. È la leva del **timing del tiro**: pause lunghe danno finestre generose. |
| `arriveThreshold` | 0.05 | Quanto vicino basta arrivare per considerare raggiunto il punto. |
| `faceMovement` / `turnSpeed` | true / 720 | Si gira nella direzione di marcia. `turnSpeed` 0 = gira di scatto. |
| `stunOnHit` | 0 | Secondi di stop dopo aver incassato un colpo senza morire. Feedback utile sui corazzati. |

> **Come si muove**: il Rigidbody viene reso **kinematic** e mosso con `MovePosition`.
> Un corpo dinamico spostato a mano combatte col solver e vibra contro i muri, mentre uno
> kinematic fa comunque da ostacolo solido al teschio. **Alla morte torna dinamico**, così
> l'onda d'urto che lo ha ucciso lo spedisce via: per questo `SkullExplosion` applica il
> danno *prima* della spinta, nella stessa iterazione.

**Stordito dalla mazza** (`EnemyStun.IsDown`) il pattugliamento si ferma. Rialzato, riparte
da dov'è e torna sul percorso camminando.

### `EnemyLaneSpawner` — la strada a corsie

`Assets/Script/Enemy/EnemyLaneSpawner.cs`

Una strada dal punto A al punto B divisa in N corsie parallele. I nemici compaiono in A,
scorrono fino a B e ricompaiono in A, distribuiti a distanza costante.

### Il percorso

| Campo | Default | Cosa fa |
|---|---|---|
| `pointA` / `pointB` | vuoti | Inizio e fine della strada. |
| `waypoints` | vuoto | Punti intermedi fra A e B: servono a **curvare** la strada. Vuoto = strada dritta. Sono in ordine, da A verso B. |
| `smoothPath` | true | Fa passare una **curva** (Catmull-Rom) per i punti invece di spezzare ad angolo. Con soli A e B non cambia niente. La curva parte e finisce esattamente su A e B, ma **passa vicino** ai waypoint intermedi senza per forza toccarli. Spegnilo se vuoi angoli netti. |
| `samplesPerSegment` | 8 | Quanti campioni per ogni tratto fra due punti. Più alti = curve più lisce e misure più precise, a costo trascurabile. Alzalo se una curva stretta appare spigolosa. |
| `laneSpacing` | 2 | Distanza fra una corsia e l'altra, perpendicolare alla strada. |
| `centerLanes` | true | Le corsie stanno a cavallo della linea centrale invece di partire da essa. |
| `enemyPool` | vuoto | **Il sacchetto da cui si pesca il tipo di nemico**: una voce per tipo, ognuna con un `prefab` e un `weight`. Peso 3 contro peso 1 significa che esce il triplo delle volte. Con una sola voce lo spawner genera sempre lo stesso nemico. |
| `faceTravelDirection` | true | I nemici guardano nel verso di marcia. |
| `respawnDelay` | 3 | Attesa **in più** prima che il posto di un nemico ucciso venga ripreso, sommata alla cadenza della corsia. **`-1` = quel posto resta vuoto per sempre.** |
| `startPopulated` | false | `false` = si parte a **strada vuota** e il flusso si forma sotto gli occhi del giocatore, uno alla volta dalla testa della corsia. `true` = la corsia è già piena al primo frame, con i nemici distribuiti secondo `phase`. Con `false` il campo `phase` non ha effetto. |
| `totalSpawnBudget` | -1 | **Quanti nemici passeranno in tutto**, su tutte le corsie insieme. `-1` = infiniti. Finito il budget la strada si svuota e non ne arrivano più. |

### L'incidente

| Campo | Default | Cosa fa |
|---|---|---|
| `stopLaneOnCrash` | true | Colpire un nemico manda in frantumi la sua corsia: **tutti** quelli di quella corsia, lui compreso, escono dai binari, vengono spazzati via e il traffico si ferma. Spegnilo per tornare a una fila che prosegue indifferente. |
| `crashOnHit` | true | L'incidente scatta al **colpo incassato**, non alla morte. È quasi sempre quello che vuoi: in Burnout ci si schianta, non si aspetta che il bersaglio muoia — e con un `explosionDamage` più basso degli HP del nemico, aspettare la morte significherebbe non vedere mai un incidente. Spegnendolo serve un'uccisione vera. |
| `crashImpulse` | 8 | Spinta data agli altri nemici della corsia, come impulso dal punto dell'uccisione. |
| `crashRadius` | 6 | Raggio entro cui la spinta si fa sentire: oltre, il nemico esce comunque dai binari ma quasi non si muove. |
| `crashUpwards` | 0.5 | Quanto la spinta va verso l'alto. Alto = tamponamento spettacolare, corpi in aria. |
| `crashDamage` | 0 | Danno agli spazzati via. **`0` = solo spinta**, restano vivi e rotolano in giro come corpi fisici. Alzarlo trasforma l'incidente in una carambola che fa punti Danni e uccisioni — attenzione se il livello ha una quota `requiredKills`, perché diventa molto più facile. |
| `crashRecovery` | 4 | Secondi prima che il traffico riparta, scaglionato. **`-1` = la corsia resta chiusa per sempre.** |

**L'incidente lo provoca solo la testa.** La mazza stordisce senza fare danno, quindi
non lo innesca: deciso dall'utente.

Un nemico **stordito dalla mazza** tiene il suo posto in fila: chi sta dietro frena come
davanti a un'auto ferma. Rialzato, torna in corsia camminando alla velocità della corsia,
poi riprende la marcia.

Solo la corsia colpita si ferma: le altre continuano. Gli spazzati via **non vengono più
guidati**: da quel momento li muove solo la fisica, quindi possono finire in mezzo alle
altre corsie o addosso al teschio. Restano vivi e colpibili — sono ancora punti — ma per
questo **non spariscono da soli**: con traffico continuo e molti incidenti i rottami si
accumulano nel livello.

### Impostazioni per corsia

| Campo | Default | Cosa fa |
|---|---|---|
| `enemyCount` | 3 | Quanti nemici percorrono la corsia **al massimo insieme**: sono i posti disponibili, non una garanzia. Con una cadenza lenta la corsia gira quasi sempre sotto capienza. |
| `spawnInterval` | 1 | Pausa fra una comparsa e l'altra, in secondi. `0` = il posto viene ripreso subito, come prima. |
| `intervalVariation` | 0.4 | Variazione della pausa: 0,4 = fra il 60% e il 140%. È quello che rende il traffico irregolare invece che a metronomo. |
| `direction` | A→B | Il senso di marcia. Mettendo due corsie in versi opposti si ottengono le due carreggiate di una strada vera. |
| `speed` | 2 | Velocità base della corsia, in metri al secondo. |
| `speedVariation` | 0 | Variazione **per singolo nemico**: 0,3 significa che ognuno va fra il 70% e il 130% della velocità base, estratta alla comparsa. Rompe l'effetto processione e fa sì che il traffico si addensi e si diradi da solo. |
| `minGap` | 1.2 | **Il paraurti**: distanza minima, in metri, da chi sta davanti. Non viene mai violata, quindi i nemici non si compenetrano più. `0` disattiva del tutto il controllo e li lascia passare uno dentro l'altro. Va tenuto sopra l'ingombro del prefab, o i modelli si toccheranno lo stesso. |
| `slowdownGap` | 3.5 | Entro questa distanza si comincia a frenare, in proporzione: a `slowdownGap` si va a velocità piena, a `minGap` si è fermi. La differenza fra i due valori è la morbidezza della frenata — se sono vicini si inchiodano di colpo, se sono lontani rallentano da lontano e gli ingorghi diventano lunghi e lenti. |
| `spawnBudget` | -1 | Budget della singola corsia. `-1` = illimitato. Si somma al vincolo di `totalSpawnBudget`: vale il più stretto dei due. |
| `phase` | 0 | Sfasa la corsia rispetto alle altre (0-1), per non avere tutti allineati in griglia. |
| `poolOverride` | vuoto | Sacchetto di tipi solo per questa corsia. Vuoto = usa `enemyPool`. Serve per corsie tematiche: una di soli esplosivi, una di soli corazzati. |

### Come funziona

Ogni nemico compare in **testa** alla corsia (che è `pointA` o `pointB` a seconda di
`direction`), la percorre e sparisce in fondo; dopo una pausa irregolare il posto viene
ripreso. Il tipo viene **ripescato a ogni comparsa**, quindi la composizione del traffico
cambia da sola nel tempo.

Di default la corsia **parte vuota** e si riempie da sola: il primo nemico esce subito, gli
altri a ruota secondo la cadenza. Senza `spawnInterval` lo scaglionamento usa il tempo che
serve a liberare `minGap`, altrimenti uscirebbero tutti insieme e si ammasserebbero sulla
testa della corsia. Con `startPopulated` acceso, invece, la strada è già trafficata al
primo frame.

Il percorso è misurato per **lunghezza d'arco**, quindi la velocità resta costante anche
dove la curva è stretta. Le corsie laterali sono un po' più lunghe o più corte della linea
centrale nelle curve, ma il conto usa sempre la centrale: sul lato esterno i nemici sono
impercettibilmente più lenti del dovuto. Non si nota, e in cambio le corsie restano
allineate fra loro.

Il **budget conta i nemici che passano**, non quelli vivi: arrivare in fondo ne consuma
uno, morire uccisi ne consuma un altro quando il posto viene ripreso. Se il budget è più
stretto del traffico impostato (`spawnBudget 2` con `enemyCount 5`), la corsia parte già
incompleta invece di sforare.

I nemici sono generati in `Awake()` e non in `Start()`, così si registrano in
`EnemyHealth.All` prima che `GameManager.Start()` conti i nemici del livello — altrimenti
`EnemiesAtStart` sarebbe zero e l'HUD scriverebbe `0 / 0`.

Un prefab **senza `EnemyHealth` è ammesso**: diventa un ostacolo che scorre sulla corsia e
non muore mai.

### Gli ingorghi

Chi ha qualcuno davanti **rallenta invece di attraversarlo**, con una frenata proporzionale
fra `slowdownGap` e `minGap`. Uniti a `speedVariation` — un lento davanti a un veloce — ne
vengono fuori ingorghi che si formano e si sciolgono da soli, e una coda all'ingresso della
corsia quando il traffico è più denso di quanto la strada regga.

Oltre alla frenata c'è un **limite duro** che non lascia mai violare `minGap`: serve quando
chi sta davanti si ferma di colpo, dove la sola frenata proporzionale arriverebbe tardi.
Nessuno può però tornare indietro, quindi due nemici già compenetrati alla comparsa restano
tali finché il primo non si allontana.

Il traffico **non rallenta per il teschio**: i nemici gli vanno addosso, ed è voluto.
Neanche per i nemici delle altre corsie — le corsie sono indipendenti — né per quelli
mossi da `EnemyPatrol`, che non partecipano a questo sistema.

> **Attenzione a `requireAllEnemiesDead`**: se almeno una corsia ha budget illimitato
> *e* `totalSpawnBudget` è `-1`, i nemici arrivano per sempre e il livello **non si può
> vincere per obiettivi**. Dai un budget allo spawner, oppure spegni `requireAllEnemiesDead`
> e lascia la buca come via di vittoria — che è poi il senso di una strada trafficata: un
> ostacolo da attraversare, non nemici da sterminare. Lo script avvisa all'avvio, ma **solo
> quando il traffico è davvero infinito**: con un budget finito non dice niente.

### Il gizmo

Sempre visibile, anche senza selezionare lo spawner. Per ogni corsia disegna il percorso
**seguendo le curve**, le frecce del senso di marcia, la sfera piena dove i nemici
compaiono e quella sbiadita dove spariscono, i cubi delle posizioni di partenza e
un'etichetta con indice, verso, densità, velocità e budget. Le sfere azzurre sono i punti
di controllo (A, i waypoint, B): servono a ritrovarli quando la curva liscia se ne
discosta. In Play una corsia incidentata diventa **rossa** e l'etichetta dice `INCIDENTE`.

La strada si regola a occhio senza entrare in Play, e i waypoint si possono spostare
**anche in Play**: il percorso viene ricostruito a ogni passo di fisica.

---

## 8. Vittoria e sconfitta — `GameManager`

`Assets/Script/Level/GameManager.cs` — su `Game_Manager`. Singleton (`GameManager.Instance`).

**Due strade di vittoria indipendenti, in OR:**

1. **Obiettivi** — nemici (tutti e/o N) + puzzle + soglia danni, tutti in **AND** fra loro (ognuno disattivabile)
2. **Buca** — imbucare nella `WinHole` vince da solo, anche a nemici vivi: è la *pacifist run*

`LastWinReason` registra quale delle due, servirà al punteggio finale.

| Campo | Default | In scena | Cosa fa |
|---|---|---|---|
| `skull` | vuoto | collegato | Il `SkullIntegrity` del giocatore. Se lo lasci vuoto lo cerca in scena da solo. |
| `winHole` | vuoto | **vuoto** | La `WinHole` del livello. Vuoto va benissimo: `Awake()` la trova con `FindFirstObjectByType`. Assegnala solo se un giorno ci fossero più buche e volessi scegliere quale conta. |
| `requireAllEnemiesDead` | true | true | Obiettivo "uccidi tutti". **Se in scena non c'è nessun nemico questo obiettivo viene ignorato**, non considerato già soddisfatto: senza questa precauzione un livello senza nemici si vincerebbe da solo al primo frame. |
| `requiredKills` | 0 | 0 | Obiettivo "uccidine N", indipendente dal precedente. `0` = non è un obiettivo. È il conto **cumulativo** (`GameManager.Kills`), non "quanti ne restano": è l'unico che regge con un `EnemyLaneSpawner` attivo, dove i nemici continuano ad arrivare. Con lo spawner usa questo e **spegni** `requireAllEnemiesDead`, che con del traffico in arrivo non si soddisferebbe mai. |
| `requiredDamage` | 0 | **10** | Soglia di punteggio Danni da raggiungere. `0` = non è un obiettivo. |
| `requiredPuzzles` | vuoto | vuoto | Lista di `PuzzleTrigger` che devono essere risolti. Vuoto = nessun puzzle richiesto. |
| `countDamageAfterWin` | false | false | Se continuare a contare i danni dopo la fine del livello. Utile per lasciar godere l'esplosione finale senza congelare il contatore. |
| `maxWrecks` | 60 | 60 | Quanti corpi liberi (`EnemyWreck`) possono convivere. Oltre il tetto sparisce il più vecchio. `0` = nessun limite. È la leva principale contro il calo di frame rate quando la scena si riempie di rottami. |
| `fallY` | -20 | -20 | Quota sotto la quale il teschio è considerato "fuori mappa" e il livello è perso. È una **rete di sicurezza** generica: le morti volute vanno fatte con `KillZone`, che dà un messaggio sensato. Tienilo sotto il punto più basso calpestabile del livello, con un buon margine. |

> **`requiredDamage = 10` è di fatto già soddisfatto**: `damageScorePerHP` è 100, quindi
> un singolo HP tolto a un nemico ne produce 100. Se vuoi che il punteggio danni sia
> un obiettivo vero, ragiona in migliaia (un nemico intero vale 550); se non lo vuoi
> come obiettivo, portalo a `0`.

### Sconfitta

Si perde in tre modi, tutti passano da `GameManager.Lose(motivo)`:

- integrità del teschio a zero (`SkullIntegrity`)
- ingresso in una `KillZone`
- caduta sotto `fallY`

`LastLoseReason` contiene il testo del motivo, pronto per la UI.

### Eventi da agganciare nell'Inspector

`onWin`, `onLose`, `onDamageChanged` sono `UnityEvent`: qui si collegheranno i pannelli
di fine livello e l'aggiornamento del contatore danni, senza toccare il codice.

---

## 9. La buca — `WinHole`

`Assets/Script/Level/WinHole.cs` — su `Win_hole`. Vuole un **collider in trigger**
(`Reset()` lo imposta da solo quando aggiungi il componente).

Non dichiara la vittoria da sola: alza `IsPotted` e lascia decidere al `GameManager`,
così c'è un solo posto che stabilisce se il livello è vinto.

| Campo | Default | In scena | Cosa fa |
|---|---|---|---|
| `requiredOpeners` | vuoto | **2 elementi** | Gli `HoleOpener` da colpire perché la buca si apra. **Array vuoto = buca aperta fin dall'inizio.** |
| `requireAllOpeners` | true | true | `true` = servono tutti; `false` = ne basta uno qualsiasi. È la leva di difficoltà della pacifist run. |
| `snapToCenter` | true | true | Centra il teschio nella buca all'imbucata, per una rifinitura pulita. |
| `freezeSkull` | true | true | Ferma il teschio e lo rende **kinematic**. Vedi la nota qui sotto. |
| `hideSkull` | false | false | Nasconde il teschio dopo l'imbucata. Con `snapToCenter` attivo di solito non serve. |

Eventi: `onOpened` (la buca si apre — luci, suono), `onPotted` (imbucato),
`onRejected` (il teschio è arrivato a buca ancora chiusa: buono per un feedback "serve altro").

> **Nota tecnica su `freezeSkull`**: rendere il Rigidbody kinematic è il motivo per cui
> `SkullRolling` ha la guardia `if (rb.isKinematic) return;`. Senza, Unity urlerebbe
> *"Setting linear velocity of a kinematic body is not supported"* a ogni frame fisico
> dopo l'imbucata. Se scrivi altro codice che tocca la velocità del teschio, mettici
> la stessa guardia.

> **Nota su `fallY`**: il `GameManager` salta il controllo di caduta quando la buca è
> imbucata. Serve perché una buca incassata nel terreno farebbe scattare la sconfitta
> proprio nell'istante della vittoria. Se metti la buca molto in basso, va comunque
> tenuta sopra `fallY`.

---

## 10. Gli apri-buca — `HoleOpener`

`Assets/Script/Level/HoleOpener.cs` — su `Hole_opener` e `Hole_opener (1)`.

Bersagli sparsi nel livello. Sono **l'unica cosa che apre la buca**: di proposito non hanno
niente a che vedere con nemici o esplosioni, così si può imbucare senza uccidere nessuno.
Funzionano sia come collider solidi che come trigger.

| Campo | Default | In scena | Cosa fa |
|---|---|---|---|
| `onlySkull` | true | true | `true` = lo attiva solo il teschio; `false` = lo attiva qualunque cosa lo colpisca, per esempio un nemico spedito addosso dall'onda d'urto. Spegnerlo apre a soluzioni indirette molto interessanti. |
| `minImpactSpeed` | 0 | **1** | Velocità d'impatto minima (m/s) per attivarlo. `0` = basta sfiorarlo. Alzarlo obbliga a colpirlo con forza, non a rotolarci contro per caso. |
| `triggeredVFX` | vuoto | vuoto | Prefab all'attivazione, distrutto dopo 3 s (costante nel codice). |
| `disableOnTriggered` | false | false | Fa sparire il bersaglio dopo il colpo. Utile per farlo capire senza VFX; da tenere spento se preferisci cambiargli materiale via `onTriggered`. |

Il gizmo è **giallo** quando è ancora da colpire, **verde** quando è stato attivato:
si legge lo stato del livello a colpo d'occhio anche in Play.

---

## 11. Le zone di morte — `KillZone`

`Assets/Script/Level/KillZone.cs` — su `Lava`. Vuole un **collider in trigger**.

| Campo | Default | In scena | Cosa fa |
|---|---|---|---|
| `reason` | "Caduto nella lava" | "Caduto nella lava" | Testo mostrato al giocatore, finisce in `GameManager.LastLoseReason`. Cambialo per ogni tipo di trappola: burrone, spuntoni, acqua. |

Riconosce il teschio dal **componente `SkullIntegrity`**, non dal tag: la stessa convenzione
di `WinHole` e `HoleOpener`. Non serve taggare niente.

---

## 12. I puzzle — `PuzzleTrigger`

`Assets/Script/Level/PuzzleTrigger.cs` — **non ancora in scena**.

Per ora è solo un aggancio: qualunque meccanismo (leva, piastra a pressione, sequenza)
chiama `Solve()` e il `GameManager` lo conta fra gli obiettivi.

| Campo | Default | Cosa fa |
|---|---|---|
| `puzzleName` | "Puzzle" | Nome leggibile, per la UI e per ritrovarlo nell'Inspector. |
| `powerReward` | 0 | Power guadagnato risolvendolo. Come per i nemici, è valuta, non punteggio. |

Va poi elencato in `GameManager.requiredPuzzles` per diventare un obiettivo.

---

## 12-bis. L'HUD — `HUDController`

`Assets/Script/UI/HUDController.cs` — sul Canvas `HUD`.

Solo visualizzazione: legge lo stato e lo disegna, non decide niente e non modifica niente.
Lo stato di fine livello viene **letto ogni frame** invece che agganciato a `onWin`/`onLose`,
così non c'è niente da ricollegare nell'Inspector e l'HUD funziona anche se lo aggiungi
a livello già avviato.

Tutti i riferimenti sono **opzionali**: quelli lasciati vuoti vengono saltati.
Si può quindi tenere un HUD ridotto senza toccare il codice.

### Generazione

Menu **`GOOMD > Crea HUD in scena`** (`Assets/Script/UI/Editor/HUDBuilder.cs`): costruisce
tutta la gerarchia già cablata. Da lì in poi si modifica normalmente nell'Inspector,
lo script non serve più. Richiede le **TMP Essential Resources** importate
(`Window > TextMeshPro > Import TMP Essential Resources`); se mancano avvisa e si ferma.

Menu **`GOOMD > Rifai barra potenza`**: rifà solo la barra di potenza dentro l'HUD già in
scena. Rigenerare tutto l'HUD vorrebbe dire rifare anche la telecronaca e ricollegarla.

Crea anche, fuori dal Canvas, **`Shot_Camera`** e **`Shot_Camera_Rig`**: la camera e il
regista della finestra della telecronaca (cap. 3-bis). Quelli non sono cablati su
`HUDController`, perché non sono UI. Dopo averlo lanciato controlla il campo `player` su
`Shot_Camera_Rig`: il generatore lo riempie solo se il personaggio è già in scena.

| Campo | Default | Cosa fa |
|---|---|---|
| `gameManager`, `skull`, `aim` | vuoti | Riferimenti di gioco. Vuoti = cercati in scena in `Awake()`. Lasciali vuoti. |
| `integrityBar` / `integrityFill` | — | Lo `Slider` della barra vitale e l'`Image` del Fill, colorata dal gradiente. |
| `integrityGradient` | rosso → giallo → verde | Colore della barra in funzione dell'integrità residua. |
| `integrityFormat` | `{0:0}%` | Formato della percentuale. |
| `integritySmooth` | 8 | Velocità con cui la barra insegue il valore reale. `0` = salta di scatto. **La percentuale scritta non è smussata**: su una barra vitale leggere 12% mentre sei a 8% sarebbe una bugia. |
| `lowIntegrityThreshold` | 0.25 | Sotto questa frazione la barra pulsa. È l'unico avviso che il teschio sta per rompersi, cioè che stai per perdere. |
| `lowIntegrityPulseSpeed` | 4 | Velocità del lampeggio. `0` lo disattiva. Usa `Time.unscaledTime`: continua a pulsare anche a gioco in pausa. |
| `powerRoot` | — | Il gruppo della barra di potenza: acceso solo mentre serve. Il generatore la fa **verticale, a sinistra**, come in Super Battle Golf. |
| `showWhileAiming` | true | Mostra la barra a zero già in stato `Aiming`, così non compare di colpo al primo click. |
| `powerGradient` | giallo → arancio → rosso | Colore della barra di potenza. |
| `powerLabel` / `powerFormat` | `{0:0}%` | La percentuale di carica. Arriva a 110: è la carica vera, non la barra. |
| `powerLabelFollowsFill` | true | La percentuale sale insieme al riempimento, accanto alla barra. Serve una barra verticale e un'etichetta ancorata in basso, come la crea il generatore. |
| `perfectColor` | giallo | Colore del riempimento nella zona perfetta (attorno al 100%). Sostituisce il gradiente. |
| `overchargeColor` | viola | Colore del riempimento oltre il 100%. |
| `weakBand` / `perfectBand` / `overchargeBand` | — | Tre strisce a destra della barra che segnano le zone della carica. L'altezza la calcola l'HUD dalle tolleranze di `SkullAimController`: cambiando le tolleranze le strisce seguono. Una zona larga zero sparisce. Le crea `GOOMD > Rifai barra potenza`. |
| `loftRoot` | — | Il gruppo dell'angolo di tiro: **sempre acceso**, perché l'angolo si sceglie anche fuori dalla mira. |
| `loftLabel` / `loftFormat` | `{0:0} gradi` | L'angolo in gradi. Scritto «gradi» e non il simbolo di grado di proposito: i `.cs` di questo progetto stanno senza caratteri accentati per non avere sorprese di encoding. |
| `loftBar` | — | Opzionale: lo `Slider` che mostra l'angolo sul ventaglio (0-1), non in gradi. |
| `damageFormat` | `{0:n0}` | Formato del punteggio danni (separatore delle migliaia). |
| `damageRollSpeed` | 6 | Velocità con cui il contatore rincorre il valore vero, stile Burnout. `0` = salto secco. Sul pannello finale viene comunque allineato al totale reale. |
| `powerCurrencyFormat` | `POWER {0}` | Formato del contatore di valuta. |
| `enemiesFormat` | `NEMICI {0} / {1}` | `{0}` = uccisi (`GameManager.Kills`, cumulativo). `{1}` = `requiredKills` se il livello lo chiede, altrimenti i nemici presenti all'inizio. |
| `holeIndicator` | — | GameObject acceso quando la buca si apre. Serve a far capire che gli `HoleOpener` hanno funzionato. |
| `winPanel` / `losePanel` | — | Pannelli di fine livello, spenti in `Start()`. |
| `statsFormat` | `Danni {0:n0}   Power {1}` | Riepilogo mostrato su entrambi i pannelli. |

`RestartLevel()` è pubblica e ricarica la scena corrente: il generatore la collega già al
bottone **RIGIOCA** di entrambi i pannelli. È l'unica cosa cliccabile dell'HUD — tutto il
resto ha `raycastTarget` spento per non rubare i click al gioco.

> **Se i bottoni non rispondono**: controlla l'`EventSystem` creato in scena. Ha un
> `InputSystemUIInputModule` (il progetto usa il new Input System); se il campo delle
> azioni è vuoto, premi *Assign Default Actions* nell'Inspector del componente.

---

## 13. La fisica non-script

Cose che non stanno in nessuno script ma pesano quanto i parametri sopra.

### Rigidbody di `Skull_player`

| Proprietà | Valore | Cosa fa |
|---|---|---|
| Mass | 1 | **Tienilo a 1.** Con massa 1, gli impulsi (`maxPower`, `explosionForce`) sono direttamente velocità in m/s: molto più facile da ragionare. |
| Linear Damping | 0.1 | Attrito dell'aria. **Si somma a `rollingDamping`**: la frenata reale è `0.1 + 0.15 = 0.25/s`. Se tocchi la lunghezza dei tiri, ricordati che ci sono *due* numeri, non uno. |
| Angular Damping | 0.05 | Freno alla rotazione. Estetico: decide quanto il teschio continua a girare su sé stesso. |
| Use Gravity | sì | |
| Is Kinematic | no | Lo mette a `true` solo `WinHole.Pot()` all'imbucata. |

I nemici hanno Rigidbody con Linear Damping `0`: volano più liberamente quando esplodi.

### Physic materials

`Assets/Graphic assets/Materials/`

| Materiale | Dynamic Friction | Static Friction | Bounciness | Combine | Su cosa |
|---|---|---|---|---|---|
| `Player/Skull_player` | 0.2 | 0.2 | 0 | Average | SphereCollider del teschio |
| `Environment/Base_ground` | 0.3 | 0.6 | 0 | Average | MeshCollider di `Test_World` |
| `Environment/Wall` | 0.05 | 0.05 | 0.8 | Friction Minimum, Bounce Maximum | sponde: **da assegnare** a `wall` |
| *(nessun materiale)* | 0.6 | 0.6 | 0 | Average | `wall`, i tre `Enemy`, `Lava`, gli `Hole_opener` |

> **La riga più importante è l'ultima.** Un collider senza physic material usa il
> **Default Material di PhysX**: attrito 0,6 — il **triplo** del teschio — e nessun rimbalzo.
> Le pareti e i nemici oggi sono tutti così.

### Perché la palla si pianta contro i muri

`Bounciness = 0` non è "rimbalza poco": è **restituzione zero**, cioè PhysX azzera *tutta*
la componente di velocità lungo la normale dell'urto. Un colpo perfettamente frontale contro
un muro ferma la palla di netto, prima ancora che `SkullRolling` faccia qualcosa. Poi arrivano
l'attrito 0,6 del Default Material sulla componente tangenziale e infine
`collisionSpeedLoss`: **tre frenate sovrapposte**.

Per un feeling da biliardo servono sponde elastiche: è a questo che serve
`Environment/Wall` (Bounciness 0,8 con Bounce Combine **Maximum**, così vince sullo 0 del
teschio, e attrito 0,05 con Friction Combine **Minimum**, così le sponde non raspano).

### Il problema nuovo: l'atterraggio

Adesso il teschio vola, quindi **atterra**. E `Bounciness = 0` su `Base_ground` crea un
problema diverso da quello dei muri.

Bounciness zero vuol dire che PhysX azzera tutta la velocità perpendicolare alla superficie.
Su una palla che cade dall'alto, quella velocità è la caduta stessa: la palla si ferma di
colpo dove tocca e riparte solo con la poca velocità orizzontale che le è rimasta. In
pratica **non rimbalza**.

Questo accorcia molto i tiri alti. I numeri di gittata del capitolo 5 contano solo il volo:
in un gioco di golf un buon terzo della distanza totale arriva dopo l'atterraggio, fra
rimbalzi e rotolata finale. Con Bounciness a zero quella parte non c'è.

Tre numeri da sistemare, dal più importante:

1. **Bounciness di `Base_ground`**, adesso 0. Portalo a 0,3-0,5 per avere i rimbalzi.
   Attenzione al Bounce Combine: il teschio ha Bounciness 0, quindi con *Average* il
   risultato viene dimezzato. Serve **Maximum**, come è già impostato su `Environment/Wall`.
2. **`collisionSpeedLoss` di `SkullRolling`**, adesso 0,35. Era tarato per gli urti frontali
   contro i muri. Ma un atterraggio *è* un urto quasi frontale rispetto al pavimento, quindi
   si prende il 35% della velocità, oltre a quello che ha già tolto PhysX. Se i tiri alti ti
   sembrano senza forza, abbassa prima questo.
3. **`wearPerImpact` di `SkullIntegrity`**, adesso 0,25. Vuol dire che ora il teschio consuma
   integrità anche solo atterrando, non solo sbattendo contro i muri.

> Nessuno dei tre è stato scelto pensando al volo: sono i valori di quando il teschio
> restava sempre a terra. Provali in editor prima di dare per buono il feel del tiro.
Va assegnato al collider di `wall` e a tutte le pareti future.

L'alternativa è alzare la Bounciness sul **teschio**: funziona contro tutto, ma lo fa
rimbalzare anche cadendo a terra, che è un gioco diverso — più flipper che biliardo.

Ricorda infine che l'attrito PhysX **non ferma una sfera che rotola** (vedi `SkullRolling`):
agisce sullo strisciamento e sugli urti, non sul rotolamento libero.

### Project Settings → Physics

Non toccati finora, ma sono leve reali se il gioco dovesse diventare impreciso:
`Fixed Timestep` (default 0,02 s = 50 Hz), `Default Solver Iterations`,
`Default Contact Offset`. Una palla veloce che attraversa i muri si cura prima con
**Collision Detection = Continuous** sul Rigidbody del teschio, poi con questi.

---

## 13-bis. Prestazioni: reggere il caos

L'obiettivo dichiarato è **tanti oggetti in scena insieme**. Ecco cosa è già stato fatto
perché regga, e cosa resta da fare quando i numeri cresceranno ancora.

### Già fatto

- **Nessun materiale istanziato per nemico.** Il flash al colpo passa da un
  `MaterialPropertyBlock` e legge il colore base da `sharedMaterial`. Leggere
  `renderer.material` avrebbe creato una copia del materiale per ogni nemico, moltiplicando
  memoria e draw call.
- **Nessun `Update` sui nemici.** Il lampeggio gira in una coroutine avviata al colpo e si
  spegne da solo. Un `Update` vuoto per centinaia di oggetti si paga comunque, perché ogni
  chiamata attraversa il confine fra motore e codice gestito.
- **`EnemyHealth.AliveCount` è un contatore, non una scansione.** Lo leggono ogni frame sia
  il `GameManager` sia l'HUD: ricalcolarlo scorrendo la lista sarebbe stato due passate
  complete al frame. Il registro si toglie in tempo costante scambiando l'ultimo elemento
  al posto di quello rimosso, invece di far scorrere la coda.
- **Tetto ai rottami** (`GameManager.maxWrecks`): il numero di Rigidbody dinamici
  contemporanei è il costo dominante, ed è quello che ora ha un limite.
- **Durata dei rottami** senza `Update`: la fa scadere `Destroy(go, lifetime)`.
- **Il percorso dello spawner non alloca**: viene ricostruito a ogni passo di fisica ma
  riusando le stesse liste.

### Da fare quando servirà

1. **Pooling dei nemici nello spawner.** Oggi ogni comparsa è un `Instantiate` e ogni
   sparizione in fondo alla corsia un `Destroy`. A poche unità al secondo non è un problema
   — il costo vero sono gli oggetti *contemporanei*, non il ricambio — ma se alzerai molto
   la cadenza sarà la prima cosa da cambiare. Attenzione: riusare le istanze obbliga a
   ripulire gli ascoltatori aggiunti a runtime su `onDied`/`onDamaged`, altrimenti si
   accumulano a ogni riuso.
2. **GPU Instancing sui materiali dei nemici.** Con il `MaterialPropertyBlock` già in uso,
   basta spuntare *Enable GPU Instancing* sul materiale perché le copie collassino in poche
   draw call.
3. **Far dormire i rottami fermi.** PhysX li addormenta da solo quando si fermano; se non
   succede, di solito è perché qualcosa li tocca di continuo.
4. **Collider più semplici.** Box e capsule costano molto meno delle mesh: i rottami sono
   il posto dove conviene di più.

---

## 14. Ricettario: "voglio ottenere X"

| Voglio... | Tocco |
|---|---|
| La palla non si pianta contro i muri | assegna `Environment/Wall` ai collider delle pareti, poi `SkullRolling.collisionSpeedLoss` ↓ — in quest'ordine, vedi cap. 13 |
| Il teschio prosegue dopo aver colpito un nemico | `SkullExplosion.freezeSkullOnExplode` **off** (di default si ferma di proposito) |
| Tiri più lunghi | `maxPower` ↑ oppure `SkullRolling.rollingDamping` ↓ (ricorda il Linear Damping del Rigidbody) |
| Livelli che durano più tiri | `SkullIntegrity.wearPerMeter` ↓ — è la leva più diretta e non cambia il feeling del colpo |
| Colpi forti più lenti da preparare | `SkullAimController.chargeTime` ↑ |
| Mira più precisa | `CameraController.lookSensitivity` ↓ |
| Più facile prendere la palla | `PlayerSwing.strikeRadius` ↑ |
| Passo di lato più rapido entrando in mira | `PlayerController.stanceMoveSpeed` ↑ e `stanceSnap` ↑ |
| Colpo perfetto più facile da prendere | `SkullAimController.perfectTolerance` ↑ o `chargeTime` ↑ |
| Angoli diversi sui tasti numerici | `SkullAimController.loftPresets` (frazioni del ventaglio) |
| Palla a ricerca più curva | `SkullHoming.arcAngle` ↑ |
| Palla a ricerca più dritta | `SkullHoming.arcAngle` ↓, o 0 per la parabola bassa alla velocità del colpo |
| Palla a ricerca che arriva più violenta | `SkullHoming.easeIn` ↑ |
| La ricerca aggancia nemici più laterali | `SkullHoming.coneAngle` ↑ |
| Nemici storditi più a lungo | `EnemyStun.stunDuration` ↑ |
| Nemici storditi che volano più lontano | `PlayerSwing.knockForce` ↑ |
| Allinearsi alla palla più fine | `PlayerController.aimStepSpeed` ↓ |
| Swing più stretto attorno al corpo | `PlayerSwing.topRadius` ↓ |
| Mazza più in alto in cima alla carica | `PlayerSwing.backswingAngle` ↑ (180 = in alto dietro la testa) |
| La mazza va in mira più lentamente | `PlayerSwing.addressBlendSpeed` ↓ |
| Camera più vicina in mira | `CameraController.aimOffset` (z meno negativo) |
| Salto più alto | `PlayerController.jumpHeight` ↑ |
| Tuffo più lungo | `PlayerController.diveSpeed` ↑ o `diveUpSpeed` ↑ (vola più a lungo) |
| Il tuffo costa di più | `PlayerController.getUpTime` ↑ o `getUpSpeed` ↓ |
| Un'esplosione uccide un nemico | `SkullExplosion.explosionDamage` ≥ `EnemyHealth.maxHP` |
| Esplosioni più spettacolari | `explosionForce` ↑ e `upwardsModifier` ↑ (non cambiano il danno) |
| Punire gli urti contro i muri | `SkullIntegrity.wearPerImpact` ↑ |
| Buca aperta da subito | svuota `WinHole.requiredOpeners` |
| Pacifist run più difficile | più `HoleOpener` con `requireAllOpeners = true`, e `minImpactSpeed` ↑ |
| Meno tempo morto fra un tiro e l'altro | `SkullRolling.stopThreshold` ↑ (attenzione: tronca i rotolamenti lenti) |
| Il punteggio danni conta davvero | `GameManager.requiredDamage` in migliaia (un nemico = 550) |
| Un livello a quota di uccisioni | `GameManager.requiredKills` = N, `requireAllEnemiesDead` **off** |
| Meno compenetrazioni nel traffico | `minGap` della corsia ↑ (sopra l'ingombro del prefab) |
| Ingorghi più marcati | `slowdownGap` ↑ e `speedVariation` ↑ sulla stessa corsia |
| Una strada curva | aggiungi `waypoints` fra A e B, `smoothPath` acceso |
| Traffico irregolare | `spawnInterval` ↑ e `intervalVariation` ↑ |
| L'incidente fa punti | `crashDamage` > 0 (ma facilita `requiredKills`) |
| Vedere il traffico formarsi da zero | `startPopulated` **off** (default) |
| Un colpo, un nemico morto | `SkullExplosion.explosionDamage` ≥ `EnemyHealth.maxHP` |
| La palla non attraversa i nemici | Collision Detection **Continuous Dynamic** sul teschio |
| I corpi colpiti restano come ostacoli | `EnemyWreck` sul prefab + `deathBehaviour` = `Wreck` |
| Carambole più devastanti | `EnemyWreck.impactDamage` ↑ e `minImpactSpeed` ↓ |
| Il frame rate regge con molti rottami | `GameManager.maxWrecks` ↓, oppure `EnemyWreck.lifetime` ≥ 0 |
| Una corsia bloccata per sempre dal primo colpo | `crashRecovery` = -1 |

---

## 15. Legami da non rompere

Punti in cui due valori devono restare coerenti fra loro. Muoverne uno senza l'altro
produce bug silenziosi, non errori di compilazione.

1. **`SkullRolling.stopThreshold` è la definizione unica di "palla ferma"** —
   `ShotCamera` la legge da lì apposta, per non avere due soglie che divergono. Il
   fallback `0.16f` nel codice (= 0,4²) si usa solo se la palla non ha `SkullRolling`.
   Se cambi la soglia, cambiala **solo** su `SkullRolling`. `IsStopped` pretende
   **anche** il contatto col terreno.
2. **`SkullExplosion.explosionDamage` vs `EnemyHealth.maxHP`** — decide quante esplosioni
   servono per uccidere. Vanno sempre guardati insieme.
3. **`GameManager.requiredDamage` vs `EnemyHealth.damageScorePerHP`/`damageScoreOnKill`** —
   la soglia va scritta nella stessa scala dei premi, altrimenti è già soddisfatta o
   irraggiungibile.
4. **`GameManager.fallY` vs la quota della `WinHole`** — la buca deve stare sopra, e il
   controllo di caduta è già disattivato quando `IsPotted`.
5. **`SkullIntegrity.wearPerMeter` vs `maxPower` e `rollingDamping`** — insieme decidono
   quanti tiri dura un livello. Vedi i conti al capitolo 5.
6. **`maxLoftAngle` vs l'altezza della geometria del livello** — i conti del capitolo 5
   dicono quanto si sale. Costruire una piattaforma più alta dell'altezza massima
   raggiungibile la rende inaccessibile, e non c'è nessun errore che lo segnali: usa il
   gizmo del ventaglio per verificarlo a occhio prima di entrare in Play.
7. **`trajectoryStep × trajectorySteps` vs la durata del volo** — se il prodotto è minore
   del tempo di volo, l'anteprima si interrompe a mezz'aria e sembra un bug. Con i default
   (80 × 0,05 = 4 s) basta per i tiri a potenza piena fino al massimo dell'angolo.
   Lo stesso vale per `maxDots × dotSpacing`: 60 × 0,4 = 24 m di curva in tutto, divisi
   fra le traiettorie.
8. **`notchesPerRevolution` vs la rotella che hai davvero** — il campo dice quanti scatti
   ha un giro di rotella, e `loftPerRevolution` è scritto in giri. Se il valore non
   corrisponde al mouse, la promessa "un giro = 25%" non vale più. 24 è il valore dei
   mouse più comuni.
9. **La sfera di colpo vive solo su `PlayerSwing`** — `SkullAimController` chiede a
   `IsInStrikeZone` se la palla è presa, invece di avere una sua portata: un numero solo,
   non due che divergono. La sfera dipende da tre cose insieme: la **posa di mira**
   (`addressPose`), la **testa della mazza** (`clubHead`) e `strikeRadius`. Spostando la
   posa di mira sposti anche dove si colpisce. `range` e `arc` invece valgono solo per il
   colpo a mano.
10. **`ShotCamera.hideDistance` vs `CameraController.offset`** — la finestra della
   telecronaca si chiude quando il corpo torna a quella distanza dalla testa. Se si
   chiude mentre la testa è ancora fuori dall'inquadratura principale, per un attimo non
   vedi né l'una né l'altra.
11. **`EnemyStun.stunDuration` vs la velocità dei nemici** — la mazza non fa danno, ma
   uno stordimento lungo toglie di mezzo un nemico per tutto quel tempo. Troppo lungo e la
   mazza diventa un modo per svuotare la strada senza tirare. Ricorda che lo swing al 100%
   e oltre lo moltiplica (`PlayerSwing.perfectStunMultiplier`, `overchargeStunMultiplier`).
12. **`SkullAimController.chargeTime` vs la velocità dei nemici** — caricare a fondo tiene
   il personaggio fermo per quel tempo. Se i nemici arrivano più in fretta di così, il
   colpo pieno diventa impossibile da dare in mezzo al traffico. È voluto che costi
   qualcosa, ma va misurato.
13. **`EnemyArmor.minBreakSpeed` vs `SkullAimController.maxPower`** — una soglia più alta
   della velocità massima ottenibile rende il nemico invincibile. Ricorda che la palla
   rallenta strada facendo: la soglia va confrontata con la velocità *all'arrivo*, non
   con quella di partenza.
14. **`EnemyExplosive.radius` vs la distanza fra i nemici** — è quello che decide se la
   catena parte. Un raggio più piccolo della spaziatura non innesca mai niente.
15. **I budget di `EnemyLaneSpawner` vs `GameManager.requireAllEnemiesDead`** — traffico
   infinito (nessun budget, né globale né di corsia) e obiettivo "uccidi tutti" insieme
   rendono il livello invincibile. Con uno spawner attivo l'obiettivo giusto è
   `requiredKills`.
16. **`Lane.minGap` vs l'ingombro del prefab** — la distanza minima è misurata fra i
   centri, non fra le superfici: se il nemico è largo più di `minGap`, i modelli si
   toccheranno lo stesso anche col controllo attivo.
17. **Il tag `enemy`** — è l'unica cosa che innesca l'esplosione (`SkullExplosion`
   controlla `CompareTag("enemy")`). Un nemico senza tag viene comunque **danneggiato**
   dall'onda d'urto se sta nel raggio, ma non la fa **partire**. Tutto il resto del
   progetto riconosce gli oggetti dal componente, non dal tag: questa è l'unica eccezione.
18. **`backswingAngle` positivo, `followThroughAngle` negativo** — sono due lati dello
   stesso arco. Con lo stesso segno il follow-through va dalla stessa parte della carica.
   Per girare tutto lo swing si usa `invertSwingSide`, non i segni.
19. **La mazza e i suoi figli non devono avere collider attivi** — `PlayerSwing.Start()` li
   spegne. Un collider sulla mazza fa parte del corpo fisico del personaggio e spinge via
   la palla dalla sfera di colpo. Se aggiungi pezzi alla mazza a runtime, spegni anche i
   loro.
20. **`PlayerController.stanceAngle` vs la posa di mira** — con il corpo girato di 90° la
   posa di mira va **davanti alla faccia**. La posa di riposo (alla destra del corpo) col
   corpo girato finisce lungo la linea di tiro, e sembra un errore.
21. **`perfectTolerance` vs `overchargeMax`** — la zona perfetta sta attorno al 100% e la
   zona "oltre" comincia dove finisce. Se `perfectTolerance` arriva a `overchargeMax` la
   palla a ricerca non si può più ottenere. `OnValidate` impedisce di superarlo.
22. **`perfectMultiplier` vs `overchargeMultiplier`** — oltre il 100% deve essere la palla
   più veloce. Se `overchargeMultiplier` è più basso, caricare oltre il perfetto dà un
   colpo più debole.
23. **`SkullHoming.arcAngle` e la velocità** — con un arco scelto la velocità della palla a
   ricerca la decide la curva, non `overchargeMultiplier`. Archi bassi verso nemici lontani
   chiedono velocità alte. Con `arcAngle` 0 vale di nuovo `overchargeMultiplier`.

---

## 16. Cose ancora non tarabili

Aperte, da tenere presenti quando si progetta il bilanciamento:

- **La fisica dell'atterraggio non è tarata.** `Bounciness` di `Base_ground` è 0,
  `collisionSpeedLoss` è 0,35 e `wearPerImpact` 0,25: tutti valori scelti quando il teschio
  stava sempre a terra. Vedi il capitolo 13, "Il problema nuovo: l'atterraggio".
- **`Collision Detection` è Discrete su tutti i Rigidbody.** A 20 m/s un passo di fisica
  copre 40 cm, quindi la palla può attraversare un bersaglio stretto o un pavimento
  sottile. Col volo peggiora, perché ora c'è anche un pavimento da attraversare cadendo.
  Se succede, la proprietà da cambiare sul Rigidbody della palla è `Continuous Dynamic`.
- **La telecronaca non è pensata per le parabole**: tiene una quota fissa sopra la testa e
  la guarda, quindi su un tiro molto alto inquadra il cielo. Non è rotto, è da tarare (o da
  rifare con un offset che segue anche la verticale).
- **Lo swing è un arco calcolato, non un'animazione.** Si tara con `swingPivot`,
  `backswingAngle`, `followThroughAngle`, `topRadius`. Prima o poi lo sostituirà
  un'animazione vera.
- **Il personaggio non ha animazioni.** `PlayerController.IsRunning`, `IsCrouching`,
  `IsGrounded`, `IsDiving`, `IsGettingUp` e `PlayerSwing.IsSwinging` sono pubblici proprio
  per attaccarci un Animator. Il tuffo inclina solo la parte visibile (`visual`).
- **Prendere gli oggetti non esiste ancora.** È la prossima funzionalità, e sarà una
  meccanica centrale: con **E** si prende la testa vicina e la si mette sulle spalle, con
  E di nuovo la si posa già sul punto di colpo. Altre cose al posto della testa, da
  valutare. È anche il "recupero della testa" di cui l'utente parlava.
- **I campi nuovi del 2026-10-13 non sono tarati**: zone della carica, palla a ricerca,
  stordimento. Hanno solo il default. L'utente li tarerà in una sessione dedicata, insieme
  a `Bounciness` di `Base_ground`.
- **`explosionVFX`, `deathVFX`, `triggeredVFX` non assegnati.**
- **HUD senza stile**: rettangoli pieni e font di default. Manca anche la UI degli HP
  dei singoli nemici (per ora il feedback è solo il flash rosso).
- **Pooling dei nemici** non implementato: vedi capitolo 13-bis.
- **Sistema di potenziamenti** non progettato: il Power si accumula ma non si spende.
- **Puzzle veri** non progettati: `PuzzleTrigger` è solo l'aggancio.
- **Nemici senza AI reattiva**: pattugliano un percorso fisso, ma non inseguono né
  reagiscono alla posizione del teschio.
- **Nessun prefab di nemico**: gli archetipi sono componenti da comporre a mano.
  `EnemyLaneSpawner` ne vuole uno per forza.
- **Oggetti distruttibili** inesistenti: per ora il punteggio Danni lo alimentano solo i
  nemici. Quando ci saranno, dovranno chiamare `GameManager.AddDamage()`.
- **Nessun restart di livello**: gli script hanno già i metodi (`ResetIntegrity`,
  `ResetHealth`, `ResetExplosion`, `ResetHole`, `ResetOpener`, `ResetPuzzle`) ma nessuno
  li chiama. Servirà `destroyOnDeath = false` sui nemici per riusarli.
