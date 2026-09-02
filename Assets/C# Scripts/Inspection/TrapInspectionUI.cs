using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class TrapInspectionUI : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private SelectionWheel selectionWheel;

    [Tooltip("The entire Trap inspection window.")]
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
    [SerializeField] private TMP_Text mammalText;
    [SerializeField] private TMP_Text descriptionText;

    // =========================================================
    // ICON
    // =========================================================

    [Header("Icon")]
    [SerializeField] private Image trapIcon;

    // =========================================================
    // BUTTONS
    // =========================================================

    [Header("Buttons")]
    [SerializeField] private Button collectButton;
    [SerializeField] private Button closeButton;

    // =========================================================
    // POSITION
    // =========================================================

    [Header("Trap Position")]
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

    [Tooltip("How small the panel becomes while dragging.")]
    [SerializeField] private float dragScale = 0.92f;

    [Tooltip("How quickly the panel shrinks when grabbed.")]
    [SerializeField] private float dragScaleSpeed = 12f;

    [Tooltip("Maximum amount the panel can tilt while dragging.")]
    [SerializeField] private float maxDragSwayAngle = 7f;

    [Tooltip("How strongly mouse movement affects the tilt.")]
    [SerializeField] private float swayStrength = 0.3f;

    [Tooltip("How smoothly the panel responds to sway.")]
    [SerializeField] private float swaySmoothSpeed = 10f;

    [Tooltip("How quickly the panel returns to normal after release.")]
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

    private Trap currentTrap;
    private InspectableTrap currentInspectableTrap;

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
        // COLLECT BUTTON
        // =====================================================

        if (collectButton != null)
        {
            collectButton.onClick.AddListener(
                CollectMammal
            );
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
        // CLOSE IF MODE CHANGES
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
            currentTrap == null ||
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

        // Changed from Normal Mode.
        if (closeWhenChangingMode &&
            !selectionWheel.IsNormalMode())
        {
            return true;
        }

        // Tab / Selection Wheel opened.
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

    public void Open(Trap trap)
    {
        if (trap == null ||
            panel == null)
        {
            return;
        }

        // =====================================================
        // CLEAR PREVIOUS TRAP
        // =====================================================

        ClearCurrentTrapHighlight();

        // =====================================================
        // STOP OLD ANIMATION
        // =====================================================

        if (animationCoroutine != null)
        {
            StopCoroutine(
                animationCoroutine
            );

            animationCoroutine = null;
        }

        // =====================================================
        // SET CURRENT TRAP
        // =====================================================

        currentTrap = trap;

        // =====================================================
        // FIND INSPECTABLE TRAP
        // =====================================================

        currentInspectableTrap =
            trap.GetComponent<InspectableTrap>();

        if (currentInspectableTrap == null)
        {
            currentInspectableTrap =
                trap.GetComponentInChildren<InspectableTrap>();
        }

        if (currentInspectableTrap == null)
        {
            currentInspectableTrap =
                trap.GetComponentInParent<InspectableTrap>();
        }

        // =====================================================
        // INSPECTED OUTLINE
        // =====================================================

        if (currentInspectableTrap != null)
        {
            currentInspectableTrap.SetInspected(
                true
            );
        }

        // =====================================================
        // RESET STATE
        // =====================================================

        isOpen = true;
        isClosing = false;
        isDragging = false;
        manuallyPositioned = false;

        currentSwayAngle = 0f;

        // =====================================================
        // ENABLE PANEL
        // =====================================================

        panel.gameObject.SetActive(
            true
        );

        panel.localRotation =
            Quaternion.identity;

        // =====================================================
        // RESET ALPHA
        // =====================================================

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                normalAlpha;
        }

        // =====================================================
        // UPDATE CONTENT
        // =====================================================

        RefreshUI();

        // =====================================================
        // POSITION BESIDE TRAP
        // =====================================================

        SetPanelPositionImmediately();

        // =====================================================
        // DON'T CLOSE FROM OPENING CLICK
        // =====================================================

        ignoreOutsideClick = true;

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
    // REFRESH UI
    // =========================================================

    public void RefreshUI()
    {
        if (currentTrap == null)
        {
            return;
        }

        // =====================================================
        // TITLE
        // =====================================================

        if (titleText != null)
        {
            titleText.text =
                "TRAP";
        }

        // =====================================================
        // EMPTY
        // =====================================================

        if (currentTrap.IsEmpty())
        {
            if (statusText != null)
            {
                statusText.text =
                    "EMPTY";
            }

            if (mammalText != null)
            {
                mammalText.text =
                    "";
            }

            if (descriptionText != null)
            {
                descriptionText.text =
                    "The trap is empty.";
            }

            if (collectButton != null)
            {
                collectButton.gameObject.SetActive(
                    false
                );
            }

            return;
        }

        // =====================================================
        // SET
        // =====================================================

        if (currentTrap.IsSet())
        {
            if (statusText != null)
            {
                statusText.text =
                    "SET";
            }

            if (mammalText != null)
            {
                mammalText.text =
                    "";
            }

            if (descriptionText != null)
            {
                descriptionText.text =
                    "Nothing has been caught yet.";
            }

            if (collectButton != null)
            {
                collectButton.gameObject.SetActive(
                    false
                );
            }

            return;
        }

        // =====================================================
        // CAUGHT
        // =====================================================

        if (currentTrap.IsCaught())
        {
            if (statusText != null)
            {
                statusText.text =
                    "CAUGHT";
            }

            if (mammalText != null)
            {
                string mammal =
                    currentTrap.GetCaughtMammalName();

                if (string.IsNullOrWhiteSpace(
                    mammal))
                {
                    mammal =
                        "MAMMAL";
                }

                mammalText.text =
                    mammal.ToUpper();
            }

            if (descriptionText != null)
            {
                descriptionText.text =
                    "A mammal has been caught.";
            }

            if (collectButton != null)
            {
                collectButton.gameObject.SetActive(
                    true
                );

                collectButton.interactable =
                    true;
            }
        }
    }

    // =========================================================
    // COLLECT MAMMAL
    // =========================================================

    private void CollectMammal()
    {
        if (currentTrap == null)
        {
            return;
        }

        if (!currentTrap.IsCaught())
        {
            return;
        }

        // =====================================================
        // FOR NOW:
        //
        // We do NOT store the mammal anywhere.
        // It is simply removed from the trap.
        // =====================================================

        currentTrap.CollectCaughtMammal();

        // =====================================================
        // UPDATE UI
        // =====================================================

        RefreshUI();
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

            animationCoroutine = null;
        }

        // Remove inspected outline.
        ClearCurrentTrapHighlight();

        isOpen = false;
        isClosing = false;
        isDragging = false;
        manuallyPositioned = false;

        currentTrap = null;

        currentSwayAngle = 0f;

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
    // CLEAR INSPECTED OUTLINE
    // =========================================================

    private void ClearCurrentTrapHighlight()
    {
        if (currentInspectableTrap != null)
        {
            currentInspectableTrap.SetInspected(
                false
            );
        }

        currentInspectableTrap = null;
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

        // Clicking anything inside the panel
        // should NOT close it.
        if (IsPointerOverInspectionUI())
        {
            return;
        }

        Close();
    }

    // =========================================================
    // IS POINTER OVER PANEL?
    // =========================================================

    private bool IsPointerOverInspectionUI()
    {
        if (panel == null)
        {
            return false;
        }

        // =====================================================
        // RECTANGLE CHECK
        // =====================================================

        if (RectTransformUtility.RectangleContainsScreenPoint(
            panel,
            Input.mousePosition,
            GetUICamera()))
        {
            return true;
        }

        // =====================================================
        // EVENT SYSTEM CHECK
        // =====================================================

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
                RectTransformUtility
                    .RectangleContainsScreenPoint(
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

                RectTransformUtility
                    .ScreenPointToLocalPointInRectangle(
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

            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
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
                    Vector3.one *
                    dragScale,
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
        // AFTER RELEASE
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
            // SCALE BACK
            // -------------------------------------------------

            panel.localScale =
                Vector3.Lerp(
                    panel.localScale,
                    Vector3.one *
                    normalScale,
                    returnAmount
                );

            // -------------------------------------------------
            // ROTATION BACK
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
    // UPDATE PANEL POSITION
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
        if (currentTrap == null ||
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
                currentTrap.transform.position
            );

        // =====================================================
        // WHICH SIDE OF TRAP?
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
            canvas.transform
                as RectTransform;

        if (canvasRect == null)
        {
            return panel.anchoredPosition;
        }

        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
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
    // CLAMP PANEL TO CANVAS
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

        panel.localScale =
            Vector3.one *
            startingScale;

        panel.localRotation =
            Quaternion.identity;

        // -----------------------------------------------------
        // SMALL -> POP
        // -----------------------------------------------------

        yield return ScalePanel(
            startingScale,
            popScale,
            popDuration,
            true
        );

        // -----------------------------------------------------
        // POP -> NORMAL
        // -----------------------------------------------------

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

        // Remove inspected outline.
        ClearCurrentTrapHighlight();

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
            // SHRINK
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

        currentTrap =
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
    // OPENING CLICK PROTECTION
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

    public Trap GetCurrentTrap()
    {
        return currentTrap;
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