using UnityEngine;
using UnityEngine.Events;

// Nemico gelatinoso: non ti ferma con gli HP ma con la fisica, si mangia gran
// parte della velocita' del teschio. Punisce chi tira forte in mezzo al gruppo:
// il primo slime passa, il secondo no.
//
// Ha senso soprattutto con SkullExplosion.freezeSkullOnExplode SPENTO: se il
// teschio si ferma comunque a ogni esplosione, l'assorbimento non si vede.
public class EnemyAbsorber : MonoBehaviour
{
    [Header("Assorbimento")]
    [Range(0f, 1f)] public float speedKeep = 0.25f;  // frazione di velocita' che sopravvive
    public float minImpactSpeed = 1f;   // sotto, non tocco niente: eviterei di incollare la palla
    public bool absorbAngular = true;   // frena anche la rotazione, cosi' non continua a filare

    [Header("Eventi")]
    public UnityEvent onAbsorbed;

    void OnCollisionEnter(Collision collision)
    {
        // dentro il callback del nemico, collision.collider e' quello dell'altro
        if (collision.collider.GetComponentInParent<SkullIntegrity>() == null) return;
        if (collision.relativeVelocity.magnitude < minImpactSpeed) return;

        Rigidbody rb = collision.rigidbody;
        if (rb == null || rb.isKinematic) return;

        // qui la velocita' e' gia' quella di rimbalzo, ed e' proprio quella che
        // voglio scalare: l'assorbimento agisce su cosa esce dall'urto
        rb.linearVelocity *= speedKeep;
        if (absorbAngular) rb.angularVelocity *= speedKeep;

        onAbsorbed.Invoke();
    }
}
