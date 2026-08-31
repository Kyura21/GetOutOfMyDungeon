using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SkullRolling : MonoBehaviour
{
    [Header("Scivolamento")]
    public float rollingDamping = 0.15f;  // attrito col terreno: basso = superficie liscia
    public float stopThreshold = 0.4f;    // sotto questa velocita' si ferma del tutto

    [Header("Perdita di velocita' negli urti")]
    [Range(0f, 1f)] public float collisionSpeedLoss = 0.35f; // frazione persa in un urto frontale
    public float minImpactSpeed = 1f;     // sotto questa velocita' l'urto non conta

    public bool IsStopped => rb.linearVelocity.sqrMagnitude < stopThreshold * stopThreshold;

    private Rigidbody rb;
    private Vector3 lastVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        Vector3 v = rb.linearVelocity;

        if (v.sqrMagnitude < stopThreshold * stopThreshold)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            lastVelocity = Vector3.zero;
            return;
        }

        if (rollingDamping > 0f)
        {
            // solo la componente orizzontale, cosi' una caduta resta libera
            float k = Mathf.Clamp01(1f - rollingDamping * Time.fixedDeltaTime);
            v = new Vector3(v.x * k, v.y, v.z * k);
            rb.linearVelocity = v;
            rb.angularVelocity *= k;
        }

        // i callback di collisione arrivano dopo la simulazione, quando la
        // velocita' e' gia' quella di rimbalzo: me la salvo prima
        lastVelocity = v;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collisionSpeedLoss <= 0f) return;

        float speed = lastVelocity.magnitude;
        if (speed < minImpactSpeed) return;

        // un urto frontale toglie tutta la frazione, uno di striscio quasi niente:
        // cosi' rotolare lungo il terreno o sfiorare un muro non frena la palla
        Vector3 normal = collision.GetContact(0).normal;
        float headOn = Mathf.Clamp01(Vector3.Dot(-lastVelocity / speed, normal));

        rb.linearVelocity *= 1f - collisionSpeedLoss * headOn;
    }
}
