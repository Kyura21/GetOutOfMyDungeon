using System.Collections.Generic;
using UnityEngine;

// Generatore a corsie: una "strada" dal punto A al punto B, divisa in N corsie
// parallele. Per ogni corsia si dice quanti nemici la percorrono insieme, a che
// velocita', in che verso e con che assortimento di tipi.
//
// Ogni nemico compare in testa alla corsia, la percorre e sparisce in fondo,
// dove ne compare subito un altro: la corsia resta piena finche' c'e' budget.
// Il budget (spawnBudget) e' il numero TOTALE di nemici che passeranno: quando
// finisce la strada si svuota e non ne arrivano piu'.
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
        public int enemyCount = 3;                    // quanti ce ne sono insieme sulla corsia
        public LaneDirection direction = LaneDirection.AtoB;

        [Header("Velocita'")]
        public float speed = 2f;
        [Range(0f, 1f)] public float speedVariation = 0f;  // 0.3 = ognuno fra il 70% e il 130%

        [Header("Quanti in tutto")]
        public int spawnBudget = -1;                  // -1 = illimitato

        [Header("Disposizione")]
        [Range(0f, 1f)] public float phase;           // sfasa la corsia rispetto alle altre

        [Header("Tipi di nemico")]
        public EnemyKind[] poolOverride;              // vuoto = usa il sacchetto dello spawner
    }

    [Header("Strada")]
    public Transform pointA;
    public Transform pointB;
    public float laneSpacing = 2f;
    public bool centerLanes = true;   // le corsie stanno a cavallo della linea A-B

    [Header("Corsie")]
    public Lane[] lanes = { new Lane() };

    [Header("Tipi di nemico (si pesca a caso, in base al peso)")]
    public EnemyKind[] enemyPool;

    [Header("Nemici")]
    public bool faceTravelDirection = true;
    public float respawnDelay = 3f;   // dopo un'uccisione. < 0 = quel posto resta vuoto

    [Header("Quanti in tutto")]
    public int totalSpawnBudget = -1;  // -1 = illimitato. Vale su tutte le corsie insieme

    public int SpawnedTotal { get; private set; }
    public int RemainingTotal => totalSpawnBudget < 0 ? -1 : Mathf.Max(0, totalSpawnBudget - SpawnedTotal);
    public bool IsExhausted => totalSpawnBudget >= 0 && SpawnedTotal >= totalSpawnBudget;

    private class Runner
    {
        public GameObject go;
        public EnemyHealth health;
        public Rigidbody rb;
        public float t;        // 0 = testa della corsia, 1 = fondo
        public float speed;    // suo, non della corsia: ognuno va per conto proprio
        public float timer;    // attesa prima di ricomparire
        public bool retired;   // budget finito o respawn disattivato: il posto resta vuoto
    }

    private readonly List<List<Runner>> runners = new List<List<Runner>>();
    private int[] spawnedPerLane;

    void Awake()
    {
        if (pointA == null || pointB == null)
        {
            Debug.LogWarning("EnemyLaneSpawner: mancano pointA o pointB, non genero niente.", this);
            return;
        }

        if (lanes == null || lanes.Length == 0) return;

        spawnedPerLane = new int[lanes.Length];

        for (int i = 0; i < lanes.Length; i++)
        {
            List<Runner> list = new List<Runner>();
            int n = Mathf.Max(0, lanes[i].enemyCount);

            for (int k = 0; k < n; k++)
            {
                Runner r = new Runner();

                // distribuiti a distanza costante lungo la corsia
                r.t = Mathf.Repeat((float)k / n + lanes[i].phase, 1f);

                // se il budget e' piu' stretto del traffico, la corsia parte
                // gia' incompleta invece di sforare
                if (!TrySpawn(i, r, r.t)) r.retired = true;

                list.Add(r);
            }

            runners.Add(list);
        }
    }

    void Start()
    {
        // il livello e' invincibile per obiettivi solo se i nemici arrivano
        // davvero all'infinito: con un budget finito, prima o poi si esauriscono
        bool endless = totalSpawnBudget < 0 && HasEndlessLane();
        if (!endless) return;

        GameManager gm = GameManager.Instance;
        if (gm != null && gm.requireAllEnemiesDead)
            Debug.LogWarning("EnemyLaneSpawner: i nemici arrivano all'infinito ma il livello " +
                             "chiede di ucciderli tutti, quindi non si potra' mai vincere per " +
                             "obiettivi. Dai un budget allo spawner, oppure spegni " +
                             "requireAllEnemiesDead e lascia la buca come via di vittoria.", this);
    }

    bool HasEndlessLane()
    {
        foreach (Lane lane in lanes)
            if (lane != null && lane.spawnBudget < 0 && lane.enemyCount > 0) return true;

        return false;
    }

    void FixedUpdate()
    {
        if (pointA == null || pointB == null || spawnedPerLane == null) return;

        float length = Vector3.Distance(pointA.position, pointB.position);
        if (length < 0.001f) return;

        for (int i = 0; i < runners.Count; i++)
        {
            List<Runner> list = runners[i];

            for (int k = 0; k < list.Count; k++)
            {
                Runner r = list[k];

                // rete di sicurezza: se e' morto senza che onDied sia arrivato
                // fin qui (nemico ucciso da qualcosa che non passa da Die)
                // me ne accorgo comunque. Un prefab senza EnemyHealth e'
                // ammesso: e' un ostacolo che scorre, e non muore mai.
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

                r.t += r.speed * Time.fixedDeltaTime / length;

                if (r.t >= 1f)
                {
                    // arrivato in fondo: sparisce e lascia subito il posto al
                    // prossimo, cosi' il traffico non si dirada
                    Retire(r, false);
                    continue;
                }

                Move(r, i);
            }
        }
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
            ? Quaternion.LookRotation(TravelDirection(laneIndex), Vector3.up)
            : prefab.transform.rotation;

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

    // killed = ucciso dal giocatore; altrimenti e' semplicemente arrivato in fondo
    void Retire(Runner r, bool killed)
    {
        if (r.go == null && r.health == null) return;

        if (killed)
        {
            // se muore mentre e' ancora sui binari va restituito alla fisica,
            // altrimenti l'esplosione non lo sposta
            if (r.rb != null) r.rb.isKinematic = false;

            // la distruzione la gestisce EnemyHealth (destroyOnDeath/deathDelay)
            if (respawnDelay < 0f) r.retired = true;
            else r.timer = respawnDelay;
        }
        else
        {
            if (r.go != null) Destroy(r.go);
            r.timer = 0f;   // rimpiazzato subito: la corsia resta piena
        }

        r.go = null;
        r.health = null;
        r.rb = null;
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

        Quaternion rotation = Quaternion.LookRotation(TravelDirection(laneIndex), Vector3.up);
        if (r.rb != null && r.rb.isKinematic) r.rb.MoveRotation(rotation);
        else r.go.transform.rotation = rotation;
    }

    // ---------- geometria della strada ----------

    Vector3 Side()
    {
        Vector3 road = pointB.position - pointA.position;
        Vector3 side = Vector3.Cross(road.normalized, Vector3.up);

        // strada verticale: la perpendicolare degenera, ripiego su un asse fisso
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
        Vector3 offset = Side() * LaneOffset(laneIndex);
        Vector3 a = pointA.position + offset;
        Vector3 b = pointB.position + offset;

        return lanes[laneIndex].direction == LaneDirection.AtoB
            ? Vector3.Lerp(a, b, t)
            : Vector3.Lerp(b, a, t);
    }

    public Vector3 TravelDirection(int laneIndex)
    {
        Vector3 road = pointB.position - pointA.position;
        if (lanes[laneIndex].direction == LaneDirection.BtoA) road = -road;

        return road.sqrMagnitude < 0.0001f ? Vector3.forward : road.normalized;
    }

    // ---------- gizmo ----------

    void OnDrawGizmos()
    {
        if (pointA == null || pointB == null || lanes == null) return;

        for (int i = 0; i < lanes.Length; i++)
        {
            if (lanes[i] == null) continue;

            Vector3 head = LanePoint(i, 0f);   // dove compaiono
            Vector3 tail = LanePoint(i, 1f);   // dove spariscono
            Vector3 dir = TravelDirection(i);

            Gizmos.color = new Color(1f, 0.55f, 0.2f, 0.85f);
            Gizmos.DrawLine(head, tail);
            Gizmos.DrawWireSphere(head, 0.3f);

            Gizmos.color = new Color(1f, 0.55f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(tail, 0.3f);

            // frecce del senso di marcia lungo tutta la corsia
            Gizmos.color = new Color(1f, 0.7f, 0.3f, 0.9f);
            for (int a = 1; a <= 4; a++)
                DrawArrow(LanePoint(i, a / 5f), dir, 0.45f);

            // anteprima delle posizioni di partenza: la spaziatura si regola
            // a occhio senza entrare in Play
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
            UnityEditor.Handles.color = new Color(1f, 0.8f, 0.4f, 1f);
            UnityEditor.Handles.Label(head + Vector3.up * 0.6f,
                $"Corsia {i}  {arrow}  x{n}  v {lanes[i].speed:0.#}  budget {budget}");
#endif
        }
    }

    static void DrawArrow(Vector3 at, Vector3 dir, float size)
    {
        Vector3 left = Quaternion.AngleAxis(150f, Vector3.up) * dir;
        Vector3 right = Quaternion.AngleAxis(-150f, Vector3.up) * dir;

        Gizmos.DrawLine(at, at + left * size);
        Gizmos.DrawLine(at, at + right * size);
    }
}
