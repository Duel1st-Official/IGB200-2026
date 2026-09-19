using UnityEngine;

public class BatColony : MonoBehaviour
{
    // =========================================================
    // ENUMS
    // =========================================================

    public enum ColonyTrend
    {
        Growing,
        Stable,
        Dying
    }

    public enum ColonyCondition
    {
        Good,
        Poor,
        Danger
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    [Tooltip("Automatically finds the EndDaySystem if empty.")]
    [SerializeField]
    private EndDaySystem endDaySystem;

    [Tooltip("Automatically finds the RangerStation if empty.")]
    [SerializeField]
    private RangerStation rangerStation;

    // =========================================================
    // COLONY POPULATION
    // =========================================================

    [Header("Colony Population")]

    [Min(0)]
    [SerializeField]
    private int colonyPopulation = 20;

    [Tooltip("Population cap and win target.")]
    [Min(1)][SerializeField] private int maximumPopulation = 100;

    public int GetMaximumPopulation() { return Mathf.Max(1, maximumPopulation); }

    private int ClampPopulation(long value)
    {
        return (int)System.Math.Max(0L, System.Math.Min(GetMaximumPopulation(), value));
    }

    [SerializeField]
    private ColonyTrend populationTrend =
        ColonyTrend.Stable;

    // =========================================================
    // COLONY VALUES
    // =========================================================

    [Header("Colony Values")]

    [Tooltip("Colony Health starts at 50 by default.")]
    [Range(0f, 100f)]
    [SerializeField]
    private float batHealth = 50f;

    [Range(0f, 100f)]
    [SerializeField]
    private float batFood = 100f;

    [Range(0f, 100f)]
    [SerializeField]
    private float batWater = 100f;

    // =========================================================
    // DAILY FOOD CONSUMPTION
    // =========================================================

    [Header("Daily Food Consumption")]

    [Tooltip("Enable automatic Food consumption each day.")]
    [SerializeField]
    private bool consumeFoodDaily = true;

    [Tooltip("How much Food the colony consumes each day.")]
    [Min(0f)]
    [SerializeField]
    private float foodConsumedPerDay = 10f;

    // =========================================================
    // DAILY HEALTH
    // =========================================================

    [Header("Daily Health")]

    [Tooltip(
        "Enable the normal daily ecosystem Health calculation."
    )]
    [SerializeField]
    private bool calculateHealthDaily = true;

    [Tooltip(
        "Maximum Health the normal ecosystem calculation " +
        "can remove in one day."
    )]
    [Min(0f)]
    [SerializeField]
    private float maximumDailyHealthLoss = 4f;

    [Tooltip(
        "Maximum Health the normal ecosystem calculation " +
        "can restore in one day."
    )]
    [Min(0f)]
    [SerializeField]
    private float maximumDailyHealthGain = 8f;

    // =========================================================
    // FOOD -> HEALTH
    // =========================================================

    [Header("Health - Food")]

    [Tooltip(
        "Food at or above this value improves Health."
    )]
    [Range(0f, 100f)]
    [SerializeField]
    private float goodFoodThreshold = 70f;

    [Tooltip(
        "Health gained each day when Food is good."
    )]
    [Min(0f)]
    [SerializeField]
    private float goodFoodHealthGain = 3f;

    [Tooltip(
        "Food at or below this value damages Health."
    )]
    [Range(0f, 100f)]
    [SerializeField]
    private float lowFoodThreshold = 25f;

    [Tooltip(
        "Health lost each day when Food is low."
    )]
    [Min(0f)]
    [SerializeField]
    private float lowFoodHealthLoss = 2f;

    // =========================================================
    // WATER -> HEALTH
    // =========================================================

    [Header("Health - Water")]

    [Tooltip(
        "Water at or above this value improves Health."
    )]
    [Range(0f, 100f)]
    [SerializeField]
    private float goodWaterThreshold = 70f;

    [Tooltip(
        "Health gained each day when Water is good."
    )]
    [Min(0f)]
    [SerializeField]
    private float goodWaterHealthGain = 3f;

    [Tooltip(
        "Water at or below this value damages Health."
    )]
    [Range(0f, 100f)]
    [SerializeField]
    private float lowWaterThreshold = 20f;

    [Tooltip(
        "Health lost each day when Water is low. " +
        "This is intentionally gentle because Water Plot " +
        "quality can change frequently."
    )]
    [Min(0f)]
    [SerializeField]
    private float lowWaterHealthLoss = 1f;

    // =========================================================
    // PREDATOR PRESSURE -> HEALTH
    // =========================================================

    [Header("Health - Predator Pressure")]

    [Tooltip(
        "Predator Pressure at or below this value " +
        "improves colony Health."
    )]
    [Range(0f, 100f)]
    [SerializeField]
    private float safePredatorThreshold = 30f;

    [Tooltip(
        "Health gained each day when Predator Pressure is low."
    )]
    [Min(0f)]
    [SerializeField]
    private float safePredatorHealthGain = 2f;

    [Tooltip(
        "Predator Pressure at or above this value damages Health."
    )]
    [Range(0f, 100f)]
    [SerializeField]
    private float dangerousPredatorThreshold = 70f;

    [Tooltip(
        "Health lost each day when Predator Pressure is high."
    )]
    [Min(0f)]
    [SerializeField]
    private float dangerousPredatorHealthLoss = 1.5f;

    // =========================================================
    // FIRE RISK -> HEALTH
    // =========================================================

    [Header("Health - Fire Risk")]

    [Tooltip(
        "Fire Risk must reach this value before it damages Health."
    )]
    [Range(0f, 100f)]
    [SerializeField]
    private float dangerousFireRiskThreshold = 80f;

    [Tooltip(
        "Health lost each day when Fire Risk is dangerous."
    )]
    [Min(0f)]
    [SerializeField]
    private float dangerousFireHealthLoss = 1f;

    // =========================================================
    // CONDITION THRESHOLDS
    // =========================================================

    [Header("Condition Thresholds")]

    [Tooltip(
        "Values at or below this amount display as Danger."
    )]
    [Range(0f, 100f)]
    [SerializeField]
    private float dangerThreshold = 30f;

    [Tooltip(
        "Values at or below this amount display as Poor."
    )]
    [Range(0f, 100f)]
    [SerializeField]
    private float poorThreshold = 65f;

    // =========================================================
    // POPULATION HEALTH THRESHOLDS
    // =========================================================

    // Retained only for compatibility with existing serialized components.
    [SerializeField, HideInInspector] private float growingHealthThreshold = 75f;
    [SerializeField, HideInInspector] private float dyingHealthThreshold = 30f;

    [Header("Daily Population Change")]
    [Tooltip("Maximum bats gained at 100 health or lost at 0 health per day. 50 health is stable. Fractions accumulate across days.")]
    [Range(0f, 20f)]
    [SerializeField] private float maximumDailyPopulationChange = 5f;
    [SerializeField, HideInInspector] private double populationRemainder;

    private void CalculateDailyPopulation()
    {
        if (colonyPopulation <= 0)
        {
            populationRemainder = 0d;
            return;
        }

        double distance = (Mathf.Clamp(batHealth, 0f, 100f) - 50d) / 50d;
        if (double.IsNaN(distance)) return;
        if (distance == 0d)
        {
            populationRemainder = 0d;
            return;
        }

        // Old growth credit must not cause births after health falls below 50,
        // or old decline credit delay recovery after health improves.
        if (populationRemainder * distance < 0d) populationRemainder = 0d;
        double rate = maximumDailyPopulationChange;
        if (double.IsNaN(rate) || double.IsInfinity(rate)) rate = 5d;
        rate = System.Math.Max(0d, System.Math.Min(20d, rate));
        populationRemainder += rate * distance * System.Math.Abs(distance);

        int wholeBats = (int)System.Math.Floor(System.Math.Abs(populationRemainder) + 1e-9d);
        if (wholeBats == 0) return;
        int change = populationRemainder > 0d ? wholeBats : -wholeBats;
        long nextPopulation = (long)colonyPopulation + change;
        colonyPopulation = ClampPopulation(nextPopulation);
        populationRemainder -= change;
        if (System.Math.Abs(populationRemainder) < 1e-9d ||
            colonyPopulation == 0 || colonyPopulation == GetMaximumPopulation())
            populationRemainder = 0d;
    }

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLogs = false;

    [SerializeField]
    private int lastProcessedDay = -1;

    [SerializeField]
    private float lastDailyHealthChange = 0f;

    [SerializeField]
    private float lastFoodHealthChange = 0f;

    [SerializeField]
    private float lastWaterHealthChange = 0f;

    [SerializeField]
    private float lastPredatorHealthChange = 0f;

    [SerializeField]
    private float lastFireHealthChange = 0f;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        colonyPopulation = ClampPopulation(colonyPopulation);
        AutoAssignReferences();
        UpdatePopulationTrendFromHealth();
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        AutoAssignReferences();

        // -----------------------------------------------------
        // Do not process daily effects immediately.
        // Start processing when the NEXT day begins.
        // -----------------------------------------------------

        if (endDaySystem != null)
        {
            lastProcessedDay =
                endDaySystem.GetCurrentDay();

            if (showDebugLogs)
            {
                Debug.Log(
                    "[BatColony] Started on Day " +
                    lastProcessedDay +
                    ". Daily effects begin next day."
                );
            }
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

        if (rangerStation == null)
        {
            rangerStation =
                FindFirstObjectByType<RangerStation>();
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        ProcessPendingDays();
    }

    public void ProcessPendingDays()
    {
        if (endDaySystem != null && endDaySystem.HasGameEnded()) return;
        if (endDaySystem == null ||
            rangerStation == null)
        {
            AutoAssignReferences();
        }

        if (endDaySystem == null)
        {
            return;
        }

        int currentDay =
            endDaySystem.GetCurrentDay();

        // -----------------------------------------------------
        // Initialise day tracker if needed.
        // -----------------------------------------------------

        if (currentDay < 1) return;

        if (lastProcessedDay < 1)
        {
            lastProcessedDay =
                currentDay;

            return;
        }

        // -----------------------------------------------------
        // No new day.
        // -----------------------------------------------------

        if (currentDay <= lastProcessedDay)
        {
            return;
        }

        // -----------------------------------------------------
        // Process each new day.
        // -----------------------------------------------------

        while (lastProcessedDay < currentDay)
        {
            lastProcessedDay++;

            ProcessNewDay();
        }
    }

    // =========================================================
    // PROCESS NEW DAY
    // =========================================================

    private void ProcessNewDay()
    {
        if (showDebugLogs)
        {
            Debug.Log(
                "====================================" +
                "\n[BAT COLONY] DAY " +
                lastProcessedDay +
                "\n===================================="
            );
        }

        // =====================================================
        // 1. COLONY EATS
        // =====================================================

        if (consumeFoodDaily)
        {
            ConsumeDailyFood();
        }

        // =====================================================
        // 2. ECOSYSTEM AFFECTS HEALTH
        // =====================================================

        if (calculateHealthDaily)
        {
            CalculateDailyHealth();
        }

        // =====================================================
        // 3. APPLY POPULATION CHANGE USING THE UPDATED HEALTH
        // =====================================================

        CalculateDailyPopulation();
        UpdatePopulationTrendFromHealth();

        if (showDebugLogs)
        {
            DebugState();
        }

        // =====================================================
        // FUTURE:
        //
        // END-OF-DAY EVENTS CAN BE PROCESSED HERE LATER.
        //
        // Examples:
        // - Heatwave
        // - Bushfire
        // - Predator outbreak
        // - Disease
        // - Water contamination
        // - Successful conservation event
        //
        // These can apply additional difficulty separately
        // from the normal ecosystem Health calculation.
        // =====================================================
    }

    // =========================================================
    // FOOD CONSUMPTION
    // =========================================================

    private void ConsumeDailyFood()
    {
        if (foodConsumedPerDay <= 0f)
        {
            return;
        }

        float oldFood =
            batFood;

        RemoveBatFood(
            foodConsumedPerDay
        );

        if (showDebugLogs)
        {
            Debug.Log(
                "[BatColony] DAILY FOOD" +
                "\nConsumed: " +
                foodConsumedPerDay +
                "\nFood: " +
                oldFood +
                " -> " +
                batFood
            );
        }
    }

    // =========================================================
    // DAILY HEALTH
    // =========================================================

    private void CalculateDailyHealth()
    {
        // =====================================================
        // FOOD
        // =====================================================

        lastFoodHealthChange =
            CalculateFoodHealthChange();

        // =====================================================
        // WATER
        // =====================================================

        lastWaterHealthChange =
            CalculateWaterHealthChange();

        // =====================================================
        // PREDATORS
        // =====================================================

        lastPredatorHealthChange =
            CalculatePredatorHealthChange();

        // =====================================================
        // FIRE
        // =====================================================

        lastFireHealthChange =
            CalculateFireHealthChange();

        // =====================================================
        // RAW TOTAL
        // =====================================================

        float rawHealthChange =
            lastFoodHealthChange +
            lastWaterHealthChange +
            lastPredatorHealthChange +
            lastFireHealthChange;

        // =====================================================
        // SAFETY CAP
        //
        // Normal ecosystem conditions cannot change Health
        // faster than these limits.
        //
        // Future events can bypass this if desired.
        // =====================================================

        lastDailyHealthChange =
            Mathf.Clamp(
                rawHealthChange,
                -maximumDailyHealthLoss,
                maximumDailyHealthGain
            );

        float oldHealth =
            batHealth;

        AddBatHealth(
            lastDailyHealthChange
        );

        if (showDebugLogs)
        {
            Debug.Log(
                "[BatColony] DAILY HEALTH" +

                "\nFood: " +
                batFood +
                " | " +
                FormatSignedNumber(
                    lastFoodHealthChange
                ) +

                "\nWater: " +
                batWater +
                " | " +
                FormatSignedNumber(
                    lastWaterHealthChange
                ) +

                "\nPredators: " +
                GetPredatorPressureForDebug() +
                " | " +
                FormatSignedNumber(
                    lastPredatorHealthChange
                ) +

                "\nFire Risk: " +
                GetFireRiskForDebug() +
                " | " +
                FormatSignedNumber(
                    lastFireHealthChange
                ) +

                "\nRaw Change: " +
                FormatSignedNumber(
                    rawHealthChange
                ) +

                "\nFinal Change: " +
                FormatSignedNumber(
                    lastDailyHealthChange
                ) +

                "\nHealth: " +
                oldHealth +
                " -> " +
                batHealth
            );
        }
    }

    // =========================================================
    // FOOD -> HEALTH
    // =========================================================

    private float CalculateFoodHealthChange()
    {
        if (batFood >=
            goodFoodThreshold)
        {
            return
                Mathf.Abs(
                    goodFoodHealthGain
                );
        }

        if (batFood <=
            lowFoodThreshold)
        {
            return
                -Mathf.Abs(
                    lowFoodHealthLoss
                );
        }

        return 0f;
    }

    // =========================================================
    // WATER -> HEALTH
    // =========================================================

    private float CalculateWaterHealthChange()
    {
        if (batWater >=
            goodWaterThreshold)
        {
            return
                Mathf.Abs(
                    goodWaterHealthGain
                );
        }

        if (batWater <=
            lowWaterThreshold)
        {
            return
                -Mathf.Abs(
                    lowWaterHealthLoss
                );
        }

        return 0f;
    }

    // =========================================================
    // PREDATOR PRESSURE -> HEALTH
    // =========================================================

    private float CalculatePredatorHealthChange()
    {
        if (rangerStation == null)
        {
            return 0f;
        }

        float predatorPressure =
            rangerStation.GetPredatorPressure();

        if (predatorPressure <=
            safePredatorThreshold)
        {
            return
                Mathf.Abs(
                    safePredatorHealthGain
                );
        }

        if (predatorPressure >=
            dangerousPredatorThreshold)
        {
            return
                -Mathf.Abs(
                    dangerousPredatorHealthLoss
                );
        }

        return 0f;
    }

    // =========================================================
    // FIRE RISK -> HEALTH
    // =========================================================

    private float CalculateFireHealthChange()
    {
        if (rangerStation == null)
        {
            return 0f;
        }

        float fireRisk =
            rangerStation.GetFireRisk();

        if (fireRisk >=
            dangerousFireRiskThreshold)
        {
            return
                -Mathf.Abs(
                    dangerousFireHealthLoss
                );
        }

        return 0f;
    }

    // =========================================================
    // POPULATION TREND FROM HEALTH
    // =========================================================

    private void UpdatePopulationTrendFromHealth()
    {
        if (batHealth > 50f)
        {
            populationTrend =
                ColonyTrend.Growing;
        }
        else if (batHealth < 50f)
        {
            populationTrend =
                ColonyTrend.Dying;
        }
        else
        {
            populationTrend =
                ColonyTrend.Stable;
        }

        if (showDebugLogs)
        {
            Debug.Log(
                "[BatColony] Population Trend: " +
                GetPopulationTrendText() +
                " | Health: " +
                batHealth
            );
        }
    }

    // =========================================================
    // POPULATION
    // =========================================================

    public void SetPopulation(
        int amount)
    {
        populationRemainder = 0d;
        colonyPopulation = ClampPopulation(amount);

        DebugState();
    }

    public void AddPopulation(
        int amount)
    {
        colonyPopulation = ClampPopulation((long)colonyPopulation + amount);

        DebugState();
    }

    public void RemovePopulation(
        int amount)
    {
        colonyPopulation = ClampPopulation((long)colonyPopulation - amount);

        DebugState();
    }

    // =========================================================
    // POPULATION TREND
    // =========================================================

    public void SetPopulationTrend(
        ColonyTrend trend)
    {
        populationTrend =
            trend;

        DebugState();
    }

    // =========================================================
    // HEALTH
    // =========================================================

    public void SetBatHealth(
        float value)
    {
        batHealth =
            Mathf.Clamp(
                value,
                0f,
                100f
            );

        if ((batHealth - 50f) * populationRemainder <= 0d)
            populationRemainder = 0d;
        UpdatePopulationTrendFromHealth();
        DebugState();
    }

    public void AddBatHealth(
        float amount)
    {
        SetBatHealth(
            batHealth +
            amount
        );
    }

    public void RemoveBatHealth(
        float amount)
    {
        SetBatHealth(
            batHealth -
            amount
        );
    }

    // =========================================================
    // FOOD
    // =========================================================

    public void SetBatFood(
        float value)
    {
        batFood =
            Mathf.Clamp(
                value,
                0f,
                100f
            );

        DebugState();
    }

    public void AddBatFood(
        float amount)
    {
        SetBatFood(
            batFood +
            amount
        );
    }

    public void RemoveBatFood(
        float amount)
    {
        SetBatFood(
            batFood -
            amount
        );
    }

    // =========================================================
    // WATER
    // =========================================================

    public void SetBatWater(
        float value)
    {
        batWater =
            Mathf.Clamp(
                value,
                0f,
                100f
            );

        DebugState();
    }

    public void AddBatWater(
        float amount)
    {
        SetBatWater(
            batWater +
            amount
        );
    }

    public void RemoveBatWater(
        float amount)
    {
        SetBatWater(
            batWater -
            amount
        );
    }

    // =========================================================
    // GETTERS
    // =========================================================

    public int GetPopulation()
    {
        return colonyPopulation;
    }

    public ColonyTrend GetPopulationTrend()
    {
        return populationTrend;
    }

    public float GetBatHealth()
    {
        return batHealth;
    }

    public float GetBatFood()
    {
        return batFood;
    }

    public float GetBatWater()
    {
        return batWater;
    }

    // =========================================================
    // FOOD SYSTEM GETTERS
    // =========================================================

    public float GetFoodConsumedPerDay()
    {
        return foodConsumedPerDay;
    }

    public bool IsDailyFoodConsumptionEnabled()
    {
        return consumeFoodDaily;
    }

    // =========================================================
    // HEALTH SYSTEM GETTERS
    // =========================================================

    public float GetLastDailyHealthChange()
    {
        return lastDailyHealthChange;
    }

    public float GetLastFoodHealthChange()
    {
        return lastFoodHealthChange;
    }

    public float GetLastWaterHealthChange()
    {
        return lastWaterHealthChange;
    }

    public float GetLastPredatorHealthChange()
    {
        return lastPredatorHealthChange;
    }

    public float GetLastFireHealthChange()
    {
        return lastFireHealthChange;
    }

    // =========================================================
    // POPULATION THRESHOLD GETTERS
    // =========================================================

    public float GetGrowingHealthThreshold()
    {
        return 50f;
    }

    public float GetDyingHealthThreshold()
    {
        return 50f;
    }

    // =========================================================
    // CONDITIONS
    // =========================================================

    public ColonyCondition GetBatHealthCondition()
    {
        return
            GetConditionFromValue(
                batHealth
            );
    }

    public ColonyCondition GetBatFoodCondition()
    {
        return
            GetConditionFromValue(
                batFood
            );
    }

    public ColonyCondition GetBatWaterCondition()
    {
        return
            GetConditionFromValue(
                batWater
            );
    }

    private ColonyCondition GetConditionFromValue(
        float value)
    {
        if (value <=
            dangerThreshold)
        {
            return
                ColonyCondition.Danger;
        }

        if (value <=
            poorThreshold)
        {
            return
                ColonyCondition.Poor;
        }

        return
            ColonyCondition.Good;
    }

    // =========================================================
    // TEXT
    // =========================================================

    public string GetPopulationTrendText()
    {
        switch (populationTrend)
        {
            case ColonyTrend.Growing:
                return "GROWING";

            case ColonyTrend.Dying:
                return "DYING";

            default:
                return "STABLE";
        }
    }

    public string GetBatHealthConditionText()
    {
        return
            GetConditionText(
                GetBatHealthCondition()
            );
    }

    public string GetBatFoodConditionText()
    {
        return
            GetConditionText(
                GetBatFoodCondition()
            );
    }

    public string GetBatWaterConditionText()
    {
        return
            GetConditionText(
                GetBatWaterCondition()
            );
    }

    private string GetConditionText(
        ColonyCondition condition)
    {
        switch (condition)
        {
            case ColonyCondition.Danger:
                return "DANGER";

            case ColonyCondition.Poor:
                return "POOR";

            default:
                return "GOOD";
        }
    }

    // =========================================================
    // DEBUG HELPERS
    // =========================================================

    private string FormatSignedNumber(
        float value)
    {
        if (value > 0f)
        {
            return
                "+" +
                value.ToString("0.#");
        }

        return
            value.ToString("0.#");
    }

    private float GetPredatorPressureForDebug()
    {
        if (rangerStation == null)
        {
            return 0f;
        }

        return
            rangerStation.GetPredatorPressure();
    }

    private float GetFireRiskForDebug()
    {
        if (rangerStation == null)
        {
            return 0f;
        }

        return
            rangerStation.GetFireRisk();
    }

    private void DebugState()
    {
        if (!showDebugLogs)
        {
            return;
        }

        Debug.Log(
            "[BatColony]" +
            "\nPopulation: " +
            colonyPopulation +
            " (" +
            GetPopulationTrendText() +
            ")" +
            "\nHealth: " +
            batHealth +
            "\nFood: " +
            batFood +
            "\nWater: " +
            batWater
        );
    }

    // =========================================================
    // DEBUG
    // =========================================================

    [ContextMenu("Debug - Calculate Daily Health")]
    private void DebugCalculateDailyHealth()
    {
        AutoAssignReferences();

        CalculateDailyHealth();

        UpdatePopulationTrendFromHealth();
    }

    [ContextMenu("Debug - Process Daily Colony Effects")]
    private void DebugProcessDailyEffects()
    {
        AutoAssignReferences();

        ProcessNewDay();
    }

    [ContextMenu("Debug - Consume One Day Food")]
    private void DebugConsumeOneDayFood()
    {
        ConsumeDailyFood();
    }

    [ContextMenu("Debug - Set Health To 50")]
    private void DebugSetHealthTo50()
    {
        SetBatHealth(
            50f
        );

        UpdatePopulationTrendFromHealth();
    }

    [ContextMenu("Debug - Set Health To 75")]
    private void DebugSetHealthTo75()
    {
        SetBatHealth(
            75f
        );

        UpdatePopulationTrendFromHealth();
    }

    [ContextMenu("Debug - Add 20 Food")]
    private void DebugAddFood()
    {
        AddBatFood(
            20f
        );
    }

    [ContextMenu("Debug - Remove 10 Food")]
    private void DebugRemoveFood()
    {
        RemoveBatFood(
            10f
        );
    }

    [ContextMenu("Debug - Empty Food")]
    private void DebugEmptyFood()
    {
        SetBatFood(
            0f
        );
    }

    [ContextMenu("Debug - Fill Food")]
    private void DebugFillFood()
    {
        SetBatFood(
            100f
        );
    }

    [ContextMenu("Debug - Damage Health 10")]
    private void DebugDamageHealth()
    {
        RemoveBatHealth(
            10f
        );

        UpdatePopulationTrendFromHealth();
    }

    [ContextMenu("Debug - Heal Health 10")]
    private void DebugHealHealth()
    {
        AddBatHealth(
            10f
        );

        UpdatePopulationTrendFromHealth();
    }

    [ContextMenu("Debug - Remove Water 10")]
    private void DebugRemoveWater()
    {
        RemoveBatWater(
            10f
        );
    }

    [ContextMenu("Debug - Add Water 10")]
    private void DebugAddWater()
    {
        AddBatWater(
            10f
        );
    }

    [ContextMenu("Debug - Auto Assign References")]
    private void DebugAutoAssignReferences()
    {
        AutoAssignReferences();

        Debug.Log(
            "[BatColony] AUTO ASSIGN" +

            "\nEnd Day System: " +
            (
                endDaySystem != null
                    ? endDaySystem.name
                    : "NOT FOUND"
            ) +

            "\nRanger Station: " +
            (
                rangerStation != null
                    ? rangerStation.name
                    : "NOT FOUND"
            )
        );
    }
}
