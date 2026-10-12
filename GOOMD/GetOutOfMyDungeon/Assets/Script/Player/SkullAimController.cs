using UnityEngine;
using UnityEngine.InputSystem;

// Il tiro, modello golf: direzione + angolo in altezza + potenza.
//
// Sequenza di input:
//   Idle     -> PlayerController entra in mira finche' si TIENE premuto il tasto destro
//   Aiming   -> la ROTELLA alza e abbassa l'angolo. La direzione parte da dove guarda
//               il personaggio e si riallinea a dove guarda la camera.
//               Premendo il tasto sinistro l'angolo e' confermato e parte la carica.
//   Powering -> piu' si tiene premuto, piu' si carica. Si tira RILASCIANDO,
//               e si torna subito Idle.
//
// Dopo il tiro non si aspetta che la palla si fermi: il personaggio torna libero
// appena finisce lo swing. Prima c'era uno stato Shot che lo bloccava finche' la
// palla rotolava. Vedi lo Storico.
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

    // dove si e' fermata la carica: lo leggono HUD e punteggio
    public enum PowerZone { Normal, Bottom, Top }

    [Header("Riferimenti")]
    public Transform ball;                  // la testa-palla
    public Rigidbody ballRb;
    public Transform cameraTransform;       // da' la direzione del tiro. Vuoto = Camera.main
    public PlayerSwing swing;               // da lui arrivano la portata della mazza e lo swing
    public SkullExplosion skullExplosion;   // se vuoto lo prendo dalla palla

    [Header("Direzione")]
    // All'ingresso in mira la direzione e' quella del corpo, poi ruota verso la
    // camera a questa velocita'. 0 = salta subito sulla camera.
    public float aimAlignSpeed = 360f;      // gradi al secondo

    [Header("Angolo di tiro (rotella)")]
    public float minLoftAngle = 0f;         // 0 = tiro raso
    public float maxLoftAngle = 55f;        // il massimo, in gradi
    [Range(0f, 1f)] public float startLoftRatio = 0f;        // angolo con cui si entra in mira
    [Range(0f, 1f)] public float loftPerRevolution = 0.25f;  // quanto cambia in un giro di rotella
    public float notchesPerRevolution = 24f; // scatti in un giro di rotella (mouse comuni: 24)
    public float loftSmooth = 14f;           // 0 = l'angolo salta di scatto
    public bool showAimGizmo = true;

    [Header("Potenza (si carica tenendo premuto)")]
    public float maxPower = 20f;
    public float chargeTime = 1.2f;         // secondi di pressione per arrivare al massimo
    public float forceMultiplier = 1f;

    [Header("Zone agli estremi della carica")]
    // Tenendo premuto si arriva al massimo da soli, quindi queste zone non sono piu'
    // una prova di tempismo come erano con la barra oscillante: la zona alta la prende
    // chiunque aspetti. Per questo zoneWidth parte da 0, cioe' spente.
    [Range(0f, 0.49f)] public float zoneWidth = 0f;
    public float topZoneMultiplier = 1.3f;
    public float bottomZoneMultiplier = 1f;
    [Range(0f, 1f)] public float bottomZonePower = 0.12f;  // la carica a zero non farebbe partire il tiro


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

    public float CurrentPower => currentPower;
    public float PowerRatio => maxPower > 0f ? currentPower / maxPower : 0f;

    public PowerZone LastShotZone { get; private set; } = PowerZone.Normal;
    public float LastShotImpulse { get; private set; }

    // Vero se l'ultimo colpo ha davvero preso la testa. Se la mazza ha mulinato a
    // vuoto e' falso, e la testa non si e' mossa. Lo legge ShotCamera per decidere
    // se aprire la finestra della telecronaca.
    public bool LastShotHitBall { get; private set; }

    // Quante volte un colpo ha preso la testa da inizio partita. ShotCamera lo
    // confronta con l'ultimo valore visto: se e' cresciuto, e' partito un tiro.
    // E' un contatore e non un evento, cosi' chi lo legge non deve agganciarsi.
    public int BallHits { get; private set; }

    private Vector3 shotDirection = Vector3.forward;
    private float currentPower;
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

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (swing == null) swing = FindFirstObjectByType<PlayerSwing>();

        if (ball != null && skullExplosion == null)
            skullExplosion = ball.GetComponent<SkullExplosion>();

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

        // l'angolo riparte dal default e non da quello del tiro precedente:
        // un colpo alto rimasto in memoria sarebbe una sorpresa
        targetLoft = loftRatio = Mathf.Clamp01(startLoftRatio);

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
        currentPower = 0f;
        chargeTimer = 0f;

        if (swing != null) swing.EndStance();
        HideTrajectory();
    }

    void Update()
    {
        switch (CurrentState)
        {
            case State.Idle:
                break;

            case State.Aiming:
                UpdateDirection();
                ReadLoftScroll();
                UpdateLoftSmoothing();
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
                    currentPower = 0f;
                }
                break;

            case State.Powering:
                // la direzione resta viva: la camera gira col mouse e il tiro la segue,
                // cosi' quello che vedi e' quello che tiri
                UpdateDirection();
                UpdateCharge();
                UpdateTrajectory(PowerRatio);

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

    void UpdateLoftSmoothing()
    {
        loftRatio = loftSmooth > 0f
            ? Mathf.Lerp(loftRatio, targetLoft, Time.deltaTime * loftSmooth)
            : targetLoft;
    }

    // La potenza sale finche' si tiene premuto e si ferma al massimo.
    void UpdateCharge()
    {
        chargeTimer += Time.deltaTime;
        currentPower = maxPower * Mathf.Clamp01(chargeTimer / Mathf.Max(0.05f, chargeTime));
    }

    void Shoot()
    {
        float charge = PowerRatio;
        float impulse = ResolveImpulse(charge, out PowerZone zone);

        // La mazza colpisce solo quello che ha davanti. Se la testa non e' a
        // portata il colpo va a vuoto: la mazza mulina e la testa non si muove.
        bool hitsBall = BallInReach;

        LastShotZone = zone;
        LastShotHitBall = hitsBall;
        LastShotImpulse = hitsBall ? impulse : 0f;

        // lo swing si vede sempre, anche a vuoto. Tutto quello che sta nel cerchio
        // viene colpito insieme alla palla e spinto lungo il tiro: nemici, e in
        // futuro gli oggetti di scena
        if (swing != null) swing.OnShotReleased(charge, LaunchDirection);

        if (!hitsBall) return;

        BallHits++;

        // riarmo il teschio: senza questo esplode una volta sola per partita
        if (skullExplosion != null)
            skullExplosion.ResetExplosion();

        // la velocita' vecchia va buttata: sommare l'impulso a una palla che si
        // muoveva ancora darebbe tiri imprevedibili
        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;

        ballRb.AddForce(LaunchDirection * impulse, ForceMode.Impulse);
    }

    // Da "quanto ho caricato" (0..1) all'impulso da dare al Rigidbody.
    // Con zoneWidth a 0 e' una proporzione diretta: carica piena = maxPower.
    public float ResolveImpulse(float chargeRatio, out PowerZone zone)
    {
        chargeRatio = Mathf.Clamp01(chargeRatio);
        zone = PowerZone.Normal;

        float shaped = chargeRatio;
        float multiplier = 1f;

        if (zoneWidth > 0f && chargeRatio >= 1f - zoneWidth)
        {
            zone = PowerZone.Top;
            multiplier = topZoneMultiplier;
        }
        else if (zoneWidth > 0f && chargeRatio <= zoneWidth)
        {
            zone = PowerZone.Bottom;
            shaped = bottomZonePower;
            multiplier = bottomZoneMultiplier;
        }

        return maxPower * Mathf.Max(0f, shaped) * multiplier * forceMultiplier;
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
            if (ballIn) used = DrawPath(ball.position, ballV0, used, perPath);

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
            DrawGizmoArc(origin, cur * ResolveImpulse(PowerRatio, out _) / mass);
        }
    }

    void DrawGizmoArc(Vector3 origin, Vector3 v0)
    {
        int count = BuildTrajectory(origin, v0, trajBuffer, trajectoryStep);
        for (int i = 1; i < count; i++)
            Gizmos.DrawLine(trajBuffer[i - 1], trajBuffer[i]);
    }
}
