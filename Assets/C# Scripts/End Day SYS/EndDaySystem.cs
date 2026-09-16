using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class EndDaySystem : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    [SerializeField]
    private TopDayDropdownUI topDayDropdown;

    [Header("Night Report")]

    [Tooltip("System that rolls and applies the nightly event. Auto-found when empty.")]
    [SerializeField]
    private EndDayEventSystem endDayEventSystem;

    [Tooltip("UI that displays the rolled event and waits for Continue. Auto-found when empty.")]
    [SerializeField]
    private EndDayEventUI endDayEventUI;

    [Tooltip(
        "Parent GameObject containing the entire transition UI."
    )]
    [SerializeField]
    private GameObject transitionUI;

    [Tooltip(
        "CanvasGroup attached to the Day Transition parent."
    )]
    [SerializeField]
    private CanvasGroup transitionCanvasGroup;

    [Tooltip(
        "Left sliding door panel."
    )]
    [SerializeField]
    private RectTransform leftDoor;

    [Tooltip(
        "Right sliding door panel."
    )]
    [SerializeField]
    private RectTransform rightDoor;

    [Tooltip(
        "Text shown while the doors are closed."
    )]
    [SerializeField]
    private TMP_Text transitionDayText;

    // =========================================================
    // DAY SETTINGS
    // =========================================================

    [Header("Day Settings")]

    [SerializeField]
    private int startingDay = 1;

    // =========================================================
    // ACTION PHASE
    // =========================================================

    [Header("Action Phase")]

    [Tooltip(
        "How many REAL seconds the player has to perform actions " +
        "before the day reaches its end time.\n\n" +
        "300 = 5 real minutes.\n" +
        "180 = 3 real minutes.\n" +
        "60 = 1 real minute."
    )]
    [Min(1f)]
    [SerializeField]
    private float actionPhaseDurationSeconds = 300f;

    [Tooltip(
        "If enabled, the action phase automatically progresses."
    )]
    [SerializeField]
    private bool timeRunsAutomatically = true;

    // =========================================================
    // TIME SETTINGS
    // =========================================================

    [Header("Time Settings")]

    [Tooltip(
        "Hour every new day begins."
    )]
    [Range(0, 23)]
    [SerializeField]
    private int newDayHour = 8;

    [Tooltip(
        "Minute every new day begins."
    )]
    [Range(0, 59)]
    [SerializeField]
    private int newDayMinute = 0;

    [Tooltip(
        "Hour at which the action phase finishes."
    )]
    [Range(0, 23)]
    [SerializeField]
    private int dayEndHour = 17;

    [Tooltip(
        "Minute at which the action phase finishes."
    )]
    [Range(0, 59)]
    [SerializeField]
    private int dayEndMinute = 0;

    // =========================================================
    // WEATHER SETTINGS
    // =========================================================

    [Header("New Day Weather")]

    [Tooltip(
        "Choose a new random weather type whenever a new day begins."
    )]
    [SerializeField]
    private bool randomizeWeatherEachDay = true;

    // =========================================================
    // STARTUP TRANSITION
    // =========================================================

    [Header("Startup Transition")]

    [Tooltip(
        "Play the sliding door transition when the world first opens."
    )]
    [SerializeField]
    private bool playTransitionOnStart = true;

    [Tooltip(
        "Small delay before the startup transition begins."
    )]
    [SerializeField]
    private float startupDelay = 0.15f;

    [Tooltip(
        "Show the current DAY text when entering the world."
    )]
    [SerializeField]
    private bool showDayTextOnStart = true;

    // =========================================================
    // DOOR MOVEMENT
    // =========================================================

    [Header("Door Movement")]

    [SerializeField]
    private float closeDuration = 0.6f;

    [SerializeField]
    private float closedHoldDuration = 0.2f;

    [SerializeField]
    private float openDuration = 0.65f;

    [SerializeField]
    private float offscreenPadding = 40f;

    // =========================================================
    // DAY TEXT ANIMATION
    // =========================================================

    [Header("Day Text Animation")]

    [SerializeField]
    private float textPopDuration = 0.18f;

    [SerializeField]
    private float textOvershootScale = 1.15f;

    [SerializeField]
    private float textSettleDuration = 0.12f;

    [SerializeField]
    private float textHoldDuration = 0.65f;

    [SerializeField]
    private float textExitDuration = 0.15f;

    // =========================================================
    // EVENTS
    // =========================================================

    [Header("Day Events")]

    [SerializeField]
    private UnityEvent onDayEnded;

    [SerializeField]
    private UnityEvent onNewDayStarted;

    [SerializeField]
    private UnityEvent onTransitionFinished;

    [Header("Startup Events")]

    [SerializeField]
    private UnityEvent onStartupTransitionStarted;

    [SerializeField]
    private UnityEvent onStartupTransitionFinished;

    [Header("Time Events")]

    [Tooltip(
        "Invoked once when the action phase reaches the end of the day."
    )]
    [SerializeField]
    private UnityEvent onTimeReachedDayEnd;

    // =========================================================
    // PRIVATE - DAY / TIME
    // =========================================================

    private int currentDay;

    private int currentHour;
    private int currentMinute;

    private float actionPhaseTimer = 0f;

    private bool reachedDayEnd = false;

    // =========================================================
    // PRIVATE - TRANSITION
    // =========================================================

    private bool transitionRunning = false;

    private bool doorPositionsCached = false;

    private Vector2 leftClosedPosition;
    private Vector2 rightClosedPosition;

    private Vector2 leftOpenPosition;
    private Vector2 rightOpenPosition;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        AutoAssignNightReportReferences();

        if (topDayDropdown == null)
        {
            topDayDropdown =
                FindFirstObjectByType<TopDayDropdownUI>();
        }

        currentDay =
            Mathf.Max(
                1,
                startingDay
            );

        ResetTimeForNewDay();

        if (topDayDropdown != null)
        {
            topDayDropdown.UpdateWeatherDisplay();
        }

        HideTransitionUI();

        if (playTransitionOnStart)
        {
            StartCoroutine(
                StartupTransitionRoutine()
            );
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        UpdateGameTime();
    }

    // =========================================================
    // GAME TIME
    // =========================================================

    private void UpdateGameTime()
    {
        if (!timeRunsAutomatically)
        {
            return;
        }

        if (transitionRunning)
        {
            return;
        }

        if (reachedDayEnd)
        {
            return;
        }

        actionPhaseTimer +=
            Time.deltaTime;

        actionPhaseTimer =
            Mathf.Clamp(
                actionPhaseTimer,
                0f,
                GetSafeActionPhaseDuration()
            );

        UpdateClockFromActionProgress();

        if (actionPhaseTimer >=
            GetSafeActionPhaseDuration())
        {
            ReachDayEnd();
        }
    }

    // =========================================================
    // CLOCK FROM PROGRESS
    // =========================================================

    private void UpdateClockFromActionProgress()
    {
        float progress =
            GetDayProgress();

        int startMinutes =
            (newDayHour * 60) +
            newDayMinute;

        int endMinutes =
            (dayEndHour * 60) +
            dayEndMinute;

        if (endMinutes <= startMinutes)
        {
            endMinutes =
                startMinutes + 1;
        }

        int totalGameMinutes =
            endMinutes -
            startMinutes;

        int elapsedGameMinutes =
            Mathf.FloorToInt(
                totalGameMinutes *
                progress
            );

        int calculatedMinutes =
            startMinutes +
            elapsedGameMinutes;

        currentHour =
            calculatedMinutes / 60;

        currentMinute =
            calculatedMinutes % 60;

        if (progress >= 1f)
        {
            currentHour =
                dayEndHour;

            currentMinute =
                dayEndMinute;
        }

        UpdateDayAndTimeHUD();
    }

    // =========================================================
    // REACH DAY END
    // =========================================================

    private void ReachDayEnd()
    {
        if (reachedDayEnd)
        {
            return;
        }

        actionPhaseTimer =
            GetSafeActionPhaseDuration();

        currentHour =
            dayEndHour;

        currentMinute =
            dayEndMinute;

        reachedDayEnd =
            true;

        UpdateDayAndTimeHUD();

        onTimeReachedDayEnd?.Invoke();
    }

    // =========================================================
    // UPDATE HUD
    // =========================================================

    private void UpdateDayAndTimeHUD()
    {
        if (topDayDropdown == null)
        {
            return;
        }

        topDayDropdown.SetDayAndTime(
            currentDay,
            currentHour,
            currentMinute
        );

        topDayDropdown.UpdateDayProgress(
            GetDayProgress()
        );
    }

    // =========================================================
    // RESET NEW DAY TIME
    // =========================================================

    private void ResetTimeForNewDay()
    {
        currentHour =
            newDayHour;

        currentMinute =
            newDayMinute;

        actionPhaseTimer =
            0f;

        reachedDayEnd =
            false;

        UpdateDayAndTimeHUD();
    }

    // =========================================================
    // RANDOMIZE WEATHER
    // =========================================================

    private void RandomizeNewDayWeather()
    {
        if (!randomizeWeatherEachDay)
        {
            return;
        }

        if (WeatherManager.Instance == null)
        {
            return;
        }

        int randomWeather =
            Random.Range(
                0,
                3
            );

        switch (randomWeather)
        {
            case 0:
                WeatherManager.Instance.MakeSunny();
                break;

            case 1:
                WeatherManager.Instance.MakeRain();
                break;

            case 2:
                WeatherManager.Instance.MakeRainAndThunder();
                break;
        }

        if (topDayDropdown != null)
        {
            topDayDropdown.UpdateWeatherDisplay();
        }
    }

    // =========================================================
    // SHOW TRANSITION UI
    // =========================================================

    private void ShowTransitionUI()
    {
        if (transitionUI != null)
        {
            transitionUI.SetActive(
                true
            );
        }

        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.alpha =
                1f;

            transitionCanvasGroup.interactable =
                true;

            transitionCanvasGroup.blocksRaycasts =
                true;
        }
    }

    // =========================================================
    // HIDE TRANSITION UI
    // =========================================================

    private void HideTransitionUI()
    {
        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.alpha =
                0f;

            transitionCanvasGroup.interactable =
                false;

            transitionCanvasGroup.blocksRaycasts =
                false;
        }

        if (transitionUI != null)
        {
            transitionUI.SetActive(
                false
            );
        }
    }

    // =========================================================
    // CACHE DOOR POSITIONS
    // =========================================================

    private void CacheDoorPositions()
    {
        if (doorPositionsCached)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        if (leftDoor != null)
        {
            leftClosedPosition =
                leftDoor.anchoredPosition;

            float width =
                leftDoor.rect.width;

            leftOpenPosition =
                leftClosedPosition +
                new Vector2(
                    -(width + offscreenPadding),
                    0f
                );
        }

        if (rightDoor != null)
        {
            rightClosedPosition =
                rightDoor.anchoredPosition;

            float width =
                rightDoor.rect.width;

            rightOpenPosition =
                rightClosedPosition +
                new Vector2(
                    width + offscreenPadding,
                    0f
                );
        }

        doorPositionsCached =
            true;
    }

    // =========================================================
    // DOORS
    // =========================================================

    private void SetDoorsClosed()
    {
        if (leftDoor != null)
        {
            leftDoor.anchoredPosition =
                leftClosedPosition;
        }

        if (rightDoor != null)
        {
            rightDoor.anchoredPosition =
                rightClosedPosition;
        }
    }

    private void SetDoorsOpen()
    {
        if (leftDoor != null)
        {
            leftDoor.anchoredPosition =
                leftOpenPosition;
        }

        if (rightDoor != null)
        {
            rightDoor.anchoredPosition =
                rightOpenPosition;
        }
    }

    // =========================================================
    // RESET DAY TEXT
    // =========================================================

    private void ResetDayText()
    {
        if (transitionDayText == null)
        {
            return;
        }

        transitionDayText.gameObject.SetActive(
            false
        );

        transitionDayText.transform.localScale =
            Vector3.zero;
    }

    // =========================================================
    // STARTUP TRANSITION
    // =========================================================

    private IEnumerator StartupTransitionRoutine()
    {
        if (transitionRunning)
        {
            yield break;
        }

        transitionRunning =
            true;

        ShowTransitionUI();

        onStartupTransitionStarted?.Invoke();

        yield return null;

        Canvas.ForceUpdateCanvases();

        CacheDoorPositions();

        SetDoorsClosed();

        ResetDayText();

        if (startupDelay > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    startupDelay
                );
        }

        if (showDayTextOnStart)
        {
            yield return
                PlayDayTextAnimation();
        }

        yield return
            MoveDoors(
                leftClosedPosition,
                leftOpenPosition,
                rightClosedPosition,
                rightOpenPosition,
                openDuration
            );

        SetDoorsOpen();

        transitionRunning =
            false;

        onStartupTransitionFinished?.Invoke();

        HideTransitionUI();
    }

    // =========================================================
    // END DAY
    // =========================================================

    public void EndDay()
    {
        if (transitionRunning)
        {
            return;
        }

        StartCoroutine(
            EndDayRoutine()
        );
    }

    // =========================================================
    // END DAY ROUTINE
    // =========================================================

    private IEnumerator EndDayRoutine()
    {
        transitionRunning =
            true;

        if (topDayDropdown != null)
        {
            topDayDropdown.SetEndDayInteractable(
                false
            );

            topDayDropdown.CloseDropdown();
        }

        ShowTransitionUI();

        yield return null;

        Canvas.ForceUpdateCanvases();

        CacheDoorPositions();

        SetDoorsOpen();

        ResetDayText();

        yield return
            MoveDoors(
                leftOpenPosition,
                leftClosedPosition,
                rightOpenPosition,
                rightClosedPosition,
                closeDuration
            );

        SetDoorsClosed();

        // =====================================================
        // OLD DAY ENDS
        // =====================================================

        onDayEnded?.Invoke();

        // Keep the current day and closed doors until Continue finishes
        // closing the report. transitionRunning stays true throughout.
        yield return PlayNightReportRoutine();

        // =====================================================
        // NEXT DAY
        // =====================================================

        currentDay++;

        ResetTimeForNewDay();

        RandomizeNewDayWeather();

        onNewDayStarted?.Invoke();

        yield return
            PlayDayTextAnimation();

        if (closedHoldDuration > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    closedHoldDuration
                );
        }

        yield return
            MoveDoors(
                leftClosedPosition,
                leftOpenPosition,
                rightClosedPosition,
                rightOpenPosition,
                openDuration
            );

        SetDoorsOpen();

        if (topDayDropdown != null)
        {
            topDayDropdown.SetEndDayInteractable(
                true
            );
        }

        transitionRunning =
            false;

        onTransitionFinished?.Invoke();

        HideTransitionUI();
    }

    // =========================================================
    // DAY TEXT
    // =========================================================

    private void AutoAssignNightReportReferences()
    {
        if (endDayEventSystem == null)
        {
            endDayEventSystem =
                FindFirstObjectByType<EndDayEventSystem>(
                    FindObjectsInactive.Include
                );
        }

        if (endDayEventUI == null)
        {
            endDayEventUI =
                FindFirstObjectByType<EndDayEventUI>(
                    FindObjectsInactive.Include
                );
        }
    }

    private IEnumerator PlayNightReportRoutine()
    {
        AutoAssignNightReportReferences();

        if (endDayEventSystem == null || endDayEventUI == null)
        {
            Debug.LogWarning(
                "[EndDaySystem] Night Report skipped: EndDayEventSystem " +
                "or EndDayEventUI is missing. Continuing to the next day.",
                this
            );
            yield break;
        }

        // The UI starts its own animation coroutine, so its host must be active.
        // Keep the UI component on an active object and assign its panel as uiRoot.
        if (!endDayEventUI.gameObject.activeInHierarchy)
        {
            endDayEventUI.gameObject.SetActive(true);
        }

        // Awake may hide uiRoot on first activation, including the host itself.
        if (!endDayEventUI.gameObject.activeInHierarchy)
        {
            endDayEventUI.gameObject.SetActive(true);
        }

        if (!endDayEventUI.gameObject.activeInHierarchy)
        {
            Debug.LogWarning(
                "[EndDaySystem] Night Report has an inactive parent. " +
                "Continuing to the next day.",
                this
            );
            yield break;
        }

        // RollRandomEvent applies the effect; do not apply it a second time.
        endDayEventSystem.RollRandomEvent();
        endDayEventUI.ShowLastEvent();

        // IsOpen remains true during the closing animation. No timeout or
        // automatic dismissal: only the report's Continue flow closes it.
        while (endDayEventUI != null && endDayEventUI.IsOpen())
        {
            yield return null;
        }
    }

    private IEnumerator PlayDayTextAnimation()
    {
        if (transitionDayText == null)
        {
            yield break;
        }

        transitionDayText.text =
            "DAY " +
            currentDay;

        transitionDayText.gameObject.SetActive(
            true
        );

        transitionDayText.transform.localScale =
            Vector3.zero;

        yield return
            ScaleText(
                Vector3.zero,
                Vector3.one *
                textOvershootScale,
                textPopDuration
            );

        yield return
            ScaleText(
                Vector3.one *
                textOvershootScale,
                Vector3.one,
                textSettleDuration
            );

        if (textHoldDuration > 0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    textHoldDuration
                );
        }

        yield return
            ScaleText(
                Vector3.one,
                Vector3.zero,
                textExitDuration
            );

        transitionDayText.gameObject.SetActive(
            false
        );
    }

    // =========================================================
    // MOVE DOORS
    // =========================================================

    private IEnumerator MoveDoors(
        Vector2 leftStart,
        Vector2 leftTarget,
        Vector2 rightStart,
        Vector2 rightTarget,
        float duration)
    {
        if (duration <= 0f)
        {
            if (leftDoor != null)
            {
                leftDoor.anchoredPosition =
                    leftTarget;
            }

            if (rightDoor != null)
            {
                rightDoor.anchoredPosition =
                    rightTarget;
            }

            yield break;
        }

        float timer =
            0f;

        while (timer < duration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    timer /
                    duration
                );

            progress =
                EaseInOutCubic(
                    progress
                );

            if (leftDoor != null)
            {
                leftDoor.anchoredPosition =
                    Vector2.LerpUnclamped(
                        leftStart,
                        leftTarget,
                        progress
                    );
            }

            if (rightDoor != null)
            {
                rightDoor.anchoredPosition =
                    Vector2.LerpUnclamped(
                        rightStart,
                        rightTarget,
                        progress
                    );
            }

            yield return null;
        }

        if (leftDoor != null)
        {
            leftDoor.anchoredPosition =
                leftTarget;
        }

        if (rightDoor != null)
        {
            rightDoor.anchoredPosition =
                rightTarget;
        }
    }

    // =========================================================
    // SCALE TEXT
    // =========================================================

    private IEnumerator ScaleText(
        Vector3 startScale,
        Vector3 targetScale,
        float duration)
    {
        if (transitionDayText == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            transitionDayText.transform.localScale =
                targetScale;

            yield break;
        }

        float timer =
            0f;

        transitionDayText.transform.localScale =
            startScale;

        while (timer < duration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    timer /
                    duration
                );

            progress =
                EaseOutCubic(
                    progress
                );

            transitionDayText.transform.localScale =
                Vector3.LerpUnclamped(
                    startScale,
                    targetScale,
                    progress
                );

            yield return null;
        }

        transitionDayText.transform.localScale =
            targetScale;
    }

    // =========================================================
    // EASING
    // =========================================================

    private float EaseOutCubic(
        float t)
    {
        return
            1f -
            Mathf.Pow(
                1f - t,
                3f
            );
    }

    private float EaseInOutCubic(
        float t)
    {
        if (t < 0.5f)
        {
            return
                4f *
                t *
                t *
                t;
        }

        return
            1f -
            Mathf.Pow(
                -2f * t + 2f,
                3f
            ) /
            2f;
    }

    // =========================================================
    // PUBLIC - SET DAY
    // =========================================================

    public void SetDay(
        int day)
    {
        currentDay =
            Mathf.Max(
                1,
                day
            );

        UpdateDayAndTimeHUD();
    }

    // =========================================================
    // PUBLIC - SET TIME
    // =========================================================

    public void SetTime(
        int hour,
        int minute)
    {
        currentHour =
            Mathf.Clamp(
                hour,
                0,
                23
            );

        currentMinute =
            Mathf.Clamp(
                minute,
                0,
                59
            );

        int startMinutes =
            (newDayHour * 60) +
            newDayMinute;

        int endMinutes =
            (dayEndHour * 60) +
            dayEndMinute;

        int requestedMinutes =
            (currentHour * 60) +
            currentMinute;

        int totalMinutes =
            Mathf.Max(
                1,
                endMinutes -
                startMinutes
            );

        float progress =
            Mathf.InverseLerp(
                startMinutes,
                endMinutes,
                requestedMinutes
            );

        actionPhaseTimer =
            progress *
            GetSafeActionPhaseDuration();

        reachedDayEnd =
            requestedMinutes >=
            endMinutes;

        if (reachedDayEnd)
        {
            currentHour =
                dayEndHour;

            currentMinute =
                dayEndMinute;

            actionPhaseTimer =
                GetSafeActionPhaseDuration();
        }

        UpdateDayAndTimeHUD();
    }

    // =========================================================
    // PUBLIC - GET DAY
    // =========================================================

    public int GetCurrentDay()
    {
        return currentDay;
    }

    // =========================================================
    // PUBLIC - GET HOUR
    // =========================================================

    public int GetCurrentHour()
    {
        return currentHour;
    }

    // =========================================================
    // PUBLIC - GET MINUTE
    // =========================================================

    public int GetCurrentMinute()
    {
        return currentMinute;
    }

    // =========================================================
    // PUBLIC - DAY FINISHED
    // =========================================================

    public bool HasReachedDayEnd()
    {
        return reachedDayEnd;
    }

    // =========================================================
    // PUBLIC - ACTION PHASE
    // =========================================================

    public bool IsActionPhaseActive()
    {
        return
            !reachedDayEnd &&
            !transitionRunning;
    }

    public float GetDayProgress()
    {
        return
            Mathf.Clamp01(
                actionPhaseTimer /
                GetSafeActionPhaseDuration()
            );
    }

    public float GetDayProgressPercent()
    {
        return
            GetDayProgress() *
            100f;
    }

    public float GetRemainingDaySeconds()
    {
        return
            Mathf.Max(
                0f,
                GetSafeActionPhaseDuration() -
                actionPhaseTimer
            );
    }

    public float GetActionPhaseDuration()
    {
        return
            GetSafeActionPhaseDuration();
    }

    public int GetDayStartHour()
    {
        return newDayHour;
    }

    public int GetDayStartMinute()
    {
        return newDayMinute;
    }

    public int GetDayEndHour()
    {
        return dayEndHour;
    }

    public int GetDayEndMinute()
    {
        return dayEndMinute;
    }

    private float GetSafeActionPhaseDuration()
    {
        return
            Mathf.Max(
                1f,
                actionPhaseDurationSeconds
            );
    }

    // =========================================================
    // PUBLIC - TRANSITION
    // =========================================================

    public bool IsTransitionRunning()
    {
        return transitionRunning;
    }

    // =========================================================
    // DEBUG
    // =========================================================

    [ContextMenu("Debug - End Day")]
    private void DebugEndDay()
    {
        EndDay();
    }

    [ContextMenu("Debug - Set Time To 8 AM")]
    private void DebugSetMorning()
    {
        SetTime(
            newDayHour,
            newDayMinute
        );
    }

    [ContextMenu("Debug - Set Time To End")]
    private void DebugSetNight()
    {
        SetTime(
            dayEndHour,
            dayEndMinute
        );
    }

    [ContextMenu("Debug - Finish Action Phase")]
    private void DebugFinishActionPhase()
    {
        actionPhaseTimer =
            GetSafeActionPhaseDuration();

        UpdateClockFromActionProgress();

        ReachDayEnd();
    }

    [ContextMenu("Debug - Random Weather")]
    private void DebugRandomWeather()
    {
        RandomizeNewDayWeather();
    }

    [ContextMenu("Debug - Startup Transition")]
    private void DebugStartupTransition()
    {
        if (transitionRunning)
        {
            return;
        }

        StartCoroutine(
            StartupTransitionRoutine()
        );
    }
}
