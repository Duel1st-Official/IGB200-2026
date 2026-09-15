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
    // CROP
    // =========================================================

    [Header("Crop")]

    [Tooltip(
        "Crop system attached to this plot. " +
        "If empty, one will automatically be found."
    )]
    [SerializeField]
    private CropPlot cropPlot;

    // =========================================================
    // RANGER STATION
    // =========================================================

    [Header("Environment")]

    [Tooltip(
        "Optional Ranger Station reference. " +
        "If empty, one will automatically be found."
    )]
    [SerializeField]
    private RangerStation rangerStation;

    // =========================================================
    // END DAY SYSTEM
    // =========================================================

    [Header("Action Phase")]

    [Tooltip(
        "Controls whether the player is currently allowed " +
        "to plant crops. If empty, one is automatically found."
    )]
    [SerializeField]
    private EndDaySystem endDaySystem;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLogs = false;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        AutoAssignReferences();
    }

    // =========================================================
    // AUTO ASSIGN REFERENCES
    // =========================================================

    private void AutoAssignReferences()
    {
        // -----------------------------------------------------
        // CROP
        // -----------------------------------------------------

        if (cropPlot == null)
        {
            cropPlot =
                GetComponent<CropPlot>();

            if (cropPlot == null)
            {
                cropPlot =
                    GetComponentInChildren<CropPlot>();
            }
        }

        // -----------------------------------------------------
        // RANGER STATION
        // -----------------------------------------------------

        if (rangerStation == null)
        {
            rangerStation =
                FindFirstObjectByType<RangerStation>();
        }

        // -----------------------------------------------------
        // END DAY SYSTEM
        // -----------------------------------------------------

        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }
    }

    // =========================================================
    // ACTION PHASE
    // =========================================================

    public bool CanPerformPlayerAction()
    {
        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }

        // If there is no EndDaySystem in the scene,
        // preserve the old behaviour rather than breaking
        // planting completely.
        if (endDaySystem == null)
        {
            return true;
        }

        return endDaySystem.IsActionPhaseActive();
    }

    // =========================================================
    // PLANT
    // =========================================================

    public void Plant()
    {
        // =====================================================
        // ACTION PHASE LOCK
        // =====================================================

        if (!CanPerformPlayerAction())
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    gameObject.name +
                    " cannot be planted because the action phase has ended."
                );
            }

            return;
        }

        // -----------------------------------------------------
        // ALREADY PLANTED
        // -----------------------------------------------------

        if (planted)
        {
            return;
        }

        // -----------------------------------------------------
        // SET STATE
        // -----------------------------------------------------

        planted =
            true;

        // -----------------------------------------------------
        // START CROP
        // -----------------------------------------------------

        if (cropPlot == null)
        {
            cropPlot =
                GetComponent<CropPlot>();

            if (cropPlot == null)
            {
                cropPlot =
                    GetComponentInChildren<CropPlot>();
            }
        }

        if (cropPlot != null)
        {
            cropPlot.PlantCrop();
        }

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
                gameObject.name +
                " planted."
            );
        }
    }

    // =========================================================
    // CLEAR PLOT
    // =========================================================

    public void ClearPlant()
    {
        // IMPORTANT:
        // No action-phase check here.
        //
        // This method is also used internally when crops are
        // harvested/removed, so automatic/system cleanup must
        // remain possible after the action phase ends.

        planted =
            false;

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " plant cleared."
            );
        }
    }

    // =========================================================
    // CLEAR PLOT AND CROP
    // =========================================================

    public void ClearPlantAndCrop()
    {
        // IMPORTANT:
        // This remains available to internal systems/debugging.
        // Player-facing removal is controlled by the
        // SelectionWheel / placement system.

        planted =
            false;

        if (cropPlot == null)
        {
            cropPlot =
                GetComponent<CropPlot>();

            if (cropPlot == null)
            {
                cropPlot =
                    GetComponentInChildren<CropPlot>();
            }
        }

        if (cropPlot != null)
        {
            cropPlot.ClearCrop();
        }

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " plant and crop cleared."
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

    public CropPlot GetCropPlot()
    {
        return cropPlot;
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
        ClearPlantAndCrop();
    }
}