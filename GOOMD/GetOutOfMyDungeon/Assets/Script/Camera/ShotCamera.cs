using UnityEngine;
using UnityEngine.UI;

// La telecronaca del tiro: una seconda camera che segue la testa in volo, mostrata
// in una finestrella dentro l'HUD. Come le camere che seguono la pallina in una
// partita di golf in televisione.
//
// Serve a risolvere un problema vero: il giocatore e' fatto di due pezzi che si
// allontanano fra loro, il corpo e la testa. La camera principale resta sempre
// dietro al corpo, quindi senza questa finestra il tiro non si vedrebbe.
//
// Si apre quando un colpo prende davvero la testa. Si chiude in due casi:
//   - la testa si e' allontanata oltre hideDistance, e poi il corpo l'ha
//     raggiunta: a quel punto si vede nell'inquadratura normale;
//   - la testa non si e' mai allontanata (colpo debole) e si e' fermata.
// Serve il primo "si e' allontanata": al momento del colpo il corpo e' sempre a
// portata di mazza, quindi vicino, e la finestra si chiudeva nello stesso frame
// in cui si apriva.
//
// Tecnicamente non e' uno SpriteRenderer ma una RawImage dentro il Canvas: e' il
// componente che sa disegnare una RenderTexture in uno spazio di schermo. Uno
// SpriteRenderer vive nel mondo 3D e andrebbe tenuto in posizione a mano.
//
// La RenderTexture viene creata a runtime, quindi non serve nessun asset.
public class ShotCamera : MonoBehaviour
{
    [Header("Riferimenti")]
    public SkullAimController aim;      // se vuoto lo cerco in scena
    public Transform ball;              // la testa. Vuoto = la prendo da aim
    public Transform player;            // il corpo, per decidere quando chiudere
    public Camera shotCamera;           // la camera della telecronaca
    public RawImage display;            // dove si disegna, dentro il Canvas
    public GameObject frame;            // cornice da accendere e spegnere. Vuoto = display

    [Header("Inquadratura")]
    public float followDistance = 8f;   // quanto sta dietro alla testa
    public float followHeight = 4f;     // quanto sta in alto
    public float followLookHeight = 0.5f; // punto guardato sopra la testa
    public float followSmooth = 5f;     // morbidezza dell'inseguimento. 0 = incollata

    [Header("Quando si chiude")]
    // Distanza fra il corpo e la testa sotto la quale la finestra si chiude: il
    // corpo e' arrivato, la testa si vede nell'inquadratura normale.
    public float hideDistance = 8f;

    [Header("Risoluzione della finestra")]
    public int textureWidth = 480;
    public int textureHeight = 270;

    public bool IsOpen { get; private set; }

    // Un colpo debole che resta vicino si chiude quando la testa si ferma. Appena
    // colpita pero' la testa e' ancora ferma: senza questa attesa la finestra si
    // chiuderebbe prima che la palla parta.
    const float MinOpenTime = 0.5f;

    private RenderTexture texture;
    private Vector3 followDir;
    private int lastBallHits;
    private bool wentFar;
    private float openTimer;
    private SkullRolling ballRolling;
    private Rigidbody ballRb;

    void Start()
    {
        if (aim == null) aim = FindFirstObjectByType<SkullAimController>();
        if (ball == null && aim != null) ball = aim.ball;
        if (aim != null) lastBallHits = aim.BallHits;

        if (ball != null)
        {
            ballRolling = ball.GetComponent<SkullRolling>();
            ballRb = ball.GetComponent<Rigidbody>();
        }

        if (shotCamera != null && display != null)
        {
            texture = new RenderTexture(Mathf.Max(16, textureWidth),
                                        Mathf.Max(16, textureHeight), 24);
            texture.name = "ShotCamera";

            shotCamera.targetTexture = texture;
            display.texture = texture;
        }

        Close();
    }

    void OnDestroy()
    {
        // la RenderTexture e' creata a mano, quindi va liberata a mano: non la
        // raccoglie il garbage collector
        if (shotCamera != null) shotCamera.targetTexture = null;

        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
            texture = null;
        }
    }

    void Update()
    {
        if (aim == null) return;

        // un colpo ha appena preso la testa: si apre, o si riparte se era gia'
        // aperta. Se la mazza ha mulinato a vuoto il contatore non sale, e non si
        // apre niente: non c'e' nessun tiro da raccontare.
        if (aim.BallHits != lastBallHits)
        {
            lastBallHits = aim.BallHits;
            Open();
        }

        if (!IsOpen) return;

        openTimer += Time.deltaTime;

        if (ShouldClose())
        {
            Close();
            return;
        }

        UpdateShotCamera();
    }

    bool ShouldClose()
    {
        if (ball == null || player == null) return true;

        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return true;

        float distance = Vector3.Distance(player.position, ball.position);
        if (distance > hideDistance) wentFar = true;

        if (wentFar) return distance <= hideDistance;

        return openTimer > MinOpenTime && IsBallStopped();
    }

    bool IsBallStopped()
    {
        // la soglia vive su SkullRolling: un numero solo per tutto il gioco
        if (ballRolling != null) return ballRolling.IsStopped;
        return ballRb == null || ballRb.linearVelocity.sqrMagnitude < 0.16f;
    }

    void Open()
    {
        if (shotCamera == null || display == null || ball == null) return;

        IsOpen = true;
        wentFar = false;
        openTimer = 0f;

        // la direzione resta quella del tiro per tutta la ripresa: una camera che
        // segue da dietro lungo la linea del colpo si legge come una telecronaca,
        // una che gira attorno alla palla no
        followDir = aim.ShotDirection;
        followDir.y = 0f;
        followDir = followDir.sqrMagnitude < 0.001f ? Vector3.forward : followDir.normalized;

        shotCamera.enabled = true;
        SetFrameActive(true);

        // in posizione subito, senza partire da dove era prima
        PlaceShotCamera(1f);
    }

    void Close()
    {
        IsOpen = false;

        if (shotCamera != null) shotCamera.enabled = false;
        SetFrameActive(false);
    }

    void SetFrameActive(bool active)
    {
        GameObject target = frame != null
            ? frame
            : (display != null ? display.gameObject : null);

        if (target != null && target.activeSelf != active)
            target.SetActive(active);
    }

    void UpdateShotCamera()
    {
        PlaceShotCamera(followSmooth > 0f ? Time.deltaTime * followSmooth : 1f);
    }

    void PlaceShotCamera(float t)
    {
        Vector3 targetPos = ball.position - followDir * followDistance + Vector3.up * followHeight;
        Vector3 lookPoint = ball.position + Vector3.up * followLookHeight;

        Transform cam = shotCamera.transform;

        cam.position = t >= 1f ? targetPos : Vector3.Lerp(cam.position, targetPos, t);

        Vector3 toLook = lookPoint - cam.position;
        if (toLook.sqrMagnitude > 0.0001f)
            cam.rotation = Quaternion.LookRotation(toLook, Vector3.up);
    }
}
