using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Il corpo del nemico come oggetto fisico: appena incassa un colpo esce dai
// binari e diventa un birillo. Da quel momento rotola, ingombra, spinge gli
// altri e li danneggia urtandoli — le auto distrutte della modalita' Crash di
// Burnout, che restano in mezzo alla strada e innescano la carambola.
//
// Vale anche da vivo: non serve morire per diventare pericolosi.
[RequireComponent(typeof(Rigidbody))]
public class EnemyWreck : MonoBehaviour
{
    // ordinati per momento dell'urto: il piu' vecchio e' il primo
    public static readonly List<EnemyWreck> Active = new List<EnemyWreck>();

    [Header("Quando esce dai binari")]
    public bool knockOnDamage = true;   // basta un colpo incassato, non serve morire
    public bool knockOnDeath = true;

    [Header("Danno da urto ad altri nemici")]
    public int impactDamage = 1;
    public float minImpactSpeed = 4f;   // sotto, e' solo una spinta
    public float impactCooldown = 0.2f; // per non applicare danno a ogni contatto del rotolamento

    [Header("Punteggio")]
    public int impactScore = 50;        // punti Danni per l'urto in se'

    [Header("Durata")]
    public float lifetime = -1f;        // secondi. < 0 = resta finche' il tetto lo consente

    [Header("Eventi")]
    public UnityEvent onKnocked;

    public bool IsLoose { get; private set; }

    private Rigidbody rb;
    private EnemyHealth health;
    private float nextImpactTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<EnemyHealth>();
    }

    void OnEnable()
    {
        if (health == null) return;

        if (knockOnDamage) health.onDamaged.AddListener(Knock);
        if (knockOnDeath) health.onDied.AddListener(Knock);
    }

    void OnDisable()
    {
        if (health != null)
        {
            health.onDamaged.RemoveListener(Knock);
            health.onDied.RemoveListener(Knock);
        }

        Active.Remove(this);
    }

    // Lo chiamano gli eventi di EnemyHealth, e lo spawner quando spazza una
    // corsia. Idempotente: chi e' gia' libero resta libero.
    public void Knock()
    {
        if (IsLoose) return;
        IsLoose = true;

        // da qui in poi lo muove la fisica: chi lo teneva sui binari
        // (EnemyLaneSpawner, EnemyPatrol) si accorge da solo di averlo perso
        if (rb != null) rb.isKinematic = false;

        Active.Add(this);
        onKnocked.Invoke();

        // niente Update per contare il tempo: la distruzione ritardata la
        // gestisce gia' Unity
        if (lifetime >= 0f) Destroy(gameObject, lifetime);

        Prune();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!IsLoose || impactDamage <= 0) return;
        if (Time.time < nextImpactTime) return;
        if (collision.relativeVelocity.magnitude < minImpactSpeed) return;

        EnemyHealth other = collision.collider.GetComponentInParent<EnemyHealth>();
        if (other == null || other == health || other.IsDead) return;

        nextImpactTime = Time.time + impactCooldown;

        // il punteggio degli HP tolti lo aggiunge gia' TakeDamage: questo e' il
        // premio per l'urto in se', come i tamponamenti di Burnout
        if (impactScore > 0 && GameManager.Instance != null)
            GameManager.Instance.AddDamage(impactScore);

        other.TakeDamage(impactDamage);
    }

    // Tetto ai corpi liberi contemporanei: senza, una strada trafficata piena
    // di rottami diventa un simulatore di fisica. Si sacrifica il piu' vecchio,
    // che e' anche quello piu' probabilmente lontano dall'azione.
    static void Prune()
    {
        int limit = GameManager.Instance != null ? GameManager.Instance.maxWrecks : 0;
        if (limit <= 0) return;

        while (Active.Count > limit)
        {
            EnemyWreck oldest = Active[0];
            Active.RemoveAt(0);

            if (oldest != null) Destroy(oldest.gameObject);
        }
    }

    public void ResetWreck()
    {
        IsLoose = false;
        nextImpactTime = 0f;
        Active.Remove(this);
    }
}
