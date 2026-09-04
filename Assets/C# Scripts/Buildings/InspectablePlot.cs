using UnityEngine;
using UnityEngine.EventSystems;

public class InspectablePlot : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private SelectionWheel selectionWheel;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Plot plot;

    [Header("Inspection UI")]
    [SerializeField] private PlotInspectionUI inspectionUI;

    // =========================================================
    // OUTLINE MATERIALS
    // =========================================================

    [Header("Outline Materials")]

    [Tooltip("Material used when the mouse is hovering over the plot.")]
    [SerializeField] private Material hoverOutlineMaterial;

    [Tooltip("Material used while this plot is actively being inspected.")]
    [SerializeField] private Material inspectedOutlineMaterial;

    // =========================================================
    // INTERACTION
    // =========================================================

    [Header("Interaction")]

    [SerializeField] private bool allowClick = true;

    [Tooltip("Maximum distance the player can inspect the plot from.")]
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
        // PLOT
        // =====================================================

        if (plot == null)
        {
            plot =
                GetComponent<Plot>();

            if (plot == null)
            {
                plot =
                    GetComponentInChildren<Plot>();
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
                FindFirstObjectByType<PlotInspectionUI>();
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
        // NORMAL MODE ONLY
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

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            isHovered = false;
            RefreshMaterial();
            return;
        }

        // =====================================================
        // HOVER CHECK
        // =====================================================

        bool hovering =
            IsMouseOverPlot();

        bool withinRange =
            IsPlayerWithinRange();

        SetHovered(
            hovering &&
            withinRange
        );

        // =====================================================
        // CLICK TO INSPECT
        // =====================================================

        if (isHovered &&
            allowClick &&
            Input.GetMouseButtonDown(0))
        {
            InspectPlot();
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

        return
            selectionWheel.IsNormalMode();
    }

    // =========================================================
    // MOUSE OVER PLOT
    // =========================================================

    private bool IsMouseOverPlot()
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

        return
            bounds.Contains(
                mouseWorld
            );
    }

    // =========================================================
    // PLAYER RANGE
    // =========================================================

    private bool IsPlayerWithinRange()
    {
        // If player wasn't found,
        // allow interaction rather than breaking it.

        if (player == null)
        {
            return true;
        }

        float distance =
            Vector2.Distance(
                player.position,
                transform.position
            );

        return
            distance <=
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
                " Plot Hover = " +
                hovered
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
                " Plot Inspected = " +
                inspected
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
        // INSPECTED
        //
        // Highest priority.
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
        // HOVERED
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
    // INSPECT PLOT
    // =========================================================

    private void InspectPlot()
    {
        if (plot == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning(
                    gameObject.name +
                    " has no Plot component."
                );
            }

            return;
        }

        // =====================================================
        // FIND UI AGAIN IF NECESSARY
        // =====================================================

        if (inspectionUI == null)
        {
            inspectionUI =
                FindFirstObjectByType<PlotInspectionUI>();
        }

        // =====================================================
        // OPEN UI
        // =====================================================

        if (inspectionUI != null)
        {
            inspectionUI.Open(
                plot
            );
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning(
                "No PlotInspectionUI found in the scene."
            );
        }

        // =====================================================
        // DEBUG
        // =====================================================

        if (showDebugLogs)
        {
            Debug.Log(
                "Inspecting Plot | " +
                "Occupied: " +
                plot.occupied +
                " | Planted: " +
                plot.planted
            );
        }
    }

    // =========================================================
    // GETTERS
    // =========================================================

    public Plot GetPlot()
    {
        return plot;
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