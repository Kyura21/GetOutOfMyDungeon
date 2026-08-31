using UnityEngine;
using UnityEngine.Events;

// Segnaposto per i puzzle: qualsiasi meccanismo (leva, piastra, sequenza)
// chiama Solve() e il GameManager lo conta fra gli obiettivi del livello.
public class PuzzleTrigger : MonoBehaviour
{
    public string puzzleName = "Puzzle";

    [Header("Ricompensa")]
    public int powerReward = 0;

    [Header("Eventi")]
    public UnityEvent onSolved;

    public bool IsSolved { get; private set; }

    public void Solve()
    {
        if (IsSolved) return;
        IsSolved = true;

        if (powerReward > 0 && GameManager.Instance != null)
            GameManager.Instance.AddPower(powerReward);

        onSolved.Invoke();
    }

    public void ResetPuzzle()
    {
        IsSolved = false;
    }
}
