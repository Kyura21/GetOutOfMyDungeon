using UnityEngine;
using UnityEngine.Events;

// Buca di fine livello, come la buca di un tavolo da biliardo.
// Va su un collider in trigger. Resta CHIUSA finche' il teschio non e'
// esploso: prima di allora il teschio ci passa sopra e non succede niente.
[RequireComponent(typeof(Collider))]
public class WinHole : MonoBehaviour
{
    [Header("Quando si apre")]
    public bool requireSkullExploded = true;

    // Guardia contro il soft lock: se la buca si aprisse con i nemici ancora
    // vivi, imbucare il teschio non farebbe vincere ma lo toglierebbe dal
    // tavolo, lasciando il livello ingiocabile e non perso.
    public bool requireLevelObjectives = true;

    [Header("Riferimenti")]
    public SkullExplosion skull;   // se vuoto lo cerco in scena

    [Header("Cattura del teschio")]
    public bool snapToCenter = true;
    public bool freezeSkull = true;
    public bool hideSkull = false;

    [Header("Eventi")]
    public UnityEvent onOpened;     // la buca si apre: accendi luci, VFX, suono
    public UnityEvent onPotted;     // teschio imbucato
    public UnityEvent onRejected;   // teschio arrivato a buca ancora chiusa

    public bool IsOpen { get; private set; }
    public bool IsPotted { get; private set; }

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Awake()
    {
        if (skull == null) skull = FindFirstObjectByType<SkullExplosion>();
    }

    void Update()
    {
        // l'apertura e' a senso unico: una volta aperta resta aperta,
        // cosi' onOpened scatta una volta sola
        if (IsOpen || IsPotted) return;
        if (!ShouldOpen()) return;

        IsOpen = true;
        onOpened.Invoke();
    }

    bool ShouldOpen()
    {
        if (requireSkullExploded && (skull == null || !skull.HasEverExploded))
            return false;

        if (requireLevelObjectives && GameManager.Instance != null
            && !GameManager.Instance.AreLevelObjectivesMet())
            return false;

        return true;
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
