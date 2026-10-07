using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TutorialUI : MonoBehaviour
{

    [Header("Main")]
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private Image dimBackground;
    [SerializeField] private RectTransform tutorialPanel;

    [Header("Information Panel UI")]
    public Image informationTitleImage;
    public TMP_Text informationDescriptionText;
    public TMP_Text informationProgressText;
    public Button informationContinueButton;
    public TMP_Text informationContinueButtonText;
    public CanvasGroup informationContentCanvasGroup;
    [SerializeField] private Vector2 informationTitleImageMaxSize = new Vector2(520f, 100f);

    [Header("Video / Action Panel UI")]
    public Image videoTitleImage;
    public TMP_Text videoDescriptionText;
    public TMP_Text videoProgressText;
    public Button videoContinueButton;
    public TMP_Text videoContinueButtonText;
    public CanvasGroup videoContentCanvasGroup;
    [SerializeField] private Vector2 videoTitleImageMaxSize = new Vector2(520f, 100f);

    [Header("Finish Transition")]
    [SerializeField] private Image fadeToBlackImage;
    [Min(0.05f)][SerializeField] private float fadeToBlackDuration = 1.25f;

    [Header("Panel Hover")]
    [Tooltip("How much larger the tutorial panel becomes while the mouse is over it.")]
    [Range(1f, 1.25f)]
    [SerializeField] private float hoverScale = 1.04f;
    [Tooltip("How quickly the panel grows and returns.")]
    [Min(0.01f)]
    [SerializeField] private float hoverScaleSpeed = 12f;

    private bool panelHovered;

    [Header("Panel Layouts")]
    [Tooltip("Drag your Information tutorial panel GameObject here.")]
    public GameObject informationTutorialPanel;

    [Tooltip("Drag your Video/Action tutorial panel GameObject here.")]
    public GameObject videoTutorialPanel;

    [Header("Panel Rect Transforms")]
    [Tooltip("RectTransform of the Information tutorial panel. Can be left empty if the GameObject above has a RectTransform.")]
    public RectTransform informationTutorialPanelRect;

    [Tooltip("RectTransform of the Video/Action tutorial panel. Can be left empty if the GameObject above has a RectTransform.")]
    public RectTransform videoTutorialPanelRect;

    [Header("Tutorial Video")]
    [SerializeField] private TutorialVideoDisplay tutorialVideoDisplay;

    [Header("Animation")]
    [SerializeField] private float showDuration = 0.22f;
    [SerializeField] private float hideDuration = 0.14f;
    [SerializeField] private float startingScale = 0.88f;
    [SerializeField] private float overshootScale = 1.04f;

    [Header("Step Content Fade")]
    [Tooltip("How long the old step content fades out.")]
    [Min(0.01f)]
    [SerializeField] private float contentFadeOutDuration = 0.12f;
    [Tooltip("How long the new step content fades in.")]
    [Min(0.01f)]
    [SerializeField] private float contentFadeInDuration = 0.18f;
    private Coroutine routine;
    private Coroutine contentRoutine;
    private Vector3 baseScale = Vector3.one;
    private Vector3 informationPanelBaseScale = Vector3.one;
    private Vector3 videoPanelBaseScale = Vector3.one;
    private bool hasShownContent;
    private bool usingVideoActionPanel;

    public Button InformationContinueButton => informationContinueButton;
    public Button VideoContinueButton => videoContinueButton;
    public Button ContinueButton => usingVideoActionPanel
        ? videoContinueButton
        : informationContinueButton;

    private void Update()
    {
        UpdatePanelHoverScale();
    }

    private void Awake()
    {
        if (tutorialVideoDisplay == null)
            tutorialVideoDisplay = GetComponentInChildren<TutorialVideoDisplay>(true);

        if (informationTutorialPanelRect == null && informationTutorialPanel != null)
            informationTutorialPanelRect = informationTutorialPanel.GetComponent<RectTransform>();

        if (videoTutorialPanelRect == null && videoTutorialPanel != null)
            videoTutorialPanelRect = videoTutorialPanel.GetComponent<RectTransform>();

        if (informationTutorialPanelRect != null)
            informationPanelBaseScale = informationTutorialPanelRect.localScale;

        if (videoTutorialPanelRect != null)
            videoPanelBaseScale = videoTutorialPanelRect.localScale;

        if (informationContinueButtonText == null && informationContinueButton != null)
            informationContinueButtonText = informationContinueButton.GetComponentInChildren<TMP_Text>(true);

        if (videoContinueButtonText == null && videoContinueButton != null)
            videoContinueButtonText = videoContinueButton.GetComponentInChildren<TMP_Text>(true);

        SetupFadeToBlackImage();

        if (rootCanvasGroup == null)
        {
            rootCanvasGroup = GetComponent<CanvasGroup>();

            if (rootCanvasGroup == null)
                rootCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (tutorialPanel != null)
            baseScale = tutorialPanel.localScale;

        HideImmediate();
    }

    public void SetContent(
        Sprite titleSprite,
        string description,
        int current,
        int total,
        bool showContinue,
        bool showDim)
    {
        if (!hasShownContent || !gameObject.activeInHierarchy)
        {
            ApplyContent(
                titleSprite, description,
                current, total, showContinue, showDim);

            SetContentAlpha(1f);
            hasShownContent = true;
            return;
        }

        if (contentRoutine != null)
            StopCoroutine(contentRoutine);

        contentRoutine = StartCoroutine(
            CrossFadeContentRoutine(
                titleSprite, description,
                current, total, showContinue, showDim));
    }

    private void ApplyContent(
        Sprite titleSprite,
        string description,
        int current,
        int total,
        bool showContinue,
        bool showDim)
    {
        ApplyContentToPanel(
            informationTitleImage,
            informationDescriptionText,
            informationProgressText,
            informationContinueButton,
            informationTitleImageMaxSize,
            titleSprite, description, current, total, showContinue);

        ApplyContentToPanel(
            videoTitleImage,
            videoDescriptionText,
            videoProgressText,
            videoContinueButton,
            videoTitleImageMaxSize,
            titleSprite, description, current, total, showContinue);

        if (dimBackground != null)
            dimBackground.gameObject.SetActive(showDim);
    }

    private void ApplyContentToPanel(
        Image panelTitle,
        TMP_Text panelDescription,
        TMP_Text panelProgress,
        Button panelContinue,
        Vector2 maxTitleSize,
        Sprite titleSprite,
        string description,
        int current,
        int total,
        bool showContinue)
    {
        if (panelTitle != null)
        {
            panelTitle.sprite = titleSprite;
            panelTitle.gameObject.SetActive(titleSprite != null);

            if (titleSprite != null)
            {
                panelTitle.preserveAspect = true;
                FitTitleImage(panelTitle, titleSprite, maxTitleSize);
            }
        }

        if (panelDescription != null)
            panelDescription.text = description;

        if (panelProgress != null)
            panelProgress.text = current + " / " + total;

        if (panelContinue != null)
            panelContinue.gameObject.SetActive(showContinue);
    }

    private IEnumerator CrossFadeContentRoutine(
        Sprite titleSprite,
        string description,
        int current,
        int total,
        bool showContinue,
        bool showDim)
    {
        float timer = 0f;
        float startAlpha = GetContentAlpha();

        while (timer < contentFadeOutDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(
                timer / Mathf.Max(0.01f, contentFadeOutDuration));

            SetContentAlpha(Mathf.Lerp(startAlpha, 0f, t));
            yield return null;
        }

        SetContentAlpha(0f);

        ApplyContent(
            titleSprite, description,
            current, total, showContinue, showDim);

        timer = 0f;

        while (timer < contentFadeInDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(
                timer / Mathf.Max(0.01f, contentFadeInDuration));

            // Smooth fade rather than a linear snap.
            t = t * t * (3f - 2f * t);
            SetContentAlpha(t);
            yield return null;
        }

        SetContentAlpha(1f);
        contentRoutine = null;
    }

    private float GetContentAlpha()
    {
        CanvasGroup activeGroup = usingVideoActionPanel
            ? videoContentCanvasGroup
            : informationContentCanvasGroup;

        return activeGroup != null ? activeGroup.alpha : 1f;
    }

    private void SetContentAlpha(float alpha)
    {
        if (informationContentCanvasGroup != null)
            informationContentCanvasGroup.alpha = alpha;

        if (videoContentCanvasGroup != null)
            videoContentCanvasGroup.alpha = alpha;

        SetPanelGraphicAlpha(informationTitleImage, informationDescriptionText,
            informationProgressText, informationContinueButton, alpha);

        SetPanelGraphicAlpha(videoTitleImage, videoDescriptionText,
            videoProgressText, videoContinueButton, alpha);
    }

    private void SetPanelGraphicAlpha(
        Graphic title,
        Graphic description,
        Graphic progress,
        Button button,
        float alpha)
    {
        SetGraphicAlpha(title, alpha);
        SetGraphicAlpha(description, alpha);
        SetGraphicAlpha(progress, alpha);

        if (button != null)
        {
            CanvasGroup group = button.GetComponent<CanvasGroup>();
            if (group == null)
                group = button.gameObject.AddComponent<CanvasGroup>();
            group.alpha = alpha;
        }
    }

    private void SetGraphicAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
            return;

        CanvasGroup group = graphic.GetComponent<CanvasGroup>();

        if (group == null)
            group = graphic.gameObject.AddComponent<CanvasGroup>();

        group.alpha = alpha;
    }

    private void FitTitleImage(Image targetImage, Sprite sprite, Vector2 maxSize)
    {
        if (targetImage == null || sprite == null)
            return;

        float width = sprite.rect.width;
        float height = sprite.rect.height;
        if (width <= 0f || height <= 0f)
            return;

        float scale = Mathf.Min(maxSize.x / width, maxSize.y / height);
        targetImage.rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal, width * scale);
        targetImage.rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical, height * scale);
    }

    public void SetPanelLayout(bool useVideoActionPanel)
    {
        usingVideoActionPanel = useVideoActionPanel;

        if (informationTutorialPanelRect == null && informationTutorialPanel != null)
            informationTutorialPanelRect = informationTutorialPanel.GetComponent<RectTransform>();

        if (videoTutorialPanelRect == null && videoTutorialPanel != null)
            videoTutorialPanelRect = videoTutorialPanel.GetComponent<RectTransform>();

        // Exactly one tutorial panel is active at a time.
        if (useVideoActionPanel)
        {
            if (informationTutorialPanel != null)
                informationTutorialPanel.SetActive(false);

            if (videoTutorialPanel != null)
                videoTutorialPanel.SetActive(true);
        }
        else
        {
            if (videoTutorialPanel != null)
                videoTutorialPanel.SetActive(false);

            if (informationTutorialPanel != null)
                informationTutorialPanel.SetActive(true);

            // Information steps do not use the demonstration video.
            if (tutorialVideoDisplay != null)
                tutorialVideoDisplay.SetVideo(null);
        }
    }

    public void SetContinueButtonLabel(string label)
    {
        if (informationContinueButtonText == null && informationContinueButton != null)
            informationContinueButtonText = informationContinueButton.GetComponentInChildren<TMP_Text>(true);

        if (videoContinueButtonText == null && videoContinueButton != null)
            videoContinueButtonText = videoContinueButton.GetComponentInChildren<TMP_Text>(true);

        if (informationContinueButtonText != null)
            informationContinueButtonText.text = label;

        if (videoContinueButtonText != null)
            videoContinueButtonText.text = label;
    }

    public IEnumerator FadeEverythingToBlack()
    {
        SetupFadeToBlackImage();
        if (fadeToBlackImage == null) yield break;

        fadeToBlackImage.gameObject.SetActive(true);
        fadeToBlackImage.transform.SetAsLastSibling();
        fadeToBlackImage.raycastTarget = true;

        Color c = fadeToBlackImage.color;
        c.a = 0f;
        fadeToBlackImage.color = c;

        float timer = 0f;
        float duration = Mathf.Max(0.05f, fadeToBlackDuration);
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            t = t * t * (3f - 2f * t);
            c.a = t;
            fadeToBlackImage.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeToBlackImage.color = c;
    }

    private void SetupFadeToBlackImage()
    {
        if (fadeToBlackImage == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                GameObject go = new GameObject("Tutorial Fade To Black",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(canvas.transform, false);

                RectTransform r = go.GetComponent<RectTransform>();
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = Vector2.zero;
                r.offsetMax = Vector2.zero;
                fadeToBlackImage = go.GetComponent<Image>();
            }
        }

        if (fadeToBlackImage != null)
        {
            fadeToBlackImage.color = new Color(0f, 0f, 0f, 0f);
            fadeToBlackImage.raycastTarget = false;
            fadeToBlackImage.gameObject.SetActive(false);
        }
    }

    public void SetTutorialVideo(VideoClip clip)
    {
        if (tutorialVideoDisplay == null)
            tutorialVideoDisplay = GetComponentInChildren<TutorialVideoDisplay>(true);

        if (tutorialVideoDisplay != null)
            tutorialVideoDisplay.SetVideo(clip);
    }

    public void Show()
    {
        // Between tutorial steps the panel remains visible. This prevents
        // the whole panel from disappearing/reappearing on every step.
        if (gameObject.activeInHierarchy &&
            rootCanvasGroup != null &&
            rootCanvasGroup.alpha >= 0.99f)
        {
            rootCanvasGroup.interactable = true;
            rootCanvasGroup.blocksRaycasts = true;
            return;
        }

        gameObject.SetActive(true);

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(ShowRoutine());
    }

    public void Hide()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(HideRoutine());
    }

    public void HideImmediate()
    {
        if (rootCanvasGroup != null)
        {
            rootCanvasGroup.alpha = 0f;
            rootCanvasGroup.interactable = false;
            rootCanvasGroup.blocksRaycasts = false;
        }

        if (tutorialPanel != null)
            tutorialPanel.localScale = baseScale * startingScale;

        if (contentRoutine != null)
        {
            StopCoroutine(contentRoutine);
            contentRoutine = null;
        }

        hasShownContent = false;
        SetContentAlpha(1f);
        panelHovered = false;

        gameObject.SetActive(false);
    }

    private void UpdatePanelHoverScale()
    {
        RectTransform activePanel = GetActiveTutorialPanelRect();

        if (activePanel == null || !activePanel.gameObject.activeInHierarchy)
            return;

        bool hoveredNow = RectTransformUtility.RectangleContainsScreenPoint(
            activePanel,
            Input.mousePosition,
            GetTutorialCanvasCamera());

        panelHovered = hoveredNow;

        float targetMultiplier = panelHovered ? hoverScale : 1f;
        Vector3 panelBaseScale = usingVideoActionPanel
            ? videoPanelBaseScale
            : informationPanelBaseScale;

        Vector3 targetScale = panelBaseScale * targetMultiplier;

        float t = 1f - Mathf.Exp(-hoverScaleSpeed * Time.unscaledDeltaTime);

        activePanel.localScale = Vector3.Lerp(
            activePanel.localScale,
            targetScale,
            t);
    }

    private Camera GetTutorialCanvasCamera()
    {
        RectTransform activePanel = GetActiveTutorialPanelRect();
        if (activePanel == null)
            return null;

        Canvas canvas = activePanel.GetComponentInParent<Canvas>();

        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }


    private RectTransform GetActiveTutorialPanelRect()
    {
        return usingVideoActionPanel
            ? videoTutorialPanelRect
            : informationTutorialPanelRect;
    }

    public void SetPanelScreenPosition(Vector2 screenPosition)
    {
        RectTransform activePanel = GetActiveTutorialPanelRect();

        if (activePanel == null)
            return;

        Canvas canvas = activePanel.GetComponentInParent<Canvas>();

        RectTransform canvasRect =
            canvas != null
                ? canvas.transform as RectTransform
                : null;

        if (canvasRect == null)
            return;

        Camera cam =
            canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            cam,
            out Vector2 local))
        {
            activePanel.anchoredPosition = local;
        }
    }

    public Vector2 GetPanelScreenSize()
    {
        RectTransform activePanel = GetActiveTutorialPanelRect();

        if (activePanel == null)
            return Vector2.zero;

        Vector2 size = activePanel.rect.size;
        size.x *= Mathf.Abs(activePanel.lossyScale.x);
        size.y *= Mathf.Abs(activePanel.lossyScale.y);
        return size;
    }

    private IEnumerator ShowRoutine()
    {
        rootCanvasGroup.interactable = true;
        rootCanvasGroup.blocksRaycasts = true;

        float timer = 0f;

        while (timer < showDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer / Mathf.Max(0.01f, showDuration));

            rootCanvasGroup.alpha = t;

            if (tutorialPanel != null)
            {
                float scale;

                if (t < 0.72f)
                {
                    float first =
                        Mathf.Clamp01(t / 0.72f);

                    scale =
                        Mathf.Lerp(
                            startingScale,
                            overshootScale,
                            1f - Mathf.Pow(1f - first, 3f));
                }
                else
                {
                    scale =
                        Mathf.Lerp(
                            overshootScale,
                            1f,
                            Mathf.InverseLerp(0.72f, 1f, t));
                }

                tutorialPanel.localScale =
                    baseScale * scale;
            }

            yield return null;
        }

        rootCanvasGroup.alpha = 1f;

        if (tutorialPanel != null)
            tutorialPanel.localScale = baseScale;

        routine = null;
    }

    private IEnumerator HideRoutine()
    {
        float startAlpha =
            rootCanvasGroup.alpha;

        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = false;

        float timer = 0f;

        while (timer < hideDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer / Mathf.Max(0.01f, hideDuration));

            rootCanvasGroup.alpha =
                Mathf.Lerp(startAlpha, 0f, t);

            if (tutorialPanel != null)
            {
                tutorialPanel.localScale =
                    baseScale *
                    Mathf.Lerp(1f, startingScale, t);
            }

            yield return null;
        }

        routine = null;
        HideImmediate();
    }



}
