using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemyHealth : MonoBehaviour
{
    public enum DeathBehaviour
    {
        Destroy,     // sparisce
        Deactivate,  // resta in scena ma spento, pronto per essere riusato
        Wreck        // il corpo resta come rottame fisico: vuole un EnemyWreck
    }

    // registro dei nemici vivi, letto dal GameManager per la vittoria
    public static readonly List<EnemyHealth> All = new List<EnemyHealth>();

    // Contatore tenuto aggiornato invece che ricalcolato: lo leggono ogni
    // frame sia il GameManager sia l'HUD, e scorrere la lista con centinaia
    // di nemici in scena costerebbe due passate complete al frame.
    public static int AliveCount { get; private set; }

    [Header("Vita")]
    public int maxHP = 3;

    [Header("Ricompensa")]
    public int powerReward = 1;          // valuta, da spendere nei potenziamenti

    [Header("Punteggio danni")]
    public int damageScorePerHP = 100;   // per ogni HP effettivamente tolto
    public int damageScoreOnKill = 250;  // bonus alla morte

    [Header("Morte")]
    public DeathBehaviour deathBehaviour = DeathBehaviour.Destroy;
    public GameObject deathVFX;
    public float deathDelay = 0f;       // ritardo prima di sparire, per far vedere il VFX

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
    private MaterialPropertyBlock block;
    private Coroutine flashRoutine;

    // posizione nel registro: serve a togliersi in tempo costante invece di
    // farsi cercare dentro una lista lunga
    private int index = -1;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    void Awake()
    {
        CurrentHP = maxHP;

        renderers = GetComponentsInChildren<Renderer>();
        baseColors = new Color[renderers.Length];
        block = new MaterialPropertyBlock();

        // sharedMaterial e non material: leggere .material creerebbe una copia
        // del materiale per ogni nemico, mandando a monte il batching
        for (int i = 0; i < renderers.Length; i++)
        {
            Material m = renderers[i].sharedMaterial;
            baseColors[i] = m != null ? m.color : Color.white;
        }
    }

    void OnEnable()
    {
        if (index >= 0) return;

        index = All.Count;
        All.Add(this);
        if (!IsDead) AliveCount++;
    }

    void OnDisable()
    {
        if (index < 0) return;

        // rimozione a scambio: sposto l'ultimo al mio posto invece di far
        // scorrere tutta la coda
        int last = All.Count - 1;
        All[index] = All[last];
        if (All[index] != null) All[index].index = index;
        All.RemoveAt(last);

        index = -1;
        if (!IsDead) AliveCount--;
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

        if (flashOnHit && gameObject.activeInHierarchy)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        if (CurrentHP == 0) Die();
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
    }

    void Die()
    {
        if (index >= 0) AliveCount--;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddKill();
            if (powerReward > 0) GameManager.Instance.AddPower(powerReward);
            if (damageScoreOnKill > 0) GameManager.Instance.AddDamage(damageScoreOnKill);
        }

        onDied.Invoke();

        if (deathVFX != null)
            Destroy(Instantiate(deathVFX, transform.position, Quaternion.identity), 2f);

        switch (deathBehaviour)
        {
            case DeathBehaviour.Destroy:
                Destroy(gameObject, deathDelay);
                break;

            case DeathBehaviour.Deactivate:
                if (deathDelay <= 0f) gameObject.SetActive(false);
                else Invoke(nameof(Deactivate), deathDelay);
                break;

            case DeathBehaviour.Wreck:
                // il corpo resta in scena come ostacolo: a renderlo fisico ci
                // pensa EnemyWreck, che gestisce anche quanto a lungo vivra'
                break;
        }
    }

    void Deactivate()
    {
        gameObject.SetActive(false);
    }

    // Nessun Update: il lampeggio gira solo mentre serve. Con centinaia di
    // nemici in scena, un Update per ognuno si paga anche quando non fa niente.
    IEnumerator FlashRoutine()
    {
        float timer = flashDuration;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            float t = flashDuration > 0f ? Mathf.Clamp01(timer / flashDuration) : 0f;

            for (int i = 0; i < renderers.Length; i++)
                Tint(i, Color.Lerp(baseColors[i], flashColor, t));

            yield return null;
        }

        RestoreColors();
        flashRoutine = null;
    }

    // MaterialPropertyBlock invece di renderer.material: cambia il colore senza
    // istanziare un materiale per nemico
    void Tint(int i, Color color)
    {
        if (renderers[i] == null) return;

        renderers[i].GetPropertyBlock(block);
        block.SetColor(BaseColorId, color);   // URP Lit
        block.SetColor(ColorId, color);       // shader vecchi
        renderers[i].SetPropertyBlock(block);
    }

    void RestoreColors()
    {
        for (int i = 0; i < renderers.Length; i++)
            Tint(i, baseColors[i]);
    }

    public void ResetHealth()
    {
        // se era gia' registrato ed era morto, torna a contare fra i vivi;
        // se era spento ci pensera' OnEnable qui sotto
        bool revived = index >= 0 && IsDead;
        CurrentHP = maxHP;
        if (revived) AliveCount++;

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }
        RestoreColors();

        gameObject.SetActive(true);
    }
}
