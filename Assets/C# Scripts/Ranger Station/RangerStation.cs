using UnityEngine;

public class RangerStation : MonoBehaviour
{
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
    // SOIL SETTINGS
    // =========================================================

    [Header("Soil Health Settings")]

    [Tooltip(
        "How much soil health is lost whenever a crop is planted."
    )]
    [SerializeField] private float soilDamagePerPlot = 2f;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

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
                "Soil Health: " +
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
    // PLOT PLANTED
    // =========================================================

    public void PlotPlanted()
    {
        DamageSoil(
            soilDamagePerPlot
        );

        if (showDebugLogs)
        {
            Debug.Log(
                "A plot was planted. " +
                "Soil Health is now " +
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
    // DEBUG CONTEXT MENUS
    // =========================================================

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