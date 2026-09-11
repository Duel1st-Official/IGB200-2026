using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TopDayDropdownUI : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    [Tooltip(
        "Your existing SelectionWheel. " +
        "The dropdown only works in Inspector Mode."
    )]
    [SerializeField]
    private SelectionWheel selectionWheel;

    [Tooltip(
        "The RectTransform containing the actual dropdown panel."
    )]
    [SerializeField]
    private RectTransform dropdownPanel;

    [Tooltip(
        "Invisible UI area at the very top of the screen. " +
        "Hovering over this opens the dropdown."
    )]
    [SerializeField]
    private RectTransform topHoverZone;

    // =========================================================
    // TEXT
    // =========================================================

    [Header("Day / Time Text")]

    [SerializeField]
    private TMP_Text dayText;

    [SerializeField]
    private TMP_Text timeText;

    // =========================================================
    // WEATHER
    // =========================================================

    [Header("Weather")]

    [Tooltip(
        "UI Image used to display the current weather icon."
    )]
    [SerializeField]
    private Image weatherIcon;

    [Tooltip(
        "Optional text showing the current weather name."
    )]
    [SerializeField]
    private TMP_Text weatherText;

    [Tooltip(
        "Icon shown during Sunny weather."
    )]
    [SerializeField]
    private Sprite sunnyIcon;

    [Tooltip(
        "Icon shown during Rain weather."
    )]
    [SerializeField]
    private Sprite rainIcon;

    [Tooltip(
        "Icon shown during Rain + Thunder weather."
    )]
    [SerializeField]
    private Sprite thunderIcon;

    [Tooltip(
        "How often the dropdown checks the WeatherManager."
    )]
    [SerializeField]
    private float weatherRefreshInterval = 0.25f;

    // =========================================================
    // END DAY
    // =========================================================

    [Header("End Day")]

    [SerializeField]
    private Button endDayButton;

    [Tooltip(
        "Called when the End Day button is pressed."
    )]
    [SerializeField]
    private UnityEvent onEndDayPressed;

    // =========================================================
    // DEFAULT DISPLAY
    // =========================================================

    [Header("Default Display")]

    [SerializeField]
    private int startingDay = 1;

    [SerializeField]
    private int startingHour = 6;

    [SerializeField]
    private int startingMinute = 0;

    [SerializeField]
    private bool use12HourClock = true;

    // =========================================================
    // DROPDOWN MOVEMENT
    // =========================================================

    [Header("Dropdown Movement")]

    [Tooltip(
        "How quickly the menu slides in and out."
    )]
    [SerializeField]
    private float slideSpeed = 12f;

    [Tooltip(
        "How far downward the dropdown travels when opened."
    )]
    [SerializeField]
    private float dropdownDistance = 100f;

    // =========================================================
    // HOVER BEHAVIOUR
    // =========================================================

    [Header("Hover Behaviour")]

    [Tooltip(
        "Small delay before the dropdown opens."
    )]
    [SerializeField]
    private float openDelay = 0.05f;

    [Tooltip(
        "Small delay before the dropdown closes."
    )]
    [SerializeField]
    private float closeDelay = 0.20f;

    // =========================================================
    // PRIVATE
    // =========================================================

    private Vector2 shownPosition;
    private Vector2 hiddenPosition;

    private bool dropdownWanted = false;
    private bool isVisible = false;

    private float hoverTimer = 0f;
    private float leaveTimer = 0f;

    private int currentDay;
    private int currentHour;
    private int currentMinute;

    private float nextWeatherRefreshTime = 0f;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // -----------------------------------------------------
        // AUTO FIND SELECTION WHEEL
        // -----------------------------------------------------

        if (selectionWheel == null)
        {
            selectionWheel =
                FindFirstObjectByType<SelectionWheel>();
        }

        // -----------------------------------------------------
        // BUTTON
        // -----------------------------------------------------

        if (endDayButton != null)
        {
            endDayButton.onClick.AddListener(
                HandleEndDayButton
            );
        }

        // -----------------------------------------------------
        // INITIAL DATE / TIME
        // -----------------------------------------------------

        currentDay =
            Mathf.Max(
                1,
                startingDay
            );

        currentHour =
            Mathf.Clamp(
                startingHour,
                0,
                23
            );

        currentMinute =
            Mathf.Clamp(
                startingMinute,
                0,
                59
            );

        RefreshDisplay();

        // -----------------------------------------------------
        // PANEL POSITIONS
        // -----------------------------------------------------

        if (dropdownPanel != null)
        {
            // Inspector position = hidden position.
            hiddenPosition =
                dropdownPanel.anchoredPosition;

            // User controls how far down the panel travels.
            shownPosition =
                hiddenPosition +
                new Vector2(
                    0f,
                    -dropdownDistance
                );

            dropdownPanel.anchoredPosition =
                hiddenPosition;
        }

        // -----------------------------------------------------
        // WEATHER
        // -----------------------------------------------------

        RefreshWeather();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // =====================================================
        // WEATHER UPDATE
        // =====================================================

        if (Time.unscaledTime >=
            nextWeatherRefreshTime)
        {
            RefreshWeather();

            nextWeatherRefreshTime =
                Time.unscaledTime +
                weatherRefreshInterval;
        }

        // =====================================================
        // ONLY ALLOW IN INSPECTOR MODE
        // =====================================================

        if (!CanUseDropdown())
        {
            ForceClose();

            AnimateDropdown();

            return;
        }

        // =====================================================
        // CHECK MOUSE
        // =====================================================

        bool mouseInTopZone =
            IsMouseInside(
                topHoverZone
            );

        bool mouseInDropdown =
            IsMouseInside(
                dropdownPanel
            );

        bool wantsOpen =
            mouseInTopZone ||
            mouseInDropdown;

        // =====================================================
        // OPEN
        // =====================================================

        if (wantsOpen)
        {
            leaveTimer = 0f;

            hoverTimer +=
                Time.unscaledDeltaTime;

            if (hoverTimer >=
                openDelay)
            {
                dropdownWanted =
                    true;
            }
        }

        // =====================================================
        // CLOSE
        // =====================================================

        else
        {
            hoverTimer = 0f;

            leaveTimer +=
                Time.unscaledDeltaTime;

            if (leaveTimer >=
                closeDelay)
            {
                dropdownWanted =
                    false;
            }
        }

        AnimateDropdown();
    }

    // =========================================================
    // CAN USE DROPDOWN
    // =========================================================

    private bool CanUseDropdown()
    {
        if (selectionWheel == null)
        {
            return false;
        }

        if (selectionWheel.IsWheelOpen())
        {
            return false;
        }

        if (!selectionWheel.IsInspectorMode())
        {
            return false;
        }

        return true;
    }

    // =========================================================
    // MOUSE INSIDE UI
    // =========================================================

    private bool IsMouseInside(
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
    // ANIMATE DROPDOWN
    // =========================================================

    private void AnimateDropdown()
    {
        if (dropdownPanel == null)
        {
            return;
        }

        Vector2 targetPosition =
            dropdownWanted
                ? shownPosition
                : hiddenPosition;

        dropdownPanel.anchoredPosition =
            Vector2.Lerp(
                dropdownPanel.anchoredPosition,
                targetPosition,
                Time.unscaledDeltaTime *
                slideSpeed
            );

        if (Vector2.Distance(
                dropdownPanel.anchoredPosition,
                targetPosition
            ) < 0.1f)
        {
            dropdownPanel.anchoredPosition =
                targetPosition;
        }

        isVisible =
            dropdownWanted;
    }

    // =========================================================
    // FORCE CLOSE
    // =========================================================

    private void ForceClose()
    {
        dropdownWanted =
            false;

        hoverTimer =
            0f;

        leaveTimer =
            0f;
    }

    // =========================================================
    // WEATHER
    // =========================================================

    private void RefreshWeather()
    {
        if (WeatherManager.Instance == null)
        {
            if (weatherText != null)
            {
                weatherText.text =
                    "Weather";
            }

            return;
        }

        WeatherManager.WeatherType currentWeather =
            WeatherManager.Instance
                .GetCurrentWeather();

        switch (currentWeather)
        {
            // =================================================
            // SUNNY
            // =================================================

            case WeatherManager.WeatherType.Sunny:

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

                break;

            // =================================================
            // RAIN
            // =================================================

            case WeatherManager.WeatherType.Rain:

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

                break;

            // =================================================
            // RAIN + THUNDER
            // =================================================

            case WeatherManager.WeatherType.RainAndThunder:

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

                break;
        }

        // Keep pixel icons from stretching strangely.

        if (weatherIcon != null)
        {
            weatherIcon.preserveAspect =
                true;
        }
    }

    // =========================================================
    // END DAY BUTTON
    // =========================================================

    private void HandleEndDayButton()
    {
        if (!CanUseDropdown())
        {
            return;
        }

        onEndDayPressed?.Invoke();
    }

    // =========================================================
    // DAY
    // =========================================================

    public void SetDay(
        int day)
    {
        currentDay =
            Mathf.Max(
                1,
                day
            );

        RefreshDayText();
    }

    // =========================================================
    // TIME
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

        RefreshTimeText();
    }

    // =========================================================
    // SET DAY + TIME
    // =========================================================

    public void SetDayAndTime(
        int day,
        int hour,
        int minute)
    {
        currentDay =
            Mathf.Max(
                1,
                day
            );

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

        RefreshDisplay();
    }

    // =========================================================
    // END DAY BUTTON STATE
    // =========================================================

    public void SetEndDayInteractable(
        bool interactable)
    {
        if (endDayButton != null)
        {
            endDayButton.interactable =
                interactable;
        }
    }

    // =========================================================
    // REFRESH DISPLAY
    // =========================================================

    private void RefreshDisplay()
    {
        RefreshDayText();
        RefreshTimeText();
        RefreshWeather();
    }

    // =========================================================
    // DAY TEXT
    // =========================================================

    private void RefreshDayText()
    {
        if (dayText == null)
        {
            return;
        }

        dayText.text =
            "DAY " +
            currentDay;
    }

    // =========================================================
    // TIME TEXT
    // =========================================================

    private void RefreshTimeText()
    {
        if (timeText == null)
        {
            return;
        }

        // =====================================================
        // 24 HOUR CLOCK
        // =====================================================

        if (!use12HourClock)
        {
            timeText.text =
                currentHour.ToString("00") +
                ":" +
                currentMinute.ToString("00");

            return;
        }

        // =====================================================
        // 12 HOUR CLOCK
        // =====================================================

        string suffix =
            currentHour >= 12
                ? "PM"
                : "AM";

        int displayHour =
            currentHour % 12;

        if (displayHour == 0)
        {
            displayHour =
                12;
        }

        timeText.text =
            displayHour.ToString("00") +
            ":" +
            currentMinute.ToString("00") +
            " " +
            suffix;
    }

    // =========================================================
    // PUBLIC WEATHER REFRESH
    // =========================================================

    public void UpdateWeatherDisplay()
    {
        RefreshWeather();
    }

    // =========================================================
    // PUBLIC OPEN / CLOSE
    // =========================================================

    public void OpenDropdown()
    {
        if (!CanUseDropdown())
        {
            return;
        }

        dropdownWanted =
            true;
    }

    public void CloseDropdown()
    {
        ForceClose();
    }

    public bool IsDropdownOpen()
    {
        return isVisible;
    }

    // =========================================================
    // DEBUG
    // =========================================================

    [ContextMenu("Debug - Open Dropdown")]
    private void DebugOpen()
    {
        dropdownWanted =
            true;
    }

    [ContextMenu("Debug - Close Dropdown")]
    private void DebugClose()
    {
        dropdownWanted =
            false;
    }

    [ContextMenu("Debug - Next Day")]
    private void DebugNextDay()
    {
        SetDay(
            currentDay + 1
        );
    }

    [ContextMenu("Debug - Add 1 Hour")]
    private void DebugAddHour()
    {
        int newHour =
            currentHour + 1;

        if (newHour >= 24)
        {
            newHour = 0;
        }

        SetTime(
            newHour,
            currentMinute
        );
    }

    [ContextMenu("Debug - Refresh Weather")]
    private void DebugRefreshWeather()
    {
        RefreshWeather();
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        if (endDayButton != null)
        {
            endDayButton.onClick.RemoveListener(
                HandleEndDayButton
            );
        }
    }
}