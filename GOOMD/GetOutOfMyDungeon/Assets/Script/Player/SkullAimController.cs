using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

// Il tiro, modello golf: direzione + angolo in altezza + potenza.
//
// Sequenza di input:
//   Idle     -> PlayerController entra in mira finche' si TIENE premuto il tasto destro
//   Aiming   -> la direzione parte da dove guarda il personaggio e si riallinea a
//               dove guarda la camera.
//               Premendo il tasto sinistro l'angolo e' confermato e parte la carica.
//   Powering -> piu' si tiene premuto, piu' si carica, fino al 110%. Si tira
//               RILASCIANDO, e si torna subito Idle. Al 100% e' il colpo perfetto,
//               oltre la palla va a cercare un nemico (SkullHoming).
//
// Dopo il tiro non si aspetta che la palla si fermi: il personaggio torna libero
// appena finisce lo swing. Prima c'era uno stato Shot che lo bloccava finche' la
// palla rotolava. Vedi lo Storico.
//
// L'ANGOLO si sceglie in ogni stato tranne Powering: ROTELLA, oppure gli angoli
// fissi sul tastierino numerico (loftPresets).
//
// Lasciare il tasto destro annulla, vedi CancelAim().
//
// La mira non dipende dalla palla. Entrando in mira si fissa un punto di colpo
// davanti al personaggio (PlayerSwing.BeginStance), e il corpo gli si mette di
// fianco. La palla viene presa solo se al rilascio sta su quel punto: entrando in
// mira nel posto sbagliato la si manca anche standole vicino.
//
// La direzione del tiro arriva dalla camera (cameraTransform.forward appiattito).
// Prima la camera si posizionava in base alla direzione di tiro, e un raycast dalla
// camera si auto-alimentava. Ora il verso e' uno solo: il mouse gira la camera, la
// camera da' la direzione. Vedi lo Storico.
//
// Questo script non muove la mazza. La mazza sta in mano al personaggio e la anima
// PlayerSwing, che fa il caricamento e il colpo.
public class SkullAimController : MonoBehaviour
{
    public enum State { Idle, Aiming, Powering }

    // dove si e' fermata la carica: lo leggono HUD, PlayerSwing e punteggio
    //   Weak       carica sotto weakTolerance: colpetto a potenza fissa
    //   Normal     potenza proporzionale alla carica
    //   Perfect    100% +- perfectTolerance: potenza in piu', nemici giu' piu' a lungo
    //   Overcharge oltre: la palla diventa a ricerca (SkullHoming), nemici giu' piu' a lungo
    public enum PowerZone { Normal, Weak, Perfect, Overcharge }

    [Header("Riferimenti")]
    public Transform ball;                  // la testa-palla
    public Rigidbody ballRb;
    public Transform cameraTransform;       // da' la direzione del tiro. Vuoto = Camera.main
    public PlayerSwing swing;               // da lui arrivano la portata della mazza e lo swing
    public SkullExplosion skullExplosion;   // se vuoto lo prendo dalla palla
    public SkullHoming homing;              // se vuoto lo prendo dalla palla, o lo aggiungo

    [Header("Direzione")]
    // All'ingresso in mira la direzione e' quella del corpo, poi ruota verso la
    // camera a questa velocita'. 0 = salta subito sulla camera.
    public float aimAlignSpeed = 360f;      // gradi al secondo

    // L'angolo si sceglie sempre, anche camminando: si arriva in mira con l'angolo
    // gia' pronto. Si blocca solo durante la carica, dove e' confermato.
    [Header("Angolo di tiro (rotella e tastierino)")]
    public float minLoftAngle = 0f;         // 0 = tiro raso
    public float maxLoftAngle = 55f;        // il massimo, in gradi
    [Range(0f, 1f)] public float startLoftRatio = 0f;        // angolo a inizio livello
    // Angoli fissi sui tasti numerici (riga in alto e tastierino): il primo va sul
    // tasto 1, il secondo sul 2 e cosi' via, fino a 9. In frazione del ventaglio:
    // 0 = minLoftAngle, 1 = maxLoftAngle.
    public float[] loftPresets = { 0f, 0.25f, 0.45f, 0.7f, 1f };
    [Range(0f, 1f)] public float loftPerRevolution = 0.25f;  // quanto cambia in un giro di rotella
    public float notchesPerRevolution = 24f; // scatti in un giro di rotella (mouse comuni: 24)
    public float loftSmooth = 14f;           // 0 = l'angolo salta di scatto
    public bool showAimGizmo = true;

    // La carica va da 0 al 100% in chargeTime secondi, poi continua fino a
    // 100% + overchargeMax nello stesso ritmo, e li' si ferma. Il colpo perfetto e'
    // al 100%: andare oltre non e' "di piu'", e' un altro colpo (la palla a ricerca).
    // Per questo e' una prova di tempismo: tenendo premuto si supera il perfetto.
    [Header("Potenza (si carica tenendo premuto)")]
    public float maxPower = 20f;            // impulso al 100% della carica
    public float chargeTime = 1.2f;         // secondi per arrivare al 100%
    [Range(0f, 1f)] public float overchargeMax = 0.1f;  // 0.1 = la carica arriva al 110%, in 1.1 x chargeTime
    public float forceMultiplier = 1f;

    [Header("Zone della carica")]
    [FormerlySerializedAs("zoneWidth")]
    [Range(0f, 0.9f)] public float weakTolerance = 0.07f;   // carica fino a qui = colpo debole. 0 = spento
    [FormerlySerializedAs("bottomZonePower")]
    [Range(0f, 1f)] public float weakPower = 0.12f;         // potenza fissa del colpo debole
    [FormerlySerializedAs("bottomZoneMultiplier")]
    public float weakMultiplier = 1f;
    [Range(0f, 0.5f)] public float perfectTolerance = 0.03f; // 100% +- questo = colpo perfetto
    [FormerlySerializedAs("topZoneMultiplier")]
    public float perfectMultiplier = 1.3f;                   // boost di potenza del colpo perfetto
    // potenza oltre il 100%: la palla piu' veloce del gioco, a ricerca o no.
    // Tienilo sopra perfectMultiplier. Piu' e' alto, piu' la curva a ricerca e' piatta
    public float overchargeMultiplier = 1.6f;


    [Header("Anteprima della traiettoria (tratteggio)")]
    // Una fila di quadratini lungo la parabola, sempre girati verso la camera.
    // Partono da ogni cosa che il colpo fara' volare (la palla, i nemici nel
    // cerchio), oppure dal cerchio se e' vuoto. Si vedono sempre in mira.
    public bool showTrajectory = true;
    public GameObject dotPrefab;            // vuoto = un quadrato creato da codice
    public Material dotMaterial;            // solo senza prefab. Vuoto = materiale di default
    public float dotSpacing = 0.4f;         // metri fra un quadratino e l'altro, lungo la curva
    public float dotSize = 0.12f;           // lato del quadratino, in metri
    public int maxDots = 60;                // in tutto, divisi fra le traiettorie
    public int trajectorySteps = 80;
    public float trajectoryStep = 0.05f;    // secondi simulati per segmento
    public LayerMask trajectoryMask = ~0;

    public State CurrentState { get; private set; } = State.Idle;

    // direzione orizzontale del tiro, presa dalla camera
    public Vector3 ShotDirection => shotDirection;

    // direzione vera del colpo, angolo compreso: e' questa che va al Rigidbody
    public Vector3 LaunchDirection => LaunchDirectionAt(LoftAngle, shotDirection);

    public float LoftAngle => Mathf.Lerp(minLoftAngle, maxLoftAngle, loftRatio);
    public float LoftRatio => loftRatio;

    // la carica attuale: 1 = 100%, puo' arrivare a 1 + overchargeMax
    public float ChargeRatio => chargeRatio;
    public float MaxChargeRatio => 1f + overchargeMax;
    public PowerZone CurrentZone => ZoneOf(chargeRatio);

    public PowerZone LastShotZone { get; private set; } = PowerZone.Normal;
    public float LastShotImpulse { get; private set; }
    public bool LastShotHoming { get; private set; }   // la palla e' partita a ricerca

    // Vero se l'ultimo colpo ha davvero preso la testa. Se la mazza ha mulinato a
    // vuoto e' falso, e la testa non si e' mossa. Lo legge ShotCamera per decidere
    // se aprire la finestra della telecronaca.
    public bool LastShotHitBall { get; private set; }

    // Quante volte un colpo ha preso la testa da inizio partita. ShotCamera lo
    // confronta con l'ultimo valore visto: se e' cresciuto, e' partito un tiro.
    // E' un contatore e non un evento, cosi' chi lo legge non deve agganciarsi.
    public int BallHits { get; private set; }

    private Vector3 shotDirection = Vector3.forward;
    private float chargeRatio;
    private float chargeTimer;
    private float loftRatio;      // angolo attuale, smussato
    private float targetLoft;     // angolo scelto con la rotella
    private Vector3[] trajBuffer;
    private Transform[] dots;
    private int dotsShown;
    private float ballRadius = 0.15f;
    private Collider ballCollider;
    private readonly System.Collections.Generic.List<Rigidbody> flyingTargets
        = new System.Collections.Generic.List<Rigidbody>();

    // stesso indice = stesso angolo: la riga in alto e il tastierino fanno uguale
    private static readonly Key[] PresetKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
        Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
    };
    private static readonly Key[] PresetNumpadKeys =
    {
        Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5,
        Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9
    };

    void Start()
    {
        targetLoft = loftRatio = Mathf.Clamp01(startLoftRatio);

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (swing == null) swing = FindFirstObjectByType<PlayerSwing>();

        if (ball != null && skullExplosion == null)
            skullExplosion = ball.GetComponent<SkullExplosion>();

        // la palla a ricerca: se la testa non ha il componente lo aggiungo coi
        // valori di default. Mettilo su Skull_player per tararlo
        if (ball != null && homing == null)
        {
            homing = ball.GetComponent<SkullHoming>();
            if (homing == null) homing = ball.gameObject.AddComponent<SkullHoming>();
        }

        // il collider serve a sapere se la mazza tocca la palla; il raggio a far
        // partire la traiettoria a vuoto all'altezza giusta
        ballCollider = ball != null ? ball.GetComponent<Collider>() : null;
        if (ballCollider != null) ballRadius = ballCollider.bounds.extents.y;

        BuildDots();
        HideTrajectory();
    }

    // I quadratini si creano una volta sola e si riusano: crearli a ogni frame
    // vorrebbe dire decine di oggetti nuovi al secondo.
    void BuildDots()
    {
        if (!showTrajectory || maxDots <= 0) return;

        Transform parent = new GameObject("Trajectory_Dots").transform;
        dots = new Transform[maxDots];

        for (int i = 0; i < maxDots; i++)
        {
            GameObject dot;

            if (dotPrefab != null)
            {
                dot = Instantiate(dotPrefab, parent);
            }
            else
            {
                dot = GameObject.CreatePrimitive(PrimitiveType.Quad);
                dot.transform.SetParent(parent, false);

                // il collider del Quad va tolto subito: Destroy aspetterebbe fine
                // frame, e intanto il Linecast della parabola lo prenderebbe
                DestroyImmediate(dot.GetComponent<Collider>());

                Renderer r = dot.GetComponent<Renderer>();
                if (dotMaterial != null) r.sharedMaterial = dotMaterial;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }

            dot.name = "Dot";
            dot.layer = 2; // Ignore Raycast: i quadratini non devono mai fare da ostacolo
            dot.transform.localScale = Vector3.one * dotSize;
            dot.SetActive(false);
            dots[i] = dot.transform;
        }
    }

    void OnValidate()
    {
        // oltre 89 gradi la direzione di lancio diventa parallela a Vector3.up
        // e LookRotation sulla freccia degenera
        maxLoftAngle = Mathf.Clamp(maxLoftAngle, 0f, 89f);
        minLoftAngle = Mathf.Clamp(minLoftAngle, 0f, maxLoftAngle);
        notchesPerRevolution = Mathf.Max(1f, notchesPerRevolution);
        chargeTime = Mathf.Max(0.05f, chargeTime);
        // il perfetto non puo' mangiarsi tutta la zona oltre il 100%, e il debole
        // non puo' arrivare al perfetto
        perfectTolerance = Mathf.Clamp(perfectTolerance, 0f, overchargeMax);
        weakTolerance = Mathf.Clamp(weakTolerance, 0f, Mathf.Max(0f, 1f - perfectTolerance - 0.01f));
        trajectorySteps = Mathf.Max(2, trajectorySteps);
        trajectoryStep = Mathf.Max(0.005f, trajectoryStep);
    }

    // Vero se la testa tocca la sfera colpita dalla mazza. Se e' falso il tiro va a
    // vuoto: e' il giocatore a decidere cosa colpire, la mira si prende comunque.
    // Il punto vive su PlayerSwing, cosi' non ci sono due numeri che divergono.
    // Senza PlayerSwing in scena non c'e' nessun controllo.
    public bool BallInReach => swing == null
        || (ballCollider != null && swing.IsInStrikeZone(ballCollider));

    // Si entra in mira sempre, anche con la palla che rotola o lontana. Rifiuta solo
    // a livello finito o se si sta gia' mirando.
    // "facing" e' dove guarda il personaggio: la mira parte da li', e li' davanti si
    // fissa il punto di colpo. Poi UpdateDirection porta la mira verso la camera.
    public bool BeginAim(Vector3 facing)
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return false;
        if (CurrentState != State.Idle) return false;

        CurrentState = State.Aiming;

        facing.y = 0f;
        if (facing.sqrMagnitude > 0.0001f) shotDirection = facing.normalized;

        if (swing != null) swing.BeginStance(shotDirection);

        // l'angolo NON riparte dal default: il giocatore lo sceglie anche fuori
        // dalla mira, e deve trovarlo dove l'ha lasciato

        UpdateDirection();
        UpdateTrajectory(1f);
        return true;
    }

    // Esce dalla mira senza tirare. La chiama PlayerController col tasto destro:
    // senza una via d'uscita, entrare in modalita' tiro per sbaglio sarebbe una
    // trappola, perche' l'unico modo di tornare a camminare sarebbe tirare.
    public void CancelAim()
    {
        if (CurrentState != State.Aiming && CurrentState != State.Powering) return;

        CurrentState = State.Idle;
        chargeRatio = 0f;
        chargeTimer = 0f;

        if (swing != null) swing.EndStance();
        HideTrajectory();
    }

    void Update()
    {
        // l'angolo si sceglie sempre, tranne in carica, dove e' gia' confermato
        bool playing = GameManager.Instance == null || GameManager.Instance.IsPlaying;
        if (playing && CurrentState != State.Powering)
        {
            ReadLoftScroll();
            ReadLoftPresets();
            UpdateLoftSmoothing();
        }

        switch (CurrentState)
        {
            case State.Idle:
                break;

            case State.Aiming:
                UpdateDirection();
                // in mira l'anteprima mostra il tiro a potenza piena: dice fin dove
                // si arriverebbe con quell'angolo dando tutto
                UpdateTrajectory(1f);

                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    // confermare vuol dire confermare l'angolo scelto: senza questo
                    // scatto, chi premesse subito dopo aver girato la rotella
                    // tirerebbe con l'angolo ancora a meta' della smussatura
                    loftRatio = targetLoft;

                    CurrentState = State.Powering;
                    chargeTimer = 0f;
                    chargeRatio = 0f;
                }
                break;

            case State.Powering:
                // la direzione resta viva: la camera gira col mouse e il tiro la segue,
                // cosi' quello che vedi e' quello che tiri. L'angolo invece e' fermo:
                // per cambiarlo si lascia la mira
                UpdateDirection();
                UpdateCharge();
                UpdateTrajectory(chargeRatio);

                if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    Shoot();
                    CurrentState = State.Idle;
                    // dopo Shoot: lo swing a vuoto ha ancora bisogno del punto
                    if (swing != null) swing.EndStance();
                    HideTrajectory();
                }
                break;
        }
    }

    // La direzione del tiro va verso quella in cui guarda la camera, appiattita sul
    // piano, a aimAlignSpeed gradi al secondo.
    void UpdateDirection()
    {
        Vector3 f = cameraTransform != null ? cameraTransform.forward : transform.forward;
        f.y = 0f;

        if (f.sqrMagnitude < 0.0001f) return;   // camera a picco: tengo l'ultima buona

        f.Normalize();

        shotDirection = aimAlignSpeed > 0f
            ? Vector3.RotateTowards(shotDirection, f, aimAlignSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f)
            : f;

        shotDirection.y = 0f;
        shotDirection = shotDirection.sqrMagnitude < 0.0001f ? f : shotDirection.normalized;
    }

    // Un giro completo di rotella cambia l'angolo di loftPerRevolution.
    void ReadLoftScroll()
    {
        if (Mouse.current == null) return;

        float raw = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(raw) < 0.01f) return;

        // L'Input System riporta lo scroll in unita' di piattaforma, e non sono le
        // stesse ovunque: su Windows uno scatto vale 120, altrove vale 1. Invece di
        // fidarsi di un numero fisso, se il valore e' grande lo converto in scatti,
        // se e' piccolo e' gia' in scatti.
        float notches = Mathf.Abs(raw) >= 10f ? raw / 120f : raw;

        float perNotch = loftPerRevolution / Mathf.Max(1f, notchesPerRevolution);
        targetLoft = Mathf.Clamp01(targetLoft + notches * perNotch);
    }

    // Tasti numerici, riga in alto o tastierino: 1 = primo angolo fisso, 2 = secondo, ecc.
    // Passa per la smussatura come la rotella, cosi' la barra scorre e non scatta.
    void ReadLoftPresets()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || loftPresets == null) return;

        int n = Mathf.Min(loftPresets.Length, PresetKeys.Length);
        for (int i = 0; i < n; i++)
        {
            if (!kb[PresetKeys[i]].wasPressedThisFrame && !kb[PresetNumpadKeys[i]].wasPressedThisFrame)
                continue;

            targetLoft = Mathf.Clamp01(loftPresets[i]);
            return;
        }
    }

    void UpdateLoftSmoothing()
    {
        loftRatio = loftSmooth > 0f
            ? Mathf.Lerp(loftRatio, targetLoft, Time.deltaTime * loftSmooth)
            : targetLoft;
    }

    // La carica sale allo stesso ritmo fino al 100% e oltre, e si ferma al massimo
    // (100% + overchargeMax).
    void UpdateCharge()
    {
        chargeTimer += Time.deltaTime;
        chargeRatio = Mathf.Min(chargeTimer / Mathf.Max(0.05f, chargeTime), MaxChargeRatio);
    }

    void Shoot()
    {
        float charge = chargeRatio;
        float impulse = ResolveImpulse(charge, out PowerZone zone);

        // La mazza colpisce solo quello che ha davanti. Se la testa non e' a
        // portata il colpo va a vuoto: la mazza mulina e la testa non si muove.
        bool hitsBall = BallInReach;

        // oltre il 100% la palla va a cercare il primo nemico davanti. Se non ce
        // n'e' nessuno parte dritta con l'angolo scelto, alla stessa potenza
        EnemyHealth target = hitsBall && zone == PowerZone.Overcharge && homing != null
            ? homing.FindTarget(ball.position, shotDirection)
            : null;

        LastShotZone = zone;
        LastShotHitBall = hitsBall;
        LastShotImpulse = hitsBall ? impulse : 0f;
        LastShotHoming = target != null;

        // lo swing si vede sempre, anche a vuoto. Tutto quello che sta nel cerchio
        // viene colpito insieme alla palla e spinto lungo il tiro: nemici, e in
        // futuro gli oggetti di scena
        if (swing != null) swing.OnShotReleased(charge, LaunchDirection, zone);

        if (!hitsBall) return;

        BallHits++;

        // riarmo il teschio: senza questo esplode una volta sola per partita
        if (skullExplosion != null)
            skullExplosion.ResetExplosion();

        // un tiro nuovo spegne la ricerca del tiro prima
        if (homing != null) homing.Stop();

        // la velocita' vecchia va buttata: sommare l'impulso a una palla che si
        // muoveva ancora darebbe tiri imprevedibili
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

        if (target != null) homing.Launch(target, impulse / Mathf.Max(0.0001f, ballRb.mass));
        else ballRb.AddForce(LaunchDirection * impulse, ForceMode.Impulse);
    }

    // In che zona cade una carica (1 = 100%).
    public PowerZone ZoneOf(float charge)
    {
        if (charge > 1f + perfectTolerance) return PowerZone.Overcharge;
        if (charge >= 1f - perfectTolerance) return PowerZone.Perfect;
        if (weakTolerance > 0f && charge <= weakTolerance) return PowerZone.Weak;
        return PowerZone.Normal;
    }

    // Da "quanto ho caricato" (1 = 100%) all'impulso da dare al Rigidbody.
    // Nella zona normale e' una proporzione diretta. Oltre il 100% e' la palla piu'
    // veloce del gioco (overchargeMultiplier), e se c'e' un nemico davanti lo cerca.
    public float ResolveImpulse(float charge, out PowerZone zone)
    {
        zone = ZoneOf(charge);

        float shaped;
        switch (zone)
        {
            case PowerZone.Weak:       shaped = weakPower * weakMultiplier; break;
            case PowerZone.Perfect:    shaped = perfectMultiplier; break;
            case PowerZone.Overcharge: shaped = overchargeMultiplier; break;
            default:                   shaped = Mathf.Clamp01(charge); break;
        }

        return maxPower * Mathf.Max(0f, shaped) * forceMultiplier;
    }

    // Ruota una direzione orizzontale verso l'alto dell'angolo dato.
    // Scritta con seno e coseno invece di Quaternion.AngleAxis su un asse calcolato:
    // niente ambiguita' di segno da sbagliare.
    public Vector3 LaunchDirectionAt(float loftDegrees, Vector3 horizontalDir)
    {
        Vector3 flat = new Vector3(horizontalDir.x, 0f, horizontalDir.z);
        if (flat.sqrMagnitude < 0.0001f) return Vector3.zero;
        flat.Normalize();

        float rad = loftDegrees * Mathf.Deg2Rad;
        return flat * Mathf.Cos(rad) + Vector3.up * Mathf.Sin(rad);
    }

    // --- Anteprima della traiettoria -------------------------------------

    // Una traiettoria per ogni cosa che il colpo fara' volare:
    //   - la palla, se e' nel cerchio;
    //   - ogni nemico nel cerchio che la spinta puo' muovere, con la SUA spinta
    //     (PlayerSwing.PushImpulse), che non e' quella della palla;
    //   - se il cerchio e' vuoto, una sola, dal cerchio: il tiro che partirebbe se
    //     la palla fosse li'. Come in Super Battle Golf (Reference/start mira.png).
    // I quadratini si dividono fra le traiettorie, cosi' la prima non li prende tutti.
    void UpdateTrajectory(float chargeRatio)
    {
        if (dots == null || ballRb == null) return;

        Vector3 dir = LaunchDirection;
        if (dir.sqrMagnitude < 0.001f)
        {
            HideTrajectory();
            return;
        }

        if (trajBuffer == null || trajBuffer.Length != trajectorySteps)
            trajBuffer = new Vector3[Mathf.Max(2, trajectorySteps)];

        Vector3 ballV0 = dir * ResolveImpulse(chargeRatio, out _) / Mathf.Max(0.0001f, ballRb.mass);

        bool ballIn = ball != null && swing != null && BallInReach;
        if (swing != null) swing.CollectFlyingTargets(flyingTargets);
        else flyingTargets.Clear();

        int paths = (ballIn ? 1 : 0) + flyingTargets.Count;
        int perPath = Mathf.Max(1, dots.Length / Mathf.Max(1, paths));
        int used = 0;

        if (paths == 0)
        {
            // il cerchio vuoto: alto quanto il raggio della palla, cioe' dove starebbe
            // il suo centro appoggiata a terra
            Vector3 origin = swing != null && swing.HasStrikePoint
                ? swing.StrikePoint + Vector3.up * ballRadius
                : (ball != null ? ball.position : transform.position);
            used = DrawPath(origin, ballV0, used, dots.Length);
        }
        else
        {
            if (ballIn)
            {
                // oltre il 100% la palla non segue l'angolo: mostro la curva che
                // farebbe verso il nemico agganciato
                Vector3 v0 = ballV0;
                if (ZoneOf(chargeRatio) == PowerZone.Overcharge && CurrentState == State.Powering
                    && homing != null)
                {
                    EnemyHealth target = homing.FindTarget(ball.position, shotDirection);
                    if (target != null) v0 = homing.LaunchVelocity(ball.position, target, ballV0.magnitude, out _);
                }

                used = DrawPath(ball.position, v0, used, perPath);
            }

            Vector3 push = swing.PushImpulse(dir);
            for (int i = 0; i < flyingTargets.Count; i++)
            {
                Rigidbody target = flyingTargets[i];
                used = DrawPath(target.worldCenterOfMass, push / Mathf.Max(0.0001f, target.mass), used, perPath);
            }
        }

        for (int i = used; i < dotsShown; i++)
            dots[i].gameObject.SetActive(false);

        dotsShown = used;
    }

    // Simula una parabola e ci posa sopra al massimo "budget" quadratini, a partire
    // dal quadratino numero "from". Ritorna quanti ne sono usati in tutto.
    int DrawPath(Vector3 origin, Vector3 v0, int from, int budget)
    {
        int count = BuildTrajectory(origin, v0, trajBuffer, trajectoryStep);
        int limit = Mathf.Min(dots.Length, from + budget);
        return PlaceDots(count, from, limit);
    }

    // Cammina lungo la spezzata della parabola e posa un quadratino ogni
    // dotSpacing metri. Spaziati per lunghezza e non per tempo: per tempo si
    // ammasserebbero in cima alla parabola, dove la palla rallenta.
    int PlaceDots(int count, int from, int limit)
    {
        Quaternion facing = cameraTransform != null ? cameraTransform.rotation : Quaternion.identity;
        float spacing = Mathf.Max(0.05f, dotSpacing);

        int placed = from;
        // il primo quadratino un passo dopo l'oggetto, non sopra di lui
        float nextAt = spacing;
        float walked = 0f;

        for (int i = 1; i < count && placed < limit; i++)
        {
            Vector3 a = trajBuffer[i - 1];
            Vector3 b = trajBuffer[i];
            float segment = Vector3.Distance(a, b);

            while (segment > 0.0001f && walked + segment >= nextAt && placed < limit)
            {
                Vector3 p = Vector3.Lerp(a, b, (nextAt - walked) / segment);

                Transform dot = dots[placed];
                dot.position = p;
                dot.rotation = facing;
                if (!dot.gameObject.activeSelf) dot.gameObject.SetActive(true);

                placed++;
                nextAt += spacing;
            }

            walked += segment;
        }

        return placed;
    }

    void HideTrajectory()
    {
        if (dots == null) return;

        for (int i = 0; i < dotsShown; i++)
            dots[i].gameObject.SetActive(false);

        dotsShown = 0;
    }

    // Integra la parabola a passi e si ferma dove incontra qualcosa, cosi' si vede
    // il punto di atterraggio invece di una curva che passa sotto terra.
    int BuildTrajectory(Vector3 origin, Vector3 v0, Vector3[] buffer, float step)
    {
        Vector3 g = Physics.gravity;
        Vector3 p = origin;
        Vector3 v = v0;

        buffer[0] = origin;
        int n = 1;

        while (n < buffer.Length)
        {
            Vector3 next = p + v * step + 0.5f * g * step * step;
            v += g * step;

            if (Physics.Linecast(p, next, out RaycastHit hit, trajectoryMask, QueryTriggerInteraction.Ignore))
            {
                buffer[n] = hit.point;
                return n + 1;
            }

            buffer[n] = next;
            p = next;
            n++;
        }

        return n;
    }

    // --- Gizmo: il ventaglio degli angoli --------------------------------

    void OnDrawGizmos()
    {
        if (!showAimGizmo || ball == null) return;

        Vector3 dir = shotDirection;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        dir.Normalize();

        Vector3 origin = ball.position;
        const float rayLength = 3f;

        Gizmos.color = new Color(1f, 0.85f, 0.2f);
        Vector3 lo = LaunchDirectionAt(minLoftAngle, dir);
        Vector3 hi = LaunchDirectionAt(maxLoftAngle, dir);
        Gizmos.DrawLine(origin, origin + lo * rayLength);
        Gizmos.DrawLine(origin, origin + hi * rayLength);

        Vector3 prev = origin + lo * rayLength;
        for (int i = 1; i <= 16; i++)
        {
            float a = Mathf.Lerp(minLoftAngle, maxLoftAngle, i / 16f);
            Vector3 p = origin + LaunchDirectionAt(a, dir) * rayLength;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }

        if (ballRb == null) return;

        float mass = Mathf.Max(0.0001f, ballRb.mass);
        float fullImpulse = ResolveImpulse(1f, out _);

        if (trajBuffer == null || trajBuffer.Length < 2)
            trajBuffer = new Vector3[Mathf.Max(2, trajectorySteps)];

        // le due parabole estreme a potenza piena: il tiro piu' raso e quello piu'
        // alto possibili da qui
        Gizmos.color = new Color(0.3f, 0.9f, 1f);
        DrawGizmoArc(origin, lo * fullImpulse / mass);

        Gizmos.color = new Color(1f, 0.4f, 0.9f);
        DrawGizmoArc(origin, hi * fullImpulse / mass);

        if (Application.isPlaying && CurrentState != State.Idle)
        {
            Gizmos.color = Color.green;
            Vector3 cur = LaunchDirection;
            Gizmos.DrawLine(origin, origin + cur * rayLength);
            DrawGizmoArc(origin, cur * ResolveImpulse(chargeRatio, out _) / mass);
        }
    }

    void DrawGizmoArc(Vector3 origin, Vector3 v0)
    {
        int count = BuildTrajectory(origin, v0, trajBuffer, trajectoryStep);
        for (int i = 1; i < count; i++)
            Gizmos.DrawLine(trajBuffer[i - 1], trajBuffer[i]);
    }
}
