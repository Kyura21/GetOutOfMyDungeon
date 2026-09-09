using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Nemico esplosivo: morendo esplode a sua volta e puo' innescare i vicini.
// E' il moltiplicatore del punteggio Danni: piazzarli bene vale piu' che
// colpirli tutti.
//
// Si aggancia da solo a EnemyHealth.onDied, non serve collegare niente
// nell'Inspector. La catena si risolve tutta nello stesso frame: ogni nemico
// esplode una volta sola (HasExploded), quindi non puo' rimbalzare all'infinito.
[RequireComponent(typeof(EnemyHealth))]
public class EnemyExplosive : MonoBehaviour
{
    [Header("Esplosione")]
    public float radius = 3f;
    public int damage = 2;
    public float force = 20f;
    public float upwardsModifier = 0.4f;
    public LayerMask affectedLayers = ~0;

    [Header("Punteggio danni")]
    public int damageScore = 300;   // per l'esplosione in se': quello dei nemici colpiti
                                    // lo aggiunge gia' EnemyHealth.TakeDamage

    [Header("VFX")]
    public GameObject explosionVFX;
    public float destroyDelay = 2f;

    [Header("Eventi")]
    public UnityEvent onExploded;

    public bool HasExploded { get; private set; }

    private EnemyHealth health;

    void Awake()
    {
        health = GetComponent<EnemyHealth>();
    }

    void OnEnable()
    {
        if (health != null) health.onDied.AddListener(Explode);
    }

    void OnDisable()
    {
        if (health != null) health.onDied.RemoveListener(Explode);
    }

    // pubblica anche per poterla innescare da fuori (un puzzle, un timer)
    public void Explode()
    {
        if (HasExploded) return;
        HasExploded = true;

        Vector3 point = transform.position;

        if (explosionVFX != null)
            Destroy(Instantiate(explosionVFX, point, Quaternion.identity), destroyDelay);

        if (damageScore > 0 && GameManager.Instance != null)
            GameManager.Instance.AddDamage(damageScore);

        // insieme locale, non riusato fra un'esplosione e l'altra: nella catena
        // questa funzione rientra dentro se stessa, e un insieme condiviso
        // verrebbe svuotato dall'esplosione innescata
        HashSet<EnemyHealth> damaged = new HashSet<EnemyHealth>();

        Collider[] hits = Physics.OverlapSphere(point, radius, affectedLayers);
        foreach (Collider c in hits)
        {
            EnemyHealth other = c.GetComponentInParent<EnemyHealth>();
            if (other != null && other != health && !other.IsDead && damaged.Add(other))
            {
                EnemyArmor armor = other.GetComponent<EnemyArmor>();
                if (armor == null || !armor.BlocksSplashFrom(point))
                    other.TakeDamage(damage);
            }

            Rigidbody rb = c.attachedRigidbody;
            if (rb == null) continue;
            if (rb.isKinematic) continue;  // nemici ancora in pattuglia: non si spingono

            rb.AddExplosionForce(force, point, radius, upwardsModifier, ForceMode.Impulse);
        }

        onExploded.Invoke();
    }

    public void ResetExplosive()
    {
        HasExploded = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
