using UnityEngine;

// Frenata scriptata del teschio. PhysX non modella l'attrito di rotolamento,
// quindi una sfera scivola all'infinito a prescindere dal physic material:
// serve per forza codice.
//
// Da quando il tiro e' in stile golf il teschio VOLA, e ogni cosa qui dentro
// deve sapere se tocca terra o no:
//   - l'attrito di rotolamento non ha senso in volo (c'e' airDamping);
//   - l'azzeramento sotto stopThreshold guardava il modulo totale della
//     velocita', quindi in cima a un pallonetto ripido (dove la verticale si
//     annulla) congelava il teschio a mezz'aria;
//   - IsStopped dice a SkullAimController quando si puo' rimirare: senza il
//     contatto col terreno si poteva tirare di nuovo con la palla per aria.
[RequireComponent(typeof(Rigidbody))]
public class SkullRolling : MonoBehaviour
{
    [Header("Scivolamento")]
    public float rollingDamping = 0.15f;  // attrito col terreno: basso = superficie liscia
    public float stopThreshold = 0.4f;    // sotto questa velocita' si ferma del tutto

    [Header("Volo")]
    public float airDamping = 0f;         // attrito dell'aria: 0 = parabola pulita
    [Range(0f, 1f)] public float groundNormalMin = 0.5f; // quanto deve essere rivolto in su un
                                                         // contatto per contare come terreno:
                                                         // cosi' strisciare su un muro non vale
    public float groundGrace = 0.12f;     // quanto resta "a terra" dopo l'ultimo contatto,
                                          // per non tremolare sui dossi

    [Header("Perdita di velocita' negli urti")]
    // Attenzione: questa perdita si somma a quella che ha gia' applicato PhysX.
    // Con Bounciness 0 sui physic material il solver azzera da solo tutta la
    // componente normale, quindi tenerla alta significa frenare due volte.
    [Range(0f, 1f)] public float collisionSpeedLoss = 0.12f; // frazione persa in un urto frontale
    public float minImpactSpeed = 1f;     // sotto questa velocita' l'urto non conta

    // Un Rigidbody addormentato e' per forza appoggiato su qualcosa, e da
    // addormentato non riceve piu' OnCollisionStay: senza questo ramo il teschio
    // fermo risulterebbe "in volo" e non si potrebbe piu' mirare.
    public bool IsGrounded => rb.IsSleeping() || Time.time - lastGroundTime <= groundGrace;
    public bool IsAirborne => !IsGrounded;

    // fonte unica della soglia di "fermo": la legge anche SkullAimController
    public bool IsStopped => IsGrounded && rb.linearVelocity.sqrMagnitude < stopThreshold * stopThreshold;

    // velocita' in ingresso all'ultimo passo di fisica: dentro un callback di
    // collisione la velocita' del Rigidbody e' gia' quella di rimbalzo, quindi
    // chi deve pesare un urto legge questa (vedi EnemyArmor)
    public Vector3 LastVelocity => lastVelocity;

    private Rigidbody rb;
    private Vector3 lastVelocity;
    private float lastGroundTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // al primo frame nessun contatto e' ancora arrivato e il corpo non
        // dorme: senza questo il teschio nasce "in volo" e il primo tiro
        // sarebbe bloccato per una frazione di secondo
        lastGroundTime = Time.time;
    }

    void FixedUpdate()
    {
        // teschio imbucato dalla WinHole: e' kinematic, non lo muove piu' la
        // fisica. Scrivergli la velocita' non farebbe niente e Unity avvisa
        // a ogni frame.
        if (rb.isKinematic) return;

        Vector3 v = rb.linearVelocity;
        bool grounded = IsGrounded;

        // l'azzeramento vale solo a terra: in volo la velocita' passa per lo
        // zero in cima alla parabola, e fermare la palla li' la farebbe cadere
        // a piombo come se avesse sbattuto contro il nulla
        if (grounded && v.sqrMagnitude < stopThreshold * stopThreshold)
        {
            // solo se c'e' davvero qualcosa da azzerare: riscrivere la velocita'
            // a un corpo fermo lo risveglia a ogni passo e non si addormenta mai
            if (v != Vector3.zero || rb.angularVelocity != Vector3.zero)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            lastVelocity = Vector3.zero;
            return;
        }

        float damping = grounded ? rollingDamping : airDamping;
        if (damping > 0f)
        {
            // solo la componente orizzontale, cosi' una caduta resta libera
            float k = Mathf.Clamp01(1f - damping * Time.fixedDeltaTime);
            v = new Vector3(v.x * k, v.y, v.z * k);
            rb.linearVelocity = v;
            rb.angularVelocity *= k;
        }

        // i callback di collisione arrivano dopo la simulazione, quando la
        // velocita' e' gia' quella di rimbalzo: me la salvo prima
        lastVelocity = v;
    }

    void OnCollisionEnter(Collision collision)
    {
        NoteGroundContact(collision);

        if (rb.isKinematic) return;
        if (collisionSpeedLoss <= 0f) return;

        float speed = lastVelocity.magnitude;
        if (speed < minImpactSpeed) return;

        // un urto frontale toglie tutta la frazione, uno di striscio quasi niente:
        // cosi' rotolare lungo il terreno o sfiorare un muro non frena la palla
        Vector3 normal = collision.GetContact(0).normal;
        float headOn = Mathf.Clamp01(Vector3.Dot(-lastVelocity / speed, normal));

        rb.linearVelocity *= 1f - collisionSpeedLoss * headOn;
    }

    void OnCollisionStay(Collision collision)
    {
        NoteGroundContact(collision);
    }

    // Terreno = un contatto la cui normale guarda abbastanza in su. Un muro ha
    // normale orizzontale e non deve contare, altrimenti strisciare contro una
    // parete in volo rimetterebbe in funzione l'attrito di rotolamento.
    void NoteGroundContact(Collision collision)
    {
        int count = collision.contactCount;
        for (int i = 0; i < count; i++)
        {
            if (collision.GetContact(i).normal.y >= groundNormalMin)
            {
                lastGroundTime = Time.time;
                return;
            }
        }
    }
}
