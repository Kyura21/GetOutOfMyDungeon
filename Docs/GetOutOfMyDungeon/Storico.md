---
aliases:
  - STORICO
  - Scelte scartate
tags:
  - goomd
  - decisioni
---

# GOOMD — Storico delle scelte

Quello che è stato provato e scartato, e perché. Serve a non rifare due volte la stessa
strada e a non "correggere" una scelta che sembra strana ma ha un motivo.

[[Taratura]] dice **come** sono tarate le cose. Questo documento dice **perché** sono fatte
così e non in un altro modo.

Due tipi di voce:

- **Scartato** — provato o progettato, poi messo da parte. C'è scritto cosa l'ha fatto
  cadere.
- **Scelto** — la via presa, quando la cosa ovvia era un'altra.

Le decisioni di design (cosa deve fare il gioco) sono dell'utente. Le decisioni tecniche
(come farlo fare al motore) sono venute dal codice, di solito dopo che qualcosa non
funzionava.

---

## 1. Il tiro e i comandi

### Scartato: la stecca da biliardo

Il gioco è nato come ibrido biliardo/golf: una stecca a forma di braccio scheletrico e tiri
sempre **rasi**, con la palla che rotolava e basta.

Cambiato dall'utente il **2026-10-04**. Il motivo non era l'estetica: con tiri solo rasi
**non si possono costruire livelli con verticalità**. Un tiro raso non scavalca niente,
quindi ogni ostacolo in altezza è un muro invalicabile invece di una scelta. Serviva un
colpo che possa alzare la palla, cioè un angolo.

Resta possibile tirare raso: basta mettere l'angolo a zero, che è il minimo del ventaglio.
Il tiro di biliardo non è stato perso, è diventato un caso particolare.

### Scartato: mirare con un raycast dalla camera

Il modo ovvio di mirare sarebbe puntare un punto sul terreno con il mouse. Non funziona in
questo gioco: in modalità tiro la camera si posiziona **in base alla direzione di tiro**.
Quindi la camera guarda dove si mira, e la mira segue dove guarda la camera. Il risultato è
un anello che si auto-alimenta: camera e mira girano insieme all'impazzata.

Si mira invece in modo **relativo**: il movimento del mouse viene accumulato su un angolo,
non letto come posizione. Se un giorno qualcuno prova a rimettere il raycast, succede di
nuovo.

### Scartato: l'angolo scelto da un set di mazze

Proposta fatta quando si è passati al golf: N mazze (putter, ferro, wedge, driver), ognuna
con un angolo fisso e una potenza massima sua, da cambiare con la rotella o con dei tasti.
Più aderente al golf vero, e ogni mazza avrebbe avuto un'identità tattica.

Scartata dall'utente: l'angolo deve essere **libero e continuo**, scelto con la rotella da
zero al massimo. Era anche stato proposto un ibrido (la mazza fissa un intervallo, il mouse
rifinisce dentro quell'intervallo) e scartato per lo stesso motivo.

Le mazze potrebbero tornare più avanti come merce del negozio dei potenziamenti, ma non
come modo di scegliere l'angolo.

### Scartato: la barra di potenza che oscilla

La potenza è passata per tre forme, in questo ordine.

1. **Barra oscillante, due click.** Un click faceva partire una barra che andava su e giù
   fra zero e il massimo, un secondo click la fermava e tirava. Era una prova di tempismo.
2. **Barra oscillante, tieni e rilascia.** Cambiata quando si è scoperto che in Super
   Battle Golf la potenza si dà tenendo premuto e rilasciando il tasto sinistro. Stesso
   meccanismo di prima, un gesto solo invece di due click.
3. **Carica a pressione**, quella attuale. Scelta dall'utente: *"più tengo premuto più la
   mazza va dietro il corpo e quando rilascio la potenza è proporzionale a quanto ho
   caricato"*. La potenza sale finché si tiene premuto e si ferma al massimo. Non c'è più
   niente che oscilla.

Il motivo del passaggio da 2 a 3 è il legame con la mazza: la carica deve essere **visibile
addosso al personaggio**, non su una barra. Una barra che oscilla non si può disegnare con
un braccio che si piega. Una carica che sale, sì.

> Fonte sui comandi di Super Battle Golf: una guida di terze parti, non la documentazione
> ufficiale. La voce Wikipedia del gioco **non descrive il sistema di tiro**, parla solo
> della parte multiplayer. Da prendere come indicazione, non come verità.

### Scartato: la potenza a due tempi con zona di precisione

Proposta: primo click ferma la potenza, poi la barra riparte veloce su una zona di
precisione; centri e il colpo è pulito, sbagli e la palla parte storta (hook/slice). È il
sistema dei giochi di golf classici.

Non scelta. In un dungeon stretto un tiro che parte storto per un errore di timing si paga
molto caro, e il gioco non è una simulazione di golf.

### Spente: le zone agli estremi della potenza

Erano state chieste quando la potenza era una barra oscillante: beccare esattamente la
cima o esattamente il fondo era difficile, quindi premiarlo aveva senso.

Con la carica a pressione il massimo lo raggiunge chiunque aspetti. Un moltiplicatore sulla
zona alta non sarebbe più un premio al tempismo, sarebbe solo potenza in più regalata a
chi tiene premuto. I campi esistono ancora, ma `zoneWidth` parte da **0**, cioè spente.

Resta utile `bottomZonePower`: a carica zero la forza è zero, e qualunque moltiplicatore
applicato a zero resta zero, quindi senza quel campo un colpo a carica nulla non
partirebbe affatto.

### Scartato: dividere lo scroll per 120

Il primo conteggio degli scatti di rotella divideva il valore letto per 120, che è quanto
manda Windows per uno scatto. Ma l'Input System non usa le stesse unità su tutti i
sistemi: dove lo scroll arriva già in scatti (valore 1), quella divisione rendeva il
movimento **120 volte più piccolo**.

Il sintomo era doppio, e all'inizio sembravano due problemi diversi: l'angolo non si
muoveva quasi, e **la palla sembrava incollata al terreno**. Era la stessa cosa — l'angolo
restava a zero, quindi ogni tiro partiva raso.

Ora non c'è nessun numero fisso: se il valore letto è grande viene convertito in scatti, se
è piccolo è già in scatti. E l'ampiezza si scrive in **giri di rotella**
(`loftPerRevolution`, `notchesPerRevolution`), che è come la ragiona l'utente, invece che
in scatti.

### Scartato: far costare il volo per "bilanciare" l'angolo

Dopo aver aggiunto il volo era stato proposto di alzare `airWearMultiplier`, cioè di far
consumare integrità anche ai metri percorsi in aria. L'idea era che altrimenti il tiro alto
conviene sempre e l'angolo non è una vera scelta.

Scartato dall'utente, ed è una correzione che vale anche per il futuro: **l'angolo è
semplicemente quanto in alto vuoi tirare la palla**. Raso terra, angolo zero. Campanile,
angolo massimo. Teso verso un punto davanti a te, angolo a metà. Non è un calcolo di
convenienza da bilanciare con un costo: è un comando espressivo, e il compromesso è già
nella geometria (oltre i 45° si guadagna altezza e si perde distanza).

`airWearMultiplier` esiste ancora come campo, ma va lasciato a 0.

### Scelto: tenere il nome della classe `SkullAimController`

Passando al golf, il nome "Aim" è rimasto. Rinominare la classe avrebbe voluto dire
rinominare il file e rifare il collegamento del componente in scena, perché Unity lega i
MonoBehaviour al nome del file. Non valeva il rischio.

I **campi** erano stati rinominati con `[FormerlySerializedAs]` (`cuePivot` → `clubPivot`
e simili), che è lo strumento giusto per rinominare un campo pubblico in Unity senza
perdere i collegamenti nell'Inspector. Poi quei campi sono stati tolti del tutto, perché
la mazza non la muove più il tiro.

### Scartato: la camera che si mette dietro la palla

Per un po' la modalità tiro aveva una sua inquadratura: la camera orbitava dietro la palla,
dal lato opposto al tiro, e girava seguendo la mira.

Scartata quando il personaggio ha smesso di spostarsi sulla palla. Se il personaggio sta
fermo e fa uno swing, **è lui che va inquadrato**: una camera puntata sulla palla
nasconderebbe l'unica cosa che si muove. Ora la camera è sempre in terza persona dietro la
capsula, anche mentre si mira.

Questo ha anche raddrizzato il verso delle dipendenze. Prima la camera si posizionava in
base alla direzione di tiro, e per questo un raycast dalla camera per mirare si
auto-alimentava. Ora il mouse gira la camera, la camera dà la direzione del tiro, e basta.

### Scartato: la camera che insegue la palla dopo il tiro

Restava un secondo stato: dopo il colpo la camera principale lasciava il personaggio e
seguiva la palla, per poi tornare indietro quando si fermava.

Tolto su richiesta dell'utente: la camera principale deve stare **sempre** dietro al corpo.
Ora `CameraController` non ha più stati, e non legge più niente del gioco: vuole solo un
transform da seguire.

Al suo posto è arrivata la soluzione dell'utente al problema dei due pezzi: una **finestra
in HUD con la telecronaca** (`ShotCamera`). Una seconda camera segue la testa e il suo
quadro viene disegnato in un angolo dello schermo, come le camere che seguono la pallina in
una partita di golf in televisione. Si apre quando un colpo prende la testa e resta aperta
finché il corpo non arriva vicino.

Due dettagli tecnici che vale la pena ricordare:

- **non è uno SpriteRenderer** ma una `RawImage` dentro il Canvas, che è il componente
  capace di disegnare una `RenderTexture` in uno spazio di schermo. Uno SpriteRenderer vive
  nel mondo 3D e andrebbe tenuto in posizione davanti alla camera a mano;
- la seconda camera resta **spenta** quando la finestra è chiusa. Una camera accesa disegna
  la scena a ogni frame anche se nessuno la guarda.

### Scartato: la camera che orbita col pitch (2026-10-12)

La camera girava attorno al personaggio su due assi: muovendo il mouse in verticale saliva
e scendeva su una sfera attorno a lui. La distanza era un numero (`distance`) e il punto
guardato un altro (`lookHeight`).

Cambiato su richiesta dell'utente: la camera sta a un **offset fisso** che sceglie lui
(`offset`, un vettore). Lo yaw gira l'offset attorno al personaggio, così la camera resta
sempre dietro rispetto a dove guardi. Il pitch gira solo lo sguardo e non sposta la camera.

Il personaggio continua a girarsi verso dove cammina, e la camera non lo segue in quella
rotazione. Era già così nel codice; il "feel strano" segnalato dall'utente veniva
probabilmente dall'orbita in verticale e dalla camera che inseguiva in ritardo
(`positionSmooth` 14, aggiornata in `Update` mentre il corpo si muove a scatti di fisica).

### Scartato: il personaggio bloccato finché la palla rotola (2026-10-12)

Dopo il tiro `SkullAimController` passava in uno stato `Shot` e ci restava finché la palla
non si fermava. Per tutto quel tempo il personaggio restava in modalità tiro: non camminava
e non menava.

Tolto su richiesta dell'utente. Lo stato `Shot` non esiste più: al rilascio si torna `Idle`
e il personaggio è libero appena finisce la frustata della mazza.

Conseguenza sulla telecronaca: si apriva guardando il passaggio a `Shot`. Ora guarda un
contatore, `SkullAimController.BallHits`.

### Scartato: chiudere la telecronaca solo per vicinanza (2026-10-12)

`ShotCamera` si chiudeva appena corpo e testa erano più vicini di `hideDistance`. Ma al
momento del colpo il corpo è sempre a portata di mazza: la finestra si chiudeva nello
stesso frame in cui si apriva. L'utente l'ha segnalato come *"la camera della telecronaca
non sta funzionando per niente"*.

Ora la vicinanza conta solo dopo che la testa si è allontanata oltre `hideDistance`. Se non
si allontana mai (colpo debole), la finestra si chiude quando la testa si ferma.

### Scartato: entrare e uscire dalla mira con un clic (2026-10-12)

Il tasto destro era un interruttore: un clic per entrare in mira, un altro per uscire.

Cambiato dall'utente: **tenere premuto = mirare**. Lasciare il tasto annulla. Se dopo un
tiro il tasto è ancora premuto, finita la frustata si rientra in mira da soli.

Insieme è sparito anche il rifiuto di entrare in mira con la palla in movimento: si entra
sempre, e cosa prendere lo decide il giocatore.

### Scartato: la freccia di mira (2026-10-12)

La freccia (`Arrow`) indicava la direzione del tiro davanti alla palla. Tolta su richiesta
dell'utente: serviva come riferimento prima che ci fosse la traiettoria. Ora la direzione
la dicono i quadratini.

### Scartato: la traiettoria come linea (2026-10-12)

L'anteprima era un `LineRenderer` da collegare a mano, e in scena non era mai stato
collegato. Sostituita da una fila di **quadratini** creati da codice, come in Super Battle
Golf. Non servono materiali né collegamenti.

### Scartato: la traiettoria solo con la palla davanti (2026-10-12)

Per un giro i quadratini si vedevano solo se la palla era a portata, come segnale che il
colpo l'avrebbe presa. Scartato guardando Super Battle Golf (`Reference/start mira.png`):
la traiettoria parte dalla **testa della mazza** anche senza palla. Ora parte dalla sfera
di colpo se è vuota, e da ogni oggetto che la tocca se ce ne sono: la palla e i nemici,
ognuno con la sua traiettoria.

### Scartato: il colpo che prende solo la palla, o solo il resto (2026-10-12)

Il colpo prendeva **o** la palla **o**, se la palla non c'era, quello che trovava davanti.
Cambiato dall'utente: **tutto quello che sta nel cerchio verde viene colpito**, palla e
nemici insieme.

### Scartato: il corpo girato verso il tiro, e la postura di fianco senza posa di mira (2026-10-12)

Due passaggi, utili da ricordare insieme.

Prima versione: in mira il corpo si girava di 90° rispetto al tiro, come un golfista
(`stanceAngle` 90), ma la mazza restava nella posa di riposo, alla destra del corpo. Col
corpo girato, quel lato finisce **lungo la linea di tiro**: la mazza sembrava davanti al
personaggio, in alto. L'utente non capiva perché il personaggio "guardasse a destra".

Per un giro è stato messo `stanceAngle` a 0, col corpo girato verso il tiro. Scartato
subito dall'utente: **90 è giusto**, è così che si posiziona un golfista. Il problema era
la posa: in mira la mazza va **davanti alla faccia**, con la testa a terra. È la posa di
partenza ed è il punto dove avviene il colpo (`PlayerSwing.addressPose`).

### Scartato: il punto di colpo che cerca la palla, poi il punto fisso davanti (2026-10-12)

La zona di colpo è passata per tre versioni.

1. Un cono davanti al corpo (`IsInFront`): prendeva la palla se era "davanti e a portata".
2. Un punto fisso nel mondo davanti al personaggio, col corpo che gli si metteva di fianco.
3. **Ora**: una sfera sotto la testa della mazza, nella posa di mira. Il punto lo dà la
   mazza, non un numero. L'utente: *"è la club head che è importante che sia allineata con
   l'oggetto da colpire"*.

Del secondo passaggio è rimasto l'**ancoraggio**: entrando in mira il corpo si sposta in
modo che la testa della mazza cada davanti a dove guardava (*"il personaggio si sposta
sulla sinistra e la mazza diventa il protagonista"*). Con WASD si sposta a piccoli passi,
come sul green.

> Differenza dal piazzamento automatico scartato più sotto: l'ancoraggio **non insegue la
> palla**, si fissa una volta e lo muove solo il giocatore. E il corpo resta dinamico: si
> muove dando una velocità, quindi i muri lo fermano.

### Scartato: la palla presa se il suo centro cade nel cerchio (2026-10-12)

Il controllo guardava la distanza fra il centro della palla e il punto di colpo. Con una
palla grande (raggio circa 0,5) e `strikeRadius` 0,3, il centro restava sempre fuori anche
con la mazza appoggiata alla palla: *"non riesco ancora a colpire la palla anche se è sul
gizmo verde"*.

Ora conta il **contatto**: la palla è presa se il suo collider tocca la sfera di colpo
(`Collider.ClosestPoint`). Come per i nemici, che già usavano `OverlapSphere`.

### Scartato: il passo di lato a ogni rientro in mira (2026-10-12)

Tenendo premuto il destro dopo un tiro si rientra in mira, e a ogni rientro si fissava un
nuovo ancoraggio davanti al personaggio: il corpo rifaceva il passo di lato. L'utente:
*"vengo spostato anche se non mi muovo"*. Ora, se non si è camminato dal tiro precedente,
l'ancoraggio resta dove sta la mazza (`PlayerSwing.AnchorHere`).

### Scartato: lo swing come rotazione attorno a un asse fisso (2026-10-12)

Lo swing è passato per quattro versioni. Tutte cercavano la stessa cosa: la mazza che
parte bassa davanti, sale a destra caricando, ripassa davanti al colpo e sale a sinistra
nel follow-through.

1. **Rotazione attorno a un asse locale** (`swingAxis`, poi `golfSwingAxis`), centrata sul
   perno della mazza. Il verso dipendeva da come era orientata la mesh, e la mazza girava
   su se stessa invece di girare attorno al corpo.
2. **Pose disegnate a mano** (marker di backswing e follow-through, interpolati in linea
   retta). Funzionava ma chiedeva molto lavoro in editor. L'utente ha preferito un arco
   calcolato, in attesa di un'animazione vera.
3. **Arco attorno alle spalle su un asse fisso**, (0,0,1) o (0,0,−1). Il piano era
   verticale: la mazza *"passava sotto il corpo"* e *"andava da una rotazione a un'altra
   senza contare che in mezzo c'è il player"*.
4. **Ora**: arco su un piano **inclinato**, calcolato dalla posa di mira. Contiene la
   linea pivot → testa della mazza e la linea delle spalle. La testa sale e gira attorno
   alla capsula insieme (`Reference/swing arc.png`).

Due correzioni sopra la quarta versione, chieste dall'utente:

- **lo swing troppo largo**: il raggio era la distanza pivot → mazza, e in cima la mazza
  finiva lontana dal corpo. Ora salendo il raggio si stringe (`topRadius`), come braccia e
  polsi che si piegano;
- **il centro dietro il collo**: `swingPivot` sta dietro, non sulle spalle. *"Le spalle
  non rientrano su se stesse ma la mazza segue il punto come centro che si trova dietro il
  collo."* Non va "corretto" riportandolo a z = 0.

### Scartato: il follow-through sempre pieno e il rientro in linea retta (2026-10-12)

Il follow-through arrivava sempre al suo massimo, anche dopo un colpetto. Ora scala con la
carica.

Finito il colpo la mazza tornava a riposo in linea retta, e quasi di scatto: *"la mazza
rientra al punto di riposo attraversando il corpo"*. Ora ripercorre l'arco del
follow-through al contrario fino alla posa di mira, in `recoverTime` secondi, e solo dopo
va a riposo.

### Nota sulle parole: follow-through

Per un giro "follow-through" era stato tradotto con "finale". L'utente chiedeva solo cosa
volesse dire, non di tradurlo: *"finale mi suona strano"*. Si scrive **follow-through**: è
la parte dello swing dopo il colpo, quando la mazza prosegue oltre la palla.

### Scartato: il colpo che prende la palla da qualunque distanza

Quando il controllo di vicinanza è stato tolto dall'**ingresso** in modalità tiro, per un
attimo è rimasto tolto anche dal **colpo**: si poteva lanciare la testa stando a venti
metri di distanza.

Corretto dall'utente: la mazza colpisce solo quello che ha davanti. Se la testa non è a
portata il colpo va a vuoto; se davanti c'è un altro oggetto, colpisce quello.

Le due cose restano separate di proposito. **Entrare** in modalità tiro si può sempre,
perché lo swing serve anche a colpire altro. **Prendere** la testa no, serve averla
davanti.

La portata è una sola, `range` e `arc` su `PlayerSwing`: `SkullAimController` la legge
invece di avere numeri suoi, così non possono divergere.

### Scartato: il corpo che si piazza attorno alla palla

Il personaggio si metteva automaticamente dalla parte opposta al tiro e girava attorno alla
palla seguendo la mira, come si fa aggiustando la posizione prima di un colpo di golf. Per
farlo il Rigidbody diventava kinematic e veniva spostato a mano.

Due problemi, e il secondo è grave:

- da kinematic i muri non lo fermavano, quindi girando la mira in un corridoio stretto il
  personaggio passava attraverso le pareti;
- il piazzamento continuava **anche dopo il colpo**, perché era legato alla posizione della
  palla. Risultato: il personaggio veniva trascinato dietro la palla che volava. L'utente
  l'ha segnalato così: *"quando colpisco il player si sposta con la palla"*.

Ora il personaggio non viene mai spostato a mano. Resta dove ha colpito, e il Rigidbody
resta sempre dinamico.

### Scartato: entrare in mira solo con la palla davanti

C'era un controllo di distanza e un cono davanti al personaggio: il tasto destro funzionava
solo con la palla a portata. Serviva a evitare di prendere la mira dando le spalle alla
testa.

Scartato dall'utente: si entra in modalità tiro quando si vuole, perché lo swing potrà
servire a colpire **altri oggetti**, non solo la testa. Insieme al controllo sono spariti
il suggerimento sull'HUD e il gizmo del cono.

Risolto dopo: cosa colpisce lo swing lo decide la sfera sotto la testa della mazza, vedi
"il punto di colpo che cerca la palla" più sopra.

### Scartato: il volo libero come ripiego della camera

Quando è arrivato il personaggio in terza persona, il vecchio stato a volo libero
(WASD + Q/E, Spazio per mirare) era stato tenuto come ripiego per quando il campo `player`
è vuoto, così la scena restava giocabile.

Tolto su richiesta dell'utente: il personaggio è parte del controller, non un'opzione.
Finché la capsula non è in scena, la camera non si muove.

---

## 2. La fisica

### Scartato: la mazza con il collider (2026-10-12)

`Skeletal_arm` aveva un CapsuleCollider, e `club_head` una SphereCollider. La mazza è
figlia del Rigidbody del personaggio, quindi i suoi collider facevano parte del **corpo
fisico**. In posa di mira la punta della mazza va proprio dove sta la palla, e la spingeva
fuori dalla sfera di colpo prima del colpo.

Ora `PlayerSwing.Start()` spegne tutti i collider della mazza. Il colpo è **logico** (la
sfera di colpo), non fisico. Vale per qualunque pezzo si aggiunga alla mazza.

### Scartato: l'attrito di rotolamento dal physic material

PhysX **non modella l'attrito di rotolamento**. Una sfera su un piano scivola
all'infinito, qualunque Dynamic Friction e Static Friction le si metta. Non è un problema
di taratura: la frenata non esiste proprio.

Per questo `SkullRolling` la scrive a mano, applicando uno smorzamento alle sole componenti
orizzontali. Se un giorno sembra codice inutile che si potrebbe sostituire con un
materiale, la risposta è no.

### Scartato: pesare un urto con la velocità letta nel callback

`OnCollisionEnter` scatta **dopo** che il solver ha risolto l'urto. La velocità del
Rigidbody letta lì dentro è già quella di **rimbalzo**, non quella di arrivo. Usarla per
pesare un urto sull'angolo di impatto dà risultati sbagliati e difficili da capire.

La velocità di arrivo viene salvata a fine `FixedUpdate` (`lastVelocity` in `SkullRolling`
e `SkullIntegrity`) e letta da lì.

### Scartato: azzerare la velocità sotto soglia senza guardare il terreno

`SkullRolling` ferma del tutto il teschio quando scende sotto `stopThreshold`. Il controllo
guardava il **modulo** della velocità, e andava bene finché il teschio stava sempre a terra.

Col volo si è rotto: in cima a una parabola la velocità verticale passa per lo zero, quindi
su un tiro ripido il modulo scende sotto soglia e il teschio **si congelava a mezz'aria**,
poi cadeva a piombo come se avesse sbattuto contro il nulla.

Ora l'azzeramento vale solo quando il teschio tocca terra. Il contatto si riconosce dalla
**normale**: un muro ha normale orizzontale e non conta come pavimento, altrimenti
strisciare contro una parete in volo rimetterebbe in funzione l'attrito di rotolamento.

C'è anche un ramo per il Rigidbody addormentato: da addormentato non riceve più
`OnCollisionStay`, quindi senza quel controllo il teschio fermo risulterebbe "in volo" e
non si potrebbe più prendere la mira.

### Scartato: un Rigidbody dinamico spostato a mano

Un Rigidbody dinamico spostato scrivendogli la posizione combatte col solver: il risultato
è tremolio e compenetrazioni.

`EnemyPatrol` lo risolve diventando **kinematic** mentre segue il percorso, e tornando
dinamico alla morte, così l'esplosione lo spedisce via.

Il prezzo è che un corpo kinematic non viene fermato dai muri. Per i nemici in pattuglia va
bene, perché il percorso è fisso e lo decidi tu. Per il personaggio no: è uno dei motivi
per cui il piazzamento automatico sulla palla è stato tolto (vedi sopra). Il personaggio
resta sempre dinamico, e viene mosso solo scrivendo la velocità.

Nota sulla rotazione: `MoveRotation` è fatto per i corpi kinematic. Su un corpo dinamico
con la rotazione congelata, come il personaggio, la via buona è scrivere direttamente
`rb.rotation`.

---

## 3. I nemici

### Scartato: il controllo della corazza dentro `EnemyArmor`

`EnemyArmor` blocca i colpi che arrivano fuori dal suo cono o sotto una certa velocità, e un
colpo bloccato **non deve far esplodere** il teschio.

La cosa naturale sarebbe fare quel controllo in un `OnCollisionEnter` su `EnemyArmor`. Non
funziona: l'ordine con cui arrivano i due callback (quello dell'armatura e quello
dell'esplosione) è **casuale**, quindi a volte il teschio esplodeva prima che l'armatura
avesse potuto dire di no.

Il controllo lo fa `SkullExplosion` prima di esplodere, interrogando l'armatura. Un solo
punto, un solo ordine.

### Scartato: il nemico che sparisce quando muore

Un nemico che muore e si cancella è un'occasione persa. Il modello è la modalità Crash di
Burnout 3: le auto distrutte **restano in strada** e innescano la carambola.

Da qui `EnemyWreck`: al primo colpo il corpo esce dai binari e diventa un birillo che
rotola, ingombra, spinge e danneggia gli altri nemici. `deathBehaviour` ha ancora `Destroy`
e `Deactivate`, ma `Wreck` è la scelta di design.

### Scartato: il rottame che danneggia sempre

I rottami danneggiano chi urtano, ma **solo mentre sono liberi**. All'inizio danneggiavano
sempre, e il risultato era che negli ingorghi il traffico si autodistruggeva da solo, senza
che il giocatore facesse niente.

### Scartato: `AliveCount` calcolato scansionando i nemici

Lo leggono ogni frame il `GameManager` e l'HUD. Con una folla, scandire la lista a ogni
frame costa. È un contatore mantenuto, aggiornato quando qualcuno nasce o muore.

Per lo stesso motivo il registro `All` si toglie un elemento scambiandolo con l'ultimo
(tempo costante) invece di cercarlo e compattare la lista.

### Scartato: un materiale istanziato per nemico per il flash rosso

Cambiare il colore di un materiale lo istanzia, e con una folla significa centinaia di
materiali. Il flash usa un `MaterialPropertyBlock` dentro una coroutine.

Conseguenza voluta: **non c'è nessun `Update` sui nemici**. Se ne aggiungi uno, il costo
torna a crescere con il numero di nemici.

### Scartato: il traffico che rallenta per il teschio

I nemici nelle corsie rallentano per chi hanno davanti nella loro corsia, ma **non** per il
teschio. È voluto: deve essere il giocatore a leggere il traffico, non il traffico a
schivare il giocatore.

---

## 4. Livello, vittoria, punteggio

### Scartato: la buca che chiama `Win()`

`WinHole` non dichiara la vittoria: imposta `IsPotted` e lascia decidere al `GameManager`.
Così c'è un solo posto in cui si stabilisce se un livello è vinto, e un solo posto da
guardare quando non lo è.

### Scartato: legare l'apertura della buca all'esplosione

La buca si apre **solo** colpendo gli `HoleOpener`, e non ha niente a che vedere con
l'uccidere nemici. È quello che rende possibile la **pacifist run**: imbucare la testa
vince il livello anche a nemici tutti vivi. Cambia il punteggio finale, non l'esito.

### Scartato: contare le uccisioni come `EnemiesAtStart - AliveCount`

Funziona solo in un livello chiuso. Con uno spawner attivo i nemici continuano ad arrivare,
quindi quella differenza non dice niente. `Kills` è un contatore **cumulativo** sul
`GameManager`.

Conseguenza pratica: con uno spawner si usa `requiredKills` e si spegne
`requireAllEnemiesDead`, che non si soddisferebbe mai.

### Scelto: `hasObjectives` calcolato all'avvio

Senza quel controllo, un livello in cui nessun obiettivo è configurato si vincerebbe da
solo al primo frame, perché una congiunzione di zero condizioni è vera.

---

## 5. HUD ed effetti

### Scartato: HUD agganciato a `onWin` / `onLose`

L'HUD legge `GameManager.State` a ogni frame. Costa un confronto e risolve due problemi:
non c'è niente da ricollegare nell'Inspector, e l'HUD funziona anche se lo aggiungi a
livello già avviato. Gli `UnityEvent` restano liberi per altro.

### Scartato: particelle con texture e trasparenze in URP

Il primo VFX di esplosione usava particelle texturizzate. In URP, con un materiale
sbagliato, la via più breve è un effetto **invisibile oppure rosa**.

Il generatore (`GOOMD > Crea VFX esplosione`) fa particelle **mesh**: nessuna texture,
nessuna trasparenza, niente da sbagliare.

---

## 6. Progetto

### Scartato (per ora): Git LFS

`Library/` pesa 2 GB ma è ignorata, e Unity la rigenera. Gli asset veri pesano **0,3 MB**.
LFS aggiungerebbe complicazioni senza risolvere un problema che non c'è. Da riconsiderare
se entrano mesh o texture pesanti.

### Rinviato: pooling dei nemici nello spawner

Non è scartato, è rinviato: serve solo quando la cadenza di comparsa salirà molto. Vedi
[[Taratura]], cap. 13-bis, con la trappola degli ascoltatori a runtime da rimuovere a ogni
riuso.
