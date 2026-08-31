using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public enum GameState { Playing, Won, Lost }

    public static GameManager Instance { get; private set; }

    [Header("Riferimenti")]
    public SkullIntegrity skull;   // se vuoto lo cerco in scena
    public WinHole winHole;        // idem

    [Header("Condizioni di vittoria")]
    public bool requireAllEnemiesDead = true;
    public PuzzleTrigger[] requiredPuzzles;
    public bool requireWinHole = true;   // il livello finisce imbucando il teschio

    [Header("Condizioni di sconfitta")]
    public float fallY = -20f;     // rete di sicurezza sotto il livello

    [Header("Eventi")]
    public UnityEvent onWin;
    public UnityEvent onLose;

    public GameState State { get; private set; } = GameState.Playing;
    public bool IsPlaying => State == GameState.Playing;
    public int Power { get; private set; }
    public string LastLoseReason { get; private set; }

    void Awake()
    {
        Instance = this;
        if (skull == null) skull = FindFirstObjectByType<SkullIntegrity>();
        if (winHole == null) winHole = FindFirstObjectByType<WinHole>();

        if (requireWinHole && winHole == null)
            Debug.LogWarning("GameManager: nessuna WinHole in scena, il livello non " +
                             "puo' essere vinto. Aggiungine una o togli Require Win Hole.", this);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!IsPlaying) return;

        // il teschio imbucato non "cade": se la buca e' incassata nel terreno
        // la rete di sicurezza scatterebbe proprio sulla vittoria
        bool potted = winHole != null && winHole.IsPotted;

        if (!potted && skull != null && skull.transform.position.y < fallY)
        {
            Lose("Caduto fuori dal livello");
            return;
        }

        if (CheckObjectives()) Win();
    }

    // Obiettivi del livello esclusa la buca. Li legge anche la WinHole per
    // sapere se aprirsi: la buca e' l'ultimo gesto, non uno dei tanti.
    public bool AreLevelObjectivesMet()
    {
        if (requireAllEnemiesDead && EnemyHealth.AliveCount > 0) return false;

        if (requiredPuzzles != null)
        {
            foreach (PuzzleTrigger p in requiredPuzzles)
                if (p != null && !p.IsSolved) return false;
        }

        return true;
    }

    bool CheckObjectives()
    {
        if (!AreLevelObjectivesMet()) return false;

        // il Power non e' un obiettivo: e' la valuta che il teschio accumula
        // uccidendo i nemici, da spendere nei potenziamenti
        if (requireWinHole) return winHole != null && winHole.IsPotted;

        return true;
    }

    public void AddPower(int amount)
    {
        if (amount <= 0) return;
        Power += amount;
    }

    public void Win()
    {
        if (!IsPlaying) return;
        State = GameState.Won;
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
