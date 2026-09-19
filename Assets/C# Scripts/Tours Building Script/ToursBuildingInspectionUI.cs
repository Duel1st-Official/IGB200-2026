using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ToursBuildingInspectionUI :
    MonoBehaviour,
    IInspectionPanel
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    [SerializeField]
    private Camera mainCamera;

    [SerializeField]
    private Canvas canvas;

    [SerializeField]
    private SelectionWheel selectionWheel;

    [SerializeField]
    private EndDaySystem endDaySystem;

    [Tooltip("The entire Tours inspection window.")]
    [SerializeField]
    private RectTransform panel;

    [Tooltip("The top/header area used to drag the window.")]
    [SerializeField]
    private RectTransform dragHandle;

    [SerializeField]
    private CanvasGroup canvasGroup;

    // =========================================================
    // TEXT
    // =========================================================

    [Header("Text")]

    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text toursCompletedText;

    // =========================================================
    // OPTIONAL TOUR INFORMATION
    // =========================================================

    [Header("Optional Tour Information")]

    [SerializeField]
    private TMP_Text tourQualityText;

    [SerializeField]
    private TMP_Text potentialRewardText;

    [SerializeField]
    private TMP_Text tourTimingText;

    // =========================================================
    // ICON
    // =========================================================

    [Header("Icon")]

    [SerializeField]
    private Image toursBuildingIcon;

    // =========================================================
    // REPUTATION
    // =========================================================

    [Header("Reputation")]

    [SerializeField]
    private Slider reputationSlider;

    // =========================================================
    // TOUR
    // =========================================================

    [Header("Tour")]

    [SerializeField]
    private Button startTourButton;

    // =========================================================
    // BUTTONS
    // =========================================================

    [Header("Buttons")]

    [SerializeField]
    private Button closeButton;

    [Header("UI Sounds")]
    [Tooltip("Optional dedicated UI AudioSource on an object that stays active when this panel closes. Created automatically if empty.")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip[] buttonHoverSounds = new AudioClip[3];
    [SerializeField] private AudioClip[] buttonClickSounds = new AudioClip[3];
    [Range(0f, 1f)][SerializeField] private float windowSoundVolume = 0.8f;
    [Range(0f, 1f)][SerializeField] private float buttonHoverVolume = 0.6f;
    [Range(0f, 1f)][SerializeField] private float buttonClickVolume = 0.8f;

    private GameObject ownedAudioObject;
    private readonly List<EventTrigger> audioTriggers = new List<EventTrigger>();
    private readonly List<EventTrigger.Entry> audioHoverEntries = new List<EventTrigger.Entry>();

    private void PlayUISound(AudioClip clip, float volume)
    {
        if (clip == null) return;
        if (uiAudioSource == null)
        {
            // Separate host lets a close sound finish after the panel is hidden.
            ownedAudioObject = new GameObject("Tours UI Audio");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(
                ownedAudioObject, gameObject.scene);
            uiAudioSource = ownedAudioObject.AddComponent<AudioSource>();
            uiAudioSource.playOnAwake = false;
            uiAudioSource.loop = false;
            uiAudioSource.spatialBlend = 0f;
        }
        if (uiAudioSource.isActiveAndEnabled)
            uiAudioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void PlayRandomUISound(AudioClip[] clips, float volume)
    {
        if (clips == null) return;
        int count = 0;
        foreach (AudioClip clip in clips) if (clip != null) count++;
        if (count == 0) return;
        int index = Random.Range(0, count);
        foreach (AudioClip clip in clips)
        {
            if (clip == null) continue;
            if (index-- == 0)
            {
                PlayUISound(clip, volume);
                return;
            }
        }
    }

    private void SetupButtonHoverSound(Button button)
    {
        if (button == null) return;
        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();
        if (trigger.triggers == null) trigger.triggers = new List<EventTrigger.Entry>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerEnter;
        entry.callback.AddListener(data =>
        {
            if (isOpen && !isClosing && button != null &&
                button.isActiveAndEnabled && button.IsInteractable())
                PlayRandomUISound(buttonHoverSounds, buttonHoverVolume);
        });
        trigger.triggers.Add(entry);
        audioTriggers.Add(trigger);
        audioHoverEntries.Add(entry);
    }

    private void HandleCloseButton()
    {
        if (!isOpen || isClosing || closeButton == null || !closeButton.IsInteractable()) return;
        PlayRandomUISound(buttonClickSounds, buttonClickVolume);
        Close();
    }

    private void OnDestroy()
    {
        if (startTourButton != null) startTourButton.onClick.RemoveListener(StartTour);
        if (closeButton != null) closeButton.onClick.RemoveListener(HandleCloseButton);
        for (int i = 0; i < audioTriggers.Count; i++)
        {
            if (audioTriggers[i] != null && audioTriggers[i].triggers != null)
                audioTriggers[i].triggers.Remove(audioHoverEntries[i]);
        }
        if (ownedAudioObject != null) Destroy(ownedAudioObject);
    }

    // =========================================================
    // POSITION
    // =========================================================

    [Header("Tours Building Position")]

    [SerializeField]
    private float horizontalOffset = 230f;

    [SerializeField]
    private float verticalOffset = 30f;

    [SerializeField]
    private float followSpeed = 15f;

    [SerializeField]
    private bool automaticallyFlipSide = true;

    [SerializeField]
    private float screenEdgePadding = 180f;

    // =========================================================
    // DRAG
    // =========================================================

    [Header("Dragging")]

    [SerializeField]
    private bool allowDragging = true;

    [SerializeField]
    private bool stopFollowingAfterDrag = true;

    [SerializeField]
    private bool clampToCanvas = true;

    [SerializeField]
    private float canvasPadding = 10f;

    [Header("Drag Visuals")]

    [SerializeField]
    private float dragScale = 0.92f;

    [SerializeField]
    private float dragScaleSpeed = 12f;

    [SerializeField]
    private float maxDragSwayAngle = 7f;

    [SerializeField]
    private float swayStrength = 0.3f;

    [SerializeField]
    private float swaySmoothSpeed = 10f;

    [SerializeField]
    private float dropReturnSpeed = 10f;

    // =========================================================
    // TRANSPARENCY
    // =========================================================

    [Header("Transparency")]

    [Range(0f, 1f)]
    [SerializeField]
    private float normalAlpha = 1f;

    [Range(0f, 1f)]
    [SerializeField]
    private float dragAlpha = 0.7f;

    [SerializeField]
    private float alphaSmoothSpeed = 10f;

    // =========================================================
    // ANIMATION
    // =========================================================

    [Header("Open Animation")]

    [SerializeField]
    private float startingScale = 0.65f;

    [SerializeField]
    private float popScale = 1.08f;

    [SerializeField]
    private float normalScale = 1f;

    [SerializeField]
    private float popDuration = 0.1f;

    [SerializeField]
    private float settleDuration = 0.1f;

    [Header("Close Animation")]

    [SerializeField]
    private float closingScale = 0.65f;

    [SerializeField]
    private float closeDuration = 0.14f;

    [SerializeField]
    private bool fadeWhileClosing = true;

    // =========================================================
    // BEHAVIOUR
    // =========================================================

    [Header("Outside Click")]

    [SerializeField]
    private bool closeWhenClickingOutside = true;

    [Header("Mode Behaviour")]

    [SerializeField]
    private bool closeWhenChangingMode = true;

    [SerializeField]
    private bool closeWhenSelectionWheelOpens = true;

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLogs = false;

    // =========================================================
    // PRIVATE
    // =========================================================

    private ToursBuilding currentToursBuilding;

    private InspectableToursBuilding
        currentInspectableToursBuilding;

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
        AutoAssignReferences();

        SetupReputationSlider();

        if (startTourButton != null)
        {
            startTourButton.onClick
                .AddListener(
                    StartTour
                );
        }

        if (closeButton != null)
        {
            closeButton.onClick
                .AddListener(
                    HandleCloseButton
                );
        }

        SetupButtonHoverSound(startTourButton);
        if (closeButton != startTourButton) SetupButtonHoverSound(closeButton);

        if (canvasGroup != null)
            canvasGroup.alpha =
                normalAlpha;

        if (panel != null)
            panel.gameObject
                .SetActive(false);
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private void AutoAssignReferences()
    {
        if (mainCamera == null)
            mainCamera =
                Camera.main;

        if (canvas == null)
            canvas =
                GetComponentInParent<Canvas>();

        if (selectionWheel == null)
            selectionWheel =
                FindFirstObjectByType<SelectionWheel>();

        if (endDaySystem == null)
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();

        if (panel == null)
            panel =
                transform as RectTransform;

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
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();

        if (endDaySystem == null)
            return true;

        return endDaySystem
            .IsActionPhaseActive();
    }

    // =========================================================
    // SLIDER
    // =========================================================

    private void SetupReputationSlider()
    {
        if (reputationSlider == null)
            return;

        reputationSlider.minValue = 0f;
        reputationSlider.maxValue = 100f;
        reputationSlider.interactable = false;
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

        // Updates immediately when the clock reaches 5 PM.
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
            currentToursBuilding == null ||
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
        ToursBuilding toursBuilding)
    {
        if (toursBuilding == null ||
            panel == null)
        {
            return;
        }

        if (InspectionUIManager.Instance != null)
            InspectionUIManager.Instance
                .OpenPanel(this);

        ClearCurrentToursBuildingHighlight();

        if (animationCoroutine != null)
        {
            StopCoroutine(
                animationCoroutine
            );

            animationCoroutine = null;
        }

        currentToursBuilding =
            toursBuilding;

        currentInspectableToursBuilding =
            toursBuilding
                .GetComponent<
                    InspectableToursBuilding>();

        if (currentInspectableToursBuilding == null)
            currentInspectableToursBuilding =
                toursBuilding
                    .GetComponentInChildren<
                        InspectableToursBuilding>();

        if (currentInspectableToursBuilding == null)
            currentInspectableToursBuilding =
                toursBuilding
                    .GetComponentInParent<
                        InspectableToursBuilding>();

        if (currentInspectableToursBuilding != null)
            currentInspectableToursBuilding
                .SetInspected(true);

        isOpen = true;
        isClosing = false;
        isDragging = false;
        manuallyPositioned = false;

        currentSwayAngle = 0f;

        panel.gameObject
            .SetActive(true);

        PlayUISound(openSound, windowSoundVolume);

        panel.localRotation =
            Quaternion.identity;

        if (canvasGroup != null)
            canvasGroup.alpha =
                normalAlpha;

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
    // REFRESH
    // =========================================================

    public void RefreshUI()
    {
        if (currentToursBuilding == null)
            return;

        if (titleText != null)
            titleText.text =
                "TOURS";

        if (reputationSlider != null)
        {
            reputationSlider
                .SetValueWithoutNotify(
                    Mathf.Clamp(
                        currentToursBuilding
                            .GetReputation(),
                        0f,
                        100f
                    )
                );
        }

        if (toursCompletedText != null)
        {
            toursCompletedText.text =
                currentToursBuilding
                    .GetToursCompleted()
                    .ToString();
        }

        if (tourQualityText != null)
        {
            tourQualityText.text =
                currentToursBuilding
                    .GetTourQuality();
        }

        if (potentialRewardText != null)
        {
            float reward =
                currentToursBuilding
                    .CalculateTourReward();

            potentialRewardText.text =
                "+" +
                reward.ToString("0") +
                " REP";
        }

        bool actionActive =
            IsActionPhaseActive();

        bool cooldownReady =
            currentToursBuilding
                .IsTourRecommended();

        // =====================================================
        // TIMING TEXT
        // =====================================================

        if (tourTimingText != null)
        {
            if (!actionActive)
            {
                tourTimingText.text =
                    "DAY ENDED";
            }
            else if (cooldownReady)
            {
                tourTimingText.text =
                    "TOUR READY";
            }
            else
            {
                int days =
                    currentToursBuilding
                        .GetDaysUntilRecommendedTour();

                if (days == 1)
                {
                    tourTimingText.text =
                        "TOUR AVAILABLE IN 1 DAY";
                }
                else
                {
                    tourTimingText.text =
                        "TOUR AVAILABLE IN " +
                        days +
                        " DAYS";
                }
            }
        }

        // =====================================================
        // BUTTON
        // =====================================================

        if (startTourButton != null)
        {
            startTourButton.interactable =
                actionActive &&
                cooldownReady;
        }
    }

    // =========================================================
    // START TOUR
    // =========================================================

    private void StartTour()
    {
        if (currentToursBuilding == null)
            return;

        // =====================================================
        // ACTION PHASE SAFETY CHECK
        // =====================================================

        if (!IsActionPhaseActive())
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    "[TOURS UI] Cannot start tour. " +
                    "The action phase has ended."
                );
            }

            RefreshUI();
            return;
        }

        // =====================================================
        // COOLDOWN SAFETY CHECK
        // =====================================================

        if (!currentToursBuilding
            .IsTourRecommended())
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    "[TOURS UI] Tour is still on cooldown. " +
                    "Days remaining: " +
                    currentToursBuilding
                        .GetDaysUntilRecommendedTour()
                );
            }

            RefreshUI();
            return;
        }

        float scoreBeforeTour =
            currentToursBuilding
                .CalculateTourScore();

        float rewardBeforeTour =
            currentToursBuilding
                .CalculateTourReward();

        string qualityBeforeTour =
            currentToursBuilding
                .GetTourQuality();

        // =====================================================
        // COMPLETE
        // =====================================================

        PlayRandomUISound(buttonClickSounds, buttonClickVolume);
        currentToursBuilding
            .CompleteTour();

        RefreshUI();

        if (showDebugLogs)
        {
            Debug.Log(
                "[TOURS UI] Tour completed." +
                "\nQuality: " +
                qualityBeforeTour +
                "\nScore: " +
                scoreBeforeTour.ToString("0.0") +
                "/100" +
                "\nReward: +" +
                rewardBeforeTour.ToString("0.0") +
                "\nTour button locked for " +
                currentToursBuilding
                    .GetRecommendedDaysBetweenTours() +
                " days."
            );
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

        PlayUISound(closeSound, windowSoundVolume);

        if (animationCoroutine != null)
            StopCoroutine(
                animationCoroutine
            );

        animationCoroutine =
            StartCoroutine(
                CloseAnimation()
            );
    }

    public void CloseImmediately()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(
                animationCoroutine
            );

            animationCoroutine = null;
        }

        ClearCurrentToursBuildingHighlight();

        isOpen = false;
        isClosing = false;
        isDragging = false;
        manuallyPositioned = false;
        ignoreOutsideClick = false;

        currentToursBuilding = null;
        currentSwayAngle = 0f;

        if (panel != null)
        {
            panel.localScale =
                Vector3.one *
                normalScale;

            panel.localRotation =
                Quaternion.identity;

            panel.gameObject
                .SetActive(false);
        }

        if (canvasGroup != null)
            canvasGroup.alpha =
                normalAlpha;

        if (InspectionUIManager.Instance != null)
            InspectionUIManager.Instance
                .ClearPanel(this);
    }

    private void ClearCurrentToursBuildingHighlight()
    {
        if (currentInspectableToursBuilding != null)
            currentInspectableToursBuilding
                .SetInspected(false);

        currentInspectableToursBuilding =
            null;
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

        if (RectTransformUtility
            .RectangleContainsScreenPoint(
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

        foreach (RaycastResult result
                 in results)
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
            canvas.transform
                as RectTransform;

        if (canvasRect == null)
            return;

        if (Input.GetMouseButtonDown(0) &&
            RectTransformUtility
                .RectangleContainsScreenPoint(
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

            Vector2 newPosition =
                mousePosition +
                dragOffset;

            if (clampToCanvas)
                newPosition =
                    ClampPanelToCanvas(
                        newPosition
                    );

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
            float amount =
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
                    Vector3.one *
                    normalScale,
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
        if (currentToursBuilding == null ||
            mainCamera == null ||
            canvas == null)
        {
            return panel.anchoredPosition;
        }

        Vector3 screenPosition =
            mainCamera.WorldToScreenPoint(
                currentToursBuilding
                    .transform.position
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
            canvas.transform
                as RectTransform;

        RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
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
        {
            return position;
        }

        RectTransform canvasRect =
            canvas.transform
                as RectTransform;

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
                size.x *
                pivot.x +
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
                size.y *
                pivot.y +
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
        if (canvas == null)
            return null;

        if (canvas.renderMode ==
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

        animationCoroutine = null;
    }

    private IEnumerator CloseAnimation()
    {
        isClosing = true;
        isDragging = false;

        ClearCurrentToursBuildingHighlight();

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
                    Vector3.one *
                    closingScale,
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

        panel.gameObject
            .SetActive(false);

        panel.localScale =
            Vector3.one *
            normalScale;

        panel.localRotation =
            Quaternion.identity;

        if (canvasGroup != null)
            canvasGroup.alpha =
                normalAlpha;

        currentToursBuilding = null;

        currentSwayAngle = 0f;

        isOpen = false;
        isClosing = false;
        isDragging = false;
        manuallyPositioned = false;

        animationCoroutine = null;

        if (InspectionUIManager.Instance != null)
            InspectionUIManager.Instance
                .ClearPanel(this);
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

    private float EaseOutBack(
        float x)
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
