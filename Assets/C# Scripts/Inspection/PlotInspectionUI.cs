using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PlotInspectionUI : MonoBehaviour, IInspectionPanel
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private SelectionWheel selectionWheel;

    [Tooltip("The entire Farm Plot inspection window.")]
    [SerializeField] private RectTransform panel;

    [Tooltip("The top/header area used to drag the window.")]
    [SerializeField] private RectTransform dragHandle;

    [SerializeField] private CanvasGroup canvasGroup;

    // =========================================================
    // TEXT
    // =========================================================

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text descriptionText;

    // =========================================================
    // ICON
    // =========================================================

    [Header("Icon")]
    [SerializeField] private Image plotIcon;

    // =========================================================
    // BUTTONS
    // =========================================================

    [Header("Buttons")]
    [SerializeField] private Button closeButton;

    // =========================================================
    // POSITION
    // =========================================================

    [Header("Plot Position")]
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
    // OPEN
    // =========================================================

    [Header("Open Animation")]
    [SerializeField] private float startingScale = 0.65f;
    [SerializeField] private float popScale = 1.08f;
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float popDuration = 0.1f;
    [SerializeField] private float settleDuration = 0.1f;

    // =========================================================
    // CLOSE
    // =========================================================

    [Header("Close Animation")]
    [SerializeField] private float closingScale = 0.65f;
    [SerializeField] private float closeDuration = 0.14f;
    [SerializeField] private bool fadeWhileClosing = true;

    // =========================================================
    // OUTSIDE
    // =========================================================

    [Header("Outside Click")]
    [SerializeField] private bool closeWhenClickingOutside = true;

    // =========================================================
    // MODE
    // =========================================================

    [Header("Mode Behaviour")]
    [SerializeField] private bool closeWhenChangingMode = true;
    [SerializeField] private bool closeWhenSelectionWheelOpens = true;

    // =========================================================
    // PRIVATE
    // =========================================================

    private Plot currentPlot;
    private InspectablePlot currentInspectablePlot;

    private Coroutine animationCoroutine;

    private bool isOpen;
    private bool isClosing;
    private bool isDragging;
    private bool manuallyPositioned;
    private bool ignoreOutsideClick;

    private Vector2 dragOffset;
    private Vector2 previousMousePosition;

    private float currentSwayAngle;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera =
                Camera.main;
        }

        if (canvas == null)
        {
            canvas =
                GetComponentInParent<Canvas>();
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

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(
                Close
            );
        }

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
            currentPlot == null ||
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
    // MODE
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

    public void Open(Plot plot)
    {
        if (plot == null ||
            panel == null)
        {
            return;
        }

        // =====================================================
        // REGISTER WITH MANAGER
        // =====================================================

        if (InspectionUIManager.Instance != null)
        {
            InspectionUIManager.Instance.OpenPanel(
                this
            );
        }

        // =====================================================
        // CLEAR OLD PLOT
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
        // CURRENT PLOT
        // =====================================================

        currentPlot =
            plot;

        currentInspectablePlot =
            plot.GetComponent<InspectablePlot>();

        if (currentInspectablePlot == null)
        {
            currentInspectablePlot =
                plot.GetComponentInChildren<InspectablePlot>();
        }

        if (currentInspectablePlot == null)
        {
            currentInspectablePlot =
                plot.GetComponentInParent<InspectablePlot>();
        }

        // =====================================================
        // OUTLINE
        // =====================================================

        if (currentInspectablePlot != null)
        {
            currentInspectablePlot.SetInspected(
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

        RefreshUI();

        SetPanelPositionImmediately();

        ignoreOutsideClick =
            true;

        StartCoroutine(
            ResetOutsideClickIgnore()
        );

        animationCoroutine =
            StartCoroutine(
                OpenAnimation()
            );
    }

    // =========================================================
    // UI CONTENT
    // =========================================================

    public void RefreshUI()
    {
        if (currentPlot == null)
        {
            return;
        }

        if (titleText != null)
        {
            titleText.text =
                "FARM PLOT";
        }

        if (currentPlot.planted)
        {
            if (statusText != null)
            {
                statusText.text =
                    "PLANTED";
            }

            if (descriptionText != null)
            {
                descriptionText.text =
                    "Something is growing in this plot.";
            }
        }
        else
        {
            if (statusText != null)
            {
                statusText.text =
                    "EMPTY";
            }

            if (descriptionText != null)
            {
                descriptionText.text =
                    "Nothing is planted in this plot.";
            }
        }
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

        ClearCurrentPlotHighlight();

        isOpen =
            false;

        isClosing =
            false;

        isDragging =
            false;

        manuallyPositioned =
            false;

        currentPlot =
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

        // =====================================================
        // CLEAR MANAGER
        // =====================================================

        if (InspectionUIManager.Instance != null)
        {
            InspectionUIManager.Instance.ClearPanel(
                this
            );
        }
    }

    // =========================================================
    // HIGHLIGHT
    // =========================================================

    private void ClearCurrentPlotHighlight()
    {
        if (currentInspectablePlot != null)
        {
            currentInspectablePlot.SetInspected(
                false
            );
        }

        currentInspectablePlot =
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
    // DRAG
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
    // POSITION
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

    private void SetPanelPositionImmediately()
    {
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

    private Vector2 CalculatePanelPosition()
    {
        if (currentPlot == null ||
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
                currentPlot.transform.position
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
    // CAMERA
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
        panel.localScale =
            Vector3.one *
            startingScale;

        panel.localRotation =
            Quaternion.identity;

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

        animationCoroutine =
            null;
    }

    // =========================================================
    // CLOSE ANIMATION
    // =========================================================

    private IEnumerator CloseAnimation()
    {
        isClosing =
            true;

        isDragging =
            false;

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

        currentPlot =
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

        // =====================================================
        // CLEAR MANAGER
        // =====================================================

        if (InspectionUIManager.Instance != null)
        {
            InspectionUIManager.Instance.ClearPanel(
                this
            );
        }
    }

    // =========================================================
    // SCALE
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

    public Plot GetCurrentPlot()
    {
        return currentPlot;
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