using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Runtime-built, phone-first learning interface and AR circuit controller.
/// It deliberately builds its own safe-area UI so scene hierarchy wiring cannot leave a blank app.
/// </summary>
public sealed class ARLogicLabController : MonoBehaviour
{
    [Header("AR Setup (scene references are discovered automatically when empty)")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private GameObject boardPrefab;

    // Kept for scene compatibility with the original prototype. The responsive UI is created at runtime.
    [Header("Legacy scene UI references (optional)")]
    [SerializeField] private Button inputAButton;
    [SerializeField] private Button inputBButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private TMP_Text inputAText;
    [SerializeField] private TMP_Text inputBText;
    [SerializeField] private TMP_Text outputText;
    [SerializeField] private TMP_Text statusText;

    private enum Page { Learn, Lab, Quiz }
    private Page currentPage;
    private LogicGateType selectedGate = LogicGateType.And;
    private int lessonIndex;
    private bool inputA;
    private bool inputB;
    private bool soundEnabled = true;
    private bool answered;
    private int quizIndex;
    private int quizScore;
    private int codeStep = 2;
    private GameObject placedBoard;
    private GateCircuitVisual circuitVisual;
    private Canvas canvas;
    private RectTransform safeRoot;
    private TextMeshProUGUI statusLabel;
    private TextMeshProUGUI lessonTitle;
    private TextMeshProUGUI lessonCopy;
    private TextMeshProUGUI lessonFormula;
    private TextMeshProUGUI labFormula;
    private TextMeshProUGUI inputAValue;
    private TextMeshProUGUI inputBValue;
    private TextMeshProUGUI outputValue;
    private TextMeshProUGUI codeDisplay;
    private TextMeshProUGUI codeStepDisplay;
    private TextMeshProUGUI quizPrompt;
    private TextMeshProUGUI quizFeedback;
    private TextMeshProUGUI quizScoreLabel;
    private Button[] gateButtons;
    private Button quizZeroButton;
    private Button quizOneButton;
    private Button quizNextButton;
    private AudioSource audioSource;
    private AudioClip feedbackClip;
    private Coroutine codeRoutine;
#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject nativeTts;
    private bool ttsReady;
#endif
    private readonly List<LogicQuizQuestion> quizBank = LogicQuizQuestion.CreateBank();
    private static readonly List<ARRaycastHit> RaycastHits = new List<ARRaycastHit>();
    private static Sprite whiteSprite;

    private static readonly Color Navy = Hex("0A1422");
    private static readonly Color Panel = Hex("122238");
    private static readonly Color PanelLight = Hex("1A3048");
    private static readonly Color Cyan = Hex("42D6E8");
    private static readonly Color Muted = Hex("9DB1C4");
    private static readonly Color Green = Hex("49DDA0");
    private static readonly Color Red = Hex("FF747E");
    private static readonly Color White = Hex("F3F8FC");

    private void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Screen.orientation = ScreenOrientation.Portrait;
#endif
        if (raycastManager == null) raycastManager = FindFirstObjectByType<ARRaycastManager>();
        EnsureEventSystem();
        HidePrototypeCanvas();
        BuildInterface();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        feedbackClip = CreateFeedbackTone();
        quizScore = PlayerPrefs.GetInt("ARLogicLab.QuizScore", 0);
        ShuffleQuizBank();
        RenderAll();
    }

    private void Update()
    {
        if (currentPage != Page.Lab || placedBoard != null || raycastManager == null) return;

        Vector2 screenPosition;
        int pointerId;
        if (!TryGetPointerDown(out screenPosition, out pointerId)) return;
        if (EventSystem.current != null &&
            (pointerId < 0 ? EventSystem.current.IsPointerOverGameObject() : EventSystem.current.IsPointerOverGameObject(pointerId))) return;

        if (!raycastManager.Raycast(screenPosition, RaycastHits, TrackableType.PlaneWithinPolygon))
        {
            SetStatus("No surface found yet | move slowly to scan a flat area");
            return;
        }

        Pose pose = RaycastHits[0].pose;
        Camera camera = Camera.main;
        if (camera != null)
        {
            Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, pose.up);
            if (forward.sqrMagnitude > .001f) pose.rotation = Quaternion.LookRotation(forward, pose.up);
        }
        placedBoard = new GameObject("AR LogicLab Circuit");
        placedBoard.transform.SetPositionAndRotation(pose.position + pose.up * .01f, pose.rotation);
        circuitVisual = placedBoard.AddComponent<GateCircuitVisual>();
        circuitVisual.Configure(selectedGate, inputA, inputB);
        SetStatus("Circuit placed | change an input to explore the truth table");
        PlayFeedback();
    }

    private static bool TryGetPointerDown(out Vector2 position, out int pointerId)
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            position = Touchscreen.current.primaryTouch.position.ReadValue();
            pointerId = (int)Touchscreen.current.primaryTouch.touchId.ReadValue();
            return true;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            position = Mouse.current.position.ReadValue();
            pointerId = -1;
            return true;
        }
        position = Vector2.zero;
        pointerId = -1;
        return false;
    }

    private void EnsureEventSystem()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject system = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem = system.GetComponent<EventSystem>();
        }
        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
        {
            BaseInputModule[] oldModules = eventSystem.GetComponents<BaseInputModule>();
            foreach (BaseInputModule oldModule in oldModules) Destroy(oldModule);
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }
        InputSystemUIInputModule inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        inputModule.AssignDefaultActions();
    }

    private void HidePrototypeCanvas()
    {
        Canvas[] existing = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas oldCanvas in existing)
        {
            if (oldCanvas.GetComponentInParent<ARLogicLabController>() == null)
                oldCanvas.gameObject.SetActive(false);
        }
    }

    private void BuildInterface()
    {
        GameObject root = new GameObject("LogicLab Runtime UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;

        GameObject safe = new GameObject("Safe Area", typeof(RectTransform));
        safe.transform.SetParent(root.transform, false);
        safeRoot = safe.GetComponent<RectTransform>();
        ApplySafeArea();

        RectTransform header = CreatePanel("Top bar", safeRoot, new Vector2(0, 1), new Vector2(1, 1), new Vector2(14, -154), new Vector2(-14, -12), Panel);
        CreateText("Brand", header, "LOGICLAB", 34, Cyan, TextAlignmentOptions.Left, new Vector2(22, -18), new Vector2(380, 54), FontStyles.Bold);
        CreateText("Tagline", header, "AR CIRCUIT STUDIO", 18, Muted, TextAlignmentOptions.Left, new Vector2(25, -76), new Vector2(390, 35), FontStyles.Bold);
        statusLabel = CreateText("AR status", header, "Preparing AR", 19, White, TextAlignmentOptions.Right, new Vector2(-24, -35), new Vector2(480, 42), FontStyles.Normal);
        statusLabel.rectTransform.anchorMin = statusLabel.rectTransform.anchorMax = new Vector2(1, 1);
        statusLabel.rectTransform.pivot = new Vector2(1, 1);
        statusLabel.rectTransform.anchoredPosition = new Vector2(-24, -24);
        statusLabel.rectTransform.sizeDelta = new Vector2(610, 42);
        Button sound = CreateButton("Sound toggle", header, "AUDIO ON", new Vector2(0, 0), new Vector2(178, 42), PanelLight, 16);
        RectTransform soundRect = sound.GetComponent<RectTransform>();
        soundRect.anchorMin = soundRect.anchorMax = new Vector2(1, 0);
        soundRect.anchoredPosition = new Vector2(-98, 26);
        sound.onClick.AddListener(ToggleSound);

        BuildLearnPage(safeRoot);
        BuildLabPage(safeRoot);
        BuildQuizPage(safeRoot);
        BuildNavigation(safeRoot);
        SetPage(Page.Learn);
        InitializeNarration();
    }

    private void BuildLearnPage(RectTransform parent)
    {
        GameObject page = new GameObject("Learn page", typeof(RectTransform));
        page.transform.SetParent(parent, false);
        RectTransform rect = page.GetComponent<RectTransform>();
        Stretch(rect);
        RectTransform card = CreatePanel("Lesson card", rect, new Vector2(.04f, 1), new Vector2(.96f, 1), new Vector2(0, -650), new Vector2(0, -182), Panel);
        CreateText("Course label", card, "THE LOGIC GATE FIELD GUIDE", 17, Cyan, TextAlignmentOptions.Left, new Vector2(26, -22), new Vector2(560, 32), FontStyles.Bold);
        lessonTitle = CreateText("Lesson title", card, "", 31, White, TextAlignmentOptions.Left, new Vector2(26, -70), new Vector2(850, 55), FontStyles.Bold);
        lessonCopy = CreateText("Lesson explanation", card, "", 22, White, TextAlignmentOptions.TopLeft, new Vector2(26, -145), new Vector2(850, 140), FontStyles.Normal);
        CreateText("Truth table hint", card, "PREDICT  >  SWITCH  >  OBSERVE", 17, Muted, TextAlignmentOptions.Left, new Vector2(26, -300), new Vector2(480, 30), FontStyles.Bold);
        lessonFormula = CreateText("Lesson formula", card, "", 25, Cyan, TextAlignmentOptions.Left, new Vector2(26, -350), new Vector2(850, 45), FontStyles.Bold);
        Button previous = CreateButton("Previous lesson", card, "PREVIOUS", new Vector2(145, -420), new Vector2(250, 70), PanelLight, 19);
        Button next = CreateButton("Next lesson", card, "NEXT", new Vector2(425, -420), new Vector2(250, 70), Cyan, 19, Navy);
        previous.onClick.AddListener(() => ChangeLesson(-1));
        next.onClick.AddListener(() => ChangeLesson(1));
        Button narration = CreateButton("Play lesson audio", card, "READ LESSON ALOUD", new Vector2(0, -505), new Vector2(560, 64), PanelLight, 17);
        narration.onClick.AddListener(PlayLessonNarration);
        Button practice = CreateButton("Practice lesson", rect, "OPEN INTERACTIVE LAB", new Vector2(0, -845), new Vector2(640, 78), Green, 20, Navy);
        practice.onClick.AddListener(() => SetPage(Page.Lab));
    }

    private void BuildLabPage(RectTransform parent)
    {
        GameObject page = new GameObject("Interactive lab page", typeof(RectTransform));
        page.transform.SetParent(parent, false);
        RectTransform rect = page.GetComponent<RectTransform>();
        Stretch(rect);
        CreateText("Lab heading", rect, "BUILD A CIRCUIT", 18, Cyan, TextAlignmentOptions.Left, new Vector2(26, -178), new Vector2(400, 36), FontStyles.Bold);
        CreateText("Gate picker hint", rect, "CHOOSE A GATE", 15, Muted, TextAlignmentOptions.Left, new Vector2(26, -220), new Vector2(250, 28), FontStyles.Bold);
        gateButtons = new Button[LogicGateDefinition.All.Length];
        for (int i = 0; i < gateButtons.Length; i++)
        {
            int index = i;
            float x = (i - 3) * 137f;
            gateButtons[i] = CreateButton("Select " + LogicGateDefinition.Name(LogicGateDefinition.All[i]), rect,
                LogicGateDefinition.Name(LogicGateDefinition.All[i]), new Vector2(x, -278), new Vector2(124, 65), Panel, 17);
            gateButtons[i].onClick.AddListener(() => SelectGate(LogicGateDefinition.All[index]));
        }

        RectTransform card = CreatePanel("Circuit controls", rect, new Vector2(.035f, 0), new Vector2(.965f, 0), new Vector2(0, 145), new Vector2(0, 560), Panel);
        labFormula = CreateText("Live rule", card, "", 24, Cyan, TextAlignmentOptions.Center, new Vector2(0, -20), new Vector2(850, 42), FontStyles.Bold);
        Button a = CreateButton("Toggle input A", card, "INPUT A   0", new Vector2(-285, -112), new Vector2(260, 78), PanelLight, 21);
        Button b = CreateButton("Toggle input B", card, "INPUT B   0", new Vector2(0, -112), new Vector2(260, 78), PanelLight, 21);
        Button reset = CreateButton("Reset circuit", card, "RESET", new Vector2(285, -112), new Vector2(178, 78), PanelLight, 18);
        a.onClick.AddListener(ToggleA);
        b.onClick.AddListener(ToggleB);
        reset.onClick.AddListener(ResetCircuit);
        inputAValue = a.GetComponentInChildren<TextMeshProUGUI>();
        inputBValue = b.GetComponentInChildren<TextMeshProUGUI>();
        outputValue = CreateText("Truth output", card, "Y = 0", 25, White, TextAlignmentOptions.Center, new Vector2(0, -178), new Vector2(360, 40), FontStyles.Bold);
        codeDisplay = CreateText("Code listing", rect, "", 18, White, TextAlignmentOptions.TopLeft, new Vector2(35, -385), new Vector2(660, 128), FontStyles.Normal, true);
        codeStepDisplay = CreateText("Code walkthrough", rect, "STEP 1 / 4", 16, Muted, TextAlignmentOptions.TopLeft, new Vector2(35, -525), new Vector2(460, 32), FontStyles.Bold);
        Button step = CreateButton("Advance code step", rect, "NEXT CODE STEP", new Vector2(0, -580), new Vector2(430, 60), PanelLight, 17);
        step.onClick.AddListener(AdvanceCodeStep);
        Button place = CreateButton("Place AR circuit", rect, "SCAN & PLACE CIRCUIT", new Vector2(0, -655), new Vector2(600, 68), Cyan, 18, Navy);
        place.onClick.AddListener(() => SetStatus("Point at a flat surface, then tap the camera view"));
    }

    private void BuildQuizPage(RectTransform parent)
    {
        GameObject page = new GameObject("Quiz page", typeof(RectTransform));
        page.transform.SetParent(parent, false);
        RectTransform rect = page.GetComponent<RectTransform>();
        Stretch(rect);
        RectTransform card = CreatePanel("Quiz card", rect, new Vector2(.04f, .5f), new Vector2(.96f, .5f), new Vector2(0, -390), new Vector2(0, 390), Panel);
        CreateText("Quiz label", card, "QUICK CHECK  |  28 QUESTIONS", 17, Cyan, TextAlignmentOptions.Left, new Vector2(28, -22), new Vector2(650, 32), FontStyles.Bold);
        quizScoreLabel = CreateText("Quiz score", card, "SCORE  0", 17, Muted, TextAlignmentOptions.Right, new Vector2(-25, -22), new Vector2(240, 32), FontStyles.Bold);
        quizPrompt = CreateText("Question", card, "", 27, White, TextAlignmentOptions.Center, new Vector2(0, -125), new Vector2(860, 155), FontStyles.Bold);
        quizZeroButton = CreateButton("Answer zero", card, "0   ·   LOW", new Vector2(-190, -260), new Vector2(330, 85), PanelLight, 23);
        quizOneButton = CreateButton("Answer one", card, "1   ·   HIGH", new Vector2(190, -260), new Vector2(330, 85), PanelLight, 23);
        quizZeroButton.onClick.AddListener(() => AnswerQuiz(false));
        quizOneButton.onClick.AddListener(() => AnswerQuiz(true));
        quizFeedback = CreateText("Quiz feedback", card, "", 19, Muted, TextAlignmentOptions.Center, new Vector2(0, -342), new Vector2(820, 38), FontStyles.Normal);
        quizNextButton = CreateButton("Next question", card, "NEXT QUESTION", new Vector2(0, -450), new Vector2(430, 68), Cyan, 18, Navy);
        quizNextButton.onClick.AddListener(NextQuestion);
        Button retry = CreateButton("Restart quiz", rect, "SHUFFLE & RESTART", new Vector2(0, 130), new Vector2(470, 56), PanelLight, 16);
        RectTransform retryRect = retry.GetComponent<RectTransform>();
        retryRect.anchorMin = retryRect.anchorMax = Vector2.zero;
        retryRect.pivot = Vector2.zero;
        retryRect.anchoredPosition = new Vector2(0, 130);
        retry.onClick.AddListener(RestartQuiz);
        RenderQuizQuestion();
    }

    private void BuildNavigation(RectTransform parent)
    {
        RectTransform nav = CreatePanel("Bottom navigation", parent, new Vector2(.035f, 0), new Vector2(.965f, 0), new Vector2(0, 20), new Vector2(0, 116), Panel);
        Button learn = CreateButton("Learn tab", nav, "LEARN", new Vector2(-300, 0), new Vector2(250, 72), PanelLight, 18);
        Button lab = CreateButton("Lab tab", nav, "CIRCUIT LAB", new Vector2(0, 0), new Vector2(310, 72), PanelLight, 18);
        Button quiz = CreateButton("Quiz tab", nav, "QUIZ", new Vector2(300, 0), new Vector2(250, 72), PanelLight, 18);
        learn.onClick.AddListener(() => SetPage(Page.Learn));
        lab.onClick.AddListener(() => SetPage(Page.Lab));
        quiz.onClick.AddListener(() => SetPage(Page.Quiz));
    }

    private void BuildInterfacePages()
    {
        // Reserved for future chapters; all current pages are constructed explicitly above.
    }

    private void RenderAll()
    {
        RenderLesson();
        RenderCircuit();
        RenderCode();
        RenderQuizQuestion();
        SetStatus("AR READY  |  OPEN CIRCUIT LAB TO BEGIN");
    }

    private void RenderLesson()
    {
        LogicLesson lesson = LogicLessonCatalog.Lessons[lessonIndex];
        lessonTitle.text = lesson.title + "     " + (lessonIndex + 1) + " / " + LogicLessonCatalog.Lessons.Length;
        lessonCopy.text = lesson.concept + "\n\n" + lesson.example;
        lessonFormula.text = LogicGateDefinition.Formula(lesson.gate);
    }

    private void RenderCircuit()
    {
        bool output = LogicGateDefinition.Evaluate(selectedGate, inputA, inputB);
        labFormula.text = LogicGateDefinition.Name(selectedGate) + "   |   " + LogicGateDefinition.Formula(selectedGate);
        inputAValue.text = "INPUT A   " + Bit(inputA);
        inputBValue.text = selectedGate == LogicGateType.Not ? "INPUT B   -" : "INPUT B   " + Bit(inputB);
        inputBValue.transform.parent.GetComponent<Button>().interactable = selectedGate != LogicGateType.Not;
        outputValue.text = "OUTPUT Y   " + Bit(output);
        outputValue.color = output ? Green : White;
        for (int i = 0; i < gateButtons.Length; i++)
        {
            bool selected = LogicGateDefinition.All[i] == selectedGate;
            gateButtons[i].GetComponent<Image>().color = selected ? Cyan : Panel;
            gateButtons[i].GetComponentInChildren<TextMeshProUGUI>().color = selected ? Navy : White;
        }
        if (circuitVisual != null) circuitVisual.Configure(selectedGate, inputA, inputB);
    }

    private void RenderCode()
    {
        string[] lines = LogicGateDefinition.CodeLines(selectedGate);
        codeStep = Mathf.Clamp(codeStep, 0, lines.Length - 1);
        var richText = new StringBuilder();
        for (int i = 0; i < lines.Length; i++)
        {
            if (i == codeStep) richText.Append("<color=#42D6E8><b>> ");
            else richText.Append("<color=#9DB1C4>   ");
            richText.Append(lines[i]).Append("</color>");
            if (i < lines.Length - 1) richText.Append('\n');
        }
        codeDisplay.text = richText.ToString();
        codeStepDisplay.text = "LIVE RESULT: " + LogicGateDefinition.Formula(selectedGate) + "     |     STEP " + (codeStep + 1) + " / " + lines.Length;
    }

    private void RenderQuizQuestion()
    {
        if (quizBank.Count == 0) return;
        LogicQuizQuestion question = quizBank[quizIndex % quizBank.Count];
        quizPrompt.text = "What is the output Y?\n\n" + LogicGateDefinition.Name(question.gate) + " gate     |     A = " + Bit(question.a) + "     B = " + Bit(question.b);
        quizScoreLabel.text = "SCORE  " + quizScore;
        quizZeroButton.interactable = !answered;
        quizOneButton.interactable = !answered;
        quizNextButton.interactable = answered;
        if (!answered) quizFeedback.text = "Choose LOW (0) or HIGH (1)";
    }

    private void SetPage(Page page)
    {
        currentPage = page;
        if (safeRoot == null) return;
        SetChildActive("Learn page", page == Page.Learn);
        SetChildActive("Interactive lab page", page == Page.Lab);
        SetChildActive("Quiz page", page == Page.Quiz);
        SetStatus(page == Page.Lab
            ? "CIRCUIT LAB  |  TAP A DETECTED SURFACE TO PLACE"
            : page == Page.Quiz ? "QUIZ  |  PRACTISE ALL SEVEN LOGIC GATES" : "LESSONS  |  FIVE SHORT, HANDS-ON CHAPTERS");
    }

    private void SetChildActive(string name, bool active)
    {
        Transform child = safeRoot.Find(name);
        if (child != null) child.gameObject.SetActive(active);
    }

    private void ToggleA()
    {
        inputA = !inputA;
        RenderCircuit();
        PlayFeedback();
    }

    private void ToggleB()
    {
        if (selectedGate == LogicGateType.Not) return;
        inputB = !inputB;
        RenderCircuit();
        PlayFeedback();
    }

    private void SelectGate(LogicGateType gate)
    {
        selectedGate = gate;
        inputA = false;
        inputB = false;
        codeStep = 2;
        RenderCircuit();
        RenderCode();
        SetStatus(LogicGateDefinition.Name(gate) + " SELECTED  |  FORMULA AND LIVE OUTPUT UPDATED");
    }

    private void ChangeLesson(int delta)
    {
        lessonIndex = (lessonIndex + delta + LogicLessonCatalog.Lessons.Length) % LogicLessonCatalog.Lessons.Length;
        RenderLesson();
    }

    private void AdvanceCodeStep()
    {
        int count = LogicGateDefinition.CodeLines(selectedGate).Length;
        codeStep = (codeStep + 1) % count;
        RenderCode();
    }

    private void AnswerQuiz(bool answer)
    {
        if (answered) return;
        LogicQuizQuestion question = quizBank[quizIndex % quizBank.Count];
        answered = true;
        bool correct = answer == question.answer;
        if (correct)
        {
            quizScore++;
            quizFeedback.color = Green;
            quizFeedback.text = "Correct!  " + LogicGateDefinition.Formula(question.gate) + " gives Y = " + Bit(question.answer) + ".";
            PlayerPrefs.SetInt("ARLogicLab.QuizScore", quizScore);
            PlayerPrefs.Save();
        }
        else
        {
            quizFeedback.color = Red;
            quizFeedback.text = "Not quite.  " + LogicGateDefinition.Formula(question.gate) + " gives Y = " + Bit(question.answer) + ".";
        }
        RenderQuizQuestion();
        PlayFeedback();
    }

    private void NextQuestion()
    {
        if (!answered) return;
        quizIndex = (quizIndex + 1) % quizBank.Count;
        answered = false;
        RenderQuizQuestion();
    }

    private void RestartQuiz()
    {
        ShuffleQuizBank();
        quizIndex = 0;
        answered = false;
        RenderQuizQuestion();
    }

    private void ShuffleQuizBank()
    {
        for (int i = quizBank.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            LogicQuizQuestion item = quizBank[i];
            quizBank[i] = quizBank[j];
            quizBank[j] = item;
        }
    }

    private void ResetCircuit()
    {
        if (placedBoard != null) Destroy(placedBoard);
        placedBoard = null;
        circuitVisual = null;
        inputA = inputB = false;
        RenderCircuit();
        SetStatus("Circuit cleared | scan a flat surface and tap to place it again");
    }

    private void ToggleSound()
    {
        soundEnabled = !soundEnabled;
        Button button = safeRoot.Find("Top bar/Sound toggle").GetComponent<Button>();
        button.GetComponentInChildren<TextMeshProUGUI>().text = soundEnabled ? "AUDIO ON" : "AUDIO OFF";
        SetStatus(soundEnabled ? "SOUND CUES ENABLED  |  SUBTITLES STAY ON SCREEN" : "SOUND CUES OFF  |  SUBTITLES STAY ON SCREEN");
    }

    private void PlayLessonNarration()
    {
        LogicLesson lesson = LogicLessonCatalog.Lessons[lessonIndex];
        // The lesson copy stays visible as a transcript while Android's built-in speech engine reads it.
        SetStatus("CAPTION  |  " + lesson.concept);
#if UNITY_ANDROID && !UNITY_EDITOR
        if (ttsReady && nativeTts != null)
        {
            using (AndroidJavaClass localeClass = new AndroidJavaClass("java.util.Locale"))
            using (AndroidJavaObject locale = localeClass.GetStatic<AndroidJavaObject>("US"))
            {
                nativeTts.Call<int>("setLanguage", locale);
                nativeTts.Call<int>("speak", lesson.title + ". " + lesson.concept + " " + lesson.example, 0, null, "logiclab-lesson");
            }
            return;
        }
#endif
        PlayFeedback();
        SetStatus("LESSON TRANSCRIPT  |  " + lesson.concept);
    }

    private void InitializeNarration()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            nativeTts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, new AndroidTtsInitListener(() => ttsReady = true));
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private sealed class AndroidTtsInitListener : AndroidJavaProxy
    {
        private readonly System.Action ready;
        public AndroidTtsInitListener(System.Action ready) : base("android.speech.tts.TextToSpeech$OnInitListener") { this.ready = ready; }
        public void onInit(int status) { if (status == 0) ready(); }
    }
#endif

    private void PlayFeedback()
    {
        if (!soundEnabled || audioSource == null || feedbackClip == null) return;
        audioSource.PlayOneShot(feedbackClip, .16f);
    }

    private void SetStatus(string text)
    {
        if (statusLabel != null) statusLabel.text = text;
        if (statusText != null) statusText.text = text;
    }

    private void ApplySafeArea()
    {
        if (safeRoot == null) return;
        Rect safe = Screen.safeArea;
        safeRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
        safeRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
    }

    private void OnRectTransformDimensionsChange()
    {
        if (safeRoot != null) ApplySafeArea();
    }

    private RectTransform CreatePanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(Image));
        item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        Image image = item.GetComponent<Image>();
        image.sprite = WhiteSprite();
        image.color = color;
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;
        return rect;
    }

    private TextMeshProUGUI CreateText(string name, RectTransform parent, string text, float fontSize, Color color,
        TextAlignmentOptions alignment, Vector2 anchoredPosition, Vector2 size, FontStyles style, bool monospace = false)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
        rect.pivot = new Vector2(.5f, 1);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        TextMeshProUGUI label = item.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.fontStyle = style;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset;
        if (monospace) label.fontSize = fontSize;
        return label;
    }

    private Button CreateButton(string name, RectTransform parent, string label, Vector2 position, Vector2 size,
        Color background, float fontSize, Color? foreground = null)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        item.transform.SetParent(parent, false);
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
        rect.pivot = new Vector2(.5f, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = item.GetComponent<Image>();
        image.sprite = WhiteSprite();
        image.color = background;
        Button button = item.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = background;
        colors.highlightedColor = Color.Lerp(background, Color.white, .12f);
        colors.pressedColor = Color.Lerp(background, Cyan, .2f);
        colors.selectedColor = background;
        colors.disabledColor = new Color(background.r, background.g, background.b, .45f);
        button.colors = colors;
        TextMeshProUGUI text = CreateText("Label", rect, label, fontSize, foreground ?? White, TextAlignmentOptions.Center, Vector2.zero, size - new Vector2(22, 16), FontStyles.Bold);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(.5f, .5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(-18, -8);
        return button;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Sprite WhiteSprite()
    {
        if (whiteSprite == null) whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f));
        return whiteSprite;
    }

    private static Color Hex(string html)
    {
        Color color;
        ColorUtility.TryParseHtmlString("#" + html, out color);
        return color;
    }

    private static string Bit(bool value) { return value ? "1" : "0"; }

    private static AudioClip CreateFeedbackTone()
    {
        const int sampleRate = 22050;
        const float duration = .12f;
        const float frequency = 880f;
        int count = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[count];
        for (int i = 0; i < count; i++)
        {
            float envelope = 1f - (float)i / count;
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * envelope * .18f;
        }
        AudioClip clip = AudioClip.Create("LogicLab feedback cue", count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void OnDestroy()
    {
        if (codeRoutine != null) StopCoroutine(codeRoutine);
        if (feedbackClip != null) Destroy(feedbackClip);
#if UNITY_ANDROID && !UNITY_EDITOR
        if (nativeTts != null)
        {
            nativeTts.Call("stop");
            nativeTts.Call("shutdown");
            nativeTts.Dispose();
            nativeTts = null;
        }
#endif
    }
}
