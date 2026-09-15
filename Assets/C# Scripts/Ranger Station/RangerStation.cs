using UnityEngine;

public class RangerStation : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    [Tooltip("The Ghost Bat colony affected by the environment.")]
    [SerializeField] private BatColony batColony;

    // =========================================================
    // ENVIRONMENT STATS
    // =========================================================

    [Header("Environment Stats")]

    [Range(0f, 100f)]
    [SerializeField] private float preyAvailability = 75f;

    [Range(0f, 100f)]
    [SerializeField] private float predatorPressure = 35f;

    [Range(0f, 100f)]
    [SerializeField] private float fireRisk = 20f;

    [Range(0f, 100f)]
    [SerializeField] private float soilHealth = 100f;

    // =========================================================
    // BASE ENVIRONMENT VALUES
    // =========================================================

    [Header("Base Environment Values")]

    [Tooltip("Prey Availability before Farm Plot effects.")]
    [Range(0f, 100f)]
    [SerializeField] private float basePreyAvailability = 30f;

    [Tooltip("Predator Pressure before Trap effects.")]
    [Range(0f, 100f)]
    [SerializeField] private float basePredatorPressure = 50f;

    [Tooltip("Fire Risk before Water Plot effects.")]
    [Range(0f, 100f)]
    [SerializeField] private float baseFireRisk = 30f;

    // =========================================================
    // FARM PLOT EFFECTS
    // =========================================================

    [Header("Farm Plot Effects")]

    [Tooltip(
        "Prey Availability added for every placed Farm Plot."
    )]
    [SerializeField] private float preyPerFarmPlot = 10f;

    // =========================================================
    // WATER PLOT - COLONY WATER
    // =========================================================

    [Header("Water Plot - Colony Water")]

    [Tooltip(
        "Water contributed by a CLEAN Water Plot."
    )]
    [SerializeField] private float cleanWaterContribution = 20f;

    [Tooltip(
        "Water contributed by a DIRTY Water Plot."
    )]
    [SerializeField] private float dirtyWaterContribution = 10f;

    [Tooltip(
        "Water contributed by a MURKY Water Plot."
    )]
    [SerializeField] private float murkyWaterContribution = 0f;

    // =========================================================
    // WATER PLOT - FIRE RISK
    // =========================================================

    [Header("Water Plot - Fire Risk")]

    [Tooltip(
        "Fire Risk removed by a CLEAN Water Plot."
    )]
    [SerializeField] private float cleanFireRiskReduction = 5f;

    [Tooltip(
        "Fire Risk removed by a DIRTY Water Plot."
    )]
    [SerializeField] private float dirtyFireRiskReduction = 2.5f;

    [Tooltip(
        "Fire Risk removed by a MURKY Water Plot."
    )]
    [SerializeField] private float murkyFireRiskReduction = 0f;

    // =========================================================
    // TRAP PLOT EFFECTS
    // =========================================================

    [Header("Trap Plot Effects")]

    [Tooltip(
        "Predator Pressure removed for every placed Trap."
    )]
    [SerializeField] private float predatorReductionPerTrap = 10f;

    // =========================================================
    // SOIL SETTINGS
    // =========================================================

    [Header("Soil Health Settings")]

    [Tooltip(
        "How much Soil Health is lost whenever a crop is planted."
    )]
    [SerializeField] private float soilDamagePerPlot = 2f;

    // =========================================================
    // UPDATE SETTINGS
    // =========================================================

    [Header("Plot Detection")]

    [Tooltip(
        "How often the Ranger Station checks plots and their current states."
    )]
    [Min(0.05f)]
    [SerializeField] private float plotCheckInterval = 0.25f;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField] private bool showDebugLogs = false;

    [SerializeField] private int detectedFarmPlots = 0;
    [SerializeField] private int detectedWaterPlots = 0;
    [SerializeField] private int detectedCleanWaterPlots = 0;
    [SerializeField] private int detectedDirtyWaterPlots = 0;
    [SerializeField] private int detectedMurkyWaterPlots = 0;
    [SerializeField] private int detectedTraps = 0;

    [SerializeField] private float calculatedColonyWater = 0f;

    // =========================================================
    // PRIVATE
    // =========================================================

    private float plotCheckTimer = 0f;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        FindBatColony();
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        RecalculatePlotEffects();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        plotCheckTimer -=
            Time.deltaTime;

        if (plotCheckTimer > 0f)
        {
            return;
        }

        plotCheckTimer =
            plotCheckInterval;

        RecalculatePlotEffects();
    }

    // =========================================================
    // FIND BAT COLONY
    // =========================================================

    private void FindBatColony()
    {
        if (batColony != null)
        {
            return;
        }

        batColony =
            FindFirstObjectByType<BatColony>();

        if (batColony == null &&
            showDebugLogs)
        {
            Debug.LogWarning(
                "[RangerStation] No BatColony was found."
            );
        }
    }

    // =========================================================
    // RECALCULATE ALL PLOT EFFECTS
    // =========================================================

    public void RecalculatePlotEffects()
    {
        FindBatColony();

        // =====================================================
        // FIND PLOTS
        // =====================================================

        Plot[] farmPlots =
            FindObjectsByType<Plot>(
                FindObjectsSortMode.None
            );

        WaterPlot[] waterPlots =
            FindObjectsByType<WaterPlot>(
                FindObjectsSortMode.None
            );

        Trap[] traps =
            FindObjectsByType<Trap>(
                FindObjectsSortMode.None
            );

        // =====================================================
        // COUNTS
        // =====================================================

        detectedFarmPlots =
            farmPlots != null
                ? farmPlots.Length
                : 0;

        detectedWaterPlots =
            waterPlots != null
                ? waterPlots.Length
                : 0;

        detectedTraps =
            traps != null
                ? traps.Length
                : 0;

        // =====================================================
        // FARM PLOTS -> PREY
        // =====================================================

        CalculateFarmPlotEffects();

        // =====================================================
        // WATER PLOTS -> WATER + FIRE RISK
        // =====================================================

        CalculateWaterPlotEffects(
            waterPlots
        );

        // =====================================================
        // TRAPS -> PREDATOR PRESSURE
        // =====================================================

        CalculateTrapEffects();

        // =====================================================
        // DEBUG
        // =====================================================

        if (showDebugLogs)
        {
            Debug.Log(
                "[RangerStation] Ecosystem Updated" +

                "\n\n--- FARM ---" +
                "\nFarm Plots: " +
                detectedFarmPlots +

                "\n\n--- WATER ---" +
                "\nWater Plots: " +
                detectedWaterPlots +
                "\nClean: " +
                detectedCleanWaterPlots +
                "\nDirty: " +
                detectedDirtyWaterPlots +
                "\nMurky: " +
                detectedMurkyWaterPlots +

                "\n\n--- TRAPS ---" +
                "\nTraps: " +
                detectedTraps +

                "\n\n--- ENVIRONMENT ---" +
                "\nPrey Availability: " +
                preyAvailability +
                "\nPredator Pressure: " +
                predatorPressure +
                "\nFire Risk: " +
                fireRisk +
                "\nSoil Health: " +
                soilHealth +

                "\n\n--- COLONY ---" +
                "\nWater: " +
                calculatedColonyWater
            );
        }
    }

    // =========================================================
    // FARM PLOT EFFECTS
    // =========================================================

    private void CalculateFarmPlotEffects()
    {
        float calculatedPrey =
            basePreyAvailability +
            (
                detectedFarmPlots *
                preyPerFarmPlot
            );

        SetPreyAvailability(
            calculatedPrey
        );
    }

    // =========================================================
    // WATER PLOT EFFECTS
    // =========================================================

    private void CalculateWaterPlotEffects(
        WaterPlot[] waterPlots)
    {
        detectedCleanWaterPlots = 0;
        detectedDirtyWaterPlots = 0;
        detectedMurkyWaterPlots = 0;

        float totalWater =
            0f;

        float totalFireReduction =
            0f;

        // =====================================================
        // NO WATER PLOTS
        // =====================================================

        if (waterPlots == null ||
            waterPlots.Length == 0)
        {
            calculatedColonyWater =
                0f;

            SetFireRisk(
                baseFireRisk
            );

            if (batColony != null)
            {
                batColony.SetBatWater(
                    0f
                );
            }

            return;
        }

        // =====================================================
        // CHECK EVERY WATER PLOT
        // =====================================================

        foreach (WaterPlot waterPlot in waterPlots)
        {
            if (waterPlot == null)
            {
                continue;
            }

            // =================================================
            // CLEAN
            // =================================================

            if (waterPlot.IsClean())
            {
                detectedCleanWaterPlots++;

                totalWater +=
                    cleanWaterContribution;

                totalFireReduction +=
                    cleanFireRiskReduction;

                continue;
            }

            // =================================================
            // DIRTY
            // =================================================

            if (waterPlot.IsDirty())
            {
                detectedDirtyWaterPlots++;

                totalWater +=
                    dirtyWaterContribution;

                totalFireReduction +=
                    dirtyFireRiskReduction;

                continue;
            }

            // =================================================
            // MURKY
            // =================================================

            if (waterPlot.IsMurky())
            {
                detectedMurkyWaterPlots++;

                totalWater +=
                    murkyWaterContribution;

                totalFireReduction +=
                    murkyFireRiskReduction;

                continue;
            }

            // =================================================
            // UNKNOWN STATE FALLBACK
            // =================================================

            detectedMurkyWaterPlots++;

            totalWater +=
                murkyWaterContribution;

            totalFireReduction +=
                murkyFireRiskReduction;
        }

        // =====================================================
        // COLONY WATER
        // =====================================================

        calculatedColonyWater =
            Mathf.Clamp(
                totalWater,
                0f,
                100f
            );

        if (batColony != null)
        {
            batColony.SetBatWater(
                calculatedColonyWater
            );
        }

        // =====================================================
        // FIRE RISK
        // =====================================================

        float calculatedFireRisk =
            baseFireRisk -
            totalFireReduction;

        SetFireRisk(
            calculatedFireRisk
        );
    }

    // =========================================================
    // TRAP EFFECTS
    // =========================================================

    private void CalculateTrapEffects()
    {
        float calculatedPredatorPressure =
            basePredatorPressure -
            (
                detectedTraps *
                predatorReductionPerTrap
            );

        SetPredatorPressure(
            calculatedPredatorPressure
        );
    }

    // =========================================================
    // PREY AVAILABILITY
    // =========================================================

    public float GetPreyAvailability()
    {
        return preyAvailability;
    }

    public void SetPreyAvailability(
        float value)
    {
        preyAvailability =
            Mathf.Clamp(
                value,
                0f,
                100f
            );
    }

    public void AddPreyAvailability(
        float amount)
    {
        SetPreyAvailability(
            preyAvailability +
            amount
        );
    }

    public void RemovePreyAvailability(
        float amount)
    {
        SetPreyAvailability(
            preyAvailability -
            amount
        );
    }

    // =========================================================
    // PREDATOR PRESSURE
    // =========================================================

    public float GetPredatorPressure()
    {
        return predatorPressure;
    }

    public void SetPredatorPressure(
        float value)
    {
        predatorPressure =
            Mathf.Clamp(
                value,
                0f,
                100f
            );
    }

    public void AddPredatorPressure(
        float amount)
    {
        SetPredatorPressure(
            predatorPressure +
            amount
        );
    }

    public void RemovePredatorPressure(
        float amount)
    {
        SetPredatorPressure(
            predatorPressure -
            amount
        );
    }

    // =========================================================
    // FIRE RISK
    // =========================================================

    public float GetFireRisk()
    {
        return fireRisk;
    }

    public void SetFireRisk(
        float value)
    {
        fireRisk =
            Mathf.Clamp(
                value,
                0f,
                100f
            );
    }

    public void AddFireRisk(
        float amount)
    {
        SetFireRisk(
            fireRisk +
            amount
        );
    }

    public void RemoveFireRisk(
        float amount)
    {
        SetFireRisk(
            fireRisk -
            amount
        );
    }

    // =========================================================
    // SOIL HEALTH
    // =========================================================

    public float GetSoilHealth()
    {
        return soilHealth;
    }

    public void SetSoilHealth(
        float value)
    {
        soilHealth =
            Mathf.Clamp(
                value,
                0f,
                100f
            );

        if (showDebugLogs)
        {
            Debug.Log(
                "[RangerStation] Soil Health: " +
                soilHealth
            );
        }
    }

    public void AddSoilHealth(
        float amount)
    {
        SetSoilHealth(
            soilHealth +
            amount
        );
    }

    public void RemoveSoilHealth(
        float amount)
    {
        SetSoilHealth(
            soilHealth -
            amount
        );
    }

    public void DamageSoil(
        float amount)
    {
        RemoveSoilHealth(
            amount
        );
    }

    // =========================================================
    // CROP PLANTED
    // =========================================================

    public void PlotPlanted()
    {
        DamageSoil(
            soilDamagePerPlot
        );

        if (showDebugLogs)
        {
            Debug.Log(
                "[RangerStation] A crop was planted." +
                " Soil Health is now " +
                soilHealth
            );
        }
    }

    // =========================================================
    // SOIL CONDITION
    // =========================================================

    public string GetSoilCondition()
    {
        if (soilHealth <= 30f)
        {
            return "Poor";
        }

        if (soilHealth <= 65f)
        {
            return "Stressed";
        }

        return "Healthy";
    }

    // =========================================================
    // PREY CONDITION
    // =========================================================

    public string GetPreyCondition()
    {
        if (preyAvailability <= 30f)
        {
            return "Poor";
        }

        if (preyAvailability <= 65f)
        {
            return "Moderate";
        }

        return "Good";
    }

    // =========================================================
    // PREDATOR CONDITION
    // =========================================================

    public string GetPredatorCondition()
    {
        if (predatorPressure <= 30f)
        {
            return "Low";
        }

        if (predatorPressure <= 65f)
        {
            return "Moderate";
        }

        return "High";
    }

    // =========================================================
    // FIRE CONDITION
    // =========================================================

    public string GetFireRiskCondition()
    {
        if (fireRisk <= 30f)
        {
            return "Low";
        }

        if (fireRisk <= 65f)
        {
            return "Moderate";
        }

        return "High";
    }

    // =========================================================
    // PLOT COUNT GETTERS
    // =========================================================

    public int GetFarmPlotCount()
    {
        return detectedFarmPlots;
    }

    public int GetWaterPlotCount()
    {
        return detectedWaterPlots;
    }

    public int GetCleanWaterPlotCount()
    {
        return detectedCleanWaterPlots;
    }

    public int GetDirtyWaterPlotCount()
    {
        return detectedDirtyWaterPlots;
    }

    public int GetMurkyWaterPlotCount()
    {
        return detectedMurkyWaterPlots;
    }

    public int GetTrapCount()
    {
        return detectedTraps;
    }

    // =========================================================
    // COLONY WATER GETTER
    // =========================================================

    public float GetCalculatedColonyWater()
    {
        return calculatedColonyWater;
    }

    // =========================================================
    // DEBUG CONTEXT MENUS
    // =========================================================

    [ContextMenu("Debug - Recalculate Plot Effects")]
    private void DebugRecalculatePlotEffects()
    {
        RecalculatePlotEffects();
    }

    [ContextMenu("Debug - Damage Soil")]
    private void DebugDamageSoil()
    {
        DamageSoil(
            soilDamagePerPlot
        );
    }

    [ContextMenu("Debug - Heal Soil")]
    private void DebugHealSoil()
    {
        AddSoilHealth(
            10f
        );
    }

    [ContextMenu("Debug - Reset Soil")]
    private void DebugResetSoil()
    {
        SetSoilHealth(
            100f
        );
    }
}