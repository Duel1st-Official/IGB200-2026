using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameEndingSystem : MonoBehaviour
{
    [Header("References - automatically found when empty")]
    [SerializeField] private EndDaySystem endDaySystem;
    [SerializeField] private BatColony batColony;
    [Tooltip("ToursBuilding that owns the reserve's reputation.")]
    [SerializeField] private ToursBuilding toursBuilding;

    [Header("Ending Screen")]
    [SerializeField] private string mainMenuSceneName = "";
    [Tooltip("Fallback background, retained from the previous version.")]
    [SerializeField] private Sprite panelBackground;
    [SerializeField] private Sprite winBackground;
    [SerializeField] private Sprite loseBackground;
    [Tooltip("Hide generated title when the selected win/lose artwork already contains it.")]
    [SerializeField] private bool backgroundContainsTitle = true;
    [SerializeField] private TMP_FontAsset font;
    [Min(0.01f)][SerializeField] private float revealDuration = 0.4f;
    [SerializeField] private Behaviour[] disableOnEnding;
    [SerializeField] private UnityEvent onWon;
    [SerializeField] private UnityEvent onLost;

    [Header("Ending Audio")]
    [Tooltip("Optional dedicated source on an always-active object. Created automatically if empty.")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip loseSound;
    [Range(0f, 1f)][SerializeField] private float endingVolume = 0.8f;
    [SerializeField] private AudioClip[] buttonHoverSounds = new AudioClip[3];
    [SerializeField] private AudioClip[] buttonClickSounds = new AudioClip[3];
    [Range(0f, 1f)][SerializeField] private float hoverVolume = 0.6f;
    [Range(0f, 1f)][SerializeField] private float clickVolume = 0.8f;
    [Tooltip("Short unscaled delay lets the click sound play before changing scenes.")]
    [Range(0f, 1f)][SerializeField] private float sceneChangeDelay = 0.2f;

    public enum Outcome { Playing, Won, Lost }
    public Outcome CurrentOutcome { get; private set; }
    private GameObject screen;
    private CanvasGroup screenGroup;
    private RectTransform card;
    private Button restartButton;
    private Button menuButton;
    private float previousTimeScale = 1f;
    private bool ownsPause;
    private bool missingWarningShown;
    private bool loading;
    private GameObject ownedAudioObject;
    private bool oldIgnoreListenerPause;
    private AudioSource configuredSource;

    public void SetEndDaySystem(EndDaySystem value) { endDaySystem = value; }

    private void FindReferences()
    {
        if (endDaySystem == null) endDaySystem = FindFirstObjectByType<EndDaySystem>();
        if (batColony == null) batColony = FindFirstObjectByType<BatColony>();
        if (toursBuilding == null) toursBuilding = FindFirstObjectByType<ToursBuilding>();
    }

    private void LateUpdate()
    {
        if (CurrentOutcome != Outcome.Playing) return;
        FindReferences();
        if (endDaySystem == null || endDaySystem.GetCurrentDay() < 1 ||
            endDaySystem.IsTransitionRunning()) return;
        CheckAfterDailyProcessing();
    }

    public static Outcome DetermineOutcome(int bats, float reputation, int maximumBats)
    {
        if (bats <= 0 || reputation <= 0f) return Outcome.Lost;
        if (bats >= Mathf.Max(1, maximumBats)) return Outcome.Won;
        return Outcome.Playing;
    }

    public bool CheckAfterDailyProcessing()
    {
        if (CurrentOutcome != Outcome.Playing) return true;
        FindReferences();
        if (endDaySystem == null || batColony == null || toursBuilding == null)
        {
            if (!missingWarningShown)
            {
                Debug.LogWarning("[GameEndingSystem] Assign EndDaySystem, BatColony and ToursBuilding to enable ending checks.", this);
                missingWarningShown = true;
            }
            return false;
        }
        batColony.ProcessPendingDays();
        toursBuilding.ProcessPendingDays();
        Outcome outcome = DetermineOutcome(batColony.GetPopulation(),
            toursBuilding.GetReputation(), batColony.GetMaximumPopulation());
        if (outcome == Outcome.Playing) return false;
        CurrentOutcome = outcome;
        endDaySystem.LockForGameEnding();
        previousTimeScale = Time.timeScale;
        ownsPause = true;
        Time.timeScale = 0f;
        BuildScreen(outcome);
        if (disableOnEnding != null)
        {
            foreach (Behaviour behaviour in disableOnEnding)
            {
                if (behaviour != null && behaviour != this && behaviour != endDaySystem &&
                    behaviour != uiAudioSource && !(behaviour is EventSystem) &&
                    !(behaviour is BaseInputModule)) behaviour.enabled = false;
            }
        }
        PlaySound(outcome == Outcome.Won ? winSound : loseSound, endingVolume);
        StartCoroutine(Reveal());
        if (outcome == Outcome.Won) onWon?.Invoke(); else onLost?.Invoke();
        return true;
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null) return;
        if (uiAudioSource == null)
        {
            ownedAudioObject = new GameObject("Ending UI Audio");
            SceneManager.MoveGameObjectToScene(ownedAudioObject, gameObject.scene);
            uiAudioSource = ownedAudioObject.AddComponent<AudioSource>();
            uiAudioSource.playOnAwake = false;
            uiAudioSource.loop = false;
            uiAudioSource.spatialBlend = 0f;
        }
        if (configuredSource != uiAudioSource)
        {
            if (configuredSource != null) configuredSource.ignoreListenerPause = oldIgnoreListenerPause;
            configuredSource = uiAudioSource;
            oldIgnoreListenerPause = configuredSource.ignoreListenerPause;
            configuredSource.ignoreListenerPause = true;
        }
        if (uiAudioSource.isActiveAndEnabled)
            uiAudioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void PlayRandomSound(AudioClip[] clips, float volume)
    {
        if (clips == null) return;
        int count = 0;
        foreach (AudioClip clip in clips) if (clip != null) count++;
        if (count == 0) return;
        int selected = Random.Range(0, count);
        foreach (AudioClip clip in clips)
        {
            if (clip == null) continue;
            if (selected-- == 0) { PlaySound(clip, volume); return; }
        }
    }

    private RectTransform MakeRect(string objectName, Transform parent, Vector2 min, Vector2 max)
    {
        RectTransform rect = new GameObject(objectName, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        return rect;
    }

    private void AddText(Transform parent, string value, float size, Vector2 min, Vector2 max, Color color)
    {
        TextMeshProUGUI text = MakeRect("Text", parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.text = value; text.fontSize = size;
        text.enableAutoSizing = true; text.fontSizeMin = 16f; text.fontSizeMax = size;
        text.alignment = TextAlignmentOptions.Center; text.color = color; text.raycastTarget = false;
    }

    private Button AddButton(string label, Vector2 min, Vector2 max, UnityAction action)
    {
        RectTransform rect = MakeRect(label, card, min, max);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.35f, 0.22f, 0.12f, 1f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        EventTrigger trigger = rect.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        entry.callback.AddListener(data =>
        {
            if (!loading && button.isActiveAndEnabled && button.IsInteractable())
                PlayRandomSound(buttonHoverSounds, hoverVolume);
        });
        trigger.triggers.Add(entry);
        AddText(rect, label, 28f, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f), Color.white);
        return button;
    }

    private void BuildScreen(Outcome outcome)
    {
        screen = new GameObject("Game Ending Screen", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        SceneManager.MoveGameObjectToScene(screen, gameObject.scene);
        Canvas canvas = screen.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32766;
        CanvasScaler scaler = screen.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
        screenGroup = screen.GetComponent<CanvasGroup>();
        screenGroup.alpha = 0f; screenGroup.blocksRaycasts = true;
        MakeRect("Input Blocker", screen.transform, Vector2.zero, Vector2.one)
            .gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);
        RectTransform bounds = MakeRect("Panel Bounds", screen.transform,
            new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f));
        card = MakeRect("Ending Panel", bounds, Vector2.zero, Vector2.one);
        bool won = outcome == Outcome.Won;
        Sprite selected = won ? winBackground : loseBackground;
        bool hasPrintedTitle = selected != null && backgroundContainsTitle;
        if (selected == null) selected = panelBackground;
        Image background = card.gameObject.AddComponent<Image>();
        background.sprite = selected;
        background.color = selected != null ? Color.white : new Color(0.86f, 0.77f, 0.59f);
        if (selected != null)
        {
            AspectRatioFitter fitter = card.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = selected.rect.width / Mathf.Max(1f, selected.rect.height);
        }
        Color ink = new Color(0.18f, 0.12f, 0.08f);
        if (!hasPrintedTitle)
            AddText(card, won ? "RESERVE SAVED" : "RESERVE LOST", 52f,
                new Vector2(0.08f, 0.73f), new Vector2(0.92f, 0.91f), ink);
        string reason = won ? "The Ghost Bat colony has reached its population goal. Your reserve is thriving!"
            : batColony.GetPopulation() <= 0 ? "The reserve has lost its Ghost Bat colony."
            : "The reserve's reputation has fallen to zero.";
        AddText(card, reason, 30f, new Vector2(0.12f, 0.43f), new Vector2(0.88f, 0.70f), ink);
        AddText(card, "BATS: " + batColony.GetPopulation() + " / " + batColony.GetMaximumPopulation() +
            "    REPUTATION: " + toursBuilding.GetReputation().ToString("0"), 26f,
            new Vector2(0.10f, 0.30f), new Vector2(0.90f, 0.42f), ink);
        restartButton = AddButton("RESTART", new Vector2(0.12f, 0.13f), new Vector2(0.47f, 0.25f), Restart);
        menuButton = AddButton("MAIN MENU", new Vector2(0.53f, 0.13f), new Vector2(0.88f, 0.25f), MainMenu);
        restartButton.interactable = false; menuButton.interactable = false;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private IEnumerator Reveal()
    {
        float duration = Mathf.Max(0.01f, revealDuration);
        for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
        {
            float t = Mathf.Clamp01(time / duration);
            screenGroup.alpha = t;
            card.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, 1f - Mathf.Pow(1f - t, 3f));
            yield return null;
        }
        screenGroup.alpha = 1f; card.localScale = Vector3.one;
        restartButton.interactable = SceneManager.GetActiveScene().buildIndex >= 0;
        menuButton.interactable = !string.IsNullOrWhiteSpace(mainMenuSceneName) &&
            Application.CanStreamedLevelBeLoaded(mainMenuSceneName);
        if (restartButton.interactable && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
    }

    public void Restart()
    {
        int index = SceneManager.GetActiveScene().buildIndex;
        if (loading || CurrentOutcome == Outcome.Playing || index < 0 ||
            (restartButton != null && !restartButton.IsInteractable())) return;
        BeginSceneChange(index, null);
    }

    public void MainMenu()
    {
        if (loading || CurrentOutcome == Outcome.Playing || string.IsNullOrWhiteSpace(mainMenuSceneName) ||
            !Application.CanStreamedLevelBeLoaded(mainMenuSceneName) ||
            (menuButton != null && !menuButton.IsInteractable())) return;
        BeginSceneChange(-1, mainMenuSceneName);
    }

    private void BeginSceneChange(int index, string sceneName)
    {
        loading = true;
        if (restartButton != null) restartButton.interactable = false;
        if (menuButton != null) menuButton.interactable = false;
        PlayRandomSound(buttonClickSounds, clickVolume);
        StartCoroutine(ChangeScene(index, sceneName));
    }

    private IEnumerator ChangeScene(int index, string sceneName)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, sceneChangeDelay));
        if (index >= 0) SceneTransition.Load(index); else SceneTransition.Load(sceneName);
    }

    private void RestoreTime()
    {
        if (!ownsPause) return;
        Time.timeScale = previousTimeScale; ownsPause = false;
    }

    private void OnDestroy()
    {
        RestoreTime();
        if (configuredSource != null) configuredSource.ignoreListenerPause = oldIgnoreListenerPause;
        if (ownedAudioObject != null) Destroy(ownedAudioObject);
        if (screen != null) Destroy(screen);
    }
}

