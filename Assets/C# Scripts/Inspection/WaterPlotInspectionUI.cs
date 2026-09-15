using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class WaterPlotInspectionUI : MonoBehaviour, IInspectionPanel
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private SelectionWheel selectionWheel;
    [SerializeField] private EndDaySystem endDaySystem;

    [Tooltip("The entire Water Plot inspection window.")]
    [SerializeField] private RectTransform panel;

    [Tooltip("The top/header area used to drag the window.")]
    [SerializeField] private RectTransform dragHandle;

    [SerializeField] private CanvasGroup canvasGroup;

    // =========================================================
    // PLAYER
    // =========================================================

    [Header("Player Action")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Animator playerAnimator;

    [Tooltip("Animator trigger used when cleaning water.")]
    [SerializeField] private string cleanAnimationTrigger = "CleanAction";

    [Tooltip("Total amount of time the cleaning action locks the player.")]
    [SerializeField] private float cleanActionDuration = 0.65f;

    [Tooltip("How long after clicking before the water actually becomes cleaner.")]
    [SerializeField] private float cleanApplyDelay = 0.4f;

    // =========================================================
    // TEXT
    // =========================================================

    [Header("Text")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text conditionText;
    [SerializeField] private TMP_Text qualityText;

    // =========================================================
    // ICON
    // =========================================================

    [Header("Icon")]
    [SerializeField] private Image waterPlotIcon;

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
    // BUTTON TEXT
    // =========================================================

    [Header("Clean Button Text")]

    [Tooltip("Optional TMP text inside the Clean Water button.")]
    [SerializeField] private TMP_Text cleanButtonText;

    [SerializeField] private string cleanButtonNormalText = "CLEAN WATER";
    [SerializeField] private string cleanButtonBusyText = "CLEANING...";
    [SerializeField] private string cleanButtonLockedText = "DAY ENDED";

    // =========================================================
    // AUDIO
    // =========================================================

    [Header("Clean Button Audio")]

    [SerializeField] private AudioSource audioSource;

    [SerializeField]
    private AudioClip[] cleanButtonHoverSounds =
        new AudioClip[3];

    [SerializeField]
    private AudioClip[] cleanButtonClickSounds =
        new AudioClip[3];

    [SerializeField]
    private AudioClip[] waterCleaningSounds =
        new AudioClip[3];

    [Range(0f, 1f)]
    [SerializeField] private float cleanButtonHoverVolume = 0.6f;

    [Range(0f, 1f)]
    [SerializeField] private float cleanButtonClickVolume = 0.8f;

    [Range(0f, 1f)]
    [SerializeField] private float waterCleaningVolume = 1f;

    [SerializeField] private float audioPitchMin = 0.95f;
    [SerializeField] private float audioPitchMax = 1.05f;

    // =========================================================
    // POSITION
    // =========================================================

    [Header("Water Plot Position")]
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
    // ANIMATION
    // =========================================================

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

    // =========================================================
    // BEHAVIOUR
    // =========================================================

    [Header("Outside Click")]
    [SerializeField] private bool closeWhenClickingOutside = true;

    [Header("Mode Behaviour")]
    [SerializeField] private bool closeWhenChangingMode = true;
    [SerializeField] private bool closeWhenSelectionWheelOpens = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // =========================================================
    // PRIVATE
    // =========================================================

    private WaterPlot currentWaterPlot;
    private InteractiveWaterPlot currentInteractiveWaterPlot;

    private Coroutine animationCoroutine;
    private Coroutine cleaningCoroutine;

    private bool isOpen;
    private bool isClosing;
    private bool isDragging;
    private bool manuallyPositioned;
    private bool ignoreOutsideClick;
    private bool isCleaning;

    private Vector2 dragOffset;
    private Vector2 previousMousePosition;

    private float currentSwayAngle;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        AutoAssignReferences();

        if (qualitySlider != null)
        {
            qualitySlider.minValue = 0f;
            qualitySlider.maxValue = 100f;
            qualitySlider.interactable = false;
        }

        SetupAudioSource();

        if (cleanButton != null)
        {
            cleanButton.onClick.AddListener(
                HandleCleanButtonClicked
            );

            SetupCleanButtonHover();
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(
                Close
            );
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = normalAlpha;
        }

        if (panel != null)
        {
            panel.gameObject.SetActive(false);
        }
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private void AutoAssignReferences()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (selectionWheel == null)
            selectionWheel =
                FindFirstObjectByType<SelectionWheel>();

        if (endDaySystem == null)
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();

        if (playerMovement == null)
            playerMovement =
                FindFirstObjectByType<PlayerMovement>();

        if (playerAnimator == null &&
            playerMovement != null)
        {
            playerAnimator =
                playerMovement.GetComponent<Animator>();
        }

        if (playerAnimator == null)
        {
            GameObject player =
                GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                playerAnimator =
                    player.GetComponent<Animator>();

                if (playerAnimator == null)
                {
                    playerAnimator =
                        player.GetComponentInChildren<Animator>();
                }
            }
        }

        if (panel == null)
            panel = transform as RectTransform;

        if (canvasGroup == null &&
            panel != null)
        {
            canvasGroup =
                panel.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup =
                    panel.gameObject
                        .AddComponent<CanvasGroup>();
            }
        }
    }

    // =========================================================
    // ACTION PHASE
    // =========================================================

    private bool IsActionPhaseActive()
    {
        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }

        if (endDaySystem == null)
            return true;

        return endDaySystem.IsActionPhaseActive();
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
        RefreshUI();

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
            currentWaterPlot == null ||
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
            return false;

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
        WaterPlot waterPlot)
    {
        if (waterPlot == null ||
            panel == null)
        {
            return;
        }

        if (InspectionUIManager.Instance != null)
        {
            InspectionUIManager.Instance.OpenPanel(this);
        }

        ClearCurrentWaterPlotHighlight();

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        currentWaterPlot = waterPlot;

        currentInteractiveWaterPlot =
            waterPlot.GetComponent<InteractiveWaterPlot>();

        if (currentInteractiveWaterPlot == null)
            currentInteractiveWaterPlot =
                waterPlot.GetComponentInChildren<InteractiveWaterPlot>();

        if (currentInteractiveWaterPlot == null)
            currentInteractiveWaterPlot =
                waterPlot.GetComponentInParent<InteractiveWaterPlot>();

        if (currentInteractiveWaterPlot != null)
            currentInteractiveWaterPlot.SetInspected(true);

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

    // =========================================================
    // REFRESH UI
    // =========================================================

    public void RefreshUI()
    {
        if (currentWaterPlot == null)
            return;

        if (titleText != null)
            titleText.text = "WATER PLOT";

        float quality =
            Mathf.Clamp(
                currentWaterPlot.GetWaterQuality(),
                0f,
                100f
            );

        if (qualityText != null)
            qualityText.text =
                Mathf.RoundToInt(quality) + "%";

        if (qualitySlider != null)
            qualitySlider.value = quality;

        if (conditionText != null)
        {
            switch (currentWaterPlot.GetWaterState())
            {
                case WaterPlot.WaterState.Clean:
                    conditionText.text = "CLEAN";
                    break;

                case WaterPlot.WaterState.Dirty:
                    conditionText.text = "DIRTY";
                    break;

                case WaterPlot.WaterState.Murky:
                    conditionText.text = "MURKY";
                    break;

                default:
                    conditionText.text = "UNKNOWN";
                    break;
            }
        }

        bool actionPhaseActive =
            IsActionPhaseActive();

        bool waterNeedsCleaning =
            !currentWaterPlot.IsClean();

        bool canClean =
            actionPhaseActive &&
            currentWaterPlot.CanClean() &&
            !isCleaning;

        if (cleanButton != null)
        {
            // Keep the button visible whenever maintenance
            // would normally be possible so the player can see
            // that it is locked after 5 PM.
            cleanButton.gameObject.SetActive(
                waterNeedsCleaning || isCleaning
            );

            cleanButton.interactable =
                canClean;
        }

        if (cleanButtonText != null)
        {
            if (isCleaning)
            {
                cleanButtonText.text =
                    cleanButtonBusyText;
            }
            else if (!actionPhaseActive &&
                     waterNeedsCleaning)
            {
                cleanButtonText.text =
                    cleanButtonLockedText;
            }
            else
            {
                cleanButtonText.text =
                    cleanButtonNormalText;
            }
        }
    }

    // =========================================================
    // CLEAN
    // =========================================================

    private void HandleCleanButtonClicked()
    {
        if (currentWaterPlot == null ||
            isCleaning)
        {
            return;
        }

        if (!IsActionPhaseActive())
        {
            RefreshUI();
            return;
        }

        if (!currentWaterPlot.CanClean())
        {
            RefreshUI();
            return;
        }

        PlayRandomSound(
            cleanButtonClickSounds,
            cleanButtonClickVolume
        );

        cleaningCoroutine =
            StartCoroutine(
                CleanWaterAction()
            );
    }

    private IEnumerator CleanWaterAction()
    {
        if (currentWaterPlot == null ||
            !IsActionPhaseActive())
        {
            yield break;
        }

        isCleaning = true;
        RefreshUI();

        if (playerMovement == null)
            playerMovement =
                FindFirstObjectByType<PlayerMovement>();

        if (playerMovement != null)
        {
            playerMovement.StartAction(
                cleanActionDuration
            );
        }

        if (playerAnimator == null &&
            playerMovement != null)
        {
            playerAnimator =
                playerMovement.GetComponent<Animator>();
        }

        if (playerAnimator != null &&
            !string.IsNullOrEmpty(
                cleanAnimationTrigger))
        {
            playerAnimator.SetTrigger(
                cleanAnimationTrigger
            );
        }

        float applyDelay =
            Mathf.Clamp(
                cleanApplyDelay,
                0f,
                Mathf.Max(
                    0.01f,
                    cleanActionDuration
                )
            );

        if (applyDelay > 0f)
        {
            yield return
                new WaitForSeconds(applyDelay);
        }

        // Recheck here as well.
        // If the day ended while the animation was playing,
        // the cleaning does NOT apply.
        if (currentWaterPlot != null &&
            IsActionPhaseActive() &&
            currentWaterPlot.CanClean())
        {
            WaterPlot.WaterState previousState =
                currentWaterPlot.GetWaterState();

            currentWaterPlot.CleanOneStage();

            WaterPlot.WaterState newState =
                currentWaterPlot.GetWaterState();

            if (previousState != newState)
            {
                PlayRandomSound(
                    waterCleaningSounds,
                    waterCleaningVolume
                );
            }
        }

        RefreshUI();

        float remaining =
            cleanActionDuration -
            applyDelay;

        if (remaining > 0f)
        {
            yield return
                new WaitForSeconds(remaining);
        }

        isCleaning = false;
        cleaningCoroutine = null;

        RefreshUI();
    }

    // =========================================================
    // AUDIO
    // =========================================================

    private void SetupAudioSource()
    {
        if (audioSource == null)
            audioSource =
                GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource =
                gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
    }

    private void SetupCleanButtonHover()
    {
        if (cleanButton == null)
            return;

        EventTrigger trigger =
            cleanButton.GetComponent<EventTrigger>();

        if (trigger == null)
            trigger =
                cleanButton.gameObject
                    .AddComponent<EventTrigger>();

        if (trigger.triggers == null)
            trigger.triggers =
                new List<EventTrigger.Entry>();

        EventTrigger.Entry entry =
            new EventTrigger.Entry();

        entry.eventID =
            EventTriggerType.PointerEnter;

        entry.callback =
            new EventTrigger.TriggerEvent();

        entry.callback.AddListener(
            (data) =>
            {
                if (cleanButton.interactable)
                {
                    PlayRandomSound(
                        cleanButtonHoverSounds,
                        cleanButtonHoverVolume
                    );
                }
            }
        );

        trigger.triggers.Add(entry);
    }

    private void PlayRandomSound(
        AudioClip[] clips,
        float volume)
    {
        if (audioSource == null)
            SetupAudioSource();

        if (audioSource == null ||
            clips == null ||
            clips.Length == 0)
        {
            return;
        }

        List<AudioClip> valid =
            new List<AudioClip>();

        foreach (AudioClip clip in clips)
        {
            if (clip != null)
                valid.Add(clip);
        }

        if (valid.Count == 0)
            return;

        float oldPitch =
            audioSource.pitch;

        audioSource.pitch =
            Random.Range(
                audioPitchMin,
                audioPitchMax
            );

        audioSource.PlayOneShot(
            valid[
                Random.Range(
                    0,
                    valid.Count
                )
            ],
            volume
        );

        audioSource.pitch =
            oldPitch;
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

        if (cleaningCoroutine != null)
        {
            StopCoroutine(cleaningCoroutine);
            cleaningCoroutine = null;
        }

        isCleaning = false;

        ClearCurrentWaterPlotHighlight();

        isOpen = false;
        isClosing = false;
        isDragging = false;
        manuallyPositioned = false;
        ignoreOutsideClick = false;

        currentWaterPlot = null;
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
            InspectionUIManager.Instance.ClearPanel(this);
    }

    private void ClearCurrentWaterPlotHighlight()
    {
        if (currentInteractiveWaterPlot != null)
            currentInteractiveWaterPlot.SetInspected(false);

        currentInteractiveWaterPlot = null;
    }

    // =========================================================
    // OUTSIDE CLICK
    // =========================================================

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
                continue;

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

        RectTransform canvasRect =
            canvas.transform as RectTransform;

        if (canvasRect == null)
            return;

        if (Input.GetMouseButtonDown(0) &&
            RectTransformUtility.RectangleContainsScreenPoint(
                dragHandle,
                Input.mousePosition,
                uiCamera))
        {
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

            isDragging = true;
            manuallyPositioned = true;

            previousMousePosition =
                Input.mousePosition;
        }

        if (isDragging &&
            Input.GetMouseButton(0))
        {
            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    Input.mousePosition,
                    uiCamera,
                    out Vector2 mousePosition
                );

            Vector2 position =
                mousePosition +
                dragOffset;

            if (clampToCanvas)
                position =
                    ClampPanelToCanvas(position);

            panel.anchoredPosition =
                position;
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
            float amount =
                1f -
                Mathf.Exp(
                    -dragScaleSpeed *
                    delta
                );

            panel.localScale =
                Vector3.Lerp(
                    panel.localScale,
                    Vector3.one * dragScale,
                    amount
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
        }

        panel.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                currentSwayAngle
            );
    }

    // =========================================================
    // POSITION
    // =========================================================

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
        if (currentWaterPlot == null ||
            mainCamera == null ||
            canvas == null)
        {
            return panel.anchoredPosition;
        }

        Vector3 screen =
            mainCamera.WorldToScreenPoint(
                currentWaterPlot.transform.position
            );

        float direction = 1f;

        if (automaticallyFlipSide &&
            screen.x >
            Screen.width -
            screenEdgePadding)
        {
            direction = -1f;
        }

        screen.x +=
            horizontalOffset *
            direction;

        screen.y +=
            verticalOffset;

        RectTransform canvasRect =
            canvas.transform as RectTransform;

        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                canvasRect,
                screen,
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
        {
            return position;
        }

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
                size.x *
                (1f - pivot.x) -
                canvasPadding
            );

        position.y =
            Mathf.Clamp(
                position.y,
                bounds.yMin +
                size.y * pivot.y +
                canvasPadding,
                bounds.yMax -
                size.y *
                (1f - pivot.y) -
                canvasPadding
            );

        return position;
    }

    private Camera GetUICamera()
    {
        if (canvas == null ||
            canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }

    // =========================================================
    // ANIMATION
    // =========================================================

    private IEnumerator OpenAnimation()
    {
        panel.localScale =
            Vector3.one *
            startingScale;

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

        animationCoroutine = null;
    }

    private IEnumerator CloseAnimation()
    {
        isClosing = true;
        isDragging = false;

        ClearCurrentWaterPlotHighlight();

        Vector3 startScale =
            panel.localScale;

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
                    timer / duration
                );

            float eased =
                t * t * t;

            panel.localScale =
                Vector3.Lerp(
                    startScale,
                    Vector3.one *
                    closingScale,
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
            Vector3.one *
            normalScale;

        panel.localRotation =
            Quaternion.identity;

        if (canvasGroup != null)
            canvasGroup.alpha =
                normalAlpha;

        currentWaterPlot = null;

        isOpen = false;
        isClosing = false;
        isDragging = false;
        manuallyPositioned = false;

        animationCoroutine = null;

        if (InspectionUIManager.Instance != null)
            InspectionUIManager.Instance.ClearPanel(this);
    }

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

        float timer = 0f;

        while (timer < duration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer / duration
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