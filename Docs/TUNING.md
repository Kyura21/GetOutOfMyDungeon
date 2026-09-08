# GOOMD — Wiki di taratura

Riferimento di tutti i parametri esposti negli script di *Get Out Of My Dungeon*:
dove stanno, cosa fanno, cosa succede se li muovi.

> **Regola d'oro**: i numeri scritti nel codice C# sono solo i *default di creazione*.
> Quando aggiungi un componente a un GameObject, Unity copia quei valori nella scena
> e da lì in poi **comanda la scena**, non il codice. Cambiare il default in un `.cs`
> **non** cambia gli oggetti già in scena.
> Le colonne "in scena" qui sotto riportano i valori attualmente salvati in
> `Assets/Scenes/SampleScene.unity` alla data di stesura (2026-09-08).

Legenda delle colonne:

- **Default** — valore nel sorgente C#, quello che prende un oggetto nuovo
- **In scena** — valore attuale in `SampleScene`, se diverso dal default è in **grassetto**

---

## 1. Mappa rapida: chi porta cosa

| GameObject in scena | Componenti da tarare |
|---|---|
| `Game_Controller` | `SkullAimController` — mira, potenza, forza del tiro |
| `Main Camera` | `CameraController` — le tre inquadrature |
| `Skull_player` | `SkullExplosion`, `SkullRolling`, `SkullIntegrity`, Rigidbody, SphereCollider (+ physic material) |
| `Enemy`, `Enemy (1)`, `Enemy (2)` | `EnemyHealth` — HP, ricompense, punteggio danni |
| `Win_hole` | `WinHole` — buca di vittoria |
| `Hole_opener`, `Hole_opener (1)` | `HoleOpener` — bersagli che aprono la buca |
| `Game_Manager` | `GameManager` — obiettivi del livello, sconfitta, punteggi |
| `Lava` | `KillZone` — morte istantanea |
| `HUD` (Canvas) | `HUDController` — barre, contatori, pannelli di fine livello |
| `Skeletal_arm` | nessuno script proprio: lo muove `SkullAimController` |
| `Arrow` → `Directional_arrow` | `Animation` legacy con `Pointing.anim`; posizione gestita da `SkullAimController` |

---

## 2. Il tiro — `SkullAimController`

`Assets/Script/Player/SkullAimController.cs` — su `Game_Controller`.

Macchina a stati `Idle → Aiming → Powering → Shot → Idle`.
Si entra in mira con **Spazio** (lo gestisce la camera), si conferma la direzione con
**click sinistro**, si spara con un **secondo click** mentre la barra oscilla.

### Riferimenti (da collegare, non da tarare)

| Campo | In scena | Note |
|---|---|---|
| `ball` | `Skull_player` | il teschio |
| `cuePivot` | `Skeletal_arm` | la stecca |
| `ballRb` | Rigidbody di `Skull_player` | è quello che riceve l'impulso |
| `skullExplosion` | vuoto | se vuoto lo pesca da `ball` in `Start()` — va bene lasciarlo così |
| `skullRolling` | vuoto | idem |
| `aimArrow` | `Arrow` | la freccia di direzione |

### Parametri Aim

| Campo | Default | In scena | Cosa fa |
|---|---|---|---|
| `cueDistance` | 1.5 | **4** | Distanza della stecca dal centro del teschio. Puramente estetico, non influenza la forza. Alzalo se il braccio compenetra la palla, abbassalo se sembra staccato. |
| `aimSensitivity` | 0.3 | 0.3 | Gradi di rotazione della mira per ogni pixel di movimento del mouse. **È la sensibilità del giocatore**: più alto = mira nervosa e veloce, più basso = mira precisa ma lenta a girare. Il candidato numero uno per finire in un menu opzioni. |
| `aimSmooth` | 12 | 12 | Velocità di inseguimento fra la mira "vera" e quella mostrata. `0` = la stecca segue il mouse di scatto, senza inerzia. Valori bassi (2-5) danno una stecca pesante e cinematografica; alti (>20) sono praticamente istantanei. Attenzione: **smorza anche la camera**, che segue la stecca. |
| `cueRotationOffset` | (90, 0, 0) | (90, 0, 0) | Correzione di orientamento della mesh della stecca. Serve perché il cilindro di Unity ha l'asse lungo su Y invece che su Z. Toccalo solo se cambi il modello del braccio e questo punta storto. |

### Parametri Power

| Campo | Default | In scena | Cosa fa |
|---|---|---|---|
| `maxPower` | 20 | 20 | Cima della barra di potenza. Con `forceMultiplier = 1` e Rigidbody di massa 1, questo è **direttamente la velocità iniziale in m/s** del teschio (impulso su massa 1 = variazione di velocità). Il numero più importante di tutto il gioco: comanda quanto lontano si arriva con un colpo. |
| `powerSpeed` | 15 | 15 | Velocità di oscillazione della barra (`PingPong`). Un ciclo completo su-giù dura `2 * maxPower / powerSpeed` secondi ≈ **2,7 s** con i valori attuali. Alzalo per rendere il timing più difficile, abbassalo per un tiro più permissivo. È la leva di *difficoltà* del tiro. |
| `forceMultiplier` | 1 | 1 | Moltiplicatore finale sull'impulso. Comodo per scalare tutto il gioco senza toccare `maxPower` (che serve anche a disegnare la barra della UI). Tienilo a 1 finché non c'è una UI di potenza. |

### Parametri Freccia

| Campo | Default | In scena | Cosa fa |
|---|---|---|---|
| `arrowDistance` | 1.2 | **2.25** | Quanto è avanti la freccia rispetto al centro del teschio. |
| `arrowHeight` | 0.1 | 0.1 | Alzata da terra. Se la freccia sparisce dentro il pavimento, alza questo. |
| `arrowRotationOffset` | (90, 0, 0) | **(0, 0, 0)** | Correzione di orientamento della mesh della freccia (nella scena è a zero: la mesh è già orientata bene). |

### Costanti non esposte (bisogna toccare il codice)

- **0,5 s** — se dopo un tiro la palla non si muove entro mezzo secondo, lo stato torna a `Idle`
  (rete di sicurezza per i colpi troppo deboli). `SkullAimController.cs:114`.
- **+0,5** — di quanto la stecca arretra a potenza massima, feedback visivo della carica.
  `SkullAimController.cs:154`.

---

## 3. La camera — `CameraController`

`Assets/Script/Camera/CameraController.cs` — su `Main Camera`.

Tre stati: **Explore** (volo libero, WASD + Q/E, drag col tasto sinistro),
**Aim** (orbita dietro la palla dal lato della stecca), **Follow** (insegue il tiro).

### Explore

| Campo | Default | In scena | Cosa fa |
|---|---|---|---|
| `dragSensitivity` | 0.2 | 0.2 | Gradi per pixel quando trascini col tasto sinistro. Sensibilità di guardarsi intorno. |
| `minPitch` | 10 | 10 | Inclinazione minima (quanto puoi guardare verso l'orizzonte). |
| `maxPitch` | 80 | 80 | Inclinazione massima (quanto puoi guardare a picco). Sotto 90 di proposito: a 90 esatti `LookRotation` diventa instabile. |
| `moveSpeed` | 8 | 8 | Metri al secondo in volo libero. Va scalato con la dimensione dei livelli: se il dungeon diventa grande, questo diventa frustrante in fretta. |

### Aim

| Campo | Default | In scena | Cosa fa |
|---|---|---|---|
| `aimDistance` | 3 | 3 | Quanto la camera sta dietro la palla, dal lato della stecca. Più alto = inquadratura larga, vedi più tavolo ma la palla è piccola. |
| `aimHeight` | 2 | 2 | Quanto sta in alto. Il rapporto `aimHeight / aimDistance` decide l'angolo: 2/3 è una vista da biliardo classica; alzalo per una vista quasi a picco, tipo minigolf dall'alto. |
| `aimLookHeight` | 0.5 | 0.5 | Punto guardato, misurato sopra il centro della palla. Alzarlo inclina la camera verso il basso e mostra più terreno davanti. |
| `aimRotateSmooth` | 10 | 10 | Quanto la camera insegue morbidamente la rotazione della stecca. `0` = incollata, nessun ritardo. Insieme a `aimSmooth` decide quanto la mira "pesa". Se ruotando dà la nausea, **abbassa questo prima di toccare `aimSensitivity`**. |

Il gizmo giallo quando selezioni la camera disegna l'anello su cui scorre in Aim: utile per
capire a occhio `aimDistance` e `aimHeight` senza entrare in Play.

### Follow

| Campo | Default | In scena | Cosa fa |
|---|---|---|---|
| `followDistance` | 8 | 8 | Quanto sta dietro alla palla in volo. Più grande = più contesto, senso di velocità minore. |
| `followHeight` | 4 | 4 | Quota durante l'inseguimento. |
| `followLookHeight` | 0.5 | 0.5 | Punto guardato sopra la palla. |
| `followSmooth` | 5 | 5 | Morbidezza dell'inseguimento *a regime*. Basso = camera lenta e pigra, la palla scappa avanti; alto = incollata e più scattosa sugli urti. |
| `enterFollowDuration` | 0.35 | 0.35 | Durata della transizione Aim → Follow, con `SmoothStep`. È lo "stacco" nel momento del tiro. Metti `0` per un taglio secco. |

---

## 4. Il rotolamento — `SkullRolling`

`Assets/Script/Player/SkullRolling.cs` — su `Skull_player`.

> **Perché esiste**: PhysX **non modella l'attrito di rotolamento**. Una sfera su un piano
> scivola all'infinito qualunque physic material tu le metta. La frenata va scritta a mano.

| Campo | Default | In scena | Cosa fa |
|---|---|---|---|
| `rollingDamping` | 0.15 | 0.15 | Attrito di rotolamento, in frazione di velocità persa al secondo. Agisce **solo sulle componenti orizzontali**, così una caduta resta una caduta libera. Alto = superficie ruvida/moquette, la palla si ferma subito; basso = ghiaccio. **Comanda la lunghezza dei tiri più di ogni altro numero.** |
| `stopThreshold` | 0.4 | 0.4 | Sotto questa velocità (m/s) la palla viene azzerata di netto, velocità lineare e angolare. Serve a non aspettare l'eternità che una sfera quasi ferma si fermi davvero. **Attenzione: questa soglia è anche la definizione di "palla ferma" per tutto il gioco** — `IsStopped` la espone e `SkullAimController` la legge per decidere quando puoi tirare di nuovo. Alzarla rende il gioco più reattivo fra un tiro e l'altro ma tronca i rotolamenti lenti; abbassarla dà tiri lunghi e attese noiose. |
| `collisionSpeedLoss` | 0.35 | 0.35 | Frazione di velocità persa in un urto **perfettamente frontale** (range 0-1). Un urto di striscio perde molto meno, in proporzione al coseno dell'angolo d'impatto: così rotolare lungo un muro non frena. `0` disattiva del tutto la perdita e lascia fare a PhysX. |
| `minImpactSpeed` | 1 | 1 | Sotto questa velocità d'impatto (m/s) l'urto non conta. Evita che i micro-contatti col pavimento fermino la palla. Se la palla "si incolla" toccando le pareti, alzalo. |

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
| `fragility` | 1 | 1 | Quanto accelera l'usura mano a mano che il teschio si rovina. `0` = consumo costante. `1` = a integrità zero consuma il **doppio** che da nuovo. Non è una moltiplicazione lineare del budget: con `fragility = 1` il budget reale è circa il **69%** di quello nominale. |
| `wearPerImpact` | 0 | **0.25** | Costo di un urto frontale in punti di integrità, pesato sull'angolo d'impatto (di striscio costa quasi zero). Rende costoso sbattere contro i muri. |
| `minImpactSpeed` | 2 | 2 | Sotto questa velocità d'impatto (m/s) l'urto non consuma niente. **Attenzione**: questo campo si chiama come quello di `SkullRolling` ma è un'altra cosa — quello frena, questo consuma HP. Sono tarabili in modo indipendente di proposito. |

### Conti utili per la taratura

Con i valori attuali (`maxIntegrity 100`, `wearPerMeter 0.5`, `fragility 1`):

- **Budget reale ≈ 139 metri** di rotolamento per livello.
  Formula: `distanza = (maxIntegrity / wearPerMeter) * ln(1 + fragility) / fragility`.
  Con `fragility = 0` verrebbe esattamente 200 m.
- **Un tiro a potenza massima percorre ≈ 80 m** in campo aperto.
  Formula approssimata: `distanza ≈ maxPower / (rollingDamping + linearDamping del Rigidbody)`
  = `20 / (0.15 + 0.1)` = 80 m. Nella realtà è meno: urti, dislivelli e attrito PhysX tolgono altro.

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
| `deathVFX` | vuoto | vuoto | Prefab alla morte, distrutto dopo 2 s (costante nel codice). **Da assegnare.** |
| `deathDelay` | 0 | **0.1** | Ritardo prima di sparire, per far vedere il VFX. |
| `destroyOnDeath` | true | true | `true` distrugge il GameObject; `false` lo disattiva soltanto — necessario se vuoi poter fare restart del livello senza ricaricare la scena. |
| `flashOnHit` | true | true | Lampeggio di colore al colpo. |
| `flashColor` | rosso | rosso | Colore del lampeggio. |
| `flashDuration` | 0.15 | 0.15 | Durata in secondi. Sotto 0,1 quasi non si vede; sopra 0,4 sembra un bug. |

> Il flash scrive su `renderer.material.color`, quindi **istanzia il materiale** per ogni
> nemico. Va bene con pochi nemici; con decine servirà un `MaterialPropertyBlock`.

---

## 8. Vittoria e sconfitta — `GameManager`

`Assets/Script/Level/GameManager.cs` — su `Game_Manager`. Singleton (`GameManager.Instance`).

**Due strade di vittoria indipendenti, in OR:**

1. **Obiettivi** — nemici + puzzle + soglia danni, tutti in **AND** fra loro (ognuno disattivabile)
2. **Buca** — imbucare nella `WinHole` vince da solo, anche a nemici vivi: è la *pacifist run*

`LastWinReason` registra quale delle due, servirà al punteggio finale.

| Campo | Default | In scena | Cosa fa |
|---|---|---|---|
| `skull` | vuoto | collegato | Il `SkullIntegrity` del giocatore. Se lo lasci vuoto lo cerca in scena da solo. |
| `winHole` | vuoto | **vuoto** | La `WinHole` del livello. Vuoto va benissimo: `Awake()` la trova con `FindFirstObjectByType`. Assegnala solo se un giorno ci fossero più buche e volessi scegliere quale conta. |
| `requireAllEnemiesDead` | true | true | Obiettivo "uccidi tutti". **Se in scena non c'è nessun nemico questo obiettivo viene ignorato**, non considerato già soddisfatto: senza questa precauzione un livello senza nemici si vincerebbe da solo al primo frame. |
| `requiredDamage` | 0 | **10** | Soglia di punteggio Danni da raggiungere. `0` = non è un obiettivo. |
| `requiredPuzzles` | vuoto | vuoto | Lista di `PuzzleTrigger` che devono essere risolti. Vuoto = nessun puzzle richiesto. |
| `countDamageAfterWin` | false | false | Se continuare a contare i danni dopo la fine del livello. Utile per lasciar godere l'esplosione finale senza congelare il contatore. |
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

| Campo | Default | Cosa fa |
|---|---|---|
| `gameManager`, `skull`, `aim` | vuoti | Riferimenti di gioco. Vuoti = cercati in scena in `Awake()`. Lasciali vuoti. |
| `integrityBar` / `integrityFill` | — | Lo `Slider` della barra vitale e l'`Image` del Fill, colorata dal gradiente. |
| `integrityGradient` | rosso → giallo → verde | Colore della barra in funzione dell'integrità residua. |
| `integrityFormat` | `{0:0}%` | Formato della percentuale. |
| `integritySmooth` | 8 | Velocità con cui la barra insegue il valore reale. `0` = salta di scatto. **La percentuale scritta non è smussata**: su una barra vitale leggere 12% mentre sei a 8% sarebbe una bugia. |
| `lowIntegrityThreshold` | 0.25 | Sotto questa frazione la barra pulsa. È l'unico avviso che il teschio sta per rompersi, cioè che stai per perdere. |
| `lowIntegrityPulseSpeed` | 4 | Velocità del lampeggio. `0` lo disattiva. Usa `Time.unscaledTime`: continua a pulsare anche a gioco in pausa. |
| `powerRoot` | — | Il gruppo della barra di potenza: acceso solo mentre serve. |
| `showWhileAiming` | true | Mostra la barra a zero già in stato `Aiming`, così non compare di colpo al primo click. |
| `powerGradient` | giallo → arancio → rosso | Colore della barra di potenza. |
| `damageFormat` | `{0:n0}` | Formato del punteggio danni (separatore delle migliaia). |
| `damageRollSpeed` | 6 | Velocità con cui il contatore rincorre il valore vero, stile Burnout. `0` = salto secco. Sul pannello finale viene comunque allineato al totale reale. |
| `powerCurrencyFormat` | `POWER {0}` | Formato del contatore di valuta. |
| `enemiesFormat` | `NEMICI {0} / {1}` | `{0}` = uccisi, `{1}` = totale a inizio livello (`GameManager.EnemiesAtStart`). |
| `holeIndicator` | — | GameObject acceso quando la buca si apre. Serve a far capire che gli `HoleOpener` hanno funzionato. |
| `winPanel` / `losePanel` | — | Pannelli di fine livello, spenti in `Start()`. |
| `statsFormat` | `Danni {0:n0}   Power {1}` | Riepilogo mostrato su entrambi i pannelli. |

`RestartLevel()` è pubblica e ricarica la scena corrente: il generatore la collega già al
bottone **RIGIOCA** di entrambi i pannelli. È l'unica cosa cliccabile dell'HUD — tutto il
resto ha `raycastTarget` spento per non rubare i click alla camera in Explore.

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

| Materiale | Dynamic Friction | Static Friction | Bounciness | Su cosa |
|---|---|---|---|---|
| `Player/Skull_player` | 0.2 | 0.2 | 0 | SphereCollider del teschio |
| `Environment/Base_ground` | 0.3 | 0.6 | 0 | il terreno |

`Bounciness = 0` ovunque: **niente rimbalzi**. Alzarla sul teschio (0,3-0,5) darebbe un gioco
molto più caotico, da flipper. È una scelta di design grossa, da fare consapevolmente.

Ricorda che l'attrito PhysX **non ferma una sfera che rotola** (vedi `SkullRolling`): agisce
sullo strisciamento e sugli urti contro le pareti, non sul rotolamento libero.

### Project Settings → Physics

Non toccati finora, ma sono leve reali se il gioco dovesse diventare impreciso:
`Fixed Timestep` (default 0,02 s = 50 Hz), `Default Solver Iterations`,
`Default Contact Offset`. Una palla veloce che attraversa i muri si cura prima con
**Collision Detection = Continuous** sul Rigidbody del teschio, poi con questi.

---

## 14. Ricettario: "voglio ottenere X"

| Voglio... | Tocco |
|---|---|
| Tiri più lunghi | `maxPower` ↑ oppure `SkullRolling.rollingDamping` ↓ (ricorda il Linear Damping del Rigidbody) |
| Livelli che durano più tiri | `SkullIntegrity.wearPerMeter` ↓ — è la leva più diretta e non cambia il feeling del colpo |
| Tiro più difficile da azzeccare | `SkullAimController.powerSpeed` ↑ |
| Mira più precisa | `aimSensitivity` ↓ |
| Camera meno nauseante in mira | `CameraController.aimRotateSmooth` ↓, poi `SkullAimController.aimSmooth` ↓ |
| Un'esplosione uccide un nemico | `SkullExplosion.explosionDamage` ≥ `EnemyHealth.maxHP` |
| Esplosioni più spettacolari | `explosionForce` ↑ e `upwardsModifier` ↑ (non cambiano il danno) |
| Punire gli urti contro i muri | `SkullIntegrity.wearPerImpact` ↑ |
| Buca aperta da subito | svuota `WinHole.requiredOpeners` |
| Pacifist run più difficile | più `HoleOpener` con `requireAllOpeners = true`, e `minImpactSpeed` ↑ |
| Meno tempo morto fra un tiro e l'altro | `SkullRolling.stopThreshold` ↑ (attenzione: tronca i rotolamenti lenti) |
| Il punteggio danni conta davvero | `GameManager.requiredDamage` in migliaia (un nemico = 550) |

---

## 15. Legami da non rompere

Punti in cui due valori devono restare coerenti fra loro. Muoverne uno senza l'altro
produce bug silenziosi, non errori di compilazione.

1. **`SkullRolling.stopThreshold` è la definizione unica di "palla ferma"** —
   `SkullAimController.IsBallStopped()` la legge da lì apposta, per non avere due soglie
   che divergono. Il fallback `0.16f` nel codice (= 0,4²) si usa solo se `skullRolling`
   è nullo. Se cambi la soglia, cambiala **solo** su `SkullRolling`.
2. **`SkullExplosion.explosionDamage` vs `EnemyHealth.maxHP`** — decide quante esplosioni
   servono per uccidere. Vanno sempre guardati insieme.
3. **`GameManager.requiredDamage` vs `EnemyHealth.damageScorePerHP`/`damageScoreOnKill`** —
   la soglia va scritta nella stessa scala dei premi, altrimenti è già soddisfatta o
   irraggiungibile.
4. **`GameManager.fallY` vs la quota della `WinHole`** — la buca deve stare sopra, e il
   controllo di caduta è già disattivato quando `IsPotted`.
5. **`SkullIntegrity.wearPerMeter` vs `maxPower` e `rollingDamping`** — insieme decidono
   quanti tiri dura un livello. Vedi i conti al capitolo 5.
6. **Il tag `enemy`** — è l'unica cosa che innesca l'esplosione (`SkullExplosion`
   controlla `CompareTag("enemy")`). Un nemico senza tag viene comunque **danneggiato**
   dall'onda d'urto se sta nel raggio, ma non la fa **partire**. Tutto il resto del
   progetto riconosce gli oggetti dal componente, non dal tag: questa è l'unica eccezione.

---

## 16. Cose ancora non tarabili

Aperte, da tenere presenti quando si progetta il bilanciamento:

- **`explosionVFX`, `deathVFX`, `triggeredVFX` non assegnati.**
- **HUD senza stile**: rettangoli pieni e font di default. Manca anche la UI degli HP
  dei singoli nemici (per ora il feedback è solo il flash rosso).
- **Sistema di potenziamenti** non progettato: il Power si accumula ma non si spende.
- **Puzzle veri** non progettati: `PuzzleTrigger` è solo l'aggancio.
- **Nemici senza AI**: bersagli fermi.
- **Oggetti distruttibili** inesistenti: per ora il punteggio Danni lo alimentano solo i
  nemici. Quando ci saranno, dovranno chiamare `GameManager.AddDamage()`.
- **Nessun restart di livello**: gli script hanno già i metodi (`ResetIntegrity`,
  `ResetHealth`, `ResetExplosion`, `ResetHole`, `ResetOpener`, `ResetPuzzle`) ma nessuno
  li chiama. Servirà `destroyOnDeath = false` sui nemici per riusarli.
