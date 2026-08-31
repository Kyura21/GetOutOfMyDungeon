using UnityEngine;

// Volume di morte istantanea: lago di lava, burroni, fuori mappa.
// Va su un collider in trigger che copre la zona.
[RequireComponent(typeof(Collider))]
public class KillZone : MonoBehaviour
{
    public string reason = "Caduto nella lava";

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // identifico il teschio dal componente, cosi' non servono tag
        if (other.GetComponentInParent<SkullIntegrity>() == null) return;

        if (GameManager.Instance != null)
            GameManager.Instance.Lose(reason);
    }
}
