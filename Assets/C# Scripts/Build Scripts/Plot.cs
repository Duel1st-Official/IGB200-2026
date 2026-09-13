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
    }

    // =========================================================
    // PLANT
    // =========================================================

    public void Plant()
    {
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