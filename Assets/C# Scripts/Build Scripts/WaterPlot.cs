using UnityEngine;
using UnityEngine.Events;

public class WaterPlot : MonoBehaviour
{
    // =========================================================
    // WATER STATE
    // =========================================================

    public enum WaterState
    {
        Clean,
        Dirty,
        Murky,

        // Legacy compatibility.
        Polluted = Murky
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    [Tooltip("Automatically finds the EndDaySystem if empty.")]
    [SerializeField]
    private EndDaySystem endDaySystem;

    [Tooltip("Automatically finds the WeatherManager if empty.")]
    [SerializeField]
    private WeatherManager weatherManager;

    // =========================================================
    // WATER QUALITY
    // =========================================================

    [Header("Water Quality")]

    [Tooltip(
        "Current water quality. 100 = perfectly clean, " +
        "0 = completely polluted."
    )]
    [Range(0f, 100f)]
    [SerializeField]
    private float waterQuality = 100f;

    [Tooltip(
        "Quality at or above this value is considered Clean."
    )]
    [Range(0f, 100f)]
    [SerializeField]
    private float cleanQualityThreshold = 70f;

    [Tooltip(
        "Quality at or below this value is considered Murky."
    )]
    [Range(0f, 100f)]
    [SerializeField]
    private float murkyQualityThreshold = 30f;

    // =========================================================
    // WATER STATE
    // =========================================================

    [Header("Water State")]

    [SerializeField]
    private WaterState waterState =
        WaterState.Clean;

    // =========================================================
    // DETERIORATION
    // =========================================================

    [Header("Water Deterioration")]

    [Tooltip(
        "How many days must pass before natural water " +
        "deterioration occurs. 2 = twice as long as before."
    )]
    [Min(1)]
    [SerializeField]
    private int daysPerDeterioration = 2;

    [Tooltip(
        "How much Water Quality is lost whenever the " +
        "deterioration timer completes."
    )]
    [Range(0f, 100f)]
    [SerializeField]
    private float deteriorationAmount = 25f;

    [Tooltip(
        "Enable automatic Water deterioration."
    )]
    [SerializeField]
    private bool deteriorateDaily = true;

    [SerializeField]
    private int deteriorationDayProgress = 0;

    // =========================================================
    // WEATHER
    // =========================================================

    [Header("Weather")]

    [Tooltip(
        "If enabled, weather determines whether a day counts " +
        "toward deterioration."
    )]
    [SerializeField]
    private bool weatherAffectsWater = true;

    [Tooltip("Sunny days count toward deterioration.")]
    [SerializeField]
    private bool deteriorateOnSunnyDays = true;

    [Tooltip("Rainy days count toward deterioration.")]
    [SerializeField]
    private bool deteriorateOnRainDays = true;

    [Tooltip("Thunder days count toward deterioration.")]
    [SerializeField]
    private bool deteriorateOnThunderDays = true;

    // =========================================================
    // VISUALS
    // =========================================================

    [Header("Visuals")]

    [SerializeField]
    private SpriteRenderer waterRenderer;

    [SerializeField]
    private Sprite cleanSprite;

    [SerializeField]
    private Sprite dirtySprite;

    [SerializeField]
    private Sprite murkySprite;

    // =========================================================
    // EVENTS
    // =========================================================

    [Header("Events")]

    public UnityEvent onWaterCleaned;

    public UnityEvent onWaterBecameDirty;

    public UnityEvent onWaterBecameMurky;

    public UnityEvent onWaterStateChanged;

    public UnityEvent onWaterQualityChanged;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLogs = false;

    [SerializeField]
    private int lastProcessedDay = -1;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        AutoAssignReferences();

        waterQuality =
            Mathf.Clamp(
                waterQuality,
                0f,
                100f
            );

        UpdateStateFromQuality(false);
        UpdateVisuals();
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        AutoAssignReferences();

        if (endDaySystem != null)
        {
            lastProcessedDay =
                endDaySystem.GetCurrentDay();
        }

        UpdateStateFromQuality(false);
        UpdateVisuals();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (endDaySystem == null ||
            weatherManager == null)
        {
            AutoAssignReferences();
        }

        if (endDaySystem == null)
        {
            return;
        }

        int currentDay =
            endDaySystem.GetCurrentDay();

        // =====================================================
        // INITIALISE
        // =====================================================

        if (lastProcessedDay < 0)
        {
            lastProcessedDay =
                currentDay;

            return;
        }

        // =====================================================
        // SAME DAY
        // =====================================================

        if (currentDay <= lastProcessedDay)
        {
            return;
        }

        // =====================================================
        // PROCESS EACH NEW DAY
        // =====================================================

        while (lastProcessedDay < currentDay)
        {
            lastProcessedDay++;

            ProcessNewDay();
        }
    }

    // =========================================================
    // AUTO ASSIGN
    // =========================================================

    private void AutoAssignReferences()
    {
        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }

        if (weatherManager == null)
        {
            weatherManager =
                FindFirstObjectByType<WeatherManager>();
        }

        if (waterRenderer == null)
        {
            waterRenderer =
                GetComponent<SpriteRenderer>();

            if (waterRenderer == null)
            {
                waterRenderer =
                    GetComponentInChildren<SpriteRenderer>();
            }
        }
    }

    // =========================================================
    // PROCESS NEW DAY
    // =========================================================

    private void ProcessNewDay()
    {
        if (!deteriorateDaily)
        {
            return;
        }

        // =====================================================
        // WEATHER CHECK
        // =====================================================

        if (weatherAffectsWater &&
            !ShouldWeatherDeteriorateWater())
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    "[WaterPlot] " +
                    gameObject.name +
                    " did not deteriorate today."
                );
            }

            return;
        }

        // =====================================================
        // ADD ONE DAY OF DETERIORATION PROGRESS
        // =====================================================

        deteriorationDayProgress++;

        if (showDebugLogs)
        {
            Debug.Log(
                "[WaterPlot] " +
                gameObject.name +
                " deterioration timer: " +
                deteriorationDayProgress +
                "/" +
                daysPerDeterioration
            );
        }

        // =====================================================
        // WAIT UNTIL ENOUGH DAYS HAVE PASSED
        // =====================================================

        if (deteriorationDayProgress <
            daysPerDeterioration)
        {
            return;
        }

        // =====================================================
        // DETERIORATE
        // =====================================================

        deteriorationDayProgress = 0;

        PolluteWater(
            deteriorationAmount
        );
    }

    // =========================================================
    // WEATHER CHECK
    // =========================================================

    private bool ShouldWeatherDeteriorateWater()
    {
        if (weatherManager == null)
        {
            return true;
        }

        // =====================================================
        // THUNDER
        // =====================================================

        if (weatherManager.IsRainAndThunder() ||
            weatherManager.IsThunder())
        {
            return
                deteriorateOnThunderDays;
        }

        // =====================================================
        // RAIN
        // =====================================================

        if (weatherManager.IsRaining() ||
            weatherManager.IsRainOnly())
        {
            return
                deteriorateOnRainDays;
        }

        // =====================================================
        // SUNNY
        // =====================================================

        if (weatherManager.IsSunny())
        {
            return
                deteriorateOnSunnyDays;
        }

        return true;
    }

    // =========================================================
    // SET WATER QUALITY
    // =========================================================

    public void SetWaterQuality(
        float value)
    {
        float oldQuality =
            waterQuality;

        waterQuality =
            Mathf.Clamp(
                value,
                0f,
                100f
            );

        UpdateStateFromQuality(true);

        if (!Mathf.Approximately(
                oldQuality,
                waterQuality))
        {
            onWaterQualityChanged?.Invoke();
        }

        if (showDebugLogs)
        {
            Debug.Log(
                "[WaterPlot] " +
                gameObject.name +
                " Quality: " +
                oldQuality.ToString("0.#") +
                " -> " +
                waterQuality.ToString("0.#")
            );
        }
    }

    // =========================================================
    // CLEAN WATER BY AMOUNT
    //
    // REQUIRED BY InteractiveWaterPlot
    // =========================================================

    public void CleanWater(
        float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        SetWaterQuality(
            waterQuality +
            Mathf.Abs(amount)
        );

        // -----------------------------------------------------
        // Cleaning gives the player a fresh deterioration timer.
        // -----------------------------------------------------

        deteriorationDayProgress = 0;

        if (showDebugLogs)
        {
            Debug.Log(
                "[WaterPlot] " +
                gameObject.name +
                " cleaned by " +
                amount.ToString("0.#") +
                ". Timer reset."
            );
        }
    }

    // =========================================================
    // CLEAN WATER - NO ARGUMENT
    // =========================================================

    public void CleanWater()
    {
        MakeClean();
    }

    // =========================================================
    // POLLUTE WATER BY AMOUNT
    //
    // REQUIRED BY InteractiveWaterPlot
    // =========================================================

    public void PolluteWater(
        float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        SetWaterQuality(
            waterQuality -
            Mathf.Abs(amount)
        );

        if (showDebugLogs)
        {
            Debug.Log(
                "[WaterPlot] " +
                gameObject.name +
                " polluted by " +
                amount.ToString("0.#")
            );
        }
    }

    // =========================================================
    // MAKE FULLY CLEAN
    //
    // REQUIRED BY InteractiveWaterPlot
    // =========================================================

    public void MakeClean()
    {
        deteriorationDayProgress = 0;

        SetWaterQuality(
            100f
        );

        if (showDebugLogs)
        {
            Debug.Log(
                "[WaterPlot] " +
                gameObject.name +
                " made fully CLEAN."
            );
        }
    }

    // =========================================================
    // MAKE FULLY POLLUTED
    //
    // REQUIRED BY InteractiveWaterPlot
    // =========================================================

    public void MakePolluted()
    {
        deteriorationDayProgress = 0;

        SetWaterQuality(
            0f
        );

        if (showDebugLogs)
        {
            Debug.Log(
                "[WaterPlot] " +
                gameObject.name +
                " made fully MURKY."
            );
        }
    }

    // =========================================================
    // STATE FROM QUALITY
    // =========================================================

    private void UpdateStateFromQuality(
        bool invokeEvents)
    {
        WaterState newState;

        // =====================================================
        // CLEAN
        // =====================================================

        if (waterQuality >=
            cleanQualityThreshold)
        {
            newState =
                WaterState.Clean;
        }

        // =====================================================
        // MURKY
        // =====================================================

        else if (waterQuality <=
                 murkyQualityThreshold)
        {
            newState =
                WaterState.Murky;
        }

        // =====================================================
        // DIRTY
        // =====================================================

        else
        {
            newState =
                WaterState.Dirty;
        }

        SetWaterStateInternal(
            newState,
            invokeEvents
        );
    }

    // =========================================================
    // SET WATER STATE
    // =========================================================

    public void SetWaterState(
        WaterState newState)
    {
        switch (newState)
        {
            case WaterState.Clean:

                MakeClean();

                break;

            case WaterState.Dirty:

                deteriorationDayProgress = 0;

                // Put the quality in the middle of the
                // Dirty range.
                SetWaterQuality(
                    (
                        cleanQualityThreshold +
                        murkyQualityThreshold
                    ) * 0.5f
                );

                break;

            case WaterState.Murky:

                MakePolluted();

                break;
        }
    }

    // =========================================================
    // INTERNAL STATE CHANGE
    // =========================================================

    private void SetWaterStateInternal(
        WaterState newState,
        bool invokeEvents)
    {
        WaterState oldState =
            waterState;

        waterState =
            newState;

        UpdateVisuals();

        if (oldState ==
            waterState)
        {
            return;
        }

        if (invokeEvents)
        {
            if (waterState ==
                WaterState.Clean)
            {
                onWaterCleaned?.Invoke();
            }
            else if (waterState ==
                     WaterState.Dirty)
            {
                onWaterBecameDirty?.Invoke();
            }
            else if (waterState ==
                     WaterState.Murky)
            {
                onWaterBecameMurky?.Invoke();
            }

            onWaterStateChanged?.Invoke();
        }

        if (showDebugLogs)
        {
            Debug.Log(
                "[WaterPlot] " +
                gameObject.name +
                " State: " +
                oldState +
                " -> " +
                waterState
            );
        }
    }

    // =========================================================
    // MAKE DIRTY
    // =========================================================

    public void MakeDirty()
    {
        deteriorationDayProgress = 0;

        float dirtyQuality =
            (
                cleanQualityThreshold +
                murkyQualityThreshold
            ) * 0.5f;

        SetWaterQuality(
            dirtyQuality
        );
    }

    // =========================================================
    // MAKE MURKY
    // =========================================================

    public void MakeMurky()
    {
        MakePolluted();
    }

    // =========================================================
    // LEGACY POLLUTE
    // =========================================================

    public void Pollute()
    {
        MakePolluted();
    }

    // =========================================================
    // LEGACY CLEAN
    // =========================================================

    public void Clean()
    {
        MakeClean();
    }

    // =========================================================
    // LEGACY PURIFY
    // =========================================================

    public void PurifyWater()
    {
        MakeClean();
    }

    // =========================================================
    // MANUAL DETERIORATION
    // =========================================================

    public void DeteriorateWater()
    {
        PolluteWater(
            deteriorationAmount
        );
    }

    // =========================================================
    // VISUALS
    // =========================================================

    private void UpdateVisuals()
    {
        if (waterRenderer == null)
        {
            return;
        }

        switch (waterState)
        {
            case WaterState.Clean:

                if (cleanSprite != null)
                {
                    waterRenderer.sprite =
                        cleanSprite;
                }

                break;

            case WaterState.Dirty:

                if (dirtySprite != null)
                {
                    waterRenderer.sprite =
                        dirtySprite;
                }

                break;

            case WaterState.Murky:

                if (murkySprite != null)
                {
                    waterRenderer.sprite =
                        murkySprite;
                }

                break;
        }
    }

    // =========================================================
    // GETTERS
    // =========================================================

    public WaterState GetWaterState()
    {
        return waterState;
    }

    public float GetWaterQuality()
    {
        return waterQuality;
    }

    public bool IsClean()
    {
        return
            waterState ==
            WaterState.Clean;
    }

    public bool IsDirty()
    {
        return
            waterState ==
            WaterState.Dirty;
    }

    public bool IsMurky()
    {
        return
            waterState ==
            WaterState.Murky;
    }

    public bool IsPolluted()
    {
        return IsMurky();
    }

    // =========================================================
    // DETERIORATION GETTERS
    // =========================================================

    public int GetDaysPerDeteriorationStage()
    {
        return daysPerDeterioration;
    }

    public int GetDaysSinceLastDeterioration()
    {
        return deteriorationDayProgress;
    }

    public float GetDeteriorationAmount()
    {
        return deteriorationAmount;
    }

    // =========================================================
    // DEBUG - CLEAN SMALL
    // =========================================================

    [ContextMenu("Debug - Clean +10")]
    private void DebugCleanSmall()
    {
        CleanWater(
            10f
        );
    }

    // =========================================================
    // DEBUG - CLEAN MEDIUM
    // =========================================================

    [ContextMenu("Debug - Clean +25")]
    private void DebugCleanMedium()
    {
        CleanWater(
            25f
        );
    }

    // =========================================================
    // DEBUG - MAKE CLEAN
    // =========================================================

    [ContextMenu("Debug - Make Clean")]
    private void DebugMakeClean()
    {
        MakeClean();
    }

    // =========================================================
    // DEBUG - POLLUTE SMALL
    // =========================================================

    [ContextMenu("Debug - Pollute -10")]
    private void DebugPolluteSmall()
    {
        PolluteWater(
            10f
        );
    }

    // =========================================================
    // DEBUG - POLLUTE MEDIUM
    // =========================================================

    [ContextMenu("Debug - Pollute -25")]
    private void DebugPolluteMedium()
    {
        PolluteWater(
            25f
        );
    }

    // =========================================================
    // DEBUG - MAKE DIRTY
    // =========================================================

    [ContextMenu("Debug - Make Dirty")]
    private void DebugMakeDirty()
    {
        MakeDirty();
    }

    // =========================================================
    // DEBUG - MAKE MURKY
    // =========================================================

    [ContextMenu("Debug - Make Murky")]
    private void DebugMakeMurky()
    {
        MakeMurky();
    }

    // =========================================================
    // DEBUG - MAKE POLLUTED
    // =========================================================

    [ContextMenu("Debug - Make Polluted")]
    private void DebugMakePolluted()
    {
        MakePolluted();
    }

    // =========================================================
    // DEBUG - ADD DETERIORATION DAY
    // =========================================================

    [ContextMenu("Debug - Add Deterioration Day")]
    private void DebugAddDeteriorationDay()
    {
        deteriorationDayProgress++;

        Debug.Log(
            "[WaterPlot] " +
            gameObject.name +
            " deterioration timer: " +
            deteriorationDayProgress +
            "/" +
            daysPerDeterioration
        );

        if (deteriorationDayProgress >=
            daysPerDeterioration)
        {
            deteriorationDayProgress = 0;

            PolluteWater(
                deteriorationAmount
            );
        }
    }

    // =========================================================
    // DEBUG - RESET TIMER
    // =========================================================

    [ContextMenu("Debug - Reset Deterioration Timer")]
    private void DebugResetDeteriorationTimer()
    {
        deteriorationDayProgress = 0;

        Debug.Log(
            "[WaterPlot] " +
            gameObject.name +
            " deterioration timer reset."
        );
    }

    // =========================================================
    // DEBUG - AUTO ASSIGN
    // =========================================================

    [ContextMenu("Debug - Auto Assign References")]
    private void DebugAutoAssignReferences()
    {
        AutoAssignReferences();

        Debug.Log(
            "[WaterPlot] AUTO ASSIGN" +

            "\nEnd Day System: " +
            (
                endDaySystem != null
                    ? endDaySystem.name
                    : "NOT FOUND"
            ) +

            "\nWeather Manager: " +
            (
                weatherManager != null
                    ? weatherManager.name
                    : "NOT FOUND"
            ) +

            "\nSprite Renderer: " +
            (
                waterRenderer != null
                    ? waterRenderer.name
                    : "NOT FOUND"
            )
        );
    }

    // =========================================================
    // CAN CLEAN
    //
    // Used by WaterPlotInspectionUI.
    // Clean water cannot be cleaned again.
    // =========================================================

    public bool CanClean()
    {
        return waterState != WaterState.Clean;
    }

    // =========================================================
    // CLEAN ONE STAGE
    //
    // Used by WaterPlotInspectionUI.
    //
    // MURKY -> DIRTY
    // DIRTY -> CLEAN
    // CLEAN -> stays CLEAN
    // =========================================================

    public void CleanOneStage()
    {
        // -----------------------------------------------------
        // Already clean
        // -----------------------------------------------------

        if (waterState == WaterState.Clean)
        {
            return;
        }

        // -----------------------------------------------------
        // Reset deterioration progress whenever the player
        // performs maintenance.
        // -----------------------------------------------------

        deteriorationDayProgress = 0;

        // =====================================================
        // MURKY -> DIRTY
        // =====================================================

        if (waterState == WaterState.Murky)
        {
            float dirtyQuality =
                (
                    cleanQualityThreshold +
                    murkyQualityThreshold
                ) * 0.5f;

            SetWaterQuality(
                dirtyQuality
            );

            if (showDebugLogs)
            {
                Debug.Log(
                    "[WaterPlot] " +
                    gameObject.name +
                    " cleaned one stage: MURKY -> DIRTY"
                );
            }

            return;
        }

        // =====================================================
        // DIRTY -> CLEAN
        // =====================================================

        if (waterState == WaterState.Dirty)
        {
            SetWaterQuality(
                100f
            );

            if (showDebugLogs)
            {
                Debug.Log(
                    "[WaterPlot] " +
                    gameObject.name +
                    " cleaned one stage: DIRTY -> CLEAN"
                );
            }
        }
    }
}