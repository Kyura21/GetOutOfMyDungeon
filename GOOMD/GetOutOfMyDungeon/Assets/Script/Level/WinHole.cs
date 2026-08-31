using UnityEngine;
using UnityEngine.Events;

// Buca di fine livello, come la buca di un tavolo da biliardo.
// Va su un collider in trigger.
//
// E' una via di vittoria PARALLELA, non un requisito: imbucare vince il
// livello anche senza aver ucciso nessuno o risolto puzzle. Cambia solo il
// punteggio finale, non l'esito.
//
// Resta chiusa finche' non vengono colpiti gli HoleOpener elencati qui sotto.
[RequireComponent(typeof(Collider))]
public class WinHole : MonoBehaviour
{
    [Header("Quando si apre")]
    public HoleOpener[] requiredOpeners;      // vuoto = aperta fin dall'inizio
    public bool requireAllOpeners = true;     // false = ne basta uno qualsiasi

    [Header("Cattura del teschio")]
    public bool snapToCenter = true;
    public bool freezeSkull = true;
    public bool hideSkull = false;

    [Header("Eventi")]
    public UnityEvent onOpened;     // la buca si apre: luci, VFX, suono
    public UnityEvent onPotted;     // teschio imbucato
    public UnityEvent onRejected;   // teschio arrivato a buca ancora chiusa

    public bool IsOpen { get; private set; }
    public bool IsPotted { get; private set; }

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Update()
    {
        // apertura a senso unico: una volta aperta resta aperta, cosi'
        // onOpened scatta una volta sola
        if (IsOpen || IsPotted) return;
        if (!ShouldOpen()) return;

        Open();
    }

    bool ShouldOpen()
    {
        if (requiredOpeners == null || requiredOpeners.Length == 0) return true;

        bool anyTriggered = false;
        foreach (HoleOpener o in requiredOpeners)
        {
            if (o == null) continue;

            if (o.IsTriggered) anyTriggered = true;
            else if (requireAllOpeners) return false;
        }

        return requireAllOpeners || anyTriggered;
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        onOpened.Invoke();
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsPotted) return;

        // stessa convenzione della KillZone: riconosco il teschio dal
        // componente, non dal tag
        SkullIntegrity s = other.GetComponentInParent<SkullIntegrity>();
        if (s == null) return;

        if (!IsOpen)
        {
            onRejected.Invoke();
            return;
        }

        Pot(s.gameObject);
    }

    void Pot(GameObject skullObject)
    {
        IsPotted = true;

        if (freezeSkull)
        {
            Rigidbody rb = skullObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }

        if (snapToCenter)
            skullObject.transform.position = transform.position;

        if (hideSkull)
            skullObject.SetActive(false);

        onPotted.Invoke();
        // la vittoria la dichiara il GameManager: qui mi limito a IsPotted,
        // cosi' c'e' un solo posto che decide se il livello e' vinto
    }

    public void ResetHole()
    {
        IsOpen = false;
        IsPotted = false;
    }

    void OnDrawGizmos()
    {
        Collider c = GetComponent<Collider>();
        if (c == null) return;

        Gizmos.color = IsPotted
            ? Color.cyan
            : (IsOpen ? new Color(0.2f, 1f, 0.3f, 0.5f) : new Color(1f, 1f, 1f, 0.2f));
        Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
    }
}
