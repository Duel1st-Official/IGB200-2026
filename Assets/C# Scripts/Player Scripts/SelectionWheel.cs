using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SelectionWheel : MonoBehaviour
{
    public enum PlayerMode
    {
        Normal,
        Build,
        Remove,
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    public GameObject selectionWheel;
    public RectTransform wheelCenter;
    public RectTransform selectionArrow;
    public TMP_Text modeText;
    public Camera mainCamera;

    // =========================================================
    // WHEEL OPTIONS
    // =========================================================

    [Header("Wheel Options")]
    public Image normalImage;
    public Image buildImage;
    public Image removeImage;

    // =========================================================
    // INPUT
    // =========================================================

    [Header("Input")]
    public KeyCode wheelKey = KeyCode.Tab;

    // =========================================================
    // SELECTION
    // =========================================================

    [Header("Selection")]
    public float deadZone = 60f;

    // =========================================================
    // ARROW
    // =========================================================

    [Header("Arrow")]
    public float arrowDistance = 45f;
    public float arrowRotationOffset = 0f;

    // =========================================================
    // HIGHLIGHT
    // =========================================================

    [Header("Highlight")]
    public float normalScale = 1f;
    public float selectedScale = 1.2f;
    public float highlightScaleSpeed = 15f;

    // =========================================================
    // HIGHLIGHT WIGGLE
    // =========================================================

    [Header("Highlight Wiggle")]
    public float wiggleAmount = 8f;
    public float wiggleSpeed = 20f;
    public float wiggleDuration = 0.25f;

    // =========================================================
    // WHEEL MOVEMENT
    // =========================================================

    [Header("Wheel Movement")]
    public float spreadSpeed = 12f;
    public float closeSpeed = 14f;
    public float closeDistanceThreshold = 1f;

    // =========================================================
    // WHEEL SCALE
    // =========================================================

    [Header("Wheel Scale")]
    public float closedScale = 0.2f;
    public float openScale = 1f;
    public float scaleInSpeed = 12f;
    public float scaleOutSpeed = 14f;

    // =========================================================
    // CURRENT MODE
    // =========================================================

    [Header("Current Mode")]
    public PlayerMode currentMode = PlayerMode.Normal;

    // =========================================================
    // GENERAL SOUND EFFECTS
    // =========================================================

    [Header("Sound Effects")]

    [Tooltip("Audio Source used by the selection wheel.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Played when the selection wheel opens.")]
    [SerializeField] private AudioClip openSound;

    [Tooltip("Played when the selection wheel closes.")]
    [SerializeField] private AudioClip closeSound;

    [Tooltip("Played whenever a new wheel option is highlighted.")]
    [SerializeField] private AudioClip hoverSound;

    // =========================================================
    // MODE SELECTION SOUNDS
    // =========================================================

    [Header("Mode Selection Sounds")]

    [Tooltip(
        "Three random sounds for selecting Inspector / Normal mode."
    )]
    [SerializeField]
    private AudioClip[] normalModeSounds =
        new AudioClip[3];

    [Tooltip(
        "Three random sounds for selecting Build mode."
    )]
    [SerializeField]
    private AudioClip[] buildModeSounds =
        new AudioClip[3];

    [Tooltip(
        "Three random sounds for selecting Remove mode."
    )]
    [SerializeField]
    private AudioClip[] removeModeSounds =
        new AudioClip[3];

    // =========================================================
    // SOUND VOLUMES
    // =========================================================

    [Header("Sound Volumes")]

    [Range(0f, 1f)]
    [SerializeField] private float openVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float closeVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float hoverVolume = 0.6f;

    [Range(0f, 1f)]
    [SerializeField] private float selectVolume = 1f;

    // =========================================================
    // SOUND PITCH
    // =========================================================

    [Header("Sound Pitch Variation")]

    [SerializeField] private float hoverPitchMin = 0.95f;
    [SerializeField] private float hoverPitchMax = 1.05f;

    [SerializeField] private float selectPitchMin = 0.95f;
    [SerializeField] private float selectPitchMax = 1.05f;

    // =========================================================
    // PRIVATE
    // =========================================================

    private bool wheelOpen;
    private bool wheelClosing;

    private int highlightedOption = -1;
    private int previousHighlightedOption = -1;

    private float wiggleTimer;

    private Vector2 normalOpenPosition;
    private Vector2 buildOpenPosition;
    private Vector2 removeOpenPosition;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (audioSource == null)
        {
            audioSource =
                GetComponent<AudioSource>();
        }

        // Save open positions from the Inspector
        if (normalImage != null)
        {
            normalOpenPosition =
                normalImage.rectTransform.anchoredPosition;
        }

        if (buildImage != null)
        {
            buildOpenPosition =
                buildImage.rectTransform.anchoredPosition;
        }

        if (removeImage != null)
        {
            removeOpenPosition =
                removeImage.rectTransform.anchoredPosition;
        }

        // Start collapsed
        SetOptionsToCenter();

        if (selectionWheel != null)
        {
            selectionWheel.transform.localScale =
                Vector3.one * closedScale;

            selectionWheel.SetActive(false);
        }

        Cursor.visible = true;

        currentMode = PlayerMode.Normal;

        UpdateModeText();
        ResetOptionRotations();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // =========================
        // OPEN
        // =========================

        if (Input.GetKeyDown(wheelKey))
        {
            OpenWheel();
        }

        // =========================
        // WHEEL OPEN
        // =========================

        if (wheelOpen)
        {
            AnimateWheelOpen();
            AnimateScaleOpen();

            UpdateArrow();
            DetectSelection();

            UpdateVisuals();
        }

        // =========================
        // RELEASE TAB
        // =========================

        if (Input.GetKeyUp(wheelKey))
        {
            ConfirmSelection();

            PlayCloseSound();

            // Return mouse to wheel centre
            MoveCursorToWheelCenter();

            wheelOpen = false;
            wheelClosing = true;

            Cursor.visible = true;

            highlightedOption = -1;
            previousHighlightedOption = -1;

            wiggleTimer = 0f;

            ResetOptionRotations();

            UpdateModeText();
        }

        // =========================
        // CLOSE ANIMATION
        // =========================

        if (wheelClosing)
        {
            AnimateWheelClosed();
            AnimateScaleClosed();
            AnimateOptionsBackToNormalScale();

            CheckIfWheelFinishedClosing();
        }
    }

    // =========================================================
    // OPEN WHEEL
    // =========================================================

    private void OpenWheel()
    {
        wheelClosing = false;
        wheelOpen = true;

        if (selectionWheel != null)
        {
            selectionWheel.SetActive(true);
        }

        PlayOpenSound();

        // Start mouse in middle
        MoveCursorToWheelCenter();

        // Hide system cursor
        Cursor.visible = false;

        highlightedOption = -1;
        previousHighlightedOption = -1;

        wiggleTimer = 0f;

        ResetOptionRotations();

        UpdateHighlightedText();
    }

    // =========================================================
    // CURSOR
    // =========================================================

    private void MoveCursorToWheelCenter()
    {
        if (wheelCenter == null ||
            mainCamera == null)
        {
            return;
        }

        Vector2 centerScreenPosition =
            mainCamera.WorldToScreenPoint(
                wheelCenter.position
            );

        if (Mouse.current != null)
        {
            Mouse.current.WarpCursorPosition(
                centerScreenPosition
            );
        }
    }

    // =========================================================
    // OPEN MOVEMENT
    // =========================================================

    private void AnimateWheelOpen()
    {
        if (normalImage != null)
        {
            normalImage.rectTransform.anchoredPosition =
                Vector2.Lerp(
                    normalImage.rectTransform.anchoredPosition,
                    normalOpenPosition,
                    Time.deltaTime * spreadSpeed
                );
        }

        if (buildImage != null)
        {
            buildImage.rectTransform.anchoredPosition =
                Vector2.Lerp(
                    buildImage.rectTransform.anchoredPosition,
                    buildOpenPosition,
                    Time.deltaTime * spreadSpeed
                );
        }

        if (removeImage != null)
        {
            removeImage.rectTransform.anchoredPosition =
                Vector2.Lerp(
                    removeImage.rectTransform.anchoredPosition,
                    removeOpenPosition,
                    Time.deltaTime * spreadSpeed
                );
        }
    }

    // =========================================================
    // CLOSE MOVEMENT
    // =========================================================

    private void AnimateWheelClosed()
    {
        Vector2 center = Vector2.zero;

        if (normalImage != null)
        {
            normalImage.rectTransform.anchoredPosition =
                Vector2.Lerp(
                    normalImage.rectTransform.anchoredPosition,
                    center,
                    Time.deltaTime * closeSpeed
                );
        }

        if (buildImage != null)
        {
            buildImage.rectTransform.anchoredPosition =
                Vector2.Lerp(
                    buildImage.rectTransform.anchoredPosition,
                    center,
                    Time.deltaTime * closeSpeed
                );
        }

        if (removeImage != null)
        {
            removeImage.rectTransform.anchoredPosition =
                Vector2.Lerp(
                    removeImage.rectTransform.anchoredPosition,
                    center,
                    Time.deltaTime * closeSpeed
                );
        }
    }

    // =========================================================
    // WHEEL SCALE OPEN
    // =========================================================

    private void AnimateScaleOpen()
    {
        if (selectionWheel == null)
        {
            return;
        }

        selectionWheel.transform.localScale =
            Vector3.Lerp(
                selectionWheel.transform.localScale,
                Vector3.one * openScale,
                Time.deltaTime * scaleInSpeed
            );
    }

    // =========================================================
    // WHEEL SCALE CLOSED
    // =========================================================

    private void AnimateScaleClosed()
    {
        if (selectionWheel == null)
        {
            return;
        }

        selectionWheel.transform.localScale =
            Vector3.Lerp(
                selectionWheel.transform.localScale,
                Vector3.one * closedScale,
                Time.deltaTime * scaleOutSpeed
            );
    }

    // =========================================================
    // FINISH CLOSING
    // =========================================================

    private void CheckIfWheelFinishedClosing()
    {
        Vector2 center = Vector2.zero;

        bool normalClosed =
            normalImage == null ||
            Vector2.Distance(
                normalImage.rectTransform.anchoredPosition,
                center
            ) < closeDistanceThreshold;

        bool buildClosed =
            buildImage == null ||
            Vector2.Distance(
                buildImage.rectTransform.anchoredPosition,
                center
            ) < closeDistanceThreshold;

        bool removeClosed =
            removeImage == null ||
            Vector2.Distance(
                removeImage.rectTransform.anchoredPosition,
                center
            ) < closeDistanceThreshold;

        bool scaleClosed =
            selectionWheel == null ||
            Mathf.Abs(
                selectionWheel.transform.localScale.x -
                closedScale
            ) < 0.02f;

        if (normalClosed &&
            buildClosed &&
            removeClosed &&
            scaleClosed)
        {
            SetOptionsToCenter();

            ResetOptionRotations();

            if (selectionWheel != null)
            {
                selectionWheel.transform.localScale =
                    Vector3.one * closedScale;

                selectionWheel.SetActive(false);
            }

            wheelClosing = false;
        }
    }

    // =========================================================
    // CENTER OPTIONS
    // =========================================================

    private void SetOptionsToCenter()
    {
        if (normalImage != null)
        {
            normalImage.rectTransform.anchoredPosition =
                Vector2.zero;
        }

        if (buildImage != null)
        {
            buildImage.rectTransform.anchoredPosition =
                Vector2.zero;
        }

        if (removeImage != null)
        {
            removeImage.rectTransform.anchoredPosition =
                Vector2.zero;
        }
    }

    // =========================================================
    // SELECTION ARROW
    // =========================================================

    private void UpdateArrow()
    {
        if (selectionArrow == null ||
            wheelCenter == null ||
            mainCamera == null)
        {
            return;
        }

        Vector2 mousePosition =
            Input.mousePosition;

        Vector2 centerPosition =
            mainCamera.WorldToScreenPoint(
                wheelCenter.position
            );

        Vector2 direction =
            mousePosition -
            centerPosition;

        // Cursor is in centre
        if (direction.sqrMagnitude <= 0.01f)
        {
            selectionArrow.anchoredPosition =
                Vector2.zero;

            return;
        }

        direction.Normalize();

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        selectionArrow.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle + arrowRotationOffset
            );

        selectionArrow.anchoredPosition =
            direction * arrowDistance;
    }

    // =========================================================
    // DETECT SELECTION
    // =========================================================

    private void DetectSelection()
    {
        if (wheelCenter == null ||
            mainCamera == null)
        {
            return;
        }

        Vector2 mousePosition =
            Input.mousePosition;

        Vector2 centerPosition =
            mainCamera.WorldToScreenPoint(
                wheelCenter.position
            );

        Vector2 direction =
            mousePosition -
            centerPosition;

        // =========================
        // DEAD ZONE
        // =========================

        if (direction.magnitude < deadZone)
        {
            highlightedOption = -1;

            UpdateHighlightedText();

            return;
        }

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) * Mathf.Rad2Deg;

        if (angle < 0f)
        {
            angle += 360f;
        }

        /*
                         BUILD
                          90°
                           ▲
                           |
                           |

           REMOVE 180° ◀────●────▶ 0° INSPECT
        */

        // BUILD - TOP
        if (angle >= 45f &&
            angle < 135f)
        {
            highlightedOption = 1;
        }

        // REMOVE - LEFT
        else if (angle >= 135f &&
                 angle < 270f)
        {
            highlightedOption = 2;
        }

        // INSPECTOR - RIGHT
        else
        {
            highlightedOption = 0;
        }

        UpdateHighlightedText();
    }

    // =========================================================
    // HIGHLIGHT VISUALS
    // =========================================================

    private void UpdateVisuals()
    {
        // =========================
        // NEW OPTION HIGHLIGHTED
        // =========================

        if (highlightedOption !=
            previousHighlightedOption)
        {
            ResetOptionRotations();

            if (highlightedOption != -1)
            {
                wiggleTimer =
                    wiggleDuration;

                PlayHoverSound();
            }
            else
            {
                wiggleTimer = 0f;
            }

            previousHighlightedOption =
                highlightedOption;
        }

        // =========================
        // SCALE
        // =========================

        UpdateOptionScale(
            normalImage,
            highlightedOption == 0
        );

        UpdateOptionScale(
            buildImage,
            highlightedOption == 1
        );

        UpdateOptionScale(
            removeImage,
            highlightedOption == 2
        );

        // =========================
        // WIGGLE
        // =========================

        if (wiggleTimer > 0f &&
            highlightedOption != -1)
        {
            wiggleTimer -=
                Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    wiggleTimer /
                    wiggleDuration
                );

            float rotation =
                Mathf.Sin(
                    Time.time *
                    wiggleSpeed
                )
                * wiggleAmount
                * progress;

            SetOptionRotation(
                highlightedOption,
                rotation
            );
        }
        else
        {
            wiggleTimer = 0f;

            ResetOptionRotations();
        }
    }

    // =========================================================
    // OPTION SCALE
    // =========================================================

    private void UpdateOptionScale(
        Image image,
        bool selected)
    {
        if (image == null)
        {
            return;
        }

        float targetScale =
            selected
                ? selectedScale
                : normalScale;

        image.rectTransform.localScale =
            Vector3.Lerp(
                image.rectTransform.localScale,
                Vector3.one * targetScale,
                Time.deltaTime *
                highlightScaleSpeed
            );
    }

    // =========================================================
    // RESET SCALE WHILE CLOSING
    // =========================================================

    private void AnimateOptionsBackToNormalScale()
    {
        if (normalImage != null)
        {
            normalImage.rectTransform.localScale =
                Vector3.Lerp(
                    normalImage.rectTransform.localScale,
                    Vector3.one * normalScale,
                    Time.deltaTime *
                    highlightScaleSpeed
                );
        }

        if (buildImage != null)
        {
            buildImage.rectTransform.localScale =
                Vector3.Lerp(
                    buildImage.rectTransform.localScale,
                    Vector3.one * normalScale,
                    Time.deltaTime *
                    highlightScaleSpeed
                );
        }

        if (removeImage != null)
        {
            removeImage.rectTransform.localScale =
                Vector3.Lerp(
                    removeImage.rectTransform.localScale,
                    Vector3.one * normalScale,
                    Time.deltaTime *
                    highlightScaleSpeed
                );
        }
    }

    // =========================================================
    // WIGGLE ROTATION
    // =========================================================

    private void SetOptionRotation(
        int option,
        float rotation)
    {
        // Reset other icons first
        ResetOptionRotations();

        switch (option)
        {
            // Inspector
            case 0:

                if (normalImage != null)
                {
                    normalImage.rectTransform.localRotation =
                        Quaternion.Euler(
                            0f,
                            0f,
                            rotation
                        );
                }

                break;

            // Build
            case 1:

                if (buildImage != null)
                {
                    buildImage.rectTransform.localRotation =
                        Quaternion.Euler(
                            0f,
                            0f,
                            rotation
                        );
                }

                break;

            // Remove
            case 2:

                if (removeImage != null)
                {
                    removeImage.rectTransform.localRotation =
                        Quaternion.Euler(
                            0f,
                            0f,
                            rotation
                        );
                }

                break;
        }
    }

    // =========================================================
    // RESET ROTATION
    // =========================================================

    private void ResetOptionRotations()
    {
        if (normalImage != null)
        {
            normalImage.rectTransform.localRotation =
                Quaternion.identity;
        }

        if (buildImage != null)
        {
            buildImage.rectTransform.localRotation =
                Quaternion.identity;
        }

        if (removeImage != null)
        {
            removeImage.rectTransform.localRotation =
                Quaternion.identity;
        }
    }

    // =========================================================
    // CONFIRM SELECTION
    // =========================================================

    private void ConfirmSelection()
    {
        if (highlightedOption == -1)
        {
            return;
        }

        switch (highlightedOption)
        {
            // Inspector
            case 0:

                currentMode =
                    PlayerMode.Normal;

                break;

            // Build
            case 1:

                currentMode =
                    PlayerMode.Build;

                break;

            // Remove
            case 2:

                currentMode =
                    PlayerMode.Remove;

                break;
        }

        // Play one of the 3 sounds belonging
        // to the mode we just selected.
        PlayModeSelectionSound();

        UpdateModeText();

        Debug.Log(
            "Current Player Mode: " +
            currentMode
        );
    }

    // =========================================================
    // HIGHLIGHT TEXT
    // =========================================================

    private void UpdateHighlightedText()
    {
        if (modeText == null)
        {
            return;
        }

        switch (highlightedOption)
        {
            case 0:

                modeText.text =
                    "Inspector Mode";

                break;

            case 1:

                modeText.text =
                    "Build Mode";

                break;

            case 2:

                modeText.text =
                    "Remove Mode";

                break;

            default:

                UpdateModeText();

                break;
        }
    }

    // =========================================================
    // CURRENT MODE TEXT
    // =========================================================

    private void UpdateModeText()
    {
        if (modeText == null)
        {
            return;
        }

        switch (currentMode)
        {
            case PlayerMode.Normal:

                modeText.text =
                    "Inspector Mode";

                break;

            case PlayerMode.Build:

                modeText.text =
                    "Build Mode";

                break;

            case PlayerMode.Remove:

                modeText.text =
                    "Remove Mode";

                break;
        }
    }

    // =========================================================
    // OPEN SOUND
    // =========================================================

    private void PlayOpenSound()
    {
        PlaySound(
            openSound,
            openVolume
        );
    }

    // =========================================================
    // CLOSE SOUND
    // =========================================================

    private void PlayCloseSound()
    {
        PlaySound(
            closeSound,
            closeVolume
        );
    }

    // =========================================================
    // HOVER SOUND
    // =========================================================

    private void PlayHoverSound()
    {
        if (audioSource == null ||
            hoverSound == null)
        {
            return;
        }

        float originalPitch =
            audioSource.pitch;

        audioSource.pitch =
            Random.Range(
                hoverPitchMin,
                hoverPitchMax
            );

        audioSource.PlayOneShot(
            hoverSound,
            hoverVolume
        );

        audioSource.pitch =
            originalPitch;
    }

    // =========================================================
    // MODE SELECTION SOUND
    // =========================================================

    private void PlayModeSelectionSound()
    {
        AudioClip[] sounds = null;

        switch (currentMode)
        {
            case PlayerMode.Normal:

                sounds =
                    normalModeSounds;

                break;

            case PlayerMode.Build:

                sounds =
                    buildModeSounds;

                break;

            case PlayerMode.Remove:

                sounds =
                    removeModeSounds;

                break;
        }

        PlayRandomModeSound(
            sounds
        );
    }

    // =========================================================
    // RANDOM MODE SOUND
    // =========================================================

    private void PlayRandomModeSound(
        AudioClip[] sounds)
    {
        if (audioSource == null)
        {
            return;
        }

        if (sounds == null ||
            sounds.Length == 0)
        {
            return;
        }

        // Build a count of valid sounds.
        int validSoundCount = 0;

        for (int i = 0;
             i < sounds.Length;
             i++)
        {
            if (sounds[i] != null)
            {
                validSoundCount++;
            }
        }

        if (validSoundCount == 0)
        {
            return;
        }

        // Choose from only the valid sounds.
        int randomValidIndex =
            Random.Range(
                0,
                validSoundCount
            );

        AudioClip selectedClip =
            null;

        int currentValidIndex = 0;

        for (int i = 0;
             i < sounds.Length;
             i++)
        {
            if (sounds[i] == null)
            {
                continue;
            }

            if (currentValidIndex ==
                randomValidIndex)
            {
                selectedClip =
                    sounds[i];

                break;
            }

            currentValidIndex++;
        }

        if (selectedClip == null)
        {
            return;
        }

        float originalPitch =
            audioSource.pitch;

        audioSource.pitch =
            Random.Range(
                selectPitchMin,
                selectPitchMax
            );

        audioSource.PlayOneShot(
            selectedClip,
            selectVolume
        );

        audioSource.pitch =
            originalPitch;
    }

    // =========================================================
    // GENERIC SOUND
    // =========================================================

    private void PlaySound(
        AudioClip clip,
        float volume)
    {
        if (audioSource == null ||
            clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(
            clip,
            volume
        );
    }

    // =========================================================
    // PUBLIC MODE CHECKS
    // =========================================================

    public bool IsNormalMode()
    {
        return currentMode ==
               PlayerMode.Normal;
    }

    public bool IsBuildMode()
    {
        return currentMode ==
               PlayerMode.Build;
    }

    public bool IsRemoveMode()
    {
        return currentMode ==
               PlayerMode.Remove;
    }

    public bool IsWheelOpen()
    {
        return wheelOpen;
    }

    public void SetMode(
        PlayerMode newMode)
    {
        currentMode =
            newMode;

        UpdateModeText();
    }

    public bool IsInspectorMode()
    {
        return currentMode ==
               PlayerMode.Normal;
    }

    // =========================================================
    // SAFETY
    // =========================================================

    private void OnDisable()
    {
        Cursor.visible = true;

        ResetOptionRotations();

        if (audioSource != null)
        {
            audioSource.pitch = 1f;
        }
    }
}