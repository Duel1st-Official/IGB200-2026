using UnityEngine;

public class RangerStation : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    [Tooltip("The Ghost Bat colony affected by the environment.")]
    [SerializeField]
    private BatColony batColony;

    // =========================================================
    // ENVIRONMENT STATS
    // =========================================================

    [Header("Environment Stats")]

    [Range(0f, 100f)]
    [SerializeField]
    private float preyAvailability = 75f;

    [Range(0f, 100f)]
    [SerializeField]
    private float predatorPressure = 35f;

    [Range(0f, 100f)]
    [SerializeField]
    private float fireRisk = 20f;

    [Range(0f, 100f)]
    [SerializeField]
    private float soilHealth = 100f;

    // =========================================================
    // BASE ENVIRONMENT VALUES
    // =========================================================

    [Header("Base Environment Values")]

    [Tooltip("Prey Availability before Farm Plot and event effects.")]
    [Range(0f, 100f)]
    [SerializeField]
    private float basePreyAvailability = 30f;

    [Tooltip("Predator Pressure before Trap and event effects.")]
    [Range(0f, 100f)]
    [SerializeField]
    private float basePredatorPressure = 50f;

    [Tooltip("Fire Risk before Water Plot and event effects.")]
    [Range(0f, 100f)]
    [SerializeField]
    private float baseFireRisk = 30f;

    // =========================================================
    // EVENT MODIFIERS
    // =========================================================

    [Header("End Day Event Modifiers")]

    [Tooltip(
        "Persistent Prey Availability modifier caused by events. " +
        "Positive values increase prey. Negative values reduce prey."
    )]
    [SerializeField]
    private float preyEventModifier = 0f;

    [Tooltip(
        "Persistent Predator Pressure modifier caused by events. " +
        "Positive values increase predator pressure."
    )]
    [SerializeField]
    private float predatorEventModifier = 0f;

    [Tooltip(
        "Persistent Fire Risk modifier caused by events. " +
        "Positive values increase fire risk."
    )]
    [SerializeField]
    private float fireEventModifier = 0f;

    [Tooltip(
        "Prevent accumulated event modifiers from becoming excessively large."
    )]
    [Min(0f)]
    [SerializeField]
    private float maximumAbsoluteEventModifier = 50f;

    // =========================================================
    // FARM PLOT EFFECTS
    // =========================================================

    [Header("Farm Plot Effects")]

    [Tooltip(
        "Prey Availability added for every placed Farm Plot."
    )]
    [SerializeField]
    private float preyPerFarmPlot = 10f;

    // =========================================================
    // WATER PLOT - COLONY WATER
    // =========================================================

    [Header("Water Plot - Colony Water")]

    [Tooltip(
        "Water contributed by a CLEAN Water Plot."
    )]
    [SerializeField]
    private float cleanWaterContribution = 20f;

    [Tooltip(
        "Water contributed by a DIRTY Water Plot."
    )]
    [SerializeField]
    private float dirtyWaterContribution = 10f;

    [Tooltip(
        "Water contributed by a MURKY Water Plot."
    )]
    [SerializeField]
    private float murkyWaterContribution = 0f;

    // =========================================================
    // WATER PLOT - FIRE RISK
    // =========================================================

    [Header("Water Plot - Fire Risk")]

    [Tooltip(
        "Fire Risk removed by a CLEAN Water Plot."
    )]
    [SerializeField]
    private float cleanFireRiskReduction = 5f;

    [Tooltip(
        "Fire Risk removed by a DIRTY Water Plot."
    )]
    [SerializeField]
    private float dirtyFireRiskReduction = 2.5f;

    [Tooltip(
        "Fire Risk removed by a MURKY Water Plot."
    )]
    [SerializeField]
    private float murkyFireRiskReduction = 0f;

    // =========================================================
    // TRAP PLOT EFFECTS
    // =========================================================

    [Header("Trap Plot Effects")]

    [Tooltip(
        "Predator Pressure removed for every placed Trap."
    )]
    [SerializeField]
    private float predatorReductionPerTrap = 10f;

    // =========================================================
    // SOIL SETTINGS
    // =========================================================

    [Header("Soil Health Settings")]

    [Tooltip(
        "How much Soil Health is lost whenever a crop is planted."
    )]
    [SerializeField]
    private float soilDamagePerPlot = 2f;

    // =========================================================
    // UPDATE SETTINGS
    // =========================================================

    [Header("Plot Detection")]

    [Tooltip(
        "How often the Ranger Station checks plots and their current states."
    )]
    [Min(0.05f)]
    [SerializeField]
    private float plotCheckInterval = 0.25f;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLogs = false;

    [SerializeField]
    private int detectedFarmPlots = 0;

    [SerializeField]
    private int detectedWaterPlots = 0;

    [SerializeField]
    private int detectedCleanWaterPlots = 0;

    [SerializeField]
    private int detectedDirtyWaterPlots = 0;

    [SerializeField]
    private int detectedMurkyWaterPlots = 0;

    [SerializeField]
    private int detectedTraps = 0;

    [SerializeField]
    private float calculatedColonyWater = 0f;

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

        CalculateFarmPlotEffects();

        CalculateWaterPlotEffects(
            waterPlots
        );

        CalculateTrapEffects();

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

                "\n\n--- EVENT MODIFIERS ---" +
                "\nPrey: " +
                preyEventModifier +
                "\nPredators: " +
                predatorEventModifier +
                "\nFire: " +
                fireEventModifier +

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
            ) +
            preyEventModifier;

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

        // -----------------------------------------------------
        // NO WATER PLOTS
        // -----------------------------------------------------

        if (waterPlots == null ||
            waterPlots.Length == 0)
        {
            calculatedColonyWater =
                0f;

            SetFireRisk(
                baseFireRisk +
                fireEventModifier
            );

            if (batColony != null)
            {
                batColony.SetBatWater(
                    0f
                );
            }

            return;
        }

        // -----------------------------------------------------
        // CHECK EVERY WATER PLOT
        // -----------------------------------------------------

        foreach (WaterPlot waterPlot in waterPlots)
        {
            if (waterPlot == null)
            {
                continue;
            }

            if (waterPlot.IsClean())
            {
                detectedCleanWaterPlots++;

                totalWater +=
                    cleanWaterContribution;

                totalFireReduction +=
                    cleanFireRiskReduction;

                continue;
            }

            if (waterPlot.IsDirty())
            {
                detectedDirtyWaterPlots++;

                totalWater +=
                    dirtyWaterContribution;

                totalFireReduction +=
                    dirtyFireRiskReduction;

                continue;
            }

            if (waterPlot.IsMurky())
            {
                detectedMurkyWaterPlots++;

                totalWater +=
                    murkyWaterContribution;

                totalFireReduction +=
                    murkyFireRiskReduction;

                continue;
            }

            detectedMurkyWaterPlots++;

            totalWater +=
                murkyWaterContribution;

            totalFireReduction +=
                murkyFireRiskReduction;
        }

        // -----------------------------------------------------
        // COLONY WATER
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // FIRE RISK
        // -----------------------------------------------------

        float calculatedFireRisk =
            baseFireRisk -
            totalFireReduction +
            fireEventModifier;

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
            ) +
            predatorEventModifier;

        SetPredatorPressure(
            calculatedPredatorPressure
        );
    }

    // =========================================================
    // EVENT MODIFIER HELPERS
    // =========================================================

    private float ClampEventModifier(
        float value)
    {
        float limit =
            Mathf.Max(
                0f,
                maximumAbsoluteEventModifier
            );

        return
            Mathf.Clamp(
                value,
                -limit,
                limit
            );
    }

    // =========================================================
    // PREY EVENT MODIFIER
    // =========================================================

    public float GetPreyEventModifier()
    {
        return preyEventModifier;
    }

    public void SetPreyEventModifier(
        float value)
    {
        preyEventModifier =
            ClampEventModifier(
                value
            );

        RecalculatePlotEffects();
    }

    public void AddPreyEventModifier(
        float amount)
    {
        SetPreyEventModifier(
            preyEventModifier +
            amount
        );
    }

    public void RemovePreyEventModifier(
        float amount)
    {
        SetPreyEventModifier(
            preyEventModifier -
            amount
        );
    }

    // =========================================================
    // PREDATOR EVENT MODIFIER
    // =========================================================

    public float GetPredatorEventModifier()
    {
        return predatorEventModifier;
    }

    public void SetPredatorEventModifier(
        float value)
    {
        predatorEventModifier =
            ClampEventModifier(
                value
            );

        RecalculatePlotEffects();
    }

    public void AddPredatorEventModifier(
        float amount)
    {
        SetPredatorEventModifier(
            predatorEventModifier +
            amount
        );
    }

    public void RemovePredatorEventModifier(
        float amount)
    {
        SetPredatorEventModifier(
            predatorEventModifier -
            amount
        );
    }

    // =========================================================
    // FIRE EVENT MODIFIER
    // =========================================================

    public float GetFireEventModifier()
    {
        return fireEventModifier;
    }

    public void SetFireEventModifier(
        float value)
    {
        fireEventModifier =
            ClampEventModifier(
                value
            );

        RecalculatePlotEffects();
    }

    public void AddFireEventModifier(
        float amount)
    {
        SetFireEventModifier(
            fireEventModifier +
            amount
        );
    }

    public void RemoveFireEventModifier(
        float amount)
    {
        SetFireEventModifier(
            fireEventModifier -
            amount
        );
    }

    // =========================================================
    // CLEAR EVENT MODIFIERS
    // =========================================================

    public void ClearEventModifiers()
    {
        preyEventModifier =
            0f;

        predatorEventModifier =
            0f;

        fireEventModifier =
            0f;

        RecalculatePlotEffects();

        if (showDebugLogs)
        {
            Debug.Log(
                "[RangerStation] Event modifiers cleared."
            );
        }
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
    // CONDITIONS
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

    [ContextMenu("Debug - Clear Event Modifiers")]
    private void DebugClearEventModifiers()
    {
        ClearEventModifiers();
    }
}