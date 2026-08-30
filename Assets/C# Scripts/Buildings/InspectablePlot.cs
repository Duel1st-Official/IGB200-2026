using UnityEngine;

public class InspectablePlot : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private SelectionWheel selectionWheel;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private SpriteRenderer spriteRenderer;

    // =========================================================
    // OUTLINE
    // =========================================================

    [Header("Outline")]
    [SerializeField] private Material outlineMaterial;

    // =========================================================
    // CLICK
    // =========================================================

    [Header("Interaction")]
    [SerializeField] private bool allowClick = true;

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
    private bool outlineActive;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // =====================================================
        // FIND CAMERA
        // =====================================================

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // =====================================================
        // FIND SPRITE
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
        // AUTO FIND SELECTION WHEEL
        // =====================================================

        if (selectionWheel == null)
        {
            selectionWheel =
                FindFirstObjectByType<SelectionWheel>();
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // =====================================================
        // ONLY INSPECT DURING NORMAL MODE
        // =====================================================

        if (!IsNormalMode())
        {
            SetHovered(false);
            return;
        }

        // =====================================================
        // DISABLE WHILE TAB WHEEL IS OPEN
        // =====================================================

        if (selectionWheel != null &&
            selectionWheel.IsWheelOpen())
        {
            SetHovered(false);
            return;
        }

        // =====================================================
        // CHECK HOVER
        // =====================================================

        bool hovering =
            IsMouseOverPlot();

        SetHovered(
            hovering
        );

        // =====================================================
        // CLICK PLOT
        // =====================================================

        if (hovering &&
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

        return selectionWheel.IsNormalMode();
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

        return bounds.Contains(
            mouseWorld
        );
    }

    // =========================================================
    // SET HOVER
    // =========================================================

    private void SetHovered(
        bool hovered)
    {
        if (isHovered == hovered)
        {
            return;
        }

        isHovered = hovered;

        SetOutline(
            hovered
        );

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " Hover = " +
                hovered
            );
        }
    }

    // =========================================================
    // OUTLINE
    // =========================================================

    private void SetOutline(
        bool enabled)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (outlineActive == enabled)
        {
            return;
        }

        // =====================================================
        // OUTLINE ON
        // =====================================================

        if (enabled)
        {
            if (outlineMaterial != null)
            {
                spriteRenderer.material =
                    outlineMaterial;
            }
        }

        // =====================================================
        // OUTLINE OFF
        // =====================================================

        else
        {
            spriteRenderer.material =
                normalMaterial;
        }

        outlineActive = enabled;
    }

    // =========================================================
    // INSPECT PLOT
    // =========================================================

    private void InspectPlot()
    {
        Plot plot =
            GetComponent<Plot>();

        if (plot == null)
        {
            plot =
                GetComponentInChildren<Plot>();
        }

        // For now just confirm that
        // the correct plot was selected.
        if (showDebugLogs)
        {
            Debug.Log(
                "Inspecting plot: " +
                gameObject.name
            );
        }

        // =====================================================
        // THIS IS WHERE WE WILL OPEN
        // THE PLOT INFORMATION UI NEXT
        // =====================================================

        if (plot != null)
        {
            Debug.Log(
                "Plot selected."
            );
        }
    }

    // =========================================================
    // RESET
    // =========================================================

    private void OnDisable()
    {
        isHovered = false;
        outlineActive = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.material =
                normalMaterial;
        }
    }
}