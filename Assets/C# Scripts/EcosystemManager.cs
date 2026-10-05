using UnityEngine;

public class EcosystemManager : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private RangerStation rangerStation;
    [SerializeField] private BatColony batColony;
    [SerializeField] private EndDaySystem endDaySystem;

    // =========================================================
    // DAILY ENVIRONMENT DRIFT
    // =========================================================

    [Header("Daily Environment Drift")]

    [Tooltip("Prey naturally decreases each day.")]
    [SerializeField] private float dailyPreyLoss = 2f;

    [Tooltip("Predator pressure naturally increases each day.")]
    [SerializeField] private float dailyPredatorIncrease = 2f;

    [Tooltip("Soil naturally restores itself each day.")]
    [SerializeField] private float dailySoilRecovery = 1f;

    // =========================================================
    // WEATHER / FIRE
    // =========================================================

    [Header("Weather Fire Risk")]

    [Tooltip("Fire risk added after a Sunny day.")]
    [SerializeField] private float sunnyFireChange = 3f;

    [Tooltip("Fire risk change after a Rain day.")]
    [SerializeField] private float rainFireChange = -4f;

    [Tooltip("Fire risk change after a thunderstorm.")]
    [SerializeField] private float thunderFireChange = 2f;

    // =========================================================
    // WATER PLOTS
    // =========================================================

    [Header("Water Plot Effects")]

    [SerializeField] private float cleanWaterAmount = 4f;
    [SerializeField] private float dirtyWaterAmount = 1f;
    [SerializeField] private float murkyWaterAmount = -3f;

    [Space]

    [SerializeField] private float cleanWaterFireReduction = 2f;
    [SerializeField] private float dirtyWaterFireReduction = 1f;

    // =========================================================
    // PREY -> FOOD
    // =========================================================

    [Header("Prey To Food")]

    [Tooltip("Prey at or above this gives the best daily Food bonus.")]
    [Range(0f, 100f)]
    [SerializeField] private float excellentPreyThreshold = 80f;

    [Range(0f, 100f)]
    [SerializeField] private float goodPreyThreshold = 60f;

    [Range(0f, 100f)]
    [SerializeField] private float poorPreyThreshold = 40f;

    [Range(0f, 100f)]
    [SerializeField] private float dangerPreyThreshold = 20f;

    [Space]

    [SerializeField] private float excellentFoodChange = 5f;
    [SerializeField] private float goodFoodChange = 2f;
    [SerializeField] private float moderateFoodChange = 0f;
    [SerializeField] private float poorFoodChange = -3f;
    [SerializeField] private float dangerFoodChange = -6f;

    // =========================================================
    // HEALTH
    // =========================================================

    [Header("Health From Food")]

    [Range(0f, 100f)]
    [SerializeField] private float healthyFoodThreshold = 70f;

    [Range(0f, 100f)]
    [SerializeField] private float dangerFoodHealthThreshold = 30f;

    [SerializeField] private float healthyFoodHealthBonus = 2f;
    [SerializeField] private float lowFoodHealthPenalty = -3f;

    [Header("Health From Water")]

    [Range(0f, 100f)]
    [SerializeField] private float healthyWaterThreshold = 70f;

    [Range(0f, 100f)]
    [SerializeField] private float dangerWaterHealthThreshold = 30f;

    [SerializeField] private float healthyWaterHealthBonus = 2f;
    [SerializeField] private float lowWaterHealthPenalty = -3f;

    [Header("Health From Predators")]

    [Range(0f, 100f)]
    [SerializeField] private float dangerousPredatorThreshold = 60f;

    [SerializeField] private float predatorHealthPenalty = -3f;

    [Header("Health From Fire")]

    [Range(0f, 100f)]
    [SerializeField] private float dangerousFireThreshold = 70f;

    [SerializeField] private float fireHealthPenalty = -2f;

    // =========================================================
    // POPULATION
    // =========================================================

    [Header("Population - Growing Conditions")]

    [Range(0f, 100f)]
    [SerializeField] private float growingHealthRequired = 75f;

    [Range(0f, 100f)]
    [SerializeField] private float growingFoodRequired = 70f;

    [Range(0f, 100f)]
    [SerializeField] private float growingWaterRequired = 70f;

    [Range(0f, 100f)]
    [SerializeField] private float growingMaxPredatorPressure = 30f;

    [Header("Population - Dying Conditions")]

    [Range(0f, 100f)]
    [SerializeField] private float dyingHealthThreshold = 40f;

    [Range(0f, 100f)]
    [SerializeField] private float dyingFoodThreshold = 30f;

    [Range(0f, 100f)]
    [SerializeField] private float dyingWaterThreshold = 30f;

    [Range(0f, 100f)]
    [SerializeField] private float dyingPredatorThreshold = 75f;

    [Header("Population Change Speed")]

    [Tooltip("How many consecutive Growing days are required for +1 bat.")]
    [Min(1)]
    [SerializeField] private int growingDaysForPopulationIncrease = 3;

    [Tooltip("How many consecutive Dying days are required for -1 bat.")]
    [Min(1)]
    [SerializeField] private int dyingDaysForPopulationDecrease = 2;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // =========================================================
    // PRIVATE
    // =========================================================

    private int lastProcessedDay = -1;

    private int growingDayProgress = 0;
    private int dyingDayProgress = 0;

    private string recordedWeather = "Sunny";

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        FindReferences();

        if (endDaySystem != null)
        {
            lastProcessedDay =
                endDaySystem.GetCurrentDay();
        }

        recordedWeather =
            GetCurrentWeatherName();
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

            if (endDaySystem == null)
            {
                return;
            }
        }

        int currentDay =
            endDaySystem.GetCurrentDay();

        if (lastProcessedDay < 0)
        {
            lastProcessedDay =
                currentDay;

            recordedWeather =
                GetCurrentWeatherName();

            return;
        }

        if (currentDay ==
            lastProcessedDay)
        {
            return;
        }

        string previousWeather =
            recordedWeather;

        ProcessNewDay(
            previousWeather
        );

        lastProcessedDay =
            currentDay;

        recordedWeather =
            GetCurrentWeatherName();
    }

    // =========================================================
    // FIND REFERENCES
    // =========================================================

    private void FindReferences()
    {
        if (rangerStation == null)
        {
            rangerStation =
                FindFirstObjectByType<RangerStation>();
        }

        if (batColony == null)
        {
            batColony =
                FindFirstObjectByType<BatColony>();
        }

        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }
    }

    // =========================================================
    // PROCESS NEW DAY
    // =========================================================

    public void ProcessNewDay(
        string previousWeather)
    {
        FindReferences();

        if (rangerStation == null ||
            batColony == null)
        {
            return;
        }

        // -----------------------------------------------------
        // 1. NATURAL ENVIRONMENT DRIFT
        // -----------------------------------------------------

        ProcessEnvironmentDrift();

        // -----------------------------------------------------
        // 2. WEATHER
        // -----------------------------------------------------

        ProcessWeather(
            previousWeather
        );

        // -----------------------------------------------------
        // 3. WATER PLOTS
        // -----------------------------------------------------

        ProcessWaterPlots();

        // -----------------------------------------------------
        // 4. PREY -> FOOD
        // -----------------------------------------------------

        ProcessFood();

        // -----------------------------------------------------
        // 5. COLONY HEALTH
        // -----------------------------------------------------

        ProcessHealth();

        // -----------------------------------------------------
        // 6. POPULATION
        // -----------------------------------------------------

        ProcessPopulation();

        if (showDebugLogs)
        {
            Debug.Log(
                "===== ECOSYSTEM NEW DAY =====" +
                "\nPrey: " +
                rangerStation.GetPreyAvailability().ToString("0") +
                "\nPredators: " +
                rangerStation.GetPredatorPressure().ToString("0") +
                "\nFire: " +
                rangerStation.GetFireRisk().ToString("0") +
                "\nSoil: " +
                rangerStation.GetSoilHealth().ToString("0") +
                "\nBat Health: " +
                batColony.GetBatHealth().ToString("0") +
                "\nBat Food: " +
                batColony.GetBatFood().ToString("0") +
                "\nBat Water: " +
                batColony.GetBatWater().ToString("0") +
                "\nPopulation: " +
                batColony.GetPopulation() +
                " (" +
                batColony.GetPopulationTrendText() +
                ")"
            );
        }
    }

    // =========================================================
    // ENVIRONMENT DRIFT
    // =========================================================

    private void ProcessEnvironmentDrift()
    {
        rangerStation.RemovePreyAvailability(
            dailyPreyLoss
        );

        rangerStation.AddPredatorPressure(
            dailyPredatorIncrease
        );

        rangerStation.AddSoilHealth(
            dailySoilRecovery
        );
    }

    // =========================================================
    // WEATHER
    // =========================================================

    private void ProcessWeather(
        string weather)
    {
        float fireChange =
            sunnyFireChange;

        if (weather ==
            "Rain")
        {
            fireChange =
                rainFireChange;
        }
        else if (
            weather ==
                "RainAndThunder" ||
            weather ==
                "Thunder" ||
            weather ==
                "Storm")
        {
            fireChange =
                thunderFireChange;
        }

        if (fireChange >= 0f)
        {
            rangerStation.AddFireRisk(
                fireChange
            );
        }
        else
        {
            rangerStation.RemoveFireRisk(
                Mathf.Abs(
                    fireChange
                )
            );
        }
    }

    // =========================================================
    // WATER PLOTS
    // =========================================================

    private void ProcessWaterPlots()
    {
        WaterPlot[] waterPlots =
            FindObjectsByType<WaterPlot>(
                FindObjectsSortMode.None
            );

        float totalWaterChange =
            0f;

        float totalFireReduction =
            0f;

        foreach (WaterPlot waterPlot
                 in waterPlots)
        {
            if (waterPlot == null)
            {
                continue;
            }

            if (waterPlot.IsClean())
            {
                totalWaterChange +=
                    cleanWaterAmount;

                totalFireReduction +=
                    cleanWaterFireReduction;

                continue;
            }

            if (waterPlot.IsDirty())
            {
                totalWaterChange +=
                    dirtyWaterAmount;

                totalFireReduction +=
                    dirtyWaterFireReduction;

                continue;
            }

            // Anything worse than Dirty is treated as Murky.
            totalWaterChange +=
                murkyWaterAmount;
        }

        batColony.AddBatWater(
            totalWaterChange
        );

        rangerStation.RemoveFireRisk(
            totalFireReduction
        );
    }

    // =========================================================
    // FOOD
    // =========================================================

    private void ProcessFood()
    {
        float prey =
            rangerStation.GetPreyAvailability();

        float foodChange;

        if (prey >=
            excellentPreyThreshold)
        {
            foodChange =
                excellentFoodChange;
        }
        else if (prey >=
                 goodPreyThreshold)
        {
            foodChange =
                goodFoodChange;
        }
        else if (prey >=
                 poorPreyThreshold)
        {
            foodChange =
                moderateFoodChange;
        }
        else if (prey >=
                 dangerPreyThreshold)
        {
            foodChange =
                poorFoodChange;
        }
        else
        {
            foodChange =
                dangerFoodChange;
        }

        batColony.AddBatFood(
            foodChange
        );
    }

    // =========================================================
    // HEALTH
    // =========================================================

    private void ProcessHealth()
    {
        float healthChange =
            0f;

        // -----------------------------------------------------
        // FOOD
        // -----------------------------------------------------

        if (batColony.GetBatFood() >=
            healthyFoodThreshold)
        {
            healthChange +=
                healthyFoodHealthBonus;
        }
        else if (
            batColony.GetBatFood() <=
            dangerFoodHealthThreshold)
        {
            healthChange +=
                lowFoodHealthPenalty;
        }

        // -----------------------------------------------------
        // WATER
        // -----------------------------------------------------

        if (batColony.GetBatWater() >=
            healthyWaterThreshold)
        {
            healthChange +=
                healthyWaterHealthBonus;
        }
        else if (
            batColony.GetBatWater() <=
            dangerWaterHealthThreshold)
        {
            healthChange +=
                lowWaterHealthPenalty;
        }

        // -----------------------------------------------------
        // PREDATORS
        // -----------------------------------------------------

        if (rangerStation.GetPredatorPressure() >=
            dangerousPredatorThreshold)
        {
            healthChange +=
                predatorHealthPenalty;
        }

        // -----------------------------------------------------
        // FIRE
        // -----------------------------------------------------

        if (rangerStation.GetFireRisk() >=
            dangerousFireThreshold)
        {
            healthChange +=
                fireHealthPenalty;
        }

        batColony.AddBatHealth(
            healthChange
        );
    }

    // =========================================================
    // POPULATION
    // =========================================================

    private void ProcessPopulation()
    {
        bool growing =
            IsColonyGrowing();

        bool dying =
            IsColonyDying();

        // =====================================================
        // GROWING
        // =====================================================

        if (growing)
        {
            batColony.SetPopulationTrend(
                BatColony.ColonyTrend.Growing
            );

            growingDayProgress++;

            dyingDayProgress =
                0;

            if (growingDayProgress >=
                growingDaysForPopulationIncrease)
            {
                batColony.AddPopulation(
                    1
                );

                growingDayProgress =
                    0;
            }

            return;
        }

        // =====================================================
        // DYING
        // =====================================================

        if (dying)
        {
            batColony.SetPopulationTrend(
                BatColony.ColonyTrend.Dying
            );

            dyingDayProgress++;

            growingDayProgress =
                0;

            if (dyingDayProgress >=
                dyingDaysForPopulationDecrease)
            {
                batColony.RemovePopulation(
                    1
                );

                dyingDayProgress =
                    0;
            }

            return;
        }

        // =====================================================
        // STABLE
        // =====================================================

        batColony.SetPopulationTrend(
            BatColony.ColonyTrend.Stable
        );

        growingDayProgress =
            0;

        dyingDayProgress =
            0;
    }

    // =========================================================
    // GROWING CHECK
    // =========================================================

    private bool IsColonyGrowing()
    {
        return
            batColony.GetBatHealth() >=
                growingHealthRequired &&
            batColony.GetBatFood() >=
                growingFoodRequired &&
            batColony.GetBatWater() >=
                growingWaterRequired &&
            rangerStation.GetPredatorPressure() <=
                growingMaxPredatorPressure;
    }

    // =========================================================
    // DYING CHECK
    // =========================================================

    private bool IsColonyDying()
    {
        return
            batColony.GetBatHealth() <=
                dyingHealthThreshold ||

            batColony.GetBatFood() <=
                dyingFoodThreshold ||

            batColony.GetBatWater() <=
                dyingWaterThreshold ||

            rangerStation.GetPredatorPressure() >=
                dyingPredatorThreshold;
    }

    // =========================================================
    // WEATHER NAME
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
    // DEBUG
    // =========================================================

    [ContextMenu("Debug - Process Sunny Day")]
    private void DebugSunnyDay()
    {
        ProcessNewDay(
            "Sunny"
        );
    }

    [ContextMenu("Debug - Process Rain Day")]
    private void DebugRainDay()
    {
        ProcessNewDay(
            "Rain"
        );
    }

    [ContextMenu("Debug - Process Thunder Day")]
    private void DebugThunderDay()
    {
        ProcessNewDay(
            "RainAndThunder"
        );
    }
}