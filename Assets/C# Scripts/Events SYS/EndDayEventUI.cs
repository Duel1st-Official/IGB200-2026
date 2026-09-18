using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndDayEventUI : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    [Tooltip("The End Day Event System containing the rolled event.")]
    [SerializeField]
    private EndDayEventSystem eventSystem;

    [Tooltip("Root object containing the entire Night Report UI.")]
    [SerializeField]
    private GameObject uiRoot;

    [Tooltip("CanvasGroup controlling the fullscreen dark background.")]
    [SerializeField]
    private CanvasGroup backgroundCanvasGroup;

    [Tooltip("CanvasGroup controlling the report panel.")]
    [SerializeField]
    private CanvasGroup panelCanvasGroup;

    [Tooltip("The report panel used for the scale animation.")]
    [SerializeField]
    private RectTransform reportPanel;

    [Header("Event Backgrounds")]
    [Tooltip("The wooden/parchment panel Image, not the fullscreen dark overlay.")]
    [SerializeField] private Image reportBackgroundImage;
    [SerializeField] private Sprite normalEventBackground;
    [SerializeField] private Sprite extremeEventBackground;

    [Header("Effect Colours")]
    [SerializeField] private Color beneficialEffectColor = new Color(0.16f, 0.38f, 0.12f, 1f);
    [SerializeField] private Color harmfulEffectColor = new Color(0.65f, 0.12f, 0.09f, 1f);
    [SerializeField] private Color neutralEffectColor = new Color(0.20f, 0.16f, 0.12f, 1f);

    private Sprite originalReportBackground;
    private bool reportBackgroundCached;

    // =========================================================
    // TEXT
    // =========================================================

    [Header("Text")]

    [SerializeField]
    private TMP_Text headerText;

    [SerializeField]
    private TMP_Text eventTitleText;

    [SerializeField]
    private TMP_Text eventDescriptionText;

    [SerializeField]
    private TMP_Text effectText;

    [TextArea]
    [SerializeField]
    private string header =
        "NIGHT REPORT";

    // =========================================================
    // ICON
    // =========================================================

    [Header("Event Icon")]

    [SerializeField]
    private Image eventIcon;

    [Tooltip(
        "Hide the icon object when the selected event " +
        "does not have an icon assigned."
    )]
    [SerializeField]
    private bool hideIconWhenMissing =
        true;

    // =========================================================
    // CONTINUE BUTTON
    // =========================================================

    [Header("Continue Button")]

    [SerializeField]
    private Button continueButton;

    [SerializeField]
    private TMP_Text continueButtonText;

    [SerializeField]
    private string continueText =
        "CONTINUE";

    // =========================================================
    // ANIMATION
    // =========================================================

    [Header("Opening Animation")]

    [Tooltip("How long the report takes to appear.")]
    [Min(0.01f)]
    [SerializeField]
    private float openDuration =
        0.3f;

    [Tooltip("Starting scale of the report.")]
    [Range(0.5f, 1f)]
    [SerializeField]
    private float startScale =
        0.90f;

    [Tooltip("Maximum alpha of the fullscreen dark background.")]
    [Range(0f, 1f)]
    [SerializeField]
    private float backgroundAlpha =
        0.65f;

    [Header("Closing Animation")]

    [Tooltip("How long the report takes to disappear.")]
    [Min(0.01f)]
    [SerializeField]
    private float closeDuration =
        0.2f;

    [Tooltip(
        "Prevent the Continue button being clicked immediately " +
        "when the report appears."
    )]
    [Min(0f)]
    [SerializeField]
    private float continueButtonDelay =
        0.25f;

    // =========================================================
    // AUDIO
    // =========================================================

    [Header("Audio")]

    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip reportOpenSound;

    [SerializeField]
    private AudioClip continueSound;

    [Range(0f, 1f)]
    [SerializeField]
    private float reportOpenVolume =
        0.8f;

    [Range(0f, 1f)]
    [SerializeField]
    private float continueVolume =
        0.8f;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLogs =
        false;

    // =========================================================
    // PRIVATE
    // =========================================================

    private Coroutine animationRoutine;

    [Header("Staggered Content Animation")]
    [SerializeField] private bool animateReportContent = true;
    [Min(0.01f)]
    [SerializeField] private float contentRevealDuration = 0.28f;
    [Min(0f)]
    [SerializeField] private float contentStagger = 0.07f;

    private readonly System.Collections.Generic.List<ContentVisual> contentVisuals =
        new System.Collections.Generic.List<ContentVisual>();

    private sealed class ContentVisual
    {
        public Transform target;
        public CanvasGroup group;
        public Vector3 scale;
        public float alpha;
        public bool pop;
    }

    private void PrepareContentAnimation()
    {
        RestoreContentVisuals();
        contentVisuals.Clear();
        if (!animateReportContent) return;

        AddContentVisual(headerText, false);
        AddContentVisual(eventIcon, true);
        AddContentVisual(eventTitleText, false);
        AddContentVisual(eventDescriptionText, false);
        AddContentVisual(effectText, true);
        AddContentVisual(continueButton, false);

        SetContentProgress(0f);
    }

    private void AddContentVisual(Component component, bool pop)
    {
        if (component == null || !component.gameObject.activeInHierarchy) return;
        Transform target = component.transform;
        // Only animate dedicated content objects, never a shared panel/root.
        if (target == transform || target == reportPanel ||
            (uiRoot != null && target == uiRoot.transform) ||
            (panelCanvasGroup != null && target == panelCanvasGroup.transform) ||
            (backgroundCanvasGroup != null && target == backgroundCanvasGroup.transform)) return;
        foreach (ContentVisual visual in contentVisuals)
        {
            if (target == visual.target || target.IsChildOf(visual.target) ||
                visual.target.IsChildOf(target)) return;
        }
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null) group = target.gameObject.AddComponent<CanvasGroup>();
        contentVisuals.Add(new ContentVisual
        {
            target = target,
            group = group,
            scale = target.localScale,
            alpha = group.alpha,
            pop = pop
        });
    }

    private void SetContentProgress(float elapsed)
    {
        for (int i = 0; i < contentVisuals.Count; i++)
        {
            ContentVisual visual = contentVisuals[i];
            if (visual.target == null || visual.group == null) continue;
            float progress = Mathf.Clamp01(
                (elapsed - i * Mathf.Max(0f, contentStagger)) /
                Mathf.Max(0.01f, contentRevealDuration));
            visual.group.alpha = visual.alpha * SmoothStep(progress);
            float eased = visual.pop ? EaseOutBack(progress) : SmoothStep(progress);
            visual.target.localScale = visual.scale *
                Mathf.LerpUnclamped(visual.pop ? 0.72f : 0.96f, 1f, eased);
        }
    }

    private IEnumerator RevealContentRoutine()
    {
        if (contentVisuals.Count == 0) yield break;
        float duration = Mathf.Max(0.01f, contentRevealDuration) +
            (contentVisuals.Count - 1) * Mathf.Max(0f, contentStagger);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetContentProgress(elapsed);
            yield return null;
        }
        RestoreContentVisuals();
    }

    private void RestoreContentVisuals()
    {
        foreach (ContentVisual visual in contentVisuals)
        {
            if (visual.target != null) visual.target.localScale = visual.scale;
            if (visual.group != null) visual.group.alpha = visual.alpha;
        }
    }

    private bool reportOpen;
    private bool closing;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        AutoAssignReferences();

        SetupButton();

        HideImmediately();
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        AutoAssignReferences();
    }

    // =========================================================
    // AUTO ASSIGN
    // =========================================================

    private void AutoAssignReferences()
    {
        if (eventSystem == null)
        {
            eventSystem =
                FindFirstObjectByType<EndDayEventSystem>(
                    FindObjectsInactive.Include
                );
        }

        if (audioSource == null)
        {
            audioSource =
                GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource =
                gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake =
                false;

            audioSource.loop =
                false;

            audioSource.spatialBlend =
                0f;
        }

        if (uiRoot == null)
        {
            uiRoot =
                gameObject;
        }
    }

    // =========================================================
    // BUTTON SETUP
    // =========================================================

    private void SetupButton()
    {
        if (continueButton == null)
        {
            return;
        }

        continueButton.onClick.RemoveListener(
            HandleContinueClicked
        );

        continueButton.onClick.AddListener(
            HandleContinueClicked
        );
    }

    // =========================================================
    // SHOW LAST EVENT
    // =========================================================

    public void ShowLastEvent()
    {
        AutoAssignReferences();

        if (eventSystem == null)
        {
            Debug.LogWarning(
                "[EndDayEventUI] No EndDayEventSystem found."
            );

            return;
        }

        PopulateUI(
            eventSystem.GetLastEventTitle(),
            eventSystem.GetLastEventDescription(),
            eventSystem.GetLastEventIcon(),
            eventSystem.GetLastEventTarget(),
            eventSystem.GetLastEventAmount()
        );

        ApplyReportBackground(eventSystem.IsLastEventExtreme());

        Show();
    }

    // =========================================================
    // POPULATE UI
    // =========================================================

    private void PopulateUI(
        string eventTitle,
        string description,
        Sprite icon,
        EndDayEventSystem.EventTarget target,
        float amount)
    {
        if (headerText != null)
        {
            headerText.text =
                header;
        }

        if (eventTitleText != null)
        {
            eventTitleText.text =
                string.IsNullOrWhiteSpace(eventTitle)
                    ? "QUIET NIGHT"
                    : eventTitle;
        }

        if (eventDescriptionText != null)
        {
            eventDescriptionText.text =
                description;
        }

        if (continueButtonText != null)
        {
            continueButtonText.text =
                continueText;
        }

        // =====================================================
        // ICON
        // =====================================================

        if (eventIcon != null)
        {
            // Display the selected event sprite without stretching or
            // intercepting pointer input intended for the report controls.
            eventIcon.type = Image.Type.Simple;
            eventIcon.preserveAspect = true;
            eventIcon.raycastTarget = false;
            eventIcon.overrideSprite = null;

            bool hasIcon =
                icon != null;

            if (hideIconWhenMissing)
            {
                eventIcon.gameObject.SetActive(
                    hasIcon
                );
            }
            else
            {
                eventIcon.gameObject.SetActive(
                    true
                );
            }

            eventIcon.sprite =
                icon;

            eventIcon.enabled =
                hasIcon;
        }

        // =====================================================
        // EFFECT
        // =====================================================

        if (effectText != null)
        {
            effectText.color = GetEffectColor(target, amount);

            effectText.text =
                BuildEffectText(
                    target,
                    amount
                );
        }
    }

    // =========================================================
    // EFFECT TEXT
    // =========================================================

    private void ApplyReportBackground(bool extreme)
    {
        if (reportBackgroundImage == null) return;

        if (!reportBackgroundCached)
        {
            originalReportBackground = reportBackgroundImage.overrideSprite;
            reportBackgroundCached = true;
        }

        Sprite normal = normalEventBackground != null
            ? normalEventBackground : originalReportBackground;
        Sprite selected = extreme && extremeEventBackground != null
            ? extremeEventBackground : normal;

        // Explicitly restore the normal art after an extreme event.
        // Missing extreme art falls back to normal; existing layout is preserved.
        reportBackgroundImage.overrideSprite = null;
        reportBackgroundImage.sprite = selected;
    }

    private Color GetEffectColor(EndDayEventSystem.EventTarget target, float amount)
    {
        if (amount == 0f || float.IsNaN(amount)) return neutralEffectColor;

        switch (target)
        {
            case EndDayEventSystem.EventTarget.PredatorPressure:
            case EndDayEventSystem.EventTarget.FireRisk:
                return amount < 0f ? beneficialEffectColor : harmfulEffectColor;

            case EndDayEventSystem.EventTarget.PreyAvailability:
            case EndDayEventSystem.EventTarget.SoilHealth:
            case EndDayEventSystem.EventTarget.BatPopulation:
            case EndDayEventSystem.EventTarget.BatHealth:
            case EndDayEventSystem.EventTarget.BatFood:
                return amount > 0f ? beneficialEffectColor : harmfulEffectColor;

            default:
                return neutralEffectColor;
        }
    }

    private string BuildEffectText(
        EndDayEventSystem.EventTarget target,
        float amount)
    {
        if (target ==
            EndDayEventSystem.EventTarget.None)
        {
            return
                "NO STAT CHANGES";
        }

        string statName =
            GetTargetDisplayName(
                target
            );

        string amountText;

        if (amount > 0f)
        {
            amountText =
                "+" +
                amount.ToString("0.#");
        }
        else
        {
            amountText =
                amount.ToString("0.#");
        }

        return
            statName +
            "  " +
            amountText;
    }

    // =========================================================
    // TARGET DISPLAY NAME
    // =========================================================

    private string GetTargetDisplayName(
        EndDayEventSystem.EventTarget target)
    {
        switch (target)
        {
            case EndDayEventSystem.EventTarget.PreyAvailability:
                return "PREY AVAILABILITY";

            case EndDayEventSystem.EventTarget.PredatorPressure:
                return "PREDATOR PRESSURE";

            case EndDayEventSystem.EventTarget.FireRisk:
                return "FIRE RISK";

            case EndDayEventSystem.EventTarget.SoilHealth:
                return "SOIL HEALTH";

            case EndDayEventSystem.EventTarget.BatPopulation:
                return "BAT POPULATION";

            case EndDayEventSystem.EventTarget.BatHealth:
                return "BAT HEALTH";

            case EndDayEventSystem.EventTarget.BatFood:
                return "BAT FOOD";

            default:
                return "";
        }
    }

    // =========================================================
    // SHOW
    // =========================================================

    public void Show()
    {
        if (reportOpen ||
            closing)
        {
            return;
        }

        if (animationRoutine != null)
        {
            StopCoroutine(
                animationRoutine
            );
        }

        animationRoutine =
            StartCoroutine(
                OpenRoutine()
            );
    }

    // =========================================================
    // OPEN ROUTINE
    // =========================================================

    private IEnumerator OpenRoutine()
    {
        reportOpen =
            true;

        closing =
            false;

        if (uiRoot != null)
        {
            uiRoot.SetActive(
                true
            );
        }

        if (backgroundCanvasGroup != null)
        {
            backgroundCanvasGroup.alpha =
                0f;

            backgroundCanvasGroup.blocksRaycasts =
                true;

            backgroundCanvasGroup.interactable =
                false;
        }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha =
                0f;

            panelCanvasGroup.blocksRaycasts =
                true;

            panelCanvasGroup.interactable =
                true;
        }

        if (reportPanel != null)
        {
            reportPanel.localScale =
                Vector3.one *
                startScale;
        }

        if (continueButton != null)
        {
            continueButton.interactable =
                false;
        }

        PlaySound(
            reportOpenSound,
            reportOpenVolume
        );

        PrepareContentAnimation();

        float timer =
            0f;

        while (timer <
               openDuration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    timer /
                    Mathf.Max(
                        0.01f,
                        openDuration
                    )
                );

            float eased =
                EaseOutBack(
                    progress
                );

            if (backgroundCanvasGroup != null)
            {
                backgroundCanvasGroup.alpha =
                    Mathf.Lerp(
                        0f,
                        backgroundAlpha,
                        progress
                    );
            }

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha =
                    progress;
            }

            if (reportPanel != null)
            {
                float scale =
                    Mathf.LerpUnclamped(
                        startScale,
                        1f,
                        eased
                    );

                reportPanel.localScale =
                    Vector3.one *
                    scale;
            }

            yield return null;
        }

        if (backgroundCanvasGroup != null)
        {
            backgroundCanvasGroup.alpha =
                backgroundAlpha;
        }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha =
                1f;
        }

        if (reportPanel != null)
        {
            reportPanel.localScale =
                Vector3.one;
        }

        yield return RevealContentRoutine();

        if (continueButtonDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    continueButtonDelay
                );
        }

        if (continueButton != null)
        {
            continueButton.interactable =
                true;
        }

        animationRoutine =
            null;

        if (showDebugLogs)
        {
            Debug.Log(
                "[EndDayEventUI] Night Report opened."
            );
        }
    }

    // =========================================================
    // CONTINUE
    // =========================================================

    private void HandleContinueClicked()
    {
        if (!reportOpen ||
            closing)
        {
            return;
        }

        if (continueButton != null &&
            !continueButton.interactable)
        {
            return;
        }

        PlaySound(
            continueSound,
            continueVolume
        );

        Close();
    }

    // =========================================================
    // CLOSE
    // =========================================================

    public void Close()
    {
        if (!reportOpen ||
            closing)
        {
            return;
        }

        if (animationRoutine != null)
        {
            StopCoroutine(
                animationRoutine
            );
        }

        animationRoutine =
            StartCoroutine(
                CloseRoutine()
            );
    }

    // =========================================================
    // CLOSE ROUTINE
    // =========================================================

    private IEnumerator CloseRoutine()
    {
        closing =
            true;

        if (continueButton != null)
        {
            continueButton.interactable =
                false;
        }

        float startingBackgroundAlpha =
            backgroundCanvasGroup != null
                ? backgroundCanvasGroup.alpha
                : 0f;

        float startingPanelAlpha =
            panelCanvasGroup != null
                ? panelCanvasGroup.alpha
                : 1f;

        Vector3 startingScale =
            reportPanel != null
                ? reportPanel.localScale
                : Vector3.one;

        float timer =
            0f;

        while (timer <
               closeDuration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    timer /
                    Mathf.Max(
                        0.01f,
                        closeDuration
                    )
                );

            float eased =
                SmoothStep(
                    progress
                );

            if (backgroundCanvasGroup != null)
            {
                backgroundCanvasGroup.alpha =
                    Mathf.Lerp(
                        startingBackgroundAlpha,
                        0f,
                        eased
                    );
            }

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha =
                    Mathf.Lerp(
                        startingPanelAlpha,
                        0f,
                        eased
                    );
            }

            if (reportPanel != null)
            {
                reportPanel.localScale =
                    Vector3.Lerp(
                        startingScale,
                        Vector3.one *
                        startScale,
                        eased
                    );
            }

            yield return null;
        }

        HideImmediately();

        animationRoutine =
            null;

        if (showDebugLogs)
        {
            Debug.Log(
                "[EndDayEventUI] Night Report closed."
            );
        }
    }

    // =========================================================
    // HIDE IMMEDIATELY
    // =========================================================

    public void HideImmediately()
    {
        RestoreContentVisuals();

        reportOpen =
            false;

        closing =
            false;

        if (backgroundCanvasGroup != null)
        {
            backgroundCanvasGroup.alpha =
                0f;

            backgroundCanvasGroup.blocksRaycasts =
                false;

            backgroundCanvasGroup.interactable =
                false;
        }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha =
                0f;

            panelCanvasGroup.blocksRaycasts =
                false;

            panelCanvasGroup.interactable =
                false;
        }

        if (reportPanel != null)
        {
            reportPanel.localScale =
                Vector3.one *
                startScale;
        }

        if (continueButton != null)
        {
            continueButton.interactable =
                false;
        }

        if (uiRoot != null &&
            uiRoot != gameObject)
        {
            uiRoot.SetActive(
                false
            );
        }
    }

    // =========================================================
    // IS OPEN
    // =========================================================

    public bool IsOpen()
    {
        return reportOpen;
    }

    // =========================================================
    // AUDIO
    // =========================================================

    private void PlaySound(
        AudioClip clip,
        float volume)
    {
        if (clip == null)
        {
            return;
        }

        if (audioSource == null)
        {
            AutoAssignReferences();
        }

        if (audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(
            clip,
            volume
        );
    }

    // =========================================================
    // EASING
    // =========================================================

    private float SmoothStep(
        float value)
    {
        value =
            Mathf.Clamp01(
                value
            );

        return
            value *
            value *
            (
                3f -
                2f *
                value
            );
    }

    private float EaseOutBack(
        float value)
    {
        value =
            Mathf.Clamp01(
                value
            );

        const float c1 =
            1.70158f;

        const float c3 =
            c1 + 1f;

        float x =
            value - 1f;

        return
            1f +
            c3 *
            x *
            x *
            x +
            c1 *
            x *
            x;
    }

    // =========================================================
    // DEBUG
    // =========================================================

    [ContextMenu("Debug - Roll And Show Event")]
    private void DebugRollAndShowEvent()
    {
        AutoAssignReferences();

        if (eventSystem == null)
        {
            return;
        }

        eventSystem.RollRandomEvent();

        ShowLastEvent();
    }

    [ContextMenu("Debug - Show Last Event")]
    private void DebugShowLastEvent()
    {
        ShowLastEvent();
    }

    [ContextMenu("Debug - Hide Report")]
    private void DebugHideReport()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(
                animationRoutine
            );

            animationRoutine =
                null;
        }

        HideImmediately();
    }

    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(
                HandleContinueClicked
            );
        }
    }
}
