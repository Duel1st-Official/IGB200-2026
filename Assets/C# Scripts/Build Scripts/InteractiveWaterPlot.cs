using UnityEngine;
using UnityEngine.EventSystems;

public class InteractiveWaterPlot : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private SelectionWheel selectionWheel;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private WaterPlot waterPlot;

    [Header("Inspection UI")]
    [SerializeField] private WaterPlotInspectionUI inspectionUI;

    // =========================================================
    // MATERIALS
    // =========================================================

    [Header("Outline Materials")]

    [Tooltip("Material used while simply hovering over the Water Plot.")]
    [SerializeField] private Material hoverOutlineMaterial;

    [Tooltip("Material used while this Water Plot is actively being inspected.")]
    [SerializeField] private Material inspectedOutlineMaterial;

    // =========================================================
    // INTERACTION
    // =========================================================

    [Header("Interaction")]
    [SerializeField] private bool allowClick = true;

    [Tooltip("Maximum distance the player can inspect the Water Plot from.")]
    [SerializeField] private float interactionDistance = 5f;

    // =========================================================
    // PLAYER
    // =========================================================

    [Header("Player")]
    [SerializeField] private Transform player;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // =========================================================
    // PRIVATE
    // =========================================================

    private Material normalMaterial;

    private bool isHovered;
    private bool isInspected;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // =====================================================
        // CAMERA
        // =====================================================

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // =====================================================
        // SPRITE RENDERER
        // =====================================================

        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                spriteRenderer =
                    GetComponentInChildren<SpriteRenderer>();
            }
        }

        // =====================================================
        // WATER PLOT
        // =====================================================

        if (waterPlot == null)
        {
            waterPlot =
                GetComponent<WaterPlot>();

            if (waterPlot == null)
            {
                waterPlot =
                    GetComponentInChildren<WaterPlot>();
            }
        }

        // =====================================================
        // SAVE NORMAL MATERIAL
        // =====================================================

        if (spriteRenderer != null)
        {
            normalMaterial =
                spriteRenderer.sharedMaterial;
        }
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // =====================================================
        // SELECTION WHEEL
        // =====================================================

        if (selectionWheel == null)
        {
            selectionWheel =
                FindFirstObjectByType<SelectionWheel>();
        }

        // =====================================================
        // INSPECTION UI
        // =====================================================

        if (inspectionUI == null)
        {
            inspectionUI =
                FindFirstObjectByType<WaterPlotInspectionUI>();
        }

        // =====================================================
        // PLAYER
        // =====================================================

        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag(
                    "Player"
                );

            if (playerObject != null)
            {
                player =
                    playerObject.transform;
            }
        }

        RefreshMaterial();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // =====================================================
        // ONLY INTERACT IN NORMAL MODE
        // =====================================================

        if (!IsNormalMode())
        {
            SetHovered(false);
            return;
        }

        // =====================================================
        // SELECTION WHEEL OPEN
        // =====================================================

        if (selectionWheel != null &&
            selectionWheel.IsWheelOpen())
        {
            SetHovered(false);
            return;
        }

        // =====================================================
        // HOVER
        // =====================================================

        bool hovering =
            IsMouseOverWaterPlot();

        bool withinRange =
            IsPlayerWithinRange();

        SetHovered(
            hovering &&
            withinRange
        );

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            if (isHovered)
            {
                isHovered = false;
                RefreshMaterial();
            }

            return;
        }

        // =====================================================
        // CLICK TO INSPECT
        // =====================================================

        if (isHovered &&
            allowClick &&
            Input.GetMouseButtonDown(0))
        {
            InspectWaterPlot();
        }
    }

    // =========================================================
    // NORMAL MODE
    // =========================================================

    private bool IsNormalMode()
    {
        if (selectionWheel == null)
        {
            return false;
        }

        return selectionWheel.IsNormalMode();
    }

    // =========================================================
    // MOUSE HOVER
    // =========================================================

    private bool IsMouseOverWaterPlot()
    {
        if (mainCamera == null ||
            spriteRenderer == null)
        {
            return false;
        }

        Vector3 mouseWorld =
            mainCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        Bounds bounds =
            spriteRenderer.bounds;

        mouseWorld.z =
            bounds.center.z;

        return bounds.Contains(
            mouseWorld
        );
    }

    // =========================================================
    // PLAYER RANGE
    // =========================================================

    private bool IsPlayerWithinRange()
    {
        if (player == null)
        {
            return true;
        }

        float distance =
            Vector2.Distance(
                player.position,
                transform.position
            );

        return distance <=
               interactionDistance;
    }

    // =========================================================
    // HOVER
    // =========================================================

    private void SetHovered(
        bool hovered)
    {
        if (isHovered == hovered)
        {
            return;
        }

        isHovered =
            hovered;

        RefreshMaterial();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " Hover = " +
                isHovered
            );
        }
    }

    // =========================================================
    // INSPECTED STATE
    // =========================================================

    public void SetInspected(
        bool inspected)
    {
        if (isInspected == inspected)
        {
            return;
        }

        isInspected =
            inspected;

        RefreshMaterial();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " Inspected = " +
                isInspected
            );
        }
    }

    // =========================================================
    // REFRESH MATERIAL
    // =========================================================

    private void RefreshMaterial()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        // =====================================================
        // PRIORITY 1:
        // BEING INSPECTED
        // =====================================================

        if (isInspected)
        {
            if (inspectedOutlineMaterial != null)
            {
                spriteRenderer.material =
                    inspectedOutlineMaterial;
            }
            else
            {
                spriteRenderer.material =
                    normalMaterial;
            }

            return;
        }

        // =====================================================
        // PRIORITY 2:
        // HOVER
        // =====================================================

        if (isHovered)
        {
            if (hoverOutlineMaterial != null)
            {
                spriteRenderer.material =
                    hoverOutlineMaterial;
            }
            else
            {
                spriteRenderer.material =
                    normalMaterial;
            }

            return;
        }

        // =====================================================
        // NORMAL
        // =====================================================

        spriteRenderer.material =
            normalMaterial;
    }

    // =========================================================
    // INSPECT WATER PLOT
    // =========================================================

    private void InspectWaterPlot()
    {
        if (waterPlot == null)
        {
            return;
        }

        // =====================================================
        // FIND UI AGAIN IF NEEDED
        // =====================================================

        if (inspectionUI == null)
        {
            inspectionUI =
                FindFirstObjectByType<WaterPlotInspectionUI>();
        }

        // =====================================================
        // OPEN UI
        // =====================================================

        if (inspectionUI != null)
        {
            inspectionUI.Open(
                waterPlot
            );
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.LogWarning(
                    "No WaterPlotInspectionUI found."
                );
            }

            return;
        }

        // =====================================================
        // DEBUG
        // =====================================================

        if (showDebugLogs)
        {
            Debug.Log(
                "Inspecting Water Plot | State: " +
                waterPlot.GetWaterState() +
                " | Quality: " +
                waterPlot.GetWaterQuality()
            );
        }
    }

    // =========================================================
    // CLEANING HELPERS
    // =========================================================

    public void CleanSmallAmount()
    {
        if (waterPlot == null)
        {
            return;
        }

        waterPlot.CleanWater(
            10f
        );
    }

    public void CleanMediumAmount()
    {
        if (waterPlot == null)
        {
            return;
        }

        waterPlot.CleanWater(
            25f
        );
    }

    public void CleanFully()
    {
        if (waterPlot == null)
        {
            return;
        }

        waterPlot.MakeClean();
    }

    // =========================================================
    // POLLUTION HELPERS
    // =========================================================

    public void AddSmallPollution()
    {
        if (waterPlot == null)
        {
            return;
        }

        waterPlot.PolluteWater(
            10f
        );
    }

    public void AddMediumPollution()
    {
        if (waterPlot == null)
        {
            return;
        }

        waterPlot.PolluteWater(
            25f
        );
    }

    public void PolluteFully()
    {
        if (waterPlot == null)
        {
            return;
        }

        waterPlot.MakePolluted();
    }

    // =========================================================
    // GETTERS
    // =========================================================

    public WaterPlot GetWaterPlot()
    {
        return waterPlot;
    }

    public bool IsHovered()
    {
        return isHovered;
    }

    public bool IsInspected()
    {
        return isInspected;
    }

    // =========================================================
    // DISABLE
    // =========================================================

    private void OnDisable()
    {
        isHovered =
            false;

        isInspected =
            false;

        if (spriteRenderer != null)
        {
            spriteRenderer.material =
                normalMaterial;
        }
    }
}