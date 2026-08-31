using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public enum GameState { Playing, Won, Lost }

    public static GameManager Instance { get; private set; }

    [Header("Riferimenti")]
    public SkullIntegrity skull;   // se vuoto lo cerco in scena

    [Header("Condizioni di vittoria")]
    public bool requireAllEnemiesDead = true;
    public int requiredPower = 0;
    public PuzzleTrigger[] requiredPuzzles;

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
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (!IsPlaying) return;

        if (skull != null && skull.transform.position.y < fallY)
        {
            Lose("Caduto fuori dal livello");
            return;
        }

        if (CheckObjectives()) Win();
    }

    bool CheckObjectives()
    {
        if (requireAllEnemiesDead && EnemyHealth.AliveCount > 0) return false;
        if (Power < requiredPower) return false;

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
