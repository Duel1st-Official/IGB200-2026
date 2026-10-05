using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TutorialUI : MonoBehaviour
{

    [Header("Main")]
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private Image dimBackground;
    [SerializeField] private RectTransform tutorialPanel;

    [Header("Title Image")]
    [SerializeField] private Image titleImage;
    [SerializeField] private Vector2 titleImageMaxSize = new Vector2(520f, 100f);

    [Header("Text")]
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text progressText;

    [Header("Optional")]
    [SerializeField] private Button continueButton;
    [Tooltip("Optional. If empty, the script finds a TMP text component inside the Continue button.")]
    [SerializeField] private TMP_Text continueButtonText;

    [Header("Finish Transition")]
    [Tooltip("Optional full-screen black Image. If empty, one is created automatically at runtime.")]
    [SerializeField] private Image fadeToBlackImage;
    [Min(0.05f)]
    [SerializeField] private float fadeToBlackDuration = 1.25f;

    [Header("Panel Hover")]
    [Tooltip("How much larger the tutorial panel becomes while the mouse is over it.")]
    [Range(1f, 1.25f)]
    [SerializeField] private float hoverScale = 1.04f;
    [Tooltip("How quickly the panel grows and returns.")]
    [Min(0.01f)]
    [SerializeField] private float hoverScaleSpeed = 12f;

    private bool panelHovered;

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
    [Tooltip("Recommended: assign a Content Root containing the title, description, progress, hint and Continue button. The wooden/parchment panel should NOT be inside this CanvasGroup.")]
    [SerializeField] private CanvasGroup contentCanvasGroup;

    private Coroutine routine;
    private Coroutine contentRoutine;
    private Vector3 baseScale = Vector3.one;
    private bool hasShownContent;

    public Button ContinueButton => continueButton;

    private void Update()
    {
        UpdatePanelHoverScale();
    }

    private void Awake()
    {
        if (rootCanvasGroup == null)
        {
            rootCanvasGroup = GetComponent<CanvasGroup>();

            if (rootCanvasGroup == null)
                rootCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (tutorialPanel != null)
            baseScale = tutorialPanel.localScale;

        if (continueButtonText == null && continueButton != null)
            continueButtonText = continueButton.GetComponentInChildren<TMP_Text>(true);

        SetupFadeToBlackImage();
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
        if (titleImage != null)
        {
            titleImage.sprite = titleSprite;
            titleImage.gameObject.SetActive(titleSprite != null);

            if (titleSprite != null)
            {
                titleImage.preserveAspect = true;
                FitTitleImage(titleSprite);
            }
        }

        if (descriptionText != null)
            descriptionText.text = description;

        if (progressText != null)
            progressText.text = current + " / " + total;

        if (continueButton != null)
            continueButton.gameObject.SetActive(showContinue);

        if (dimBackground != null)
            dimBackground.gameObject.SetActive(showDim);
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
        return contentCanvasGroup != null
            ? contentCanvasGroup.alpha
            : 1f;
    }

    private void SetContentAlpha(float alpha)
    {
        if (contentCanvasGroup != null)
        {
            contentCanvasGroup.alpha = alpha;
            return;
        }

        SetGraphicAlpha(titleImage, alpha);
        SetGraphicAlpha(descriptionText, alpha);
        SetGraphicAlpha(progressText, alpha);

        if (continueButton != null)
        {
            CanvasGroup group = continueButton.GetComponent<CanvasGroup>();
            if (group == null)
                group = continueButton.gameObject.AddComponent<CanvasGroup>();
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

    private void FitTitleImage(Sprite sprite)
    {
        if (titleImage == null || sprite == null)
            return;

        float width = sprite.rect.width;
        float height = sprite.rect.height;

        if (width <= 0f || height <= 0f)
            return;

        float scale = Mathf.Min(
            titleImageMaxSize.x / width,
            titleImageMaxSize.y / height,
            1f);

        titleImage.rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            width * scale);

        titleImage.rectTransform.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            height * scale);
    }

    public void SetContinueButtonLabel(string label)
    {
        if (continueButtonText == null && continueButton != null)
            continueButtonText = continueButton.GetComponentInChildren<TMP_Text>(true);

        if (continueButtonText != null)
            continueButtonText.text = label;
    }

    public float GetFadeToBlackDuration()
    {
        return Mathf.Max(0.05f, fadeToBlackDuration);
    }

    public IEnumerator FadeEverythingToBlack()
    {
        SetupFadeToBlackImage();

        if (fadeToBlackImage == null)
            yield break;

        fadeToBlackImage.gameObject.SetActive(true);
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
            // Smoothstep makes the world and tutorial panel disappear together.
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
            if (canvas == null)
                canvas = FindFirstObjectByType<Canvas>();

            if (canvas != null)
            {
                GameObject fadeObject = new GameObject(
                    "Tutorial Fade To Black",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

                fadeObject.transform.SetParent(canvas.transform, false);

                RectTransform rect = fadeObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                fadeToBlackImage = fadeObject.GetComponent<Image>();
                fadeToBlackImage.color = new Color(0f, 0f, 0f, 0f);
                fadeToBlackImage.raycastTarget = true;
                fadeObject.transform.SetAsLastSibling();
            }
        }

        if (fadeToBlackImage != null)
        {
            Color c = fadeToBlackImage.color;
            c.r = 0f;
            c.g = 0f;
            c.b = 0f;
            c.a = 0f;
            fadeToBlackImage.color = c;
            fadeToBlackImage.raycastTarget = false;
            fadeToBlackImage.gameObject.SetActive(false);
        }
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
        if (tutorialPanel == null || !tutorialPanel.gameObject.activeInHierarchy)
            return;

        bool hoveredNow = RectTransformUtility.RectangleContainsScreenPoint(
            tutorialPanel,
            Input.mousePosition,
            GetTutorialCanvasCamera());

        panelHovered = hoveredNow;

        float targetMultiplier = panelHovered ? hoverScale : 1f;
        Vector3 targetScale = baseScale * targetMultiplier;

        float t = 1f - Mathf.Exp(-hoverScaleSpeed * Time.unscaledDeltaTime);

        tutorialPanel.localScale = Vector3.Lerp(
            tutorialPanel.localScale,
            targetScale,
            t);
    }

    private Camera GetTutorialCanvasCamera()
    {
        if (tutorialPanel == null)
            return null;

        Canvas canvas = tutorialPanel.GetComponentInParent<Canvas>();

        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    public void SetPanelScreenPosition(Vector2 screenPosition)
    {
        if (tutorialPanel == null)
            return;

        Canvas canvas =
            tutorialPanel.GetComponentInParent<Canvas>();

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
            tutorialPanel.anchoredPosition = local;
        }
    }

    public Vector2 GetPanelScreenSize()
    {
        if (tutorialPanel == null)
            return Vector2.zero;

        Vector2 size = tutorialPanel.rect.size;

        size.x *= Mathf.Abs(tutorialPanel.lossyScale.x);
        size.y *= Mathf.Abs(tutorialPanel.lossyScale.y);

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
