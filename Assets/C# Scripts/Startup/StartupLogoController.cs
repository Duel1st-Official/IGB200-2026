using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class StartupLogoController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoImage;

    [Header("Black UI")]
    [Tooltip("Fullscreen black Image behind the video.")]
    [SerializeField] private Image blackBackground;
    [Tooltip("Fullscreen black Image above everything. Keep this as the last Canvas child.")]
    [SerializeField] private Image fadeOverlay;

    [Header("Animation Timing")]
    [Min(0f)][SerializeField] private float openingBlackHold = 0.35f;
    [Min(0.05f)][SerializeField] private float fadeInDuration = 0.65f;
    [Min(0f)][SerializeField] private float finalFrameHold = 0.25f;
    [Min(0.05f)][SerializeField] private float fadeOutDuration = 0.8f;
    [Min(0f)][SerializeField] private float blackBeforeSceneLoad = 0.2f;

    [Header("Optional Skip")]
    [SerializeField] private bool allowSkip = true;
    [Min(0f)][SerializeField] private float skipInputDelay = 0.75f;

    private bool running;
    private bool skipRequested;
    private bool videoFinished;
    private float startTime;

    private void Awake()
    {
        Time.timeScale = 1f;

        if (blackBackground != null)
        {
            blackBackground.color = Color.black;
            blackBackground.raycastTarget = false;
        }

        if (fadeOverlay != null)
        {
            fadeOverlay.color = Color.black;
            fadeOverlay.raycastTarget = true;
            fadeOverlay.transform.SetAsLastSibling();
        }

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.errorReceived += OnVideoError;
        }

        if (videoImage != null)
        {
            Color c = videoImage.color;
            c.a = 1f;
            videoImage.color = c;
        }
    }

    private void Start()
    {
        StartCoroutine(StartupRoutine());
    }

    private void Update()
    {
        if (!running || !allowSkip || skipRequested)
            return;

        if (Time.unscaledTime - startTime < skipInputDelay)
            return;

        if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
            skipRequested = true;
    }

    private IEnumerator StartupRoutine()
    {
        running = true;
        startTime = Time.unscaledTime;
        skipRequested = false;
        videoFinished = false;

        SetFadeAlpha(1f);

        if (openingBlackHold > 0f)
            yield return WaitOrSkip(openingBlackHold);

        if (!skipRequested && videoPlayer != null && videoPlayer.clip != null)
        {
            videoPlayer.Prepare();

            while (!videoPlayer.isPrepared && !skipRequested)
                yield return null;

            if (!skipRequested)
                videoPlayer.Play();
        }
        else if (!skipRequested)
        {
            Debug.LogWarning("StartupLogoController: Assign a VideoPlayer with a VideoClip.");
            skipRequested = true;
        }

        if (!skipRequested)
            yield return Fade(1f, 0f, fadeInDuration);

        while (!videoFinished && !skipRequested)
            yield return null;

        if (!skipRequested && finalFrameHold > 0f)
            yield return WaitOrSkip(finalFrameHold);

        if (videoPlayer != null && skipRequested)
            videoPlayer.Pause();

        float currentAlpha = fadeOverlay != null ? fadeOverlay.color.a : 0f;
        yield return Fade(currentAlpha, 1f, fadeOutDuration);

        if (videoPlayer != null)
            videoPlayer.Stop();

        if (blackBeforeSceneLoad > 0f)
            yield return new WaitForSecondsRealtime(blackBeforeSceneLoad);

        if (string.IsNullOrWhiteSpace(mainMenuSceneName))
        {
            Debug.LogError("StartupLogoController: Main Menu Scene Name is empty.");
            running = false;
            yield break;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeOverlay == null)
            yield break;

        duration = Mathf.Max(0.01f, duration);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            t = t * t * (3f - 2f * t);
            SetFadeAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetFadeAlpha(to);
    }

    private IEnumerator WaitOrSkip(float duration)
    {
        float timer = 0f;

        while (timer < duration && !skipRequested)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeOverlay == null)
            return;

        Color c = fadeOverlay.color;
        c.a = Mathf.Clamp01(alpha);
        fadeOverlay.color = c;
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        videoFinished = true;
    }

    private void OnVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning("StartupLogoController Video Error: " + message);
        videoFinished = true;
    }

    private void OnDestroy()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.loopPointReached -= OnVideoFinished;
        videoPlayer.errorReceived -= OnVideoError;
    }
}
