using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SkullExplosion : MonoBehaviour
{
    [Header("VFX")]
    public GameObject explosionVFX;
    public float destroyDelay = 2f;

    [Header("Parametri esplosione")]
    public float explosionForce = 700f;
    public float explosionRadius = 5f;
    public float upwardsModifier = 0.5f;   // spinge verso l'alto, 0 = solo radiale
    public LayerMask affectedLayers = ~0;  // cosa viene investito dall'onda d'urto

    [Header("Danno")]
    public int explosionDamage = 1;
    public bool damageFalloff = false;  // il danno cala allontanandosi dal centro

    [Header("Comportamento teschio")]
    public bool freezeSkullOnExplode = true;
    public bool hideSkullOnExplode = false;

    [Header("Eventi")]
    public UnityEvent onExploded;

    // esploso in questo tiro: si riarma a ogni Shoot()
    public bool HasExploded => hasExploded;

    // esploso almeno una volta in questo livello: NON si riarma tra un tiro
    // e l'altro, e' quello che apre la WinHole
    public bool HasEverExploded { get; private set; }

    private bool hasExploded;

    // riusato a ogni esplosione: evita di colpire due volte lo stesso nemico
    // quando ha piu' di un collider dentro il raggio
    private readonly HashSet<EnemyHealth> damaged = new HashSet<EnemyHealth>();

    void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        if (collision.gameObject.CompareTag("enemy"))
            Explode(collision.contacts[0].point);
    }

    public void Explode(Vector3 point)
    {
        hasExploded = true;
        HasEverExploded = true;

        if (explosionVFX != null)
        {
            GameObject vfx = Instantiate(explosionVFX, point, Quaternion.identity);
            Destroy(vfx, destroyDelay);
        }

        damaged.Clear();

        Collider[] hits = Physics.OverlapSphere(point, explosionRadius, affectedLayers);
        foreach (Collider c in hits)
        {
            EnemyHealth enemy = c.GetComponentInParent<EnemyHealth>();
            if (enemy != null && !enemy.IsDead && damaged.Add(enemy))
                enemy.TakeDamage(GetDamageAt(c, point));

            Rigidbody rb = c.attachedRigidbody;
            if (rb == null) continue;
            if (rb.gameObject == gameObject) continue; // non spinge se stesso

            rb.AddExplosionForce(explosionForce, point, explosionRadius, upwardsModifier, ForceMode.Impulse);
        }

        if (freezeSkullOnExplode)
        {
            Rigidbody myRb = GetComponent<Rigidbody>();
            if (myRb != null)
            {
                myRb.linearVelocity = Vector3.zero;
                myRb.angularVelocity = Vector3.zero;
            }
        }

        if (hideSkullOnExplode)
            gameObject.SetActive(false);

        onExploded.Invoke();
    }

    int GetDamageAt(Collider c, Vector3 point)
    {
        if (!damageFalloff || explosionRadius <= 0f) return explosionDamage;

        float dist = Vector3.Distance(point, c.ClosestPoint(point));
        float t = Mathf.Clamp01(1f - dist / explosionRadius);
        return Mathf.Max(1, Mathf.RoundToInt(explosionDamage * t));
    }

    // riarma il teschio per il tiro successivo. Non tocca HasEverExploded:
    // una volta esplosa, la buca resta aperta per il resto del livello.
    public void ResetExplosion()
    {
        hasExploded = false;
        gameObject.SetActive(true);
    }

    // azzera anche la memoria di livello: da usare quando si ricomincia
    public void ResetForLevel()
    {
        HasEverExploded = false;
        ResetExplosion();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}