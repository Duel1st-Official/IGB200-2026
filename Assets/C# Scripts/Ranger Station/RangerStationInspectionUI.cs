using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class RangerStationInspectionUI :
    MonoBehaviour,
    IInspectionPanel
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    [SerializeField] private Camera mainCamera;

    [SerializeField] private Canvas canvas;

    [SerializeField] private SelectionWheel selectionWheel;

    [Tooltip("The entire Ranger Station inspection window.")]
    [SerializeField] private RectTransform panel;

    [Tooltip("The top/header area used to drag the window.")]
    [SerializeField] private RectTransform dragHandle;

    [SerializeField] private CanvasGroup canvasGroup;

    // =========================================================
    // TEXT
    // =========================================================

    [Header("Text")]

    [SerializeField] private TMP_Text titleText;

    [SerializeField] private TMP_Text preyValueText;

    [SerializeField] private TMP_Text predatorValueText;

    [SerializeField] private TMP_Text fireRiskValueText;

    [SerializeField] private TMP_Text soilHealthValueText;

    // =========================================================
    // ICON
    // =========================================================

    [Header("Icon")]

    [SerializeField] private Image rangerStationIcon;

    // =========================================================
    // RADIAL ENVIRONMENT DASHBOARD
    // =========================================================

    [Header("Radial Environment Dashboard")]
    [Tooltip("Coloured quarter arc for Prey Availability.")]
    [SerializeField] private Image preyRadialFill;

    [Tooltip("Coloured quarter arc for Predator Pressure.")]
    [SerializeField] private Image predatorRadialFill;

    [Tooltip("Coloured quarter arc for Fire Risk.")]
    [SerializeField] private Image fireRiskRadialFill;

    [Tooltip("Coloured quarter arc for Soil Health.")]
    [SerializeField] private Image soilHealthRadialFill;

    [Header("Radial Arc Shader")]
    [Tooltip("Material using GhostBat/UI/QuarterArcFill. The same shader/material setup used by the Cave UI.")]
    [SerializeField] private Material radialArcMaterial;

    [Header("Radial Arc Angles")]
    [Tooltip("Top-left quarter. Clockwise fill begins at the left edge.")]
    [Range(0f, 360f)][SerializeField] private float preyStartAngle = 180f;

    [Tooltip("Top-right quarter. Clockwise fill begins at the top edge.")]
    [Range(0f, 360f)][SerializeField] private float predatorStartAngle = 90f;

    [Tooltip("Bottom-left quarter. Clockwise fill begins at the bottom edge.")]
    [Range(0f, 360f)][SerializeField] private float fireRiskStartAngle = 270f;

    [Tooltip("Bottom-right quarter. Clockwise fill begins at the right edge.")]
    [Range(0f, 360f)][SerializeField] private float soilHealthStartAngle = 0f;

    [Range(1f, 180f)][SerializeField] private float radialArcAngle = 90f;
    [SerializeField] private bool fillClockwise = true;

    [Header("Radial Icons")]
    [SerializeField] private RectTransform preyIcon;
    [SerializeField] private RectTransform predatorIcon;
    [SerializeField] private RectTransform fireRiskIcon;
    [SerializeField] private RectTransform soilHealthIcon;

    [Header("Radial Background")]
    [Tooltip("The brown segmented ring behind all four coloured arcs. It fades in but never changes size.")]
    [SerializeField] private RectTransform ringBackground;

    [Header("Radial Hover")]
    [SerializeField] private bool enableRadialHover = true;

    [Tooltip("Optional unmasked parent roots for each quarter. Assign these if the fill Images sit inside a Mask/RectMask2D. The root will enlarge instead of the clipped fill itself.")]
    [SerializeField] private RectTransform preyHoverRoot;
    [SerializeField] private RectTransform predatorHoverRoot;
    [SerializeField] private RectTransform fireRiskHoverRoot;
    [SerializeField] private RectTransform soilHealthHoverRoot;
    [Min(1f)][SerializeField] private float hoveredScale = 1.15f;
    [Min(1f)][SerializeField] private float hoveredIconScale = 1.18f;
    [Min(0.01f)][SerializeField] private float hoverScaleSpeed = 14f;
    [Range(0f, 1f)][SerializeField] private float nonHoveredSaturation = 0f;
    [Tooltip("Optional single text field used to show e.g. '10% Fire Risk'. If empty, the hovered stat's own value text is used.")]
    [SerializeField] private TMP_Text hoverStatText;

    [Header("Legacy Sliders - Optional")]
    [Tooltip("These can be left empty when using the radial dashboard.")]
    [SerializeField] private Slider preySlider;
    [SerializeField] private Slider predatorSlider;
    [SerializeField] private Slider fireRiskSlider;
    [SerializeField] private Slider soilHealthSlider;

    // =========================================================
    // BUTTONS
    // =========================================================

    [Header("Buttons")]

    [SerializeField] private Button closeButton;

    // =========================================================
    // POSITION
    // =========================================================

    [Header("Ranger Station Position")]

    [SerializeField] private float horizontalOffset = 230f;

    [SerializeField] private float verticalOffset = 30f;

    [SerializeField] private float followSpeed = 15f;

    [SerializeField] private bool automaticallyFlipSide = true;

    [SerializeField] private float screenEdgePadding = 180f;

    // =========================================================
    // DRAGGING
    // =========================================================

    [Header("Dragging")]

    [SerializeField] private bool allowDragging = true;

    [SerializeField] private bool stopFollowingAfterDrag = true;

    [SerializeField] private bool clampToCanvas = true;

    [SerializeField] private float canvasPadding = 10f;

    // =========================================================
    // DRAG VISUALS
    // =========================================================

    [Header("Drag Visuals")]

    [SerializeField] private float dragScale = 0.92f;

    [SerializeField] private float dragScaleSpeed = 12f;

    [SerializeField] private float maxDragSwayAngle = 7f;

    [SerializeField] private float swayStrength = 0.3f;

    [SerializeField] private float swaySmoothSpeed = 10f;

    [SerializeField] private float dropReturnSpeed = 10f;

    // =========================================================
    // TRANSPARENCY
    // =========================================================

    [Header("Transparency")]

    [Range(0f, 1f)]
    [SerializeField] private float normalAlpha = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float dragAlpha = 0.7f;

    [SerializeField] private float alphaSmoothSpeed = 10f;

    // =========================================================
    // OPEN ANIMATION
    // =========================================================

    [Header("Open Animation")]

    [SerializeField] private float startingScale = 0.65f;

    [SerializeField] private float popScale = 1.08f;

    [SerializeField] private float normalScale = 1f;

    [SerializeField] private float popDuration = 0.1f;

    [SerializeField] private float settleDuration = 0.1f;

    [Header("Dashboard Open Animation")]
    [SerializeField] private bool animateDashboardOnOpen = true;
    [Min(0f)][SerializeField] private float dashboardStartDelay = 0.015f;
    [Min(0.01f)][SerializeField] private float dashboardFadeDuration = 0.10f;
    [Min(0.01f)][SerializeField] private float dashboardPopDuration = 0.13f;
    [Range(0.1f, 1f)][SerializeField] private float dashboardStartingScale = 0.72f;
    [Min(1f)][SerializeField] private float dashboardOvershootScale = 1.10f;
    [Min(0f)][SerializeField] private float dashboardItemStagger = 0.025f;
    [Min(0.01f)][SerializeField] private float meterSweepDuration = 0.24f;

    [Header("Dashboard Animation Objects")]
    [Tooltip("Optional. Uses Title Text automatically when empty.")]
    [SerializeField] private RectTransform animatedTitle;

    [Tooltip("Optional. Uses Ranger Station Icon automatically when empty.")]
    [SerializeField] private RectTransform animatedStationIcon;

    [Tooltip("Assign the complete PREY stat card/root.")]
    [SerializeField] private RectTransform preyStatRoot;

    [Tooltip("Assign the complete PREDATOR stat card/root.")]
    [SerializeField] private RectTransform predatorStatRoot;

    [Tooltip("Assign the complete FIRE stat card/root.")]
    [SerializeField] private RectTransform fireRiskStatRoot;

    [Tooltip("Assign the complete SOIL stat card/root.")]
    [SerializeField] private RectTransform soilHealthStatRoot;

    [Header("Smooth Live Meters")]
    [SerializeField] private bool smoothLiveMeterChanges = true;
    [Min(0.01f)][SerializeField] private float liveMeterSmoothSpeed = 10f;

    // =========================================================
    // CLOSE ANIMATION
    // =========================================================

    [Header("Close Animation")]

    [SerializeField] private float closingScale = 0.65f;

    [SerializeField] private float closeDuration = 0.14f;

    [SerializeField] private bool fadeWhileClosing = true;

    // =========================================================
    // OUTSIDE CLICK
    // =========================================================

    [Header("Outside Click")]

    [SerializeField] private bool closeWhenClickingOutside = true;

    // =========================================================
    // MODE BEHAVIOUR
    // =========================================================

    [Header("Mode Behaviour")]

    [SerializeField] private bool closeWhenChangingMode = true;

    [SerializeField] private bool closeWhenSelectionWheelOpens = true;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField] private bool showDebugLogs = false;

    // =========================================================
    // PRIVATE
    // =========================================================

    private RangerStation currentStation;

    private InspectableRangerStation currentInspectableRangerStation;

    private Transform followTarget;

    private Coroutine animationCoroutine;

    private bool isOpen;

    private bool isClosing;

    private bool isDragging;

    private bool manuallyPositioned;

    private bool ignoreOutsideClick;

    private Vector2 dragOffset;

    private Vector2 previousMousePosition;

    private float currentSwayAngle;

    private bool dashboardOpenAnimationPlaying;

    private float displayedPrey;
    private float displayedPredator;
    private float displayedFireRisk;
    private float displayedSoilHealth;
    private bool displayedValuesInitialised;

    private float openingPreyTarget;
    private float openingPredatorTarget;
    private float openingFireRiskTarget;
    private float openingSoilHealthTarget;

    private readonly Dictionary<RectTransform, Vector3> dashboardOriginalScales =
        new Dictionary<RectTransform, Vector3>();

    private Material preyRadialMaterial;
    private Material predatorRadialMaterial;
    private Material fireRiskRadialMaterial;
    private Material soilHealthRadialMaterial;

    private int hoveredRadialStat = -1;
    private Vector3 preyHoverBaseScale = Vector3.one;
    private Vector3 predatorHoverBaseScale = Vector3.one;
    private Vector3 fireRiskHoverBaseScale = Vector3.one;
    private Vector3 soilHealthHoverBaseScale = Vector3.one;
    private Vector3 preyIconHoverBaseScale = Vector3.one;
    private Vector3 predatorIconHoverBaseScale = Vector3.one;
    private Vector3 fireRiskIconHoverBaseScale = Vector3.one;
    private Vector3 soilHealthIconHoverBaseScale = Vector3.one;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // =====================================================
        // CAMERA
        // =====================================================

        if (mainCamera == null)
        {
            mainCamera =
                Camera.main;
        }

        // =====================================================
        // CANVAS
        // =====================================================

        if (canvas == null)
        {
            canvas =
                GetComponentInParent<Canvas>();
        }

        // =====================================================
        // SELECTION WHEEL
        // =====================================================

        if (selectionWheel == null)
        {
            selectionWheel =
                FindFirstObjectByType<SelectionWheel>();
        }

        // =====================================================
        // PANEL
        // =====================================================

        if (panel == null)
        {
            panel =
                transform as RectTransform;
        }

        // =====================================================
        // CANVAS GROUP
        // =====================================================

        if (canvasGroup == null &&
            panel != null)
        {
            canvasGroup =
                panel.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup =
                    panel.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                normalAlpha;
        }

        // =====================================================
        // SLIDERS
        // =====================================================

        SetupSlider(
            preySlider
        );

        SetupSlider(
            predatorSlider
        );

        SetupSlider(
            fireRiskSlider
        );

        SetupSlider(
            soilHealthSlider
        );

        SetupRadialMaterials();
        SetupRadialHoverEvents();
        CacheHoverBaseScales();

        // =====================================================
        // CLOSE BUTTON
        // =====================================================

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(
                Close
            );
        }

        CacheDefaultDashboardObjects();
        CacheDashboardOriginalScales();

        // =====================================================
        // START HIDDEN
        // =====================================================

        if (panel != null)
        {
            panel.gameObject.SetActive(
                false
            );
        }
    }

    // =========================================================
    // SLIDER SETUP
    // =========================================================

    private void SetupSlider(
        Slider slider)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue =
            0f;

        slider.maxValue =
            100f;

        slider.interactable =
            false;
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        UpdateRadialHoverVisuals();

        if (!isOpen ||
            isClosing ||
            panel == null)
        {
            return;
        }

        // =====================================================
        // MODE CHECK
        // =====================================================

        if (ShouldCloseBecauseOfMode())
        {
            Close();

            return;
        }

        // =====================================================
        // DRAGGING
        // =====================================================

        HandleDragging();

        UpdateDragVisuals();

        // =====================================================
        // LIVE STATS
        // =====================================================

        RefreshUI();

        // =====================================================
        // OUTSIDE CLICK
        // =====================================================

        if (closeWhenClickingOutside &&
            Input.GetMouseButtonDown(0))
        {
            HandleOutsideClick();
        }
    }

    // =========================================================
    // LATE UPDATE
    // =========================================================

    private void LateUpdate()
    {
        if (!isOpen ||
            isClosing ||
            currentStation == null ||
            panel == null)
        {
            return;
        }

        if (manuallyPositioned &&
            stopFollowingAfterDrag)
        {
            return;
        }

        UpdatePanelPosition();
    }

    // =========================================================
    // MODE CHECK
    // =========================================================

    private bool ShouldCloseBecauseOfMode()
    {
        if (selectionWheel == null)
        {
            return false;
        }

        if (closeWhenChangingMode &&
            !selectionWheel.IsNormalMode())
        {
            return true;
        }

        if (closeWhenSelectionWheelOpens &&
            selectionWheel.IsWheelOpen())
        {
            return true;
        }

        return false;
    }

    // =========================================================
    // OPEN - COMPATIBILITY VERSION
    // =========================================================

    public void Open(
        RangerStation station)
    {
        Open(
            station,
            station != null
                ? station.transform
                : null
        );
    }

    // =========================================================
    // OPEN
    // =========================================================

    public void Open(
        RangerStation station,
        Transform target)
    {
        if (station == null ||
            panel == null)
        {
            return;
        }

        // =====================================================
        // SINGLE INSPECTION HUD
        // =====================================================

        if (InspectionUIManager.Instance != null)
        {
            InspectionUIManager.Instance.OpenPanel(
                this
            );
        }

        // =====================================================
        // CLEAR OLD HIGHLIGHT
        // =====================================================

        ClearCurrentRangerStationHighlight();

        // =====================================================
        // STOP OLD ANIMATION
        // =====================================================

        if (animationCoroutine != null)
        {
            StopCoroutine(
                animationCoroutine
            );

            animationCoroutine =
                null;
        }

        // =====================================================
        // CURRENT STATION
        // =====================================================

        currentStation =
            station;

        // =====================================================
        // FOLLOW TARGET
        // =====================================================

        followTarget =
            target != null
                ? target
                : station.transform;

        // =====================================================
        // FIND INSPECTABLE RANGER STATION
        // =====================================================

        currentInspectableRangerStation =
            station.GetComponent<InspectableRangerStation>();

        if (currentInspectableRangerStation == null)
        {
            currentInspectableRangerStation =
                station.GetComponentInChildren<InspectableRangerStation>();
        }

        if (currentInspectableRangerStation == null)
        {
            currentInspectableRangerStation =
                station.GetComponentInParent<InspectableRangerStation>();
        }

        // =====================================================
        // INSPECTED OUTLINE
        // =====================================================

        if (currentInspectableRangerStation != null)
        {
            currentInspectableRangerStation.SetInspected(
                true
            );
        }

        // =====================================================
        // STATE
        // =====================================================

        isOpen =
            true;

        isClosing =
            false;

        isDragging =
            false;

        manuallyPositioned =
            false;

        currentSwayAngle =
            0f;

        // =====================================================
        // SHOW PANEL
        // =====================================================

        panel.gameObject.SetActive(
            true
        );

        panel.localRotation =
            Quaternion.identity;

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                normalAlpha;
        }

        // =====================================================
        // REFRESH
        // =====================================================

        SetupRadialMaterials();
        SetupRadialHoverEvents();
        CacheHoverBaseScales();
        hoveredRadialStat = -1;
        CacheDefaultDashboardObjects();
        CacheDashboardOriginalScales();
        displayedValuesInitialised = false;
        RefreshUI();

        // =====================================================
        // POSITION
        // =====================================================

        SetPanelPositionImmediately();

        // =====================================================
        // IGNORE OPENING CLICK
        // =====================================================

        ignoreOutsideClick =
            true;

        StartCoroutine(
            ResetOutsideClickIgnore()
        );

        // =====================================================
        // OPEN ANIMATION
        // =====================================================

        animationCoroutine =
            StartCoroutine(
                OpenAnimation()
            );

        // =====================================================
        // DEBUG
        // =====================================================

        if (showDebugLogs)
        {
            Debug.Log(
                "Ranger Station inspection opened."
            );
        }

        // =====================================================
        // TUTORIAL - RANGER STATION INSPECTED
        // =====================================================

        TutorialEvents.Report(
            TutorialAction.RangerStationInspected
        );
    }

    // =========================================================
    // REFRESH UI
    // =========================================================

    public void RefreshUI()
    {
        if (currentStation == null)
        {
            return;
        }

        // =====================================================
        // TITLE
        // =====================================================

        if (titleText != null)
        {
            titleText.text =
                "RANGER STATION";
        }

        // =====================================================
        // VALUES
        // =====================================================

        float prey =
            Mathf.Clamp(
                currentStation.GetPreyAvailability(),
                0f,
                100f
            );

        float predator =
            Mathf.Clamp(
                currentStation.GetPredatorPressure(),
                0f,
                100f
            );

        float fireRisk =
            Mathf.Clamp(
                currentStation.GetFireRisk(),
                0f,
                100f
            );

        float soilHealth =
            Mathf.Clamp(
                currentStation.GetSoilHealth(),
                0f,
                100f
            );

        // =====================================================
        // SMOOTH DISPLAY VALUES
        // =====================================================

        if (!dashboardOpenAnimationPlaying)
        {
            if (!displayedValuesInitialised ||
                !smoothLiveMeterChanges)
            {
                displayedPrey = prey;
                displayedPredator = predator;
                displayedFireRisk = fireRisk;
                displayedSoilHealth = soilHealth;
                displayedValuesInitialised = true;
            }
            else
            {
                float smoothing =
                    1f -
                    Mathf.Exp(
                        -Mathf.Max(
                            0.01f,
                            liveMeterSmoothSpeed
                        ) *
                        Time.unscaledDeltaTime
                    );

                displayedPrey =
                    Mathf.Lerp(
                        displayedPrey,
                        prey,
                        smoothing
                    );

                displayedPredator =
                    Mathf.Lerp(
                        displayedPredator,
                        predator,
                        smoothing
                    );

                displayedFireRisk =
                    Mathf.Lerp(
                        displayedFireRisk,
                        fireRisk,
                        smoothing
                    );

                displayedSoilHealth =
                    Mathf.Lerp(
                        displayedSoilHealth,
                        soilHealth,
                        smoothing
                    );
            }

            SetSliderValue(preySlider, displayedPrey);
            SetSliderValue(predatorSlider, displayedPredator);
            SetSliderValue(fireRiskSlider, displayedFireRisk);
            SetSliderValue(soilHealthSlider, displayedSoilHealth);

            SetArcFill(preyRadialMaterial, displayedPrey / 100f);
            SetArcFill(predatorRadialMaterial, displayedPredator / 100f);
            SetArcFill(fireRiskRadialMaterial, displayedFireRisk / 100f);
            SetArcFill(soilHealthRadialMaterial, displayedSoilHealth / 100f);
        }

        UpdateRadialStatTexts(
            prey,
            predator,
            fireRisk,
            soilHealth
        );
    }

    private void UpdateRadialStatTexts(
        float prey,
        float predator,
        float fireRisk,
        float soilHealth)
    {
        if (preyValueText != null)
        {
            preyValueText.text =
                hoveredRadialStat == 0 && hoverStatText == null
                    ? Mathf.RoundToInt(prey) + "% Prey Availability"
                    : Mathf.RoundToInt(prey) + "%";
        }

        if (predatorValueText != null)
        {
            predatorValueText.text =
                hoveredRadialStat == 1 && hoverStatText == null
                    ? Mathf.RoundToInt(predator) + "% Predator Pressure"
                    : Mathf.RoundToInt(predator) + "%";
        }

        if (fireRiskValueText != null)
        {
            fireRiskValueText.text =
                hoveredRadialStat == 2 && hoverStatText == null
                    ? Mathf.RoundToInt(fireRisk) + "% Fire Risk"
                    : Mathf.RoundToInt(fireRisk) + "%";
        }

        if (soilHealthValueText != null)
        {
            soilHealthValueText.text =
                hoveredRadialStat == 3 && hoverStatText == null
                    ? Mathf.RoundToInt(soilHealth) + "% Soil Health"
                    : Mathf.RoundToInt(soilHealth) + "%";
        }

        if (hoverStatText != null)
        {
            switch (hoveredRadialStat)
            {
                case 0:
                    hoverStatText.text =
                        Mathf.RoundToInt(prey) +
                        "% Prey Availability";
                    break;

                case 1:
                    hoverStatText.text =
                        Mathf.RoundToInt(predator) +
                        "% Predator Pressure";
                    break;

                case 2:
                    hoverStatText.text =
                        Mathf.RoundToInt(fireRisk) +
                        "% Fire Risk";
                    break;

                case 3:
                    hoverStatText.text =
                        Mathf.RoundToInt(soilHealth) +
                        "% Soil Health";
                    break;

                default:
                    hoverStatText.text = "";
                    break;
            }
        }
    }

    private void SetupRadialHoverEvents()
    {
        SetupHoverEvent(preyRadialFill, 0);
        SetupHoverEvent(predatorRadialFill, 1);
        SetupHoverEvent(fireRiskRadialFill, 2);
        SetupHoverEvent(soilHealthRadialFill, 3);
    }

    private void SetupHoverEvent(
        Image image,
        int statIndex)
    {
        if (image == null)
        {
            return;
        }

        image.raycastTarget = true;

        EventTrigger trigger =
            image.GetComponent<EventTrigger>();

        if (trigger == null)
        {
            trigger =
                image.gameObject.AddComponent<EventTrigger>();
        }

        if (trigger.triggers == null)
        {
            trigger.triggers =
                new List<EventTrigger.Entry>();
        }

        AddHoverTrigger(
            trigger,
            EventTriggerType.PointerEnter,
            statIndex
        );

        AddHoverTrigger(
            trigger,
            EventTriggerType.PointerExit,
            -1
        );
    }

    private void AddHoverTrigger(
        EventTrigger trigger,
        EventTriggerType type,
        int statIndex)
    {
        EventTrigger.Entry entry =
            new EventTrigger.Entry();

        entry.eventID = type;

        entry.callback.AddListener(
            (data) =>
            {
                if (!enableRadialHover)
                {
                    return;
                }

                hoveredRadialStat =
                    statIndex;
            }
        );

        trigger.triggers.Add(entry);
    }

    private void CacheHoverBaseScales()
    {
        preyHoverBaseScale =
            GetHoverBaseScale(
                GetHoverTarget(
                    preyHoverRoot,
                    preyRadialFill
                )
            );

        predatorHoverBaseScale =
            GetHoverBaseScale(
                GetHoverTarget(
                    predatorHoverRoot,
                    predatorRadialFill
                )
            );

        fireRiskHoverBaseScale =
            GetHoverBaseScale(
                GetHoverTarget(
                    fireRiskHoverRoot,
                    fireRiskRadialFill
                )
            );

        soilHealthHoverBaseScale =
            GetHoverBaseScale(
                GetHoverTarget(
                    soilHealthHoverRoot,
                    soilHealthRadialFill
                )
            );

        preyIconHoverBaseScale =
            GetHoverBaseScale(preyIcon);

        predatorIconHoverBaseScale =
            GetHoverBaseScale(predatorIcon);

        fireRiskIconHoverBaseScale =
            GetHoverBaseScale(fireRiskIcon);

        soilHealthIconHoverBaseScale =
            GetHoverBaseScale(soilHealthIcon);
    }

    private Vector3 GetHoverBaseScale(
        Image image)
    {
        if (image == null)
        {
            return Vector3.one;
        }

        return image.rectTransform.localScale;
    }

    private RectTransform GetHoverTarget(
        RectTransform explicitRoot,
        Image fallbackImage)
    {
        if (explicitRoot != null)
        {
            return explicitRoot;
        }

        if (fallbackImage != null)
        {
            return fallbackImage.rectTransform;
        }

        return null;
    }

    private Vector3 GetHoverBaseScale(
        RectTransform target)
    {
        if (target == null)
        {
            return Vector3.one;
        }

        return target.localScale;
    }

    private void UpdateRadialHoverVisuals()
    {
        if (!enableRadialHover)
        {
            hoveredRadialStat = -1;
        }

        AnimateHoverScale(
            GetHoverTarget(
                preyHoverRoot,
                preyRadialFill
            ),
            preyHoverBaseScale,
            hoveredRadialStat == 0,
            hoveredScale
        );

        AnimateHoverScale(
            GetHoverTarget(
                predatorHoverRoot,
                predatorRadialFill
            ),
            predatorHoverBaseScale,
            hoveredRadialStat == 1,
            hoveredScale
        );

        AnimateHoverScale(
            GetHoverTarget(
                fireRiskHoverRoot,
                fireRiskRadialFill
            ),
            fireRiskHoverBaseScale,
            hoveredRadialStat == 2,
            hoveredScale
        );

        AnimateHoverScale(
            GetHoverTarget(
                soilHealthHoverRoot,
                soilHealthRadialFill
            ),
            soilHealthHoverBaseScale,
            hoveredRadialStat == 3,
            hoveredScale
        );

        AnimateHoverScale(
            preyIcon,
            preyIconHoverBaseScale,
            hoveredRadialStat == 0,
            hoveredIconScale
        );

        AnimateHoverScale(
            predatorIcon,
            predatorIconHoverBaseScale,
            hoveredRadialStat == 1,
            hoveredIconScale
        );

        AnimateHoverScale(
            fireRiskIcon,
            fireRiskIconHoverBaseScale,
            hoveredRadialStat == 2,
            hoveredIconScale
        );

        AnimateHoverScale(
            soilHealthIcon,
            soilHealthIconHoverBaseScale,
            hoveredRadialStat == 3,
            hoveredIconScale
        );

        bool hasHover =
            hoveredRadialStat >= 0;

        SetArcSaturation(
            preyRadialMaterial,
            !hasHover || hoveredRadialStat == 0
                ? 1f
                : nonHoveredSaturation
        );

        SetArcSaturation(
            predatorRadialMaterial,
            !hasHover || hoveredRadialStat == 1
                ? 1f
                : nonHoveredSaturation
        );

        SetArcSaturation(
            fireRiskRadialMaterial,
            !hasHover || hoveredRadialStat == 2
                ? 1f
                : nonHoveredSaturation
        );

        SetArcSaturation(
            soilHealthRadialMaterial,
            !hasHover || hoveredRadialStat == 3
                ? 1f
                : nonHoveredSaturation
        );
    }

    private void AnimateHoverScale(
        Image image,
        Vector3 baseScale,
        bool hovered)
    {
        if (image == null)
        {
            return;
        }

        Vector3 targetScale =
            baseScale *
            (hovered ? hoveredScale : 1f);

        float smoothing =
            1f -
            Mathf.Exp(
                -Mathf.Max(
                    0.01f,
                    hoverScaleSpeed
                ) *
                Time.unscaledDeltaTime
            );

        image.rectTransform.localScale =
            Vector3.Lerp(
                image.rectTransform.localScale,
                targetScale,
                smoothing
            );
    }

    private void AnimateHoverScale(
        RectTransform target,
        Vector3 baseScale,
        bool hovered,
        float hoverMultiplier)
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetScale =
            baseScale *
            (hovered ? hoverMultiplier : 1f);

        float smoothing =
            1f -
            Mathf.Exp(
                -Mathf.Max(
                    0.01f,
                    hoverScaleSpeed
                ) *
                Time.unscaledDeltaTime
            );

        target.localScale =
            Vector3.Lerp(
                target.localScale,
                targetScale,
                smoothing
            );
    }

    private void SetArcSaturation(
        Material material,
        float saturation)
    {
        if (material == null ||
            !material.HasProperty("_Saturation"))
        {
            return;
        }

        material.SetFloat(
            "_Saturation",
            Mathf.Clamp01(saturation)
        );
    }

    private void SetupRadialMaterials()
    {
        SetupArcMaterial(
            preyRadialFill,
            ref preyRadialMaterial,
            preyStartAngle
        );

        SetupArcMaterial(
            predatorRadialFill,
            ref predatorRadialMaterial,
            predatorStartAngle
        );

        SetupArcMaterial(
            fireRiskRadialFill,
            ref fireRiskRadialMaterial,
            fireRiskStartAngle
        );

        SetupArcMaterial(
            soilHealthRadialFill,
            ref soilHealthRadialMaterial,
            soilHealthStartAngle
        );
    }

    private void SetupArcMaterial(
        Image image,
        ref Material runtimeMaterial,
        float startAngle)
    {
        if (image == null ||
            radialArcMaterial == null)
        {
            return;
        }

        if (runtimeMaterial == null)
        {
            runtimeMaterial =
                new Material(radialArcMaterial);

            runtimeMaterial.name =
                radialArcMaterial.name +
                " (Ranger Runtime)";
        }

        image.material =
            runtimeMaterial;

        image.type =
            Image.Type.Simple;

        image.preserveAspect =
            true;

        image.raycastTarget =
            false;

        runtimeMaterial.SetFloat(
            "_StartAngle",
            startAngle
        );

        runtimeMaterial.SetFloat(
            "_ArcAngle",
            radialArcAngle
        );

        runtimeMaterial.SetFloat(
            "_Clockwise",
            fillClockwise ? 1f : 0f
        );

        if (runtimeMaterial.HasProperty("_Saturation"))
        {
            runtimeMaterial.SetFloat(
                "_Saturation",
                1f
            );
        }
    }

    private void SetArcFill(
        Material material,
        float amount)
    {
        if (material == null)
        {
            return;
        }

        material.SetFloat(
            "_FillAmount",
            Mathf.Clamp01(amount)
        );
    }

    private void SetSliderValue(
        Slider slider,
        float value)
    {
        if (slider == null)
        {
            return;
        }

        slider.SetValueWithoutNotify(
            Mathf.Clamp(value, 0f, 100f)
        );
    }

    // =========================================================
    // CLOSE
    // =========================================================

    public void Close()
    {
        if (!isOpen ||
            isClosing)
        {
            return;
        }

        if (animationCoroutine != null)
        {
            StopCoroutine(
                animationCoroutine
            );
        }

        animationCoroutine =
            StartCoroutine(
                CloseAnimation()
            );
    }

    // =========================================================
    // CLOSE IMMEDIATELY
    // =========================================================

    public void CloseImmediately()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(
                animationCoroutine
            );

            animationCoroutine =
                null;
        }

        ClearCurrentRangerStationHighlight();

        dashboardOpenAnimationPlaying = false;
        RestoreDashboardVisuals();
        displayedValuesInitialised = false;

        isOpen =
            false;

        isClosing =
            false;

        isDragging =
            false;

        manuallyPositioned =
            false;

        ignoreOutsideClick =
            false;

        currentStation =
            null;

        followTarget =
            null;

        currentSwayAngle =
            0f;

        if (panel != null)
        {
            panel.localScale =
                Vector3.one *
                normalScale;

            panel.localRotation =
                Quaternion.identity;

            panel.gameObject.SetActive(
                false
            );
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                normalAlpha;
        }

        if (InspectionUIManager.Instance != null)
        {
            InspectionUIManager.Instance.ClearPanel(
                this
            );
        }
    }

    // =========================================================
    // CLEAR RANGER STATION HIGHLIGHT
    // =========================================================

    private void ClearCurrentRangerStationHighlight()
    {
        if (currentInspectableRangerStation != null)
        {
            currentInspectableRangerStation.SetInspected(
                false
            );
        }

        currentInspectableRangerStation =
            null;
    }

    // =========================================================
    // OUTSIDE CLICK
    // =========================================================

    private void HandleOutsideClick()
    {
        if (ignoreOutsideClick)
        {
            return;
        }

        if (IsPointerOverInspectionUI())
        {
            return;
        }

        Close();
    }

    // =========================================================
    // POINTER OVER THIS HUD
    // =========================================================

    private bool IsPointerOverInspectionUI()
    {
        if (panel == null)
        {
            return false;
        }

        if (RectTransformUtility.RectangleContainsScreenPoint(
            panel,
            Input.mousePosition,
            GetUICamera()))
        {
            return true;
        }

        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerData =
            new PointerEventData(
                EventSystem.current
            );

        pointerData.position =
            Input.mousePosition;

        List<RaycastResult> results =
            new List<RaycastResult>();

        EventSystem.current.RaycastAll(
            pointerData,
            results
        );

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == null)
            {
                continue;
            }

            Transform hitTransform =
                result.gameObject.transform;

            if (hitTransform == panel ||
                hitTransform.IsChildOf(panel))
            {
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // DRAGGING
    // =========================================================

    private void HandleDragging()
    {
        if (!allowDragging ||
            dragHandle == null ||
            canvas == null)
        {
            return;
        }

        Camera uiCamera =
            GetUICamera();

        // =====================================================
        // START DRAG
        // =====================================================

        if (Input.GetMouseButtonDown(0))
        {
            bool overHandle =
                RectTransformUtility.RectangleContainsScreenPoint(
                    dragHandle,
                    Input.mousePosition,
                    uiCamera
                );

            if (overHandle)
            {
                RectTransform canvasRect =
                    canvas.transform
                        as RectTransform;

                if (canvasRect == null)
                {
                    return;
                }

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    Input.mousePosition,
                    uiCamera,
                    out Vector2 mousePosition
                );

                dragOffset =
                    panel.anchoredPosition -
                    mousePosition;

                isDragging =
                    true;

                manuallyPositioned =
                    true;

                previousMousePosition =
                    Input.mousePosition;
            }
        }

        // =====================================================
        // WHILE DRAGGING
        // =====================================================

        if (isDragging &&
            Input.GetMouseButton(0))
        {
            RectTransform canvasRect =
                canvas.transform
                    as RectTransform;

            if (canvasRect == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                Input.mousePosition,
                uiCamera,
                out Vector2 mousePosition
            );

            Vector2 newPosition =
                mousePosition +
                dragOffset;

            if (clampToCanvas)
            {
                newPosition =
                    ClampPanelToCanvas(
                        newPosition
                    );
            }

            panel.anchoredPosition =
                newPosition;
        }

        // =====================================================
        // RELEASE
        // =====================================================

        if (isDragging &&
            Input.GetMouseButtonUp(0))
        {
            isDragging =
                false;
        }
    }

    // =========================================================
    // DRAG VISUALS
    // =========================================================

    private void UpdateDragVisuals()
    {
        if (panel == null)
        {
            return;
        }

        float delta =
            Time.unscaledDeltaTime;

        // =====================================================
        // TRANSPARENCY
        // =====================================================

        float targetAlpha =
            isDragging
                ? dragAlpha
                : normalAlpha;

        if (canvasGroup != null)
        {
            float alphaAmount =
                1f -
                Mathf.Exp(
                    -alphaSmoothSpeed *
                    delta
                );

            canvasGroup.alpha =
                Mathf.Lerp(
                    canvasGroup.alpha,
                    targetAlpha,
                    alphaAmount
                );
        }

        // =====================================================
        // DRAGGING
        // =====================================================

        if (isDragging)
        {
            float scaleAmount =
                1f -
                Mathf.Exp(
                    -dragScaleSpeed *
                    delta
                );

            panel.localScale =
                Vector3.Lerp(
                    panel.localScale,
                    Vector3.one *
                    dragScale,
                    scaleAmount
                );

            Vector2 currentMouse =
                Input.mousePosition;

            Vector2 mouseDelta =
                currentMouse -
                previousMousePosition;

            previousMousePosition =
                currentMouse;

            float targetSway =
                -mouseDelta.x *
                swayStrength;

            targetSway =
                Mathf.Clamp(
                    targetSway,
                    -maxDragSwayAngle,
                    maxDragSwayAngle
                );

            float swayAmount =
                1f -
                Mathf.Exp(
                    -swaySmoothSpeed *
                    delta
                );

            currentSwayAngle =
                Mathf.Lerp(
                    currentSwayAngle,
                    targetSway,
                    swayAmount
                );

            panel.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    currentSwayAngle
                );
        }

        // =====================================================
        // RETURN TO NORMAL
        // =====================================================

        else
        {
            float returnAmount =
                1f -
                Mathf.Exp(
                    -dropReturnSpeed *
                    delta
                );

            panel.localScale =
                Vector3.Lerp(
                    panel.localScale,
                    Vector3.one *
                    normalScale,
                    returnAmount
                );

            currentSwayAngle =
                Mathf.Lerp(
                    currentSwayAngle,
                    0f,
                    returnAmount
                );

            panel.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    currentSwayAngle
                );
        }
    }

    // =========================================================
    // FOLLOW RANGER STATION
    // =========================================================

    private void UpdatePanelPosition()
    {
        Vector2 targetPosition =
            CalculatePanelPosition();

        float smoothing =
            1f -
            Mathf.Exp(
                -followSpeed *
                Time.unscaledDeltaTime
            );

        panel.anchoredPosition =
            Vector2.Lerp(
                panel.anchoredPosition,
                targetPosition,
                smoothing
            );
    }

    // =========================================================
    // POSITION IMMEDIATELY
    // =========================================================

    private void SetPanelPositionImmediately()
    {
        if (panel == null)
        {
            return;
        }

        Vector2 position =
            CalculatePanelPosition();

        if (clampToCanvas)
        {
            position =
                ClampPanelToCanvas(
                    position
                );
        }

        panel.anchoredPosition =
            position;
    }

    // =========================================================
    // CALCULATE POSITION
    // =========================================================

    private Vector2 CalculatePanelPosition()
    {
        if (currentStation == null ||
            mainCamera == null ||
            canvas == null ||
            panel == null)
        {
            return panel != null
                ? panel.anchoredPosition
                : Vector2.zero;
        }

        Transform positionTarget =
            followTarget != null
                ? followTarget
                : currentStation.transform;

        Vector3 screenPosition =
            mainCamera.WorldToScreenPoint(
                positionTarget.position
            );

        float direction =
            1f;

        if (automaticallyFlipSide &&
            screenPosition.x >
            Screen.width -
            screenEdgePadding)
        {
            direction =
                -1f;
        }

        screenPosition.x +=
            horizontalOffset *
            direction;

        screenPosition.y +=
            verticalOffset;

        RectTransform canvasRect =
            canvas.transform
                as RectTransform;

        if (canvasRect == null)
        {
            return panel.anchoredPosition;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            GetUICamera(),
            out Vector2 canvasPosition
        );

        if (clampToCanvas)
        {
            canvasPosition =
                ClampPanelToCanvas(
                    canvasPosition
                );
        }

        return canvasPosition;
    }

    // =========================================================
    // CLAMP TO CANVAS
    // =========================================================

    private Vector2 ClampPanelToCanvas(
        Vector2 targetPosition)
    {
        if (canvas == null ||
            panel == null)
        {
            return targetPosition;
        }

        RectTransform canvasRect =
            canvas.transform
                as RectTransform;

        if (canvasRect == null)
        {
            return targetPosition;
        }

        Rect canvasBounds =
            canvasRect.rect;

        Vector2 panelSize =
            panel.rect.size;

        Vector2 pivot =
            panel.pivot;

        float minX =
            canvasBounds.xMin +
            panelSize.x *
            pivot.x +
            canvasPadding;

        float maxX =
            canvasBounds.xMax -
            panelSize.x *
            (1f - pivot.x) -
            canvasPadding;

        float minY =
            canvasBounds.yMin +
            panelSize.y *
            pivot.y +
            canvasPadding;

        float maxY =
            canvasBounds.yMax -
            panelSize.y *
            (1f - pivot.y) -
            canvasPadding;

        targetPosition.x =
            Mathf.Clamp(
                targetPosition.x,
                minX,
                maxX
            );

        targetPosition.y =
            Mathf.Clamp(
                targetPosition.y,
                minY,
                maxY
            );

        return targetPosition;
    }

    // =========================================================
    // UI CAMERA
    // =========================================================

    private Camera GetUICamera()
    {
        if (canvas == null)
        {
            return null;
        }

        if (canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }

    // =========================================================
    // OPEN ANIMATION
    // =========================================================

    private IEnumerator OpenAnimation()
    {
        if (panel == null)
        {
            yield break;
        }

        CacheDefaultDashboardObjects();
        CacheDashboardOriginalScales();

        panel.localScale =
            Vector3.one *
            startingScale;

        panel.localRotation =
            Quaternion.identity;

        if (animateDashboardOnOpen)
        {
            PrepareDashboardOpenAnimation();
        }

        yield return ScalePanel(
            startingScale,
            popScale,
            popDuration,
            true
        );

        yield return ScalePanel(
            popScale,
            normalScale,
            settleDuration,
            false
        );

        panel.localScale =
            Vector3.one *
            normalScale;

        if (animateDashboardOnOpen)
        {
            yield return AnimateDashboardOpen();
        }

        animationCoroutine =
            null;
    }

    private void CacheDefaultDashboardObjects()
    {
        if (animatedTitle == null &&
            titleText != null)
        {
            animatedTitle =
                titleText.rectTransform;
        }

        if (animatedStationIcon == null &&
            rangerStationIcon != null)
        {
            animatedStationIcon =
                rangerStationIcon.rectTransform;
        }
    }

    private void CacheDashboardOriginalScales()
    {
        CacheDashboardScale(animatedTitle);
        CacheDashboardScale(animatedStationIcon);
        CacheDashboardScale(preyStatRoot);
        CacheDashboardScale(predatorStatRoot);
        CacheDashboardScale(fireRiskStatRoot);
        CacheDashboardScale(soilHealthStatRoot);
        CacheDashboardScale(preyIcon);
        CacheDashboardScale(predatorIcon);
        CacheDashboardScale(fireRiskIcon);
        CacheDashboardScale(soilHealthIcon);
        CacheDashboardScale(ringBackground);
    }

    private void CacheDashboardScale(
        RectTransform target)
    {
        if (target == null ||
            dashboardOriginalScales.ContainsKey(target))
        {
            return;
        }

        dashboardOriginalScales.Add(
            target,
            target.localScale
        );
    }

    private Vector3 GetDashboardOriginalScale(
        RectTransform target)
    {
        if (target == null)
        {
            return Vector3.one;
        }

        if (dashboardOriginalScales.TryGetValue(
            target,
            out Vector3 scale))
        {
            return scale;
        }

        scale =
            target.localScale;

        if (scale == Vector3.zero)
        {
            scale =
                Vector3.one;
        }

        dashboardOriginalScales[target] =
            scale;

        return scale;
    }

    private void PrepareDashboardOpenAnimation()
    {
        dashboardOpenAnimationPlaying =
            true;

        openingPreyTarget =
            currentStation != null
                ? Mathf.Clamp(
                    currentStation.GetPreyAvailability(),
                    0f,
                    100f
                )
                : 0f;

        openingPredatorTarget =
            currentStation != null
                ? Mathf.Clamp(
                    currentStation.GetPredatorPressure(),
                    0f,
                    100f
                )
                : 0f;

        openingFireRiskTarget =
            currentStation != null
                ? Mathf.Clamp(
                    currentStation.GetFireRisk(),
                    0f,
                    100f
                )
                : 0f;

        openingSoilHealthTarget =
            currentStation != null
                ? Mathf.Clamp(
                    currentStation.GetSoilHealth(),
                    0f,
                    100f
                )
                : 0f;

        SetSliderValue(preySlider, 0f);
        SetSliderValue(predatorSlider, 0f);
        SetSliderValue(fireRiskSlider, 0f);
        SetSliderValue(soilHealthSlider, 0f);

        SetArcFill(preyRadialMaterial, 0f);
        SetArcFill(predatorRadialMaterial, 0f);
        SetArcFill(fireRiskRadialMaterial, 0f);
        SetArcFill(soilHealthRadialMaterial, 0f);

        HideDashboardObject(animatedTitle);
        HideDashboardObject(animatedStationIcon);

        // The ring fades in but deliberately never scales.
        RestoreDashboardObject(ringBackground);
        SetDashboardAlpha(ringBackground, 0f);

        HideDashboardObject(preyIcon);
        HideDashboardObject(predatorIcon);
        HideDashboardObject(fireRiskIcon);
        HideDashboardObject(soilHealthIcon);

        // Optional label/value roots can still be animated if assigned.
        HideDashboardObject(preyStatRoot);
        HideDashboardObject(predatorStatRoot);
        HideDashboardObject(fireRiskStatRoot);
        HideDashboardObject(soilHealthStatRoot);
    }

    private IEnumerator AnimateDashboardOpen()
    {
        if (dashboardStartDelay > 0f)
        {
            yield return WaitUnscaled(
                dashboardStartDelay
            );
        }

        yield return AnimateDashboardObject(
            animatedTitle,
            1.04f
        );

        yield return WaitUnscaled(
            dashboardItemStagger
        );

        yield return AnimateDashboardObject(
            animatedStationIcon,
            dashboardOvershootScale
        );

        yield return WaitUnscaled(
            dashboardItemStagger
        );
        StartCoroutine(
            FadeDashboardRect(
                ringBackground,
                0f,
                1f,
                dashboardFadeDuration
            )
        );

        StartCoroutine(
            SweepRadialArc(
                preyRadialMaterial,
                openingPreyTarget / 100f,
                0f
            )
        );

        StartCoroutine(
            AnimateDashboardObject(
                preyIcon,
                dashboardOvershootScale
            )
        );

        StartCoroutine(
            AnimateDashboardObject(
                preyStatRoot,
                dashboardOvershootScale
            )
        );

        yield return WaitUnscaled(
            dashboardItemStagger
        );

        StartCoroutine(
            SweepRadialArc(
                predatorRadialMaterial,
                openingPredatorTarget / 100f,
                0f
            )
        );

        StartCoroutine(
            AnimateDashboardObject(
                predatorIcon,
                dashboardOvershootScale
            )
        );

        StartCoroutine(
            AnimateDashboardObject(
                predatorStatRoot,
                dashboardOvershootScale
            )
        );

        yield return WaitUnscaled(
            dashboardItemStagger
        );

        StartCoroutine(
            SweepRadialArc(
                fireRiskRadialMaterial,
                openingFireRiskTarget / 100f,
                0f
            )
        );

        StartCoroutine(
            AnimateDashboardObject(
                fireRiskIcon,
                dashboardOvershootScale
            )
        );

        StartCoroutine(
            AnimateDashboardObject(
                fireRiskStatRoot,
                dashboardOvershootScale
            )
        );

        yield return WaitUnscaled(
            dashboardItemStagger
        );

        StartCoroutine(
            SweepRadialArc(
                soilHealthRadialMaterial,
                openingSoilHealthTarget / 100f,
                0f
            )
        );

        StartCoroutine(
            AnimateDashboardObject(
                soilHealthIcon,
                dashboardOvershootScale
            )
        );

        StartCoroutine(
            AnimateDashboardObject(
                soilHealthStatRoot,
                dashboardOvershootScale
            )
        );

        yield return WaitUnscaled(
            meterSweepDuration +
            dashboardPopDuration
        );

        displayedPrey =
            openingPreyTarget;

        displayedPredator =
            openingPredatorTarget;

        displayedFireRisk =
            openingFireRiskTarget;

        displayedSoilHealth =
            openingSoilHealthTarget;

        displayedValuesInitialised =
            true;

        SetArcFill(preyRadialMaterial, openingPreyTarget / 100f);
        SetArcFill(predatorRadialMaterial, openingPredatorTarget / 100f);
        SetArcFill(fireRiskRadialMaterial, openingFireRiskTarget / 100f);
        SetArcFill(soilHealthRadialMaterial, openingSoilHealthTarget / 100f);

        RestoreDashboardVisuals();

        dashboardOpenAnimationPlaying =
            false;
    }

    private IEnumerator SweepRadialArc(
        Material material,
        float target,
        float delay)
    {
        if (material == null)
        {
            yield break;
        }

        if (delay > 0f)
        {
            yield return WaitUnscaled(delay);
        }

        float duration =
            Mathf.Max(
                0.01f,
                meterSweepDuration
            );

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float eased =
                EaseOutCubic(
                    Mathf.Clamp01(
                        timer / duration
                    )
                );

            SetArcFill(
                material,
                Mathf.Lerp(
                    0f,
                    target,
                    eased
                )
            );

            yield return null;
        }

        SetArcFill(material, target);
    }

    private IEnumerator FadeDashboardRect(
        RectTransform target,
        float from,
        float to,
        float duration)
    {
        if (target == null)
        {
            yield break;
        }

        CanvasGroup group =
            GetOrCreateDashboardCanvasGroup(target);

        if (group == null)
        {
            yield break;
        }

        // Never alter scale here.
        target.localScale =
            GetDashboardOriginalScale(target);

        duration =
            Mathf.Max(0.01f, duration);

        group.alpha = from;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float eased =
                EaseOutCubic(
                    Mathf.Clamp01(
                        timer / duration
                    )
                );

            group.alpha =
                Mathf.Lerp(from, to, eased);

            yield return null;
        }

        group.alpha = to;
        target.localScale =
            GetDashboardOriginalScale(target);
    }

    private void SetDashboardAlpha(
        RectTransform target,
        float alpha)
    {
        if (target == null)
        {
            return;
        }

        CanvasGroup group =
            GetOrCreateDashboardCanvasGroup(target);

        if (group != null)
        {
            group.alpha = alpha;
        }
    }

    private IEnumerator AnimateDashboardObject(
        RectTransform target,
        float overshoot)
    {
        if (target == null)
        {
            yield break;
        }

        Vector3 originalScale =
            GetDashboardOriginalScale(target);

        CanvasGroup group =
            GetOrCreateDashboardCanvasGroup(target);

        target.localScale =
            originalScale *
            dashboardStartingScale;

        if (group != null)
        {
            group.alpha =
                0f;
        }

        float duration =
            Mathf.Max(
                0.01f,
                dashboardPopDuration
            );

        float firstDuration =
            duration * 0.68f;

        float timer =
            0f;

        while (timer < firstDuration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float normalized =
                Mathf.Clamp01(
                    timer /
                    firstDuration
                );

            float eased =
                EaseOutBack(normalized);

            target.localScale =
                Vector3.LerpUnclamped(
                    originalScale *
                    dashboardStartingScale,
                    originalScale *
                    overshoot,
                    eased
                );

            if (group != null)
            {
                group.alpha =
                    Mathf.Clamp01(
                        timer /
                        Mathf.Max(
                            0.01f,
                            dashboardFadeDuration
                        )
                    );
            }

            yield return null;
        }

        float secondDuration =
            Mathf.Max(
                0.01f,
                duration -
                firstDuration
            );

        timer =
            0f;

        while (timer < secondDuration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float eased =
                EaseOutCubic(
                    Mathf.Clamp01(
                        timer /
                        secondDuration
                    )
                );

            target.localScale =
                Vector3.Lerp(
                    originalScale *
                    overshoot,
                    originalScale,
                    eased
                );

            if (group != null)
            {
                group.alpha =
                    1f;
            }

            yield return null;
        }

        target.localScale =
            originalScale;

        if (group != null)
        {
            group.alpha =
                1f;
        }
    }

    private IEnumerator SweepSlider(
        Slider slider,
        float target,
        float delay)
    {
        if (slider == null)
        {
            yield break;
        }

        if (delay > 0f)
        {
            yield return WaitUnscaled(delay);
        }

        float duration =
            Mathf.Max(
                0.01f,
                meterSweepDuration
            );

        float timer =
            0f;

        while (timer < duration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float eased =
                EaseOutCubic(
                    Mathf.Clamp01(
                        timer /
                        duration
                    )
                );

            SetSliderValue(
                slider,
                Mathf.Lerp(
                    0f,
                    target,
                    eased
                )
            );

            yield return null;
        }

        SetSliderValue(
            slider,
            target
        );
    }

    private IEnumerator WaitUnscaled(
        float duration)
    {
        float timer =
            0f;

        while (timer < duration)
        {
            timer +=
                Time.unscaledDeltaTime;

            yield return null;
        }
    }

    private CanvasGroup GetOrCreateDashboardCanvasGroup(
        RectTransform target)
    {
        if (target == null)
        {
            return null;
        }

        CanvasGroup group =
            target.GetComponent<CanvasGroup>();

        if (group == null)
        {
            group =
                target.gameObject.AddComponent<CanvasGroup>();
        }

        return group;
    }

    private void HideDashboardObject(
        RectTransform target)
    {
        if (target == null)
        {
            return;
        }

        target.localScale =
            GetDashboardOriginalScale(target) *
            dashboardStartingScale;

        CanvasGroup group =
            GetOrCreateDashboardCanvasGroup(target);

        if (group != null)
        {
            group.alpha =
                0f;
        }
    }

    private void RestoreDashboardObject(
        RectTransform target)
    {
        if (target == null)
        {
            return;
        }

        target.localScale =
            GetDashboardOriginalScale(target);

        CanvasGroup group =
            GetOrCreateDashboardCanvasGroup(target);

        if (group != null)
        {
            group.alpha =
                1f;
        }
    }

    private void RestoreDashboardVisuals()
    {
        RestoreDashboardObject(animatedTitle);
        RestoreDashboardObject(animatedStationIcon);
        RestoreDashboardObject(ringBackground);
        RestoreDashboardObject(preyIcon);
        RestoreDashboardObject(predatorIcon);
        RestoreDashboardObject(fireRiskIcon);
        RestoreDashboardObject(soilHealthIcon);
        RestoreDashboardObject(preyStatRoot);
        RestoreDashboardObject(predatorStatRoot);
        RestoreDashboardObject(fireRiskStatRoot);
        RestoreDashboardObject(soilHealthStatRoot);
    }

    // =========================================================
    // CLOSE ANIMATION
    // =========================================================

    private IEnumerator CloseAnimation()
    {
        if (panel == null)
        {
            yield break;
        }

        isClosing =
            true;

        isDragging =
            false;

        ClearCurrentRangerStationHighlight();

        dashboardOpenAnimationPlaying = false;
        RestoreDashboardVisuals();
        displayedValuesInitialised = false;

        Vector3 startScale =
            panel.localScale;

        Quaternion startRotation =
            panel.localRotation;

        float startAlpha =
            canvasGroup != null
                ? canvasGroup.alpha
                : normalAlpha;

        Vector3 targetScale =
            Vector3.one *
            closingScale;

        float duration =
            Mathf.Max(
                0.01f,
                closeDuration
            );

        float timer =
            0f;

        while (timer <
               duration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    duration
                );

            float eased =
                EaseInCubic(
                    t
                );

            panel.localScale =
                Vector3.Lerp(
                    startScale,
                    targetScale,
                    eased
                );

            panel.localRotation =
                Quaternion.Lerp(
                    startRotation,
                    Quaternion.identity,
                    eased
                );

            if (canvasGroup != null &&
                fadeWhileClosing)
            {
                canvasGroup.alpha =
                    Mathf.Lerp(
                        startAlpha,
                        0f,
                        eased
                    );
            }

            yield return null;
        }

        panel.gameObject.SetActive(
            false
        );

        panel.localScale =
            Vector3.one *
            normalScale;

        panel.localRotation =
            Quaternion.identity;

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                normalAlpha;
        }

        currentStation =
            null;

        followTarget =
            null;

        currentSwayAngle =
            0f;

        isOpen =
            false;

        isClosing =
            false;

        isDragging =
            false;

        manuallyPositioned =
            false;

        ignoreOutsideClick =
            false;

        animationCoroutine =
            null;

        if (InspectionUIManager.Instance != null)
        {
            InspectionUIManager.Instance.ClearPanel(
                this
            );
        }
    }

    // =========================================================
    // SCALE ANIMATION
    // =========================================================

    private IEnumerator ScalePanel(
        float from,
        float to,
        float duration,
        bool useBackEase)
    {
        duration =
            Mathf.Max(
                duration,
                0.01f
            );

        float timer =
            0f;

        Vector3 start =
            Vector3.one *
            from;

        Vector3 end =
            Vector3.one *
            to;

        while (timer <
               duration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    duration
                );

            float eased =
                useBackEase
                    ? EaseOutBack(t)
                    : EaseOutCubic(t);

            panel.localScale =
                Vector3.LerpUnclamped(
                    start,
                    end,
                    eased
                );

            yield return null;
        }

        panel.localScale =
            end;
    }

    // =========================================================
    // OPEN CLICK PROTECTION
    // =========================================================

    private IEnumerator ResetOutsideClickIgnore()
    {
        yield return
            new WaitForEndOfFrame();

        ignoreOutsideClick =
            false;
    }

    // =========================================================
    // GETTERS
    // =========================================================

    public RangerStation GetCurrentStation()
    {
        return currentStation;
    }

    public bool IsOpen()
    {
        return isOpen;
    }

    public bool IsDragging()
    {
        return isDragging;
    }

    // =========================================================
    // EASING
    // =========================================================

    private float EaseOutCubic(
        float x)
    {
        return
            1f -
            Mathf.Pow(
                1f - x,
                3f
            );
    }

    private float EaseInCubic(
        float x)
    {
        return
            x *
            x *
            x;
    }

    private float EaseOutBack(
        float x)
    {
        const float c1 =
            1.70158f;

        const float c3 =
            c1 + 1f;

        return
            1f +
            c3 *
            Mathf.Pow(
                x - 1f,
                3f
            ) +
            c1 *
            Mathf.Pow(
                x - 1f,
                2f
            );
    }

    private void DestroyRuntimeMaterial(
        Material material)
    {
        if (material == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(material);
        }
        else
        {
            DestroyImmediate(material);
        }
    }

    private void OnDestroy()
    {
        DestroyRuntimeMaterial(preyRadialMaterial);
        DestroyRuntimeMaterial(predatorRadialMaterial);
        DestroyRuntimeMaterial(fireRiskRadialMaterial);
        DestroyRuntimeMaterial(soilHealthRadialMaterial);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
        }
    }

}