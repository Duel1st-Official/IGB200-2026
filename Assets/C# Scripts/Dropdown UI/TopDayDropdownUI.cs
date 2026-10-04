using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class TopDayDropdownUI : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("Day Progress Position - Always Visible")]
    [Tooltip("Optional container for the progress bar and its text. Defaults to Day Progress Visual. Use a separate sibling of Dropdown Panel.")]
    [SerializeField] private RectTransform dayProgressMoveTarget;
    [Tooltip("Distance downward in UI units when the top bar is Always visible.")]
    [Min(0f)][SerializeField] private float alwaysVisibleProgressDrop = 150f;
    [Min(0.01f)][SerializeField] private float progressMoveSpeed = 15f;
    private RectTransform cachedProgressMoveTarget;
    private Vector2 originalProgressPosition;

    private void UpdateProgressPosition()
    {
        RectTransform target = dayProgressMoveTarget != null
            ? dayProgressMoveTarget : dayProgressVisual;
        if (target == null) return;

        // Never move the dropdown itself, or a shared parent containing it.
        if (dropdownPanel != null &&
            (target == dropdownPanel || dropdownPanel.IsChildOf(target))) return;

        if (cachedProgressMoveTarget != target)
        {
            if (cachedProgressMoveTarget != null)
                cachedProgressMoveTarget.anchoredPosition = originalProgressPosition;
            cachedProgressMoveTarget = target;
            originalProgressPosition = target.anchoredPosition;
        }

        bool alwaysVisible = endDaySystem != null &&
            endDaySystem.GetShowTopDayBar() && !endDaySystem.IsTransitionRunning();
        Vector2 destination = originalProgressPosition;
        // Children already inherit the dropdown's slide; avoid doubling it.
        bool movesWithDropdown = dropdownPanel != null && target.IsChildOf(dropdownPanel);
        if (alwaysVisible && !movesWithDropdown)
            destination += Vector2.down * Mathf.Max(0f, alwaysVisibleProgressDrop);

        float blend = 1f - Mathf.Exp(-Mathf.Max(0.01f, progressMoveSpeed) * Time.unscaledDeltaTime);
        target.anchoredPosition = Vector2.Lerp(target.anchoredPosition, destination, blend);
    }


    [Header("References")]
    [SerializeField] private SelectionWheel selectionWheel;
    [SerializeField] private EndDaySystem endDaySystem;

    [Tooltip(
        "The entire dropdown panel that slides down from the top."
    )]
    [SerializeField]
    private RectTransform dropdownPanel;

    [Tooltip(
        "Invisible or visible area at the top of the screen " +
        "that opens the dropdown when hovered."
    )]
    [SerializeField]
    private RectTransform topHoverZone;

    // =========================================================
    // DAY / TIME
    // =========================================================

    [Header("Day / Time")]
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text timeText;

    [Tooltip(
        "Use AM / PM display instead of 24-hour time."
    )]
    [SerializeField]
    private bool use12HourClock = true;

    // =========================================================
    // DAY PROGRESS
    // =========================================================

    [Header("Day Progress")]

    [Tooltip(
        "Slider showing how much of the player's action phase has passed."
    )]
    [SerializeField]
    private Slider dayProgressBar;

    [Tooltip(
        "Optional text beside the progress bar."
    )]
    [SerializeField]
    private TMP_Text dayProgressText;

    [Tooltip(
        "Show the progress as a percentage."
    )]
    [SerializeField]
    private bool showProgressPercentage = false;

    [Tooltip(
        "Text shown while the player can still perform actions."
    )]
    [SerializeField]
    private string actionPhaseText = "ACTION TIME";

    [Tooltip(
        "Text shown once the action phase has finished."
    )]
    [SerializeField]
    private string actionPhaseFinishedText = "DAY FINISHED";

    // =========================================================
    // DAY PROGRESS CROSS FADE
    // =========================================================

    [Header("Day Progress Cross Fade")]

    [Tooltip(
        "CanvasGroup containing the entire Day Progress display. " +
        "Put the progress bar and its text under the same parent."
    )]
    [SerializeField]
    private CanvasGroup dayProgressCanvasGroup;

    [Tooltip(
        "Optional RectTransform used to slightly scale the Day Progress " +
        "display while it fades."
    )]
    [SerializeField]
    private RectTransform dayProgressVisual;

    [Tooltip(
        "How quickly the Day Progress display fades in and out."
    )]
    [SerializeField]
    private float progressFadeSpeed = 10f;

    [Tooltip(
        "How quickly the Day Progress display scales in and out."
    )]
    [SerializeField]
    private float progressScaleSpeed = 10f;

    [Tooltip(
        "Scale of the Day Progress display while hidden."
    )]
    [Range(0.5f, 1f)]
    [SerializeField]
    private float progressHiddenScale = 0.94f;

    [Tooltip(
        "Fade the progress display as soon as the player hovers " +
        "the top hover zone."
    )]
    [SerializeField]
    private bool fadeProgressImmediatelyOnHover = true;

    // =========================================================
    // END DAY ATTENTION
    // =========================================================

    [Header("End Day Attention")]

    [SerializeField] private bool forceDropdownOpenAtDayEnd = true;

    [Tooltip("Pulse the top day bar when the day has finished. No shaking or rotation is used.")]
    [SerializeField] private bool pulseAtDayEnd = true;

    [Tooltip("Visual that pulses at day end. If empty, Dropdown Panel is used.")]
    [SerializeField] private RectTransform dropdownWiggleVisual;

    [Range(1f, 1.3f)]
    [SerializeField] private float wigglePulseScale = 1.06f;

    [Min(0.01f)]
    [SerializeField] private float wiggleSpeed = 1.8f;

    [Min(0.01f)]
    [SerializeField] private float wiggleReturnSpeed = 12f;

    [Header("End Day Button Attention")]

    [SerializeField] private bool enlargeEndDayButtonNearDayEnd = true;

    [Tooltip("0.85 means the button begins growing during the final 15% of the day.")]
    [Range(0f, 1f)]
    [SerializeField] private float endDayButtonGrowStartProgress = 0.85f;

    [Tooltip("Final scale of the button when the day reaches 100%.")]
    [Range(1f, 3f)]
    [SerializeField] private float endDayButtonFinishedScale = 1.8f;

    [Tooltip("Extra scale added by the pulse after the day has finished.")]
    [Range(0f, 1f)]
    [SerializeField] private float endDayButtonPulseAmount = 0.22f;

    [Min(0.01f)]
    [SerializeField] private float endDayButtonPulseSpeed = 2.2f;

    [Min(0.01f)]
    [SerializeField] private float endDayButtonScaleSpeed = 10f;

    // =========================================================
    // WEATHER
    // =========================================================

    [Header("Weather")]
    [SerializeField] private Image weatherIcon;
    [SerializeField] private TMP_Text weatherText;

    [Header("Weather Icons")]
    [SerializeField] private Sprite sunnyIcon;
    [SerializeField] private Sprite rainIcon;
    [SerializeField] private Sprite thunderIcon;

    [Tooltip(
        "How often the weather icon checks for changes."
    )]
    [SerializeField]
    private float weatherRefreshInterval = 0.25f;

    // =========================================================
    // END DAY
    // =========================================================

    [Header("End Day")]
    [SerializeField] private Button endDayButton;

    // =========================================================
    // BUTTON AUDIO
    // =========================================================

    [Header("Dropdown Button Audio")]

    [Tooltip(
        "AudioSource used for dropdown button sounds. " +
        "If empty, one will be found or created automatically."
    )]
    [SerializeField]
    private AudioSource audioSource;

    [Tooltip(
        "Random sounds played when hovering over the End Day button."
    )]
    [SerializeField]
    private AudioClip[] buttonHoverSounds =
        new AudioClip[3];

    [Tooltip(
        "Random sounds played when clicking the End Day button."
    )]
    [SerializeField]
    private AudioClip[] buttonClickSounds =
        new AudioClip[3];

    [Range(0f, 1f)]
    [SerializeField]
    private float buttonHoverVolume = 0.6f;

    [Range(0f, 1f)]
    [SerializeField]
    private float buttonClickVolume = 0.8f;

    [Header("Button Audio Pitch")]
    [SerializeField] private float audioPitchMin = 0.95f;
    [SerializeField] private float audioPitchMax = 1.05f;

    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    [Header("Dropdown Behaviour")]

    [Tooltip(
        "Delay before opening after the mouse enters the top area."
    )]
    [SerializeField]
    private float openDelay = 0.05f;

    [Tooltip(
        "Delay before closing after the mouse leaves."
    )]
    [SerializeField]
    private float closeDelay = 0.20f;

    [Tooltip(
        "How quickly the dropdown slides."
    )]
    [SerializeField]
    private float slideSpeed = 15f;

    [Tooltip(
        "How far downward the dropdown travels when opened."
    )]
    [SerializeField]
    private float dropdownDistance = 150f;

    // =========================================================
    // DEFAULT DISPLAY
    // =========================================================

    [Header("Default Display")]
    [SerializeField] private int defaultDay = 1;
    [SerializeField] private int defaultHour = 6;
    [SerializeField] private int defaultMinute = 0;

    // =========================================================
    // PRIVATE
    // =========================================================

    private Vector2 hiddenPosition;
    private Vector2 shownPosition;

    private bool dropdownOpen;
    private bool positionsCached;

    private float openTimer;
    private float closeTimer;

    private float weatherTimer;

    private int displayedDay;
    private int displayedHour;
    private int displayedMinute;

    private EventTrigger endDayEventTrigger;
    private EventTrigger.Entry hoverEntry;

    private bool wasDayFinishedLastFrame;

    private RectTransform endDayButtonRect;
    private Vector3 endDayButtonBaseScale = Vector3.one;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (selectionWheel == null)
        {
            selectionWheel =
                FindFirstObjectByType<SelectionWheel>();
        }

        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }

        SetupAudioSource();

        if (endDayButton != null)
        {
            endDayButton.onClick.AddListener(
                HandleEndDayButton
            );

            SetupEndDayButtonHover();

            endDayButtonRect =
                endDayButton.transform as RectTransform;

            if (endDayButtonRect != null)
            {
                endDayButtonBaseScale =
                    endDayButtonRect.localScale;
            }
        }

        SetupProgressBar();

        SetupProgressCrossFade();

        SetupEndDayAttention();

        CacheDropdownPositions();

        if (dropdownPanel != null)
        {
            dropdownPanel.anchoredPosition =
                hiddenPosition;
        }

        dropdownOpen =
            false;

        displayedDay =
            defaultDay;

        displayedHour =
            defaultHour;

        displayedMinute =
            defaultMinute;

        if (endDaySystem != null)
        {
            displayedDay =
                endDaySystem.GetCurrentDay();

            displayedHour =
                endDaySystem.GetCurrentHour();

            displayedMinute =
                endDaySystem.GetCurrentMinute();
        }

        RefreshDayText();
        RefreshTimeText();
        UpdateWeatherDisplay();
        RefreshDayProgress();

        wasDayFinishedLastFrame =
            IsDayFinished();
    }

    // =========================================================
    // PROGRESS BAR SETUP
    // =========================================================

    private void SetupProgressBar()
    {
        if (dayProgressBar == null)
        {
            return;
        }

        dayProgressBar.minValue =
            0f;

        dayProgressBar.maxValue =
            1f;

        dayProgressBar.wholeNumbers =
            false;

        dayProgressBar.interactable =
            false;

        dayProgressBar.SetValueWithoutNotify(
            0f
        );
    }

    // =========================================================
    // PROGRESS CROSS FADE SETUP
    // =========================================================

    private void SetupProgressCrossFade()
    {
        if (dayProgressVisual == null &&
            dayProgressBar != null)
        {
            dayProgressVisual =
                dayProgressBar.transform.parent
                    as RectTransform;
        }

        if (dayProgressCanvasGroup == null &&
            dayProgressVisual != null)
        {
            dayProgressCanvasGroup =
                dayProgressVisual.GetComponent<CanvasGroup>();

            if (dayProgressCanvasGroup == null)
            {
                dayProgressCanvasGroup =
                    dayProgressVisual.gameObject
                        .AddComponent<CanvasGroup>();
            }
        }

        if (dayProgressCanvasGroup != null)
        {
            dayProgressCanvasGroup.alpha =
                1f;

            dayProgressCanvasGroup.interactable =
                false;

            dayProgressCanvasGroup.blocksRaycasts =
                false;
        }

        if (dayProgressVisual != null)
        {
            dayProgressVisual.localScale =
                Vector3.one;
        }
    }

    // =========================================================
    // END DAY ATTENTION SETUP
    // =========================================================

    private void SetupEndDayAttention()
    {
        if (dropdownWiggleVisual == null)
        {
            dropdownWiggleVisual =
                dropdownPanel;
        }

        ResetDropdownWiggleImmediately();
    }

    // =========================================================
    // AUDIO SOURCE
    // =========================================================

    private void SetupAudioSource()
    {
        if (audioSource == null)
        {
            audioSource =
                GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource =
                gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake =
            false;

        audioSource.loop =
            false;

        audioSource.spatialBlend =
            0f;
    }

    // =========================================================
    // END DAY HOVER SETUP
    // =========================================================

    private void SetupEndDayButtonHover()
    {
        if (endDayButton == null)
        {
            return;
        }

        endDayEventTrigger =
            endDayButton.GetComponent<EventTrigger>();

        if (endDayEventTrigger == null)
        {
            endDayEventTrigger =
                endDayButton.gameObject
                    .AddComponent<EventTrigger>();
        }

        if (endDayEventTrigger.triggers == null)
        {
            endDayEventTrigger.triggers =
                new List<EventTrigger.Entry>();
        }

        hoverEntry =
            new EventTrigger.Entry();

        hoverEntry.eventID =
            EventTriggerType.PointerEnter;

        hoverEntry.callback =
            new EventTrigger.TriggerEvent();

        hoverEntry.callback.AddListener(
            (data) =>
            {
                PlayButtonHoverSound();
            }
        );

        endDayEventTrigger.triggers.Add(
            hoverEntry
        );
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }

        if (selectionWheel == null)
        {
            selectionWheel =
                FindFirstObjectByType<SelectionWheel>();
        }

        UpdateDayAndTimeFromSystem();

        RefreshDayProgress();

        UpdateEndDayButtonAttention();

        UpdateWeatherTimer();

        bool dayFinished =
            IsDayFinished();

        // =====================================================
        // NEW DAY DETECTED
        // =====================================================

        if (wasDayFinishedLastFrame &&
            !dayFinished)
        {
            dropdownOpen =
                false;

            openTimer =
                0f;

            closeTimer =
                0f;

            ResetEndDayButtonAttentionImmediately();
        }

        wasDayFinishedLastFrame =
            dayFinished;

        // =====================================================
        // TRANSITION
        // =====================================================

        if (endDaySystem != null &&
            endDaySystem.IsTransitionRunning())
        {
            CloseDropdownImmediately();

            UpdateProgressCrossFade(
                false
            );

            UpdateDropdownWiggle(
                false
            );

            ResetEndDayButtonAttentionImmediately();

            return;
        }

        // The pause-menu preference controls the actual slide position.
        // Transition handling above still closes the bar while doors/reports run.
        if (endDaySystem != null && endDaySystem.GetShowTopDayBar())
        {

            dropdownOpen = true;
            openTimer = 0f;
            closeTimer = 0f;
            UpdateDropdownPosition();
            UpdateProgressCrossFade(false);
            UpdateDropdownWiggle(dayFinished && pulseAtDayEnd);
            return;
        }

        // =====================================================
        // FORCED END DAY MODE
        // =====================================================

        if (dayFinished)
        {
            UpdateEndDayAttentionState();

            return;
        }

        // =====================================================
        // NORMAL MODE LOCK
        // =====================================================

        if (!CanUseDropdown())
        {
            CloseDropdownImmediately();

            UpdateProgressCrossFade(
                false
            );

            UpdateDropdownWiggle(
                false
            );

            return;
        }

        // =====================================================
        // NORMAL HOVER BEHAVIOUR
        // =====================================================

        UpdateNormalDropdownBehaviour();

        UpdateDropdownWiggle(
            false
        );
    }

    // =========================================================
    // WEATHER TIMER
    // =========================================================

    private void UpdateWeatherTimer()
    {
        weatherTimer +=
            Time.unscaledDeltaTime;

        if (weatherTimer <
            weatherRefreshInterval)
        {
            return;
        }

        weatherTimer =
            0f;

        UpdateWeatherDisplay();
    }

    // =========================================================
    // IS DAY FINISHED
    // =========================================================

    private bool IsDayFinished()
    {
        if (endDaySystem == null)
        {
            return false;
        }

        return
            endDaySystem.HasReachedDayEnd();
    }

    // =========================================================
    // NORMAL DROPDOWN
    // =========================================================

    private void UpdateNormalDropdownBehaviour()
    {
        bool hoveringTop =
            IsPointerOverRect(
                topHoverZone
            );

        bool hoveringPanel =
            dropdownOpen &&
            IsPointerOverRect(
                dropdownPanel
            );

        bool shouldStayOpen =
            hoveringTop ||
            hoveringPanel;

        // -----------------------------------------------------
        // OPEN
        // -----------------------------------------------------

        if (!dropdownOpen &&
            hoveringTop)
        {
            openTimer +=
                Time.unscaledDeltaTime;

            if (openTimer >=
                openDelay)
            {
                OpenDropdown();
            }
        }
        else
        {
            openTimer =
                0f;
        }

        // -----------------------------------------------------
        // CLOSE
        // -----------------------------------------------------

        if (dropdownOpen &&
            !shouldStayOpen)
        {
            closeTimer +=
                Time.unscaledDeltaTime;

            if (closeTimer >=
                closeDelay)
            {
                CloseDropdown();
            }
        }
        else
        {
            closeTimer =
                0f;
        }

        // -----------------------------------------------------
        // SLIDE
        // -----------------------------------------------------

        UpdateDropdownPosition();

        // -----------------------------------------------------
        // PROGRESS VISIBILITY
        // -----------------------------------------------------

        bool hideProgress;

        if (fadeProgressImmediatelyOnHover)
        {
            hideProgress =
                hoveringTop ||
                dropdownOpen;
        }
        else
        {
            hideProgress =
                dropdownOpen;
        }

        UpdateProgressCrossFade(
            hideProgress
        );
    }

    // =========================================================
    // END DAY ATTENTION STATE
    // =========================================================

    private void UpdateEndDayAttentionState()
    {
        // -----------------------------------------------------
        // FORCE DROPDOWN OPEN
        // -----------------------------------------------------

        if (forceDropdownOpenAtDayEnd)
        {
            dropdownOpen =
                true;

            openTimer =
                0f;

            closeTimer =
                0f;
        }

        // -----------------------------------------------------
        // KEEP PROGRESS HIDDEN
        // -----------------------------------------------------

        UpdateProgressCrossFade(
            true
        );

        // -----------------------------------------------------
        // SLIDE DROPDOWN INTO VIEW
        // -----------------------------------------------------

        UpdateDropdownPosition();

        // -----------------------------------------------------
        // WIGGLE
        // -----------------------------------------------------

        UpdateDropdownWiggle(
            pulseAtDayEnd
        );
    }

    // =========================================================
    // END DAY WIGGLE
    // =========================================================

    private void UpdateDropdownWiggle(
        bool shouldPulse)
    {
        if (dropdownWiggleVisual == null)
        {
            return;
        }

        // Explicitly remove all rotation/shaking.
        dropdownWiggleVisual.localRotation =
            Quaternion.identity;

        float targetScale = 1f;

        if (shouldPulse)
        {
            float pulse =
                (Mathf.Sin(
                    Time.unscaledTime *
                    Mathf.Max(0.01f, wiggleSpeed) *
                    Mathf.PI * 2f
                ) + 1f) * 0.5f;

            targetScale =
                Mathf.Lerp(
                    1f,
                    wigglePulseScale,
                    pulse
                );
        }

        float smoothing =
            1f -
            Mathf.Exp(
                -Mathf.Max(0.01f, wiggleReturnSpeed) *
                Time.unscaledDeltaTime
            );

        dropdownWiggleVisual.localScale =
            Vector3.Lerp(
                dropdownWiggleVisual.localScale,
                Vector3.one * targetScale,
                smoothing
            );
    }

    // =========================================================
    // RESET WIGGLE
    // =========================================================

    private void ResetDropdownWiggleImmediately()
    {
        if (dropdownWiggleVisual == null)
        {
            return;
        }

        dropdownWiggleVisual.localRotation =
            Quaternion.identity;

        dropdownWiggleVisual.localScale =
            Vector3.one;
    }

    // =========================================================
    // END DAY BUTTON ATTENTION
    // =========================================================

    private void UpdateEndDayButtonAttention()
    {
        if (endDayButton == null)
        {
            return;
        }

        if (endDayButtonRect == null)
        {
            endDayButtonRect =
                endDayButton.transform as RectTransform;

            if (endDayButtonRect == null)
            {
                return;
            }

            endDayButtonBaseScale =
                endDayButtonRect.localScale;
        }

        float targetMultiplier = 1f;

        if (enlargeEndDayButtonNearDayEnd &&
            endDaySystem != null &&
            !endDaySystem.IsTransitionRunning())
        {
            float progress =
                Mathf.Clamp01(
                    endDaySystem.GetDayProgress()
                );

            float start =
                Mathf.Clamp01(
                    endDayButtonGrowStartProgress
                );

            if (progress >= start)
            {
                float growT =
                    Mathf.InverseLerp(
                        start,
                        1f,
                        progress
                    );

                growT =
                    growT * growT *
                    (3f - 2f * growT);

                targetMultiplier =
                    Mathf.Lerp(
                        1f,
                        endDayButtonFinishedScale,
                        growT
                    );
            }

            if (IsDayFinished())
            {
                float pulse =
                    (Mathf.Sin(
                        Time.unscaledTime *
                        Mathf.Max(
                            0.01f,
                            endDayButtonPulseSpeed
                        ) *
                        Mathf.PI * 2f
                    ) + 1f) * 0.5f;

                targetMultiplier =
                    endDayButtonFinishedScale +
                    pulse *
                    endDayButtonPulseAmount;
            }
        }

        float smoothing =
            1f -
            Mathf.Exp(
                -Mathf.Max(
                    0.01f,
                    endDayButtonScaleSpeed
                ) *
                Time.unscaledDeltaTime
            );

        endDayButtonRect.localScale =
            Vector3.Lerp(
                endDayButtonRect.localScale,
                endDayButtonBaseScale *
                targetMultiplier,
                smoothing
            );
    }

    private void ResetEndDayButtonAttentionImmediately()
    {
        if (endDayButtonRect != null)
        {
            endDayButtonRect.localScale =
                endDayButtonBaseScale;
        }
    }

    // =========================================================
    // PROGRESS CROSS FADE
    // =========================================================

    private void UpdateProgressCrossFade(
        bool hideProgress)
    {
        UpdateProgressPosition();
        float delta =
            Time.unscaledDeltaTime;

        // -----------------------------------------------------
        // ALPHA
        // -----------------------------------------------------

        if (dayProgressCanvasGroup != null)
        {
            float targetAlpha =
                hideProgress
                    ? 0f
                    : 1f;

            float fadeAmount =
                1f -
                Mathf.Exp(
                    -Mathf.Max(
                        0.01f,
                        progressFadeSpeed
                    ) *
                    delta
                );

            dayProgressCanvasGroup.alpha =
                Mathf.Lerp(
                    dayProgressCanvasGroup.alpha,
                    targetAlpha,
                    fadeAmount
                );

            dayProgressCanvasGroup.interactable =
                false;

            dayProgressCanvasGroup.blocksRaycasts =
                false;
        }

        // -----------------------------------------------------
        // SCALE
        // -----------------------------------------------------

        if (dayProgressVisual != null)
        {
            float targetScale =
                hideProgress
                    ? progressHiddenScale
                    : 1f;

            float scaleAmount =
                1f -
                Mathf.Exp(
                    -Mathf.Max(
                        0.01f,
                        progressScaleSpeed
                    ) *
                    delta
                );

            dayProgressVisual.localScale =
                Vector3.Lerp(
                    dayProgressVisual.localScale,
                    Vector3.one *
                    targetScale,
                    scaleAmount
                );
        }
    }

    // =========================================================
    // DAY PROGRESS
    // =========================================================

    private void RefreshDayProgress()
    {
        if (endDaySystem == null)
        {
            return;
        }

        UpdateDayProgress(
            endDaySystem.GetDayProgress()
        );
    }

    public void UpdateDayProgress(
        float progress)
    {
        progress =
            Mathf.Clamp01(
                progress
            );

        if (dayProgressBar != null)
        {
            dayProgressBar.SetValueWithoutNotify(
                progress
            );
        }

        if (dayProgressText == null)
        {
            return;
        }

        if (endDaySystem != null &&
            endDaySystem.HasReachedDayEnd())
        {
            dayProgressText.text =
                actionPhaseFinishedText;

            return;
        }

        if (showProgressPercentage)
        {
            dayProgressText.text =
                actionPhaseText +
                " " +
                Mathf.RoundToInt(
                    progress * 100f
                ) +
                "%";
        }
        else
        {
            dayProgressText.text =
                actionPhaseText;
        }
    }

    // =========================================================
    // CAN USE DROPDOWN
    // =========================================================

    private bool CanUseDropdown()
    {
        if (selectionWheel == null)
        {
            return true;
        }

        if (!selectionWheel.IsNormalMode())
        {
            return false;
        }

        if (selectionWheel.IsWheelOpen())
        {
            return false;
        }

        return true;
    }

    // =========================================================
    // UPDATE DAY / TIME
    // =========================================================

    private void UpdateDayAndTimeFromSystem()
    {
        if (endDaySystem == null)
        {
            return;
        }

        int newDay =
            endDaySystem.GetCurrentDay();

        int newHour =
            endDaySystem.GetCurrentHour();

        int newMinute =
            endDaySystem.GetCurrentMinute();

        if (newDay !=
            displayedDay)
        {
            displayedDay =
                newDay;

            RefreshDayText();
        }

        if (newHour !=
                displayedHour ||
            newMinute !=
                displayedMinute)
        {
            displayedHour =
                newHour;

            displayedMinute =
                newMinute;

            RefreshTimeText();
        }
    }

    // =========================================================
    // DAY
    // =========================================================

    public void SetDay(
        int day)
    {
        displayedDay =
            Mathf.Max(
                1,
                day
            );

        RefreshDayText();
    }

    private void RefreshDayText()
    {
        if (dayText == null)
        {
            return;
        }

        dayText.text =
            "DAY " +
            displayedDay;
    }

    // =========================================================
    // TIME
    // =========================================================

    public void SetTime(
        int hour,
        int minute)
    {
        displayedHour =
            Mathf.Clamp(
                hour,
                0,
                23
            );

        displayedMinute =
            Mathf.Clamp(
                minute,
                0,
                59
            );

        RefreshTimeText();
    }

    public void SetDayAndTime(
        int day,
        int hour,
        int minute)
    {
        SetDay(
            day
        );

        SetTime(
            hour,
            minute
        );
    }

    private void RefreshTimeText()
    {
        if (timeText == null)
        {
            return;
        }

        if (!use12HourClock)
        {
            timeText.text =
                displayedHour.ToString("00") +
                ":" +
                displayedMinute.ToString("00");

            return;
        }

        string period =
            displayedHour >= 12
                ? "PM"
                : "AM";

        int displayHour =
            displayedHour % 12;

        if (displayHour == 0)
        {
            displayHour =
                12;
        }

        timeText.text =
            displayHour +
            ":" +
            displayedMinute.ToString("00") +
            " " +
            period;
    }

    // =========================================================
    // WEATHER
    // =========================================================

    public void UpdateWeatherDisplay()
    {
        if (WeatherManager.Instance == null)
        {
            return;
        }

        string weather =
            WeatherManager.Instance
                .GetCurrentWeather()
                .ToString();

        if (weather ==
            "Sunny")
        {
            if (weatherIcon != null)
            {
                weatherIcon.sprite =
                    sunnyIcon;
            }

            if (weatherText != null)
            {
                weatherText.text =
                    "Sunny";
            }

            return;
        }

        if (weather ==
            "Rain")
        {
            if (weatherIcon != null)
            {
                weatherIcon.sprite =
                    rainIcon;
            }

            if (weatherText != null)
            {
                weatherText.text =
                    "Rain";
            }

            return;
        }

        if (weather ==
                "RainAndThunder" ||
            weather ==
                "Thunder" ||
            weather ==
                "Storm")
        {
            if (weatherIcon != null)
            {
                weatherIcon.sprite =
                    thunderIcon;
            }

            if (weatherText != null)
            {
                weatherText.text =
                    "Storm";
            }
        }
    }

    // =========================================================
    // END DAY BUTTON
    // =========================================================

    private void HandleEndDayButton()
    {
        if (Time.timeScale <= 0f || (!CanUseDropdown() && !IsDayFinished())) return;

        if (endDayButton == null ||
            !endDayButton.interactable)
        {
            return;
        }

        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }

        if (endDaySystem == null)
        {
            return;
        }

        if (endDaySystem.IsTransitionRunning())
        {
            return;
        }

        PlayButtonClickSound();

        // Stop the attention effects immediately.
        ResetDropdownWiggleImmediately();
        ResetEndDayButtonAttentionImmediately();

        dropdownOpen =
            false;

        openTimer =
            0f;

        closeTimer =
            0f;

        endDaySystem.EndDay();
    }

    // =========================================================
    // BUTTON HOVER AUDIO
    // =========================================================

    private void PlayButtonHoverSound()
    {
        if (endDayButton == null)
        {
            return;
        }

        if (!endDayButton.gameObject.activeInHierarchy)
        {
            return;
        }

        if (!endDayButton.interactable)
        {
            return;
        }

        PlayRandomSound(
            buttonHoverSounds,
            buttonHoverVolume
        );
    }

    // =========================================================
    // BUTTON CLICK AUDIO
    // =========================================================

    private void PlayButtonClickSound()
    {
        if (endDayButton == null ||
            !endDayButton.interactable)
        {
            return;
        }

        PlayRandomSound(
            buttonClickSounds,
            buttonClickVolume
        );
    }

    // =========================================================
    // RANDOM AUDIO
    // =========================================================

    private void PlayRandomSound(
        AudioClip[] clips,
        float volume)
    {
        if (audioSource == null)
        {
            SetupAudioSource();
        }

        if (audioSource == null ||
            clips == null ||
            clips.Length == 0)
        {
            return;
        }

        int validCount =
            0;

        for (int i = 0;
             i < clips.Length;
             i++)
        {
            if (clips[i] != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return;
        }

        int chosenValidIndex =
            Random.Range(
                0,
                validCount
            );

        AudioClip chosenClip =
            null;

        int currentValidIndex =
            0;

        for (int i = 0;
             i < clips.Length;
             i++)
        {
            if (clips[i] == null)
            {
                continue;
            }

            if (currentValidIndex ==
                chosenValidIndex)
            {
                chosenClip =
                    clips[i];

                break;
            }

            currentValidIndex++;
        }

        if (chosenClip == null)
        {
            return;
        }

        float oldPitch =
            audioSource.pitch;

        float minPitch =
            Mathf.Min(
                audioPitchMin,
                audioPitchMax
            );

        float maxPitch =
            Mathf.Max(
                audioPitchMin,
                audioPitchMax
            );

        audioSource.pitch =
            Random.Range(
                minPitch,
                maxPitch
            );

        audioSource.PlayOneShot(
            chosenClip,
            volume
        );

        audioSource.pitch =
            oldPitch;
    }

    // =========================================================
    // END DAY INTERACTABLE
    // =========================================================

    public void SetEndDayInteractable(
        bool interactable)
    {
        if (endDayButton == null)
        {
            return;
        }

        endDayButton.interactable =
            interactable;
    }

    // =========================================================
    // OPEN DROPDOWN
    // =========================================================

    public void OpenDropdown()
    {
        if (!CanUseDropdown() &&
            !IsDayFinished())
        {
            return;
        }

        dropdownOpen =
            true;

        openTimer =
            0f;

        closeTimer =
            0f;
    }

    // =========================================================
    // CLOSE DROPDOWN
    // =========================================================

    public void CloseDropdown()
    {
        // Once the day has finished, mouse/UI calls are not
        // allowed to close the dropdown. EndDaySystem can still
        // hide it during its transition through the immediate
        // close path.
        if (IsDayFinished() &&
            forceDropdownOpenAtDayEnd &&
            (endDaySystem == null ||
             !endDaySystem.IsTransitionRunning()))
        {
            dropdownOpen =
                true;

            return;
        }

        dropdownOpen =
            false;

        openTimer =
            0f;

        closeTimer =
            0f;
    }

    // =========================================================
    // CLOSE IMMEDIATELY
    // =========================================================

    private void CloseDropdownImmediately()
    {
        dropdownOpen =
            false;

        openTimer =
            0f;

        closeTimer =
            0f;

        if (dropdownPanel != null)
        {
            dropdownPanel.anchoredPosition =
                hiddenPosition;
        }

        ResetDropdownWiggleImmediately();
    }

    // =========================================================
    // IS OPEN
    // =========================================================

    public bool IsDropdownOpen()
    {
        return dropdownOpen;
    }

    // =========================================================
    // CACHE POSITIONS
    // =========================================================

    private void CacheDropdownPositions()
    {
        if (positionsCached ||
            dropdownPanel == null)
        {
            return;
        }

        hiddenPosition =
            dropdownPanel.anchoredPosition;

        shownPosition =
            hiddenPosition +
            new Vector2(
                0f,
                -dropdownDistance
            );

        positionsCached =
            true;
    }

    // =========================================================
    // MOVE DROPDOWN
    // =========================================================

    private void UpdateDropdownPosition()
    {
        if (dropdownPanel == null)
        {
            return;
        }

        if (!positionsCached)
        {
            CacheDropdownPositions();
        }

        Vector2 target =
            dropdownOpen
                ? shownPosition
                : hiddenPosition;

        float smoothing =
            1f -
            Mathf.Exp(
                -slideSpeed *
                Time.unscaledDeltaTime
            );

        dropdownPanel.anchoredPosition =
            Vector2.Lerp(
                dropdownPanel.anchoredPosition,
                target,
                smoothing
            );
    }

    // =========================================================
    // POINTER OVER RECT
    // =========================================================

    private bool IsPointerOverRect(
        RectTransform rect)
    {
        if (rect == null)
        {
            return false;
        }

        Canvas canvas =
            rect.GetComponentInParent<Canvas>();

        Camera uiCamera =
            null;

        if (canvas != null &&
            canvas.renderMode !=
            RenderMode.ScreenSpaceOverlay)
        {
            uiCamera =
                canvas.worldCamera;
        }

        return
            RectTransformUtility
                .RectangleContainsScreenPoint(
                    rect,
                    Input.mousePosition,
                    uiCamera
                );
    }

    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        if (endDayButton != null)
        {
            endDayButton.onClick.RemoveListener(
                HandleEndDayButton
            );
        }

        if (endDayEventTrigger != null &&
            hoverEntry != null &&
            endDayEventTrigger.triggers != null)
        {
            endDayEventTrigger.triggers.Remove(
                hoverEntry
            );
        }
    }
}



