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
        "Hour at which the clock stops."
    )]
    [Range(0, 23)]
    [SerializeField]
    private int dayEndHour = 20;

    [Tooltip(
        "Minute at which the clock stops."
    )]
    [Range(0, 59)]
    [SerializeField]
    private int dayEndMinute = 0;

    [Tooltip(
        "How many real-world seconds it takes for one in-game minute to pass.\n\n" +
        "1 = 1 real second per game minute.\n" +
        "0.5 = 2 game minutes per real second.\n" +
        "2 = 1 game minute every 2 real seconds."
    )]
    [Min(0.01f)]
    [SerializeField]
    private float realSecondsPerGameMinute = 1f;

    [Tooltip(
        "If true, game time automatically progresses."
    )]
    [SerializeField]
    private bool timeRunsAutomatically = true;

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
        "Invoked when the clock reaches the end of the day."
    )]
    [SerializeField]
    private UnityEvent onTimeReachedDayEnd;

    // =========================================================
    // PRIVATE - DAY / TIME
    // =========================================================

    private int currentDay;

    private int currentHour;
    private int currentMinute;

    private float timeTimer = 0f;

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
        // -----------------------------------------------------
        // FIND HUD
        // -----------------------------------------------------

        if (topDayDropdown == null)
        {
            topDayDropdown =
                FindFirstObjectByType<TopDayDropdownUI>();
        }

        // -----------------------------------------------------
        // DAY
        // -----------------------------------------------------

        currentDay =
            Mathf.Max(
                1,
                startingDay
            );

        // -----------------------------------------------------
        // START TIME AT 8AM
        // -----------------------------------------------------

        currentHour =
            newDayHour;

        currentMinute =
            newDayMinute;

        reachedDayEnd =
            false;

        timeTimer =
            0f;

        // -----------------------------------------------------
        // HUD
        // -----------------------------------------------------

        UpdateDayAndTimeHUD();

        if (topDayDropdown != null)
        {
            topDayDropdown.UpdateWeatherDisplay();
        }

        // -----------------------------------------------------
        // TRANSITION UI
        // -----------------------------------------------------

        HideTransitionUI();

        // -----------------------------------------------------
        // STARTUP TRANSITION
        // -----------------------------------------------------

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
        // Don't run time if disabled.

        if (!timeRunsAutomatically)
        {
            return;
        }

        // Don't move the clock during the day transition.

        if (transitionRunning)
        {
            return;
        }

        // Clock already reached 8PM.

        if (reachedDayEnd)
        {
            return;
        }

        // -----------------------------------------------------
        // TIMER
        // -----------------------------------------------------

        timeTimer +=
            Time.deltaTime;

        // -----------------------------------------------------
        // ADVANCE MINUTES
        // -----------------------------------------------------

        while (timeTimer >= realSecondsPerGameMinute)
        {
            timeTimer -=
                realSecondsPerGameMinute;

            AdvanceOneMinute();

            if (reachedDayEnd)
            {
                break;
            }
        }
    }

    // =========================================================
    // ADVANCE ONE MINUTE
    // =========================================================

    private void AdvanceOneMinute()
    {
        currentMinute++;

        // -----------------------------------------------------
        // NEXT HOUR
        // -----------------------------------------------------

        if (currentMinute >= 60)
        {
            currentMinute =
                0;

            currentHour++;
        }

        // -----------------------------------------------------
        // STOP AT 8PM
        // -----------------------------------------------------

        if (HasReachedEndTime())
        {
            currentHour =
                dayEndHour;

            currentMinute =
                dayEndMinute;

            reachedDayEnd =
                true;

            UpdateDayAndTimeHUD();

            onTimeReachedDayEnd?.Invoke();

            return;
        }

        // -----------------------------------------------------
        // UPDATE HUD
        // -----------------------------------------------------

        UpdateDayAndTimeHUD();
    }

    // =========================================================
    // HAS REACHED END TIME
    // =========================================================

    private bool HasReachedEndTime()
    {
        if (currentHour > dayEndHour)
        {
            return true;
        }

        if (
            currentHour == dayEndHour &&
            currentMinute >= dayEndMinute
        )
        {
            return true;
        }

        return false;
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

        timeTimer =
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

        // -----------------------------------------------------
        // 0 = SUNNY
        // 1 = RAIN
        // 2 = RAIN + THUNDER
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // REFRESH WEATHER HUD
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // LEFT
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // RIGHT
        // -----------------------------------------------------

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
    // DOORS CLOSED
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

    // =========================================================
    // DOORS OPEN
    // =========================================================

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

        // -----------------------------------------------------
        // SHOW UI
        // -----------------------------------------------------

        ShowTransitionUI();

        onStartupTransitionStarted?.Invoke();

        // Let Unity initialise the UI.

        yield return null;

        Canvas.ForceUpdateCanvases();

        // -----------------------------------------------------
        // CACHE DOORS
        // -----------------------------------------------------

        CacheDoorPositions();

        // -----------------------------------------------------
        // BEGIN CLOSED
        // -----------------------------------------------------

        SetDoorsClosed();

        ResetDayText();

        // -----------------------------------------------------
        // DELAY
        // -----------------------------------------------------

        if (startupDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(
                startupDelay
            );
        }

        // -----------------------------------------------------
        // DAY 1 TEXT
        // -----------------------------------------------------

        if (showDayTextOnStart)
        {
            yield return PlayDayTextAnimation();
        }

        // -----------------------------------------------------
        // OPEN
        // -----------------------------------------------------

        yield return MoveDoors(
            leftClosedPosition,
            leftOpenPosition,
            rightClosedPosition,
            rightOpenPosition,
            openDuration
        );

        SetDoorsOpen();

        // -----------------------------------------------------
        // FINISH
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // DROPDOWN
        // -----------------------------------------------------

        if (topDayDropdown != null)
        {
            topDayDropdown.SetEndDayInteractable(
                false
            );

            topDayDropdown.CloseDropdown();
        }

        // -----------------------------------------------------
        // SHOW UI
        // -----------------------------------------------------

        ShowTransitionUI();

        yield return null;

        Canvas.ForceUpdateCanvases();

        CacheDoorPositions();

        // -----------------------------------------------------
        // ALWAYS BEGIN OPEN
        // -----------------------------------------------------

        SetDoorsOpen();

        ResetDayText();

        // =====================================================
        // CLOSE DOORS
        // =====================================================

        yield return MoveDoors(
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

        // =====================================================
        // NEXT DAY
        // =====================================================

        currentDay++;

        // =====================================================
        // RESET TIME TO 8AM
        // =====================================================

        ResetTimeForNewDay();

        // =====================================================
        // RANDOM NEW WEATHER
        // =====================================================

        RandomizeNewDayWeather();

        // =====================================================
        // NEW DAY EVENT
        // =====================================================

        onNewDayStarted?.Invoke();

        // =====================================================
        // DAY TEXT
        // =====================================================

        yield return PlayDayTextAnimation();

        // =====================================================
        // CLOSED HOLD
        // =====================================================

        if (closedHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                closedHoldDuration
            );
        }

        // =====================================================
        // OPEN DOORS
        // =====================================================

        yield return MoveDoors(
            leftClosedPosition,
            leftOpenPosition,
            rightClosedPosition,
            rightOpenPosition,
            openDuration
        );

        SetDoorsOpen();

        // =====================================================
        // BUTTON
        // =====================================================

        if (topDayDropdown != null)
        {
            topDayDropdown.SetEndDayInteractable(
                true
            );
        }

        // =====================================================
        // FINISH
        // =====================================================

        transitionRunning =
            false;

        onTransitionFinished?.Invoke();

        HideTransitionUI();
    }

    // =========================================================
    // DAY TEXT
    // =========================================================

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

        // -----------------------------------------------------
        // POP
        // -----------------------------------------------------

        yield return ScaleText(
            Vector3.zero,
            Vector3.one *
            textOvershootScale,
            textPopDuration
        );

        // -----------------------------------------------------
        // SETTLE
        // -----------------------------------------------------

        yield return ScaleText(
            Vector3.one *
            textOvershootScale,
            Vector3.one,
            textSettleDuration
        );

        // -----------------------------------------------------
        // HOLD
        // -----------------------------------------------------

        if (textHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                textHoldDuration
            );
        }

        // -----------------------------------------------------
        // EXIT
        // -----------------------------------------------------

        yield return ScaleText(
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
                    timer / duration
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
                    timer / duration
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

        timeTimer =
            0f;

        reachedDayEnd =
            HasReachedEndTime();

        if (reachedDayEnd)
        {
            currentHour =
                dayEndHour;

            currentMinute =
                dayEndMinute;
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
            8,
            0
        );
    }

    [ContextMenu("Debug - Set Time To 8 PM")]
    private void DebugSetNight()
    {
        SetTime(
            20,
            0
        );
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