using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class WaterPlotInspectionUI : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private SelectionWheel selectionWheel;

    [Tooltip("The ENTIRE Water Plot inspection window.")]
    [SerializeField] private RectTransform panel;

    [Tooltip("Only this top/header area can drag the window.")]
    [SerializeField] private RectTransform dragHandle;

    [Tooltip("Controls transparency of the entire window.")]
    [SerializeField] private CanvasGroup canvasGroup;

    // =========================================================
    // TEXT
    // =========================================================

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text conditionText;
    [SerializeField] private TMP_Text qualityText;

    // =========================================================
    // QUALITY
    // =========================================================

    [Header("Quality")]
    [SerializeField] private Slider qualitySlider;

    // =========================================================
    // BUTTONS
    // =========================================================

    [Header("Buttons")]
    [SerializeField] private Button cleanButton;
    [SerializeField] private Button closeButton;

    // =========================================================
    // POSITION NEXT TO PLOT
    // =========================================================

    [Header("Plot Position")]

    [SerializeField] private float horizontalOffset = 150f;
    [SerializeField] private float verticalOffset = 30f;

    [SerializeField] private float followSpeed = 15f;

    [SerializeField] private bool automaticallyFlipSide = true;

    [SerializeField] private float screenEdgePadding = 180f;

    // =========================================================
    // DRAGGING
    // =========================================================

    [Header("Dragging")]

    [SerializeField] private bool allowDragging = true;

    [Tooltip("After dragging, keep the window where the player dropped it.")]
    [SerializeField] private bool stopFollowingAfterDrag = true;

    [SerializeField] private bool clampToCanvas = true;

    [SerializeField] private float canvasPadding = 10f;

    // =========================================================
    // DRAG VISUALS
    // =========================================================

    [Header("Drag Visuals")]

    [Tooltip("How small the window becomes while being dragged.")]
    [SerializeField] private float dragScale = 0.92f;

    [Tooltip("How quickly it shrinks when picked up.")]
    [SerializeField] private float dragScaleSpeed = 12f;

    [Tooltip("Maximum rotation while dragging.")]
    [SerializeField] private float maxDragSwayAngle = 4f;

    [Tooltip("How much horizontal mouse movement affects the sway.")]
    [SerializeField] private float swayStrength = 0.15f;

    [Tooltip("How quickly the sway responds.")]
    [SerializeField] private float swaySmoothSpeed = 10f;

    [Tooltip("How quickly scale and rotation return after dropping.")]
    [SerializeField] private float dropReturnSpeed = 10f;

    // =========================================================
    // TRANSPARENCY
    // =========================================================

    [Header("Transparency")]

    [Range(0f, 1f)]
    [SerializeField] private float normalAlpha = 0.92f;

    [Range(0f, 1f)]
    [SerializeField] private float dragAlpha = 0.82f;

    [SerializeField] private float alphaSmoothSpeed = 10f;

    // =========================================================
    // OPEN ANIMATION
    // =========================================================

    [Header("Open Animation")]

    [SerializeField] private float startingScale = 0.65f;
    [SerializeField] private float popScale = 1.08f;
    [SerializeField] private float normalScale = 1f;

    [SerializeField] private float popDuration = 0.10f;
    [SerializeField] private float settleDuration = 0.10f;

    // =========================================================
    // CLOSE ANIMATION
    // =========================================================

    [Header("Close Animation")]

    [SerializeField] private float closingScale = 0.65f;
    [SerializeField] private float closeDuration = 0.14f;

    [Tooltip("Fade slightly during the close animation.")]
    [SerializeField] private bool fadeWhileClosing = true;

    // =========================================================
    // CLEANING
    // =========================================================

    [Header("Cleaning")]

    [SerializeField] private float cleaningAmount = 25f;

    // =========================================================
    // OUTSIDE CLICK
    // =========================================================

    [Header("Outside Click")]

    [Tooltip("Clicking outside the inspection window closes it.")]
    [SerializeField] private bool closeWhenClickingOutside = true;

    // =========================================================
    // MODE CHANGING
    // =========================================================

    [Header("Mode Behaviour")]

    [Tooltip("Automatically close inspection when leaving Normal mode.")]
    [SerializeField] private bool closeWhenChangingMode = true;

    [Tooltip("Also close if the Selection Wheel itself opens.")]
    [SerializeField] private bool closeWhenSelectionWheelOpens = true;

    // =========================================================
    // PRIVATE
    // =========================================================

    private WaterPlot currentWaterPlot;
    private InteractiveWaterPlot currentInteractivePlot;

    private Coroutine animationCoroutine;

    private bool isOpen;
    private bool isClosing;

    private bool isDragging;
    private bool manuallyPositioned;

    private Vector2 dragOffset;

    private bool ignoreOutsideClick;

    private Vector2 previousMousePosition;

    private float currentSwayAngle;

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
            mainCamera = Camera.main;
        }

        // =====================================================
        // CANVAS
        // =====================================================

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
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
            panel = transform as RectTransform;
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
        // SLIDER
        // =====================================================

        if (qualitySlider != null)
        {
            qualitySlider.minValue = 0f;
            qualitySlider.maxValue = 100f;
        }

        // =====================================================
        // BUTTONS
        // =====================================================

        if (cleanButton != null)
        {
            cleanButton.onClick.AddListener(
                CleanWater
            );
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(
                Close
            );
        }

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

        // =====================================================
        // CLOSE WHEN MODE CHANGES
        // =====================================================

        if (ShouldCloseBecauseOfMode())
        {
            Close();
            return;
        }

        // =====================================================
        // DRAG
        // =====================================================

        HandleDragging();

        // =====================================================
        // DRAG VISUALS
        // =====================================================

        UpdateDragVisuals();

        // =====================================================
        // CLICK OUTSIDE
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
            currentWaterPlot == null ||
            panel == null ||
            mainCamera == null)
        {
            return;
        }

        // If we manually moved it,
        // don't force it back beside the plot.

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

        // =====================================================
        // MODE CHANGED
        // =====================================================

        if (closeWhenChangingMode &&
            !selectionWheel.IsNormalMode())
        {
            return true;
        }

        // =====================================================
        // WHEEL OPENED
        // =====================================================

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
        WaterPlot waterPlot)
    {
        if (waterPlot == null ||
            panel == null)
        {
            return;
        }

        // =====================================================
        // IF ANOTHER PLOT WAS INSPECTED
        // =====================================================

        ClearCurrentPlotHighlight();

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
        // NEW TARGET
        // =====================================================

        currentWaterPlot =
            waterPlot;

        currentInteractivePlot =
            waterPlot.GetComponent<InteractiveWaterPlot>();

        if (currentInteractivePlot == null)
        {
            currentInteractivePlot =
                waterPlot.GetComponentInChildren<InteractiveWaterPlot>();
        }

        if (currentInteractivePlot == null)
        {
            currentInteractivePlot =
                waterPlot.GetComponentInParent<InteractiveWaterPlot>();
        }

        // =====================================================
        // MAKE SELECTED PLOT ORANGE
        // =====================================================

        if (currentInteractivePlot != null)
        {
            currentInteractivePlot.SetInspected(
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
        // RESET TRANSFORM
        // =====================================================

        panel.localRotation =
            Quaternion.identity;

        // =====================================================
        // ENABLE
        // =====================================================

        panel.gameObject.SetActive(
            true
        );

        // =====================================================
        // ALPHA
        // =====================================================

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                normalAlpha;
        }

        // =====================================================
        // INFORMATION
        // =====================================================

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
    }

    // =========================================================
    // RESET OPENING CLICK
    // =========================================================

    private IEnumerator ResetOutsideClickIgnore()
    {
        yield return new WaitForEndOfFrame();

        ignoreOutsideClick =
            false;
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

        // =====================================================
        // REMOVE ORANGE HIGHLIGHT
        // =====================================================

        ClearCurrentPlotHighlight();

        // =====================================================
        // RESET
        // =====================================================

        isOpen =
            false;

        isClosing =
            false;

        isDragging =
            false;

        manuallyPositioned =
            false;

        currentWaterPlot =
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
    }

    // =========================================================
    // CLEAR PLOT HIGHLIGHT
    // =========================================================

    private void ClearCurrentPlotHighlight()
    {
        if (currentInteractivePlot != null)
        {
            currentInteractivePlot.SetInspected(
                false
            );
        }

        currentInteractivePlot =
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

        // Clicked something belonging to this window.
        if (IsPointerOverInspectionUI())
        {
            return;
        }

        // Genuinely outside.
        Close();
    }

    // =========================================================
    // POINTER OVER THIS WINDOW?
    // =========================================================

    private bool IsPointerOverInspectionUI()
    {
        if (EventSystem.current == null ||
            panel == null)
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

            Transform hit =
                result.gameObject.transform;

            if (hit == panel ||
                hit.IsChildOf(panel))
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
        // BEGIN DRAG
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
                    canvas.transform as RectTransform;

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
        // DRAG
        // =====================================================

        if (isDragging &&
            Input.GetMouseButton(0))
        {
            RectTransform canvasRect =
                canvas.transform as RectTransform;

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
        // DROP
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
        // WHILE DRAGGING
        // =====================================================

        if (isDragging)
        {
            // -------------------------------------------------
            // SHRINK
            // -------------------------------------------------

            float scaleAmount =
                1f -
                Mathf.Exp(
                    -dragScaleSpeed *
                    delta
                );

            panel.localScale =
                Vector3.Lerp(
                    panel.localScale,
                    Vector3.one * dragScale,
                    scaleAmount
                );

            // -------------------------------------------------
            // SWAY
            // -------------------------------------------------

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
        // AFTER DROP
        // =====================================================

        else
        {
            float returnAmount =
                1f -
                Mathf.Exp(
                    -dropReturnSpeed *
                    delta
                );

            // -------------------------------------------------
            // RETURN SCALE
            // -------------------------------------------------

            panel.localScale =
                Vector3.Lerp(
                    panel.localScale,
                    Vector3.one * normalScale,
                    returnAmount
                );

            // -------------------------------------------------
            // RETURN ROTATION
            // -------------------------------------------------

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
            canvas.transform as RectTransform;

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
    // FOLLOW PLOT
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
        if (currentWaterPlot == null ||
            mainCamera == null ||
            canvas == null ||
            panel == null)
        {
            return panel != null
                ? panel.anchoredPosition
                : Vector2.zero;
        }

        // =====================================================
        // WORLD -> SCREEN
        // =====================================================

        Vector3 screenPosition =
            mainCamera.WorldToScreenPoint(
                currentWaterPlot.transform.position
            );

        // =====================================================
        // SIDE
        // =====================================================

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

        // =====================================================
        // SCREEN -> CANVAS
        // =====================================================

        RectTransform canvasRect =
            canvas.transform as RectTransform;

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

        panel.localScale =
            Vector3.one *
            startingScale;

        panel.localRotation =
            Quaternion.identity;

        // SMALL -> POP

        yield return ScalePanel(
            startingScale,
            popScale,
            popDuration,
            true
        );

        // POP -> NORMAL

        yield return ScalePanel(
            popScale,
            normalScale,
            settleDuration,
            false
        );

        panel.localScale =
            Vector3.one *
            normalScale;

        animationCoroutine =
            null;
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

        // =====================================================
        // REMOVE ORANGE AS CLOSE STARTS
        // =====================================================

        ClearCurrentPlotHighlight();

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
                EaseInCubic(t);

            // -------------------------------------------------
            // SCALE DOWN
            // -------------------------------------------------

            panel.localScale =
                Vector3.Lerp(
                    startScale,
                    targetScale,
                    eased
                );

            // -------------------------------------------------
            // STRAIGHTEN
            // -------------------------------------------------

            panel.localRotation =
                Quaternion.Lerp(
                    startRotation,
                    Quaternion.identity,
                    eased
                );

            // -------------------------------------------------
            // FADE
            // -------------------------------------------------

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

        // =====================================================
        // HIDE
        // =====================================================

        panel.gameObject.SetActive(
            false
        );

        // =====================================================
        // RESET
        // =====================================================

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

        currentWaterPlot =
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

        animationCoroutine =
            null;
    }

    // =========================================================
    // SCALE HELPER
    // =========================================================

    private IEnumerator ScalePanel(
        float from,
        float to,
        float duration,
        bool backEase)
    {
        duration =
            Mathf.Max(
                0.01f,
                duration
            );

        float timer =
            0f;

        Vector3 start =
            Vector3.one *
            from;

        Vector3 target =
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
                backEase
                    ? EaseOutBack(t)
                    : EaseOutCubic(t);

            panel.localScale =
                Vector3.LerpUnclamped(
                    start,
                    target,
                    eased
                );

            yield return null;
        }

        panel.localScale =
            target;
    }

    // =========================================================
    // CLEAN WATER
    // =========================================================

    public void CleanWater()
    {
        if (currentWaterPlot == null ||
            isClosing)
        {
            return;
        }

        if (currentWaterPlot.GetWaterQuality() >=
            100f)
        {
            return;
        }

        currentWaterPlot.CleanWater(
            cleaningAmount
        );

        RefreshUI();
    }

    // =========================================================
    // REFRESH UI
    // =========================================================

    private void RefreshUI()
    {
        if (currentWaterPlot == null)
        {
            return;
        }

        float quality =
            currentWaterPlot.GetWaterQuality();

        WaterPlot.WaterState state =
            currentWaterPlot.GetWaterState();

        // =====================================================
        // TITLE
        // =====================================================

        if (titleText != null)
        {
            titleText.text =
                "WATER PLOT";
        }

        // =====================================================
        // QUALITY TEXT
        // =====================================================

        if (qualityText != null)
        {
            qualityText.text =
                Mathf.RoundToInt(
                    quality
                ) +
                "%";
        }

        // =====================================================
        // SLIDER
        // =====================================================

        if (qualitySlider != null)
        {
            qualitySlider.value =
                quality;
        }

        // =====================================================
        // CONDITION
        // =====================================================

        if (conditionText != null)
        {
            switch (state)
            {
                case WaterPlot.WaterState.Clean:

                    conditionText.text =
                        "CLEAN";

                    break;

                case WaterPlot.WaterState.Dirty:

                    conditionText.text =
                        "DIRTY";

                    break;

                case WaterPlot.WaterState.Polluted:

                    conditionText.text =
                        "POLLUTED";

                    break;
            }
        }

        // =====================================================
        // CLEAN BUTTON
        // =====================================================

        if (cleanButton != null)
        {
            cleanButton.interactable =
                quality < 100f;
        }
    }

    // =========================================================
    // GETTERS
    // =========================================================

    public WaterPlot GetCurrentWaterPlot()
    {
        return currentWaterPlot;
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
}