using UnityEngine;
using UnityEngine.EventSystems;

public class InspectableRangerStation : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    [SerializeField] private SelectionWheel selectionWheel;

    [SerializeField] private Camera mainCamera;

    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private RangerStation rangerStation;

    // =========================================================
    // INSPECTION UI
    // =========================================================

    [Header("Inspection UI")]

    [SerializeField] private RangerStationInspectionUI inspectionUI;

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
                GetComponentInChildren<SpriteRenderer>();
        }

        if (rangerStation == null)
        {
            rangerStation =
                GetComponent<RangerStation>();
        }

        if (rangerStation == null)
        {
            rangerStation =
                GetComponentInChildren<RangerStation>();
        }

        if (selectionWheel == null)
        {
            selectionWheel =
                FindFirstObjectByType<SelectionWheel>();
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

        if (spriteRenderer != null)
        {
            normalMaterial =
                spriteRenderer.sharedMaterial;
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // =====================================================
        // WRONG MODE
        // =====================================================

        if (selectionWheel != null &&
            !selectionWheel.IsNormalMode())
        {
            ClearHover();

            return;
        }

        // =====================================================
        // SELECTION WHEEL OPEN
        // =====================================================

        if (selectionWheel != null &&
            selectionWheel.IsWheelOpen())
        {
            ClearHover();

            return;
        }

        // =====================================================
        // POINTER OVER UI
        // =====================================================

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            ClearHover();

            return;
        }

        // =====================================================
        // CAMERA
        // =====================================================

        if (mainCamera == null)
        {
            mainCamera =
                Camera.main;

            if (mainCamera == null)
            {
                return;
            }
        }

        // =====================================================
        // MOUSE WORLD POSITION
        // =====================================================

        Vector3 mouseWorld =
            mainCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        mouseWorld.z =
            transform.position.z;

        // =====================================================
        // HOVER CHECK
        // =====================================================

        bool mouseOver =
            IsMouseOver(
                mouseWorld
            );

        bool inRange =
            IsPlayerInRange();

        bool shouldHover =
            mouseOver &&
            inRange;

        if (shouldHover !=
            isHovered)
        {
            isHovered =
                shouldHover;

            RefreshMaterial();
        }

        // =====================================================
        // CLICK
        // =====================================================

        if (!allowClick)
        {
            return;
        }

        if (!isHovered)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Inspect();
        }
    }

    // =========================================================
    // MOUSE OVER
    // =========================================================

    private bool IsMouseOver(
        Vector3 mouseWorld)
    {
        if (spriteRenderer == null)
        {
            return false;
        }

        return
            spriteRenderer.bounds.Contains(
                mouseWorld
            );
    }

    // =========================================================
    // PLAYER RANGE
    // =========================================================

    private bool IsPlayerInRange()
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
    // INSPECT
    // =========================================================

    private void Inspect()
    {
        if (rangerStation == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning(
                    "InspectableRangerStation could not find RangerStation."
                );
            }

            return;
        }

        if (inspectionUI == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning(
                    "InspectableRangerStation has no RangerStationInspectionUI assigned."
                );
            }

            return;
        }

        SetInspected(
            true
        );

        inspectionUI.Open(
            rangerStation,
            transform
        );

        if (showDebugLogs)
        {
            Debug.Log(
                "Ranger Station inspected."
            );
        }
    }

    // =========================================================
    // SET INSPECTED
    // =========================================================

    public void SetInspected(
        bool value)
    {
        isInspected =
            value;

        RefreshMaterial();
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

        if (isInspected &&
            inspectedOutlineMaterial != null)
        {
            spriteRenderer.sharedMaterial =
                inspectedOutlineMaterial;

            return;
        }

        if (isHovered &&
            hoverOutlineMaterial != null)
        {
            spriteRenderer.sharedMaterial =
                hoverOutlineMaterial;

            return;
        }

        spriteRenderer.sharedMaterial =
            normalMaterial;
    }

    // =========================================================
    // CLEAR HOVER
    // =========================================================

    private void ClearHover()
    {
        if (!isHovered)
        {
            return;
        }

        isHovered =
            false;

        RefreshMaterial();
    }

    // =========================================================
    // GETTERS
    // =========================================================

    public RangerStation GetRangerStation()
    {
        return rangerStation;
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