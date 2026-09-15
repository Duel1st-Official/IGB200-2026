using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class TrapInspectionUI : MonoBehaviour, IInspectionPanel
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private SelectionWheel selectionWheel;
    [SerializeField] private EndDaySystem endDaySystem;

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
    [SerializeField] private TMP_Text predatorText;
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

    [Tooltip("Shown only while the trap is Empty.")]
    [SerializeField] private Button setTrapButton;

    [Tooltip("Shown only when a predator has been caught.")]
    [SerializeField] private Button relocateButton;

    [SerializeField] private Button closeButton;

    // =========================================================
    // AUDIO SOURCE
    // =========================================================

    [Header("Trap Audio")]

    [Tooltip(
        "AudioSource used by the Trap inspection UI. " +
        "If left empty, one will be found or created automatically."
    )]
    [SerializeField] private AudioSource audioSource;

    // =========================================================
    // BUTTON HOVER SOUNDS
    // =========================================================

    [Header("Button Hover Sounds")]

    [Tooltip(
        "Random sound played when hovering over Set Trap or Relocate."
    )]
    [SerializeField]
    private AudioClip[] buttonHoverSounds =
        new AudioClip[3];

    // =========================================================
    // BUTTON CLICK SOUNDS
    // =========================================================

    [Header("Button Click Sounds")]

    [Tooltip(
        "Random UI click sound played when Set Trap or Relocate is pressed."
    )]
    [SerializeField]
    private AudioClip[] buttonClickSounds =
        new AudioClip[3];

    // =========================================================
    // TRAP SET SOUNDS
    // =========================================================

    [Header("Trap Set Sounds")]

    [Tooltip(
        "Random sound played when the trap is successfully set."
    )]
    [SerializeField]
    private AudioClip[] trapSetSounds =
        new AudioClip[3];

    // =========================================================
    // ANIMAL-SPECIFIC RELOCATE SOUNDS
    // =========================================================

    [Header("Feral Cat Relocate Sounds")]

    [Tooltip(
        "Sounds played when relocating a Feral Cat."
    )]
    [SerializeField]
    private AudioClip[] feralCatRelocateSounds =
        new AudioClip[3];

    [Header("Fox Relocate Sounds")]

    [Tooltip(
        "Sounds played when relocating a Fox."
    )]
    [SerializeField]
    private AudioClip[] foxRelocateSounds =
        new AudioClip[3];

    [Header("Generic Relocate Sounds")]

    [Tooltip(
        "Fallback sounds used if the caught predator does not have its own sound group."
    )]
    [SerializeField]
    private AudioClip[] genericRelocateSounds =
        new AudioClip[3];

    // =========================================================
    // AUDIO VOLUME
    // =========================================================

    [Header("Audio Volume")]

    [Range(0f, 1f)]
    [SerializeField] private float hoverVolume = 0.6f;

    [Range(0f, 1f)]
    [SerializeField] private float clickVolume = 0.8f;

    [Range(0f, 1f)]
    [SerializeField] private float trapSetVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float relocateVolume = 1f;

    // =========================================================
    // AUDIO PITCH
    // =========================================================

    [Header("Audio Pitch")]
    [SerializeField] private float audioPitchMin = 0.95f;
    [SerializeField] private float audioPitchMax = 1.05f;

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

    private EventTrigger setTrapEventTrigger;
    private EventTrigger relocateEventTrigger;

    private EventTrigger.Entry setTrapHoverEntry;
    private EventTrigger.Entry relocateHoverEntry;

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

        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }

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
        // AUDIO
        // -----------------------------------------------------

        SetupAudioSource();

        // -----------------------------------------------------
        // BUTTONS
        // -----------------------------------------------------

        if (setTrapButton != null)
        {
            setTrapButton.onClick.AddListener(
                HandleSetTrapButton
            );

            SetupSetTrapHover();
        }

        if (relocateButton != null)
        {
            relocateButton.onClick.AddListener(
                HandleRelocateButton
            );

            SetupRelocateHover();
        }

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
    // ACTION PHASE
    // =========================================================

    private bool IsActionPhaseActive()
    {
        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }

        // If no EndDaySystem exists, don't break the Trap UI.
        if (endDaySystem == null)
        {
            return true;
        }

        return endDaySystem.IsActionPhaseActive();
    }

    // =========================================================
    // AUDIO SOURCE
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

        audioSource.enabled =
            true;

        audioSource.playOnAwake =
            false;

        audioSource.loop =
            false;

        audioSource.spatialBlend =
            0f;
    }

    // =========================================================
    // SET TRAP HOVER
    // =========================================================

    private void SetupSetTrapHover()
    {
        if (setTrapButton == null)
        {
            return;
        }

        setTrapEventTrigger =
            setTrapButton.GetComponent<EventTrigger>();

        if (setTrapEventTrigger == null)
        {
            setTrapEventTrigger =
                setTrapButton.gameObject.AddComponent<EventTrigger>();
        }

        if (setTrapEventTrigger.triggers == null)
        {
            setTrapEventTrigger.triggers =
                new List<EventTrigger.Entry>();
        }

        setTrapHoverEntry =
            new EventTrigger.Entry();

        setTrapHoverEntry.eventID =
            EventTriggerType.PointerEnter;

        setTrapHoverEntry.callback =
            new EventTrigger.TriggerEvent();

        setTrapHoverEntry.callback.AddListener(
            (data) =>
            {
                PlaySetTrapHoverSound();
            }
        );

        setTrapEventTrigger.triggers.Add(
            setTrapHoverEntry
        );
    }

    // =========================================================
    // RELOCATE HOVER
    // =========================================================

    private void SetupRelocateHover()
    {
        if (relocateButton == null)
        {
            return;
        }

        relocateEventTrigger =
            relocateButton.GetComponent<EventTrigger>();

        if (relocateEventTrigger == null)
        {
            relocateEventTrigger =
                relocateButton.gameObject.AddComponent<EventTrigger>();
        }

        if (relocateEventTrigger.triggers == null)
        {
            relocateEventTrigger.triggers =
                new List<EventTrigger.Entry>();
        }

        relocateHoverEntry =
            new EventTrigger.Entry();

        relocateHoverEntry.eventID =
            EventTriggerType.PointerEnter;

        relocateHoverEntry.callback =
            new EventTrigger.TriggerEvent();

        relocateHoverEntry.callback.AddListener(
            (data) =>
            {
                PlayRelocateHoverSound();
            }
        );

        relocateEventTrigger.triggers.Add(
            relocateHoverEntry
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

        // Refresh every frame so Set / Relocate immediately
        // become unavailable when the action phase ends.
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
        Trap trap)
    {
        if (trap == null ||
            panel == null)
        {
            return;
        }

        if (InspectionUIManager.Instance != null)
        {
            InspectionUIManager.Instance.OpenPanel(
                this
            );
        }

        ClearCurrentTrapHighlight();

        if (animationCoroutine != null)
        {
            StopCoroutine(
                animationCoroutine
            );

            animationCoroutine =
                null;
        }

        currentTrap =
            trap;

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

        if (currentInspectableTrap != null)
        {
            currentInspectableTrap.SetInspected(
                true
            );
        }

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
    // REFRESH UI
    // =========================================================

    public void RefreshUI()
    {
        if (currentTrap == null)
        {
            return;
        }

        bool actionPhaseActive =
            IsActionPhaseActive();

        if (titleText != null)
        {
            titleText.text =
                "PREDATOR TRAP";
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

            if (predatorText != null)
            {
                predatorText.text =
                    "";
            }

            if (descriptionText != null)
            {
                if (actionPhaseActive)
                {
                    descriptionText.text =
                        "Set and bait the trap to monitor nearby predators.";
                }
                else
                {
                    descriptionText.text =
                        "The working day has ended. End the day before setting this trap.";
                }
            }

            if (setTrapButton != null)
            {
                setTrapButton.gameObject.SetActive(
                    true
                );

                setTrapButton.interactable =
                    actionPhaseActive;
            }

            if (relocateButton != null)
            {
                relocateButton.gameObject.SetActive(
                    false
                );

                relocateButton.interactable =
                    false;
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

            if (predatorText != null)
            {
                predatorText.text =
                    "";
            }

            if (descriptionText != null)
            {
                descriptionText.text =
                    "The trap is baited and ready. Check again tomorrow.";
            }

            if (setTrapButton != null)
            {
                setTrapButton.gameObject.SetActive(
                    false
                );

                setTrapButton.interactable =
                    false;
            }

            if (relocateButton != null)
            {
                relocateButton.gameObject.SetActive(
                    false
                );

                relocateButton.interactable =
                    false;
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

            if (predatorText != null)
            {
                string predator =
                    currentTrap.GetCaughtMammalName();

                if (string.IsNullOrWhiteSpace(
                    predator))
                {
                    predator =
                        "PREDATOR";
                }

                predatorText.text =
                    predator.ToUpper();
            }

            if (descriptionText != null)
            {
                if (actionPhaseActive)
                {
                    descriptionText.text =
                        "A predator has been safely captured. Relocate it away from the conservation area.";
                }
                else
                {
                    descriptionText.text =
                        "A predator has been safely captured. End the day before relocating it.";
                }
            }

            if (setTrapButton != null)
            {
                setTrapButton.gameObject.SetActive(
                    false
                );

                setTrapButton.interactable =
                    false;
            }

            if (relocateButton != null)
            {
                relocateButton.gameObject.SetActive(
                    true
                );

                relocateButton.interactable =
                    actionPhaseActive;
            }

            return;
        }

        // =====================================================
        // FALLBACK
        // =====================================================

        if (setTrapButton != null)
        {
            setTrapButton.gameObject.SetActive(
                false
            );

            setTrapButton.interactable =
                false;
        }

        if (relocateButton != null)
        {
            relocateButton.gameObject.SetActive(
                false
            );

            relocateButton.interactable =
                false;
        }
    }

    // =========================================================
    // SET TRAP BUTTON
    // =========================================================

    private void HandleSetTrapButton()
    {
        if (currentTrap == null)
        {
            return;
        }

        // -----------------------------------------------------
        // END DAY LOCK
        // -----------------------------------------------------

        if (!IsActionPhaseActive())
        {
            RefreshUI();
            return;
        }

        if (!currentTrap.IsEmpty())
        {
            return;
        }

        if (setTrapButton != null &&
            !setTrapButton.interactable)
        {
            return;
        }

        // -----------------------------------------------------
        // CLICK
        // -----------------------------------------------------

        PlayRandomSound(
            buttonClickSounds,
            clickVolume
        );

        // -----------------------------------------------------
        // SET
        // -----------------------------------------------------

        currentTrap.SetTrap();

        // -----------------------------------------------------
        // ACTION SFX
        // -----------------------------------------------------

        PlayRandomSound(
            trapSetSounds,
            trapSetVolume
        );

        RefreshUI();
    }

    // =========================================================
    // RELOCATE BUTTON
    // =========================================================

    private void HandleRelocateButton()
    {
        if (currentTrap == null)
        {
            return;
        }

        // -----------------------------------------------------
        // END DAY LOCK
        // -----------------------------------------------------

        if (!IsActionPhaseActive())
        {
            RefreshUI();
            return;
        }

        if (!currentTrap.IsCaught())
        {
            return;
        }

        if (relocateButton != null &&
            !relocateButton.interactable)
        {
            return;
        }

        // -----------------------------------------------------
        // SAVE PREDATOR NAME FIRST
        // -----------------------------------------------------

        string predatorName =
            currentTrap.GetCaughtMammalName();

        // -----------------------------------------------------
        // CLICK
        // -----------------------------------------------------

        PlayRandomSound(
            buttonClickSounds,
            clickVolume
        );

        // -----------------------------------------------------
        // RELOCATE
        // -----------------------------------------------------

        currentTrap.RelocateCaughtPredator();

        // -----------------------------------------------------
        // ANIMAL-SPECIFIC SFX
        // -----------------------------------------------------

        PlayRelocateSoundForPredator(
            predatorName
        );

        RefreshUI();
    }

    // =========================================================
    // ANIMAL-SPECIFIC RELOCATE SOUND
    // =========================================================

    private void PlayRelocateSoundForPredator(
        string predatorName)
    {
        if (string.IsNullOrWhiteSpace(
            predatorName))
        {
            PlayRandomSound(
                genericRelocateSounds,
                relocateVolume
            );

            return;
        }

        string normalizedName =
            predatorName
                .Trim()
                .ToLowerInvariant();

        // =====================================================
        // FERAL CAT
        // =====================================================

        if (normalizedName ==
                "feral cat" ||
            normalizedName ==
                "cat")
        {
            PlayRandomSound(
                feralCatRelocateSounds,
                relocateVolume
            );

            return;
        }

        // =====================================================
        // FOX
        // =====================================================

        if (normalizedName ==
                "fox" ||
            normalizedName ==
                "red fox")
        {
            PlayRandomSound(
                foxRelocateSounds,
                relocateVolume
            );

            return;
        }

        // =====================================================
        // FALLBACK
        // =====================================================

        PlayRandomSound(
            genericRelocateSounds,
            relocateVolume
        );
    }

    // =========================================================
    // SET TRAP HOVER SOUND
    // =========================================================

    private void PlaySetTrapHoverSound()
    {
        if (setTrapButton == null)
        {
            return;
        }

        if (!setTrapButton.gameObject.activeInHierarchy ||
            !setTrapButton.interactable)
        {
            return;
        }

        PlayRandomSound(
            buttonHoverSounds,
            hoverVolume
        );
    }

    // =========================================================
    // RELOCATE HOVER SOUND
    // =========================================================

    private void PlayRelocateHoverSound()
    {
        if (relocateButton == null)
        {
            return;
        }

        if (!relocateButton.gameObject.activeInHierarchy ||
            !relocateButton.interactable)
        {
            return;
        }

        PlayRandomSound(
            buttonHoverSounds,
            hoverVolume
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

        if (!audioSource.enabled)
        {
            audioSource.enabled =
                true;
        }

        if (!audioSource.gameObject.activeInHierarchy)
        {
            return;
        }

        // -----------------------------------------------------
        // COUNT VALID CLIPS
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // PICK RANDOM VALID CLIP
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // PITCH
        // -----------------------------------------------------

        float originalPitch =
            audioSource.pitch;

        float minPitch =
            Mathf.Min(
                audioPitchMin,
                audioPitchMax
            );

        float maxPitch =
            Mathf.Max(
                audioPitchMin,
                audioPitchMax
            );

        audioSource.pitch =
            Random.Range(
                minPitch,
                maxPitch
            );

        // -----------------------------------------------------
        // PLAY
        // -----------------------------------------------------

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

        ClearCurrentTrapHighlight();

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

        currentTrap =
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

    private void ClearCurrentTrapHighlight()
    {
        if (currentInspectableTrap != null)
        {
            currentInspectableTrap.SetInspected(
                false
            );
        }

        currentInspectableTrap =
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

        // =====================================================
        // RETURN
        // =====================================================

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

            panel.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    currentSwayAngle
                );
        }
    }

    // =========================================================
    // FOLLOW
    // =========================================================

    private void UpdatePanelPosition()
    {
        Vector2 targetPosition =
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
                targetPosition,
                amount
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

        Vector3 screenPosition =
            mainCamera.WorldToScreenPoint(
                currentTrap.transform.position
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
            out Vector2 result
        );

        if (clampToCanvas)
        {
            result =
                ClampPanelToCanvas(
                    result
                );
        }

        return result;
    }

    // =========================================================
    // CLAMP
    // =========================================================

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
        {
            return position;
        }

        Rect bounds =
            canvasRect.rect;

        Vector2 size =
            panel.rect.size;

        Vector2 pivot =
            panel.pivot;

        float minX =
            bounds.xMin +
            size.x *
            pivot.x +
            canvasPadding;

        float maxX =
            bounds.xMax -
            size.x *
            (1f - pivot.x) -
            canvasPadding;

        float minY =
            bounds.yMin +
            size.y *
            pivot.y +
            canvasPadding;

        float maxY =
            bounds.yMax -
            size.y *
            (1f - pivot.y) -
            canvasPadding;

        position.x =
            Mathf.Clamp(
                position.x,
                minX,
                maxX
            );

        position.y =
            Mathf.Clamp(
                position.y,
                minY,
                maxY
            );

        return position;
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

        ClearCurrentTrapHighlight();

        Vector3 startScale =
            panel.localScale;

        Quaternion startRotation =
            panel.localRotation;

        float startAlpha =
            canvasGroup != null
                ? canvasGroup.alpha
                : normalAlpha;

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
                t *
                t *
                t;

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
    // SCALE
    // =========================================================

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
            Vector3.one *
            to;
    }

    // =========================================================
    // OUTSIDE CLICK DELAY
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

    private void OnDestroy()
    {
        // -----------------------------------------------------
        // BUTTON LISTENERS
        // -----------------------------------------------------

        if (setTrapButton != null)
        {
            setTrapButton.onClick.RemoveListener(
                HandleSetTrapButton
            );
        }

        if (relocateButton != null)
        {
            relocateButton.onClick.RemoveListener(
                HandleRelocateButton
            );
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(
                Close
            );
        }

        // -----------------------------------------------------
        // HOVER EVENTS
        // -----------------------------------------------------

        if (setTrapEventTrigger != null &&
            setTrapHoverEntry != null &&
            setTrapEventTrigger.triggers != null)
        {
            setTrapEventTrigger.triggers.Remove(
                setTrapHoverEntry
            );
        }

        if (relocateEventTrigger != null &&
            relocateHoverEntry != null &&
            relocateEventTrigger.triggers != null)
        {
            relocateEventTrigger.triggers.Remove(
                relocateHoverEntry
            );
        }
    }
}