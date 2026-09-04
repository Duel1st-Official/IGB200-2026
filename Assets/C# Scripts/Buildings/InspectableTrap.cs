using UnityEngine;
using UnityEngine.EventSystems;

public class InspectableTrap : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private SelectionWheel selectionWheel;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Trap trap;

    [Header("Inspection UI")]
    [SerializeField] private TrapInspectionUI inspectionUI;

    // =========================================================
    // OUTLINES
    // =========================================================

    [Header("Outline Materials")]

    [Tooltip("Material used when hovering over the trap.")]
    [SerializeField] private Material hoverOutlineMaterial;

    [Tooltip("Material used while the trap inspection UI is open.")]
    [SerializeField] private Material inspectedOutlineMaterial;

    // =========================================================
    // INTERACTION
    // =========================================================

    [Header("Interaction")]
    [SerializeField] private bool allowClick = true;

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
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

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

        if (trap == null)
        {
            trap =
                GetComponent<Trap>();

            if (trap == null)
            {
                trap =
                    GetComponentInChildren<Trap>();
            }
        }

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
        if (selectionWheel == null)
        {
            selectionWheel =
                FindFirstObjectByType<SelectionWheel>();
        }

        if (inspectionUI == null)
        {
            inspectionUI =
                FindFirstObjectByType<TrapInspectionUI>();
        }

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
        // WHEEL OPEN
        // =====================================================

        if (selectionWheel != null &&
            selectionWheel.IsWheelOpen())
        {
            SetHovered(false);
            return;
        }

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
        // HOVER
        // =====================================================

        bool hovering =
            IsMouseOverTrap();

        bool withinRange =
            IsPlayerWithinRange();

        SetHovered(
            hovering &&
            withinRange
        );

        // =====================================================
        // CLICK
        // =====================================================

        if (isHovered &&
            allowClick &&
            Input.GetMouseButtonDown(0))
        {
            InspectTrap();
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
    // MOUSE OVER
    // =========================================================

    private bool IsMouseOverTrap()
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
    // RANGE
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

        return
            distance <=
            interactionDistance;
    }

    // =========================================================
    // HOVER
    // =========================================================

    private void SetHovered(bool hovered)
    {
        if (isHovered == hovered)
        {
            return;
        }

        isHovered =
            hovered;

        RefreshMaterial();
    }

    // =========================================================
    // INSPECTED
    // =========================================================

    public void SetInspected(bool inspected)
    {
        if (isInspected == inspected)
        {
            return;
        }

        isInspected =
            inspected;

        RefreshMaterial();
    }

    // =========================================================
    // MATERIAL
    // =========================================================

    private void RefreshMaterial()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        // =====================================================
        // INSPECTED HAS HIGHEST PRIORITY
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
    // INSPECT
    // =========================================================

    private void InspectTrap()
    {
        if (trap == null)
        {
            return;
        }

        if (inspectionUI == null)
        {
            inspectionUI =
                FindFirstObjectByType<TrapInspectionUI>();
        }

        if (inspectionUI != null)
        {
            inspectionUI.Open(
                trap
            );
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning(
                "No TrapInspectionUI found in the scene."
            );
        }
    }

    // =========================================================
    // GETTERS
    // =========================================================

    public Trap GetTrap()
    {
        return trap;
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
        isHovered = false;

        isInspected = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.material =
                normalMaterial;
        }
    }
}