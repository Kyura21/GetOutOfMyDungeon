using UnityEngine;
using UnityEngine.InputSystem;

// Il personaggio in terza persona: la capsula con la mazza in mano.
//
// Comandi:
//   WASD          si muove, nella direzione in cui guarda la camera
//   Shift         corre
//   Alt           si abbassa e rallenta
//   Spazio        salta
//   Ctrl          tuffo, a terra o in aria
//   tasto destro  TENUTO: modalita' tiro. Lasciato: si esce
//   tasto sinistro  colpo di mazza, oppure carica del tiro se e' in modalita' tiro
//
// Due modi:
//   Free      si muove, salta e mena con la mazza
//   Shooting  sta fermo e fa lo swing. Comanda SkullAimController.
//
// In modalita' tiro il personaggio NON si sposta e NON segue la palla: resta dove
// ha colpito, girato di fianco al tiro come un golfista (stanceAngle). Il Rigidbody
// resta dinamico, non viene mai mosso a mano.
// Dopo il tiro torna libero appena finisce lo swing, senza aspettare la palla.
//
// Il tuffo e' la schivata: Ctrl lancia il corpo in avanti con un piccolo arco, sia
// da fermo a terra sia a mezz'aria dopo un salto. Toccata terra il personaggio si
// rialza per getUpTime secondi, e intanto si muove piano.
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public enum Mode { Free, Shooting }

    [Header("Riferimenti")]
    public SkullAimController aim;          // se vuoto lo cerco in scena
    public PlayerSwing swing;               // se vuoto lo prendo da qui
    public Transform cameraTransform;       // da' la direzione del movimento. Vuoto = Camera.main
    public CapsuleCollider capsule;         // serve per abbassarsi. Vuoto = lo prendo da qui
    // La parte visibile che si inclina nel tuffo. Deve essere un FIGLIO: il corpo
    // vero resta dritto, perche' una capsula coricata che si rialza a terra verrebbe
    // sparata via dalla fisica. Vuoto = nessuna inclinazione.
    public Transform visual;

    [Header("Velocita'")]
    public float walkSpeed = 4f;            // m/s camminando
    public float runSpeed = 7.5f;           // m/s con shift
    public float crouchSpeed = 2f;          // m/s abbassato, con alt
    public float acceleration = 25f;        // quanto in fretta raggiunge la velocita' voluta
    public float turnSpeed = 14f;           // quanto in fretta si gira verso il moto
    [Range(0f, 1f)] public float airControl = 0.4f; // in aria, frazione di acceleration

    [Header("Abbassarsi (Alt)")]
    [Range(0.2f, 1f)] public float crouchHeightScale = 0.6f; // altezza della capsula, in frazione
    public float crouchChangeSpeed = 10f;   // quanto in fretta cambia l'altezza

    [Header("Modalita' tiro")]
    // Di quanti gradi il corpo e' girato rispetto alla direzione del tiro.
    // 90 = di fianco come un golfista vero: guarda la palla e il tiro parte alla
    // sua sinistra. La mazza in mira va messa davanti alla faccia (posa di mira su
    // PlayerSwing): la posa di riposo, alla sua destra, finirebbe lungo la linea di
    // tiro.
    public float stanceAngle = 90f;
    // In mira il corpo va a mettersi di fianco al punto di colpo, e quando la mira
    // gira gli gira attorno. Si muove con la velocita' del Rigidbody, quindi i muri
    // lo fermano: non viene mai teletrasportato.
    public float stanceMoveSpeed = 5f;      // m/s massimi per raggiungere la posizione
    public float stanceSnap = 12f;          // quanto in fretta ci arriva: piu' alto = piu' secco
    public float aimStepSpeed = 1f;         // m/s dei piccoli passi con WASD in mira

    [Header("Salto (Spazio)")]
    public float jumpHeight = 1.2f;         // metri
    public LayerMask groundMask = ~0;       // cosa conta come terreno
    public float groundCheckDistance = 0.15f; // quanto sotto i piedi si cerca il terreno
    [Range(0f, 89f)] public float maxGroundAngle = 50f; // pendenza oltre la quale e' un muro

    [Header("Tuffo (Ctrl)")]
    public float diveSpeed = 9f;            // m/s in orizzontale all'inizio del tuffo
    public float diveUpSpeed = 1.5f;        // m/s verso l'alto: un piccolo arco prima di cadere
    public float diveTilt = 75f;            // gradi di inclinazione di visual
    public float tiltSpeed = 12f;           // quanto in fretta si inclina

    [Header("Rialzarsi")]
    public float getUpTime = 0.8f;          // secondi dopo l'atterraggio di un tuffo
    public float getUpSpeed = 1.5f;         // m/s mentre si rialza

    public Mode CurrentMode { get; private set; } = Mode.Free;
    public bool IsRunning { get; private set; }
    public bool IsCrouching { get; private set; }
    public bool IsGrounded { get; private set; }
    public bool IsDiving { get; private set; }
    public bool IsGettingUp => getUpTimer > 0f;

    // Dopo un salto i piedi restano vicini al terreno per un paio di passi di fisica,
    // e il controllo direbbe ancora "a terra". Per questo tempo il terreno si ignora.
    const float JumpGroundLock = 0.15f;
    // Il tuffo parte in aria ma vicino a terra: senza questo tempo minimo un tuffo
    // fatto subito dopo il salto finirebbe nello stesso passo in cui comincia.
    const float MinDiveTime = 0.1f;
    // Di quanto la sfera del controllo parte sopra i piedi
    const float GroundSkin = 0.05f;

    private Rigidbody rb;
    private CameraController cameraController;
    private Vector3 desiredVelocity;
    private float standHeight;
    private float standCenterY;

    private bool jumpRequested;
    private bool diveRequested;
    private float groundLockTimer;
    private float diveTimer;
    private Vector3 diveDirection;
    private float getUpTimer;

    // Vero dal momento in cui si entra in mira finche' non si cammina di nuovo: il
    // corpo e' girato di stanceAngle, e "dove guarda" e' la direzione del tiro, non il
    // suo forward. Serve a rientrare in mira subito dopo un tiro senza girare di 90.
    private bool inStance;

    private Quaternion visualRestRotation;
    private float currentTilt;
    private readonly RaycastHit[] groundHits = new RaycastHit[8];

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // il corpo non deve ribaltarsi: a girarlo ci pensiamo noi
        rb.freezeRotation = true;

        // La fisica gira a 50 Hz, lo schermo di solito di piu'. Senza interpolazione
        // il corpo si sposta a scatti fra un passo e l'altro, e una camera incollata
        // a lui fa vedere quegli scatti come un tremolio.
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (swing == null) swing = GetComponent<PlayerSwing>();

        if (capsule == null) capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            standHeight = capsule.height;
            standCenterY = capsule.center.y;
        }

        // inclinare il corpo stesso vorrebbe dire inclinare il collider: vedi visual
        if (visual == transform) visual = null;
        if (visual != null) visualRestRotation = visual.localRotation;
    }

    void Start()
    {
        if (aim == null) aim = FindFirstObjectByType<SkullAimController>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        if (cameraTransform != null) cameraController = cameraTransform.GetComponent<CameraController>();
    }

    void Update()
    {
        bool playing = GameManager.Instance == null || GameManager.Instance.IsPlaying;

        if (getUpTimer > 0f) getUpTimer -= Time.deltaTime;

        if (!playing)
        {
            desiredVelocity = Vector3.zero;
            jumpRequested = false;
            diveRequested = false;
            SetCameraAiming(false);
            return;
        }

        HandleShootingInput();

        // Dopo il rilascio SkullAimController torna subito Idle. Il personaggio resta
        // fermo solo per la frustata della mazza, poi riprende il controllo: la palla
        // non si aspetta. Leggiamo lo stato, come fa l'HUD: non serve che qualcuno
        // ce lo dica.
        if (CurrentMode == Mode.Shooting && aim != null
            && aim.CurrentState == SkullAimController.State.Idle
            && (swing == null || !swing.IsSwinging))
        {
            CurrentMode = Mode.Free;
        }

        if (CurrentMode == Mode.Free)
        {
            ReadMovementInput();
            ReadJumpAndDiveInput();
        }
        else
        {
            ReadAimStepInput();
            desiredVelocity = StanceVelocity();
        }

        SetCameraAiming(CurrentMode == Mode.Shooting);
        UpdateCrouchHeight();
        UpdateVisualTilt();
    }

    void SetCameraAiming(bool aiming)
    {
        if (cameraController != null) cameraController.Aiming = aiming;
    }

    // In mira si entra solo con i piedi per terra e il corpo libero: a mezz'aria o
    // mentre si rialza da un tuffo uno swing non ha senso.
    bool CanEnterAim => IsGrounded && !IsDiving && !IsGettingUp;

    // Tenere premuto il tasto destro = mirare. Lasciarlo = uscire senza tirare.
    // Se dopo un tiro il tasto e' ancora premuto, finita la frustata si torna in
    // mira da soli: il tasto tenuto dice "sto mirando".
    void HandleShootingInput()
    {
        if (Mouse.current == null) return;

        bool held = Mouse.current.rightButton.isPressed;

        if (CurrentMode == Mode.Shooting)
        {
            // lasciato a meta' mira o carica: si annulla. Dopo il rilascio del tiro
            // invece lo stato e' gia' Idle, e si aspetta la fine della frustata
            if (!held && aim != null
                && (aim.CurrentState == SkullAimController.State.Aiming
                    || aim.CurrentState == SkullAimController.State.Powering))
            {
                aim.CancelAim();
                CurrentMode = Mode.Free;
            }
            return;
        }

        // nessun controllo sulla palla: si entra in modalita' tiro quando si vuole,
        // anche con la palla lontana o in movimento. Se al rilascio la palla non e'
        // nella zona di colpo il colpo va a vuoto.
        if (held && CanEnterAim && (swing == null || !swing.IsSwinging)
            && aim != null && aim.BeginAim(LookDirection()))
        {
            // gia' in postura (subito dopo un tiro, senza aver camminato): il punto
            // di colpo resta dov'e' la mazza, cosi' il corpo non rifa' il passo di lato
            if (inStance && swing != null) swing.AnchorHere();

            CurrentMode = Mode.Shooting;
            inStance = true;
            desiredVelocity = Vector3.zero;
        }
    }

    // Dove guarda il personaggio. Girato di stanceAngle dopo un tiro, guarda lungo il
    // tiro, non verso il suo forward.
    Vector3 LookDirection()
    {
        Vector3 f = transform.forward;
        return inStance ? Quaternion.Euler(0f, -stanceAngle, 0f) * f : f;
    }

    // Dove guarda il corpo in mira: la direzione del tiro, girata di stanceAngle
    Vector3 StanceFacing()
    {
        return aim != null ? Quaternion.Euler(0f, stanceAngle, 0f) * aim.ShotDirection : transform.forward;
    }

    // In mira il corpo va dove il cerchio di colpo cade sull'ancoraggio. Rallenta
    // avvicinandosi, cosi' non oscilla. Dopo il tiro (stato Idle, frustata in
    // corso) resta fermo.
    Vector3 StanceVelocity()
    {
        if (aim == null || swing == null || !swing.HasStrikePoint) return Vector3.zero;

        Vector3 toTarget = swing.StancePosition(StanceFacing()) - transform.position;
        toTarget.y = 0f;

        return Vector3.ClampMagnitude(toTarget * stanceSnap, stanceMoveSpeed);
    }

    // I piccoli passi in mira, come sul green per allinearsi alla palla: WASD
    // sposta il punto dove deve cadere il cerchio, e il corpo lo segue. Anche
    // durante la carica; dopo il rilascio no, i piedi restano fermi per la frustata.
    void ReadAimStepInput()
    {
        if (Keyboard.current == null || swing == null || aim == null) return;
        if (aim.CurrentState != SkullAimController.State.Aiming
            && aim.CurrentState != SkullAimController.State.Powering) return;

        Vector2 input = ReadWASD();
        if (input.sqrMagnitude < 0.0001f) return;

        swing.MoveStance(CameraRelative(input.normalized) * aimStepSpeed * Time.deltaTime);
    }

    Vector2 ReadWASD()
    {
        Vector2 input = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) input.y += 1f;
        if (Keyboard.current.sKey.isPressed) input.y -= 1f;
        if (Keyboard.current.dKey.isPressed) input.x += 1f;
        if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        return input;
    }

    // Da un input 2D a una direzione sul piano: avanti vuol dire dove guarda la camera
    Vector3 CameraRelative(Vector2 input)
    {
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;

        if (cameraTransform != null)
        {
            forward = new Vector3(cameraTransform.forward.x, 0f, cameraTransform.forward.z);
            right = new Vector3(cameraTransform.right.x, 0f, cameraTransform.right.z);

            forward = forward.sqrMagnitude < 0.0001f ? Vector3.forward : forward.normalized;
            right = right.sqrMagnitude < 0.0001f ? Vector3.right : right.normalized;
        }

        return forward * input.y + right * input.x;
    }

    void ReadMovementInput()
    {
        if (Keyboard.current == null)
        {
            desiredVelocity = Vector3.zero;
            IsRunning = false;
            IsCrouching = false;
            return;
        }

        IsCrouching = Keyboard.current.leftAltKey.isPressed
                      || Keyboard.current.rightAltKey.isPressed;

        // abbassato non si corre: altrimenti alt+shift darebbe una corsa accucciata.
        // Nemmeno mentre ci si rialza da un tuffo.
        IsRunning = !IsCrouching && !IsGettingUp
                    && (Keyboard.current.leftShiftKey.isPressed
                        || Keyboard.current.rightShiftKey.isPressed);

        Vector2 input = ReadWASD();

        if (input.sqrMagnitude < 0.0001f)
        {
            desiredVelocity = Vector3.zero;
            return;
        }

        float speed = IsCrouching ? crouchSpeed : (IsRunning ? runSpeed : walkSpeed);
        if (IsGettingUp) speed = Mathf.Min(speed, getUpSpeed);

        // il movimento e' relativo alla camera: avanti vuol dire dove stai guardando
        desiredVelocity = CameraRelative(input.normalized).normalized * speed;
    }

    // L'input si legge qui, a ogni frame, e si esegue in FixedUpdate: letto in
    // FixedUpdate, un tasto premuto fra due passi di fisica andrebbe perso.
    void ReadJumpAndDiveInput()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame
            && IsGrounded && !IsDiving && !IsGettingUp)
        {
            jumpRequested = true;
        }

        bool ctrlPressed = Keyboard.current.leftCtrlKey.wasPressedThisFrame
                           || Keyboard.current.rightCtrlKey.wasPressedThisFrame;

        if (ctrlPressed && !IsDiving && !IsGettingUp)
            diveRequested = true;
    }

    // Abbassa la capsula tenendo i piedi dov'erano. Se si riducesse solo l'altezza,
    // la capsula si stringerebbe attorno al proprio centro e il personaggio
    // resterebbe sospeso per aria. Bassa anche durante il tuffo e mentre si rialza:
    // la schivata deve passare sotto le cose.
    void UpdateCrouchHeight()
    {
        if (capsule == null || standHeight <= 0f) return;

        bool low = IsCrouching || IsDiving || IsGettingUp;
        float wanted = low ? standHeight * crouchHeightScale : standHeight;

        float height = crouchChangeSpeed > 0f
            ? Mathf.Lerp(capsule.height, wanted, Time.deltaTime * crouchChangeSpeed)
            : wanted;

        Vector3 center = capsule.center;
        center.y = standCenterY - (standHeight - height) * 0.5f;

        capsule.height = height;
        capsule.center = center;
    }

    // Nel tuffo la parte visibile si corica in avanti. Mentre si rialza torna
    // dritta in proporzione al tempo che manca.
    void UpdateVisualTilt()
    {
        if (visual == null) return;

        float wanted = 0f;
        if (IsDiving) wanted = diveTilt;
        else if (IsGettingUp) wanted = diveTilt * Mathf.Clamp01(getUpTimer / Mathf.Max(0.01f, getUpTime));

        currentTilt = tiltSpeed > 0f
            ? Mathf.Lerp(currentTilt, wanted, Time.deltaTime * tiltSpeed)
            : wanted;

        visual.localRotation = visualRestRotation * Quaternion.Euler(currentTilt, 0f, 0f);
    }

    void FixedUpdate()
    {
        UpdateGrounded();

        if (jumpRequested)
        {
            jumpRequested = false;
            if (IsGrounded && !IsDiving) Jump();
        }

        if (diveRequested)
        {
            diveRequested = false;
            if (!IsDiving && !IsGettingUp) StartDive();
        }

        if (IsDiving)
        {
            diveTimer += Time.fixedDeltaTime;

            if (diveTimer >= MinDiveTime && IsGrounded)
            {
                // atterrato: da qui si rialza, e la scivolata la frena acceleration
                IsDiving = false;
                getUpTimer = getUpTime;
            }
            else
            {
                // in volo il tuffo non si guida: e' un impegno, come ogni schivata
                FaceDirection(diveDirection);
                return;
            }
        }

        // La velocita' voluta si applica solo alle componenti orizzontali: la
        // verticale resta alla gravita', altrimenti il personaggio non cadrebbe.
        Vector3 v = rb.linearVelocity;
        Vector3 horizontal = new Vector3(v.x, 0f, v.z);

        float accel = acceleration * (IsGrounded ? 1f : airControl);
        horizontal = Vector3.MoveTowards(horizontal, desiredVelocity, accel * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(horizontal.x, v.y, horizontal.z);

        if (CurrentMode == Mode.Shooting)
        {
            // fermo, di fianco al tiro come un golfista: cosi' lo swing si legge
            if (aim != null) FaceDirection(StanceFacing());
            return;
        }

        if (desiredVelocity.sqrMagnitude > 0.0001f)
        {
            // camminando si esce dalla postura: da qui "dove guarda" e' il forward
            inStance = false;
            FaceDirection(desiredVelocity);
        }
    }

    void Jump()
    {
        // dall'altezza voluta alla velocita' di partenza: v = radice(2 g h)
        float g = Mathf.Abs(Physics.gravity.y);
        float up = Mathf.Sqrt(2f * g * Mathf.Max(0f, jumpHeight));

        Vector3 v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(v.x, up, v.z);

        IsGrounded = false;
        groundLockTimer = JumpGroundLock;
    }

    void StartDive()
    {
        // verso dove si sta andando; da fermi, verso dove si guarda
        Vector3 dir = desiredVelocity.sqrMagnitude > 0.0001f ? desiredVelocity : LookDirection();
        dir.y = 0f;
        dir = dir.sqrMagnitude < 0.0001f ? Vector3.forward : dir.normalized;

        diveDirection = dir;
        diveTimer = 0f;
        IsDiving = true;
        inStance = false;

        rb.linearVelocity = dir * diveSpeed + Vector3.up * diveUpSpeed;

        // partendo da terra i piedi restano vicini al suolo per qualche passo: come
        // nel salto, il terreno si ignora per un attimo, altrimenti il tuffo finirebbe
        // appena cominciato
        IsGrounded = false;
        groundLockTimer = JumpGroundLock;
    }

    // Una sfera appena sopra i piedi, lanciata verso il basso. Conta solo il terreno
    // abbastanza piano: un muro toccato di lato non deve permettere di saltare.
    void UpdateGrounded()
    {
        if (groundLockTimer > 0f)
        {
            groundLockTimer -= Time.fixedDeltaTime;
            IsGrounded = false;
            return;
        }

        if (capsule == null)
        {
            IsGrounded = true;
            return;
        }

        // i bounds sono gia' in coordinate di mondo, scala compresa
        Bounds b = capsule.bounds;
        float radius = Mathf.Max(0.01f, Mathf.Min(b.extents.x, b.extents.z) * 0.9f);
        Vector3 origin = new Vector3(b.center.x, b.min.y + radius + GroundSkin, b.center.z);

        int count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down, groundHits,
                                               GroundSkin + groundCheckDistance,
                                               groundMask, QueryTriggerInteraction.Ignore);

        float minNormalY = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
        bool grounded = false;

        for (int i = 0; i < count; i++)
        {
            Collider c = groundHits[i].collider;
            if (c == null || c.attachedRigidbody == rb) continue;

            // distanza 0 = la sfera parte gia' dentro qualcosa: siamo appoggiati
            if (groundHits[i].distance <= 0f || groundHits[i].normal.y >= minNormalY)
            {
                grounded = true;
                break;
            }
        }

        IsGrounded = grounded;
    }

    void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(direction.normalized, Vector3.up);

        // rb.rotation e non MoveRotation: MoveRotation e' fatto per i corpi kinematic,
        // e questo resta dinamico con la rotazione congelata
        rb.rotation = turnSpeed > 0f
            ? Quaternion.Slerp(rb.rotation, target, Time.fixedDeltaTime * turnSpeed)
            : target;
    }
}
