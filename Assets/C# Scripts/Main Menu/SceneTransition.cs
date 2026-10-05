using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Automatically created on first use. An optional scene instance can configure timing.
public class SceneTransition : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 0.4f;
    [SerializeField, Min(0.01f)] private float fadeInDuration = 0.5f;
    [SerializeField, Min(0f)] private float blackHoldDuration = 0.1f;
    private static SceneTransition instance;
    private CanvasGroup overlay;
    private bool running;
    public static bool IsTransitioning => instance != null && instance.running;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        GameObject screen = new GameObject("Scene Fade", typeof(RectTransform), typeof(Canvas),
            typeof(GraphicRaycaster), typeof(CanvasGroup));
        screen.transform.SetParent(transform, false);
        Canvas canvas = screen.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        overlay = screen.GetComponent<CanvasGroup>();
        overlay.alpha = 0f; overlay.blocksRaycasts = false;
        GameObject black = new GameObject("Black", typeof(RectTransform), typeof(Image));
        black.transform.SetParent(screen.transform, false);
        RectTransform rect = black.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        black.GetComponent<Image>().color = Color.black;
        screen.SetActive(false);
    }

    public static void Load(string sceneName) { Begin(sceneName, -1); }
    public static void Load(int buildIndex) { Begin(null, buildIndex); }
    // Covers a directly opened scene before its first rendered frame.
    // An existing scene-change fade already handles arrival and must not restart.
    public static void FadeInOnStart(float duration = 0.8f)
    {
        if (instance == null) new GameObject("Scene Transition").AddComponent<SceneTransition>();
        if (instance.running) return;
        instance.running = true;
        instance.overlay.gameObject.SetActive(true);
        instance.overlay.alpha = 1f;
        instance.overlay.blocksRaycasts = true;
        instance.StartCoroutine(instance.StartupFade(duration));
    }
    private IEnumerator StartupFade(float duration)
    {
        yield return null;
        yield return Fade(0f, duration);
        overlay.blocksRaycasts = false;
        overlay.gameObject.SetActive(false);
        running = false;
    }
    private static void Begin(string sceneName, int buildIndex)
    {
        bool valid = buildIndex >= 0 ? Application.CanStreamedLevelBeLoaded(buildIndex)
            : !string.IsNullOrWhiteSpace(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName);
        if (!valid) { Debug.LogError("SceneTransition: destination is missing from the build scene list."); return; }
        if (instance == null) new GameObject("Scene Transition").AddComponent<SceneTransition>();
        if (instance.running) return;
        instance.running = true;
        instance.StartCoroutine(instance.Switch(sceneName, buildIndex));
    }

    private IEnumerator Switch(string sceneName, int buildIndex)
    {
        overlay.gameObject.SetActive(true); overlay.blocksRaycasts = true;
        EventSystem oldEvents = EventSystem.current;
        bool oldNavigation = oldEvents != null && oldEvents.sendNavigationEvents;
        if (oldEvents != null) oldEvents.sendNavigationEvents = false;
        yield return Fade(1f, fadeOutDuration);
        // Black now covers the paused scene; resume time only at this point.
        Time.timeScale = 1f;
        AsyncOperation operation = buildIndex >= 0 ? SceneManager.LoadSceneAsync(buildIndex)
            : SceneManager.LoadSceneAsync(sceneName);
        if (operation != null) yield return operation;
        // Allow the destination's Start methods and UI layout to finish behind black.
        yield return null;
        if (oldEvents != null) oldEvents.sendNavigationEvents = oldNavigation;
        EventSystem newEvents = EventSystem.current;
        bool newNavigation = newEvents != null && newEvents.sendNavigationEvents;
        if (newEvents != null) newEvents.sendNavigationEvents = false;
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, blackHoldDuration));
        yield return Fade(0f, fadeInDuration);
        if (newEvents != null) newEvents.sendNavigationEvents = newNavigation;
        overlay.blocksRaycasts = false; overlay.gameObject.SetActive(false);
        running = false;
    }
    private IEnumerator Fade(float target, float duration)
    {
        float start = overlay.alpha;
        duration = Mathf.Max(0.01f, duration);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            overlay.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            yield return null;
        }
        overlay.alpha = target;
    }
    private void OnDestroy() { if (instance == this) instance = null; }
}

