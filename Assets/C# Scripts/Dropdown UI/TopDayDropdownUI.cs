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

    [Header("References")]
    [SerializeField] private SelectionWheel selectionWheel;
    [SerializeField] private EndDaySystem endDaySystem;

    [Tooltip("The entire dropdown panel that slides down from the top.")]
    [SerializeField] private RectTransform dropdownPanel;

    [Tooltip("Invisible or visible area at the top of the screen that opens the dropdown when hovered.")]
    [SerializeField] private RectTransform topHoverZone;

    // =========================================================
    // DAY / TIME TEXT
    // =========================================================

    [Header("Day / Time")]
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text timeText;

    [Tooltip("Use AM / PM display instead of 24-hour time.")]
    [SerializeField] private bool use12HourClock = true;

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

    [Tooltip("How often the weather icon checks for changes.")]
    [SerializeField] private float weatherRefreshInterval = 0.25f;

    // =========================================================
    // END DAY
    // =========================================================

    [Header("End Day")]
    [SerializeField] private Button endDayButton;

    // =========================================================
    // BUTTON AUDIO
    // =========================================================

    [Header("Dropdown Button Audio")]

    [Tooltip("AudioSource used for dropdown button sounds. If empty, one will be found or created automatically.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Random sounds played when hovering over the End Day button.")]
    [SerializeField] private AudioClip[] buttonHoverSounds = new AudioClip[3];

    [Tooltip("Random sounds played when clicking the End Day button.")]
    [SerializeField] private AudioClip[] buttonClickSounds = new AudioClip[3];

    [Range(0f, 1f)]
    [SerializeField] private float buttonHoverVolume = 0.6f;

    [Range(0f, 1f)]
    [SerializeField] private float buttonClickVolume = 0.8f;

    [Header("Button Audio Pitch")]
    [SerializeField] private float audioPitchMin = 0.95f;
    [SerializeField] private float audioPitchMax = 1.05f;

    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    [Header("Dropdown Behaviour")]

    [Tooltip("Delay before opening after the mouse enters the top area.")]
    [SerializeField] private float openDelay = 0.05f;

    [Tooltip("Delay before closing after the mouse leaves.")]
    [SerializeField] private float closeDelay = 0.20f;

    [Tooltip("How quickly the dropdown slides.")]
    [SerializeField] private float slideSpeed = 15f;

    [Tooltip("How far downward the dropdown travels when opened.")]
    [SerializeField] private float dropdownDistance = 150f;

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

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // -----------------------------------------------------
        // SELECTION WHEEL
        // -----------------------------------------------------

        if (selectionWheel == null)
        {
            selectionWheel =
                FindFirstObjectByType<SelectionWheel>();
        }

        // -----------------------------------------------------
        // END DAY SYSTEM
        // -----------------------------------------------------

        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }

        // -----------------------------------------------------
        // AUDIO
        // -----------------------------------------------------

        SetupAudioSource();

        // -----------------------------------------------------
        // END DAY BUTTON
        // -----------------------------------------------------

        if (endDayButton != null)
        {
            endDayButton.onClick.AddListener(
                HandleEndDayButton
            );

            SetupEndDayButtonHover();
        }

        // -----------------------------------------------------
        // POSITIONS
        // -----------------------------------------------------

        CacheDropdownPositions();

        if (dropdownPanel != null)
        {
            dropdownPanel.anchoredPosition =
                hiddenPosition;
        }

        dropdownOpen =
            false;

        // -----------------------------------------------------
        // DEFAULT DISPLAY
        // -----------------------------------------------------

        displayedDay =
            defaultDay;

        displayedHour =
            defaultHour;

        displayedMinute =
            defaultMinute;

        // -----------------------------------------------------
        // USE END DAY SYSTEM VALUES
        // -----------------------------------------------------

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
                endDayButton.gameObject.AddComponent<EventTrigger>();
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
        // -----------------------------------------------------
        // FIND SYSTEM IF NEEDED
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // DAY / TIME
        // -----------------------------------------------------

        UpdateDayAndTimeFromSystem();

        // -----------------------------------------------------
        // WEATHER
        // -----------------------------------------------------

        weatherTimer +=
            Time.unscaledDeltaTime;

        if (weatherTimer >=
            weatherRefreshInterval)
        {
            weatherTimer =
                0f;

            UpdateWeatherDisplay();
        }

        // -----------------------------------------------------
        // TRANSITION RUNNING
        // -----------------------------------------------------

        if (endDaySystem != null &&
            endDaySystem.IsTransitionRunning())
        {
            CloseDropdownImmediately();

            return;
        }

        // -----------------------------------------------------
        // NORMAL MODE ONLY
        // -----------------------------------------------------

        if (!CanUseDropdown())
        {
            CloseDropdownImmediately();

            return;
        }

        // -----------------------------------------------------
        // HOVER
        // -----------------------------------------------------

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
        // OPEN TIMER
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
        // CLOSE TIMER
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

        // -----------------------------------------------------
        // DAY CHANGED
        // -----------------------------------------------------

        if (newDay !=
            displayedDay)
        {
            displayedDay =
                newDay;

            RefreshDayText();
        }

        // -----------------------------------------------------
        // TIME CHANGED
        // -----------------------------------------------------

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

        // =====================================================
        // 24 HOUR
        // =====================================================

        if (!use12HourClock)
        {
            timeText.text =
                displayedHour.ToString("00") +
                ":" +
                displayedMinute.ToString("00");

            return;
        }

        // =====================================================
        // 12 HOUR
        // =====================================================

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

        // Using ToString keeps this compatible with the
        // existing WeatherManager enum without requiring
        // the enum type to be duplicated here.

        string weather =
            WeatherManager.Instance
                .GetCurrentWeather()
                .ToString();

        // =====================================================
        // SUNNY
        // =====================================================

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

        // =====================================================
        // RAIN
        // =====================================================

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

        // =====================================================
        // THUNDER STORM
        // =====================================================

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

            return;
        }
    }

    // =========================================================
    // END DAY BUTTON
    // =========================================================

    private void HandleEndDayButton()
    {
        // -----------------------------------------------------
        // BUTTON CLICK SFX
        // -----------------------------------------------------

        PlayButtonClickSound();

        // -----------------------------------------------------
        // FIND SYSTEM
        // -----------------------------------------------------

        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }

        if (endDaySystem == null)
        {
            return;
        }

        // -----------------------------------------------------
        // ALREADY TRANSITIONING
        // -----------------------------------------------------

        if (endDaySystem.IsTransitionRunning())
        {
            return;
        }

        // -----------------------------------------------------
        // CLOSE DROPDOWN
        // -----------------------------------------------------

        CloseDropdown();

        // -----------------------------------------------------
        // END DAY
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // COUNT VALID CLIPS
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // PICK RANDOM VALID CLIP
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // RANDOM PITCH
        // -----------------------------------------------------

        float oldPitch =
            audioSource.pitch;

        audioSource.pitch =
            Random.Range(
                audioPitchMin,
                audioPitchMax
            );

        // -----------------------------------------------------
        // PLAY
        // -----------------------------------------------------

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
        if (!CanUseDropdown())
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
            RectTransformUtility.RectangleContainsScreenPoint(
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
        // -----------------------------------------------------
        // BUTTON CLICK LISTENER
        // -----------------------------------------------------

        if (endDayButton != null)
        {
            endDayButton.onClick.RemoveListener(
                HandleEndDayButton
            );
        }

        // -----------------------------------------------------
        // HOVER LISTENER
        // -----------------------------------------------------

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