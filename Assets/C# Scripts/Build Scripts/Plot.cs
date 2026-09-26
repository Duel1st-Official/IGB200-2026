using UnityEngine;

public class Plot : MonoBehaviour
{
    [Header("Destroyed plot / debris")]
    [SerializeField] private bool destroyed;
    [SerializeField] private Sprite debrisSprite;
    [SerializeField] private SpriteRenderer debrisRenderer;
    [SerializeField] private string destructionCause;
    [SerializeField] private int criticalSoilDays;
    private int soilCheckedDay = -1;
    public bool IsDestroyed() { return destroyed; }
    public string GetDestructionCause() { return destructionCause; }

    private void Start()
    {
        soilCheckedDay = endDaySystem != null ? endDaySystem.GetCurrentDay() : -1;
        if (destroyed) { DiscardContents(false); ApplyDebrisVisual(); occupied = true; }
    }
    private void Update() { CheckSoilDamage(); }
    public void CheckSoilDamage()
    {
        if (destroyed || endDaySystem == null || endDaySystem.HasGameEnded()) return;
        int day = endDaySystem.GetCurrentDay();
        if (day < 1) return;
        if (soilCheckedDay < 1 || day < soilCheckedDay) { soilCheckedDay = day; criticalSoilDays = 0; return; }
        if (day == soilCheckedDay) return;
        soilCheckedDay = day;
        if (rangerStation == null) rangerStation = FindFirstObjectByType<RangerStation>();
        if (rangerStation == null) return;
        PlotDisasterSystem settings = PlotDisasterSystem.GetOrCreate();
        criticalSoilDays = rangerStation.GetSoilHealth() <= settings.criticalSoilHealth ? criticalSoilDays + 1 : 0;
        if (criticalSoilDays >= Mathf.Max(1, settings.consecutiveCriticalDays)) DestroyPlot("Critical soil health");
    }
    public bool DestroyPlot(string cause)
    {
        if (destroyed) return false;
        AutoAssignReferences();
        destroyed = true; destructionCause = cause;
        DiscardContents(false);
        occupied = true; planted = false;
        ApplyDebrisVisual();
        return true;
    }
    public void ClearContentsForRemoval()
    {
        DiscardContents(true);
    }
    private void DiscardContents(bool removing)
    {
        AutoAssignReferences();
        if (cropPlot != null) cropPlot.ClearCrop();
        foreach (Trap trap in FindObjectsByType<Trap>(FindObjectsSortMode.None))
            if (trap.GetOwningPlot() == this)
            {
                if (removing) trap.RemoveWithPlot(); else trap.DestroyWithPlot();
            }
    }
    private void ApplyDebrisVisual()
    {
        if (debrisRenderer == null) debrisRenderer = GetComponent<SpriteRenderer>();
        if (debrisRenderer == null) debrisRenderer = GetComponentInChildren<SpriteRenderer>();
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>())
            if (renderer != debrisRenderer) renderer.enabled = false;
        if (debrisRenderer != null)
        {
            if (debrisSprite != null) debrisRenderer.sprite = debrisSprite;
            else if (cropPlot != null && cropPlot.GetEmptyPlotSprite() != null)
                debrisRenderer.sprite = cropPlot.GetEmptyPlotSprite();
            debrisRenderer.color = debrisSprite != null ? Color.white : new Color(0.25f, 0.19f, 0.17f);
            debrisRenderer.enabled = true;
        }
    }
    [ContextMenu("Debug - Destroy Plot")]
    private void DebugDestroyPlot() { DestroyPlot("Debug"); }


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
        if (destroyed) return false;
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
            destroyed || value;
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
        return destroyed || occupied;
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
