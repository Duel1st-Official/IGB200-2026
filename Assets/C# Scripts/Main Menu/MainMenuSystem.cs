using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// Add once to an active object in your Main Menu scene.
public class MainMenuSystem : MonoBehaviour
{
    [Header("Scenes - exact names from your build scene list")]
    [SerializeField] private string gameSceneName = "";
    [SerializeField] private string tutorialSceneName = "";
    [Header("Text")]
    [SerializeField] private TMP_FontAsset font;
    [System.Serializable]
    public class DeveloperCredit
    {
        public string name;
        [TextArea(2, 5)] public string contribution;
        public Sprite characterSprite;
        public DeveloperCredit(string name, string contribution)
        { this.name = name; this.contribution = contribution; }
    }
    [Header("Credits - assign each developer's character sprite")]
    [SerializeField]
    private DeveloperCredit[] developers = new DeveloperCredit[]
    {
        new DeveloperCredit("Patrick A", "Artist, programmer, game designer, Audio designer"),
        new DeveloperCredit("Paul N", "Programmer, game director, Project manager"),
        new DeveloperCredit("Munkush G", "Artist, concept artist"),
        new DeveloperCredit("Lucas M", "Game designer, level designer"),
        new DeveloperCredit("Wenqi W", "Artist, concept artist")
    };
    [TextArea(6, 15)]
    [SerializeField]
    private string projectBrief =
        "Teach players about our threatened ghost bats\n\n" +
        "Show the environmental threats they face\n\n" +
        "Make learning fun and accessible for students\n\n" +
        "Highlight how Indigenous rangers protect wildlife";
    private GameObject brief;
    private Button briefBack;
    private TextMeshProUGUI developerDetails;
    [Header("Optional artwork")]
    [Tooltip("Full-screen artwork. Leave empty for a plain dark background.")]
    [SerializeField] private Sprite backgroundSprite;
    [Header("Credits background transition")]
    [SerializeField] private Sprite creditsBackgroundSprite;
    [Tooltip("Assign the MenuBackgroundMotionBlur shader asset here so it is included in builds.")]
    [SerializeField] private Shader backgroundMotionBlurShader;
    [SerializeField, Range(0.2f, 2f)] private float creditsTransitionDuration = 0.75f;
    [SerializeField, Range(0f, 0.03f)] private float motionBlurStrength = 0.012f;
    [SerializeField, Range(0f, 0.08f)] private float backgroundSlide = 0.025f;
    private Image creditsBackground;
    private Material skyMotionMaterial, caveMotionMaterial;
    private Coroutine backgroundTransitionRoutine;
    private float backgroundBlend;
    [SerializeField] private Sprite logoSprite;
    [SerializeField] private Sprite buttonSprite;
    [Header("Optional audio")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField, Range(0, 1)] private float hoverVolume = 0.5f;
    [SerializeField, Range(0, 1)] private float musicVolume = 0.7f;
    [SerializeField, Range(0, 1)] private float clickVolume = 0.8f;
    [SerializeField, Range(0, 1)] private float clickDelay = 0.15f;
    private GameObject ui, ownedEvents, home, question, credits;
    private Button playButton, yesButton, creditsBack;
    private TextMeshProUGUI status;
    private AudioSource music, sounds;
    private bool loading;
    private CanvasGroup canvasGroup;
    [Header("Menu animations")]
    [SerializeField] private bool animateMenu = true;
    [SerializeField, Range(0.1f, 0.8f)] private float pagePopDuration = 0.3f;
    private Coroutine pageAnimation;
    private GameObject animatedPage;
    private static readonly Color Ink = new Color(1f, 0.97f, 0.86f);
    private RectTransform backgroundRect, logoRect;
    private CanvasGroup logoGroup;
    private CanvasGroup[] homeButtonGroups;
    private bool introRunning;
    [Header("Logo and button entrance")]
    [SerializeField, Range(0.1f, 2f)] private float logoRevealDuration = 0.65f;
    [SerializeField, Range(0f, 1f)] private float logoHoldDuration = 0.2f;
    [SerializeField, Range(0.1f, 1f)] private float buttonRevealDuration = 0.35f;
    [SerializeField, Range(0f, 0.4f)] private float buttonStagger = 0.12f;
    [SerializeField, Range(1f, 1.1f)] private float backgroundZoom = 1.04f;

    [Header("Opening transition")]
    [SerializeField, Range(0.1f, 3f)] private float openingFadeDuration = 0.8f;

    private IEnumerator Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        sounds = gameObject.AddComponent<AudioSource>();
        sounds.playOnAwake = false; sounds.spatialBlend = 0f;
        if (musicClip != null)
        {
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false; music.spatialBlend = 0f;
            music.clip = musicClip; music.loop = true;
            music.volume = musicVolume * Mathf.Clamp01(PlayerPrefs.GetFloat("Reserve.MusicVolume", 1f));
            music.Play();
        }
        BuildUI();
        home.SetActive(false); question.SetActive(false);
        credits.SetActive(false); brief.SetActive(false);
        canvasGroup.interactable = false;
        SceneTransition.FadeInOnStart(openingFadeDuration);
        while (SceneTransition.IsTransitioning) yield return null;
        yield return Intro();
    }
    private IEnumerator Intro()
    {
        introRunning = true;
        home.SetActive(true);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        float logoDuration = Mathf.Max(0.1f, logoRevealDuration);
        float buttonDuration = Mathf.Max(0.1f, buttonRevealDuration);
        float buttonsStart = logoDuration + logoHoldDuration;
        float total = buttonsStart + 2f * buttonStagger + buttonDuration;
        if (!animateMenu) total = 0f;
        for (float elapsed = 0f; elapsed < total; elapsed += Time.unscaledDeltaTime)
        {
            float logoT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / logoDuration));
            logoGroup.alpha = logoT;
            logoRect.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, logoT);
            backgroundRect.localScale = Vector3.one * Mathf.Lerp(1f, Mathf.Max(backgroundZoom, 1f + backgroundSlide * 2f + 0.01f),
                Mathf.SmoothStep(0f, 1f, elapsed / total));
            for (int i = 0; i < homeButtonGroups.Length; i++)
            {
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - buttonsStart - i * buttonStagger) / buttonDuration));
                homeButtonGroups[i].alpha = t;
                homeButtonGroups[i].transform.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, t);
            }
            yield return null;
        }
        logoGroup.alpha = 1f; logoRect.localScale = Vector3.one;
        backgroundRect.localScale = Vector3.one * Mathf.Max(backgroundZoom, 1f + backgroundSlide * 2f + 0.01f);
        foreach (CanvasGroup group in homeButtonGroups) { group.alpha = 1f; group.transform.localScale = Vector3.one; }
        introRunning = false; canvasGroup.interactable = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(playButton.gameObject);
    }

    public void PlayGame()
    {
        if (loading || introRunning || SceneTransition.IsTransitioning) return;
        Click(); Show(question, yesButton);
    }
    public void OpenCredits()
    {
        if (loading || introRunning || SceneTransition.IsTransitioning) return;
        Click(); Show(credits, creditsBack);
    }
    public void Back()
    {
        if (loading || introRunning || SceneTransition.IsTransitioning) return;
        Click(); Show(home, playButton);
    }
    public void OpenProjectBrief()
    {
        if (loading || introRunning || SceneTransition.IsTransitioning) return;
        Click(); Show(brief, briefBack);
    }
    public void StartTutorial() { BeginLoad(tutorialSceneName, "Tutorial Scene Name"); }
    public void StartGame() { BeginLoad(gameSceneName, "Game Scene Name"); }
    private void BeginLoad(string sceneName, string field)
    {
        if (loading || introRunning || SceneTransition.IsTransitioning) return;
        Click();
        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            status.text = "Scene unavailable. Set " + field + " and include that scene in the build scene list.";
            Debug.LogWarning("Main menu: check " + field + " and your build scene list.", this);
            return;
        }
        loading = true; canvasGroup.interactable = false;
        status.text = "Loading...";
        StartCoroutine(Load(sceneName));
    }
    private IEnumerator Load(string sceneName)
    {
        yield return new WaitForSecondsRealtime(clickDelay);
        Time.timeScale = 1f;
        SceneTransition.Load(sceneName);

    }
    public void ExitGame()
    {
        if (loading || introRunning || SceneTransition.IsTransitioning) return;
        Click(); StartCoroutine(Quit());
    }
    private IEnumerator Quit()
    {
        loading = true; canvasGroup.interactable = false;
        yield return new WaitForSecondsRealtime(clickDelay);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    private void Click() { if (clickSound != null) sounds.PlayOneShot(clickSound, clickVolume); }
    private void Hover(Button button)
    {
        if (loading || SceneTransition.IsTransitioning || !button.isActiveAndEnabled ||
            !button.IsInteractable() || hoverSound == null || sounds == null) return;
        sounds.PlayOneShot(hoverSound, hoverVolume);
    }
    private void Show(GameObject page, Button selected)
    {
        TransitionBackground(page == credits || page == brief);
        if (pageAnimation != null) StopCoroutine(pageAnimation);
        if (animatedPage != null)
        {
            animatedPage.transform.localScale = Vector3.one;
            CanvasGroup previous = animatedPage.GetComponent<CanvasGroup>();
            if (previous != null) { previous.alpha = 1f; previous.interactable = true; }
        }
        home.SetActive(page == home); question.SetActive(page == question); credits.SetActive(page == credits);
        brief.SetActive(page == brief);
        if (page == credits) developerDetails.text = "Hover over or select a developer to see their contribution.";
        status.text = "";
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        animatedPage = page;
        if (animateMenu) pageAnimation = StartCoroutine(PopPage(page, selected));
        else if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(selected.gameObject);
    }
    private IEnumerator PopPage(GameObject page, Button selected)
    {
        CanvasGroup group = page.GetComponent<CanvasGroup>();
        if (group == null) group = page.AddComponent<CanvasGroup>();
        group.interactable = false;
        float duration = Mathf.Max(0.1f, pagePopDuration);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            // A small overshoot gives the page a soft bounce without moving its layout.
            float eased = 1f + 2.2f * Mathf.Pow(t - 1f, 3f) + 1.2f * Mathf.Pow(t - 1f, 2f);
            page.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.90f, 1f, eased);
            group.alpha = Mathf.Clamp01(t * 2f);
            yield return null;
        }
        page.transform.localScale = Vector3.one; group.alpha = 1f; group.interactable = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(selected.gameObject);
        pageAnimation = null;
    }
    private RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
    }
    private TextMeshProUGUI Text(Transform parent, string value, Vector2 min, Vector2 max, float size, Color color)
    {
        TextMeshProUGUI text = Rect("Text", parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = value; text.color = color; text.alignment = TextAlignmentOptions.Center;
        text.outlineColor = new Color(0.08f, 0.17f, 0.23f); text.outlineWidth = 0.18f;
        text.enableAutoSizing = true; text.fontSizeMin = 12f; text.fontSizeMax = size;
        text.raycastTarget = false; return text;
    }
    private Button Button(Transform parent, string title, float bottom, UnityAction action)
    {
        RectTransform slot = Rect(title + " Slot", parent, new Vector2(0.26f, bottom), new Vector2(0.74f, bottom + 0.12f));
        RectTransform rect = Rect(title, slot, Vector2.zero, Vector2.one);
        AspectRatioFitter fit = rect.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = buttonSprite != null ? buttonSprite.rect.width / buttonSprite.rect.height : 5f;
        Image image = rect.gameObject.AddComponent<Image>(); image.sprite = buttonSprite;
        image.color = buttonSprite != null ? Color.white : new Color(0.5f, 0.28f, 0.12f);
        Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        if (animateMenu) rect.gameObject.AddComponent<MenuPopAnimation>();
        button.onClick.AddListener(action);
        EventTrigger hoverTrigger = rect.gameObject.AddComponent<EventTrigger>();
        AddCreditEvent(hoverTrigger, EventTriggerType.PointerEnter, () => Hover(button));
        Text(rect, title, new Vector2(0.06f, 0.1f), new Vector2(0.94f, 0.9f), 32f, new Color(1f, 0.95f, 0.8f));
        return button;
    }
    private GameObject Page(string name, Transform parent)
    { return Rect(name, parent, Vector2.zero, Vector2.one).gameObject; }
    private void BuildUI()
    {
        if (EventSystem.current == null)
        {
            ownedEvents = new GameObject("Menu EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            ownedEvents.AddComponent<InputSystemUIInputModule>();
#else
            ownedEvents.AddComponent<StandaloneInputModule>();
#endif
            SceneManager.MoveGameObjectToScene(ownedEvents, gameObject.scene);
        }
        ui = new GameObject("Main Menu UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        SceneManager.MoveGameObjectToScene(ui, gameObject.scene);
        canvasGroup = ui.GetComponent<CanvasGroup>();
        Canvas canvas = ui.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
        CanvasScaler scaler = ui.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
        Image backdrop = Rect("Background", ui.transform, Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
        backgroundRect = backdrop.rectTransform;
        backdrop.raycastTarget = false;
        creditsBackground = Rect("Credits Background", ui.transform, Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
        creditsBackground.sprite = creditsBackgroundSprite;
        creditsBackground.color = new Color(1f, 1f, 1f, 0f);
        creditsBackground.raycastTarget = false;
        creditsBackground.gameObject.SetActive(false);
        if (backgroundMotionBlurShader != null && backgroundMotionBlurShader.isSupported)
        {
            skyMotionMaterial = new Material(backgroundMotionBlurShader);
            caveMotionMaterial = new Material(backgroundMotionBlurShader);
            backdrop.material = skyMotionMaterial;
            creditsBackground.material = caveMotionMaterial;
        }
        backdrop.sprite = backgroundSprite; backdrop.color = backgroundSprite != null ? Color.white : new Color(0.08f, 0.13f, 0.09f);
        RectTransform bounds = Rect("Bounds", ui.transform, new Vector2(0.10f, 0.08f), new Vector2(0.90f, 0.92f));
        RectTransform panel = Rect("Menu Layout", bounds, Vector2.zero, Vector2.one);
        AspectRatioFitter fit = panel.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 1.6f;
        home = Page("Home", panel); question = Page("First Time", panel); credits = Page("Credits", panel);
        brief = Page("Project Brief", panel);
        logoRect = Rect("Game Logo", home.transform, new Vector2(0.16f, 0.58f), new Vector2(0.84f, 0.98f));
        Image logo = logoRect.gameObject.AddComponent<Image>(); logo.sprite = logoSprite;
        logo.preserveAspect = true; logo.raycastTarget = false; logo.enabled = logoSprite != null;
        logoGroup = logoRect.gameObject.AddComponent<CanvasGroup>(); logoGroup.alpha = 0f;
        playButton = Button(home.transform, "PLAY GAME", 0.41f, PlayGame);
        Button creditsButton = Button(home.transform, "CREDITS", 0.26f, OpenCredits);
        Button exitButton = Button(home.transform, "EXIT GAME", 0.11f, ExitGame);
        Button[] homeButtons = { playButton, creditsButton, exitButton };
        homeButtonGroups = new CanvasGroup[homeButtons.Length];
        for (int i = 0; i < homeButtons.Length; i++)
        {
            // Animate the slot so hover scaling on the child does not fight the entrance.
            homeButtonGroups[i] = homeButtons[i].transform.parent.gameObject.AddComponent<CanvasGroup>();
            homeButtonGroups[i].alpha = 0f;
        }
        Text(question.transform, "Is this your first time playing?", new Vector2(0.1f, 0.69f), new Vector2(0.9f, 0.85f), 40f, Ink);
        yesButton = Button(question.transform, "YES - TUTORIAL", 0.49f, StartTutorial);
        Button(question.transform, "NO - PLAY GAME", 0.33f, StartGame);
        Button(question.transform, "BACK", 0.17f, Back);
        Text(credits.transform, "CREDITS", new Vector2(0.1f, 0.76f), new Vector2(0.9f, 0.88f), 44f, Ink);
        BuildDevelopers(credits.transform);
        Button(credits.transform, "PROJECT BRIEF", 0.27f, OpenProjectBrief);
        creditsBack = Button(credits.transform, "MAIN MENU", 0.13f, Back);
        Text(brief.transform, "INDUSTRY PARTNER PROJECT BRIEF", new Vector2(0.1f, 0.76f), new Vector2(0.9f, 0.89f), 36f, Ink);
        BuildCreditsScroll(brief.transform);
        briefBack = Button(brief.transform, "BACK TO CREDITS", 0.15f, OpenCredits);
        status = Text(panel, "", new Vector2(0.10f, 0.035f), new Vector2(0.90f, 0.12f), 20f, Ink);
    }
    private void BuildCreditsScroll(Transform parent)
    {
        RectTransform viewport = Rect("Credits Scroll", parent, new Vector2(0.13f, 0.31f), new Vector2(0.87f, 0.72f));
        Image hitArea = viewport.gameObject.AddComponent<Image>(); hitArea.color = new Color(0, 0, 0, 0);
        viewport.gameObject.AddComponent<RectMask2D>();
        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
        RectTransform content = Rect("Credits Content", viewport, new Vector2(0, 1), Vector2.one);
        content.pivot = new Vector2(0.5f, 1f);
        TextMeshProUGUI body = content.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) body.font = font;
        body.text = projectBrief; body.fontSize = 28f; body.color = Ink;
        body.outlineColor = new Color(0.08f, 0.17f, 0.23f); body.outlineWidth = 0.18f;
        body.alignment = TextAlignmentOptions.Top; body.raycastTarget = false;
        ContentSizeFitter size = content.gameObject.AddComponent<ContentSizeFitter>(); size.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content;
    }
    private void BuildDevelopers(Transform parent)
    {
        developerDetails = Text(parent, "", new Vector2(0.12f, 0.405f), new Vector2(0.88f, 0.515f), 26f, Ink);
        int count = developers == null ? 0 : developers.Length;
        for (int i = 0; i < count; i++)
        {
            DeveloperCredit dev = developers[i];
            if (dev == null) continue;
            float left = 0.10f + 0.80f * i / count;
            float right = 0.10f + 0.80f * (i + 1) / count;
            RectTransform tile = Rect(dev.name, parent, new Vector2(left + 0.006f, 0.53f), new Vector2(right - 0.006f, 0.735f));
            Image hit = tile.gameObject.AddComponent<Image>(); hit.color = new Color(0.55f, 0.35f, 0.15f, 0.12f);
            Button select = tile.gameObject.AddComponent<Button>(); select.targetGraphic = hit;
            if (animateMenu) tile.gameObject.AddComponent<MenuPopAnimation>();
            if (dev.characterSprite != null)
            {
                Image portrait = Rect("Character", tile, new Vector2(0.12f, 0.25f), new Vector2(0.88f, 0.95f)).gameObject.AddComponent<Image>();
                portrait.sprite = dev.characterSprite; portrait.preserveAspect = true; portrait.raycastTarget = false;
            }
            else Text(tile, "?", new Vector2(0.12f, 0.25f), new Vector2(0.88f, 0.95f), 40f, Ink);
            Text(tile, dev.name, new Vector2(0.02f, 0.01f), new Vector2(0.98f, 0.24f), 22f, Ink);
            UnityAction reveal = () => developerDetails.text = dev.name + "\n" + dev.contribution;
            select.onClick.AddListener(() => { Click(); reveal(); });
            EventTrigger trigger = tile.gameObject.AddComponent<EventTrigger>();
            AddCreditEvent(trigger, EventTriggerType.PointerEnter, reveal);
            AddCreditEvent(trigger, EventTriggerType.PointerEnter, () => Hover(select));
            AddCreditEvent(trigger, EventTriggerType.Select, reveal);
        }
    }
    private void AddCreditEvent(EventTrigger trigger, EventTriggerType type, UnityAction action)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(data => action()); trigger.triggers.Add(entry);
    }
    private void TransitionBackground(bool showCredits)
    {
        if (creditsBackground == null || creditsBackgroundSprite == null) return;
        if (backgroundTransitionRoutine != null) StopCoroutine(backgroundTransitionRoutine);
        backgroundTransitionRoutine = StartCoroutine(BlendBackground(showCredits ? 1f : 0f));
    }
    private IEnumerator BlendBackground(float target)
    {
        float start = backgroundBlend;
        float duration = animateMenu ? Mathf.Max(0.2f, creditsTransitionDuration) : 0f;
        creditsBackground.gameObject.SetActive(true);
        // Overscan keeps the moving backgrounds covering every edge of the screen.
        float zoom = Mathf.Max(backgroundZoom, 1f + backgroundSlide * 2f + 0.01f);
        for (float elapsed = 0f; elapsed < duration && Mathf.Abs(start - target) > 0.001f; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            backgroundBlend = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
            ApplyBackgroundBlend(zoom, Mathf.Sin(t * Mathf.PI) * motionBlurStrength);
            yield return null;
        }
        backgroundBlend = target; ApplyBackgroundBlend(zoom, 0f);
        if (target == 0f) creditsBackground.gameObject.SetActive(false);
        backgroundTransitionRoutine = null;
    }
    private void ApplyBackgroundBlend(float zoom, float blur)
    {
        backgroundRect.localScale = Vector3.one * zoom;
        creditsBackground.rectTransform.localScale = Vector3.one * zoom;
        float distance = ((RectTransform)ui.transform).rect.height * backgroundSlide;
        backgroundRect.anchoredPosition = new Vector2(0f, distance * backgroundBlend);
        creditsBackground.rectTransform.anchoredPosition = new Vector2(0f, -distance * (1f - backgroundBlend));
        creditsBackground.color = new Color(1f, 1f, 1f, backgroundBlend);
        if (skyMotionMaterial != null) skyMotionMaterial.SetFloat("_BlurStrength", blur);
        if (caveMotionMaterial != null) caveMotionMaterial.SetFloat("_BlurStrength", blur);
    }
    private void OnDestroy()
    {
        if (skyMotionMaterial != null) Destroy(skyMotionMaterial);
        if (caveMotionMaterial != null) Destroy(caveMotionMaterial);
        if (ui != null) Destroy(ui);
        if (ownedEvents != null) Destroy(ownedEvents);
        if (sounds != null) Destroy(sounds);
        if (music != null) Destroy(music);
    }
}




