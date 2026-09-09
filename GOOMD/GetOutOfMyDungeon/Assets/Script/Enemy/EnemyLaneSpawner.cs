using System.Collections.Generic;
using UnityEngine;

// Generatore a corsie: una "strada" dal punto A al punto B, eventualmente curva,
// divisa in N corsie parallele. Per ogni corsia si dice quanti nemici la
// percorrono insieme, a che velocita', in che verso, con che cadenza e con che
// assortimento di tipi.
//
// Ogni nemico compare in testa alla corsia, la percorre e sparisce in fondo;
// dopo una pausa irregolare ne arriva un altro. Il budget (spawnBudget) e' il
// numero TOTALE di nemici che passeranno: quando finisce la strada si svuota.
//
// Uccidere un nemico su una corsia provoca un incidente: gli altri di quella
// corsia vengono spazzati via e il traffico si ferma.
//
// I nemici vengono generati in Awake e non in Start: cosi' si registrano in
// EnemyHealth.All prima che GameManager.Start conti i nemici del livello.
public class EnemyLaneSpawner : MonoBehaviour
{
    public enum LaneDirection { AtoB, BtoA }

    // una voce del sacchetto da cui si pesca il tipo di nemico
    [System.Serializable]
    public class EnemyKind
    {
        public GameObject prefab;
        [Min(0f)] public float weight = 1f;   // peso relativo: 3 esce il triplo di 1
    }

    [System.Serializable]
    public class Lane
    {
        [Header("Traffico")]
        public int enemyCount = 3;                    // quanti al massimo insieme sulla corsia
        public LaneDirection direction = LaneDirection.AtoB;

        [Header("Cadenza")]
        public float spawnInterval = 1f;                    // pausa fra una comparsa e l'altra
        [Range(0f, 1f)] public float intervalVariation = 0.4f;  // 0.4 = fra il 60% e il 140%

        [Header("Velocita'")]
        public float speed = 2f;
        [Range(0f, 1f)] public float speedVariation = 0f;  // 0.3 = ognuno fra il 70% e il 130%

        [Header("Distanza di sicurezza")]
        public float minGap = 1.2f;      // paraurti: piu' vicino di cosi' non si arriva mai
        public float slowdownGap = 3.5f; // entro questa distanza si comincia a frenare

        [Header("Quanti in tutto")]
        public int spawnBudget = -1;                  // -1 = illimitato

        [Header("Disposizione iniziale")]
        [Range(0f, 1f)] public float phase;           // sfasa la corsia rispetto alle altre

        [Header("Tipi di nemico")]
        public EnemyKind[] poolOverride;              // vuoto = usa il sacchetto dello spawner
    }

    [Header("Percorso della strada")]
    public Transform pointA;
    public Transform[] waypoints;   // punti intermedi: servono a curvare la strada
    public Transform pointB;
    public bool smoothPath = true;  // passa una curva per i punti invece di spezzare
    [Range(2, 24)] public int samplesPerSegment = 8;

    [Header("Corsie")]
    public float laneSpacing = 2f;
    public bool centerLanes = true;   // le corsie stanno a cavallo della linea centrale
    public Lane[] lanes = { new Lane() };

    [Header("Tipi di nemico (si pesca a caso, in base al peso)")]
    public EnemyKind[] enemyPool;

    [Header("Nemici")]
    public bool faceTravelDirection = true;
    public float respawnDelay = 3f;   // attesa in piu' dopo un'uccisione. < 0 = posto perso

    [Header("Quanti in tutto")]
    public int totalSpawnBudget = -1;  // -1 = illimitato. Vale su tutte le corsie insieme

    [Header("Incidente")]
    public bool stopLaneOnCrash = true;   // uccidere un nemico blocca la sua corsia
    public float crashImpulse = 8f;       // spinta sugli altri nemici della corsia
    public float crashRadius = 6f;
    public float crashUpwards = 0.5f;
    public int crashDamage = 0;           // danno agli spazzati via. 0 = solo spinta
    public float crashRecovery = 4f;      // secondi prima che il traffico riparta. < 0 = mai

    public int SpawnedTotal { get; private set; }
    public int RemainingTotal => totalSpawnBudget < 0 ? -1 : Mathf.Max(0, totalSpawnBudget - SpawnedTotal);
    public bool IsExhausted => totalSpawnBudget >= 0 && SpawnedTotal >= totalSpawnBudget;

    private class Runner
    {
        public GameObject go;
        public EnemyHealth health;
        public Rigidbody rb;
        public int lane;
        public float t;        // 0 = testa della corsia, 1 = fondo
        public float speed;    // sua, non della corsia: ognuno va per conto proprio
        public float timer;    // attesa prima di ricomparire
        public bool retired;   // budget finito, o posto perso: non viene ripreso
    }

    private class LaneState
    {
        public bool crashed;
        public float recoveryTimer;
    }

    private readonly List<List<Runner>> runners = new List<List<Runner>>();
    private LaneState[] laneStates;
    private int[] spawnedPerLane;

    // percorso campionato: tutto il resto lavora su questa spezzata, curva o no
    private readonly List<Vector3> controls = new List<Vector3>();
    private readonly List<Vector3> path = new List<Vector3>();
    private readonly List<float> cumulative = new List<float>();
    private float pathLength;

    void Awake()
    {
        RebuildPath();

        if (pathLength <= 0.001f)
        {
            Debug.LogWarning("EnemyLaneSpawner: il percorso e' vuoto o lungo zero " +
                             "(servono almeno pointA e pointB), non genero niente.", this);
            return;
        }

        if (lanes == null || lanes.Length == 0) return;

        spawnedPerLane = new int[lanes.Length];
        laneStates = new LaneState[lanes.Length];

        for (int i = 0; i < lanes.Length; i++)
        {
            laneStates[i] = new LaneState();
            List<Runner> list = new List<Runner>();

            if (lanes[i] != null)
            {
                int n = Mathf.Max(0, lanes[i].enemyCount);
                for (int k = 0; k < n; k++)
                {
                    Runner r = new Runner { lane = i };

                    // la corsia parte gia' popolata, a distanza costante: la
                    // cadenza irregolare regola solo chi arriva dopo
                    r.t = Mathf.Repeat((float)k / n + lanes[i].phase, 1f);

                    // se il budget e' piu' stretto del traffico, la corsia
                    // parte incompleta invece di sforare
                    if (!TrySpawn(i, r, r.t)) r.retired = true;

                    list.Add(r);
                }
            }

            runners.Add(list);
        }
    }

    void Start()
    {
        // il livello e' invincibile per obiettivi solo se i nemici arrivano
        // davvero all'infinito: con un budget finito prima o poi si esauriscono
        if (totalSpawnBudget >= 0 || !HasEndlessLane()) return;

        GameManager gm = GameManager.Instance;
        if (gm != null && gm.requireAllEnemiesDead)
            Debug.LogWarning("EnemyLaneSpawner: i nemici arrivano all'infinito ma il livello " +
                             "chiede di ucciderli tutti, quindi non si potra' mai vincere per " +
                             "obiettivi. Dai un budget allo spawner, oppure usa requiredKills " +
                             "al posto di requireAllEnemiesDead.", this);
    }

    bool HasEndlessLane()
    {
        if (lanes == null) return false;

        foreach (Lane lane in lanes)
            if (lane != null && lane.spawnBudget < 0 && lane.enemyCount > 0) return true;

        return false;
    }

    void FixedUpdate()
    {
        // ricostruito ogni passo: cosi' si possono spostare i waypoint in Play
        // e vedere la strada cambiare. Le liste sono riusate, non alloca.
        RebuildPath();
        if (pathLength <= 0.001f || spawnedPerLane == null) return;

        for (int i = 0; i < runners.Count; i++)
        {
            Lane lane = lanes[i];
            if (lane == null) continue;

            if (!UpdateCrashState(i)) continue;

            List<Runner> list = runners[i];

            for (int k = 0; k < list.Count; k++)
            {
                Runner r = list[k];

                // rete di sicurezza: se e' morto senza che onDied sia arrivato
                // fin qui me ne accorgo comunque. Un prefab senza EnemyHealth
                // e' ammesso: e' un ostacolo che scorre, e non muore mai.
                if (r.go != null && r.health != null && r.health.IsDead)
                    Retire(r, true);

                if (r.go == null)
                {
                    if (r.retired) continue;

                    r.timer -= Time.fixedDeltaTime;
                    if (r.timer > 0f) continue;

                    // ricompare in testa alla corsia; se il budget e' finito,
                    // quel posto resta vuoto per sempre
                    if (!TrySpawn(i, r, 0f)) r.retired = true;
                    continue;
                }

                Advance(r, list, lane);

                if (r.t >= 1f)
                {
                    Retire(r, false);
                    continue;
                }

                Move(r, i);
            }
        }
    }

    // ---------- incidente ----------

    // torna false se la corsia e' ferma per un incidente
    bool UpdateCrashState(int laneIndex)
    {
        LaneState state = laneStates[laneIndex];
        if (!state.crashed) return true;

        if (crashRecovery < 0f) return false;   // corsia chiusa per sempre

        state.recoveryTimer -= Time.fixedDeltaTime;
        if (state.recoveryTimer > 0f) return false;

        state.crashed = false;

        // il traffico riparte scaglionato, non tutto insieme dalla testa
        List<Runner> list = runners[laneIndex];
        for (int k = 0; k < list.Count; k++)
        {
            list[k].retired = false;
            list[k].t = 0f;
            list[k].timer = SpawnWait(lanes[laneIndex]) * (k + 1);
        }

        return true;
    }

    // Un nemico ucciso sulla corsia manda in frantumi la fila: gli altri
    // vengono spazzati via e il traffico si ferma, come un tamponamento.
    void TriggerCrash(int laneIndex, Vector3 at)
    {
        if (!stopLaneOnCrash) return;
        if (laneStates == null || laneIndex < 0 || laneIndex >= laneStates.Length) return;

        LaneState state = laneStates[laneIndex];
        if (state.crashed) return;

        // prima di spazzare, altrimenti un nemico che muore nello sweep
        // rientrerebbe qui dentro
        state.crashed = true;
        state.recoveryTimer = crashRecovery;

        foreach (Runner other in runners[laneIndex])
        {
            if (other.go == null) continue;

            if (other.rb != null)
            {
                other.rb.isKinematic = false;
                other.rb.AddExplosionForce(crashImpulse, at, crashRadius, crashUpwards,
                                           ForceMode.Impulse);
            }

            if (crashDamage > 0 && other.health != null)
                other.health.TakeDamage(crashDamage);

            // esce dai binari: da qui in poi lo muove solo la fisica
            other.go = null;
            other.health = null;
            other.rb = null;
            other.retired = true;
        }
    }

    // ---------- traffico ----------

    // Chi ha qualcuno davanti rallenta invece di attraversarlo: entro
    // slowdownGap frena in proporzione, a minGap e' fermo. Ne vengono fuori
    // ingorghi che si formano e si sciolgono da soli, e nessuna compenetrazione.
    void Advance(Runner r, List<Runner> list, Lane lane)
    {
        float previous = r.t;

        if (lane.minGap <= 0f)
        {
            r.t += r.speed * Time.fixedDeltaTime / pathLength;
            return;
        }

        float leaderT = LeaderT(list, r);
        float factor = 1f;

        if (!float.IsPositiveInfinity(leaderT))
        {
            float gap = (leaderT - r.t) * pathLength;

            // slowdownGap sotto minGap non avrebbe senso: eviterei una
            // divisione per zero e una frenata a gradino
            float slow = Mathf.Max(lane.slowdownGap, lane.minGap + 0.01f);
            factor = Mathf.Clamp01((gap - lane.minGap) / (slow - lane.minGap));
        }

        r.t += r.speed * factor * Time.fixedDeltaTime / pathLength;

        if (float.IsPositiveInfinity(leaderT)) return;

        // paraurti duro: la frenata proporzionale da sola non basta se chi sta
        // davanti si e' fermato di colpo
        float cap = leaderT - lane.minGap / pathLength;
        if (r.t > cap) r.t = Mathf.Max(previous, cap);   // mai indietro
    }

    // il primo che sta davanti sulla corsia: t cresce sempre nel verso di
    // marcia, quindi "davanti" e' semplicemente il t piu' vicino fra i maggiori
    static float LeaderT(List<Runner> list, Runner self)
    {
        float best = float.PositiveInfinity;

        foreach (Runner other in list)
        {
            if (other == self || other.go == null) continue;
            if (other.t <= self.t) continue;
            if (other.t < best) best = other.t;
        }

        return best;
    }

    // ---------- comparsa e sparizione ----------

    bool TrySpawn(int laneIndex, Runner r, float t)
    {
        if (!HasBudget(laneIndex)) return false;

        GameObject prefab = PickPrefab(laneIndex);
        if (prefab == null)
        {
            Debug.LogWarning($"EnemyLaneSpawner: la corsia {laneIndex} non ha nessun prefab " +
                             "da cui pescare.", this);
            return false;
        }

        Lane lane = lanes[laneIndex];

        Quaternion rotation = faceTravelDirection
            ? Quaternion.LookRotation(TravelDirection(laneIndex, t), Vector3.up)
            : prefab.transform.rotation;

        r.lane = laneIndex;
        r.t = t;
        r.speed = lane.speed * Random.Range(1f - lane.speedVariation, 1f + lane.speedVariation);
        r.timer = 0f;

        r.go = Instantiate(prefab, LanePoint(laneIndex, t), rotation, transform);
        r.health = r.go.GetComponent<EnemyHealth>();
        r.rb = r.go.GetComponent<Rigidbody>();

        // sui binari finche' e' vivo: un corpo dinamico spostato a mano
        // combatte col solver
        if (r.rb != null) r.rb.isKinematic = true;

        // lo libero nell'istante in cui muore, non al prossimo FixedUpdate:
        // l'onda d'urto che lo ha ucciso deve poterlo ancora spedire via
        if (r.health != null)
            r.health.onDied.AddListener(() => Retire(r, true));

        SpawnedTotal++;
        spawnedPerLane[laneIndex]++;
        return true;
    }

    // killed = ucciso; altrimenti e' semplicemente arrivato in fondo
    void Retire(Runner r, bool killed)
    {
        if (r.go == null && r.health == null) return;

        Vector3 at = r.go != null ? r.go.transform.position : transform.position;

        Lane lane = lanes != null && r.lane >= 0 && r.lane < lanes.Length
            ? lanes[r.lane]
            : null;

        if (killed)
        {
            // se muore mentre e' ancora sui binari va restituito alla fisica,
            // altrimenti l'esplosione non lo sposta
            if (r.rb != null) r.rb.isKinematic = false;

            // la distruzione la gestisce EnemyHealth (destroyOnDeath/deathDelay)
            if (respawnDelay < 0f) r.retired = true;
            else r.timer = respawnDelay + SpawnWait(lane);
        }
        else
        {
            if (r.go != null) Destroy(r.go);
            r.timer = SpawnWait(lane);
        }

        r.go = null;
        r.health = null;
        r.rb = null;

        if (killed) TriggerCrash(r.lane, at);
    }

    float SpawnWait(Lane lane)
    {
        if (lane == null || lane.spawnInterval <= 0f) return 0f;

        return lane.spawnInterval * Random.Range(1f - lane.intervalVariation,
                                                 1f + lane.intervalVariation);
    }

    bool HasBudget(int laneIndex)
    {
        if (totalSpawnBudget >= 0 && SpawnedTotal >= totalSpawnBudget) return false;

        int laneBudget = lanes[laneIndex].spawnBudget;
        return laneBudget < 0 || spawnedPerLane[laneIndex] < laneBudget;
    }

    GameObject PickPrefab(int laneIndex)
    {
        EnemyKind[] pool = lanes[laneIndex].poolOverride;
        if (pool == null || pool.Length == 0) pool = enemyPool;
        if (pool == null || pool.Length == 0) return null;

        float total = 0f;
        foreach (EnemyKind kind in pool)
            if (kind != null && kind.prefab != null && kind.weight > 0f) total += kind.weight;

        // nessun peso valido: ripiego sul primo prefab utile invece di non
        // generare niente, che sembrerebbe uno spawner rotto
        if (total <= 0f)
        {
            foreach (EnemyKind kind in pool)
                if (kind != null && kind.prefab != null) return kind.prefab;

            return null;
        }

        float roll = Random.Range(0f, total);
        foreach (EnemyKind kind in pool)
        {
            if (kind == null || kind.prefab == null || kind.weight <= 0f) continue;

            roll -= kind.weight;
            if (roll <= 0f) return kind.prefab;
        }

        return null;
    }

    void Move(Runner r, int laneIndex)
    {
        Vector3 position = LanePoint(laneIndex, r.t);

        if (r.rb != null && r.rb.isKinematic) r.rb.MovePosition(position);
        else r.go.transform.position = position;

        if (!faceTravelDirection) return;

        Quaternion rotation = Quaternion.LookRotation(TravelDirection(laneIndex, r.t), Vector3.up);
        if (r.rb != null && r.rb.isKinematic) r.rb.MoveRotation(rotation);
        else r.go.transform.rotation = rotation;
    }

    // ---------- geometria della strada ----------

    // Campiona il percorso in una spezzata e ne misura le lunghezze cumulate:
    // da li' in poi tutto lavora per lunghezza d'arco, cosi' la velocita' resta
    // costante anche dove la curva e' stretta.
    void RebuildPath()
    {
        controls.Clear();
        if (pointA != null) controls.Add(pointA.position);

        if (waypoints != null)
            foreach (Transform w in waypoints)
                if (w != null) controls.Add(w.position);

        if (pointB != null) controls.Add(pointB.position);

        path.Clear();
        cumulative.Clear();
        pathLength = 0f;

        if (controls.Count < 2) return;

        if (smoothPath && controls.Count > 2)
        {
            int last = controls.Count - 1;
            for (int i = 0; i < last; i++)
            {
                // Catmull-Rom: agli estremi duplico il punto di bordo, cosi' la
                // curva parte e finisce esattamente su A e B
                Vector3 p0 = controls[Mathf.Max(i - 1, 0)];
                Vector3 p1 = controls[i];
                Vector3 p2 = controls[i + 1];
                Vector3 p3 = controls[Mathf.Min(i + 2, last)];

                for (int s = 0; s < samplesPerSegment; s++)
                    path.Add(CatmullRom(p0, p1, p2, p3, (float)s / samplesPerSegment));
            }

            path.Add(controls[last]);
        }
        else
        {
            path.AddRange(controls);
        }

        cumulative.Add(0f);
        for (int i = 1; i < path.Count; i++)
        {
            pathLength += Vector3.Distance(path[i - 1], path[i]);
            cumulative.Add(pathLength);
        }
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * ((2f * p1)
                       + (-p0 + p2) * t
                       + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                       + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    // u va sempre da A verso B, indipendentemente dal senso di marcia
    void Sample(float u, out Vector3 position, out Vector3 direction)
    {
        position = transform.position;
        direction = Vector3.forward;

        if (path.Count < 2) return;

        u = Mathf.Clamp01(u);
        float target = u * pathLength;

        int i = 1;
        while (i < cumulative.Count - 1 && cumulative[i] < target) i++;

        float segmentStart = cumulative[i - 1];
        float segmentLength = cumulative[i] - segmentStart;
        float local = segmentLength > 0.0001f ? (target - segmentStart) / segmentLength : 0f;

        position = Vector3.Lerp(path[i - 1], path[i], local);

        Vector3 d = path[i] - path[i - 1];
        if (d.sqrMagnitude > 0.0001f) direction = d.normalized;
    }

    // perpendicolare orizzontale: e' su questa che si spostano le corsie
    static Vector3 SideOf(Vector3 direction)
    {
        Vector3 side = Vector3.Cross(direction, Vector3.up);

        // tratto verticale: la perpendicolare degenera, ripiego su un asse fisso
        return side.sqrMagnitude < 0.0001f ? Vector3.right : side.normalized;
    }

    float LaneOffset(int laneIndex)
    {
        return centerLanes
            ? (laneIndex - (lanes.Length - 1) * 0.5f) * laneSpacing
            : laneIndex * laneSpacing;
    }

    // t = 0 e' sempre la testa della corsia, qualunque sia il senso di marcia
    public Vector3 LanePoint(int laneIndex, float t)
    {
        float u = lanes[laneIndex].direction == LaneDirection.AtoB ? t : 1f - t;

        Sample(u, out Vector3 position, out Vector3 direction);

        // lo scostamento si misura sempre sulla direzione A->B, altrimenti le
        // corsie invertite finirebbero dal lato sbagliato della strada
        return position + SideOf(direction) * LaneOffset(laneIndex);
    }

    public Vector3 TravelDirection(int laneIndex, float t)
    {
        float u = lanes[laneIndex].direction == LaneDirection.AtoB ? t : 1f - t;

        Sample(u, out Vector3 _, out Vector3 direction);

        return lanes[laneIndex].direction == LaneDirection.AtoB ? direction : -direction;
    }

    // ---------- gizmo ----------

    void OnDrawGizmos()
    {
        if (lanes == null) return;

        RebuildPath();
        if (pathLength <= 0.001f) return;

        // i punti di controllo, per ritrovarli quando la curva se ne discosta
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.7f);
        foreach (Vector3 c in controls)
            Gizmos.DrawWireSphere(c, 0.2f);

        const int steps = 40;

        for (int i = 0; i < lanes.Length; i++)
        {
            if (lanes[i] == null) continue;

            bool crashed = laneStates != null && i < laneStates.Length
                           && laneStates[i] != null && laneStates[i].crashed;

            Gizmos.color = crashed
                ? new Color(1f, 0.25f, 0.2f, 0.9f)
                : new Color(1f, 0.55f, 0.2f, 0.85f);

            Vector3 previous = LanePoint(i, 0f);
            for (int s = 1; s <= steps; s++)
            {
                Vector3 current = LanePoint(i, (float)s / steps);
                Gizmos.DrawLine(previous, current);
                previous = current;
            }

            Vector3 head = LanePoint(i, 0f);
            Gizmos.DrawWireSphere(head, 0.3f);          // dove compaiono

            Gizmos.color = new Color(1f, 0.55f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(LanePoint(i, 1f), 0.3f);   // dove spariscono

            // frecce del senso di marcia lungo tutta la corsia
            Gizmos.color = new Color(1f, 0.7f, 0.3f, 0.9f);
            for (int a = 1; a <= 4; a++)
            {
                float t = a / 5f;
                DrawArrow(LanePoint(i, t), TravelDirection(i, t), 0.45f);
            }

            // anteprima delle posizioni di partenza
            int n = Mathf.Max(0, lanes[i].enemyCount);
            Gizmos.color = new Color(1f, 0.8f, 0.4f, 0.8f);
            for (int k = 0; k < n; k++)
            {
                float t = Mathf.Repeat((float)k / n + lanes[i].phase, 1f);
                Gizmos.DrawWireCube(LanePoint(i, t), Vector3.one * 0.4f);
            }

#if UNITY_EDITOR
            string budget = lanes[i].spawnBudget < 0 ? "inf" : lanes[i].spawnBudget.ToString();
            string arrow = lanes[i].direction == LaneDirection.AtoB ? "A->B" : "B->A";
            string label = $"Corsia {i}  {arrow}  x{n}  v {lanes[i].speed:0.#}  budget {budget}";
            if (crashed) label += "  INCIDENTE";

            UnityEditor.Handles.color = new Color(1f, 0.8f, 0.4f, 1f);
            UnityEditor.Handles.Label(head + Vector3.up * 0.6f, label);
#endif
        }
    }

    static void DrawArrow(Vector3 at, Vector3 direction, float size)
    {
        Vector3 left = Quaternion.AngleAxis(150f, Vector3.up) * direction;
        Vector3 right = Quaternion.AngleAxis(-150f, Vector3.up) * direction;

        Gizmos.DrawLine(at, at + left * size);
        Gizmos.DrawLine(at, at + right * size);
    }
}
