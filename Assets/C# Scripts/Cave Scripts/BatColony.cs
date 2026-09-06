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
    // POPULATION
    // =========================================================

    [Header("Colony Population")]
    [Min(0)]
    [SerializeField] private int colonyPopulation = 20;

    [SerializeField]
    private ColonyTrend populationTrend =
        ColonyTrend.Stable;

    // =========================================================
    // COLONY VALUES
    // =========================================================

    [Header("Colony Values")]

    [Range(0f, 100f)]
    [SerializeField] private float batHealth = 100f;

    [Range(0f, 100f)]
    [SerializeField] private float batFood = 100f;

    [Range(0f, 100f)]
    [SerializeField] private float batWater = 100f;

    // =========================================================
    // CONDITION THRESHOLDS
    // =========================================================

    [Header("Condition Thresholds")]

    [Tooltip("At or below this value, the condition becomes Danger.")]
    [Range(0f, 100f)]
    [SerializeField] private float dangerThreshold = 30f;

    [Tooltip("At or below this value, the condition becomes Poor.")]
    [Range(0f, 100f)]
    [SerializeField] private float poorThreshold = 65f;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // =========================================================
    // POPULATION
    // =========================================================

    public void SetPopulation(int amount)
    {
        colonyPopulation =
            Mathf.Max(
                0,
                amount
            );

        DebugState();
    }

    public void AddPopulation(int amount)
    {
        colonyPopulation =
            Mathf.Max(
                0,
                colonyPopulation + amount
            );

        DebugState();
    }

    public void RemovePopulation(int amount)
    {
        colonyPopulation =
            Mathf.Max(
                0,
                colonyPopulation - amount
            );

        DebugState();
    }

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

    public void SetBatHealth(float value)
    {
        batHealth =
            Mathf.Clamp(
                value,
                0f,
                100f
            );

        DebugState();
    }

    public void AddBatHealth(float amount)
    {
        SetBatHealth(
            batHealth + amount
        );
    }

    // =========================================================
    // FOOD
    // =========================================================

    public void SetBatFood(float value)
    {
        batFood =
            Mathf.Clamp(
                value,
                0f,
                100f
            );

        DebugState();
    }

    public void AddBatFood(float amount)
    {
        SetBatFood(
            batFood + amount
        );
    }

    // =========================================================
    // WATER
    // =========================================================

    public void SetBatWater(float value)
    {
        batWater =
            Mathf.Clamp(
                value,
                0f,
                100f
            );

        DebugState();
    }

    public void AddBatWater(float amount)
    {
        SetBatWater(
            batWater + amount
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
    // CONDITION GETTERS
    // =========================================================

    public ColonyCondition GetBatHealthCondition()
    {
        return GetConditionFromValue(
            batHealth
        );
    }

    public ColonyCondition GetBatFoodCondition()
    {
        return GetConditionFromValue(
            batFood
        );
    }

    public ColonyCondition GetBatWaterCondition()
    {
        return GetConditionFromValue(
            batWater
        );
    }

    private ColonyCondition GetConditionFromValue(
        float value)
    {
        if (value <= dangerThreshold)
        {
            return
                ColonyCondition.Danger;
        }

        if (value <= poorThreshold)
        {
            return
                ColonyCondition.Poor;
        }

        return
            ColonyCondition.Good;
    }

    // =========================================================
    // DISPLAY
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

    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugState()
    {
        if (!showDebugLogs)
        {
            return;
        }

        Debug.Log(
            "Bat Colony | Population: " +
            colonyPopulation +
            " (" +
            GetPopulationTrendText() +
            ")" +
            " | Health: " +
            batHealth +
            " | Food: " +
            batFood +
            " | Water: " +
            batWater
        );
    }

    [ContextMenu("Debug - Healthy Colony")]
    private void DebugHealthyColony()
    {
        colonyPopulation = 25;

        populationTrend =
            ColonyTrend.Growing;

        batHealth = 90f;
        batFood = 85f;
        batWater = 95f;

        DebugState();
    }

    [ContextMenu("Debug - Struggling Colony")]
    private void DebugStrugglingColony()
    {
        colonyPopulation = 12;

        populationTrend =
            ColonyTrend.Dying;

        batHealth = 55f;
        batFood = 40f;
        batWater = 20f;

        DebugState();
    }
}