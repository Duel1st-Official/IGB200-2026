using UnityEngine;

public class BuildItemSelector : MonoBehaviour
{
    public enum BuildItem
    {
        Plot,
        Trap,
        WaterPlot
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private SelectionWheel selectionWheel;

    [SerializeField] private PlotPlacementSystem plotPlacementSystem;
    [SerializeField] private TrapPlacementSystem trapPlacementSystem;
    [SerializeField] private WaterPlotPlacementSystem waterPlotPlacementSystem;

    // =========================================================
    // CURRENT SELECTION
    // =========================================================

    [Header("Current Build Item")]
    [SerializeField] private BuildItem currentBuildItem = BuildItem.Plot;

    // =========================================================
    // SCROLL SETTINGS
    // =========================================================

    [Header("Scroll Wheel")]
    [Tooltip("Prevents extremely sensitive mouse wheels from cycling multiple times instantly.")]
    [SerializeField] private float scrollCooldown = 0.12f;

    [Tooltip("Reverse the mouse wheel direction if desired.")]
    [SerializeField] private bool invertScroll = false;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // =========================================================
    // PRIVATE
    // =========================================================

    private bool wasInBuildMode = false;
    private float nextScrollTime = 0f;

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

        if (waterPlotPlacementSystem == null)
        {
            waterPlotPlacementSystem =
                FindFirstObjectByType<WaterPlotPlacementSystem>();
        }

        // Start with everything inactive.
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
            // Start on Plot whenever Build Mode begins.
            SelectBuildItem(BuildItem.Plot);
        }

        // =====================================================
        // JUST LEFT BUILD MODE
        // =====================================================

        if (!isBuildMode && wasInBuildMode)
        {
            DisableAllPlacementSystems();
        }

        wasInBuildMode =
            isBuildMode;

        // =====================================================
        // ONLY WORK IN BUILD MODE
        // =====================================================

        if (!isBuildMode)
        {
            return;
        }

        // =====================================================
        // DON'T SWITCH ITEMS WHILE TAB WHEEL IS OPEN
        // =====================================================

        if (selectionWheel != null &&
            selectionWheel.IsWheelOpen())
        {
            return;
        }

        // =====================================================
        // SCROLL COOLDOWN
        // =====================================================

        if (Time.unscaledTime <
            nextScrollTime)
        {
            return;
        }

        // =====================================================
        // READ MOUSE WHEEL
        // =====================================================

        float scroll =
            Input.mouseScrollDelta.y;

        if (Mathf.Abs(scroll) <
            0.01f)
        {
            return;
        }

        if (invertScroll)
        {
            scroll *= -1f;
        }

        // =====================================================
        // SCROLL UP
        // =====================================================

        if (scroll > 0f)
        {
            PreviousBuildItem();
        }

        // =====================================================
        // SCROLL DOWN
        // =====================================================

        else if (scroll < 0f)
        {
            NextBuildItem();
        }

        nextScrollTime =
            Time.unscaledTime +
            scrollCooldown;
    }

    // =========================================================
    // NEXT ITEM
    // =========================================================

    private void NextBuildItem()
    {
        switch (currentBuildItem)
        {
            case BuildItem.Plot:
                SelectBuildItem(
                    BuildItem.Trap
                );
                break;

            case BuildItem.Trap:
                SelectBuildItem(
                    BuildItem.WaterPlot
                );
                break;

            case BuildItem.WaterPlot:
                SelectBuildItem(
                    BuildItem.Plot
                );
                break;
        }
    }

    // =========================================================
    // PREVIOUS ITEM
    // =========================================================

    private void PreviousBuildItem()
    {
        switch (currentBuildItem)
        {
            case BuildItem.Plot:
                SelectBuildItem(
                    BuildItem.WaterPlot
                );
                break;

            case BuildItem.Trap:
                SelectBuildItem(
                    BuildItem.Plot
                );
                break;

            case BuildItem.WaterPlot:
                SelectBuildItem(
                    BuildItem.Trap
                );
                break;
        }
    }

    // =========================================================
    // SELECT BUILD ITEM
    // =========================================================

    public void SelectBuildItem(
        BuildItem item)
    {
        currentBuildItem =
            item;

        // First turn everything off.
        DisableAllPlacementSystems();

        // =====================================================
        // PLOT
        // =====================================================

        if (currentBuildItem ==
            BuildItem.Plot)
        {
            if (plotPlacementSystem != null)
            {
                plotPlacementSystem
                    .ActivatePlotPlacement();
            }
        }

        // =====================================================
        // TRAP
        // =====================================================

        else if (currentBuildItem ==
                 BuildItem.Trap)
        {
            if (trapPlacementSystem != null)
            {
                trapPlacementSystem
                    .ActivateTrapPlacement();
            }
        }

        // =====================================================
        // WATER PLOT
        // =====================================================

        else if (currentBuildItem ==
                 BuildItem.WaterPlot)
        {
            if (waterPlotPlacementSystem != null)
            {
                waterPlotPlacementSystem
                    .ActivateWaterPlotPlacement();
            }
        }

        // =====================================================
        // DEBUG
        // =====================================================

        if (showDebugLogs)
        {
            Debug.Log(
                "Build Item Selected: " +
                currentBuildItem
            );
        }
    }

    // =========================================================
    // DISABLE ALL
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

        if (waterPlotPlacementSystem != null)
        {
            waterPlotPlacementSystem
                .DeactivateWaterPlotPlacement();
        }
    }

    // =========================================================
    // PUBLIC SELECTORS
    // =========================================================

    public void SelectPlot()
    {
        SelectBuildItem(
            BuildItem.Plot
        );
    }

    public void SelectTrap()
    {
        SelectBuildItem(
            BuildItem.Trap
        );
    }

    public void SelectWaterPlot()
    {
        SelectBuildItem(
            BuildItem.WaterPlot
        );
    }

    // =========================================================
    // GETTERS
    // =========================================================

    public BuildItem GetCurrentBuildItem()
    {
        return currentBuildItem;
    }

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

    public bool IsWaterPlotSelected()
    {
        return currentBuildItem ==
               BuildItem.WaterPlot;
    }
}