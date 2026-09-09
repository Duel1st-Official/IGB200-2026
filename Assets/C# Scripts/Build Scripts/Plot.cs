using UnityEngine;

public class Plot : MonoBehaviour
{
    // =========================================================
    // PLOT STATE
    // =========================================================

    [Header("Plot State")]

    public bool occupied;

    public bool planted;

    // =========================================================
    // RANGER STATION
    // =========================================================

    [Header("Environment")]

    [Tooltip(
        "Optional Ranger Station reference. " +
        "If empty, one will automatically be found."
    )]
    [SerializeField] private RangerStation rangerStation;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (rangerStation == null)
        {
            rangerStation =
                FindFirstObjectByType<RangerStation>();
        }
    }

    // =========================================================
    // PLANT
    // =========================================================

    public void Plant()
    {
        // Don't damage the soil repeatedly
        // if this plot has already been planted.
        if (planted)
        {
            return;
        }

        planted =
            true;

        // =====================================================
        // DAMAGE SOIL
        // =====================================================

        if (rangerStation == null)
        {
            rangerStation =
                FindFirstObjectByType<RangerStation>();
        }

        if (rangerStation != null)
        {
            rangerStation.PlotPlanted();
        }

        // =====================================================
        // DEBUG
        // =====================================================

        if (showDebugLogs)
        {
            Debug.Log(
                "Plot planted."
            );
        }
    }

    // =========================================================
    // CLEAR PLOT
    // =========================================================

    public void ClearPlant()
    {
        planted =
            false;

        if (showDebugLogs)
        {
            Debug.Log(
                "Plot cleared."
            );
        }
    }

    // =========================================================
    // OCCUPIED
    // =========================================================

    public void SetOccupied(
        bool value)
    {
        occupied =
            value;
    }

    // =========================================================
    // CHECKS
    // =========================================================

    public bool IsPlanted()
    {
        return planted;
    }

    public bool IsOccupied()
    {
        return occupied;
    }

    // =========================================================
    // DEBUG
    // =========================================================

    [ContextMenu("Debug - Plant")]
    private void DebugPlant()
    {
        Plant();
    }

    [ContextMenu("Debug - Clear Plant")]
    private void DebugClearPlant()
    {
        ClearPlant();
    }
}