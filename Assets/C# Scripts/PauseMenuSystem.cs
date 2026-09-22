using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

// Add ONE instance to an always-active scene object, outside the generated UI.
public class PauseMenuSystem : MonoBehaviour
{
    [Header("Artwork - drag your sprites here")]
    [SerializeField] private Sprite pauseBackground;
    [SerializeField] private Sprite buttonSprite;
    [SerializeField] private Sprite sliderTrackSprite;
    [SerializeField] private Sprite sliderHandleSprite;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private bool backgroundContainsTitle = true;
    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "";
    [Header("Audio mixer - recommended")]
    [Tooltip("Route effects/UI to an Audio group and music to a Music group. Expose their Volume parameters using the names below.")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string audioVolumeParameter = "AudioVolume";
    [SerializeField] private string musicVolumeParameter = "MusicVolume";
    [Header("Without a mixer - assign sources instead")]
    [Tooltip("Assign effects sources here. Do not include music sources. Sources created later should use the mixer instead.")]
    [SerializeField] private AudioSource[] audioSources;
    [SerializeField] private AudioSource[] musicSources;
    [Header("Music - drop an AudioClip here")]
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private bool playMusicOnStart = true;
    [SerializeField] private bool loopMusic = true;
    [SerializeField, Range(0f, 1f)] private float musicBaseVolume = 0.7f;
    [Tooltip("Optional: route to the same Music mixer group controlled by MusicVolume. Leave empty for direct volume control.")]
    [SerializeField] private AudioMixerGroup musicOutputGroup;
    [Header("Optional menu sounds")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip clickSound;
    [Header("Behaviour")]
    [SerializeField, Min(0.01f)] private float animationDuration = 0.18f;
    [SerializeField, Range(0f, 1f)] private float sceneChangeDelay = 0.2f;
    [Tooltip("Optional camera/player scripts that should stop receiving input while paused. Do not assign this script or the EventSystem.")]
    [SerializeField] private Behaviour[] disableWhilePaused;
    public bool IsPaused { get; private set; }
    private const string AudioKey = "Reserve.AudioVolume";
    private const string MusicKey = "Reserve.MusicVolume";
    private float audioVolume, musicVolume, savedTimeScale;
    private float[] audioBase, musicBase;
    private bool[] enabledBefore;
    private GameObject root;
    private CanvasGroup group;
    private RectTransform panel;
    private Button resumeButton;
    private AudioSource menuAudio;
    private AudioSource ownedMusic;
    private Coroutine pauseAnimationRoutine;
    private bool busy;
    private CursorLockMode savedCursorLock;
    private bool savedCursorVisible;
    private GameObject priorSelection;

    private void Start()
    {
        // SFX no longer has a slider; discard any previous muted slider setting.
        audioVolume = 1f;
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 1f));
        audioBase = CaptureVolumes(audioSources);
        musicBase = CaptureVolumes(musicSources);
        menuAudio = gameObject.AddComponent<AudioSource>();
        menuAudio.playOnAwake = false; menuAudio.spatialBlend = 0f;
        menuAudio.ignoreListenerPause = true;
        if (musicClip != null)
        {
            ownedMusic = gameObject.AddComponent<AudioSource>();
            ownedMusic.playOnAwake = false;
            ownedMusic.spatialBlend = 0f;
            ownedMusic.clip = musicClip;
            ownedMusic.loop = loopMusic;
            ownedMusic.outputAudioMixerGroup = musicOutputGroup;
        }
        BuildUI();
        ApplyAudio(); ApplyMusic();
        if (ownedMusic != null && playMusicOnStart) ownedMusic.Play();
    }

    private void Update()
    {
        bool escape = false;
#if ENABLE_INPUT_SYSTEM
        escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        escape = Input.GetKeyDown(KeyCode.Escape);
#endif
        if (escape && !busy && !SceneTransition.IsTransitioning) { if (IsPaused) Resume(); else Pause(); }
    }

    public void Pause()
    {
        if (IsPaused || busy || root == null || Time.timeScale <= 0f) return;
        EndDaySystem day = FindFirstObjectByType<EndDaySystem>();
        if (day != null && day.IsTransitionRunning()) return;
        GameEndingSystem ending = FindFirstObjectByType<GameEndingSystem>();
        if (ending != null && ending.CurrentOutcome != GameEndingSystem.Outcome.Playing) return;
        IsPaused = true; savedTimeScale = Time.timeScale; Time.timeScale = 0f;
        savedCursorLock = Cursor.lockState; savedCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        enabledBefore = new bool[disableWhilePaused == null ? 0 : disableWhilePaused.Length];
        for (int i = 0; i < enabledBefore.Length; i++)
        {
            Behaviour item = disableWhilePaused[i];
            if (item == null || item == this || item is EventSystem || item is BaseInputModule || item is AudioSource) continue;
            enabledBefore[i] = item.enabled; item.enabled = false;
        }
        priorSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        root.SetActive(true); Play(openSound);
        StartFade(true);
    }

    public void Resume()
    {
        if (!IsPaused || busy) return;
        Play(closeSound != null ? closeSound : clickSound);
        SaveVolumes(); StartFade(false);
    }

    private void StartFade(bool opening)
    {
        if (pauseAnimationRoutine != null) StopCoroutine(pauseAnimationRoutine);
        pauseAnimationRoutine = StartCoroutine(Fade(opening));
    }

    private IEnumerator Fade(bool opening)
    {
        busy = true; group.interactable = false;
        float from = group.alpha;
        float duration = Mathf.Max(0.01f, animationDuration);
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float value = Mathf.SmoothStep(from, opening ? 1f : 0f, t / duration);
            group.alpha = value; panel.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, value);
            yield return null;
        }
        group.alpha = opening ? 1f : 0f; panel.localScale = Vector3.one;
        if (opening)
        {
            group.interactable = true;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
        }
        else { root.SetActive(false); RestoreGameplay(); }
        busy = false; pauseAnimationRoutine = null;
    }

    private void RestoreGameplay()
    {
        if (!IsPaused) return;
        IsPaused = false; Time.timeScale = savedTimeScale;
        Cursor.lockState = savedCursorLock; Cursor.visible = savedCursorVisible;
        for (int i = 0; enabledBefore != null && i < enabledBefore.Length; i++)
            if (enabledBefore[i] && disableWhilePaused[i] != null) disableWhilePaused[i].enabled = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(priorSelection);
    }

    public void Restart()
    {
        int index = SceneManager.GetActiveScene().buildIndex;
        if (IsPaused && !busy && index >= 0) StartCoroutine(LoadScene(index, null));
    }
    public void MainMenu()
    {
        if (IsPaused && !busy && !string.IsNullOrWhiteSpace(mainMenuSceneName) &&
            Application.CanStreamedLevelBeLoaded(mainMenuSceneName)) StartCoroutine(LoadScene(-1, mainMenuSceneName));
    }
    private IEnumerator LoadScene(int index, string sceneName)
    {
        busy = true; group.interactable = false; Play(clickSound); SaveVolumes();
        yield return new WaitForSecondsRealtime(sceneChangeDelay);
        if (index >= 0) SceneTransition.Load(index); else SceneTransition.Load(sceneName);
    }

    private float[] CaptureVolumes(AudioSource[] sources)
    {
        float[] values = new float[sources == null ? 0 : sources.Length];
        for (int i = 0; i < values.Length; i++) if (sources[i] != null) values[i] = sources[i].volume;
        return values;
    }
    private void ApplySources(AudioSource[] sources, float[] baseline, float value)
    {
        for (int i = 0; sources != null && baseline != null && i < sources.Length && i < baseline.Length; i++)
            if (sources[i] != null) sources[i].volume = baseline[i] * value;
    }
    private float Decibels(float value) { return value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f; }
    private void ApplyAudio()
    {
        if (audioMixer != null)
        {
            if (!audioMixer.SetFloat(audioVolumeParameter, Decibels(audioVolume)))
                Debug.LogWarning("Pause menu: expose mixer parameter '" + audioVolumeParameter + "'.", this);
        }
        else ApplySources(audioSources, audioBase, audioVolume);
        if (menuAudio != null) menuAudio.volume = audioVolume;
    }
    private void ApplyMusic()
    {
        if (audioMixer != null)
        {
            if (!audioMixer.SetFloat(musicVolumeParameter, Decibels(musicVolume)))
                Debug.LogWarning("Pause menu: expose mixer parameter '" + musicVolumeParameter + "'.", this);
        }
        else ApplySources(musicSources, musicBase, musicVolume);
        if (ownedMusic != null)
        {
            bool controlledByMixer = audioMixer != null && musicOutputGroup != null &&
                musicOutputGroup.audioMixer == audioMixer;
            ownedMusic.volume = musicBaseVolume * (controlledByMixer ? 1f : musicVolume);
        }
    }
    private void SaveVolumes()
    {
        PlayerPrefs.SetFloat(AudioKey, audioVolume); PlayerPrefs.SetFloat(MusicKey, musicVolume); PlayerPrefs.Save();
    }
    private void Play(AudioClip clip) { if (clip != null && menuAudio != null) menuAudio.PlayOneShot(clip); }

    private RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero; return rect;
    }
    private Image Picture(RectTransform rect, Sprite sprite, Color fallback)
    {
        Image image = rect.gameObject.AddComponent<Image>(); image.sprite = sprite;
        image.color = sprite != null ? Color.white : fallback; return image;
    }
    private void Label(Transform parent, string value, Vector2 min, Vector2 max, Color color)
    {
        TextMeshProUGUI text = Rect(value, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value; if (font != null) text.font = font;
        text.enableAutoSizing = true; text.fontSizeMin = 12; text.fontSizeMax = 30;
        text.alignment = TextAlignmentOptions.Center; text.color = color; text.raycastTarget = false;
    }
    private Button MakeButton(string title, float bottom, UnityEngine.Events.UnityAction action)
    {
        RectTransform slot = Rect(title + " Slot", panel, new Vector2(0.32f, bottom), new Vector2(0.68f, bottom + 0.105f));
        RectTransform rect = Rect(title, slot, Vector2.zero, Vector2.one);
        FitSprite(rect, buttonSprite, 5.2f);
        Image image = Picture(rect, buttonSprite, new Color(0.5f, 0.28f, 0.12f));
        Button button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        button.onClick.AddListener(action);
        Label(rect, title, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.88f), new Color(1f, 0.94f, 0.78f));
        return button;
    }
    private void MakeSlider(string title, float bottom, float value, UnityEngine.Events.UnityAction<float> changed)
    {
        Label(panel, title, new Vector2(0.17f, bottom), new Vector2(0.36f, bottom + 0.07f), new Color(0.23f, 0.13f, 0.06f));
        RectTransform rect = Rect(title + " Slider", panel, new Vector2(0.39f, bottom), new Vector2(0.81f, bottom + 0.07f));
        RectTransform track = Rect("Track", rect, new Vector2(0f, 0.18f), new Vector2(1f, 0.82f));
        Image trackImage = Picture(track, sliderTrackSprite, new Color(0.3f, 0.18f, 0.08f));
        trackImage.preserveAspect = true;
        RectTransform area = Rect("Handle Area", rect, Vector2.zero, Vector2.one);
        area.offsetMin = new Vector2(16f, 0f); area.offsetMax = new Vector2(-16f, 0f);
        RectTransform handle = Rect("Handle", area, new Vector2(0f, 0f), new Vector2(0f, 1f));
        float handleRatio = sliderHandleSprite != null ? sliderHandleSprite.rect.width / sliderHandleSprite.rect.height : 1f;
        handle.sizeDelta = new Vector2(36f * handleRatio, 0f);
        RectTransform visual = Rect("Handle Artwork", handle, Vector2.zero, Vector2.one);
        FitSprite(visual, sliderHandleSprite, 1f);
        Image knob = Picture(visual, sliderHandleSprite, new Color(0.9f, 0.6f, 0.2f));
        Slider slider = rect.gameObject.AddComponent<Slider>(); slider.handleRect = handle;
        slider.targetGraphic = knob; slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f; slider.maxValue = 1f; slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(changed);
    }
    private void FitSprite(RectTransform rect, Sprite sprite, float fallbackRatio)
    {
        AspectRatioFitter fit = rect.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = sprite != null ? sprite.rect.width / sprite.rect.height : fallbackRatio;
    }
    private void BuildUI()
    {
        if (EventSystem.current == null)
        {
            GameObject events = new GameObject("Pause EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            events.AddComponent<InputSystemUIInputModule>();
#else
            events.AddComponent<StandaloneInputModule>();
#endif
            SceneManager.MoveGameObjectToScene(events, gameObject.scene);
        }
        root = new GameObject("Pause Menu UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        Canvas canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32760;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
        group = root.GetComponent<CanvasGroup>(); group.alpha = 0f;
        Picture(Rect("Dimmer", root.transform, Vector2.zero, Vector2.one), null, new Color(0, 0, 0, 0.7f));
        RectTransform bounds = Rect("Bounds", root.transform, new Vector2(0.13f, 0.10f), new Vector2(0.87f, 0.90f));
        panel = Rect("Pause Panel", bounds, Vector2.zero, Vector2.one);
        Picture(panel, pauseBackground, new Color(0.87f, 0.78f, 0.6f));
        AspectRatioFitter fitter = panel.gameObject.AddComponent<AspectRatioFitter>(); fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = pauseBackground != null ? pauseBackground.rect.width / pauseBackground.rect.height : 1.6f;
        if (!backgroundContainsTitle || pauseBackground == null)
            Label(panel, "PAUSED", new Vector2(0.25f, 0.79f), new Vector2(0.75f, 0.89f), new Color(0.23f, 0.13f, 0.06f));
        MakeSlider("MUSIC", 0.61f, musicVolume, value => { musicVolume = value; ApplyMusic(); });
        resumeButton = MakeButton("RESUME", 0.425f, Resume);
        Button restart = MakeButton("RESTART", 0.31f, Restart);
        restart.interactable = SceneManager.GetActiveScene().buildIndex >= 0;
        Button menu = MakeButton("MAIN MENU", 0.195f, MainMenu);
        menu.interactable = !string.IsNullOrWhiteSpace(mainMenuSceneName) && Application.CanStreamedLevelBeLoaded(mainMenuSceneName);
        root.SetActive(false);
    }
    private void OnDisable()
    {
        if (pauseAnimationRoutine != null) StopCoroutine(pauseAnimationRoutine);
        StopAllCoroutines(); pauseAnimationRoutine = null; busy = false;
        if (root != null) { root.SetActive(false); group.alpha = 0f; }
        if (IsPaused) { SaveVolumes(); RestoreGameplay(); }
    }
    private void OnDestroy()
    {
        RestoreGameplay(); if (root != null) Destroy(root);
        if (menuAudio != null) Destroy(menuAudio);
        if (ownedMusic != null) Destroy(ownedMusic);
    }
}


