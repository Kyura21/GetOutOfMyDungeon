using UnityEngine;
using UnityEngine.InputSystem;

// La camera di gioco: terza persona, a un offset fisso dal personaggio.
//
// Il mouse gira la camera, non il personaggio. Quando il personaggio si gira per
// camminare, la camera non si gira con lui: resta dove l'ha messa il mouse.
//
// L'offset e' misurato nello spazio della camera, girata solo in orizzontale:
//   x  di lato (positivo = a destra, per una camera sopra la spalla)
//   y  in alto
//   z  avanti/indietro (negativo = dietro)
// Il pitch (guardare su e giu') gira solo lo sguardo, non sposta la camera:
// la camera resta sempre allo stesso offset.
//
// La palla che vola non la segue questa camera. Se ne occupa ShotCamera, che apre
// una finestra in HUD con la telecronaca del tiro.
//
// La direzione della camera e' quella del mouse, e da qui SkullAimController prende
// la direzione del tiro. Il verso delle dipendenze e' uno solo: mouse -> camera ->
// tiro. Non invertirlo: quando la camera si posizionava in base alla direzione di
// tiro, mirare con un raycast dalla camera si auto-alimentava. Vedi lo Storico.
//
// Questo script non sa niente del gioco: non legge ne' il tiro ne' lo stato del
// livello. Vuole solo un transform da seguire, e qualcuno che gli dica se si sta
// mirando (Aiming, lo imposta PlayerController): in mira scivola su aimOffset,
// piu' vicino al personaggio.
public class CameraController : MonoBehaviour
{
    [Header("Riferimenti")]
    public Transform player;            // la capsula del personaggio

    [Header("Posizione")]
    public Vector3 offset = new Vector3(0f, 2f, -5f); // dal pivot del personaggio, vedi sopra
    public float positionSmooth = 0f;   // 0 = incollata. Sopra 0 la camera insegue in ritardo

    [Header("In mira")]
    // Stesse regole di offset. In mira il corpo sta a sinistra del punto di colpo:
    // con x a 0 il corpo e' al centro del quadro e la mazza alla sua destra, come in
    // Reference/start mira.png. x positivo porta la mazza piu' verso il centro.
    public Vector3 aimOffset = new Vector3(0f, 1.8f, -2.8f);
    public float aimBlendTime = 0.25f;  // secondi per passare da un offset all'altro. 0 = di scatto

    [Header("Sguardo col mouse")]
    public float lookSensitivity = 0.15f;   // gradi di rotazione per pixel di mouse
    public float lookMinPitch = -25f;       // quanto puoi guardare in alto (negativo = su)
    public float lookMaxPitch = 70f;        // quanto puoi guardare in basso
    public bool lockCursor = true;          // cattura il puntatore

    [Header("Urti col muro")]
    // In un dungeon la camera finirebbe dentro le pareti. Se fra il personaggio e
    // la camera c'e' un ostacolo, la camera si avvicina fino a liberarsi.
    public LayerMask collisionMask = ~0;
    public float collisionPadding = 0.3f;
    public float minDistance = 1f;          // piu' vicino di cosi' non viene

    // Lo yaw attuale. Chi deve muoversi rispetto all'inquadratura usa il forward di
    // questo transform, che e' la stessa cosa.
    public float Yaw => yaw;

    // vero mentre si mira: la camera passa su aimOffset
    public bool Aiming { get; set; }

    private float yaw;
    private float pitch;
    private float aimBlend;     // 0 = offset, 1 = aimOffset

    void Start()
    {
        // parte dalla rotazione impostata in editor, cosi' la prima inquadratura e'
        // quella che hai composto tu
        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = Mathf.Clamp(NormalizePitch(e.x), lookMinPitch, lookMaxPitch);

        SetCursor(lockCursor);
    }

    void OnDisable()
    {
        // se lo script si spegne il puntatore deve tornare libero, altrimenti resta
        // catturato e non si riesce piu' a usare l'editor
        SetCursor(false);
    }

    static void SetCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    // gli euler di Unity tornano 0..360: oltre 180 e' in realta' un angolo negativo
    static float NormalizePitch(float degrees)
    {
        return degrees > 180f ? degrees - 360f : degrees;
    }

    // LateUpdate e non Update: la camera si piazza dopo che il personaggio si e'
    // mosso in questo frame, altrimenti lo insegue sempre con un frame di ritardo
    void LateUpdate()
    {
        UpdateLook();
        UpdatePosition();
    }

    void UpdateLook()
    {
        if (Mouse.current == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        yaw += delta.x * lookSensitivity;
        pitch -= delta.y * lookSensitivity;
        pitch = Mathf.Clamp(pitch, lookMinPitch, lookMaxPitch);
    }

    void UpdatePosition()
    {
        if (player == null) return;

        // l'offset gira solo con lo yaw: col pitch la camera salirebbe e scenderebbe
        // attorno al personaggio, e non sarebbe piu' un offset fisso
        Quaternion flat = Quaternion.Euler(0f, yaw, 0f);

        float wantedBlend = Aiming ? 1f : 0f;
        aimBlend = aimBlendTime > 0f
            ? Mathf.MoveTowards(aimBlend, wantedBlend, Time.deltaTime / aimBlendTime)
            : wantedBlend;

        // SmoothStep: parte e arriva piano, cosi' il passaggio non da' uno strappo
        Vector3 currentOffset = Vector3.Lerp(offset, aimOffset, Mathf.SmoothStep(0f, 1f, aimBlend));

        // il controllo dei muri parte da sopra il personaggio, all'altezza della
        // camera: partendo dai piedi il pavimento stesso conterebbe come muro
        Vector3 pivot = player.position + Vector3.up * currentOffset.y;
        Vector3 wanted = player.position + flat * currentOffset;

        Vector3 target = ResolveWalls(pivot, wanted);

        transform.position = positionSmooth > 0f
            ? Vector3.Lerp(transform.position, target, Time.deltaTime * positionSmooth)
            : target;

        // la rotazione non si smussa: segue il mouse, e un ritardo qui si sente
        // subito come comando impastato
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    Vector3 ResolveWalls(Vector3 pivot, Vector3 wanted)
    {
        Vector3 toCamera = wanted - pivot;
        float length = toCamera.magnitude;
        if (length < 0.0001f) return wanted;

        if (Physics.Linecast(pivot, wanted, out RaycastHit hit,
                             collisionMask, QueryTriggerInteraction.Ignore))
        {
            float d = Mathf.Max(Mathf.Min(minDistance, length), hit.distance - collisionPadding);
            return pivot + toCamera / length * d;
        }

        return wanted;
    }
}
