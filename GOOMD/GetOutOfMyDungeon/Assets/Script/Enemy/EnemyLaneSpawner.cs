using System.Collections.Generic;
using UnityEngine;

// Generatore a corsie: una "strada" che va dal punto A al punto B, divisa in N
// corsie parallele. Per ogni corsia si dice quanti nemici la percorrono e a che
// velocita': compaiono in A, scorrono fino a B e ricompaiono in A.
//
// I nemici sono distribuiti a distanza costante e riciclati a fine corsa, senza
// un Instantiate/Destroy continuo: il numero per corsia resta esattamente
// quello impostato. Solo chi viene ucciso viene ricreato, dopo respawnDelay.
//
// Vengono generati in Awake e non in Start: cosi' si registrano in
// EnemyHealth.All prima che GameManager.Start conti i nemici del livello.
public class EnemyLaneSpawner : MonoBehaviour
{
    [System.Serializable]
    public class Lane
    {
        public int enemyCount = 3;
        public float speed = 2f;
        public bool reversed = false;         // scorre da B verso A
        [Range(0f, 1f)] public float phase;   // sfasa la corsia rispetto alle altre
        public GameObject prefabOverride;     // vuoto = enemyPrefab
    }

    [Header("Strada")]
    public Transform pointA;      // dove compaiono
    public Transform pointB;      // dove spariscono
    public float laneSpacing = 2f;
    public bool centerLanes = true;   // le corsie stanno a cavallo della linea A-B

    [Header("Corsie")]
    public Lane[] lanes = { new Lane() };

    [Header("Nemici")]
    public GameObject enemyPrefab;
    public bool faceTravelDirection = true;
    public float respawnDelay = 3f;   // < 0 = i nemici uccisi non tornano

    private class Runner
    {
        public GameObject go;
        public EnemyHealth health;
        public Rigidbody rb;
        public float t;             // 0 = punto A della corsia, 1 = punto B
        public float respawnTimer;
        public bool driving;        // false = morto, in attesa di tornare
    }

    private readonly List<List<Runner>> runners = new List<List<Runner>>();

    void Awake()
    {
        if (pointA == null || pointB == null)
        {
            Debug.LogWarning("EnemyLaneSpawner: mancano pointA o pointB, non genero niente.", this);
            return;
        }

        if (lanes == null) return;

        for (int i = 0; i < lanes.Length; i++)
        {
            List<Runner> list = new List<Runner>();
            int n = Mathf.Max(0, lanes[i].enemyCount);

            for (int k = 0; k < n; k++)
            {
                // distribuiti a distanza costante lungo la corsia
                float t = Mathf.Repeat((float)k / n + lanes[i].phase, 1f);
                list.Add(Spawn(i, t));
            }

            runners.Add(list);
        }
    }

    void Start()
    {
        if (respawnDelay < 0f) return;

        GameManager gm = GameManager.Instance;
        if (gm != null && gm.requireAllEnemiesDead)
            Debug.LogWarning("EnemyLaneSpawner: i nemici ricompaiono ma il livello chiede di " +
                             "ucciderli tutti, quindi non si potra' mai vincere per obiettivi. " +
                             "Metti respawnDelay a -1, oppure spegni requireAllEnemiesDead e " +
                             "lascia la buca come via di vittoria.", this);
    }

    void FixedUpdate()
    {
        if (pointA == null || pointB == null) return;

        float length = Vector3.Distance(pointA.position, pointB.position);
        if (length < 0.001f) return;

        for (int i = 0; i < runners.Count; i++)
        {
            List<Runner> list = runners[i];
            float step = lanes[i].speed * Time.fixedDeltaTime / length;

            for (int k = 0; k < list.Count; k++)
            {
                Runner r = list[k];

                // rete di sicurezza: se il nemico e' stato distrutto senza
                // passare da onDied (ricarica, cancellazione a mano) me ne
                // accorgo qui
                if (r.driving && (r.go == null || r.health == null || r.health.IsDead))
                    ReleaseRunner(r);

                if (!r.driving)
                {
                    if (respawnDelay < 0f) continue;

                    r.respawnTimer -= Time.fixedDeltaTime;
                    if (r.respawnTimer > 0f) continue;

                    list[k] = Spawn(i, 0f);   // ricompare in A, come i nuovi
                    continue;
                }

                r.t = Mathf.Repeat(r.t + step, 1f);
                Move(r, i);
            }
        }
    }

    Runner Spawn(int laneIndex, float t)
    {
        Runner r = new Runner { t = t, respawnTimer = respawnDelay };

        GameObject prefab = PrefabFor(lanes[laneIndex]);
        if (prefab == null)
        {
            Debug.LogWarning($"EnemyLaneSpawner: la corsia {laneIndex} non ha un prefab.", this);
            return r;
        }

        Quaternion rotation = faceTravelDirection
            ? Quaternion.LookRotation(TravelDirection(laneIndex), Vector3.up)
            : prefab.transform.rotation;

        r.go = Instantiate(prefab, LanePoint(laneIndex, t), rotation, transform);
        r.health = r.go.GetComponent<EnemyHealth>();
        r.rb = r.go.GetComponent<Rigidbody>();

        // sui binari finche' e' vivo: un corpo dinamico spostato a mano
        // combatte col solver
        if (r.rb != null) r.rb.isKinematic = true;

        // lo libero nell'istante in cui muore, non al prossimo FixedUpdate:
        // l'onda d'urto che lo ha ucciso deve poterlo ancora spedire via
        if (r.health != null)
            r.health.onDied.AddListener(() => ReleaseRunner(r));

        r.driving = true;
        return r;
    }

    void ReleaseRunner(Runner r)
    {
        if (!r.driving) return;

        r.driving = false;
        r.respawnTimer = respawnDelay;

        if (r.rb != null) r.rb.isKinematic = false;

        r.go = null;
        r.health = null;
        r.rb = null;
    }

    void Move(Runner r, int laneIndex)
    {
        if (r.go == null) return;

        Vector3 position = LanePoint(laneIndex, r.t);

        if (r.rb != null && r.rb.isKinematic) r.rb.MovePosition(position);
        else r.go.transform.position = position;

        if (!faceTravelDirection) return;

        Quaternion rotation = Quaternion.LookRotation(TravelDirection(laneIndex), Vector3.up);
        if (r.rb != null && r.rb.isKinematic) r.rb.MoveRotation(rotation);
        else r.go.transform.rotation = rotation;
    }

    GameObject PrefabFor(Lane lane)
    {
        return lane.prefabOverride != null ? lane.prefabOverride : enemyPrefab;
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

    public Vector3 LanePoint(int laneIndex, float t)
    {
        Vector3 offset = Side() * LaneOffset(laneIndex);
        Vector3 a = pointA.position + offset;
        Vector3 b = pointB.position + offset;

        return lanes[laneIndex].reversed ? Vector3.Lerp(b, a, t) : Vector3.Lerp(a, b, t);
    }

    Vector3 TravelDirection(int laneIndex)
    {
        Vector3 road = pointB.position - pointA.position;
        if (lanes[laneIndex].reversed) road = -road;

        return road.sqrMagnitude < 0.0001f ? Vector3.forward : road.normalized;
    }

    void OnDrawGizmos()
    {
        if (pointA == null || pointB == null || lanes == null) return;

        for (int i = 0; i < lanes.Length; i++)
        {
            Vector3 start = LanePoint(i, 0f);
            Vector3 end = LanePoint(i, 1f);

            Gizmos.color = new Color(1f, 0.55f, 0.2f, 0.85f);
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(start, 0.25f);     // dove compaiono

            Gizmos.color = new Color(1f, 0.55f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(end, 0.25f);       // dove spariscono

            // anteprima delle posizioni di partenza, cosi' la spaziatura si
            // regola a occhio senza entrare in Play
            int n = Mathf.Max(0, lanes[i].enemyCount);
            Gizmos.color = new Color(1f, 0.75f, 0.35f, 0.8f);
            for (int k = 0; k < n; k++)
            {
                float t = Mathf.Repeat((float)k / n + lanes[i].phase, 1f);
                Gizmos.DrawWireCube(LanePoint(i, t), Vector3.one * 0.4f);
            }
        }
    }
}
