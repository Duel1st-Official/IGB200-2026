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
    [SerializeField] private string gameTitle = "GHOST BAT RESERVE";
    [TextArea(6, 20)][SerializeField] private string creditsText = "Add your credits in the Inspector.";
    [SerializeField] private TMP_FontAsset font;
    [Header("Optional artwork")]
    [Tooltip("Full-screen artwork. Leave empty for a plain dark background.")]
    [SerializeField] private Sprite backgroundSprite;
    [Tooltip("Use a blank panel without a PAUSED or EVENT title. Leave empty for a plain parchment panel.")]
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite buttonSprite;
    [Header("Optional audio")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private AudioClip clickSound;
    [SerializeField, Range(0, 1)] private float musicVolume = 0.7f;
    [SerializeField, Range(0, 1)] private float clickVolume = 0.8f;
    [SerializeField, Range(0, 1)] private float clickDelay = 0.15f;
    private GameObject ui, ownedEvents, home, question, credits;
    private Button playButton, yesButton, creditsBack;
    private TextMeshProUGUI status;
    private AudioSource music, sounds;
    private bool loading;
    private CanvasGroup canvasGroup;
    private static readonly Color Ink = new Color(0.23f, 0.13f, 0.06f);

    private void Start()
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
        BuildUI(); Show(home, playButton);
    }

    public void PlayGame()
    {
        if (loading) return;
        Click(); Show(question, yesButton);
    }
    public void OpenCredits()
    {
        if (loading) return;
        Click(); Show(credits, creditsBack);
    }
    public void Back()
    {
        if (loading) return;
        Click(); Show(home, playButton);
    }
    public void StartTutorial() { BeginLoad(tutorialSceneName, "Tutorial Scene Name"); }
    public void StartGame() { BeginLoad(gameSceneName, "Game Scene Name"); }
    private void BeginLoad(string sceneName, string field)
    {
        if (loading) return;
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
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation != null) yield return operation;
    }
    public void ExitGame()
    {
        if (loading) return;
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
    private void Show(GameObject page, Button selected)
    {
        home.SetActive(page == home); question.SetActive(page == question); credits.SetActive(page == credits);
        status.text = "";
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(selected.gameObject);
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
        button.onClick.AddListener(action);
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
        backdrop.sprite = backgroundSprite; backdrop.color = backgroundSprite != null ? Color.white : new Color(0.08f, 0.13f, 0.09f);
        RectTransform bounds = Rect("Bounds", ui.transform, new Vector2(0.10f, 0.08f), new Vector2(0.90f, 0.92f));
        RectTransform panel = Rect("Panel", bounds, Vector2.zero, Vector2.one);
        AspectRatioFitter fit = panel.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = panelSprite != null ? panelSprite.rect.width / panelSprite.rect.height : 1.6f;
        Image paper = panel.gameObject.AddComponent<Image>(); paper.sprite = panelSprite;
        paper.color = panelSprite != null ? Color.white : new Color(0.90f, 0.81f, 0.63f);
        home = Page("Home", panel); question = Page("First Time", panel); credits = Page("Credits", panel);
        Text(home.transform, gameTitle, new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.86f), 52f, Ink);
        playButton = Button(home.transform, "PLAY GAME", 0.51f, PlayGame);
        Button(home.transform, "CREDITS", 0.35f, OpenCredits);
        Button(home.transform, "EXIT GAME", 0.19f, ExitGame);
        Text(question.transform, "Is this your first time playing?", new Vector2(0.1f, 0.69f), new Vector2(0.9f, 0.85f), 40f, Ink);
        yesButton = Button(question.transform, "YES - TUTORIAL", 0.49f, StartTutorial);
        Button(question.transform, "NO - PLAY GAME", 0.33f, StartGame);
        Button(question.transform, "BACK", 0.17f, Back);
        Text(credits.transform, "CREDITS", new Vector2(0.1f, 0.76f), new Vector2(0.9f, 0.88f), 44f, Ink);
        BuildCreditsScroll(credits.transform);
        creditsBack = Button(credits.transform, "BACK", 0.15f, Back);
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
        body.text = creditsText; body.fontSize = 28f; body.color = Ink;
        body.alignment = TextAlignmentOptions.Top; body.raycastTarget = false;
        ContentSizeFitter size = content.gameObject.AddComponent<ContentSizeFitter>(); size.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content;
    }
    private void OnDestroy()
    {
        if (ui != null) Destroy(ui);
        if (ownedEvents != null) Destroy(ownedEvents);
        if (sounds != null) Destroy(sounds);
        if (music != null) Destroy(music);
    }
}
