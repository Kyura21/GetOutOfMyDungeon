using UnityEngine;
using UnityEngine.Events;

// Pattugliamento su N punti impostabili.
//
// Muove il Rigidbody in kinematic con MovePosition: un corpo dinamico spostato
// a mano combatte col solver e vibra contro i muri, mentre uno kinematic fa
// comunque da ostacolo solido al teschio. Alla morte torna dinamico, cosi'
// l'onda d'urto lo spedisce via come gli altri.
[RequireComponent(typeof(Rigidbody))]
public class EnemyPatrol : MonoBehaviour
{
    public enum Mode { Loop, PingPong, Once }

    [Header("Percorso")]
    public Transform[] waypoints;
    public Mode mode = Mode.PingPong;
    public bool snapToFirstWaypoint = true;   // parte esattamente sul primo punto

    [Header("Movimento")]
    public float speed = 2f;
    public float waitAtWaypoint = 0.5f;
    public float arriveThreshold = 0.05f;

    [Header("Orientamento")]
    public bool faceMovement = true;
    public float turnSpeed = 720f;   // gradi al secondo, 0 = gira di scatto

    [Header("Reazione al colpo")]
    public float stunOnHit = 0f;     // secondi di stop dopo aver incassato un colpo

    [Header("Eventi")]
    public UnityEvent onWaypointReached;

    public bool IsPatrolling => !released && HasPath;
    public int CurrentWaypoint => index;

    private bool HasPath => waypoints != null && waypoints.Length > 0;

    private Rigidbody rb;
    private EnemyHealth health;
    private int index;
    private int direction = 1;
    private float waitTimer;
    private bool released;   // morto: la fisica se lo riprende
    private bool finished;   // modalita' Once arrivata in fondo

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        health = GetComponent<EnemyHealth>();
    }

    void OnEnable()
    {
        if (health == null) return;
        health.onDied.AddListener(Release);
        health.onDamaged.AddListener(Stun);
    }

    void OnDisable()
    {
        if (health == null) return;
        health.onDied.RemoveListener(Release);
        health.onDamaged.RemoveListener(Stun);
    }

    void Start()
    {
        if (!HasPath) return;

        // un waypoint figlio del nemico si muoverebbe insieme a lui e il
        // percorso scapperebbe in avanti all'infinito
        foreach (Transform w in waypoints)
            if (w != null && w.IsChildOf(transform))
                Debug.LogWarning($"EnemyPatrol: il waypoint '{w.name}' e' figlio del nemico, " +
                                 "spostalo fuori o il percorso si muovera' con lui.", this);

        if (snapToFirstWaypoint && waypoints[0] != null)
            rb.position = waypoints[0].position;

        index = waypoints.Length > 1 ? 1 : 0;
    }

    void FixedUpdate()
    {
        if (released || finished || !HasPath) return;

        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            return;
        }

        Transform target = waypoints[Mathf.Clamp(index, 0, waypoints.Length - 1)];
        if (target == null) return;

        Vector3 to = target.position - rb.position;
        float step = speed * Time.fixedDeltaTime;

        if (to.magnitude <= step + arriveThreshold)
        {
            rb.MovePosition(target.position);
            Advance();
            return;
        }

        Vector3 dir = to.normalized;
        rb.MovePosition(rb.position + dir * step);
        Turn(dir);
    }

    void Turn(Vector3 dir)
    {
        if (!faceMovement) return;

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion wanted = Quaternion.LookRotation(dir, Vector3.up);
        rb.MoveRotation(turnSpeed > 0f
            ? Quaternion.RotateTowards(rb.rotation, wanted, turnSpeed * Time.fixedDeltaTime)
            : wanted);
    }

    void Advance()
    {
        onWaypointReached.Invoke();
        waitTimer = waitAtWaypoint;

        int n = waypoints.Length;
        if (n <= 1) return;

        switch (mode)
        {
            case Mode.Loop:
                index = (index + 1) % n;
                break;

            case Mode.PingPong:
                if (index + direction < 0 || index + direction >= n)
                    direction = -direction;
                index += direction;
                break;

            case Mode.Once:
                if (index >= n - 1) finished = true;
                else index++;
                break;
        }
    }

    void Stun()
    {
        if (stunOnHit > 0f) waitTimer = Mathf.Max(waitTimer, stunOnHit);
    }

    // il nemico e' morto: smetto di guidarlo e lo restituisco alla fisica,
    // altrimenti resterebbe kinematic e l'esplosione non lo sposterebbe
    public void Release()
    {
        released = true;
        if (rb != null) rb.isKinematic = false;
    }

    public void ResetPatrol()
    {
        released = false;
        finished = false;
        direction = 1;
        waitTimer = 0f;
        if (rb != null) rb.isKinematic = true;

        if (HasPath && waypoints[0] != null)
        {
            transform.position = waypoints[0].position;
            index = waypoints.Length > 1 ? 1 : 0;
        }
    }

    void OnDrawGizmos()
    {
        if (!HasPath) return;

        Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.8f);

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawWireSphere(waypoints[i].position, 0.2f);

            Transform next = null;
            if (i + 1 < waypoints.Length) next = waypoints[i + 1];
            else if (mode == Mode.Loop) next = waypoints[0];

            if (next != null)
                Gizmos.DrawLine(waypoints[i].position, next.position);
        }
    }
}
