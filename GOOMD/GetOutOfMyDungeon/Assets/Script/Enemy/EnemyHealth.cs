using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemyHealth : MonoBehaviour
{
    // registro dei nemici vivi, letto dal GameManager per la vittoria
    public static readonly List<EnemyHealth> All = new List<EnemyHealth>();

    public static int AliveCount
    {
        get
        {
            int n = 0;
            foreach (EnemyHealth e in All)
                if (e != null && !e.IsDead) n++;
            return n;
        }
    }

    [Header("Vita")]
    public int maxHP = 3;

    [Header("Ricompensa")]
    public int powerReward = 1;          // valuta, da spendere nei potenziamenti

    [Header("Punteggio danni")]
    public int damageScorePerHP = 100;   // per ogni HP effettivamente tolto
    public int damageScoreOnKill = 250;  // bonus alla morte

    [Header("Morte")]
    public GameObject deathVFX;
    public float deathDelay = 0f;       // ritardo prima di sparire, per far vedere il VFX
    public bool destroyOnDeath = true;  // false = resta in scena ma disattivato

    [Header("Feedback colpo")]
    public bool flashOnHit = true;
    public Color flashColor = Color.red;
    public float flashDuration = 0.15f;

    [Header("Eventi")]
    public UnityEvent onDamaged;
    public UnityEvent onDied;

    public int CurrentHP { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    private Renderer[] renderers;
    private Color[] baseColors;
    private float flashTimer;

    void Awake()
    {
        CurrentHP = maxHP;

        renderers = GetComponentsInChildren<Renderer>();
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            baseColors[i] = renderers[i].material.color;
    }

    void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    void Update()
    {
        if (flashTimer <= 0f) return;

        flashTimer -= Time.deltaTime;
        float t = Mathf.Clamp01(flashTimer / flashDuration);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].material.color = Color.Lerp(baseColors[i], flashColor, t);
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        // conto gli HP tolti davvero: un colpo da 5 su un nemico con 2 HP
        // vale 2, altrimenti si gonfierebbe il punteggio sull'ultimo colpo
        int applied = Mathf.Min(CurrentHP, amount);
        CurrentHP -= applied;

        if (damageScorePerHP > 0 && GameManager.Instance != null)
            GameManager.Instance.AddDamage(applied * damageScorePerHP);

        onDamaged.Invoke();

        if (flashOnHit) flashTimer = flashDuration;

        if (CurrentHP == 0) Die();
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
    }

    void Die()
    {
        if (GameManager.Instance != null)
        {
            if (powerReward > 0) GameManager.Instance.AddPower(powerReward);
            if (damageScoreOnKill > 0) GameManager.Instance.AddDamage(damageScoreOnKill);
        }

        onDied.Invoke();

        if (deathVFX != null)
            Destroy(Instantiate(deathVFX, transform.position, Quaternion.identity), 2f);

        if (destroyOnDeath)
            Destroy(gameObject, deathDelay);
        else if (deathDelay <= 0f)
            gameObject.SetActive(false);
        else
            Invoke(nameof(Deactivate), deathDelay);
    }

    void Deactivate()
    {
        gameObject.SetActive(false);
    }

    public void ResetHealth()
    {
        CurrentHP = maxHP;
        flashTimer = 0f;
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].material.color = baseColors[i];
        gameObject.SetActive(true);
    }
}
