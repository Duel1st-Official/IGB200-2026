using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CaveInspectionUI : MonoBehaviour, IInspectionPanel
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private SelectionWheel selectionWheel;

    [Tooltip("The entire Cave inspection window.")]
    [SerializeField] private RectTransform panel;

    [Tooltip("The top/header area used to drag the window.")]
    [SerializeField] private RectTransform dragHandle;

    [SerializeField] private CanvasGroup canvasGroup;

    // =========================================================
    // TEXT
    // =========================================================

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text populationText;

    // =========================================================
    // POPULATION SLIDER
    // =========================================================

    [Header("Population Trend Icon")]
    [Tooltip("Optional trend symbol positioned beside the population text.")]
    [SerializeField] private Image populationTrendIcon;

    [SerializeField] private Sprite growingTrendIcon;
    [SerializeField] private Sprite stableTrendIcon;
    [SerializeField] private Sprite dyingTrendIcon;


    // =========================================================
    // ICON
    // =========================================================

    [Header("Icon")]
    [SerializeField] private Image caveIcon;

    // =========================================================
    // RADIAL COLONY STATS
    // =========================================================

    [Header("Radial Colony Stats")]
    [Tooltip("The coloured Population arc Image.")]
    [SerializeField] private Image populationRadialFill;

    [Tooltip("The coloured Health arc Image.")]
    [SerializeField] private Image healthRadialFill;

    [Tooltip("The coloured Food arc Image.")]
    [SerializeField] private Image foodRadialFill;

    [Tooltip("The coloured Water arc Image.")]
    [SerializeField] private Image waterRadialFill;

    [Header("Radial Arc Shader")]
    [Tooltip("Material using GhostBat/UI/QuarterArcFill. A separate runtime copy is made for each stat.")]
    [SerializeField] private Material radialArcMaterial;

    [Header("Radial Arc Angles")]
    [Range(0f, 360f)][SerializeField] private float populationStartAngle = 180f;
    [Range(0f, 360f)][SerializeField] private float healthStartAngle = 90f;
    [Range(0f, 360f)][SerializeField] private float foodStartAngle = 0f;
    [Range(0f, 360f)][SerializeField] private float waterStartAngle = 270f;
    [Range(1f, 180f)][SerializeField] private float radialArcAngle = 90f;
    [SerializeField] private bool fillClockwise = true;

    [Header("Radial Stat Text")]
    [SerializeField] private TMP_Text healthRadialText;
    [SerializeField] private TMP_Text foodRadialText;
    [SerializeField] private TMP_Text waterRadialText;

    [Header("Radial Animation")]
    [SerializeField] private bool animateRadialStats = true;
    [Min(0.01f)][SerializeField] private float radialFillSpeed = 8f;

    [Header("Radial Hover")]
    [SerializeField] private bool enableRadialHover = true;
    [Tooltip("Assign an UNMASKED parent containing the mask + fill for each quarter. This lets the whole quarter grow without being clipped.")]
    [SerializeField] private RectTransform populationHoverRoot;
    [SerializeField] private RectTransform healthHoverRoot;
    [SerializeField] private RectTransform foodHoverRoot;
    [SerializeField] private RectTransform waterHoverRoot;
    [Min(1f)][SerializeField] private float hoveredArcScale = 1.15f;
    [Min(1f)][SerializeField] private float hoveredIconScale = 1.18f;
    [Min(0.01f)][SerializeField] private float hoverScaleSpeed = 14f;
    [Range(0f, 1f)][SerializeField] private float nonHoveredSaturation = 0f;
    [Tooltip("Optional shared hover label. If empty, Health/Food/Water use their existing percentage text.")]
    [SerializeField] private TMP_Text hoverStatText;

    // =========================================================
    // BUTTONS
    // =========================================================

    [Header("Buttons")]
    [SerializeField] private Button closeButton;

    // =========================================================
    // POSITION
    // =========================================================

    [Header("Cave Position")]
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

    [Header("Detailed Open Animation")]
    [SerializeField] private bool animateContentsOnOpen = true;
    [Min(0f)][SerializeField] private float contentStartDelay = 0.015f;
    [Min(0.01f)][SerializeField] private float contentFadeDuration = 0.10f;
    [Min(0.01f)][SerializeField] private float contentPopDuration = 0.13f;
    [Range(0.1f, 1f)][SerializeField] private float contentStartingScale = 0.65f;
    [Min(1f)][SerializeField] private float contentOvershootScale = 1.12f;
    [Min(0f)][SerializeField] private float itemStagger = 0.025f;
    [Min(0.01f)][SerializeField] private float arcSweepDuration = 0.24f;

    [Header("Open Animation Objects")]
    [Tooltip("Optional. If left empty the script uses Title Text.")]
    [SerializeField] private RectTransform animatedTitle;

    [Tooltip("Usually the cave Image in the centre of the radial ring.")]
    [SerializeField] private RectTransform animatedCaveIcon;

    [Tooltip("Optional brown/background ring. Assign this if you want it to pop in separately.")]
    [SerializeField] private RectTransform animatedRingBackground;

    [Tooltip("Circular Population/Bat icon.")]
    [SerializeField] private RectTransform animatedPopulationIcon;

    [Tooltip("Circular Health/Heart icon.")]
    [SerializeField] private RectTransform animatedHealthIcon;

    [Tooltip("Circular Food icon.")]
    [SerializeField] private RectTransform animatedFoodIcon;

    [Tooltip("Circular Water icon.")]
    [SerializeField] private RectTransform animatedWaterIcon;

    [Tooltip("Optional. If left empty the script uses Population Text.")]
    [SerializeField] private RectTransform animatedPopulationText;

    private void RestoreOpenAnimationVisuals()
    {
        RestoreAnimatedObject(animatedTitle);
        RestoreAnimatedObject(animatedCaveIcon);
        RestoreAnimatedObject(animatedRingBackground);
        RestoreAnimatedObject(animatedPopulationIcon);
        RestoreAnimatedObject(animatedHealthIcon);
        RestoreAnimatedObject(animatedFoodIcon);
        RestoreAnimatedObject(animatedWaterIcon);
        RestoreAnimatedObject(animatedPopulationText);

        SetGraphicAlpha(healthRadialText, 1f);
        SetGraphicAlpha(foodRadialText, 1f);
        SetGraphicAlpha(waterRadialText, 1f);

        if (populationTrendIcon != null)
        {
            SetGraphicAlpha(populationTrendIcon, 1f);
        }
    }

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
    // PRIVATE
    // =========================================================

    private BatColony currentColony;
    private InspectableCave currentInspectableCave;

    private Coroutine animationCoroutine;

    private bool isOpen;
    private bool isClosing;
    private bool isDragging;
    private bool manuallyPositioned;
    private bool ignoreOutsideClick;

    private Vector2 dragOffset;
    private Vector2 previousMousePosition;

    private float currentSwayAngle;

    private float displayedPopulationFill;
    private float displayedHealthFill;
    private float displayedFoodFill;
    private float displayedWaterFill;
    private bool radialValuesInitialised;

    private Material populationRadialMaterial;
    private Material healthRadialMaterial;
    private Material foodRadialMaterial;
    private Material waterRadialMaterial;

    private int hoveredRadialStat = -1;
    private Vector3 populationHoverBaseScale = Vector3.one;
    private Vector3 healthHoverBaseScale = Vector3.one;
    private Vector3 foodHoverBaseScale = Vector3.one;
    private Vector3 waterHoverBaseScale = Vector3.one;
    private Vector3 populationIconHoverBaseScale = Vector3.one;
    private Vector3 healthIconHoverBaseScale = Vector3.one;
    private Vector3 foodIconHoverBaseScale = Vector3.one;
    private Vector3 waterIconHoverBaseScale = Vector3.one;

    private bool contentOpenAnimationPlaying;
    private float openingPopulationTarget;
    private float openingHealthTarget;
    private float openingFoodTarget;
    private float openingWaterTarget;

    private readonly Dictionary<RectTransform, Vector3> openAnimationOriginalScales =
        new Dictionary<RectTransform, Vector3>();

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }

        if (selectionWheel == null)
        {
            selectionWheel =
                FindFirstObjectByType<SelectionWheel>();
        }

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
        // CLOSE BUTTON
        // =====================================================

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(
                Close
            );
        }

        SetupRadialMaterials();
        SetupRadialHoverEvents();
        CacheDefaultAnimationObjects();
        CacheOpenAnimationOriginalScales();
        CacheRadialHoverBaseScales();

        // =====================================================
        // START CLOSED
        // =====================================================

        if (panel != null)
        {
            panel.gameObject.SetActive(
                false
            );
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!isOpen ||
            isClosing ||
            panel == null)
        {
            return;
        }

        if (ShouldCloseBecauseOfMode())
        {
            Close();
            return;
        }

        HandleDragging();

        UpdateDragVisuals();
        UpdateRadialHoverVisuals();

        // =====================================================
        // LIVE COLONY HUD
        // =====================================================

        RefreshUI();

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
            currentColony == null ||
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
    // OPEN
    // =========================================================

    public void Open(
        BatColony colony)
    {
        if (colony == null ||
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

        ClearCurrentCaveHighlight();

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
        // COLONY
        // =====================================================

        currentColony =
            colony;

        // =====================================================
        // FIND INSPECTABLE CAVE
        // =====================================================

        currentInspectableCave =
            colony.GetComponent<InspectableCave>();

        if (currentInspectableCave == null)
        {
            currentInspectableCave =
                colony.GetComponentInChildren<InspectableCave>();
        }

        if (currentInspectableCave == null)
        {
            currentInspectableCave =
                colony.GetComponentInParent<InspectableCave>();
        }

        // =====================================================
        // HIGHLIGHT
        // =====================================================

        if (currentInspectableCave != null)
        {
            currentInspectableCave.SetInspected(
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
        // FORCE UI UPDATE
        // =====================================================

        SetupRadialMaterials();
        SetupRadialHoverEvents();
        CacheRadialHoverBaseScales();
        hoveredRadialStat = -1;
        ResetRadialAnimation();
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
        // TUTORIAL - GHOST BAT CAVE INSPECTED
        // =====================================================

        TutorialEvents.Report(
            TutorialAction.CaveInspected
        );
    }

    // =========================================================
    // REFRESH UI
    // =========================================================

    public void RefreshUI()
    {
        if (currentColony == null)
        {
            return;
        }

        // =====================================================
        // TITLE
        // =====================================================

        if (titleText != null)
        {
            titleText.text = "GHOST BAT COLONY";
        }

        // =====================================================
        // POPULATION
        // =====================================================

        if (populationText != null)
        {
            populationText.text =
                "Population: " +
                currentColony.GetPopulation() +
                " (" +
                FormatPopulationTrendTitle(
                    currentColony.GetPopulationTrendText()
                ) +
                ")";
        }

        RefreshPopulationTrendIcon();

        // =====================================================
        // COLONY VALUES
        // =====================================================

        float health =
            Mathf.Clamp(
                currentColony.GetBatHealth(),
                0f,
                100f
            );

        float food =
            Mathf.Clamp(
                currentColony.GetBatFood(),
                0f,
                100f
            );

        float water =
            Mathf.Clamp(
                currentColony.GetBatWater(),
                0f,
                100f
            );

        RefreshRadialStats(health, food, water);
    }

    private string FormatPopulationTrendTitle(string trend)
    {
        if (string.IsNullOrEmpty(trend))
        {
            return "Stable";
        }

        trend = trend.ToLowerInvariant();

        return
            char.ToUpperInvariant(trend[0]) +
            trend.Substring(1);
    }

    private void RefreshPopulationTrendIcon()
    {
        if (populationTrendIcon == null ||
            currentColony == null)
        {
            return;
        }

        Sprite targetSprite = null;

        switch (currentColony.GetPopulationTrend())
        {
            case BatColony.ColonyTrend.Growing:
                targetSprite = growingTrendIcon;
                break;

            case BatColony.ColonyTrend.Dying:
                targetSprite = dyingTrendIcon;
                break;

            default:
                targetSprite = stableTrendIcon;
                break;
        }

        populationTrendIcon.sprite = targetSprite;
        populationTrendIcon.enabled = targetSprite != null;
        populationTrendIcon.preserveAspect = true;
    }

    // =========================================================
    // POPULATION TREND ICON
    // =========================================================

    // =========================================================
    // RADIAL COLONY STATS
    // =========================================================

    private void RefreshRadialStats(float health, float food, float water)
    {
        if (currentColony == null) return;

        if (contentOpenAnimationPlaying)
        {
            UpdateRadialHoverText(health, food, water);
            return;
        }

        float populationTarget = Mathf.Clamp01(
            currentColony.GetPopulation() /
            (float)Mathf.Max(1, currentColony.GetMaximumPopulation()));

        float healthTarget = Mathf.Clamp01(health / 100f);
        float foodTarget = Mathf.Clamp01(food / 100f);
        float waterTarget = Mathf.Clamp01(water / 100f);

        if (!radialValuesInitialised || !animateRadialStats)
        {
            displayedPopulationFill = populationTarget;
            displayedHealthFill = healthTarget;
            displayedFoodFill = foodTarget;
            displayedWaterFill = waterTarget;
            radialValuesInitialised = true;
        }
        else
        {
            float smoothing = 1f - Mathf.Exp(
                -Mathf.Max(0.01f, radialFillSpeed) * Time.unscaledDeltaTime);

            displayedPopulationFill = Mathf.Lerp(displayedPopulationFill, populationTarget, smoothing);
            displayedHealthFill = Mathf.Lerp(displayedHealthFill, healthTarget, smoothing);
            displayedFoodFill = Mathf.Lerp(displayedFoodFill, foodTarget, smoothing);
            displayedWaterFill = Mathf.Lerp(displayedWaterFill, waterTarget, smoothing);
        }

        SetArcFill(
            populationRadialMaterial,
            displayedPopulationFill
        );

        SetArcFill(
            healthRadialMaterial,
            displayedHealthFill
        );

        SetArcFill(
            foodRadialMaterial,
            displayedFoodFill
        );

        SetArcFill(
            waterRadialMaterial,
            displayedWaterFill
        );

        UpdateRadialHoverText(health, food, water);
    }


    private void UpdateRadialHoverText(float health, float food, float water)
    {
        int population = currentColony != null ? currentColony.GetPopulation() : 0;

        if (healthRadialText != null)
            healthRadialText.text =
                hoveredRadialStat == 1 && hoverStatText == null
                    ? Mathf.RoundToInt(health) + "% Health"
                    : Mathf.RoundToInt(health) + "%";

        if (foodRadialText != null)
            foodRadialText.text =
                hoveredRadialStat == 2 && hoverStatText == null
                    ? Mathf.RoundToInt(food) + "% Food"
                    : Mathf.RoundToInt(food) + "%";

        if (waterRadialText != null)
            waterRadialText.text =
                hoveredRadialStat == 3 && hoverStatText == null
                    ? Mathf.RoundToInt(water) + "% Water"
                    : Mathf.RoundToInt(water) + "%";

        if (hoverStatText == null) return;

        switch (hoveredRadialStat)
        {
            case 0:
                hoverStatText.text = population + " Population";
                break;
            case 1:
                hoverStatText.text = Mathf.RoundToInt(health) + "% Health";
                break;
            case 2:
                hoverStatText.text = Mathf.RoundToInt(food) + "% Food";
                break;
            case 3:
                hoverStatText.text = Mathf.RoundToInt(water) + "% Water";
                break;
            default:
                hoverStatText.text = "";
                break;
        }
    }

    private void SetupRadialHoverEvents()
    {
        SetupHoverEvent(populationRadialFill, 0);
        SetupHoverEvent(healthRadialFill, 1);
        SetupHoverEvent(foodRadialFill, 2);
        SetupHoverEvent(waterRadialFill, 3);
    }

    private void SetupHoverEvent(Image image, int statIndex)
    {
        if (image == null) return;

        image.raycastTarget = true;

        CaveRadialHoverTarget target =
            image.GetComponent<CaveRadialHoverTarget>();

        if (target == null)
            target = image.gameObject.AddComponent<CaveRadialHoverTarget>();

        target.Configure(this, statIndex);
    }

    public void SetRadialHoverStat(int statIndex)
    {
        hoveredRadialStat = enableRadialHover ? statIndex : -1;
    }

    private void CacheRadialHoverBaseScales()
    {
        populationHoverBaseScale =
            HoverScaleOf(HoverTarget(populationHoverRoot, populationRadialFill));
        healthHoverBaseScale =
            HoverScaleOf(HoverTarget(healthHoverRoot, healthRadialFill));
        foodHoverBaseScale =
            HoverScaleOf(HoverTarget(foodHoverRoot, foodRadialFill));
        waterHoverBaseScale =
            HoverScaleOf(HoverTarget(waterHoverRoot, waterRadialFill));

        populationIconHoverBaseScale = HoverScaleOf(animatedPopulationIcon);
        healthIconHoverBaseScale = HoverScaleOf(animatedHealthIcon);
        foodIconHoverBaseScale = HoverScaleOf(animatedFoodIcon);
        waterIconHoverBaseScale = HoverScaleOf(animatedWaterIcon);
    }

    private RectTransform HoverTarget(RectTransform root, Image fallback)
    {
        if (root != null) return root;
        return fallback != null ? fallback.rectTransform : null;
    }

    private Vector3 HoverScaleOf(RectTransform target)
    {
        return target != null ? target.localScale : Vector3.one;
    }

    private void UpdateRadialHoverVisuals()
    {
        if (!enableRadialHover)
        {
            hoveredRadialStat = -1;
        }

        if (contentOpenAnimationPlaying) return;

        SmoothHoverScale(
            HoverTarget(populationHoverRoot, populationRadialFill),
            populationHoverBaseScale, hoveredRadialStat == 0, hoveredArcScale);

        SmoothHoverScale(
            HoverTarget(healthHoverRoot, healthRadialFill),
            healthHoverBaseScale, hoveredRadialStat == 1, hoveredArcScale);

        SmoothHoverScale(
            HoverTarget(foodHoverRoot, foodRadialFill),
            foodHoverBaseScale, hoveredRadialStat == 2, hoveredArcScale);

        SmoothHoverScale(
            HoverTarget(waterHoverRoot, waterRadialFill),
            waterHoverBaseScale, hoveredRadialStat == 3, hoveredArcScale);

        SmoothHoverScale(
            animatedPopulationIcon, populationIconHoverBaseScale,
            hoveredRadialStat == 0, hoveredIconScale);

        SmoothHoverScale(
            animatedHealthIcon, healthIconHoverBaseScale,
            hoveredRadialStat == 1, hoveredIconScale);

        SmoothHoverScale(
            animatedFoodIcon, foodIconHoverBaseScale,
            hoveredRadialStat == 2, hoveredIconScale);

        SmoothHoverScale(
            animatedWaterIcon, waterIconHoverBaseScale,
            hoveredRadialStat == 3, hoveredIconScale);

        bool hovering = hoveredRadialStat >= 0;

        SetArcSaturation(populationRadialMaterial,
            !hovering || hoveredRadialStat == 0 ? 1f : nonHoveredSaturation);
        SetArcSaturation(healthRadialMaterial,
            !hovering || hoveredRadialStat == 1 ? 1f : nonHoveredSaturation);
        SetArcSaturation(foodRadialMaterial,
            !hovering || hoveredRadialStat == 2 ? 1f : nonHoveredSaturation);
        SetArcSaturation(waterRadialMaterial,
            !hovering || hoveredRadialStat == 3 ? 1f : nonHoveredSaturation);
    }

    private void SmoothHoverScale(
        RectTransform target,
        Vector3 baseScale,
        bool hovered,
        float multiplier)
    {
        if (target == null) return;

        Vector3 wanted = baseScale * (hovered ? multiplier : 1f);
        float amount = 1f - Mathf.Exp(
            -Mathf.Max(0.01f, hoverScaleSpeed) * Time.unscaledDeltaTime);

        target.localScale =
            Vector3.Lerp(target.localScale, wanted, amount);
    }

    private void SetArcSaturation(Material material, float saturation)
    {
        if (material == null || !material.HasProperty("_Saturation")) return;
        material.SetFloat("_Saturation", Mathf.Clamp01(saturation));
    }

    private void ResetRadialHover()
    {
        hoveredRadialStat = -1;

        RestoreHoverScale(
            HoverTarget(populationHoverRoot, populationRadialFill),
            populationHoverBaseScale);
        RestoreHoverScale(
            HoverTarget(healthHoverRoot, healthRadialFill),
            healthHoverBaseScale);
        RestoreHoverScale(
            HoverTarget(foodHoverRoot, foodRadialFill),
            foodHoverBaseScale);
        RestoreHoverScale(
            HoverTarget(waterHoverRoot, waterRadialFill),
            waterHoverBaseScale);

        RestoreHoverScale(animatedPopulationIcon, populationIconHoverBaseScale);
        RestoreHoverScale(animatedHealthIcon, healthIconHoverBaseScale);
        RestoreHoverScale(animatedFoodIcon, foodIconHoverBaseScale);
        RestoreHoverScale(animatedWaterIcon, waterIconHoverBaseScale);

        SetArcSaturation(populationRadialMaterial, 1f);
        SetArcSaturation(healthRadialMaterial, 1f);
        SetArcSaturation(foodRadialMaterial, 1f);
        SetArcSaturation(waterRadialMaterial, 1f);

        if (hoverStatText != null) hoverStatText.text = "";
    }

    private void RestoreHoverScale(RectTransform target, Vector3 scale)
    {
        if (target != null) target.localScale = scale;
    }

    private void SetupRadialMaterials()
    {
        SetupArcMaterial(
            populationRadialFill,
            ref populationRadialMaterial,
            populationStartAngle
        );

        SetupArcMaterial(
            healthRadialFill,
            ref healthRadialMaterial,
            healthStartAngle
        );

        SetupArcMaterial(
            foodRadialFill,
            ref foodRadialMaterial,
            foodStartAngle
        );

        SetupArcMaterial(
            waterRadialFill,
            ref waterRadialMaterial,
            waterStartAngle
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
                " (Cave Runtime)";
        }

        image.material =
            runtimeMaterial;

        image.type =
            Image.Type.Simple;

        image.preserveAspect =
            true;

        // Needed for hover detection on the visible quarter.
        image.raycastTarget =
            true;

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
            runtimeMaterial.SetFloat("_Saturation", 1f);
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

    private void ResetRadialAnimation()
    {
        radialValuesInitialised = false;
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

        ClearCurrentCaveHighlight();
        ResetRadialHover();
        ResetRadialAnimation();
        contentOpenAnimationPlaying = false;
        RestoreOpenAnimationVisuals();

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

        currentColony =
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
    // CLEAR CAVE HIGHLIGHT
    // =========================================================

    private void ClearCurrentCaveHighlight()
    {
        if (currentInspectableCave != null)
        {
            currentInspectableCave.SetInspected(
                false
            );
        }

        currentInspectableCave =
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
    // FOLLOW CAVE
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
        if (currentColony == null ||
            mainCamera == null ||
            canvas == null ||
            panel == null)
        {
            return panel != null
                ? panel.anchoredPosition
                : Vector2.zero;
        }

        Vector3 screenPosition =
            mainCamera.WorldToScreenPoint(
                currentColony.transform.position
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

        CacheDefaultAnimationObjects();
        CacheOpenAnimationOriginalScales();

        panel.localScale =
            Vector3.one *
            startingScale;

        panel.localRotation =
            Quaternion.identity;

        if (animateContentsOnOpen)
        {
            PrepareContentsForOpenAnimation();
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

        if (animateContentsOnOpen)
        {
            yield return AnimateContentsOpen();
        }

        animationCoroutine =
            null;
    }

    private void CacheDefaultAnimationObjects()
    {
        if (animatedTitle == null &&
            titleText != null)
        {
            animatedTitle =
                titleText.rectTransform;
        }

        if (animatedCaveIcon == null &&
            caveIcon != null)
        {
            animatedCaveIcon =
                caveIcon.rectTransform;
        }

        if (animatedPopulationText == null &&
            populationText != null)
        {
            animatedPopulationText =
                populationText.rectTransform;
        }
    }

    private void PrepareContentsForOpenAnimation()
    {
        contentOpenAnimationPlaying = true;

        openingPopulationTarget =
            currentColony != null
                ? Mathf.Clamp01(
                    currentColony.GetPopulation() /
                    (float)Mathf.Max(
                        1,
                        currentColony.GetMaximumPopulation()
                    )
                )
                : 0f;

        openingHealthTarget =
            currentColony != null
                ? Mathf.Clamp01(
                    currentColony.GetBatHealth() / 100f
                )
                : 0f;

        openingFoodTarget =
            currentColony != null
                ? Mathf.Clamp01(
                    currentColony.GetBatFood() / 100f
                )
                : 0f;

        openingWaterTarget =
            currentColony != null
                ? Mathf.Clamp01(
                    currentColony.GetBatWater() / 100f
                )
                : 0f;

        SetArcFill(
            populationRadialMaterial,
            0f
        );

        SetArcFill(
            healthRadialMaterial,
            0f
        );

        SetArcFill(
            foodRadialMaterial,
            0f
        );

        SetArcFill(
            waterRadialMaterial,
            0f
        );

        SetAnimatedObjectHidden(animatedTitle);
        SetAnimatedObjectHidden(animatedCaveIcon);

        // The brown ring keeps its authored size, but starts transparent.
        RestoreAnimatedObject(animatedRingBackground);
        SetRectAlpha(animatedRingBackground, 0f);

        SetAnimatedObjectHidden(animatedPopulationIcon);
        SetAnimatedObjectHidden(animatedHealthIcon);
        SetAnimatedObjectHidden(animatedFoodIcon);
        SetAnimatedObjectHidden(animatedWaterIcon);
        SetAnimatedObjectHidden(animatedPopulationText);

        SetGraphicAlpha(
            healthRadialText,
            0f
        );

        SetGraphicAlpha(
            foodRadialText,
            0f
        );

        SetGraphicAlpha(
            waterRadialText,
            0f
        );

        if (populationTrendIcon != null)
        {
            SetGraphicAlpha(
                populationTrendIcon,
                0f
            );
        }
    }

    private IEnumerator AnimateContentsOpen()
    {
        if (contentStartDelay > 0f)
        {
            yield return WaitUnscaled(
                contentStartDelay
            );
        }

        yield return AnimateRectPop(
            animatedTitle,
            1f
        );

        yield return WaitUnscaled(
            itemStagger
        );

        yield return AnimateRectPop(
            animatedCaveIcon,
            contentOvershootScale
        );

        yield return WaitUnscaled(
            itemStagger * 0.5f
        );

        // Brown ring background fades in quickly without changing size.
        StartCoroutine(
            FadeRect(
                animatedRingBackground,
                0f,
                1f,
                contentFadeDuration
            )
        );

        StartCoroutine(
            AnimateArcSweep(
                populationRadialMaterial,
                openingPopulationTarget,
                0f
            )
        );

        yield return WaitUnscaled(
            itemStagger
        );

        StartCoroutine(
            AnimateRectPop(
                animatedPopulationIcon,
                contentOvershootScale
            )
        );

        StartCoroutine(
            AnimateArcSweep(
                healthRadialMaterial,
                openingHealthTarget,
                itemStagger
            )
        );

        yield return WaitUnscaled(
            itemStagger
        );

        StartCoroutine(
            AnimateRectPop(
                animatedHealthIcon,
                contentOvershootScale
            )
        );

        StartCoroutine(
            AnimateArcSweep(
                foodRadialMaterial,
                openingFoodTarget,
                itemStagger * 2f
            )
        );

        yield return WaitUnscaled(
            itemStagger
        );

        StartCoroutine(
            AnimateRectPop(
                animatedFoodIcon,
                contentOvershootScale
            )
        );

        StartCoroutine(
            AnimateArcSweep(
                waterRadialMaterial,
                openingWaterTarget,
                itemStagger * 3f
            )
        );

        yield return WaitUnscaled(
            itemStagger
        );

        StartCoroutine(
            AnimateRectPop(
                animatedWaterIcon,
                contentOvershootScale
            )
        );

        StartCoroutine(
            FadeGraphic(
                healthRadialText,
                0f,
                1f,
                contentFadeDuration
            )
        );

        StartCoroutine(
            FadeGraphic(
                foodRadialText,
                0f,
                1f,
                contentFadeDuration
            )
        );

        StartCoroutine(
            FadeGraphic(
                waterRadialText,
                0f,
                1f,
                contentFadeDuration
            )
        );

        if (populationTrendIcon != null)
        {
            StartCoroutine(
                FadeGraphic(
                    populationTrendIcon,
                    0f,
                    1f,
                    contentFadeDuration
                )
            );
        }

        yield return AnimateRectPop(
            animatedPopulationText,
            1.05f
        );

        float remaining =
            arcSweepDuration +
            itemStagger * 3f;

        if (remaining > 0f)
        {
            yield return WaitUnscaled(
                remaining
            );
        }

        SetArcFill(
            populationRadialMaterial,
            openingPopulationTarget
        );

        SetArcFill(
            healthRadialMaterial,
            openingHealthTarget
        );

        SetArcFill(
            foodRadialMaterial,
            openingFoodTarget
        );

        SetArcFill(
            waterRadialMaterial,
            openingWaterTarget
        );

        displayedPopulationFill =
            openingPopulationTarget;

        displayedHealthFill =
            openingHealthTarget;

        displayedFoodFill =
            openingFoodTarget;

        displayedWaterFill =
            openingWaterTarget;

        radialValuesInitialised =
            true;

        RestoreAnimatedObject(animatedTitle);
        RestoreAnimatedObject(animatedCaveIcon);
        RestoreAnimatedObject(animatedRingBackground);
        RestoreAnimatedObject(animatedPopulationIcon);
        RestoreAnimatedObject(animatedHealthIcon);
        RestoreAnimatedObject(animatedFoodIcon);
        RestoreAnimatedObject(animatedWaterIcon);
        RestoreAnimatedObject(animatedPopulationText);

        SetGraphicAlpha(
            healthRadialText,
            1f
        );

        SetGraphicAlpha(
            foodRadialText,
            1f
        );

        SetGraphicAlpha(
            waterRadialText,
            1f
        );

        if (populationTrendIcon != null)
        {
            SetGraphicAlpha(
                populationTrendIcon,
                1f
            );
        }

        contentOpenAnimationPlaying =
            false;
    }

    private IEnumerator AnimateArcSweep(
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
            yield return WaitUnscaled(
                delay
            );
        }

        float duration =
            Mathf.Max(
                0.01f,
                arcSweepDuration
            );

        float timer =
            0f;

        while (timer < duration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    duration
                );

            float eased =
                EaseOutCubic(t);

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

        SetArcFill(
            material,
            target
        );
    }

    private IEnumerator AnimateRectPop(
        RectTransform target,
        float overshoot)
    {
        if (target == null)
        {
            yield break;
        }

        Vector3 originalScale =
            GetOriginalAnimationScale(target);

        CanvasGroup group =
            GetOrCreateCanvasGroup(target);

        if (group != null)
        {
            group.alpha =
                0f;
        }

        target.localScale =
            originalScale *
            contentStartingScale;

        float duration =
            Mathf.Max(
                0.01f,
                contentPopDuration
            );

        float firstPart =
            duration * 0.68f;

        float timer =
            0f;

        while (timer < firstPart)
        {
            timer +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    firstPart
                );

            float eased =
                EaseOutBack(t);

            target.localScale =
                Vector3.LerpUnclamped(
                    originalScale *
                    contentStartingScale,
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
                            contentFadeDuration
                        )
                    );
            }

            yield return null;
        }

        float secondPart =
            Mathf.Max(
                0.01f,
                duration - firstPart
            );

        timer =
            0f;

        while (timer < secondPart)
        {
            timer +=
                Time.unscaledDeltaTime;

            float t =
                EaseOutCubic(
                    Mathf.Clamp01(
                        timer /
                        secondPart
                    )
                );

            target.localScale =
                Vector3.Lerp(
                    originalScale *
                    overshoot,
                    originalScale,
                    t
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

    private IEnumerator FadeGraphic(
        Graphic graphic,
        float from,
        float to,
        float duration)
    {
        if (graphic == null)
        {
            yield break;
        }

        Color original =
            graphic.color;

        duration =
            Mathf.Max(
                0.01f,
                duration
            );

        float timer =
            0f;

        while (timer < duration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float t =
                EaseOutCubic(
                    Mathf.Clamp01(
                        timer /
                        duration
                    )
                );

            Color color =
                original;

            color.a =
                Mathf.Lerp(
                    from,
                    to,
                    t
                );

            graphic.color =
                color;

            yield return null;
        }

        original.a =
            to;

        graphic.color =
            original;
    }

    private IEnumerator WaitUnscaled(float duration)
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

    private CanvasGroup GetOrCreateCanvasGroup(
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

    private void CacheOpenAnimationOriginalScales()
    {
        CacheOriginalAnimationScale(animatedTitle);
        CacheOriginalAnimationScale(animatedCaveIcon);
        CacheOriginalAnimationScale(animatedPopulationIcon);
        CacheOriginalAnimationScale(animatedHealthIcon);
        CacheOriginalAnimationScale(animatedFoodIcon);
        CacheOriginalAnimationScale(animatedWaterIcon);
        CacheOriginalAnimationScale(animatedPopulationText);

        // Ring background is intentionally NOT animated/scaled.
        // It stays at its authored size at all times.
        if (animatedRingBackground != null)
        {
            CacheOriginalAnimationScale(animatedRingBackground);
            RestoreAnimatedObject(animatedRingBackground);
        }
    }

    private void CacheOriginalAnimationScale(
        RectTransform target)
    {
        if (target == null ||
            openAnimationOriginalScales.ContainsKey(target))
        {
            return;
        }

        openAnimationOriginalScales.Add(
            target,
            target.localScale
        );
    }

    private Vector3 GetOriginalAnimationScale(
        RectTransform target)
    {
        if (target == null)
        {
            return Vector3.one;
        }

        if (openAnimationOriginalScales.TryGetValue(
            target,
            out Vector3 originalScale))
        {
            return originalScale;
        }

        originalScale =
            target.localScale;

        if (originalScale == Vector3.zero)
        {
            originalScale =
                Vector3.one;
        }

        openAnimationOriginalScales[target] =
            originalScale;

        return originalScale;
    }

    private void SetAnimatedObjectHidden(
        RectTransform target)
    {
        if (target == null)
        {
            return;
        }

        CanvasGroup group =
            GetOrCreateCanvasGroup(target);

        if (group != null)
        {
            group.alpha =
                0f;
        }

        Vector3 originalScale =
            GetOriginalAnimationScale(target);

        target.localScale =
            originalScale *
            contentStartingScale;
    }

    private void RestoreAnimatedObject(
        RectTransform target)
    {
        if (target == null)
        {
            return;
        }

        target.localScale =
            GetOriginalAnimationScale(target);

        CanvasGroup group =
            GetOrCreateCanvasGroup(target);

        if (group != null)
        {
            group.alpha =
                1f;
        }
    }

    private IEnumerator FadeRect(
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
            GetOrCreateCanvasGroup(target);

        if (group == null)
        {
            yield break;
        }

        duration =
            Mathf.Max(0.01f, duration);

        group.alpha = from;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float normalized =
                Mathf.Clamp01(timer / duration);

            group.alpha =
                Mathf.Lerp(
                    from,
                    to,
                    EaseOutCubic(normalized)
                );

            yield return null;
        }

        group.alpha = to;
    }

    private void SetRectAlpha(
        RectTransform target,
        float alpha)
    {
        if (target == null)
        {
            return;
        }

        CanvasGroup group =
            GetOrCreateCanvasGroup(target);

        if (group != null)
        {
            group.alpha = alpha;
        }
    }

    private void SetGraphicAlpha(
        Graphic graphic,
        float alpha)
    {
        if (graphic == null)
        {
            return;
        }

        Color color =
            graphic.color;

        color.a =
            alpha;

        graphic.color =
            color;
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

        ClearCurrentCaveHighlight();
        ResetRadialHover();
        ResetRadialAnimation();
        contentOpenAnimationPlaying = false;
        RestoreOpenAnimationVisuals();

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

        while (timer < duration)
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

        currentColony =
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

        while (timer < duration)
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

    public BatColony GetCurrentColony()
    {
        return currentColony;
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

    // =========================================================
    // CLEANUP
    // =========================================================

    private void DestroyRuntimeMaterial(Material material)
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
        DestroyRuntimeMaterial(populationRadialMaterial);
        DestroyRuntimeMaterial(healthRadialMaterial);
        DestroyRuntimeMaterial(foodRadialMaterial);
        DestroyRuntimeMaterial(waterRadialMaterial);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(
                Close
            );
        }
    }
}

public class CaveRadialHoverTarget :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    private CaveInspectionUI owner;
    private int statIndex = -1;

    public void Configure(CaveInspectionUI targetOwner, int targetStatIndex)
    {
        owner = targetOwner;
        statIndex = targetStatIndex;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner != null)
            owner.SetRadialHoverStat(statIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (owner != null)
            owner.SetRadialHoverStat(-1);
    }
}
