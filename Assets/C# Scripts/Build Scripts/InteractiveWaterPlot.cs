using UnityEngine;

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

    [Tooltip("Maximum distance the player can interact with the water plot.")]
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

    private bool isHovered = false;
    private bool outlineActive = false;

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
        // =====================================================
        // FIND SELECTION WHEEL
        // =====================================================

        if (selectionWheel == null)
        {
            selectionWheel =
                FindFirstObjectByType<SelectionWheel>();
        }

        // =====================================================
        // FIND PLAYER
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
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // =====================================================
        // ONLY ACTIVE IN NORMAL MODE
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
        // CHECK CURSOR
        // =====================================================

        bool hovering =
            IsMouseOverWaterPlot();

        // =====================================================
        // CHECK PLAYER DISTANCE
        // =====================================================

        bool withinRange =
            IsPlayerWithinRange();

        // Only outline if:
        //
        // 1. Mouse is over water plot
        // 2. Player is close enough

        SetHovered(
            hovering &&
            withinRange
        );

        // =====================================================
        // LEFT CLICK
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

        // =====================================================
        // SCREEN -> WORLD
        // =====================================================

        Vector3 mouseWorld =
            mainCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        // =====================================================
        // SPRITE BOUNDS
        // =====================================================

        Bounds bounds =
            spriteRenderer.bounds;

        // Bounds.Contains checks Z as well.
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

        SetOutline(
            hovered
        );

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " Water Plot Hover = " +
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
            else if (showDebugLogs)
            {
                Debug.LogWarning(
                    gameObject.name +
                    " has no Outline Material assigned."
                );
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

        outlineActive =
            enabled;
    }

    // =========================================================
    // INSPECT WATER PLOT
    // =========================================================

    private void InspectWaterPlot()
    {
        if (waterPlot == null)
        {
            Debug.LogWarning(
                gameObject.name +
                " has no WaterPlot component."
            );

            return;
        }

        // =====================================================
        // GET WATER INFORMATION
        // =====================================================

        WaterPlot.WaterState state =
            waterPlot.GetWaterState();

        float quality =
            waterPlot.GetWaterQuality();

        // =====================================================
        // DEBUG
        // =====================================================

        if (showDebugLogs)
        {
            Debug.Log(
                "Inspecting Water Plot: " +
                gameObject.name +
                " | State: " +
                state +
                " | Quality: " +
                quality.ToString("0") +
                "%"
            );
        }

        // =====================================================
        // STATE RESPONSE
        // =====================================================

        switch (state)
        {
            // =================================================
            // CLEAN
            // =================================================

            case WaterPlot.WaterState.Clean:

                if (showDebugLogs)
                {
                    Debug.Log(
                        "Water is CLEAN. " +
                        "Good water source for the habitat."
                    );
                }

                break;

            // =================================================
            // DIRTY
            // =================================================

            case WaterPlot.WaterState.Dirty:

                if (showDebugLogs)
                {
                    Debug.Log(
                        "Water is DIRTY. " +
                        "Water quality should be improved."
                    );
                }

                break;

            // =================================================
            // POLLUTED
            // =================================================

            case WaterPlot.WaterState.Polluted:

                if (showDebugLogs)
                {
                    Debug.Log(
                        "Water is POLLUTED. " +
                        "The water source needs attention."
                    );
                }

                break;
        }

        // =====================================================
        // LATER:
        //
        // WaterPlotUI.Open(waterPlot);
        //
        // This is where your actual inspection
        // panel can be opened.
        // =====================================================
    }

    // =========================================================
    // PUBLIC CLEANING INTERACTIONS
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
    // PUBLIC POLLUTION INTERACTIONS
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
}