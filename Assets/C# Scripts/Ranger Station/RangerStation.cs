using UnityEngine;

public class RangerStation : MonoBehaviour
{
    // =========================================================
    // ENVIRONMENT VALUES
    // =========================================================

    [Header("Environment Values")]

    [Range(0f, 100f)]
    [SerializeField] private float preyAvailability = 75f;

    [Range(0f, 100f)]
    [SerializeField] private float predatorPressure = 35f;

    [Range(0f, 100f)]
    [SerializeField] private float fireRisk = 20f;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // =========================================================
    // PREY
    // =========================================================

    public void SetPreyAvailability(float value)
    {
        preyAvailability =
            Mathf.Clamp(
                value,
                0f,
                100f
            );

        DebugState();
    }

    public void AddPreyAvailability(float amount)
    {
        SetPreyAvailability(
            preyAvailability + amount
        );
    }

    // =========================================================
    // PREDATOR PRESSURE
    // =========================================================

    public void SetPredatorPressure(float value)
    {
        predatorPressure =
            Mathf.Clamp(
                value,
                0f,
                100f
            );

        DebugState();
    }

    public void AddPredatorPressure(float amount)
    {
        SetPredatorPressure(
            predatorPressure + amount
        );
    }

    // =========================================================
    // FIRE RISK
    // =========================================================

    public void SetFireRisk(float value)
    {
        fireRisk =
            Mathf.Clamp(
                value,
                0f,
                100f
            );

        DebugState();
    }

    public void AddFireRisk(float amount)
    {
        SetFireRisk(
            fireRisk + amount
        );
    }

    // =========================================================
    // GETTERS
    // =========================================================

    public float GetPreyAvailability()
    {
        return preyAvailability;
    }

    public float GetPredatorPressure()
    {
        return predatorPressure;
    }

    public float GetFireRisk()
    {
        return fireRisk;
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
            "Ranger Station | Prey: " +
            preyAvailability +
            " | Predator Pressure: " +
            predatorPressure +
            " | Fire Risk: " +
            fireRisk
        );
    }

    [ContextMenu("Debug - Good Conditions")]
    private void DebugGoodConditions()
    {
        preyAvailability = 90f;
        predatorPressure = 20f;
        fireRisk = 15f;

        DebugState();
    }

    [ContextMenu("Debug - Dangerous Conditions")]
    private void DebugDangerousConditions()
    {
        preyAvailability = 25f;
        predatorPressure = 85f;
        fireRisk = 80f;

        DebugState();
    }
}