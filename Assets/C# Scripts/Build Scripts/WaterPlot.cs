using UnityEngine;

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

        // Legacy alias for older code.
        Polluted = Murky
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Used to detect when a new day begins.")]
    [SerializeField] private EndDaySystem endDaySystem;

    // =========================================================
    // WATER SPRITES
    // =========================================================

    [Header("Water Sprites")]

    [Tooltip("Sprite shown when the water is completely clean.")]
    [SerializeField] private Sprite cleanSprite;

    [Tooltip("Sprite shown when the water is dirty.")]
    [SerializeField] private Sprite dirtySprite;

    [Tooltip("Sprite shown when the water is at its dirtiest stage.")]
    [SerializeField] private Sprite murkySprite;

    // =========================================================
    // WATER STATE
    // =========================================================

    [Header("Water State")]
    [SerializeField] private WaterState currentState = WaterState.Clean;

    [Range(0f, 100f)]
    [SerializeField] private float quality = 100f;

    // =========================================================
    // STAGE QUALITY
    // =========================================================

    [Header("Stage Quality Values")]

    [Range(0f, 100f)]
    [SerializeField] private float cleanQuality = 100f;

    [Range(0f, 100f)]
    [SerializeField] private float dirtyQuality = 50f;

    [Range(0f, 100f)]
    [SerializeField] private float murkyQuality = 0f;

    // =========================================================
    // QUALITY THRESHOLDS
    // =========================================================

    [Header("Quality Thresholds")]

    [Tooltip("Quality at or below this becomes Dirty.")]
    [Range(0f, 100f)]
    [SerializeField] private float dirtyThreshold = 65f;

    [Tooltip("Quality at or below this becomes Murky.")]
    [Range(0f, 100f)]
    [SerializeField] private float murkyThreshold = 30f;

    // =========================================================
    // DAILY WEATHER DIRTINESS
    // =========================================================

    [Header("Daily Weather Dirtiness")]

    [Tooltip("Automatically make this Water Plot dirtier whenever a new day begins.")]
    [SerializeField] private bool dirtyEachNewDay = true;

    [Tooltip("How many dirtiness stages are added after a Sunny day.")]
    [Range(0, 2)]
    [SerializeField] private int sunnyDirtyStages = 1;

    [Tooltip("How many dirtiness stages are added after a Rain day.")]
    [Range(0, 2)]
    [SerializeField] private int rainDirtyStages = 1;

    [Tooltip("How many dirtiness stages are added after a thunderstorm day.")]
    [Range(0, 2)]
    [SerializeField] private int thunderDirtyStages = 2;

    // =========================================================
    // OPTIONAL OLD DEGRADATION
    // =========================================================

    [Header("Automatic Real-Time Degradation")]

    [Tooltip("Leave this OFF if dirtiness should mainly happen between days.")]
    [SerializeField] private bool degradeOverTime = false;

    [Tooltip("Water quality lost per second when real-time degradation is enabled.")]
    [SerializeField] private float degradationRate = 0.25f;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // =========================================================
    // PRIVATE
    // =========================================================

    private int lastProcessedDay = -1;

    // Weather from the CURRENT day.
    // When the day changes, this represents the day that just ended.
    private string recordedDayWeather = "Sunny";

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // -----------------------------------------------------
        // SPRITE RENDERER
        // -----------------------------------------------------

        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                spriteRenderer =
                    GetComponentInChildren<SpriteRenderer>();
            }
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
        // QUALITY
        // -----------------------------------------------------

        quality =
            Mathf.Clamp(
                quality,
                0f,
                100f
            );

        UpdateStateFromQuality();
        RefreshSprite();
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // -----------------------------------------------------
        // FIND END DAY SYSTEM
        // -----------------------------------------------------

        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }

        // -----------------------------------------------------
        // RECORD CURRENT DAY
        // -----------------------------------------------------

        if (endDaySystem != null)
        {
            lastProcessedDay =
                endDaySystem.GetCurrentDay();
        }

        // -----------------------------------------------------
        // RECORD TODAY'S WEATHER
        // -----------------------------------------------------

        recordedDayWeather =
            GetCurrentWeatherName();

        // -----------------------------------------------------
        // REFRESH STATE
        // -----------------------------------------------------

        UpdateStateFromQuality();
        RefreshSprite();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " started on Day " +
                lastProcessedDay +
                " | Weather: " +
                recordedDayWeather +
                " | Water: " +
                currentState
            );
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // -----------------------------------------------------
        // NEW DAY CHECK
        // -----------------------------------------------------

        CheckForNewDay();

        // -----------------------------------------------------
        // OPTIONAL REAL-TIME DEGRADATION
        // -----------------------------------------------------

        if (!degradeOverTime)
        {
            return;
        }

        if (quality <= 0f)
        {
            return;
        }

        PolluteWater(
            degradationRate *
            Time.deltaTime
        );
    }

    // =========================================================
    // CHECK NEW DAY
    // =========================================================

    private void CheckForNewDay()
    {
        if (!dirtyEachNewDay)
        {
            return;
        }

        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();

            if (endDaySystem == null)
            {
                return;
            }
        }

        int currentDay =
            endDaySystem.GetCurrentDay();

        // First-time safety.
        if (lastProcessedDay < 0)
        {
            lastProcessedDay =
                currentDay;

            recordedDayWeather =
                GetCurrentWeatherName();

            return;
        }

        // Still the same day.
        if (currentDay ==
            lastProcessedDay)
        {
            return;
        }

        // -----------------------------------------------------
        // A NEW DAY HAS STARTED
        //
        // recordedDayWeather contains the weather from the
        // day that just finished.
        // -----------------------------------------------------

        ApplyWeatherDirtiness(
            recordedDayWeather
        );

        // -----------------------------------------------------
        // SAVE NEW DAY
        // -----------------------------------------------------

        lastProcessedDay =
            currentDay;

        // -----------------------------------------------------
        // RECORD THE NEW DAY'S WEATHER
        //
        // EndDaySystem randomizes the new weather when the
        // new day begins. This becomes the weather remembered
        // for tomorrow.
        // -----------------------------------------------------

        recordedDayWeather =
            GetCurrentWeatherName();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " entered Day " +
                currentDay +
                " | Previous Weather: " +
                recordedDayWeather +
                " | Water State: " +
                currentState
            );
        }
    }

    // =========================================================
    // APPLY WEATHER DIRTINESS
    // =========================================================

    private void ApplyWeatherDirtiness(
        string previousWeather)
    {
        int stages =
            1;

        // -----------------------------------------------------
        // SUNNY
        // -----------------------------------------------------

        if (previousWeather ==
            "Sunny")
        {
            stages =
                sunnyDirtyStages;
        }

        // -----------------------------------------------------
        // RAIN
        // -----------------------------------------------------

        else if (previousWeather ==
                 "Rain")
        {
            stages =
                rainDirtyStages;
        }

        // -----------------------------------------------------
        // THUNDERSTORM
        // -----------------------------------------------------

        else if (previousWeather ==
                     "RainAndThunder" ||
                 previousWeather ==
                     "Thunder" ||
                 previousWeather ==
                     "Storm")
        {
            stages =
                thunderDirtyStages;
        }

        // -----------------------------------------------------
        // APPLY STAGES
        // -----------------------------------------------------

        DirtyByStages(
            stages
        );

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " received " +
                stages +
                " dirtiness stage(s) from yesterday's " +
                previousWeather +
                " weather."
            );
        }
    }

    // =========================================================
    // GET CURRENT WEATHER NAME
    // =========================================================

    private string GetCurrentWeatherName()
    {
        if (WeatherManager.Instance == null)
        {
            return "Sunny";
        }

        return
            WeatherManager.Instance
                .GetCurrentWeather()
                .ToString();
    }

    // =========================================================
    // DIRTY BY MULTIPLE STAGES
    // =========================================================

    public void DirtyByStages(
        int stages)
    {
        stages =
            Mathf.Clamp(
                stages,
                0,
                2
            );

        for (int i = 0;
             i < stages;
             i++)
        {
            DirtyOneStage();
        }
    }

    // =========================================================
    // CLEAN ONE STAGE
    // =========================================================

    /// <summary>
    /// Murky -> Dirty
    /// Dirty -> Clean
    /// Clean -> Clean
    /// </summary>
    public void CleanOneStage()
    {
        switch (currentState)
        {
            // =================================================
            // MURKY -> DIRTY
            // =================================================

            case WaterState.Murky:

                SetDirtyState();

                if (showDebugLogs)
                {
                    Debug.Log(
                        gameObject.name +
                        " cleaned from MURKY to DIRTY."
                    );
                }

                break;

            // =================================================
            // DIRTY -> CLEAN
            // =================================================

            case WaterState.Dirty:

                SetCleanState();

                if (showDebugLogs)
                {
                    Debug.Log(
                        gameObject.name +
                        " cleaned from DIRTY to CLEAN."
                    );
                }

                break;

            // =================================================
            // ALREADY CLEAN
            // =================================================

            case WaterState.Clean:

                if (showDebugLogs)
                {
                    Debug.Log(
                        gameObject.name +
                        " is already CLEAN."
                    );
                }

                break;
        }

        RefreshSprite();
    }

    // =========================================================
    // DIRTY ONE STAGE
    // =========================================================

    /// <summary>
    /// Clean -> Dirty
    /// Dirty -> Murky
    /// Murky -> Murky
    /// </summary>
    public void DirtyOneStage()
    {
        switch (currentState)
        {
            // =================================================
            // CLEAN -> DIRTY
            // =================================================

            case WaterState.Clean:

                SetDirtyState();

                break;

            // =================================================
            // DIRTY -> MURKY
            // =================================================

            case WaterState.Dirty:

                SetMurkyState();

                break;

            // =================================================
            // ALREADY MURKY
            // =================================================

            case WaterState.Murky:

                break;
        }

        RefreshSprite();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " dirtied one stage. State: " +
                currentState
            );
        }
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

        quality -=
            amount;

        quality =
            Mathf.Clamp(
                quality,
                0f,
                100f
            );

        UpdateStateFromQuality();
        RefreshSprite();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " water dirtied by " +
                amount +
                ". Quality: " +
                quality +
                " | State: " +
                currentState
            );
        }
    }

    // =========================================================
    // CLEAN WATER BY AMOUNT
    // =========================================================

    public void CleanWater(
        float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        quality +=
            amount;

        quality =
            Mathf.Clamp(
                quality,
                0f,
                100f
            );

        UpdateStateFromQuality();
        RefreshSprite();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " water quality increased by " +
                amount +
                ". Quality: " +
                quality +
                " | State: " +
                currentState
            );
        }
    }

    // =========================================================
    // SET WATER QUALITY
    // =========================================================

    public void SetWaterQuality(
        float newQuality)
    {
        quality =
            Mathf.Clamp(
                newQuality,
                0f,
                100f
            );

        UpdateStateFromQuality();
        RefreshSprite();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " water quality set to " +
                quality +
                ". State: " +
                currentState
            );
        }
    }

    // =========================================================
    // MAKE CLEAN
    // =========================================================

    public void MakeClean()
    {
        SetCleanState();
        RefreshSprite();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " water made CLEAN."
            );
        }
    }

    // =========================================================
    // MAKE DIRTY
    // =========================================================

    public void MakeDirty()
    {
        SetDirtyState();
        RefreshSprite();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " water made DIRTY."
            );
        }
    }

    // =========================================================
    // MAKE MURKY
    // =========================================================

    public void MakeMurky()
    {
        SetMurkyState();
        RefreshSprite();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " water made MURKY."
            );
        }
    }

    // =========================================================
    // LEGACY MAKE POLLUTED
    // =========================================================

    public void MakePolluted()
    {
        MakeMurky();
    }

    // =========================================================
    // INTERNAL CLEAN STATE
    // =========================================================

    private void SetCleanState()
    {
        currentState =
            WaterState.Clean;

        quality =
            Mathf.Clamp(
                cleanQuality,
                0f,
                100f
            );
    }

    // =========================================================
    // INTERNAL DIRTY STATE
    // =========================================================

    private void SetDirtyState()
    {
        currentState =
            WaterState.Dirty;

        quality =
            Mathf.Clamp(
                dirtyQuality,
                0f,
                100f
            );
    }

    // =========================================================
    // INTERNAL MURKY STATE
    // =========================================================

    private void SetMurkyState()
    {
        currentState =
            WaterState.Murky;

        quality =
            Mathf.Clamp(
                murkyQuality,
                0f,
                100f
            );
    }

    // =========================================================
    // UPDATE STATE FROM QUALITY
    // =========================================================

    private void UpdateStateFromQuality()
    {
        if (quality <=
            murkyThreshold)
        {
            currentState =
                WaterState.Murky;
        }
        else if (quality <=
                 dirtyThreshold)
        {
            currentState =
                WaterState.Dirty;
        }
        else
        {
            currentState =
                WaterState.Clean;
        }
    }

    // =========================================================
    // REFRESH SPRITE
    // =========================================================

    private void RefreshSprite()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        switch (currentState)
        {
            // =================================================
            // CLEAN
            // =================================================

            case WaterState.Clean:

                if (cleanSprite != null)
                {
                    spriteRenderer.sprite =
                        cleanSprite;
                }

                break;

            // =================================================
            // DIRTY
            // =================================================

            case WaterState.Dirty:

                if (dirtySprite != null)
                {
                    spriteRenderer.sprite =
                        dirtySprite;
                }

                break;

            // =================================================
            // MURKY
            // =================================================

            case WaterState.Murky:

                if (murkySprite != null)
                {
                    spriteRenderer.sprite =
                        murkySprite;
                }

                break;
        }
    }

    // =========================================================
    // GET QUALITY
    // =========================================================

    public float GetWaterQuality()
    {
        return quality;
    }

    // =========================================================
    // GET STATE
    // =========================================================

    public WaterState GetWaterState()
    {
        return currentState;
    }

    // =========================================================
    // STATE CHECKS
    // =========================================================

    public bool IsClean()
    {
        return
            currentState ==
            WaterState.Clean;
    }

    public bool IsDirty()
    {
        return
            currentState ==
            WaterState.Dirty;
    }

    public bool IsMurky()
    {
        return
            currentState ==
            WaterState.Murky;
    }

    // =========================================================
    // LEGACY CHECK
    // =========================================================

    public bool IsPolluted()
    {
        return IsMurky();
    }

    // =========================================================
    // CAN CLEAN
    // =========================================================

    public bool CanClean()
    {
        return !IsClean();
    }

    // =========================================================
    // BAT WATER VALUE
    // =========================================================

    public float GetBatWaterValue()
    {
        switch (currentState)
        {
            case WaterState.Clean:

                return 1f;

            case WaterState.Dirty:

                return 0.5f;

            case WaterState.Murky:

                return 0f;
        }

        return 0f;
    }

    // =========================================================
    // DAY / WEATHER DEBUG INFO
    // =========================================================

    public int GetLastProcessedDay()
    {
        return lastProcessedDay;
    }

    public string GetRecordedWeather()
    {
        return recordedDayWeather;
    }

    // =========================================================
    // DEBUG
    // =========================================================

    [ContextMenu("Debug - Make Clean")]
    private void DebugMakeClean()
    {
        MakeClean();
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

    [ContextMenu("Debug - Clean One Stage")]
    private void DebugCleanOneStage()
    {
        CleanOneStage();
    }

    [ContextMenu("Debug - Dirty One Stage")]
    private void DebugDirtyOneStage()
    {
        DirtyOneStage();
    }

    [ContextMenu("Debug - Dirty Two Stages")]
    private void DebugDirtyTwoStages()
    {
        DirtyByStages(
            2
        );
    }

    [ContextMenu("Debug - Apply Sunny Day")]
    private void DebugSunnyDay()
    {
        ApplyWeatherDirtiness(
            "Sunny"
        );
    }

    [ContextMenu("Debug - Apply Rain Day")]
    private void DebugRainDay()
    {
        ApplyWeatherDirtiness(
            "Rain"
        );
    }

    [ContextMenu("Debug - Apply Thunder Day")]
    private void DebugThunderDay()
    {
        ApplyWeatherDirtiness(
            "RainAndThunder"
        );
    }

    [ContextMenu("Debug - Pollute 25")]
    private void DebugPollute25()
    {
        PolluteWater(
            25f
        );
    }

    [ContextMenu("Debug - Clean 25")]
    private void DebugClean25()
    {
        CleanWater(
            25f
        );
    }
}