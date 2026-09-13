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

    // =========================================================
    // CLEAN BUTTON AUDIO
    // =========================================================

    [Header("Clean Button Audio")]

    [Tooltip("AudioSource used for Water Plot UI sounds. If empty, one is automatically found or created.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Random sound played when hovering over Clean Water.")]
    [SerializeField]
    private AudioClip[] cleanButtonHoverSounds =
        new AudioClip[3];

    [Tooltip("Random sound played when Clean Water is clicked.")]
    [SerializeField]
    private AudioClip[] cleanButtonClickSounds =
        new AudioClip[3];

    [Tooltip("Actual cleaning/splash sound played when the water changes stage.")]
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

    private EventTrigger cleanButtonEventTrigger;
    private EventTrigger.Entry cleanHoverEntry;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // -----------------------------------------------------
        // CAMERA
        // -----------------------------------------------------

        if (mainCamera == null)
        {
            mainCamera =
                Camera.main;
        }

        // -----------------------------------------------------
        // CANVAS
        // -----------------------------------------------------

        if (canvas == null)
        {
            canvas =
                GetComponentInParent<Canvas>();
        }

        // -----------------------------------------------------
        // SELECTION WHEEL
        // -----------------------------------------------------

        if (selectionWheel == null)
        {
            selectionWheel =
                FindFirstObjectByType<SelectionWheel>();
        }

        // -----------------------------------------------------
        // PLAYER MOVEMENT
        // -----------------------------------------------------

        if (playerMovement == null)
        {
            playerMovement =
                FindFirstObjectByType<PlayerMovement>();
        }

        // -----------------------------------------------------
        // PLAYER ANIMATOR
        // -----------------------------------------------------

        if (playerAnimator == null &&
            playerMovement != null)
        {
            playerAnimator =
                playerMovement.GetComponent<Animator>();
        }

        if (playerAnimator == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag(
                    "Player"
                );

            if (playerObject != null)
            {
                playerAnimator =
                    playerObject.GetComponent<Animator>();

                if (playerAnimator == null)
                {
                    playerAnimator =
                        playerObject.GetComponentInChildren<Animator>();
                }
            }
        }

        // -----------------------------------------------------
        // PANEL
        // -----------------------------------------------------

        if (panel == null)
        {
            panel =
                transform as RectTransform;
        }

        // -----------------------------------------------------
        // CANVAS GROUP
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // QUALITY SLIDER
        // -----------------------------------------------------

        if (qualitySlider != null)
        {
            qualitySlider.minValue =
                0f;

            qualitySlider.maxValue =
                100f;

            qualitySlider.interactable =
                false;
        }

        // -----------------------------------------------------
        // AUDIO
        // -----------------------------------------------------

        SetupAudioSource();

        // -----------------------------------------------------
        // CLEAN BUTTON
        // -----------------------------------------------------

        if (cleanButton != null)
        {
            cleanButton.onClick.AddListener(
                HandleCleanButtonClicked
            );

            SetupCleanButtonHover();
        }

        // -----------------------------------------------------
        // CLOSE BUTTON
        // -----------------------------------------------------

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(
                Close
            );
        }

        // -----------------------------------------------------
        // START CLOSED
        // -----------------------------------------------------

        if (panel != null)
        {
            panel.gameObject.SetActive(
                false
            );
        }
    }

    // =========================================================
    // AUDIO SETUP
    // =========================================================

    private void SetupAudioSource()
    {
        if (audioSource == null)
        {
            audioSource =
                GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource =
                gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake =
            false;

        audioSource.loop =
            false;

        audioSource.spatialBlend =
            0f;
    }

    // =========================================================
    // CLEAN BUTTON HOVER
    // =========================================================

    private void SetupCleanButtonHover()
    {
        if (cleanButton == null)
        {
            return;
        }

        cleanButtonEventTrigger =
            cleanButton.GetComponent<EventTrigger>();

        if (cleanButtonEventTrigger == null)
        {
            cleanButtonEventTrigger =
                cleanButton.gameObject.AddComponent<EventTrigger>();
        }

        if (cleanButtonEventTrigger.triggers == null)
        {
            cleanButtonEventTrigger.triggers =
                new List<EventTrigger.Entry>();
        }

        cleanHoverEntry =
            new EventTrigger.Entry();

        cleanHoverEntry.eventID =
            EventTriggerType.PointerEnter;

        cleanHoverEntry.callback =
            new EventTrigger.TriggerEvent();

        cleanHoverEntry.callback.AddListener(
            (data) =>
            {
                PlayCleanButtonHoverSound();
            }
        );

        cleanButtonEventTrigger.triggers.Add(
            cleanHoverEntry
        );
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

    // =========================================================
    // LATE UPDATE
    // =========================================================

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
        WaterPlot waterPlot)
    {
        if (waterPlot == null ||
            panel == null)
        {
            return;
        }

        // -----------------------------------------------------
        // INSPECTION MANAGER
        // -----------------------------------------------------

        if (InspectionUIManager.Instance != null)
        {
            InspectionUIManager.Instance.OpenPanel(
                this
            );
        }

        // -----------------------------------------------------
        // OLD WATER PLOT
        // -----------------------------------------------------

        ClearCurrentWaterPlotHighlight();

        // -----------------------------------------------------
        // OLD ANIMATION
        // -----------------------------------------------------

        if (animationCoroutine != null)
        {
            StopCoroutine(
                animationCoroutine
            );

            animationCoroutine =
                null;
        }

        // -----------------------------------------------------
        // CURRENT WATER PLOT
        // -----------------------------------------------------

        currentWaterPlot =
            waterPlot;

        // -----------------------------------------------------
        // FIND INTERACTIVE COMPONENT
        // -----------------------------------------------------

        currentInteractiveWaterPlot =
            waterPlot.GetComponent<InteractiveWaterPlot>();

        if (currentInteractiveWaterPlot == null)
        {
            currentInteractiveWaterPlot =
                waterPlot.GetComponentInChildren<InteractiveWaterPlot>();
        }

        if (currentInteractiveWaterPlot == null)
        {
            currentInteractiveWaterPlot =
                waterPlot.GetComponentInParent<InteractiveWaterPlot>();
        }

        // -----------------------------------------------------
        // INSPECTED OUTLINE
        // -----------------------------------------------------

        if (currentInteractiveWaterPlot != null)
        {
            currentInteractiveWaterPlot.SetInspected(
                true
            );
        }

        // -----------------------------------------------------
        // STATE
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // SHOW PANEL
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // IGNORE ORIGINAL WORLD CLICK
        // -----------------------------------------------------

        ignoreOutsideClick =
            true;

        StartCoroutine(
            ResetOutsideClickIgnore()
        );

        // -----------------------------------------------------
        // OPEN ANIMATION
        // -----------------------------------------------------

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
        {
            return;
        }

        // -----------------------------------------------------
        // TITLE
        // -----------------------------------------------------

        if (titleText != null)
        {
            titleText.text =
                "WATER PLOT";
        }

        // -----------------------------------------------------
        // QUALITY
        // -----------------------------------------------------

        float quality =
            Mathf.Clamp(
                currentWaterPlot.GetWaterQuality(),
                0f,
                100f
            );

        if (qualityText != null)
        {
            qualityText.text =
                Mathf.RoundToInt(
                    quality
                ) +
                "%";
        }

        if (qualitySlider != null)
        {
            qualitySlider.value =
                quality;
        }

        // -----------------------------------------------------
        // CONDITION
        // -----------------------------------------------------

        if (conditionText != null)
        {
            switch (
                currentWaterPlot.GetWaterState())
            {
                case WaterPlot.WaterState.Clean:

                    conditionText.text =
                        "CLEAN";

                    break;

                case WaterPlot.WaterState.Dirty:

                    conditionText.text =
                        "DIRTY";

                    break;

                case WaterPlot.WaterState.Murky:

                    conditionText.text =
                        "MURKY";

                    break;

                default:

                    conditionText.text =
                        "UNKNOWN";

                    break;
            }
        }

        // -----------------------------------------------------
        // CLEAN BUTTON
        // -----------------------------------------------------

        if (cleanButton != null)
        {
            bool canClean =
                currentWaterPlot.CanClean();

            // While cleaning we KEEP it visible,
            // but stop the player pressing it again.
            if (isCleaning)
            {
                cleanButton.gameObject.SetActive(
                    true
                );

                cleanButton.interactable =
                    false;
            }
            else
            {
                cleanButton.gameObject.SetActive(
                    canClean
                );

                cleanButton.interactable =
                    canClean;
            }
        }

        // -----------------------------------------------------
        // CLEAN BUTTON TEXT
        // -----------------------------------------------------

        if (cleanButtonText != null)
        {
            cleanButtonText.text =
                isCleaning
                    ? cleanButtonBusyText
                    : cleanButtonNormalText;
        }
    }

    // =========================================================
    // CLEAN BUTTON CLICK
    // =========================================================

    private void HandleCleanButtonClicked()
    {
        if (currentWaterPlot == null)
        {
            return;
        }

        if (isCleaning)
        {
            return;
        }

        if (!currentWaterPlot.CanClean())
        {
            RefreshUI();
            return;
        }

        // -----------------------------------------------------
        // CLICK SOUND
        // -----------------------------------------------------

        PlayCleanButtonClickSound();

        // -----------------------------------------------------
        // START CLEAN ACTION
        // -----------------------------------------------------

        cleaningCoroutine =
            StartCoroutine(
                CleanWaterAction()
            );
    }

    // =========================================================
    // CLEAN ACTION
    // =========================================================

    private IEnumerator CleanWaterAction()
    {
        if (currentWaterPlot == null)
        {
            yield break;
        }

        isCleaning =
            true;

        RefreshUI();

        // -----------------------------------------------------
        // LOCK PLAYER
        // -----------------------------------------------------

        if (playerMovement == null)
        {
            playerMovement =
                FindFirstObjectByType<PlayerMovement>();
        }

        if (playerMovement != null)
        {
            playerMovement.StartAction(
                cleanActionDuration
            );
        }

        // -----------------------------------------------------
        // ANIMATION
        // -----------------------------------------------------

        if (playerAnimator == null &&
            playerMovement != null)
        {
            playerAnimator =
                playerMovement.GetComponent<Animator>();
        }

        if (playerAnimator != null &&
            !string.IsNullOrEmpty(
                cleanAnimationTrigger
            ))
        {
            playerAnimator.SetTrigger(
                cleanAnimationTrigger
            );
        }

        if (showDebugLogs)
        {
            Debug.Log(
                "Started cleaning Water Plot."
            );
        }

        // -----------------------------------------------------
        // WAIT UNTIL ACTION IMPACT POINT
        // -----------------------------------------------------

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
                new WaitForSeconds(
                    applyDelay
                );
        }

        // -----------------------------------------------------
        // CLEAN ONE STAGE
        // -----------------------------------------------------

        if (currentWaterPlot != null &&
            currentWaterPlot.CanClean())
        {
            WaterPlot.WaterState previousState =
                currentWaterPlot.GetWaterState();

            currentWaterPlot.CleanOneStage();

            WaterPlot.WaterState newState =
                currentWaterPlot.GetWaterState();

            if (previousState != newState)
            {
                PlayWaterCleaningSound();

                if (showDebugLogs)
                {
                    Debug.Log(
                        "Water cleaned: " +
                        previousState +
                        " -> " +
                        newState
                    );
                }
            }
        }

        RefreshUI();

        // -----------------------------------------------------
        // WAIT FOR REST OF ACTION
        // -----------------------------------------------------

        float remainingTime =
            cleanActionDuration -
            applyDelay;

        if (remainingTime > 0f)
        {
            yield return
                new WaitForSeconds(
                    remainingTime
                );
        }

        // -----------------------------------------------------
        // FINISH
        // -----------------------------------------------------

        isCleaning =
            false;

        cleaningCoroutine =
            null;

        RefreshUI();
    }

    // =========================================================
    // HOVER SOUND
    // =========================================================

    private void PlayCleanButtonHoverSound()
    {
        if (cleanButton == null ||
            isCleaning ||
            !cleanButton.gameObject.activeInHierarchy ||
            !cleanButton.interactable)
        {
            return;
        }

        PlayRandomSound(
            cleanButtonHoverSounds,
            cleanButtonHoverVolume
        );
    }

    // =========================================================
    // CLICK SOUND
    // =========================================================

    private void PlayCleanButtonClickSound()
    {
        PlayRandomSound(
            cleanButtonClickSounds,
            cleanButtonClickVolume
        );
    }

    // =========================================================
    // CLEANING SOUND
    // =========================================================

    private void PlayWaterCleaningSound()
    {
        PlayRandomSound(
            waterCleaningSounds,
            waterCleaningVolume
        );
    }

    // =========================================================
    // RANDOM SOUND
    // =========================================================

    private void PlayRandomSound(
        AudioClip[] clips,
        float volume)
    {
        if (audioSource == null)
        {
            SetupAudioSource();
        }

        if (audioSource == null ||
            clips == null ||
            clips.Length == 0)
        {
            return;
        }

        int validCount =
            0;

        for (int i = 0;
             i < clips.Length;
             i++)
        {
            if (clips[i] != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return;
        }

        int targetIndex =
            Random.Range(
                0,
                validCount
            );

        int validIndex =
            0;

        AudioClip chosenClip =
            null;

        for (int i = 0;
             i < clips.Length;
             i++)
        {
            if (clips[i] == null)
            {
                continue;
            }

            if (validIndex ==
                targetIndex)
            {
                chosenClip =
                    clips[i];

                break;
            }

            validIndex++;
        }

        if (chosenClip == null)
        {
            return;
        }

        float originalPitch =
            audioSource.pitch;

        audioSource.pitch =
            Random.Range(
                audioPitchMin,
                audioPitchMax
            );

        audioSource.PlayOneShot(
            chosenClip,
            volume
        );

        audioSource.pitch =
            originalPitch;
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

        // Don't let closing cancel the water cleaning
        // halfway through the actual action.
        if (isCleaning)
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
        // If another inspection window is opened during
        // cleaning, finish/cancel this HUD safely.
        if (cleaningCoroutine != null)
        {
            StopCoroutine(
                cleaningCoroutine
            );

            cleaningCoroutine =
                null;
        }

        isCleaning =
            false;

        if (animationCoroutine != null)
        {
            StopCoroutine(
                animationCoroutine
            );

            animationCoroutine =
                null;
        }

        ClearCurrentWaterPlotHighlight();

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

        if (InspectionUIManager.Instance != null)
        {
            InspectionUIManager.Instance.ClearPanel(
                this
            );
        }
    }

    // =========================================================
    // CLEAR HIGHLIGHT
    // =========================================================

    private void ClearCurrentWaterPlotHighlight()
    {
        if (currentInteractiveWaterPlot != null)
        {
            currentInteractiveWaterPlot.SetInspected(
                false
            );
        }

        currentInteractiveWaterPlot =
            null;
    }

    // =========================================================
    // OUTSIDE CLICK
    // =========================================================

    private void HandleOutsideClick()
    {
        if (ignoreOutsideClick ||
            isCleaning)
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
    // POINTER OVER UI
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
            isCleaning ||
            dragHandle == null ||
            canvas == null)
        {
            return;
        }

        Camera uiCamera =
            GetUICamera();

        // -----------------------------------------------------
        // START DRAG
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // DRAG
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // RELEASE
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // DRAGGING
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // RETURN
        // -----------------------------------------------------

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
    // FOLLOW WATER PLOT
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

        Vector3 screenPosition =
            mainCamera.WorldToScreenPoint(
                currentWaterPlot.transform.position
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
    // CLAMP
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
        if (panel == null)
        {
            yield break;
        }

        isClosing =
            true;

        isDragging =
            false;

        ClearCurrentWaterPlotHighlight();

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
    // RESET OUTSIDE CLICK
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

    public bool IsCleaning()
    {
        return isCleaning;
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
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        if (cleanButton != null)
        {
            cleanButton.onClick.RemoveListener(
                HandleCleanButtonClicked
            );
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(
                Close
            );
        }

        if (cleanButtonEventTrigger != null &&
            cleanHoverEntry != null &&
            cleanButtonEventTrigger.triggers != null)
        {
            cleanButtonEventTrigger.triggers.Remove(
                cleanHoverEntry
            );
        }
    }
}