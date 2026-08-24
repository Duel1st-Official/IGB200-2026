using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class SelectionWheel : MonoBehaviour
{
    public enum PlayerMode
    {
        Normal,
        Build,
        Remove
    }

    [Header("References")]
    public GameObject selectionWheel;
    public RectTransform wheelCenter;
    public RectTransform selectionArrow;
    public TMP_Text modeText;
    public Camera mainCamera;

    [Header("Wheel Options")]
    public Image normalImage;
    public Image buildImage;
    public Image removeImage;

    [Header("Input")]
    public KeyCode wheelKey = KeyCode.Tab;

    [Header("Selection")]
    public float deadZone = 60f;

    [Header("Arrow")]
    public float arrowDistance = 45f;
    public float arrowRotationOffset = 0f;

    [Header("Highlight")]
    public float normalScale = 1f;
    public float selectedScale = 1.2f;
    public float highlightScaleSpeed = 15f;

    [Header("Highlight Wiggle")]
    public float wiggleAmount = 8f;
    public float wiggleSpeed = 20f;
    public float wiggleDuration = 0.25f;

    [Header("Wheel Movement")]
    public float spreadSpeed = 12f;
    public float closeSpeed = 14f;
    public float closeDistanceThreshold = 1f;

    [Header("Wheel Scale")]
    public float closedScale = 0.2f;
    public float openScale = 1f;
    public float scaleInSpeed = 12f;
    public float scaleOutSpeed = 14f;

    [Header("Current Mode")]
    public PlayerMode currentMode = PlayerMode.Normal;

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
        if (wheelCenter == null || mainCamera == null)
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
            mousePosition - centerPosition;

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
            mousePosition - centerPosition;

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
        if (angle >= 45f && angle < 135f)
        {
            highlightedOption = 1;
        }

        // REMOVE - LEFT
        else if (angle >= 135f && angle < 270f)
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
        // NEW OPTION SELECTED
        // =========================

        if (highlightedOption != previousHighlightedOption)
        {
            // Reset previous option
            ResetOptionRotations();

            if (highlightedOption != -1)
            {
                // Start new wiggle
                wiggleTimer = wiggleDuration;
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
            wiggleTimer -= Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    wiggleTimer / wiggleDuration
                );

            float rotation =
                Mathf.Sin(
                    Time.time * wiggleSpeed
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
                Time.deltaTime * highlightScaleSpeed
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
                    Time.deltaTime * highlightScaleSpeed
                );
        }

        if (buildImage != null)
        {
            buildImage.rectTransform.localScale =
                Vector3.Lerp(
                    buildImage.rectTransform.localScale,
                    Vector3.one * normalScale,
                    Time.deltaTime * highlightScaleSpeed
                );
        }

        if (removeImage != null)
        {
            removeImage.rectTransform.localScale =
                Vector3.Lerp(
                    removeImage.rectTransform.localScale,
                    Vector3.one * normalScale,
                    Time.deltaTime * highlightScaleSpeed
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
        currentMode = newMode;

        UpdateModeText();
    }

    // =========================================================
    // SAFETY
    // =========================================================

    private void OnDisable()
    {
        Cursor.visible = true;

        ResetOptionRotations();
    }
}