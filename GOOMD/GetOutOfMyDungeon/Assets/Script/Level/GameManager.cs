using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public enum GameState { Playing, Won, Lost }

    // come si e' chiuso il livello: servira' al punteggio finale
    public enum WinReason { None, Objectives, WinHole, Manual }

    public static GameManager Instance { get; private set; }

    [Header("Riferimenti")]
    public SkullIntegrity skull;   // se vuoto lo cerco in scena
    public WinHole winHole;        // idem

    [Header("Obiettivi del livello (in AND fra loro)")]
    public bool requireAllEnemiesDead = true;
    public int requiredDamage = 0;             // 0 = il punteggio danni non e' un obiettivo
    public PuzzleTrigger[] requiredPuzzles;

    [Header("Punteggio danni")]
    public bool countDamageAfterWin = false;   // continua a contare a livello finito

    [Header("Condizioni di sconfitta")]
    public float fallY = -20f;     // rete di sicurezza sotto il livello

    [Header("Eventi")]
    public UnityEvent onWin;
    public UnityEvent onLose;
    public UnityEvent onDamageChanged;

    public GameState State { get; private set; } = GameState.Playing;
    public bool IsPlaying => State == GameState.Playing;

    // valuta: la guadagna il teschio uccidendo i nemici, si spendera' nei potenziamenti
    public int Power { get; private set; }

    // punteggio distruzione, stile modalita' Crash di Burnout
    public int Damage { get; private set; }

    public WinReason LastWinReason { get; private set; } = WinReason.None;
    public string LastLoseReason { get; private set; }

    private bool hasObjectives;

    void Awake()
    {
        Instance = this;
        if (skull == null) skull = FindFirstObjectByType<SkullIntegrity>();
        if (winHole == null) winHole = FindFirstObjectByType<WinHole>();
    }

    void Start()
    {
        // I nemici si registrano in OnEnable, quindi qui il conto e' completo.
        // Serve perche' "uccidi tutti i nemici" non e' un obiettivo se in
        // scena non ce n'e' nessuno: altrimenti il livello si vince da solo
        // al primo frame.
        int enemies = EnemyHealth.AliveCount;

        hasObjectives = (requireAllEnemiesDead && enemies > 0)
                        || requiredDamage > 0
                        || (requiredPuzzles != null && requiredPuzzles.Length > 0);

        if (!hasObjectives && winHole == null)
            Debug.LogWarning("GameManager: il livello non ha ne' obiettivi ne' WinHole, " +
                             "non si puo' vincere.", this);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!IsPlaying) return;

        bool potted = winHole != null && winHole.IsPotted;

        // il teschio imbucato non "cade": se la buca e' incassata nel terreno
        // la rete di sicurezza scatterebbe proprio sulla vittoria
        if (!potted && skull != null && skull.transform.position.y < fallY)
        {
            Lose("Caduto fuori dal livello");
            return;
        }

        // due strade indipendenti verso la vittoria: la buca vale da sola,
        // anche senza aver ucciso o risolto niente
        if (potted)
        {
            Win(WinReason.WinHole);
            return;
        }

        if (hasObjectives && AreLevelObjectivesMet())
            Win(WinReason.Objectives);
    }

    public bool AreLevelObjectivesMet()
    {
        if (requireAllEnemiesDead && EnemyHealth.AliveCount > 0) return false;
        if (Damage < requiredDamage) return false;

        if (requiredPuzzles != null)
        {
            foreach (PuzzleTrigger p in requiredPuzzles)
                if (p != null && !p.IsSolved) return false;
        }

        return true;
    }

    public void AddPower(int amount)
    {
        if (amount <= 0) return;
        Power += amount;
    }

    public void AddDamage(int amount)
    {
        if (amount <= 0) return;
        if (!IsPlaying && !countDamageAfterWin) return;

        Damage += amount;
        onDamageChanged.Invoke();
    }

    // overload senza argomenti per poterla agganciare a un UnityEvent nell'Inspector
    public void Win()
    {
        Win(WinReason.Manual);
    }

    public void Win(WinReason reason)
    {
        if (!IsPlaying) return;
        State = GameState.Won;
        LastWinReason = reason;
        onWin.Invoke();
    }

    public void Lose(string reason)
    {
        if (!IsPlaying) return;
        State = GameState.Lost;
        LastLoseReason = reason;
        onLose.Invoke();
    }
}
