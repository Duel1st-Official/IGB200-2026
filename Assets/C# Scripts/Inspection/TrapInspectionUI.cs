using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class TrapInspectionUI : MonoBehaviour, IInspectionPanel
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private SelectionWheel selectionWheel;
    [SerializeField] private RectTransform panel;
    [SerializeField] private RectTransform dragHandle;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text mammalText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Icon")]
    [SerializeField] private Image trapIcon;

    [Header("Buttons")]
    [SerializeField] private Button collectButton;
    [SerializeField] private Button closeButton;

    [Header("Trap Position")]
    [SerializeField] private float horizontalOffset = 230f;
    [SerializeField] private float verticalOffset = 30f;
    [SerializeField] private float followSpeed = 15f;
    [SerializeField] private bool automaticallyFlipSide = true;
    [SerializeField] private float screenEdgePadding = 180f;

    [Header("Dragging")]
    [SerializeField] private bool allowDragging = true;
    [SerializeField] private bool stopFollowingAfterDrag = true;
    [SerializeField] private bool clampToCanvas = true;
    [SerializeField] private float canvasPadding = 10f;

    [Header("Drag Visuals")]
    [SerializeField] private float dragScale = 0.92f;
    [SerializeField] private float dragScaleSpeed = 12f;
    [SerializeField] private float maxDragSwayAngle = 7f;
    [SerializeField] private float swayStrength = 0.3f;
    [SerializeField] private float swaySmoothSpeed = 10f;
    [SerializeField] private float dropReturnSpeed = 10f;

    [Header("Transparency")]

    [Range(0f, 1f)]
    [SerializeField] private float normalAlpha = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float dragAlpha = 0.7f;

    [SerializeField] private float alphaSmoothSpeed = 10f;

    [Header("Open Animation")]
    [SerializeField] private float startingScale = 0.65f;
    [SerializeField] private float popScale = 1.08f;
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float popDuration = 0.1f;
    [SerializeField] private float settleDuration = 0.1f;

    [Header("Close Animation")]
    [SerializeField] private float closingScale = 0.65f;
    [SerializeField] private float closeDuration = 0.14f;
    [SerializeField] private bool fadeWhileClosing = true;

    [Header("Outside Click")]
    [SerializeField] private bool closeWhenClickingOutside = true;

    [Header("Mode Behaviour")]
    [SerializeField] private bool closeWhenChangingMode = true;
    [SerializeField] private bool closeWhenSelectionWheelOpens = true;

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

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (selectionWheel == null)
            selectionWheel = FindFirstObjectByType<SelectionWheel>();

        if (panel == null)
            panel = transform as RectTransform;

        if (canvasGroup == null && panel != null)
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
            canvasGroup.alpha = normalAlpha;

        if (collectButton != null)
            collectButton.onClick.AddListener(CollectMammal);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (panel != null)
            panel.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isOpen ||
            isClosing ||
            panel == null)
            return;

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

    private void LateUpdate()
    {
        if (!isOpen ||
            isClosing ||
            currentTrap == null ||
            panel == null)
            return;

        if (manuallyPositioned &&
            stopFollowingAfterDrag)
            return;

        UpdatePanelPosition();
    }

    private bool ShouldCloseBecauseOfMode()
    {
        if (selectionWheel == null)
            return false;

        if (closeWhenChangingMode &&
            !selectionWheel.IsNormalMode())
            return true;

        if (closeWhenSelectionWheelOpens &&
            selectionWheel.IsWheelOpen())
            return true;

        return false;
    }

    public void Open(Trap trap)
    {
        if (trap == null ||
            panel == null)
            return;

        // =====================================================
        // SINGLE INSPECTION PANEL
        // =====================================================

        if (InspectionUIManager.Instance != null)
        {
            InspectionUIManager.Instance.OpenPanel(
                this
            );
        }

        ClearCurrentTrapHighlight();

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        currentTrap = trap;

        currentInspectableTrap =
            trap.GetComponent<InspectableTrap>();

        if (currentInspectableTrap == null)
            currentInspectableTrap =
                trap.GetComponentInChildren<InspectableTrap>();

        if (currentInspectableTrap == null)
            currentInspectableTrap =
                trap.GetComponentInParent<InspectableTrap>();

        if (currentInspectableTrap != null)
            currentInspectableTrap.SetInspected(true);

        isOpen = true;
        isClosing = false;
        isDragging = false;
        manuallyPositioned = false;

        currentSwayAngle = 0f;

        panel.gameObject.SetActive(true);
        panel.localRotation = Quaternion.identity;

        if (canvasGroup != null)
            canvasGroup.alpha = normalAlpha;

        RefreshUI();

        SetPanelPositionImmediately();

        ignoreOutsideClick = true;

        StartCoroutine(
            ResetOutsideClickIgnore()
        );

        animationCoroutine =
            StartCoroutine(
                OpenAnimation()
            );
    }

    public void RefreshUI()
    {
        if (currentTrap == null)
            return;

        if (titleText != null)
            titleText.text = "TRAP";

        if (currentTrap.IsEmpty())
        {
            if (statusText != null)
                statusText.text = "EMPTY";

            if (mammalText != null)
                mammalText.text = "";

            if (descriptionText != null)
                descriptionText.text =
                    "The trap is empty.";

            if (collectButton != null)
                collectButton.gameObject.SetActive(false);

            return;
        }

        if (currentTrap.IsSet())
        {
            if (statusText != null)
                statusText.text = "SET";

            if (mammalText != null)
                mammalText.text = "";

            if (descriptionText != null)
                descriptionText.text =
                    "Nothing has been caught yet.";

            if (collectButton != null)
                collectButton.gameObject.SetActive(false);

            return;
        }

        if (currentTrap.IsCaught())
        {
            if (statusText != null)
                statusText.text = "CAUGHT";

            if (mammalText != null)
            {
                string mammal =
                    currentTrap.GetCaughtMammalName();

                if (string.IsNullOrWhiteSpace(mammal))
                    mammal = "MAMMAL";

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
                collectButton.gameObject.SetActive(true);
                collectButton.interactable = true;
            }
        }
    }

    private void CollectMammal()
    {
        if (currentTrap == null)
            return;

        if (!currentTrap.IsCaught())
            return;

        currentTrap.CollectCaughtMammal();

        RefreshUI();
    }

    public void Close()
    {
        if (!isOpen ||
            isClosing)
            return;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine =
            StartCoroutine(
                CloseAnimation()
            );
    }

    public void CloseImmediately()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

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
                Vector3.one * normalScale;

            panel.localRotation =
                Quaternion.identity;

            panel.gameObject.SetActive(false);
        }

        if (canvasGroup != null)
            canvasGroup.alpha = normalAlpha;

        if (InspectionUIManager.Instance != null)
        {
            InspectionUIManager.Instance.ClearPanel(
                this
            );
        }
    }

    private void ClearCurrentTrapHighlight()
    {
        if (currentInspectableTrap != null)
            currentInspectableTrap.SetInspected(false);

        currentInspectableTrap = null;
    }

    private void HandleOutsideClick()
    {
        if (ignoreOutsideClick)
            return;

        if (IsPointerOverInspectionUI())
            return;

        Close();
    }

    private bool IsPointerOverInspectionUI()
    {
        if (panel == null)
            return false;

        if (RectTransformUtility.RectangleContainsScreenPoint(
            panel,
            Input.mousePosition,
            GetUICamera()))
        {
            return true;
        }

        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData =
            new PointerEventData(EventSystem.current);

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
                continue;

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

    private void HandleDragging()
    {
        if (!allowDragging ||
            dragHandle == null ||
            canvas == null)
            return;

        Camera uiCamera =
            GetUICamera();

        if (Input.GetMouseButtonDown(0))
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(
                dragHandle,
                Input.mousePosition,
                uiCamera))
            {
                RectTransform canvasRect =
                    canvas.transform as RectTransform;

                if (canvasRect == null)
                    return;

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    Input.mousePosition,
                    uiCamera,
                    out Vector2 mousePosition
                );

                dragOffset =
                    panel.anchoredPosition -
                    mousePosition;

                isDragging = true;
                manuallyPositioned = true;

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
                return;

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
                newPosition =
                    ClampPanelToCanvas(newPosition);

            panel.anchoredPosition =
                newPosition;
        }

        if (isDragging &&
            Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    private void UpdateDragVisuals()
    {
        if (panel == null)
            return;

        float delta =
            Time.unscaledDeltaTime;

        float targetAlpha =
            isDragging
                ? dragAlpha
                : normalAlpha;

        if (canvasGroup != null)
        {
            float amount =
                1f -
                Mathf.Exp(
                    -alphaSmoothSpeed *
                    delta
                );

            canvasGroup.alpha =
                Mathf.Lerp(
                    canvasGroup.alpha,
                    targetAlpha,
                    amount
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
                    Vector3.one * dragScale,
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
                Mathf.Clamp(
                    -mouseDelta.x *
                    swayStrength,
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
            float amount =
                1f -
                Mathf.Exp(
                    -dropReturnSpeed *
                    delta
                );

            panel.localScale =
                Vector3.Lerp(
                    panel.localScale,
                    Vector3.one * normalScale,
                    amount
                );

            currentSwayAngle =
                Mathf.Lerp(
                    currentSwayAngle,
                    0f,
                    amount
                );

            panel.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    currentSwayAngle
                );
        }
    }

    private void UpdatePanelPosition()
    {
        Vector2 target =
            CalculatePanelPosition();

        float amount =
            1f -
            Mathf.Exp(
                -followSpeed *
                Time.unscaledDeltaTime
            );

        panel.anchoredPosition =
            Vector2.Lerp(
                panel.anchoredPosition,
                target,
                amount
            );
    }

    private void SetPanelPositionImmediately()
    {
        panel.anchoredPosition =
            ClampPanelToCanvas(
                CalculatePanelPosition()
            );
    }

    private Vector2 CalculatePanelPosition()
    {
        if (currentTrap == null ||
            mainCamera == null ||
            canvas == null)
        {
            return panel.anchoredPosition;
        }

        Vector3 screenPosition =
            mainCamera.WorldToScreenPoint(
                currentTrap.transform.position
            );

        float direction = 1f;

        if (automaticallyFlipSide &&
            screenPosition.x >
            Screen.width -
            screenEdgePadding)
        {
            direction = -1f;
        }

        screenPosition.x +=
            horizontalOffset *
            direction;

        screenPosition.y +=
            verticalOffset;

        RectTransform canvasRect =
            canvas.transform as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            GetUICamera(),
            out Vector2 result
        );

        return clampToCanvas
            ? ClampPanelToCanvas(result)
            : result;
    }

    private Vector2 ClampPanelToCanvas(
        Vector2 position)
    {
        if (canvas == null ||
            panel == null)
            return position;

        RectTransform canvasRect =
            canvas.transform as RectTransform;

        if (canvasRect == null)
            return position;

        Rect bounds =
            canvasRect.rect;

        Vector2 size =
            panel.rect.size;

        Vector2 pivot =
            panel.pivot;

        position.x =
            Mathf.Clamp(
                position.x,
                bounds.xMin +
                size.x * pivot.x +
                canvasPadding,
                bounds.xMax -
                size.x * (1f - pivot.x) -
                canvasPadding
            );

        position.y =
            Mathf.Clamp(
                position.y,
                bounds.yMin +
                size.y * pivot.y +
                canvasPadding,
                bounds.yMax -
                size.y * (1f - pivot.y) -
                canvasPadding
            );

        return position;
    }

    private Camera GetUICamera()
    {
        if (canvas == null)
            return null;

        if (canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private IEnumerator OpenAnimation()
    {
        panel.localScale =
            Vector3.one * startingScale;

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
            Vector3.one * normalScale;

        animationCoroutine = null;
    }

    private IEnumerator CloseAnimation()
    {
        isClosing = true;
        isDragging = false;

        ClearCurrentTrapHighlight();

        Vector3 startScale =
            panel.localScale;

        Quaternion startRotation =
            panel.localRotation;

        float startAlpha =
            canvasGroup != null
                ? canvasGroup.alpha
                : normalAlpha;

        float timer = 0f;

        float duration =
            Mathf.Max(
                0.01f,
                closeDuration
            );

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
                t * t * t;

            panel.localScale =
                Vector3.Lerp(
                    startScale,
                    Vector3.one * closingScale,
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

        panel.gameObject.SetActive(false);

        panel.localScale =
            Vector3.one * normalScale;

        panel.localRotation =
            Quaternion.identity;

        if (canvasGroup != null)
            canvasGroup.alpha = normalAlpha;

        currentTrap = null;

        currentSwayAngle = 0f;

        isOpen = false;
        isClosing = false;
        isDragging = false;
        manuallyPositioned = false;

        animationCoroutine = null;

        if (InspectionUIManager.Instance != null)
        {
            InspectionUIManager.Instance.ClearPanel(
                this
            );
        }
    }

    private IEnumerator ScalePanel(
        float from,
        float to,
        float duration,
        bool backEase)
    {
        duration =
            Mathf.Max(
                duration,
                0.01f
            );

        float timer = 0f;

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
                    : 1f -
                      Mathf.Pow(
                          1f - t,
                          3f
                      );

            panel.localScale =
                Vector3.one *
                Mathf.LerpUnclamped(
                    from,
                    to,
                    eased
                );

            yield return null;
        }

        panel.localScale =
            Vector3.one * to;
    }

    private IEnumerator ResetOutsideClickIgnore()
    {
        yield return
            new WaitForEndOfFrame();

        ignoreOutsideClick = false;
    }

    private float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;

        return
            1f +
            c3 *
            Mathf.Pow(x - 1f, 3f) +
            c1 *
            Mathf.Pow(x - 1f, 2f);
    }
}