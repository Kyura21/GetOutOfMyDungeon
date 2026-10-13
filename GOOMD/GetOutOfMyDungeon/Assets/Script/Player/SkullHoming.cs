using UnityEngine;

// La palla a ricerca: il colpo caricato oltre il 100%.
//
// Il bersaglio e' il nemico vivo piu' vicino dentro un cono davanti al tiro
// (range, coneAngle). A parita' di distanza (entro tieDistance) vince quello piu'
// allineato al tiro. Lo sceglie SkullAimController al rilascio, con FindTarget.
//
// LA TRAIETTORIA e' una parabola con l'arco scelto (arcAngle): la palla parte
// arcAngle gradi sopra la linea dritta verso il nemico, alla velocita' che serve
// per arrivarci. A velocita' fissa non si potrebbe scegliere l'arco: esistono solo
// due parabole, una quasi dritta e una altissima. Con arcAngle a 0 si torna a
// quella: la velocita' e' quella del colpo e la parabola e' la bassa.
// Mira a aimHeight dell'altezza del nemico: verso la testa, non ai piedi.
//
// IN VOLO la palla non vola libera: percorre quella parabola guidata dal codice,
// con un'accelerazione (easeIn). Parte lenta e arriva velocissima, e la curva resta
// la stessa. Resta un corpo fisico: le si da' una velocita' a ogni passo, non la si
// sposta a mano, quindi gli urti funzionano.
// Se il nemico si sposta, la fine della curva lo segue (steering), sempre di piu'
// man mano che la palla si avvicina. Smette al primo urto, quando il bersaglio
// muore, o a fine curva: da li' la palla prosegue libera con la velocita' che ha.
[RequireComponent(typeof(Rigidbody))]
public class SkullHoming : MonoBehaviour
{
    [Header("Bersaglio")]
    public float range = 25f;                        // metri, dalla palla
    [Range(0f, 180f)] public float coneAngle = 35f;  // gradi a destra e a sinistra del tiro
    public float tieDistance = 0.5f;                 // metri: piu' vicini di cosi' contano uguali
    [Range(0f, 1f)] public float aimHeight = 0.75f;  // dove mirare sul nemico: 0 = piedi, 1 = cima

    [Header("Traiettoria")]
    // L'arco del tiro: di quanti gradi la palla parte sopra la linea dritta verso il
    // nemico. La curva ha la stessa forma a ogni distanza, e la velocita' si adatta
    // per arrivarci. 0 = niente arco scelto: la palla parte alla potenza del colpo
    // (overchargeMultiplier) sulla parabola piu' bassa, quasi dritta.
    [Range(0f, 60f)] public float arcAngle = 10f;
    // L'accelerazione lungo la curva. 1 = velocita' costante. 2 = parte da ferma e
    // arriva al doppio della velocita' media. Piu' alto = partenza piu' lenta e
    // arrivo piu' violento. La durata del volo resta la stessa.
    [Range(1f, 4f)] public float easeIn = 2f;
    [Range(0f, 1f)] public float steering = 1f;  // quanto la curva segue il nemico che si sposta. 0 = per niente
    public float ignoreHitsFor = 0.1f;   // secondi dopo il lancio in cui un urto non ferma la ricerca

    [Header("Gizmo")]
    public bool showGizmo = true;        // il cono di ricerca, attorno alla direzione di tiro

    public bool IsHoming { get; private set; }
    public EnemyHealth Target { get; private set; }

    private Rigidbody rb;
    private float flightTime;
    private float elapsed;
    // la curva, fissata al lancio
    private Vector3 startPosition;
    private Vector3 startVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Il nemico vivo piu' vicino nel cono davanti al tiro. Null se non ce n'e'.
    public EnemyHealth FindTarget(Vector3 from, Vector3 direction)
    {
        Vector3 flatDir = new Vector3(direction.x, 0f, direction.z);
        if (flatDir.sqrMagnitude < 0.0001f) return null;
        flatDir.Normalize();

        EnemyHealth best = null;
        float bestDistance = float.MaxValue;
        float bestAngle = float.MaxValue;

        var all = EnemyHealth.All;
        for (int i = 0; i < all.Count; i++)
        {
            EnemyHealth enemy = all[i];
            if (enemy == null || enemy.IsDead) continue;

            Vector3 to = AimPoint(enemy) - from;
            float distance = to.magnitude;
            if (distance > range) continue;

            Vector3 flat = new Vector3(to.x, 0f, to.z);
            float angle = flat.sqrMagnitude > 0.0001f ? Vector3.Angle(flatDir, flat) : 0f;
            if (angle > coneAngle) continue;

            // piu' vicino di un tanto: vince. Quasi alla pari: vince il piu' allineato
            bool closer = distance < bestDistance - tieDistance;
            bool tie = Mathf.Abs(distance - bestDistance) <= tieDistance && angle < bestAngle;
            if (!closer && !tie) continue;

            best = enemy;
            bestDistance = distance;
            bestAngle = angle;
        }

        return best;
    }

    // La velocita' di partenza verso il bersaglio. Con arcAngle > 0 segue l'arco
    // scelto e "speed" non conta; con arcAngle 0 parte a "speed" sulla parabola bassa.
    // "time" e' quanto dura il volo.
    public Vector3 LaunchVelocity(Vector3 from, EnemyHealth target, float speed, out float time)
    {
        Vector3 d = AimPoint(target) - from;
        return arcAngle > 0f ? ArcVelocity(d, arcAngle, out time) : DirectVelocity(d, speed, out time);
    }

    public void Launch(EnemyHealth target, float speed)
    {
        if (target == null) return;

        startPosition = rb.position;
        startVelocity = LaunchVelocity(startPosition, target, speed, out flightTime);
        flightTime = Mathf.Max(0.05f, flightTime);

        // parte lenta: la velocita' vera la decide il primo passo di FixedUpdate
        rb.linearVelocity = Vector3.zero;

        Target = target;
        elapsed = 0f;
        IsHoming = true;
    }

    // Il punto della parabola a una frazione u del volo (0 = partenza, 1 = arrivo).
    Vector3 PathPoint(float u)
    {
        float t = u * flightTime;
        return startPosition + startVelocity * t + 0.5f * Physics.gravity * t * t;
    }

    public void Stop()
    {
        IsHoming = false;
        Target = null;
    }

    void FixedUpdate()
    {
        if (!IsHoming) return;

        if (Target == null || Target.IsDead)
        {
            Stop();
            return;
        }

        float dt = Time.fixedDeltaTime;
        elapsed += dt;

        // dove deve essere la palla alla fine di questo passo
        float t = Mathf.Clamp01(elapsed / flightTime);
        float u = Mathf.Pow(t, easeIn);

        // la fine della curva va portata sul nemico di adesso: si e' spostato, oppure
        // era fuori portata e la curva a 45 gradi finiva corta. La correzione pesa
        // tanto piu' quanto la palla e' avanti, cosi' la partenza non scatta
        Vector3 drift = (AimPoint(Target) - PathPoint(1f)) * (steering * u);
        Vector3 next = PathPoint(u) + drift;

        // la velocita' che ce la porta in un passo. Il motore aggiunge la gravita'
        // prima di muovere il corpo: la tolgo qui, o la palla scenderebbe sotto la curva
        Vector3 velocity = (next - rb.position) / dt;
        if (rb.useGravity) velocity -= Physics.gravity * dt;
        rb.linearVelocity = velocity;

        // fine della curva senza urto: da qui la palla prosegue libera
        if (t >= 1f) Stop();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (IsHoming && elapsed >= ignoreHitsFor) Stop();
    }

    // Parte "arc" gradi sopra la linea dritta verso d, e trova la velocita' che con
    // quell'angolo arriva esattamente su d.
    static Vector3 ArcVelocity(Vector3 d, float arc, out float time)
    {
        float g = Mathf.Max(0.01f, Physics.gravity.magnitude);

        Vector3 flat = new Vector3(d.x, 0f, d.z);
        float x = flat.magnitude;
        float y = d.y;

        // bersaglio quasi in verticale: un arco non ha senso, vado dritto
        if (x < 0.01f)
        {
            time = 0.3f;
            return d / time - 0.5f * Physics.gravity * time;
        }

        float line = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
        float angle = Mathf.Min(line + arc, 85f) * Mathf.Deg2Rad;

        float cos = Mathf.Cos(angle);
        // dalla parabola: y = x tan(a) - g x^2 / (2 v^2 cos^2(a))
        float rise = x * Mathf.Tan(angle) - y;   // > 0 perche' parto sopra la linea
        float speed = Mathf.Sqrt(g * x * x / (2f * cos * cos * Mathf.Max(0.001f, rise)));

        time = x / Mathf.Max(0.01f, speed * cos);
        Vector3 flatDir = flat / x;
        return flatDir * (speed * cos) + Vector3.up * (speed * Mathf.Sin(angle));
    }

    // A velocita' fissa, verso uno spostamento d, ci sono due parabole: una bassa e
    // una alta. Questa prende la bassa, la piu' diretta. Se il bersaglio e' fuori
    // portata tira a 45 gradi, il tiro che va piu' lontano: la correzione in volo
    // seguira' comunque la curva fino al nemico.
    static Vector3 DirectVelocity(Vector3 d, float speed, out float time)
    {
        float g = Mathf.Max(0.01f, Physics.gravity.magnitude);
        speed = Mathf.Max(0.01f, speed);

        Vector3 flat = new Vector3(d.x, 0f, d.z);
        float x = flat.magnitude;
        float y = d.y;

        // bersaglio quasi in verticale: dritto verso di lui
        if (x < 0.01f)
        {
            time = d.magnitude / speed;
            return d.normalized * speed;
        }

        Vector3 flatDir = flat / x;
        float v2 = speed * speed;
        float disc = v2 * v2 - g * (g * x * x + 2f * y * v2);

        float angle = disc >= 0f
            ? Mathf.Atan((v2 - Mathf.Sqrt(disc)) / (g * x))
            : 45f * Mathf.Deg2Rad;

        float cos = Mathf.Cos(angle);
        time = x / Mathf.Max(0.01f, speed * cos);
        return flatDir * (speed * cos) + Vector3.up * (speed * Mathf.Sin(angle));
    }

    // Dove mirare sul nemico: aimHeight dell'altezza del suo collider.
    Vector3 AimPoint(EnemyHealth enemy)
    {
        Collider col = enemy.GetComponentInChildren<Collider>();
        if (col == null) return enemy.transform.position;

        Bounds b = col.bounds;
        return b.center + Vector3.up * (b.extents.y * (aimHeight * 2f - 1f));
    }

    // --- Gizmo ---------------------------------------------------------------

    private static SkullAimController gizmoAim;

    // Il cono di ricerca a terra: la portata (cerchio) e i due lati del cono, attorno
    // alla direzione di tiro. Fuori dal Play la direzione e' il forward della palla.
    void OnDrawGizmos()
    {
        if (!showGizmo) return;

        if (gizmoAim == null) gizmoAim = FindFirstObjectByType<SkullAimController>();

        Vector3 dir = gizmoAim != null && Application.isPlaying ? gizmoAim.ShotDirection : transform.forward;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
        dir.Normalize();

        Vector3 origin = transform.position;

        // la portata, tutta attorno
        Gizmos.color = new Color(0.7f, 0.3f, 1f, 0.35f);
        DrawArc(origin, dir, 180f, range, 48);

        // il cono, piu' marcato
        Gizmos.color = new Color(0.7f, 0.3f, 1f, 0.9f);
        Vector3 left = Quaternion.Euler(0f, -coneAngle, 0f) * dir;
        Vector3 right = Quaternion.Euler(0f, coneAngle, 0f) * dir;
        Gizmos.DrawLine(origin, origin + left * range);
        Gizmos.DrawLine(origin, origin + right * range);
        DrawArc(origin, dir, coneAngle, range, 24);

        if (IsHoming && Target != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(origin, AimPoint(Target));
        }
    }

    // Arco orizzontale di raggio r, da -half a +half gradi attorno a dir.
    static void DrawArc(Vector3 origin, Vector3 dir, float half, float r, int segments)
    {
        Vector3 prev = origin + (Quaternion.Euler(0f, -half, 0f) * dir) * r;
        for (int i = 1; i <= segments; i++)
        {
            float a = Mathf.Lerp(-half, half, i / (float)segments);
            Vector3 p = origin + (Quaternion.Euler(0f, a, 0f) * dir) * r;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
}
