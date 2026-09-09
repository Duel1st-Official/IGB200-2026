using UnityEngine;

public class ToursBuilding : MonoBehaviour
{
    // =========================================================
    // REPUTATION
    // =========================================================

    [Header("Colony Reputation")]

    [Range(0f, 100f)]
    [SerializeField] private float colonyReputation = 50f;

    [Min(0)]
    [SerializeField] private int toursCompleted = 0;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // =========================================================
    // REPUTATION
    // =========================================================

    public void SetReputation(float value)
    {
        colonyReputation =
            Mathf.Clamp(
                value,
                0f,
                100f
            );

        DebugState();
    }

    public void AddReputation(float amount)
    {
        SetReputation(
            colonyReputation + amount
        );
    }

    public void RemoveReputation(float amount)
    {
        SetReputation(
            colonyReputation - amount
        );
    }

    // =========================================================
    // TOURS
    // =========================================================

    public void CompleteTour(float reputationReward)
    {
        toursCompleted++;

        AddReputation(
            reputationReward
        );

        if (showDebugLogs)
        {
            Debug.Log(
                "Tour completed! Reputation increased by " +
                reputationReward
            );
        }
    }

    // =========================================================
    // GETTERS
    // =========================================================

    public float GetReputation()
    {
        return colonyReputation;
    }

    public int GetToursCompleted()
    {
        return toursCompleted;
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
            "Tours Building | Reputation: " +
            colonyReputation +
            " | Tours Completed: " +
            toursCompleted
        );
    }

    [ContextMenu("Debug - Increase Reputation")]
    private void DebugIncreaseReputation()
    {
        AddReputation(
            10f
        );
    }

    [ContextMenu("Debug - Decrease Reputation")]
    private void DebugDecreaseReputation()
    {
        RemoveReputation(
            10f
        );
    }

    [ContextMenu("Debug - Complete Tour")]
    private void DebugCompleteTour()
    {
        CompleteTour(
            5f
        );
    }
}