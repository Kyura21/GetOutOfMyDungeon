using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public enum CamState { Explore, Aim, Follow }

    [Header("Riferimenti")]
    public SkullAimController aimController;
    public Transform ball;
    public Transform cuePivot;

    [Header("Explore")]
    public float dragSensitivity = 0.2f;
    public float minPitch = 10f;
    public float maxPitch = 80f;
    public float moveSpeed = 8f;

    [Header("Aim (orbita dietro la palla seguendo l'asta)")]
    public float aimDistance = 3f;      // quanto sta dietro la palla, lato asta
    public float aimHeight = 2f;        // quanto sta in alto
    public float aimLookHeight = 0.5f;  // punto guardato sulla palla
    public float aimRotateSmooth = 10f; // 0 = segue l'asta di scatto

    [Header("Follow (relativo alla direzione di tiro)")]
    public float followDistance = 8f;    // quanto sta dietro alla palla
    public float followHeight = 4f;      // quanto sta in alto
    public float followLookHeight = 0.5f;// punto guardato sulla palla
    public float followSmooth = 5f;
    public float enterFollowDuration = 0.35f; // durata transizione da Aim a Follow
    private Vector3 followStartPos;
    private Quaternion followStartRot;
    private float followTransitionTimer;

    public CamState CurrentState { get; private set; } = CamState.Explore;

    private float yaw;
    private float pitch;
    private Vector3 followDir;
    private Vector3 aimDir; // direzione palla -> asta, sul piano orizzontale

    void Start()
    {
        // salva la rotazione iniziale impostata in editor come base per il drag
        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x;
    }

    void Update()
    {
        switch (CurrentState)
        {
            case CamState.Explore:
                HandleExploreMovement();
                HandleExploreRotation();
                if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    if (aimController.BeginAim())
                        EnterAim();
                }
                break;

            case CamState.Aim:
                UpdateAimCamera();   // ricalcola ogni frame, così l'Inspector è live
                if (aimController.CurrentState == SkullAimController.State.Shot)
                    EnterFollow();
                break;

            case CamState.Follow:
                UpdateFollow();
                if (aimController.CurrentState == SkullAimController.State.Idle)
                    EnterExplore();
                break;
        }
    }

    void HandleExploreRotation()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.isPressed)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            yaw += delta.x * dragSensitivity;
            pitch -= delta.y * dragSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }

    void HandleExploreMovement()
    {
        if (Keyboard.current == null) return;

        Vector3 move = Vector3.zero;
        if (Keyboard.current.wKey.isPressed) move += transform.forward;
        if (Keyboard.current.sKey.isPressed) move -= transform.forward;
        if (Keyboard.current.aKey.isPressed) move -= transform.right;
        if (Keyboard.current.dKey.isPressed) move += transform.right;
        if (Keyboard.current.eKey.isPressed) move += Vector3.up;
        if (Keyboard.current.qKey.isPressed) move -= Vector3.up;

        transform.position += move.normalized * moveSpeed * Time.deltaTime * (move.magnitude > 0 ? 1f : 0f);
    }

    void EnterAim()
    {
        CurrentState = CamState.Aim;

        // niente smoothing all'ingresso: la camera e' gia' dietro la palla
        // insieme all'asta fin dal primo frame
        aimDir = ResolveAimDir();
        ApplyAimCamera();
    }

    void UpdateAimCamera()
    {
        Vector3 target = ResolveAimDir();

        aimDir = aimRotateSmooth > 0f
            ? Vector3.Slerp(aimDir, target, Time.deltaTime * aimRotateSmooth).normalized
            : target;

        ApplyAimCamera();
    }

    void ApplyAimCamera()
    {
        transform.position = ball.position + aimDir * aimDistance + Vector3.up * aimHeight;

        Vector3 lookPoint = ball.position + Vector3.up * aimLookHeight;
        transform.rotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
    }

    // "dietro" = il lato dove sta l'asta, cioe' l'opposto della direzione di tiro
    Vector3 ResolveAimDir()
    {
        Vector3 d = -aimController.ShotDirection;
        d.y = 0f;

        if (d.sqrMagnitude < 0.001f && cuePivot != null)
        {
            d = cuePivot.position - ball.position;
            d.y = 0f;
        }

        if (d.sqrMagnitude < 0.001f) d = aimDir;          // ultima direzione buona

        if (d.sqrMagnitude < 0.001f)                       // primo aim, mouse fuori dal terreno
        {
            d = -transform.forward;
            d.y = 0f;
            if (d.sqrMagnitude < 0.001f) d = Vector3.back;
        }

        return d.normalized;
    }

    void EnterFollow()
    {
        CurrentState = CamState.Follow;

        followDir = aimController.ShotDirection;
        followDir.y = 0f;
        if (followDir.sqrMagnitude < 0.001f)
            followDir = transform.forward;
        followDir.Normalize();

        followStartPos = transform.position;
        followStartRot = transform.rotation;
        followTransitionTimer = 0f;
    }

    Vector3 GetFollowTargetPos()
    {
        return ball.position - followDir * followDistance + Vector3.up * followHeight;
    }

    void UpdateFollow()
    {
        Vector3 targetPos = GetFollowTargetPos();
        Vector3 lookPoint = ball.position + Vector3.up * followLookHeight;
        Quaternion targetRot = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);

        if (followTransitionTimer < enterFollowDuration && enterFollowDuration > 0f)
        {
            followTransitionTimer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, followTransitionTimer / enterFollowDuration);

            transform.position = Vector3.Lerp(followStartPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(followStartRot, targetRot, t);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSmooth);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * followSmooth);
        }
    }

    void EnterExplore()
    {
        CurrentState = CamState.Explore;
        // la camera resta dov'è (posizione follow finale) e riprende il drag da lì
        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x;
    }

    void OnDrawGizmosSelected()
    {
        if (ball == null) return;

        // anello alla quota/distanza di aim: la camera scorre qui seguendo l'asta
        Gizmos.color = Color.yellow;
        Vector3 center = ball.position + Vector3.up * aimHeight;
        Vector3 prev = center + Vector3.forward * aimDistance;
        for (int i = 1; i <= 32; i++)
        {
            Vector3 p = center + Quaternion.Euler(0f, i * 360f / 32f, 0f) * Vector3.forward * aimDistance;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
        Gizmos.DrawLine(center + Vector3.forward * aimDistance, ball.position + Vector3.up * aimLookHeight);
    }
}