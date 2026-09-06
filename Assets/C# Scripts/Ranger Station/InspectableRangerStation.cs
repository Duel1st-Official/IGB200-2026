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
                GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                spriteRenderer =
                    GetComponentInChildren<SpriteRenderer>();
            }
        }

        if (rangerStation == null)
        {
            rangerStation =
                GetComponent<RangerStation>();

            if (rangerStation == null)
            {
                rangerStation =
                    GetComponentInChildren<RangerStation>();
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
                FindFirstObjectByType<RangerStationInspectionUI>();
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

        // Normal mode only.
        if (!selectionWheel.IsNormalMode())
        {
            SetHovered(false);
            return;
        }

        // Selection wheel open.
        if (selectionWheel.IsWheelOpen())
        {
            SetHovered(false);
            return;
        }

        // Block interaction through UI.
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            SetHovered(false);
            return;
        }

        Vector3 mouseWorld =
            mainCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        mouseWorld.z =
            transform.position.z;

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

        if (isHovered &&
            allowClick &&
            Input.GetMouseButtonDown(0))
        {
            InspectRangerStation();
        }
    }

    // =========================================================
    // INSPECT
    // =========================================================

    private void InspectRangerStation()
    {
        if (rangerStation == null)
        {
            return;
        }

        if (inspectionUI == null)
        {
            inspectionUI =
                FindFirstObjectByType<RangerStationInspectionUI>();
        }

        if (inspectionUI == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning(
                    "No RangerStationInspectionUI found."
                );
            }

            return;
        }

        inspectionUI.Open(
            rangerStation
        );

        if (showDebugLogs)
        {
            Debug.Log(
                "Inspecting Ranger Station."
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

        if (isInspected)
        {
            spriteRenderer.sharedMaterial =
                inspectedOutlineMaterial != null
                    ? inspectedOutlineMaterial
                    : normalMaterial;

            return;
        }

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

    public RangerStation GetRangerStation()
    {
        return rangerStation;
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
            spriteRenderer.sharedMaterial =
                normalMaterial;
        }
    }
}