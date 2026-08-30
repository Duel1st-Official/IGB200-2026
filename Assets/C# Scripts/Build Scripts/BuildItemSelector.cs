using UnityEngine;

public class BuildItemSelector : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private SelectionWheel selectionWheel;

    [SerializeField] private PlotPlacementSystem plotPlacementSystem;
    [SerializeField] private TrapPlacementSystem trapPlacementSystem;

    // =========================================================
    // DEFAULT SELECTION
    // =========================================================

    [Header("Default Build Item")]
    [Tooltip("When entering Build Mode, Plot will be selected by default.")]
    [SerializeField] private bool defaultToPlot = true;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // =========================================================
    // BUILD ITEM
    // =========================================================

    public enum BuildItem
    {
        Plot,
        Trap
    }

    [Header("Current Selection")]
    [SerializeField] private BuildItem currentBuildItem = BuildItem.Plot;

    // =========================================================
    // PRIVATE
    // =========================================================

    private bool wasInBuildMode = false;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // -----------------------------------------------------
        // AUTO FIND SELECTION WHEEL
        // -----------------------------------------------------

        if (selectionWheel == null)
        {
            selectionWheel =
                FindFirstObjectByType<SelectionWheel>();
        }

        // -----------------------------------------------------
        // AUTO FIND PLACEMENT SYSTEMS
        // -----------------------------------------------------

        if (plotPlacementSystem == null)
        {
            plotPlacementSystem =
                FindFirstObjectByType<PlotPlacementSystem>();
        }

        if (trapPlacementSystem == null)
        {
            trapPlacementSystem =
                FindFirstObjectByType<TrapPlacementSystem>();
        }

        // Start disabled until Build Mode is active.
        DisableAllPlacementSystems();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        bool isBuildMode =
            selectionWheel != null &&
            selectionWheel.IsBuildMode();

        // =====================================================
        // JUST ENTERED BUILD MODE
        // =====================================================

        if (isBuildMode && !wasInBuildMode)
        {
            if (defaultToPlot)
            {
                SelectPlot();
            }
            else
            {
                SelectTrap();
            }
        }

        // =====================================================
        // LEFT BUILD MODE
        // =====================================================

        if (!isBuildMode && wasInBuildMode)
        {
            DisableAllPlacementSystems();
        }

        wasInBuildMode =
            isBuildMode;

        // =====================================================
        // NOT BUILD MODE
        // =====================================================

        if (!isBuildMode)
        {
            return;
        }

        // =====================================================
        // DON'T CHANGE WHILE WHEEL IS OPEN
        // =====================================================

        if (selectionWheel.IsWheelOpen())
        {
            return;
        }

        // =====================================================
        // 1 = PLOT
        // =====================================================

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectPlot();
        }

        // =====================================================
        // 2 = TRAP
        // =====================================================

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectTrap();
        }
    }

    // =========================================================
    // SELECT PLOT
    // =========================================================

    public void SelectPlot()
    {
        currentBuildItem =
            BuildItem.Plot;

        // Trap OFF
        if (trapPlacementSystem != null)
        {
            trapPlacementSystem
                .DeactivateTrapPlacement();
        }

        // Plot ON
        if (plotPlacementSystem != null)
        {
            plotPlacementSystem
                .ActivatePlotPlacement();
        }

        if (showDebugLogs)
        {
            Debug.Log(
                "Build Item Selected: PLOT"
            );
        }
    }

    // =========================================================
    // SELECT TRAP
    // =========================================================

    public void SelectTrap()
    {
        currentBuildItem =
            BuildItem.Trap;

        // Plot OFF
        if (plotPlacementSystem != null)
        {
            plotPlacementSystem
                .DeactivatePlotPlacement();
        }

        // Trap ON
        if (trapPlacementSystem != null)
        {
            trapPlacementSystem
                .ActivateTrapPlacement();
        }

        if (showDebugLogs)
        {
            Debug.Log(
                "Build Item Selected: TRAP"
            );
        }
    }

    // =========================================================
    // DISABLE EVERYTHING
    // =========================================================

    private void DisableAllPlacementSystems()
    {
        if (plotPlacementSystem != null)
        {
            plotPlacementSystem
                .DeactivatePlotPlacement();
        }

        if (trapPlacementSystem != null)
        {
            trapPlacementSystem
                .DeactivateTrapPlacement();
        }
    }

    // =========================================================
    // GET CURRENT BUILD ITEM
    // =========================================================

    public BuildItem GetCurrentBuildItem()
    {
        return currentBuildItem;
    }

    // =========================================================
    // CHECK CURRENT ITEM
    // =========================================================

    public bool IsPlotSelected()
    {
        return currentBuildItem ==
               BuildItem.Plot;
    }

    public bool IsTrapSelected()
    {
        return currentBuildItem ==
               BuildItem.Trap;
    }
}