using UnityEngine;

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

    // =========================================================
    // OUTLINE
    // =========================================================

    [Header("Outline")]
    [SerializeField] private Material outlineMaterial;

    // =========================================================
    // INTERACTION
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

    private bool isHovered = false;
    private bool outlineActive = false;

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
        // FIND SPRITE RENDERER
        // =====================================================

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                spriteRenderer =
                    GetComponentInChildren<SpriteRenderer>();
            }
        }

        // =====================================================
        // FIND TRAP
        // =====================================================

        if (trap == null)
        {
            trap = GetComponent<Trap>();

            if (trap == null)
            {
                trap = GetComponentInChildren<Trap>();
            }
        }

        // =====================================================
        // SAVE ORIGINAL MATERIAL
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
        // Automatically find SelectionWheel.
        // Useful because traps are spawned as prefabs.

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
        // ONLY WORK IN NORMAL MODE
        // =====================================================

        if (!IsNormalMode())
        {
            SetHovered(false);
            return;
        }

        // =====================================================
        // DISABLE WHILE SELECTION WHEEL IS OPEN
        // =====================================================

        if (selectionWheel != null &&
            selectionWheel.IsWheelOpen())
        {
            SetHovered(false);
            return;
        }

        // =====================================================
        // CHECK CURSOR
        // =====================================================

        bool hovering =
            IsMouseOverTrap();

        SetHovered(hovering);

        // =====================================================
        // LEFT CLICK
        // =====================================================

        if (hovering &&
            allowClick &&
            Input.GetMouseButtonDown(0))
        {
            InspectTrap();
        }
    }

    // =========================================================
    // NORMAL MODE CHECK
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
    // MOUSE HOVER CHECK
    // =========================================================

    private bool IsMouseOverTrap()
    {
        if (mainCamera == null ||
            spriteRenderer == null)
        {
            return false;
        }

        // Convert screen cursor position
        // into world position.

        Vector3 mouseWorld =
            mainCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        // Get the actual visual bounds
        // of the trap sprite.

        Bounds bounds =
            spriteRenderer.bounds;

        // Match Z so Bounds.Contains works.
        mouseWorld.z =
            bounds.center.z;

        return bounds.Contains(mouseWorld);
    }

    // =========================================================
    // SET HOVER STATE
    // =========================================================

    private void SetHovered(bool hovered)
    {
        if (isHovered == hovered)
        {
            return;
        }

        isHovered = hovered;

        SetOutline(hovered);

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " Trap Hover = " +
                hovered
            );
        }
    }

    // =========================================================
    // OUTLINE
    // =========================================================

    private void SetOutline(bool enabled)
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
        // TURN OUTLINE ON
        // =====================================================

        if (enabled)
        {
            if (outlineMaterial != null)
            {
                spriteRenderer.material =
                    outlineMaterial;
            }
            else if (showDebugLogs)
            {
                Debug.LogWarning(
                    gameObject.name +
                    " has no Outline Material assigned."
                );
            }
        }

        // =====================================================
        // TURN OUTLINE OFF
        // =====================================================

        else
        {
            spriteRenderer.material =
                normalMaterial;
        }

        outlineActive = enabled;
    }

    // =========================================================
    // INSPECT TRAP
    // =========================================================

    private void InspectTrap()
    {
        if (trap == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning(
                    gameObject.name +
                    " has no Trap component."
                );
            }

            return;
        }

        // =====================================================
        // READ CURRENT TRAP STATE
        // =====================================================

        Trap.TrapState state =
            trap.GetState();

        if (showDebugLogs)
        {
            Debug.Log(
                "Inspecting Trap: " +
                gameObject.name +
                " | State: " +
                state
            );
        }

        // =====================================================
        // STATE-SPECIFIC DEBUG
        // =====================================================

        switch (state)
        {
            case Trap.TrapState.Empty:

                if (showDebugLogs)
                {
                    Debug.Log(
                        "Trap is EMPTY."
                    );
                }

                break;


            case Trap.TrapState.Set:

                if (showDebugLogs)
                {
                    Debug.Log(
                        "Trap is SET and waiting."
                    );
                }

                break;


            case Trap.TrapState.Caught:

                if (showDebugLogs)
                {
                    Debug.Log(
                        "Trap has CAUGHT an animal."
                    );
                }

                break;
        }

        // =====================================================
        // LATER:
        //
        // Open your Trap Inspection UI here.
        //
        // Example:
        //
        // trapUI.OpenTrap(trap);
        //
        // =====================================================
    }

    // =========================================================
    // PUBLIC INFORMATION
    // =========================================================

    public bool IsHovered()
    {
        return isHovered;
    }

    public Trap GetTrap()
    {
        return trap;
    }

    // =========================================================
    // DISABLE
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

    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        // Make sure we don't leave
        // an instantiated outline material around.

        if (spriteRenderer != null)
        {
            spriteRenderer.material =
                normalMaterial;
        }
    }
}