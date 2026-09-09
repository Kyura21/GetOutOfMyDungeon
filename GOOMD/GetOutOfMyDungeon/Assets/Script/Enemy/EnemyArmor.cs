using UnityEngine;
using UnityEngine.Events;

// Corazza: decide se un colpo ferisce il nemico o viene respinto.
// Due regole indipendenti, entrambe disattivabili:
//   - direzionale: i colpi che arrivano sul lato corazzato non passano
//   - a soglia:    i colpi troppo lenti non passano da nessun lato
//
// Un colpo bloccato NON fa esplodere il teschio: rimbalza come su una sponda e
// resta in gioco. Il controllo lo fa SkullExplosion prima di esplodere, non
// questo componente: se lo facessimo qui, l'ordine dei due OnCollisionEnter
// sarebbe casuale e ogni tanto il teschio esploderebbe lo stesso.
public class EnemyArmor : MonoBehaviour
{
    [Header("Corazza direzionale")]
    public bool directional = true;
    public Transform armorFacing;                     // vuoto = questo transform
    [Range(0f, 180f)] public float armorHalfAngle = 70f;  // semiapertura del cono corazzato

    [Header("Soglia di velocita'")]
    public float minBreakSpeed = 0f;   // sotto questa velocita' d'impatto il colpo non passa

    [Header("Rimbalzo del colpo bloccato")]
    [Range(0f, 2f)] public float bounceKeep = 0.9f;  // 1 = sponda perfetta, >1 accelera
    public float minBounceSpeed = 2f;  // sotto, lascio fare a PhysX: rilanciarla la farebbe scattare

    [Header("Anche contro le esplosioni")]
    public bool blocksSplash = false;  // la corazza para pure l'onda d'urto dal suo lato

    [Header("Eventi")]
    public UnityEvent onBlocked;

    public Vector3 Facing => (armorFacing != null ? armorFacing : transform).forward;

    // La normale del contatto punta dal nemico verso il teschio: coincide con la
    // direzione in cui "guarda" la superficie colpita. Se quella direzione cade
    // nel cono corazzato, il colpo e' arrivato sullo scudo.
    public bool BlocksContact(Vector3 contactNormal, float impactSpeed)
    {
        if (impactSpeed < minBreakSpeed) return true;
        if (!directional) return false;

        return Vector3.Angle(contactNormal, Facing) <= armorHalfAngle;
    }

    public bool BlocksSplashFrom(Vector3 explosionPoint)
    {
        if (!blocksSplash) return false;
        if (!directional) return true;

        Vector3 toBlast = explosionPoint - transform.position;
        if (toBlast.sqrMagnitude < 0.0001f) return false;  // esplosione addosso: non si para

        return Vector3.Angle(toBlast.normalized, Facing) <= armorHalfAngle;
    }

    public void Block(Rigidbody skullRb, Vector3 incomingVelocity, Vector3 contactNormal)
    {
        onBlocked.Invoke();

        if (skullRb == null || skullRb.isKinematic) return;
        if (bounceKeep <= 0f) return;

        float speed = incomingVelocity.magnitude;
        if (speed < minBounceSpeed) return;

        // PhysX ha gia' risolto l'urto e con Bounciness 0 ha azzerato la
        // componente normale: qui riscrivo la velocita' di sana pianta
        skullRb.linearVelocity = Vector3.Reflect(incomingVelocity, contactNormal) * bounceKeep;
    }

    void OnDrawGizmosSelected()
    {
        Transform facing = armorFacing != null ? armorFacing : transform;
        Vector3 origin = transform.position;
        float length = 1.5f;

        Gizmos.color = new Color(0.35f, 0.65f, 1f, 0.9f);
        Gizmos.DrawRay(origin, facing.forward * length);

        if (!directional) return;

        Gizmos.color = new Color(0.35f, 0.65f, 1f, 0.45f);
        Gizmos.DrawRay(origin, Quaternion.AngleAxis(-armorHalfAngle, Vector3.up) * facing.forward * length);
        Gizmos.DrawRay(origin, Quaternion.AngleAxis(armorHalfAngle, Vector3.up) * facing.forward * length);
    }
}
