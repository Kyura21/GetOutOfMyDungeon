using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// HUD di gioco. Legge lo stato e lo disegna: non decide niente, non modifica
// niente. Chi stabilisce se il livello e' vinto resta il GameManager.
//
// Tutti i riferimenti sono opzionali: quelli lasciati vuoti vengono saltati,
// cosi' si puo' costruire la UI un pezzo alla volta senza NullReference.
//
// Lo stato del livello viene letto ogni frame invece che agganciato a
// onWin/onLose: cosi' l'HUD funziona anche se lo si aggiunge a livello gia'
// avviato, e non c'e' niente da ricollegare nell'Inspector.
public class HUDController : MonoBehaviour
{
    [Header("Riferimenti di gioco (vuoti = li cerco in scena)")]
    public GameManager gameManager;
    public SkullIntegrity skull;
    public SkullAimController aim;

    [Header("Integrita' del teschio")]
    public Slider integrityBar;
    public Image integrityFill;          // colorato dal gradiente
    public Gradient integrityGradient;
    public TMP_Text integrityLabel;
    public string integrityFormat = "{0:0}%";
    public float integritySmooth = 8f;   // 0 = la barra salta al valore reale
    [Range(0f, 1f)] public float lowIntegrityThreshold = 0.25f;
    public float lowIntegrityPulseSpeed = 4f;  // 0 = niente lampeggio

    [Header("Barra di potenza")]
    public GameObject powerRoot;         // acceso solo mentre si carica il tiro
    public Slider powerBar;
    public Image powerFill;
    public Gradient powerGradient;
    public bool showWhileAiming = true;  // mostra la barra a zero gia' in mira
    public TMP_Text powerLabel;          // la percentuale di carica
    public string powerFormat = "{0:0}%";
    // la percentuale sale insieme al riempimento, accanto alla barra. Serve una
    // barra verticale e un'etichetta ancorata in basso (come la crea il generatore)
    public bool powerLabelFollowsFill = true;

    [Header("Angolo di tiro")]
    // L'angolo si sceglie con la rotella in mira. Il ventaglio col gizmo si
    // vede solo in editor, quindi in gioco serve un numero da leggere.
    public GameObject loftRoot;          // acceso solo mentre si mira o si carica
    public TMP_Text loftLabel;
    public string loftFormat = "{0:0} gradi";
    public Slider loftBar;               // opzionale: l'angolo sul ventaglio, 0..1

    [Header("Punteggi")]
    public TMP_Text damageText;
    public string damageFormat = "{0:n0}";
    public float damageRollSpeed = 6f;   // 0 = il contatore salta al valore
    public TMP_Text powerCurrencyText;
    public string powerCurrencyFormat = "{0}";

    [Header("Obiettivi")]
    public TMP_Text enemiesText;
    public string enemiesFormat = "{0} / {1}";
    public GameObject holeIndicator;     // acceso quando la buca si apre

    [Header("Fine livello")]
    public GameObject winPanel;
    public GameObject losePanel;
    public TMP_Text winReasonText;
    public TMP_Text winStatsText;
    public TMP_Text loseReasonText;
    public TMP_Text loseStatsText;
    public string statsFormat = "Danni {0:n0}   Power {1}";

    private float shownIntegrity = 1f;
    private float shownDamage;
    private GameManager.GameState lastState = GameManager.GameState.Playing;

    void Reset()
    {
        // gradienti di partenza sensati: rosso quando sta per rompersi,
        // verde da nuovo. Senza questo l'Inspector parte con un gradiente
        // bianco piatto e le barre sembrano rotte.
        integrityGradient = MakeGradient(
            new Color(0.85f, 0.15f, 0.15f),
            new Color(0.95f, 0.75f, 0.15f),
            new Color(0.25f, 0.8f, 0.35f));

        powerGradient = MakeGradient(
            new Color(0.95f, 0.85f, 0.3f),
            new Color(0.95f, 0.55f, 0.15f),
            new Color(0.9f, 0.2f, 0.15f));
    }

    void Awake()
    {
        if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        if (skull == null) skull = FindFirstObjectByType<SkullIntegrity>();
        if (aim == null) aim = FindFirstObjectByType<SkullAimController>();
    }

    void Start()
    {
        if (skull != null) shownIntegrity = skull.Ratio;
        if (gameManager != null) shownDamage = gameManager.Damage;

        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (powerRoot != null) powerRoot.SetActive(false);
        if (loftRoot != null) loftRoot.SetActive(false);
        if (holeIndicator != null) holeIndicator.SetActive(false);
    }

    void Update()
    {
        UpdateIntegrity();
        UpdatePower();
        UpdateLoft();
        UpdateScores();
        UpdateObjectives();
        UpdateEndPanels();
    }

    void UpdateIntegrity()
    {
        if (skull == null) return;

        float target = Mathf.Clamp01(skull.Ratio);
        shownIntegrity = integritySmooth > 0f
            ? Mathf.Lerp(shownIntegrity, target, Time.deltaTime * integritySmooth)
            : target;

        if (integrityBar != null) integrityBar.value = shownIntegrity;

        if (integrityFill != null && integrityGradient != null)
        {
            Color c = integrityGradient.Evaluate(shownIntegrity);

            // sotto soglia pulsa: e' l'unico avviso che il teschio sta per
            // rompersi, e rompersi significa perdere il livello
            if (target <= lowIntegrityThreshold && lowIntegrityPulseSpeed > 0f)
                c.a *= 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * lowIntegrityPulseSpeed));

            integrityFill.color = c;
        }

        // la percentuale segue il valore vero, non quello smussato: leggere
        // "12%" mentre sei gia' a 8% sarebbe una bugia su una barra vitale
        if (integrityLabel != null)
            integrityLabel.text = string.Format(integrityFormat, target * 100f);
    }

    void UpdatePower()
    {
        if (aim == null) return;

        bool powering = aim.CurrentState == SkullAimController.State.Powering;
        bool aiming = aim.CurrentState == SkullAimController.State.Aiming;
        bool visible = powering || (showWhileAiming && aiming);

        if (powerRoot != null && powerRoot.activeSelf != visible)
            powerRoot.SetActive(visible);

        if (!visible) return;

        float ratio = powering ? Mathf.Clamp01(aim.PowerRatio) : 0f;

        if (powerBar != null) powerBar.value = ratio;
        if (powerFill != null && powerGradient != null)
            powerFill.color = powerGradient.Evaluate(ratio);

        if (powerLabel != null)
        {
            powerLabel.text = string.Format(powerFormat, ratio * 100f);

            if (powerLabelFollowsFill)
            {
                RectTransform rt = powerLabel.rectTransform;
                rt.anchorMin = new Vector2(rt.anchorMin.x, ratio);
                rt.anchorMax = new Vector2(rt.anchorMax.x, ratio);
            }
        }
    }

    // L'angolo resta scritto anche durante Powering: e' stato confermato, ma
    // il giocatore deve poter ricordare con che angolo sta per tirare.
    void UpdateLoft()
    {
        if (aim == null) return;

        bool visible = aim.CurrentState == SkullAimController.State.Aiming
                       || aim.CurrentState == SkullAimController.State.Powering;

        if (loftRoot != null && loftRoot.activeSelf != visible)
            loftRoot.SetActive(visible);

        if (!visible) return;

        if (loftLabel != null)
            loftLabel.text = string.Format(loftFormat, aim.LoftAngle);

        if (loftBar != null)
            loftBar.value = Mathf.Clamp01(aim.LoftRatio);
    }

    void UpdateScores()
    {
        if (gameManager == null) return;

        if (damageText != null)
        {
            // il contatore sale rincorrendo il valore vero, stile Burnout:
            // un salto secco non fa capire che l'esplosione ha reso
            if (damageRollSpeed > 0f)
            {
                shownDamage = Mathf.Lerp(shownDamage, gameManager.Damage,
                                         Time.deltaTime * damageRollSpeed);
                if (Mathf.Abs(gameManager.Damage - shownDamage) < 1f)
                    shownDamage = gameManager.Damage;
            }
            else
            {
                shownDamage = gameManager.Damage;
            }

            damageText.text = string.Format(damageFormat, Mathf.RoundToInt(shownDamage));
        }

        if (powerCurrencyText != null)
            powerCurrencyText.text = string.Format(powerCurrencyFormat, gameManager.Power);
    }

    void UpdateObjectives()
    {
        if (gameManager == null) return;

        if (enemiesText != null)
        {
            // il traguardo e' la quota richiesta se c'e', altrimenti quanti
            // nemici c'erano all'inizio. Il numeratore e' sempre il conto
            // cumulativo: con uno spawner attivo "vivi contro iniziali"
            // andrebbe in negativo.
            int goal = gameManager.requiredKills > 0
                ? gameManager.requiredKills
                : gameManager.EnemiesAtStart;

            enemiesText.text = string.Format(enemiesFormat, gameManager.Kills, goal);
        }

        if (holeIndicator != null)
        {
            bool open = gameManager.winHole != null && gameManager.winHole.IsOpen;
            if (holeIndicator.activeSelf != open) holeIndicator.SetActive(open);
        }
    }

    void UpdateEndPanels()
    {
        if (gameManager == null) return;
        if (gameManager.State == lastState) return;

        lastState = gameManager.State;

        if (lastState == GameManager.GameState.Won) ShowWin();
        else if (lastState == GameManager.GameState.Lost) ShowLose();
    }

    // pubbliche anche per poterle agganciare a onWin/onLose nell'Inspector,
    // se un giorno servisse mostrarle fuori dal normale flusso
    public void ShowWin()
    {
        if (winPanel != null) winPanel.SetActive(true);
        if (winReasonText != null) winReasonText.text = DescribeWin(gameManager.LastWinReason);
        if (winStatsText != null) winStatsText.text = BuildStats();
    }

    public void ShowLose()
    {
        if (losePanel != null) losePanel.SetActive(true);
        if (loseReasonText != null) loseReasonText.text = gameManager.LastLoseReason;
        if (loseStatsText != null) loseStatsText.text = BuildStats();
    }

    string BuildStats()
    {
        // il contatore animato viene allineato: sul pannello finale deve
        // comparire il totale vero, non quello a meta' rincorsa
        shownDamage = gameManager.Damage;
        return string.Format(statsFormat, gameManager.Damage, gameManager.Power);
    }

    static string DescribeWin(GameManager.WinReason reason)
    {
        switch (reason)
        {
            case GameManager.WinReason.WinHole: return "Imbucato!";
            case GameManager.WinReason.Objectives: return "Obiettivi completati";
            default: return "Livello completato";
        }
    }

    // da agganciare a un Button sui pannelli di fine livello
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    static Gradient MakeGradient(Color a, Color b, Color c)
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(a, 0f),
                new GradientColorKey(b, 0.5f),
                new GradientColorKey(c, 1f),
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            });
        return g;
    }
}
