using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Genera la gerarchia dell'HUD gia' cablata su HUDController.
// Costruire a mano una trentina di RectTransform con le ancore giuste e' lungo
// e facile da sbagliare: questo lo fa in un colpo solo. Da qui in poi si
// modifica normalmente nell'Inspector, lo script non serve piu'.
//
// Sta in una cartella Editor: non finisce nella build.
public static class HUDBuilder
{
    // dimensioni di riferimento del CanvasScaler: tutte le coordinate qui
    // sotto sono in questo spazio, non in pixel dello schermo
    private static readonly Vector2 Reference = new Vector2(1920f, 1080f);

    private static readonly Color Ink = new Color(0.95f, 0.94f, 0.90f);
    private static readonly Color InkDim = new Color(0.72f, 0.70f, 0.66f);
    private static readonly Color BarTrack = new Color(0.08f, 0.07f, 0.09f, 0.75f);
    private static readonly Color Accent = new Color(0.95f, 0.75f, 0.25f);
    private static readonly Color Good = new Color(0.35f, 0.85f, 0.45f);
    private static readonly Color Bad = new Color(0.9f, 0.3f, 0.25f);
    private static readonly Color Veil = new Color(0.03f, 0.03f, 0.05f, 0.8f);

    [MenuItem("GOOMD/Crea HUD in scena")]
    public static void CreateHUD()
    {
        // Senza le TMP Essential Resources i testi esistono ma non hanno font
        // e non si vede niente: meglio dirlo subito che far cercare il bug.
        if (Resources.Load("TMP Settings") == null)
        {
            EditorUtility.DisplayDialog(
                "Mancano le risorse di TextMeshPro",
                "Importa prima Window > TextMeshPro > Import TMP Essential Resources, " +
                "poi rilancia GOOMD > Crea HUD in scena.",
                "Ok");
            return;
        }

        if (Object.FindFirstObjectByType<HUDController>() != null)
        {
            EditorUtility.DisplayDialog(
                "HUD gia' presente",
                "In scena c'e' gia' un HUDController. Cancella il vecchio HUD " +
                "prima di generarne un altro.",
                "Ok");
            return;
        }

        GameObject canvasGO = new GameObject("HUD",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = Reference;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        HUDController hud = canvasGO.AddComponent<HUDController>();
        ApplyDefaultGradients(hud);

        Transform root = canvasGO.transform;

        BuildIntegrity(root, hud);
        BuildScores(root, hud);
        BuildObjectives(root, hud);
        BuildPowerMeter(root, hud);
        BuildLoftMeter(root, hud);
        BuildShotCamera(root);
        hud.winPanel = BuildEndPanel(root, hud, true);
        hud.losePanel = BuildEndPanel(root, hud, false);

        EnsureEventSystem();

        Undo.RegisterCreatedObjectUndo(canvasGO, "Crea HUD");
        Selection.activeGameObject = canvasGO;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("HUD creato e cablato. I riferimenti di gioco (GameManager, teschio, " +
                  "SkullAimController) restano vuoti: l'HUDController li cerca da solo in Awake.\n" +
                  "Creati anche Shot_Camera e Shot_Camera_Rig per la finestra della " +
                  "telecronaca. Due cose da controllare: su Shot_Camera_Rig il campo 'player' " +
                  "deve puntare alla capsula del personaggio, e se la finestra resta nera " +
                  "aggiungi a Shot_Camera il componente Universal Additional Camera Data.",
                  canvasGO);
    }

    // Rifa' solo la barra di potenza dentro l'HUD gia' in scena. Rigenerare tutto
    // l'HUD vorrebbe dire rifare anche la telecronaca e ricollegarla.
    [MenuItem("GOOMD/Rifai barra potenza")]
    public static void RebuildPowerMeter()
    {
        HUDController hud = Object.FindFirstObjectByType<HUDController>();
        if (hud == null)
        {
            EditorUtility.DisplayDialog("HUD assente",
                "In scena non c'e' un HUDController. Usa GOOMD > Crea HUD in scena.", "Ok");
            return;
        }

        Transform old = hud.transform.Find("PowerMeter");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        Undo.RecordObject(hud, "Rifai barra potenza");
        BuildPowerMeter(hud.transform, hud);

        Transform created = hud.transform.Find("PowerMeter");
        if (created != null) Undo.RegisterCreatedObjectUndo(created.gameObject, "Rifai barra potenza");

        EditorUtility.SetDirty(hud);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
    }

    // ---------- blocchi dell'HUD ----------

    // in alto a sinistra: la barra vitale del giocatore
    static void BuildIntegrity(Transform root, HUDController hud)
    {
        RectTransform group = NewRect("Integrity", root);
        Place(group, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(440f, 60f));

        TextMeshProUGUI caption = NewText("Caption", group, "INTEGRITA'", 22f,
            TextAlignmentOptions.Left, InkDim);
        Place(caption.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(280f, 26f));

        TextMeshProUGUI value = NewText("Value", group, "100%", 22f,
            TextAlignmentOptions.Right, Ink);
        Place(value.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(140f, 26f));

        Image fill;
        Slider bar = NewBar("Bar", group, out fill);
        StretchHorizontal(bar.GetComponent<RectTransform>(), -30f, 26f);

        hud.integrityBar = bar;
        hud.integrityFill = fill;
        hud.integrityLabel = value;
    }

    // in alto a destra: danni (grande, e' il punteggio) e Power (piccolo, e' valuta)
    static void BuildScores(Transform root, HUDController hud)
    {
        RectTransform group = NewRect("Scores", root);
        Place(group, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(460f, 130f));

        TextMeshProUGUI caption = NewText("DamageCaption", group, "DANNI", 22f,
            TextAlignmentOptions.Right, InkDim);
        Place(caption.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(460f, 26f));

        TextMeshProUGUI damage = NewText("DamageValue", group, "0", 60f,
            TextAlignmentOptions.Right, Accent);
        Place(damage.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -26f), new Vector2(460f, 66f));

        TextMeshProUGUI power = NewText("PowerValue", group, "POWER 0", 24f,
            TextAlignmentOptions.Right, Ink);
        Place(power.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -96f), new Vector2(460f, 30f));

        hud.damageText = damage;
        hud.powerCurrencyText = power;
        hud.powerCurrencyFormat = "POWER {0}";
    }

    // in alto al centro: quanti nemici restano e se la buca si e' aperta
    static void BuildObjectives(Transform root, HUDController hud)
    {
        RectTransform group = NewRect("Objectives", root);
        Place(group, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(520f, 74f));

        TextMeshProUGUI enemies = NewText("Enemies", group, "NEMICI 0 / 0", 26f,
            TextAlignmentOptions.Center, Ink);
        Place(enemies.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(520f, 32f));

        RectTransform indicator = NewRect("HoleIndicator", group);
        Place(indicator, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(520f, 32f));

        TextMeshProUGUI holeLabel = NewText("Label", indicator, "BUCA APERTA", 26f,
            TextAlignmentOptions.Center, Good);
        Stretch(holeLabel.rectTransform);

        indicator.gameObject.SetActive(false);

        hud.enemiesText = enemies;
        hud.enemiesFormat = "NEMICI {0} / {1}";
        hud.holeIndicator = indicator.gameObject;
    }

    // A sinistra, a meta' altezza: una barra verticale che si riempie dal basso,
    // con la percentuale accanto che sale insieme al riempimento. Il modello e'
    // Super Battle Golf (Reference/camerra position + aim.png). Si accende in mira.
    static void BuildPowerMeter(Transform root, HUDController hud)
    {
        RectTransform group = NewRect("PowerMeter", root);
        Place(group, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(200f, -60f), new Vector2(56f, 440f));

        Image fill;
        Slider bar = NewBar("Bar", group, out fill);
        Stretch(bar.GetComponent<RectTransform>());
        bar.direction = Slider.Direction.BottomToTop;

        // il Fill di NewBar e' fatto per crescere in orizzontale: in verticale
        // parte da una striscia piena in larghezza, alta zero
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(1f, 0f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        bar.value = 0f;

        // a sinistra della barra; HUDController ne sposta l'ancora in altezza
        // seguendo la carica
        TextMeshProUGUI value = NewText("Value", group, "0%", 30f,
            TextAlignmentOptions.Right, Ink);
        RectTransform vr = value.rectTransform;
        vr.anchorMin = new Vector2(0f, 0f);
        vr.anchorMax = new Vector2(0f, 0f);
        vr.pivot = new Vector2(1f, 0.5f);
        vr.anchoredPosition = new Vector2(-14f, 0f);
        vr.sizeDelta = new Vector2(120f, 40f);

        // a destra della barra, le strisce delle zone: debole in basso, perfetto
        // attorno al 100%, oltre il massimo in cima. L'altezza la decide l'HUD
        hud.weakBand = NewBand("WeakBand", group, Bad);
        hud.perfectBand = NewBand("PerfectBand", group, hud.perfectColor);
        hud.overchargeBand = NewBand("OverchargeBand", group, hud.overchargeColor);

        group.gameObject.SetActive(false);

        hud.powerRoot = group.gameObject;
        hud.powerBar = bar;
        hud.powerFill = fill;
        hud.powerLabel = value;
    }

    static RectTransform NewBand(string name, Transform parent, Color color)
    {
        RectTransform rt = NewImage(name, parent, color).rectTransform;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(6f, 0f);
        rt.sizeDelta = new Vector2(10f, 0f);
        return rt;
    }

    // L'angolo di tiro, scelto con la rotella. Sta sopra la barra di potenza
    // perche' nella sequenza viene prima: si decide l'angolo, poi si carica.
    static void BuildLoftMeter(Transform root, HUDController hud)
    {
        RectTransform group = NewRect("LoftMeter", root);
        Place(group, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(640f, 56f));

        TextMeshProUGUI caption = NewText("Caption", group, "ANGOLO", 22f,
            TextAlignmentOptions.Left, InkDim);
        Place(caption.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(300f, 26f));

        TextMeshProUGUI value = NewText("Value", group, "0 gradi", 22f,
            TextAlignmentOptions.Right, Ink);
        Place(value.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(300f, 26f));

        Image fill;
        Slider bar = NewBar("Bar", group, out fill);
        StretchHorizontal(bar.GetComponent<RectTransform>(), -30f, 18f);
        bar.value = 0f;
        fill.color = new Color(0.4f, 0.8f, 1f);

        group.gameObject.SetActive(false);

        hud.loftRoot = group.gameObject;
        hud.loftLabel = value;
        hud.loftBar = bar;
    }

    // La finestrella della telecronaca, in basso a destra, e la camera che la
    // riempie. Si accende da sola quando un colpo prende la testa.
    //
    // Non e' cablata su HUDController: la gestisce ShotCamera, che non e' UI. Qui
    // si costruisce solo la cornice e si collega il tutto.
    static void BuildShotCamera(Transform root)
    {
        RectTransform frame = NewRect("ShotCamera", root);
        Place(frame, new Vector2(1f, 0f), new Vector2(1f, 0f),
              new Vector2(-40f, 40f), new Vector2(440f, 288f));

        // fondo scuro: senza, la finestra non si stacca dallo sfondo del gioco
        Image backdrop = NewImage("Backdrop", frame, Veil);
        Stretch(backdrop.rectTransform);

        RectTransform viewRect = NewRect("View", frame);
        Place(viewRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
              new Vector2(0f, 8f), new Vector2(424f, 238f));

        RawImage view = viewRect.gameObject.AddComponent<RawImage>();
        view.raycastTarget = false;

        TextMeshProUGUI caption = NewText("Caption", frame, "IL TIRO", 20f,
            TextAlignmentOptions.Center, InkDim);
        Place(caption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
              new Vector2(0f, -6f), new Vector2(424f, 24f));

        frame.gameObject.SetActive(false);

        // la camera che riempie la finestra. Resta spenta finche' non serve: una
        // camera accesa disegna la scena a ogni frame anche se non la guardi.
        GameObject camGO = new GameObject("Shot_Camera", typeof(Camera));
        Camera cam = camGO.GetComponent<Camera>();
        cam.enabled = false;

        GameObject rigGO = new GameObject("Shot_Camera_Rig");
        ShotCamera rig = rigGO.AddComponent<ShotCamera>();
        rig.shotCamera = cam;
        rig.display = view;
        rig.frame = frame.gameObject;

        PlayerController found = Object.FindFirstObjectByType<PlayerController>();
        if (found != null) rig.player = found.transform;

        Undo.RegisterCreatedObjectUndo(camGO, "Crea HUD");
        Undo.RegisterCreatedObjectUndo(rigGO, "Crea HUD");
    }

    static GameObject BuildEndPanel(Transform root, HUDController hud, bool win)
    {
        RectTransform panel = NewRect(win ? "WinPanel" : "LosePanel", root);
        Stretch(panel);

        Image veil = NewImage("Veil", panel, Veil);
        Stretch(veil.rectTransform);

        TextMeshProUGUI title = NewText("Title", panel, win ? "VITTORIA" : "SCONFITTA", 84f,
            TextAlignmentOptions.Center, win ? Good : Bad);
        Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 150f), new Vector2(1000f, 100f));

        TextMeshProUGUI reason = NewText("Reason", panel, "-", 34f,
            TextAlignmentOptions.Center, Ink);
        Place(reason.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 60f), new Vector2(1000f, 50f));

        TextMeshProUGUI stats = NewText("Stats", panel, "-", 28f,
            TextAlignmentOptions.Center, InkDim);
        Place(stats.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f), new Vector2(1000f, 44f));

        // il bottone e' l'unica cosa cliccabile dell'HUD: tutto il resto ha
        // raycastTarget spento per non rubare i click alla camera
        RectTransform button = NewRect("RestartButton", panel);
        Place(button, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -100f), new Vector2(300f, 68f));

        Image buttonBg = button.gameObject.AddComponent<Image>();
        buttonBg.color = new Color(0.16f, 0.15f, 0.18f, 0.95f);

        Button btn = button.gameObject.AddComponent<Button>();
        btn.targetGraphic = buttonBg;
        UnityEventTools.AddPersistentListener(btn.onClick, hud.RestartLevel);

        TextMeshProUGUI btnLabel = NewText("Label", button, "RIGIOCA", 30f,
            TextAlignmentOptions.Center, Ink);
        Stretch(btnLabel.rectTransform);

        if (win)
        {
            hud.winReasonText = reason;
            hud.winStatsText = stats;
        }
        else
        {
            hud.loseReasonText = reason;
            hud.loseStatsText = stats;
        }

        panel.gameObject.SetActive(false);
        return panel.gameObject;
    }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Undo.RegisterCreatedObjectUndo(go, "Crea EventSystem");
    }

    // ---------- utilita' ----------

    static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // largo quanto il genitore, agganciato in alto: cosi' allargando il gruppo
    // nell'Inspector la barra segue senza doverla ritoccare
    static void StretchHorizontal(RectTransform rt, float top, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, top);
        rt.sizeDelta = new Vector2(0f, height);
    }

    static TextMeshProUGUI NewText(string name, Transform parent, string content, float size,
                                   TextAlignmentOptions alignment, Color color)
    {
        RectTransform rt = NewRect(name, parent);
        TextMeshProUGUI text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    static Image NewImage(string name, Transform parent, Color color)
    {
        RectTransform rt = NewRect(name, parent);
        Image image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static Slider NewBar(string name, Transform parent, out Image fill)
    {
        RectTransform rt = NewRect(name, parent);

        Slider slider = rt.gameObject.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;
        slider.interactable = false;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        Navigation nav = slider.navigation;
        nav.mode = Navigation.Mode.None;
        slider.navigation = nav;

        Image track = NewImage("Background", rt, BarTrack);
        Stretch(track.rectTransform);

        RectTransform area = NewRect("Fill Area", rt);
        Stretch(area);

        fill = NewImage("Fill", area, Accent);
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // e' lo Slider a guidare la larghezza del Fill a ogni cambio di value
        slider.fillRect = fillRect;

        return slider;
    }

    // stessi gradienti del Reset() di HUDController: aggiungendo il componente
    // via codice Reset() non viene chiamato, quindi li rimetto qui
    static void ApplyDefaultGradients(HUDController hud)
    {
        hud.integrityGradient = Gradient3(
            new Color(0.85f, 0.15f, 0.15f),
            new Color(0.95f, 0.75f, 0.15f),
            new Color(0.25f, 0.8f, 0.35f));

        hud.powerGradient = Gradient3(
            new Color(0.95f, 0.85f, 0.3f),
            new Color(0.95f, 0.55f, 0.15f),
            new Color(0.9f, 0.2f, 0.15f));
    }

    static Gradient Gradient3(Color a, Color b, Color c)
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
