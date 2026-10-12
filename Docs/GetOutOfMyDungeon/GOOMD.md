---
aliases:
  - Get Out Of My Dungeon
  - Home
tags:
  - goomd
---

# Get Out Of My Dungeon

Action puzzle in terza persona. Controlli uno scheletro senza testa: la sua testa è la
pallina, e la colpisci con una mazza da golf scegliendo direzione, angolo e potenza. La
testa esplode al contatto con i nemici.

Il modello per le collisioni è la **modalità Crash di Burnout 3**: lo schianto è
l'obiettivo, non un fallimento.

## I due documenti

| Nota | A cosa risponde |
|---|---|
| [[Taratura]] | **Come** sono tarate le cose: ogni parametro esposto negli script, cosa fa, il valore attuale in scena, i legami fra valori e i conti di bilanciamento. |
| [[Storico]] | **Perché** sono fatte così: le scelte provate e scartate, con il motivo. Da leggere prima di "correggere" qualcosa che sembra strano. |

Il contesto per lavorare al progetto (convenzioni, trappole, prossimi passi) sta in
`CLAUDE.md`, nella radice del repository. Resta fuori dal vault perché è il file che
Claude Code legge automaticamente.

## Comandi

| Tasto | Cosa fa |
|---|---|
| WASD | si muove. In modalità tiro: piccoli passi per allineare la mazza alla palla |
| Shift | corre |
| Alt | si abbassa e rallenta |
| Spazio | salta |
| Ctrl | tuffo, a terra o in aria. È la schivata: atterrati, ci si rialza piano |
| mouse | gira la camera, e la camera dà la direzione del tiro |
| tasto destro | **tenuto**: modalità tiro. Lasciato: si esce |
| tasto sinistro | colpo di mazza. In modalità tiro: tieni premuto per caricare, rilascia per tirare |
| rotella | angolo del tiro, solo in modalità tiro |

## Il tiro, in breve

- Entrando in mira il personaggio si mette **di fianco**, come un golfista, e la mazza va
  davanti alla sua faccia con la testa a terra. La camera si avvicina.
- La mira **parte da dove guarda il personaggio** e si riallinea a dove guarda la camera.
- L'**angolo** è quanto in alto vuoi tirare la palla: zero è raso terra, il massimo è un
  campanile, metà è teso in avanti. Un giro di rotella lo cambia del 25%.
- La **potenza** si carica tenendo premuto. Più carichi, più la mazza sale dietro il corpo.
  La barra è verticale, a sinistra, con la percentuale.
- Sotto la testa della mazza c'è una **sfera di colpo**: tutto quello che la tocca viene
  colpito, palla e nemici insieme. Se non tocca niente, la mazza mulina a vuoto.
- I **quadratini** mostrano la traiettoria: dalla palla e dai nemici nella sfera, o dalla
  sfera stessa se è vuota.
- Dopo il colpo il personaggio è subito libero: non si aspetta che la palla si fermi.

## I due pezzi del giocatore

Il giocatore è fatto di due cose che si allontanano: il **corpo** (la capsula) e la
**testa** (la pallina). La camera principale resta sempre dietro al corpo.

Per vedere il tiro c'è una seconda camera, mostrata in una finestrella nell'HUD, come la
telecronaca di una partita di golf. Si apre quando un colpo prende la testa e resta aperta
finché il corpo non arriva vicino alla testa. Serve anche a ritrovarla.

## Dove sono le cose

### Taratura, per argomento

- **cap. 2** — il tiro: direzione, angolo, potenza, cosa prende il colpo, traiettoria
- **cap. 2-bis** — il personaggio: movimento, corsa, crouch, postura in mira, salto, tuffo
- **cap. 2-ter** — la mazza: pose, arco dello swing, sfera di colpo, colpo a mano
- **cap. 3** — la camera: terza persona a offset fisso, più vicina in mira
- **cap. 3-bis** — la telecronaca: la finestra che segue il tiro nell'HUD
- **cap. 4** — il rotolamento: attrito, frenata, volo
- **cap. 5** — l'integrità del teschio, e i conti di gittata e altezza
- **cap. 6** — l'esplosione
- **cap. 7 e 7-bis** — i nemici e i loro archetipi
- **cap. 8** — vittoria e sconfitta
- **cap. 9 e 10** — la buca e gli apri-buca
- **cap. 11 e 12** — zone di morte e puzzle
- **cap. 12-bis** — l'HUD
- **cap. 13** — la fisica non-script: Rigidbody, physic material, atterraggio
- **cap. 13-bis** — prestazioni

### Da consultare quando serve

- **cap. 14, Ricettario** — "voglio ottenere X, quale numero muovo"
- **cap. 15, Legami da non rompere** — coppie di valori che devono restare coerenti
- **cap. 16, Cose ancora non tarabili** — i buchi noti

Tutti dentro [[Taratura]]: aprila e usa il pannello Struttura di Obsidian per saltare al
capitolo.

### Immagini di riferimento

La cartella `Reference/`, nella radice del repository, raccoglie gli screen usati per
spiegare la mira e lo swing: frame di Super Battle Golf (`0%`, `50%`, `100%`, `start mira`,
`camerra position + aim`) e screen della scena con i disegni sopra (`swing arc`,
`How it should be`, `come dovrebbe essere`).

## Regola d'oro della taratura

I numeri scritti nel codice C# sono solo i **default di creazione**. Quando aggiungi un
componente a un GameObject, Unity copia quei valori nella scena, e da lì in poi comanda la
scena. Cambiare il default in un `.cs` **non** cambia gli oggetti già in scena.
