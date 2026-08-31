using UnityEngine;
using UnityEngine.Events;

// Integrita' del teschio: si consuma rotolando, non con le esplosioni.
// SkullExplosion infatti danneggia solo gli EnemyHealth, mai il teschio.
[RequireComponent(typeof(Rigidbody))]
public class SkullIntegrity : MonoBehaviour
{
    [Header("Integrita'")]
    public float maxIntegrity = 100f;

    [Header("Usura da rotolamento")]
    public float wearPerMeter = 1f;   // quanta integrita' costa un metro percorso
    public float fragility = 1f;      // 0 = usura costante, 1 = a fine vita consuma il doppio

    [Header("Usura da urti")]
    public float wearPerImpact = 0f;  // costo di un urto frontale, 0 = solo rotolamento
    public float minImpactSpeed = 2f;

    [Header("Eventi")]
    public UnityEvent onWear;
    public UnityEvent onBroken;

    public float Current { get; private set; }
    public float Ratio => maxIntegrity > 0f ? Current / maxIntegrity : 0f;
    public bool IsBroken => Current <= 0f;

    private Rigidbody rb;
    private Vector3 lastVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Current = maxIntegrity;
    }

    void FixedUpdate()
    {
        if (IsBroken) return;

        Vector3 v = rb.linearVelocity;

        // solo il moto orizzontale: una caduta non e' rotolamento
        float distance = new Vector2(v.x, v.z).magnitude * Time.fixedDeltaTime;
        if (distance > 0f && wearPerMeter > 0f)
            ApplyWear(distance * wearPerMeter);

        lastVelocity = v;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (IsBroken || wearPerImpact <= 0f) return;

        float speed = lastVelocity.magnitude;
        if (speed < minImpactSpeed) return;

        // come per la frenata: un urto di striscio incide molto meno di uno in pieno
        Vector3 normal = collision.GetContact(0).normal;
        float headOn = Mathf.Clamp01(Vector3.Dot(-lastVelocity / speed, normal));

        ApplyWear(wearPerImpact * headOn);
    }

    public void ApplyWear(float amount)
    {
        if (IsBroken || amount <= 0f) return;

        // piu' e' consumato, piu' in fretta si consuma: "sempre piu' fragile"
        Current -= amount * Mathf.Lerp(1f, 1f + fragility, 1f - Ratio);
        onWear.Invoke();

        if (Current <= 0f)
        {
            Current = 0f;
            Break();
        }
    }

    void Break()
    {
        onBroken.Invoke();

        if (GameManager.Instance != null)
            GameManager.Instance.Lose("Il teschio si e' rotto");
    }

    public void ResetIntegrity()
    {
        Current = maxIntegrity;
    }
}
