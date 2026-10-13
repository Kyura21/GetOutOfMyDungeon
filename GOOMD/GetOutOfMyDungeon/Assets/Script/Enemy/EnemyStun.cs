using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Lo stordimento: quello che fa la mazza a un nemico. Non toglie vita.
//
// Il colpo lo butta giu': il corpo diventa fisico, vola, rotola e si ferma.
// Dopo stunDuration si rialza, si raddrizza e torna a fare quello che faceva.
// La palla resta l'arma vera: la mazza serve solo a togliersi un nemico di torno.
//
// Il nemico oggi e' un cubo, quindi il "ragdoll" e' il Rigidbody intero lasciato
// alla fisica. Con un modello con le ossa questo e' il punto dove accendere il
// ragdoll vero.
//
// Chi muove il nemico (EnemyPatrol, EnemyLaneSpawner) legge IsDown e si ferma
// finche' e' vero. Se il nemico muore o esce dai binari (EnemyWreck) mentre e' a
// terra, lo stordimento si interrompe e il corpo resta alla fisica.
//
// Se il nemico non ha questo componente lo aggiunge EnemyHealth all'avvio, con i
// valori di default. Mettilo sul prefab per tararlo.
[RequireComponent(typeof(Rigidbody))]
public class EnemyStun : MonoBehaviour
{
    [Header("A terra")]
    public float stunDuration = 2f;     // secondi a terra prima di rialzarsi
    public float settleSpeed = 0.5f;    // si rialza solo sotto questa velocita'...
    public float maxExtraWait = 2f;     // ...ma dopo questi secondi in piu' si rialza comunque

    [Header("Caduta")]
    public float tumbleSpin = 6f;       // rotazione data dal colpo, per farlo cadere. 0 = solo spinta

    [Header("Rialzarsi")]
    public float getUpTime = 0.5f;      // secondi per tornare dritto

    [Header("Eventi")]
    public UnityEvent onStunned;
    public UnityEvent onRecovered;

    // a terra o mentre si rialza: chi lo muove deve aspettare
    public bool IsDown { get; private set; }

    // morto o rottame: non si stordisce, la fisica e' gia' sua
    public bool CanStun => (health == null || !health.IsDead) && (wreck == null || !wreck.IsLoose);

    private Rigidbody rb;
    private EnemyHealth health;
    private EnemyWreck wreck;
    private bool wasKinematic;
    private Coroutine routine;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<EnemyHealth>();
        wreck = GetComponent<EnemyWreck>();
    }

    void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        IsDown = false;
    }

    // Lo chiama la mazza. "impulse" e' la spinta del colpo. "durationMultiplier"
    // allunga il tempo a terra: lo swing caricato al 100% o oltre lo tiene giu' di piu'.
    public void Stun(Vector3 impulse, float durationMultiplier = 1f)
    {
        if (!CanStun)
        {
            // morto o rottame: niente stordimento, solo la spinta
            if (!rb.isKinematic) rb.AddForce(impulse, ForceMode.Impulse);
            return;
        }

        // colpito di nuovo mentre era gia' giu': ricomincia il conto, ma lo stato
        // da ripristinare resta quello di prima del primo colpo
        if (!IsDown) wasKinematic = rb.isKinematic;
        if (routine != null) StopCoroutine(routine);

        IsDown = true;
        rb.isKinematic = false;
        rb.AddForce(impulse, ForceMode.Impulse);

        // lo fa ribaltare in avanti, nel verso della spinta
        Vector3 flat = new Vector3(impulse.x, 0f, impulse.z);
        if (tumbleSpin > 0f && flat.sqrMagnitude > 0.0001f)
            rb.AddTorque(Vector3.Cross(Vector3.up, flat.normalized) * tumbleSpin, ForceMode.VelocityChange);

        onStunned.Invoke();
        routine = StartCoroutine(StunRoutine(stunDuration * Mathf.Max(0f, durationMultiplier)));
    }

    IEnumerator StunRoutine(float duration)
    {
        WaitForFixedUpdate step = new WaitForFixedUpdate();

        // 1. a terra: aspetta il tempo minimo, poi che si fermi
        float timer = 0f;
        while (timer < duration
               || (rb.linearVelocity.magnitude > settleSpeed && timer < duration + maxExtraWait))
        {
            if (!CanStun) { Abort(); yield break; }

            timer += Time.fixedDeltaTime;
            yield return step;
        }

        // 2. si rialza: torna dritto, girato dove guardava
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        Quaternion from = rb.rotation;
        Quaternion to = UprightRotation(from);

        float t = 0f;
        while (t < 1f)
        {
            if (!CanStun) { Abort(); yield break; }

            t += Time.fixedDeltaTime / Mathf.Max(0.01f, getUpTime);
            float eased = t * t * (3f - 2f * Mathf.Clamp01(t));
            rb.MoveRotation(Quaternion.Slerp(from, to, Mathf.Clamp01(eased)));
            yield return step;
        }

        rb.isKinematic = wasKinematic;
        IsDown = false;
        routine = null;
        onRecovered.Invoke();
    }

    // Dritto, con lo yaw piu' vicino a come e' caduto. Se e' finito a faccia in giu'
    // il suo avanti punta in verticale, e allora uso il suo alto appiattito.
    static Quaternion UprightRotation(Quaternion current)
    {
        Vector3 forward = current * Vector3.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.01f)
        {
            forward = current * Vector3.up;
            forward.y = 0f;
        }

        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    // Morto o rottame mentre era giu': il corpo resta alla fisica.
    void Abort()
    {
        rb.isKinematic = false;
        IsDown = false;
        routine = null;
    }

    public void ResetStun()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        IsDown = false;
    }
}
