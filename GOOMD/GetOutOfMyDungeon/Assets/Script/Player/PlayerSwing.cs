using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// La mazza (lo Skeletal_arm) sta in mano al personaggio, e questo script la anima.
// Fa due cose diverse a seconda del momento:
//
//   IN MODALITA' TIRO  lo swing da golf. In mira la mazza va in posa al lato del
//                      corpo, con la testa a terra sul punto di colpo; tenendo premuto
//                      sale indietro lungo un arco attorno alle spalle, tanto piu'
//                      quanto si carica; al rilascio ripassa sul punto, prosegue
//                      oltre (il follow-through) e torna a riposo.
//   FUORI              il colpo a mano col tasto sinistro: portata corta, niente danno,
//                      tempo di ricarica. Stordisce e basta: la palla resta l'arma.
//
// LO SWING E' UN ARCO, non un'animazione. In mira il corpo sta di fianco al tiro
// (PlayerController.stanceAngle 90) e la mazza e' davanti alla faccia, con la testa
// a terra: e' la posa di partenza ed e' dove avviene il colpo. Da li' la mazza
// ruota attorno alle spalle (swingPivot) su un piano INCLINATO, quello che passa
// per la testa della mazza e per la linea delle spalle. Per questo la testa sale e
// gira attorno alla capsula insieme, come una mazza vera, e non passa mai sotto il
// corpo:
//   basso davanti -> carica: alto a destra -> colpo: basso davanti -> follow-through:
//   alto a sinistra.
// Il piano non e' un campo: si calcola dalla posa di mira. Riferimenti:
// Reference/swing arc.png, 0%.png, 50%.png, 100%.png. Prima o poi lo sostituira'
// un'animazione vera.
//
// IL PUNTO DI COLPO sta sotto la testa della mazza (clubHead) nella posa di mira.
// Tutto quello che c'e' nel cerchio di raggio strikeRadius viene colpito: la palla
// parte, i nemici volano. Non si vede in gioco: e' un dato del controller. Si vede
// come gizmo verde selezionando il personaggio.
//
// La mazza e' un transform solo: a muoverla c'e' solo questo script. SkullAimController
// non la tocca.
public class PlayerSwing : MonoBehaviour
{
    [Header("Riferimenti")]
    public SkullAimController aim;       // se vuoto lo cerco in scena
    public Transform clubPivot;          // la mazza, figlia del personaggio
    // Un oggetto vuoto figlio della mazza, sulla punta che tocca terra. Il punto di
    // colpo sta sotto di lui nella posa di mira. Vuoto = il punto sta
    // stanceDistance davanti al corpo.
    public Transform clubHead;

    [Header("Pose della mazza")]
    // Transform vuoti figli del personaggio, creati dal menu del componente (tasto
    // destro sul titolo di PlayerSwing): sposti la mazza e salvi la posa.
    public Transform restPose;           // a riposo. Vuoto = dove sta la mazza all'avvio
    public Transform addressPose;        // in mira, ferma. Vuoto = la posa di riposo
    public float addressBlendSpeed = 14f;  // quanto in fretta la mazza va in posa di mira

    [Header("Swing da golf (arco)")]
    // Il centro dell'arco, nello spazio del personaggio: un punto DIETRO IL COLLO.
    // Non le spalle: le spalle non ruotano su se stesse, e' la mazza che gira
    // attorno a quel punto (deciso dall'utente).
    public Vector3 swingPivot = new Vector3(0f, 0.5f, -0.5f);
    // Gradi lungo l'arco. 90 = testa della mazza di lato all'altezza delle spalle,
    // 180 = in alto dietro la testa. Positivo verso la destra del corpo.
    public float backswingAngle = 160f;          // a carica piena, a destra. POSITIVO
    // a fine follow-through, a sinistra. NEGATIVO: positivo andrebbe dalla stessa
    // parte della carica. Scala con la carica: un colpo piano ha un follow-through corto.
    public float followThroughAngle = -160f;
    public bool invertSwingSide = false;         // se carica a sinistra invece che a destra
    // Quanto si stringe l'arco salendo: il raggio (spalle -> mazza) viene
    // moltiplicato per questo numero in cima, come braccia e polsi che si piegano.
    // 1 = arco largo quanto la mazza, 0.5 = in cima la mazza sta a meta' distanza.
    [Range(0.1f, 1f)] public float topRadius = 0.5f;
    public float swingDuration = 0.3f;           // durata della frustata al rilascio
    // A che frazione della frustata la mazza ripassa sul punto di colpo. Prima la
    // discesa dalla carica, dopo il follow-through.
    [Range(0.05f, 0.95f)] public float impactAt = 0.4f;
    // Finito il follow-through, quanti secondi per tornare alla posa di mira
    // ripercorrendo l'arco al contrario. Morbido all'inizio e alla fine. Poi, se non
    // si mira piu', va a riposo con returnSpeed.
    public float recoverTime = 0.4f;
    public float returnSpeed = 6f;               // quanto in fretta torna a riposo, fuori da un colpo

    [Header("Punto di colpo")]
    // Entrando in mira si fissa un ancoraggio strikeReach metri davanti a dove
    // guardava il personaggio. Il corpo si sposta in modo che il punto cada li':
    // e' il "si sposta a sinistra e la mazza diventa protagonista". Con WASD in mira
    // si sposta l'ancoraggio a piccoli passi (PlayerController.aimStepSpeed).
    public float strikeReach = 0.6f;     // quanto davanti al corpo, all'ingresso in mira
    public float stanceDistance = 0.7f;  // ripiego senza clubHead: punto davanti al corpo
    // Raggio della sfera colpita, appoggiata a terra sotto la testa della mazza.
    // Conta il CONTATTO: un oggetto e' preso se il suo collider tocca la sfera,
    // non se il suo centro ci cade dentro (una palla grande ha il centro lontano).
    public float strikeRadius = 0.3f;
    public LayerMask groundMask = ~0;    // dove appoggiare il punto

    [Header("Colpo a mano")]
    public Vector3 meleeAxis = Vector3.right;    // asse del colpo a mano: (1,0,0) = dall'alto in basso
    public float range = 2.5f;           // portata, dal centro del personaggio
    [Range(0f, 360f)] public float arc = 110f;  // ampiezza del ventaglio colpito
    public float cooldown = 0.55f;       // secondi fra un colpo e il successivo
    public float meleeDuration = 0.3f;
    [Range(0f, 1f)] public float hitAt = 0.45f; // a che punto del colpo si prende il bersaglio
    public float meleeFrom = -70f;       // gradi di partenza dell'arco a mano
    public float meleeTo = 70f;          // gradi di arrivo

    // La mazza non toglie vita: butta giu' il nemico (EnemyStun) con questa spinta.
    [Header("Spinta")]
    public float knockForce = 6f;
    public float knockLift = 0.25f;      // quota verso l'alto, in frazione di knockForce

    // Lo swing da golf che prende un nemico al 100% o oltre lo tiene giu' piu' a lungo.
    // Moltiplicano EnemyStun.stunDuration.
    [Header("Stordimento dello swing")]
    public float perfectStunMultiplier = 2f;     // carica al 100%
    public float overchargeStunMultiplier = 2f;  // carica oltre il 100%

    [Header("Cosa si puo' colpire")]
    public LayerMask hitMask = ~0;
    public int maxTargetsPerSwing = 16;  // buffer prealloc: un colpo non alloca niente

    public bool IsSwinging => meleeTimer > 0f || followTimer > 0f;
    public bool IsReady => cooldownTimer <= 0f;

    // Il punto esiste solo in mira: fra BeginStance ed EndStance.
    public bool HasStrikePoint { get; private set; }

    // Il centro del cerchio, appoggiato a terra. Segue il corpo: e' dove la mazza
    // colpirebbe adesso, non dove dovrebbe colpire.
    public Vector3 StrikePoint => SnapToGround(transform.TransformPoint(StrikeLocalOffset()));

    // Il centro della sfera colpita: appoggiata a terra sotto la testa della mazza.
    public Vector3 StrikeCenter => StrikePoint + Vector3.up * strikeRadius;

    private Vector3 anchor;              // dove deve cadere il punto, in mondo
    private float recoverTimer;
    private float recoverFromAngle;      // l'angolo da cui riparte il rientro, sull'arco
    private readonly RaycastHit[] groundHits = new RaycastHit[8];

    private float meleeTimer;
    private float cooldownTimer;
    private float followTimer;
    private bool hitDone;
    private float releasedCharge;        // a che punto della carica e' partita la frustata

    private Vector3 restLocalPosition;
    private Quaternion restLocalRotation;
    private bool restCaptured;

    private Collider[] hits;
    private readonly List<EnemyHealth> alreadyHit = new List<EnemyHealth>();

    void Start()
    {
        if (aim == null) aim = FindFirstObjectByType<SkullAimController>();

        hits = new Collider[Mathf.Max(1, maxTargetsPerSwing)];

        if (clubPivot != null)
        {
            if (restPose != null)
                clubPivot.SetPositionAndRotation(restPose.position, restPose.rotation);

            restLocalPosition = clubPivot.localPosition;
            restLocalRotation = clubPivot.localRotation;
            restCaptured = true;

            // La mazza non deve urtare niente: e' figlia del Rigidbody del corpo, quindi
            // un suo collider fa parte del corpo fisico. In posa di mira la sua punta va
            // proprio dove sta la palla e la spingeva via prima del colpo. Il colpo e'
            // logico (il cerchio), non fisico.
            foreach (Collider c in clubPivot.GetComponentsInChildren<Collider>())
                c.enabled = false;
        }
    }

    // --- Il punto di colpo ------------------------------------------------

    // Dove sta il punto rispetto al corpo, nello spazio del corpo: la testa della
    // mazza quando la mazza e' in posa di mira.
    Vector3 StrikeLocalOffset()
    {
        if (clubHead != null && clubPivot != null)
            return transform.InverseTransformPoint(AddressHeadWorld());

        return new Vector3(0f, 0f, stanceDistance);
    }

    // Fissa l'ancoraggio davanti al corpo, nella direzione data. La chiama
    // SkullAimController entrando in mira.
    public void BeginStance(Vector3 facing)
    {
        facing.y = 0f;
        facing = facing.sqrMagnitude < 0.0001f ? transform.forward : facing.normalized;

        anchor = transform.position + facing * strikeReach;
        HasStrikePoint = true;
    }

    public void EndStance()
    {
        HasStrikePoint = false;
    }

    // L'ancoraggio dove sta gia' la mazza: il corpo non si muove. Serve quando si
    // rientra in mira subito dopo un tiro, gia' in postura: senza, a ogni rientro
    // il corpo rifaceva il passo di lato.
    public void AnchorHere()
    {
        anchor = StrikePoint;
    }

    // I piccoli passi in mira: spostano l'ancoraggio, e il corpo lo segue.
    public void MoveStance(Vector3 delta)
    {
        delta.y = 0f;
        anchor += delta;
    }

    // Dove deve stare il corpo perche' il punto cada sull'ancoraggio, girato verso
    // bodyFacing. Solo in orizzontale: l'altezza la decide la fisica.
    public Vector3 StancePosition(Vector3 bodyFacing)
    {
        bodyFacing.y = 0f;
        if (bodyFacing.sqrMagnitude < 0.0001f) bodyFacing = transform.forward;

        Quaternion rot = Quaternion.LookRotation(bodyFacing.normalized, Vector3.up);
        Vector3 offset = rot * Vector3.Scale(StrikeLocalOffset(), transform.lossyScale);

        Vector3 p = anchor - offset;
        p.y = transform.position.y;
        return p;
    }

    // Vero se il collider tocca la sfera colpita. Fuori dalla mira non c'e' sfera.
    // La legge SkullAimController per sapere se il colpo prende la testa.
    // ClosestPoint e non la distanza dal centro: conta il contatto con la mazza,
    // e una palla grande tocca la mazza col bordo, non col centro.
    public bool IsInStrikeZone(Collider target)
    {
        if (!HasStrikePoint || target == null || !target.enabled) return false;

        Vector3 center = StrikeCenter;
        Vector3 closest = target.ClosestPoint(center);
        return (closest - center).sqrMagnitude <= strikeRadius * strikeRadius;
    }

    // I nemici nel cerchio che il colpo farebbe volare, cioe' quelli con un
    // Rigidbody che la spinta puo' muovere. La usa SkullAimController per disegnare
    // la loro traiettoria. Riempie la lista passata, cosi' non alloca.
    public void CollectFlyingTargets(List<Rigidbody> result)
    {
        result.Clear();
        if (!HasStrikePoint || hits == null || knockForce <= 0f) return;

        int count = OverlapStrike();
        for (int i = 0; i < count; i++)
        {
            EnemyHealth health = hits[i] != null ? hits[i].GetComponentInParent<EnemyHealth>() : null;
            if (health == null || health.IsDead) continue;

            Rigidbody targetRb = health.GetComponent<Rigidbody>();
            if (targetRb == null || result.Contains(targetRb)) continue;

            // un nemico in fila e' kinematic, ma lo stordimento lo rende fisico
            // prima di spingerlo (HitTarget), quindi vola anche lui
            EnemyStun stun = health.GetComponent<EnemyStun>();
            if (targetRb.isKinematic && (stun == null || !stun.CanStun)) continue;

            result.Add(targetRb);
        }
    }

    // L'impulso che la spinta da' a un bersaglio colpito in quella direzione.
    public Vector3 PushImpulse(Vector3 direction)
    {
        if (knockForce <= 0f) return Vector3.zero;

        Vector3 dir = direction;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        dir.Normalize();

        return (dir + Vector3.up * knockLift) * knockForce;
    }

    int OverlapStrike()
    {
        return Physics.OverlapSphereNonAlloc(StrikeCenter, strikeRadius, hits, hitMask,
                                             QueryTriggerInteraction.Ignore);
    }

    // Conta solo il terreno fermo, cioe' i collider senza Rigidbody: altrimenti il
    // punto si appoggerebbe sopra la palla, o sopra un nemico.
    Vector3 SnapToGround(Vector3 p)
    {
        int count = Physics.RaycastNonAlloc(p + Vector3.up, Vector3.down, groundHits, 3f,
                                            groundMask, QueryTriggerInteraction.Ignore);

        float best = float.MaxValue;
        Vector3 result = p;

        for (int i = 0; i < count; i++)
        {
            if (groundHits[i].collider.attachedRigidbody != null) continue;
            if (groundHits[i].distance < best)
            {
                best = groundHits[i].distance;
                result = groundHits[i].point;
            }
        }

        return result;
    }

    // --- Il colpo -----------------------------------------------------------

    // La chiama SkullAimController nel momento in cui il tasto viene rilasciato.
    // Da qui parte la frustata e qui si colpiscono i nemici nel cerchio, spinti
    // lungo "launchDirection" come la palla. La palla la lancia SkullAimController.
    //
    // E' una chiamata diretta e non un controllo sullo stato, perche' l'ordine
    // degli Update fra due script non e' garantito: accorgersi del rilascio
    // guardando lo stato lo farebbe arrivare un frame dopo.
    public void OnShotReleased(float chargeRatio, Vector3 launchDirection,
                               SkullAimController.PowerZone zone)
    {
        releasedCharge = Mathf.Clamp01(chargeRatio);
        followTimer = Mathf.Max(0.01f, swingDuration);
        meleeTimer = 0f;
        cooldownTimer = Mathf.Max(swingDuration, cooldown);

        if (!HasStrikePoint || hits == null) return;

        float stunMultiplier = zone == SkullAimController.PowerZone.Perfect ? perfectStunMultiplier
                             : zone == SkullAimController.PowerZone.Overcharge ? overchargeStunMultiplier
                             : 1f;

        alreadyHit.Clear();
        int count = OverlapStrike();

        for (int i = 0; i < count; i++)
        {
            EnemyHealth health = hits[i] != null ? hits[i].GetComponentInParent<EnemyHealth>() : null;
            if (health == null || health.IsDead || alreadyHit.Contains(health)) continue;

            alreadyHit.Add(health);
            HitTarget(health, launchDirection, stunMultiplier);
        }
    }

    void Update()
    {
        SkullAimController.State state = aim != null
            ? aim.CurrentState
            : SkullAimController.State.Idle;

        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        bool inShotMode = state == SkullAimController.State.Aiming
                          || state == SkullAimController.State.Powering;

        // il colpo a mano si fa solo fuori dalla modalita' tiro: dentro, il tasto
        // sinistro serve a caricare
        if (!inShotMode && followTimer <= 0f)
            ReadMeleeInput();

        Animate(state, inShotMode);
    }

    void ReadMeleeInput()
    {
        if (aim != null && aim.CurrentState != SkullAimController.State.Idle) return;
        if (!IsReady || meleeTimer > 0f) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

        meleeTimer = Mathf.Max(0.01f, meleeDuration);
        cooldownTimer = Mathf.Max(meleeDuration, cooldown);
        hitDone = false;
    }

    // --- Animazione ---------------------------------------------------------

    void Animate(SkullAimController.State state, bool inShotMode)
    {
        if (clubPivot == null || !restCaptured) return;

        // 1. la frustata dopo il rilascio: scende dall'angolo della carica fino al
        // punto di colpo, poi prosegue nel follow-through
        if (followTimer > 0f)
        {
            followTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(followTimer / Mathf.Max(0.01f, swingDuration));

            float angle;
            if (t < impactAt)
            {
                // parte piano e accelera, come una mazza vera
                float u = t / impactAt;
                angle = backswingAngle * releasedCharge * (1f - u * u);
            }
            else
            {
                // dopo l'impatto rallenta fino a fermarsi in cima al follow-through,
                // che e' tanto piu' alto quanto piu' si era caricato
                float v = (t - impactAt) / (1f - impactAt);
                angle = followThroughAngle * releasedCharge * (1f - (1f - v) * (1f - v));
            }

            ApplySwingAngle(angle);

            // finito: da qui la mazza rientra ripercorrendo l'arco al contrario
            if (followTimer <= 0f) StartRecover(angle);
            return;
        }

        // 2. la carica: piu' si tiene premuto, piu' la mazza sale lungo l'arco
        if (state == SkullAimController.State.Powering)
        {
            recoverTimer = 0f;
            ApplySwingAngle(backswingAngle * Mathf.Clamp01(aim.ChargeRatio));
            return;
        }

        // 3. in mira la mazza va in posa, con la testa sul punto di colpo
        if (inShotMode)
        {
            AddressPose(out Vector3 pos, out Quaternion rot);

            if (Recover()) return;

            float k = addressBlendSpeed > 0f ? Mathf.Clamp01(Time.deltaTime * addressBlendSpeed) : 1f;
            clubPivot.SetPositionAndRotation(
                Vector3.Lerp(clubPivot.position, pos, k),
                Quaternion.Slerp(clubPivot.rotation, rot, k));
            return;
        }

        // 4. il colpo a mano
        if (meleeTimer > 0f)
        {
            recoverTimer = 0f;
            meleeTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(meleeTimer / Mathf.Max(0.01f, meleeDuration));

            float eased = t * t;
            SetMeleeAngle(Mathf.Lerp(meleeFrom, meleeTo, eased));

            // il colpo prende una volta sola, a meta' dell'arco: un controllo a
            // ogni frame colpirebbe lo stesso nemico piu' volte nello stesso colpo
            if (!hitDone && t >= hitAt)
            {
                hitDone = true;
                ApplyMeleeHit();
            }
            return;
        }

        // 5. riposo: dopo un colpo prima ripercorre l'arco fino alla posa di mira,
        // poi va a riposo morbidamente con returnSpeed, cosi' non scatta
        if (Recover()) return;

        if (returnSpeed > 0f)
        {
            float k = Mathf.Clamp01(Time.deltaTime * returnSpeed);
            clubPivot.localPosition = Vector3.Lerp(clubPivot.localPosition, restLocalPosition, k);
            clubPivot.localRotation = Quaternion.Slerp(clubPivot.localRotation, restLocalRotation, k);
        }
        else
        {
            clubPivot.localPosition = restLocalPosition;
            clubPivot.localRotation = restLocalRotation;
        }
    }

    // Il rientro parte dall'angolo a cui e' arrivato il follow-through.
    void StartRecover(float fromAngle)
    {
        recoverTimer = recoverTime;
        recoverFromAngle = fromAngle;
    }

    // Il rientro dopo un colpo: ripercorre l'arco del follow-through al contrario,
    // fino alla posa di mira, in recoverTime secondi, lento all'inizio e alla fine.
    // Sull'arco e non in linea retta: in linea retta la mazza attraversava il corpo.
    // L'arco e' nello spazio del corpo, quindi segue il personaggio se cammina.
    // Vero finche' e' in corso.
    bool Recover()
    {
        if (recoverTimer <= 0f || recoverTime <= 0f) return false;

        recoverTimer -= Time.deltaTime;
        float t = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Clamp01(recoverTimer / recoverTime));

        ApplySwingAngle(recoverFromAngle * (1f - t));
        return true;
    }

    // La posa di mira, in mondo. Senza marker e' la posa di riposo.
    void AddressPose(out Vector3 pos, out Quaternion rot)
    {
        if (addressPose != null)
        {
            pos = addressPose.position;
            rot = addressPose.rotation;
            return;
        }

        Transform parent = clubPivot.parent;
        pos = parent != null ? parent.TransformPoint(restLocalPosition) : restLocalPosition;
        rot = parent != null ? parent.rotation * restLocalRotation : restLocalRotation;
    }

    // La testa della mazza nella posa di mira, in mondo. Senza clubHead, il centro
    // della mazza.
    Vector3 AddressHeadWorld()
    {
        // la testa misurata nella mazza, poi riportata sulla posa di mira. La
        // posa ha scala 1, la mazza no: la scala la rimette HeadAt
        AddressPose(out Vector3 pos, out Quaternion rot);
        return HeadAt(pos, rot);
    }

    // L'asse dell'arco, in mondo. E' perpendicolare al piano che contiene la linea
    // spalle -> testa della mazza in mira e la linea delle spalle (la destra del
    // corpo). Ruotando attorno a lui la testa parte dal basso davanti e va verso
    // destra salendo: la componente laterale la fa girare attorno alla capsula,
    // l'inclinazione del piano la fa salire.
    Vector3 SwingAxisWorld()
    {
        Vector3 toHead = AddressHeadWorld() - transform.TransformPoint(swingPivot);
        Vector3 axis = Vector3.Cross(toHead, transform.right);

        // testa esattamente sulla linea delle spalle: il piano non e' definito
        if (axis.sqrMagnitude < 0.0001f) axis = transform.forward;

        axis.Normalize();
        return invertSwingSide ? -axis : axis;
    }

    // La posa di mira ruotata attorno alle spalle. 0 = posa di mira, positivo =
    // carica (verso destra), negativo = follow-through (verso sinistra).
    // Salendo la mazza si avvicina alle spalle (topRadius): senza, l'arco e' largo
    // quanto la distanza spalle -> mazza, e in cima la mazza finiva lontana dal corpo.
    void SwingPose(float degrees, out Vector3 pos, out Quaternion rot)
    {
        AddressPose(out Vector3 basePos, out Quaternion baseRot);

        Vector3 pivot = transform.TransformPoint(swingPivot);
        Quaternion r = Quaternion.AngleAxis(degrees, SwingAxisWorld());

        float rise = Mathf.Clamp01(Mathf.Abs(degrees) / 180f);
        float radius = Mathf.Lerp(1f, topRadius, Mathf.SmoothStep(0f, 1f, rise));

        pos = pivot + r * (basePos - pivot) * radius;
        rot = r * baseRot;
    }

    // Dove sta la testa della mazza con la mazza in quella posa
    Vector3 HeadAt(Vector3 clubPos, Quaternion clubRot)
    {
        if (clubHead == null || clubPivot == null) return clubPos;
        Vector3 headInClub = clubPivot.InverseTransformPoint(clubHead.position);
        return clubPos + clubRot * Vector3.Scale(headInClub, clubPivot.lossyScale);
    }

    void ApplySwingAngle(float degrees)
    {
        SwingPose(degrees, out Vector3 pos, out Quaternion rot);
        clubPivot.SetPositionAndRotation(pos, rot);
    }

    void SetMeleeAngle(float degrees)
    {
        Vector3 axis = meleeAxis.sqrMagnitude < 0.0001f ? Vector3.right : meleeAxis.normalized;
        clubPivot.localPosition = restLocalPosition;
        clubPivot.localRotation = Quaternion.AngleAxis(degrees, axis) * restLocalRotation;
    }

    // --- Colpire ------------------------------------------------------------

    // Il colpo a mano: tutto quello che sta nel ventaglio davanti al corpo, spinto
    // via dal personaggio.
    void ApplyMeleeHit()
    {
        if (hits == null) return;

        alreadyHit.Clear();

        int count = Physics.OverlapSphereNonAlloc(
            transform.position, range, hits, hitMask, QueryTriggerInteraction.Ignore);

        Vector3 facing = new Vector3(transform.forward.x, 0f, transform.forward.z);
        if (facing.sqrMagnitude < 0.0001f) facing = Vector3.forward;
        facing.Normalize();

        float halfArc = arc * 0.5f;

        for (int i = 0; i < count; i++)
        {
            Collider col = hits[i];
            if (col == null) continue;

            // il collider puo' essere figlio del nemico: cerco il componente salendo,
            // come fa SkullExplosion
            EnemyHealth health = col.GetComponentInParent<EnemyHealth>();
            if (health == null || health.IsDead) continue;
            if (alreadyHit.Contains(health)) continue;

            Vector3 toTarget = health.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.0001f
                && Vector3.Angle(facing, toTarget.normalized) > halfArc)
                continue;

            alreadyHit.Add(health);
            HitTarget(health, toTarget);
        }
    }

    // La mazza non fa danno: stordisce. Il nemico cade, rotola e si rialza
    // (EnemyStun). Deciso dall'utente: la palla deve restare l'arma vera.
    // Vale per il colpo a mano e per i nemici presi dallo swing da golf.
    void HitTarget(EnemyHealth health, Vector3 pushDirection, float stunMultiplier = 1f)
    {
        EnemyStun stun = health.GetComponent<EnemyStun>();

        if (stun != null) stun.Stun(PushImpulse(pushDirection), stunMultiplier);
        else Push(health, pushDirection);
    }

    void Push(EnemyHealth health, Vector3 direction)
    {
        Rigidbody targetRb = health.GetComponent<Rigidbody>();
        if (targetRb == null || targetRb.isKinematic) return;

        Vector3 impulse = PushImpulse(direction);
        if (impulse.sqrMagnitude > 0f) targetRb.AddForce(impulse, ForceMode.Impulse);
    }

    // --- Gizmo ----------------------------------------------------------------

    void OnDrawGizmosSelected()
    {
        // il ventaglio colpito dal colpo a mano, per tarare range e arc
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);

        Vector3 origin = transform.position;
        Vector3 facing = new Vector3(transform.forward.x, 0f, transform.forward.z);
        if (facing.sqrMagnitude < 0.0001f) facing = Vector3.forward;
        facing.Normalize();

        float half = arc * 0.5f;
        Vector3 prev = origin + (Quaternion.Euler(0f, -half, 0f) * facing) * range;
        Gizmos.DrawLine(origin, prev);
        Gizmos.DrawLine(origin, origin + (Quaternion.Euler(0f, half, 0f) * facing) * range);

        for (int i = 1; i <= 20; i++)
        {
            Vector3 p = origin
                        + (Quaternion.Euler(0f, Mathf.Lerp(-half, half, i / 20f), 0f) * facing) * range;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }

        if (clubPivot == null) return;

        // fuori dal Play la posa di riposo non e' ancora stata letta: la mazza e'
        // dove l'hai messa tu
        if (!Application.isPlaying)
        {
            restLocalPosition = clubPivot.localPosition;
            restLocalRotation = clubPivot.localRotation;
        }

        // la sfera colpita: in Play dov'e' davvero, fuori dove starebbe. Tutto
        // quello che la tocca viene colpito
        Vector3 c = Application.isPlaying && HasStrikePoint
            ? StrikeCenter
            : transform.TransformPoint(StrikeLocalOffset()) + Vector3.up * strikeRadius;

        Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.9f);
        Gizmos.DrawWireSphere(c, strikeRadius);

        // il centro dell'arco
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.TransformPoint(swingPivot), 0.08f);

        // la mazza in posa di mira (rossa), in cima alla carica (gialla) e in cima
        // al follow-through (arancio): per tarare l'arco senza entrare in Play
        AddressPose(out Vector3 ap, out Quaternion ar);
        DrawClubGizmo(ap, ar, Color.red);

        SwingPose(backswingAngle, out Vector3 bp, out Quaternion br);
        DrawClubGizmo(bp, br, Color.yellow);

        SwingPose(followThroughAngle, out Vector3 fp, out Quaternion fr);
        DrawClubGizmo(fp, fr, new Color(1f, 0.55f, 0.1f));

        // la strada della testa della mazza: dalla fine del follow-through (arancio)
        // alla cima della carica (giallo), passando per il punto di colpo
        SwingPose(followThroughAngle, out Vector3 sp, out Quaternion sr);
        Vector3 lastHead = HeadAt(sp, sr);
        const int steps = 48;
        for (int i = 1; i <= steps; i++)
        {
            float a = Mathf.Lerp(followThroughAngle, backswingAngle, i / (float)steps);
            SwingPose(a, out sp, out sr);
            Vector3 p = HeadAt(sp, sr);
            Gizmos.color = a >= 0f ? Color.yellow : new Color(1f, 0.55f, 0.1f);
            Gizmos.DrawLine(lastHead, p);
            lastHead = p;
        }

        Gizmos.matrix = Matrix4x4.identity;
    }

    void DrawClubGizmo(Vector3 pos, Quaternion rot, Color color)
    {
        MeshFilter mf = clubPivot.GetComponentInChildren<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        // la mesh puo' stare su un figlio della mazza: la sua posizione relativa
        // alla mazza si conserva, la mazza viene messa sulla posa
        Matrix4x4 club = Matrix4x4.TRS(pos, rot, clubPivot.lossyScale);
        Gizmos.matrix = club * clubPivot.worldToLocalMatrix * mf.transform.localToWorldMatrix;
        Gizmos.color = color;
        Gizmos.DrawWireMesh(mf.sharedMesh);
        Gizmos.matrix = Matrix4x4.identity;
    }

#if UNITY_EDITOR
    // --- Menu del componente: salvare le pose -----------------------------------
    // Tasto destro sul titolo di PlayerSwing nell'Inspector. Sposti la mazza in
    // scena, poi salvi la posa: viene creato un marker figlio del personaggio
    // nella posizione e rotazione attuali della mazza.

    [ContextMenu("GOOMD - 1. Salva posa di riposo")]
    void SaveRestPose() { restPose = SavePose(restPose, "Pose_Rest"); }

    [ContextMenu("GOOMD - 2. Salva posa di mira")]
    void SaveAddressPose() { addressPose = SavePose(addressPose, "Pose_Address"); }

    [ContextMenu("GOOMD - 3. Rimetti la mazza a riposo")]
    void ResetClubToRest()
    {
        if (clubPivot == null || restPose == null) return;
        UnityEditor.Undo.RecordObject(clubPivot, "Mazza a riposo");
        clubPivot.SetPositionAndRotation(restPose.position, restPose.rotation);
    }

    [ContextMenu("GOOMD - Crea testa della mazza")]
    void CreateClubHead()
    {
        if (clubPivot == null || clubHead != null) return;

        GameObject go = new GameObject("Club_Head");
        UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Crea testa della mazza");
        go.transform.SetParent(clubPivot, false);
        // il cilindro di Unity va da -1 a +1 sul suo asse Y: parte da un'estremita',
        // da spostare sulla punta vera se serve
        go.transform.localPosition = new Vector3(0f, -1f, 0f);

        UnityEditor.Undo.RecordObject(this, "Crea testa della mazza");
        clubHead = go.transform;
    }

    Transform SavePose(Transform existing, string name)
    {
        if (clubPivot == null) return existing;

        UnityEditor.Undo.RecordObject(this, "Salva posa");

        Transform pose = existing;
        if (pose == null)
        {
            GameObject go = new GameObject(name);
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Salva posa");
            go.transform.SetParent(transform, false);
            pose = go.transform;
        }
        else
        {
            UnityEditor.Undo.RecordObject(pose, "Salva posa");
        }

        pose.SetPositionAndRotation(clubPivot.position, clubPivot.rotation);
        return pose;
    }
#endif
}
