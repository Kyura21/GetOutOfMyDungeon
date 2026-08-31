using UnityEngine;
using UnityEngine.Events;

// Oggetto da colpire per aprire la WinHole. Si piazza in giro per il livello:
// serve a poter imbucare senza uccidere nessuno (pacifist run), quindi non ha
// niente a che vedere con nemici o esplosioni.
// Funziona sia come collider solido sia come trigger.
[RequireComponent(typeof(Collider))]
public class HoleOpener : MonoBehaviour
{
    [Header("Cosa lo attiva")]
    public bool onlySkull = true;      // false = lo apre qualunque cosa lo colpisca
    public float minImpactSpeed = 0f;  // 0 = basta sfiorarlo

    [Header("Reazione")]
    public GameObject triggeredVFX;
    public bool disableOnTriggered = false;

    [Header("Eventi")]
    public UnityEvent onTriggered;

    public bool IsTriggered { get; private set; }

    void OnCollisionEnter(Collision collision)
    {
        // relativeVelocity e' la velocita' d'impatto vera: la velocita' del
        // Rigidbody qui dentro sarebbe gia' quella di rimbalzo
        TryTrigger(collision.collider, collision.relativeVelocity.magnitude);
    }

    void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        TryTrigger(other, rb != null ? rb.linearVelocity.magnitude : 0f);
    }

    void TryTrigger(Collider other, float speed)
    {
        if (IsTriggered) return;
        if (speed < minImpactSpeed) return;

        // stessa convenzione di KillZone e WinHole: il teschio si riconosce
        // dal componente, non dal tag
        if (onlySkull && other.GetComponentInParent<SkullIntegrity>() == null) return;

        Trigger();
    }

    public void Trigger()
    {
        if (IsTriggered) return;
        IsTriggered = true;

        if (triggeredVFX != null)
            Destroy(Instantiate(triggeredVFX, transform.position, Quaternion.identity), 3f);

        onTriggered.Invoke();

        if (disableOnTriggered)
            gameObject.SetActive(false);
    }

    public void ResetOpener()
    {
        IsTriggered = false;
        gameObject.SetActive(true);
    }

    void OnDrawGizmos()
    {
        Collider c = GetComponent<Collider>();
        if (c == null) return;

        Gizmos.color = IsTriggered
            ? new Color(0.2f, 1f, 0.3f, 0.6f)
            : new Color(1f, 0.8f, 0.1f, 0.6f);
        Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
    }
}
