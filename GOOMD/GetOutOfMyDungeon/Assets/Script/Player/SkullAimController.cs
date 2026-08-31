using UnityEngine;
using UnityEngine.InputSystem;

public class SkullAimController : MonoBehaviour
{
    public enum State { Idle, Aiming, Powering, Shot }

    [Header("Riferimenti")]
    public Transform ball;          // la palla/teschio
    public Transform cuePivot;      // asta, deve essere figlia/vicina alla palla
    public Rigidbody ballRb;
    public SkullExplosion skullExplosion; // se vuoto lo prendo dalla palla
    public SkullRolling skullRolling;     // idem

    [Header("Parametri Aim")]
    public float cueDistance = 1.5f;   // distanza asta dalla palla
    public float aimSensitivity = 0.3f; // gradi di rotazione per pixel di mouse
    public float aimSmooth = 12f;       // 0 = l'asta segue il mouse di scatto
    public Vector3 cueRotationOffset = new Vector3(90f, 0f, 0f); // cilindro Unity ha l'asse lungo su Y

    [Header("Parametri Power")]
    public float maxPower = 20f;
    public float powerSpeed = 15f;     // velocità oscillazione barra potenza
    public float forceMultiplier = 1f;

    public State CurrentState { get; private set; } = State.Idle;
    public Vector3 ShotDirection => shotDirection;

    private Vector3 shotDirection;
    private float currentPower;
    private float powerTimer;
    private Camera cam;
    private bool hasMoved;
    private float shotTimer;
    private float aimYaw;      // angolo attuale, smussato
    private float targetYaw;   // angolo accumulato dal mouse

    public Transform aimArrow;         // freccia direzione tiro
    public float arrowDistance = 1.2f; // distanza dal centro palla
    public float arrowHeight = 0.1f;
    public Vector3 arrowRotationOffset = new Vector3(90f, 0f, 0f);

    void Start()
    {
        cam = Camera.main;

        if (ball != null)
        {
            if (skullExplosion == null) skullExplosion = ball.GetComponent<SkullExplosion>();
            if (skullRolling == null) skullRolling = ball.GetComponent<SkullRolling>();
        }
    }

    public bool BeginAim()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return false;

        if (CurrentState == State.Idle && IsBallStopped())
        {
            CurrentState = State.Aiming;

            // parto mirando dove sta guardando la camera in quel momento,
            // cosi' entrando in mira non c'e' nessuno scarto
            Vector3 f = cam != null ? cam.transform.forward : Vector3.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 0.001f) f = Vector3.forward;
            targetYaw = aimYaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;

            // piazzo subito asta e freccia: cosi' la camera ha gia' una
            // direzione valida da seguire nello stesso frame
            UpdateAim();
            UpdateArrow();
            return true;
        }
        return false;
    }

    void Update()
    {
        switch (CurrentState)
        {
            case State.Idle:
                // non fa nulla: l'aiming viene avviato da CameraController.BeginAim()
                break;

            case State.Aiming:
                UpdateAim();
                UpdateArrow();
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    CurrentState = State.Powering;
                    powerTimer = 0f;
                }
                break;

            case State.Powering:
                UpdatePower();
                UpdateArrow();
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Shoot();
                    CurrentState = State.Shot;
                }
                break;

            case State.Shot:
                HideArrow();
                shotTimer += Time.deltaTime;
                // aspetto che la palla inizi davvero a muoversi prima di poter tornare Idle
                if (!hasMoved)
                {
                    if (!IsBallStopped())
                        hasMoved = true;
                    else if (shotTimer > 0.5f)
                        CurrentState = State.Idle; // colpo troppo debole, non si e' mossa
                }
                else if (IsBallStopped())
                {
                    CurrentState = State.Idle;
                }
                break;
        }
    }

    void UpdateAim()
    {
        // il mouse ruota la mira invece di puntare un punto sul terreno:
        // un raycast dalla camera si auto-alimenterebbe, visto che ora la
        // camera si posiziona proprio in base a questa direzione
        if (Mouse.current != null)
            targetYaw += Mouse.current.delta.ReadValue().x * aimSensitivity;

        aimYaw = aimSmooth > 0f
            ? Mathf.LerpAngle(aimYaw, targetYaw, Time.deltaTime * aimSmooth)
            : targetYaw;

        shotDirection = Quaternion.Euler(0f, aimYaw, 0f) * Vector3.forward;

        // l'asta sta dietro, dal lato opposto al tiro
        Vector3 dir = -shotDirection;
        cuePivot.position = ball.position + dir * cueDistance;
        cuePivot.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(cueRotationOffset);
    }

    void UpdatePower()
    {
        // oscilla tra 0 e maxPower avanti e indietro
        powerTimer += Time.deltaTime * powerSpeed;
        currentPower = Mathf.PingPong(powerTimer, maxPower);

        // avvicina/allontana l'asta per dare feedback visivo della potenza
        float t = currentPower / maxPower;
        Vector3 dirFromBall = (cuePivot.position - ball.position).normalized;
        float dynamicDistance = cueDistance + t * 0.5f;
        cuePivot.position = ball.position + dirFromBall * dynamicDistance;
    }

    void Shoot()
    {
        hasMoved = false;
        shotTimer = 0f;

        // riarmo il teschio: senza questo esplode una volta sola per partita
        if (skullExplosion != null)
            skullExplosion.ResetExplosion();

        ballRb.AddForce(shotDirection * currentPower * forceMultiplier, ForceMode.Impulse);
    }

    bool IsBallStopped()
    {
        // la soglia vive su SkullRolling, cosi' non ho due numeri che divergono
        if (skullRolling != null) return skullRolling.IsStopped;
        return ballRb.linearVelocity.sqrMagnitude < 0.16f;
    }
    void UpdateArrow()
    {
        if (aimArrow == null) return;

        // finché il mouse non trova il terreno shotDirection resta zero:
        // LookRotation esploderebbe, quindi tengo la freccia nascosta
        if (shotDirection.sqrMagnitude < 0.001f)
        {
            HideArrow();
            return;
        }

        if (!aimArrow.gameObject.activeSelf)
            aimArrow.gameObject.SetActive(true);

        aimArrow.position = ball.position + shotDirection * arrowDistance + Vector3.up * arrowHeight;
        aimArrow.rotation = Quaternion.LookRotation(shotDirection, Vector3.up) * Quaternion.Euler(arrowRotationOffset);
    }

    void HideArrow()
    {
        if (aimArrow != null) aimArrow.gameObject.SetActive(false);
    }
}