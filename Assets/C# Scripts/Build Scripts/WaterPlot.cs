using UnityEngine;
using UnityEngine.Events;

public class WaterPlot : MonoBehaviour
{
    [Header("Destroyed plot / debris")]
    [SerializeField] private bool destroyed;
    [SerializeField] private Sprite debrisSprite;
    [SerializeField] private SpriteRenderer debrisRenderer;
    [SerializeField] private string destructionCause;
    [SerializeField] private int criticalSoilDays;
    private int soilCheckedDay = -1;
    public bool IsDestroyed() { return destroyed; }
    public string GetDestructionCause() { return destructionCause; }
    public bool DestroyWaterPlot(string cause)
    {
        if (destroyed) return false;
        destroyed = true; destructionCause = cause;
        waterQuality = 0f; waterState = WaterState.Destroyed; deteriorationDayProgress = 0;
        ApplyDebrisVisual();
        onWaterQualityChanged?.Invoke(); onWaterStateChanged?.Invoke();
        return true;
    }
    private void ApplyDebrisVisual()
    {
        if (debrisRenderer == null) debrisRenderer = waterRenderer;
        if (debrisRenderer == null) debrisRenderer = GetComponentInChildren<SpriteRenderer>();
        if (debrisRenderer == null) return;
        if (waterRenderer != null && waterRenderer != debrisRenderer) waterRenderer.enabled = false;
        debrisRenderer.enabled = true;
        if (debrisSprite != null) debrisRenderer.sprite = debrisSprite;
        debrisRenderer.color = debrisSprite != null ? Color.white : new Color(0.25f, 0.19f, 0.17f);
    }
    private void CheckSoilDamage()
    {
        if (destroyed || endDaySystem == null || endDaySystem.HasGameEnded()) return;
        int day = endDaySystem.GetCurrentDay();
        if (day < 1) return;
        if (soilCheckedDay < 1 || day < soilCheckedDay) { soilCheckedDay = day; criticalSoilDays = 0; return; }
        if (day == soilCheckedDay) return;
        soilCheckedDay = day;
        RangerStation station = FindFirstObjectByType<RangerStation>();
        if (station == null) return;
        PlotDisasterSystem settings = PlotDisasterSystem.GetOrCreate();
        criticalSoilDays = station.GetSoilHealth() <= settings.criticalSoilHealth ? criticalSoilDays + 1 : 0;
        if (criticalSoilDays >= Mathf.Max(1, settings.consecutiveCriticalDays)) DestroyWaterPlot("Critical soil health");
    }
    [ContextMenu("Debug - Destroy Water Plot")]
    private void DebugDestroyWaterPlot() { DestroyWaterPlot("Debug"); }


    // =========================================================
    // WATER STATE
    // =========================================================

    public enum WaterState
    {
        Clean,
        Dirty,
        Murky,

        // Legacy compatibility.
        Polluted = Murky,
        Destroyed = 3
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
        if (destroyed) return; CheckSoilDamage(); if (destroyed) return;
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

        if (lastProcessedDay < 0)
        {
            lastProcessedDay =
                currentDay;

            return;
        }

        if (currentDay <= lastProcessedDay)
        {
            return;
        }

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
    // ACTION PHASE
    // =========================================================

    public bool CanPerformPlayerAction()
    {
        if (destroyed) return false;
        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }

        if (endDaySystem == null)
        {
            return true;
        }

        return endDaySystem.IsActionPhaseActive();
    }

    // =========================================================
    // PROCESS NEW DAY
    // =========================================================

    private void ProcessNewDay()
    {
        if (destroyed) return;
        if (!deteriorateDaily)
        {
            return;
        }

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

        if (deteriorationDayProgress <
            daysPerDeterioration)
        {
            return;
        }

        deteriorationDayProgress = 0;

        // IMPORTANT:
        // Automatic deterioration bypasses the PLAYER action
        // lock because this is part of new-day simulation.
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

        if (weatherManager.IsRainAndThunder() ||
            weatherManager.IsThunder())
        {
            return deteriorateOnThunderDays;
        }

        if (weatherManager.IsRaining() ||
            weatherManager.IsRainOnly())
        {
            return deteriorateOnRainDays;
        }

        if (weatherManager.IsSunny())
        {
            return deteriorateOnSunnyDays;
        }

        return true;
    }

    // =========================================================
    // SET WATER QUALITY
    // =========================================================

    public void SetWaterQuality(
        float value)
    {
        if (destroyed) return;
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
    // =========================================================

    public void CleanWater(
        float amount)
    {
        // =====================================================
        // ACTION PHASE LOCK
        // =====================================================

        if (!CanPerformPlayerAction())
        {
            return;
        }

        if (amount <= 0f)
        {
            return;
        }

        SetWaterQuality(
            waterQuality +
            Mathf.Abs(amount)
        );

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
    // CLEAN WATER
    // =========================================================

    public void CleanWater()
    {
        if (!CanPerformPlayerAction())
        {
            return;
        }

        MakeCleanInternal();
    }

    // =========================================================
    // POLLUTE WATER
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
    // INTERNAL MAKE CLEAN
    // =========================================================

    private void MakeCleanInternal()
    {
        if (destroyed) return;
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
    // MAKE FULLY CLEAN
    // =========================================================

    public void MakeClean()
    {
        if (!CanPerformPlayerAction())
        {
            return;
        }

        MakeCleanInternal();
    }

    // =========================================================
    // MAKE FULLY POLLUTED
    // =========================================================

    public void MakePolluted()
    {
        if (destroyed) return;
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
        if (destroyed) { waterQuality = 0f; waterState = WaterState.Destroyed; return; }
        WaterState newState;

        if (waterQuality >=
            cleanQualityThreshold)
        {
            newState =
                WaterState.Clean;
        }
        else if (waterQuality <=
                 murkyQualityThreshold)
        {
            newState =
                WaterState.Murky;
        }
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
        if (newState == WaterState.Destroyed) { DestroyWaterPlot("Manual state change"); return; }
        if (destroyed) return;
        switch (newState)
        {
            case WaterState.Clean:

                MakeCleanInternal();

                break;

            case WaterState.Dirty:

                deteriorationDayProgress = 0;

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
        if (destroyed) return;
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
        if (!CanPerformPlayerAction())
        {
            return;
        }

        MakeCleanInternal();
    }

    // =========================================================
    // LEGACY PURIFY
    // =========================================================

    public void PurifyWater()
    {
        if (!CanPerformPlayerAction())
        {
            return;
        }

        MakeCleanInternal();
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
        if (destroyed) { ApplyDebrisVisual(); return; }
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
        if (destroyed) return 0f;
        return waterQuality;
    }

    public bool IsClean()
    {
        if (destroyed) return false;
        return
            waterState ==
            WaterState.Clean;
    }

    public bool IsDirty()
    {
        if (destroyed) return false;
        return
            waterState ==
            WaterState.Dirty;
    }

    public bool IsMurky()
    {
        if (destroyed) return false;
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
    // CAN CLEAN
    // =========================================================

    public bool CanClean()
    {
        // This now also tells the UI that cleaning is unavailable
        // once the action phase has ended.

        return
            waterState != WaterState.Clean &&
            CanPerformPlayerAction();
    }

    // =========================================================
    // CLEAN ONE STAGE
    // =========================================================

    public void CleanOneStage()
    {
        // =====================================================
        // ACTION PHASE LOCK
        // =====================================================

        if (!CanPerformPlayerAction())
        {
            return;
        }

        if (waterState == WaterState.Clean)
        {
            return;
        }

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

    // =========================================================
    // DEBUG
    // =========================================================

    [ContextMenu("Debug - Clean +10")]
    private void DebugCleanSmall()
    {
        CleanWater(
            10f
        );
    }

    [ContextMenu("Debug - Clean +25")]
    private void DebugCleanMedium()
    {
        CleanWater(
            25f
        );
    }

    [ContextMenu("Debug - Make Clean")]
    private void DebugMakeClean()
    {
        MakeClean();
    }

    [ContextMenu("Debug - Pollute -10")]
    private void DebugPolluteSmall()
    {
        PolluteWater(
            10f
        );
    }

    [ContextMenu("Debug - Pollute -25")]
    private void DebugPolluteMedium()
    {
        PolluteWater(
            25f
        );
    }

    [ContextMenu("Debug - Make Dirty")]
    private void DebugMakeDirty()
    {
        MakeDirty();
    }

    [ContextMenu("Debug - Make Murky")]
    private void DebugMakeMurky()
    {
        MakeMurky();
    }

    [ContextMenu("Debug - Make Polluted")]
    private void DebugMakePolluted()
    {
        MakePolluted();
    }

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
}
