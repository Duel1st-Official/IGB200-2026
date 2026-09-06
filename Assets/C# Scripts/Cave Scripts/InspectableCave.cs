using UnityEngine;
using UnityEngine.EventSystems;

public class InspectableCave : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private SelectionWheel selectionWheel;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private BatColony batColony;

    // =========================================================
    // INSPECTION UI
    // =========================================================

    [Header("Inspection UI")]
    [SerializeField] private CaveInspectionUI inspectionUI;

    // =========================================================
    // OUTLINE MATERIALS
    // =========================================================

    [Header("Outline Materials")]
    [SerializeField] private Material hoverOutlineMaterial;
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
            mainCamera =
                Camera.main;
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

        if (batColony == null)
        {
            batColony =
                GetComponent<BatColony>();

            if (batColony == null)
            {
                batColony =
                    GetComponentInChildren<BatColony>();
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
                FindFirstObjectByType<CaveInspectionUI>();
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
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (selectionWheel == null ||
            mainCamera == null ||
            spriteRenderer == null)
        {
            return;
        }

        // =====================================================
        // NORMAL MODE ONLY
        // =====================================================

        if (!selectionWheel.IsNormalMode())
        {
            SetHovered(
                false
            );

            return;
        }

        // =====================================================
        // SELECTION WHEEL OPEN
        // =====================================================

        if (selectionWheel.IsWheelOpen())
        {
            SetHovered(
                false
            );

            return;
        }

        // =====================================================
        // BLOCK WORLD INTERACTION BEHIND UI
        // =====================================================

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            SetHovered(
                false
            );

            return;
        }

        // =====================================================
        // MOUSE POSITION
        // =====================================================

        Vector3 mouseWorld =
            mainCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        mouseWorld.z =
            transform.position.z;

        // =====================================================
        // HOVER
        // =====================================================

        bool mouseOver =
            spriteRenderer.bounds.Contains(
                mouseWorld
            );

        bool inRange =
            IsWithinInteractionDistance();

        SetHovered(
            mouseOver &&
            inRange
        );

        // =====================================================
        // LEFT CLICK
        // =====================================================

        if (isHovered &&
            allowClick &&
            Input.GetMouseButtonDown(0))
        {
            InspectCave();
        }
    }

    // =========================================================
    // INSPECT
    // =========================================================

    private void InspectCave()
    {
        if (batColony == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning(
                    "InspectableCave has no BatColony assigned."
                );
            }

            return;
        }

        if (inspectionUI == null)
        {
            inspectionUI =
                FindFirstObjectByType<CaveInspectionUI>();
        }

        if (inspectionUI == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning(
                    "No CaveInspectionUI found."
                );
            }

            return;
        }

        inspectionUI.Open(
            batColony
        );

        if (showDebugLogs)
        {
            Debug.Log(
                "Inspecting Bat Cave."
            );
        }
    }

    // =========================================================
    // RANGE
    // =========================================================

    private bool IsWithinInteractionDistance()
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

    private void SetHovered(
        bool hovered)
    {
        if (isHovered ==
            hovered)
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

    public void SetInspected(
        bool inspected)
    {
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

        // Inspected has highest priority.
        if (isInspected)
        {
            spriteRenderer.sharedMaterial =
                inspectedOutlineMaterial != null
                    ? inspectedOutlineMaterial
                    : normalMaterial;

            return;
        }

        // Hover second.
        if (isHovered)
        {
            spriteRenderer.sharedMaterial =
                hoverOutlineMaterial != null
                    ? hoverOutlineMaterial
                    : normalMaterial;

            return;
        }

        spriteRenderer.sharedMaterial =
            normalMaterial;
    }

    // =========================================================
    // GETTERS
    // =========================================================

    public BatColony GetBatColony()
    {
        return batColony;
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
            spriteRenderer.sharedMaterial =
                normalMaterial;
        }
    }
}