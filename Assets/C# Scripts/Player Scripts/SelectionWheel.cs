using UnityEngine;
using UnityEngine.UI;
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

    private Vector2 normalOpenPosition;
    private Vector2 buildOpenPosition;
    private Vector2 removeOpenPosition;

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Save the open positions from the Inspector
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

        // Start options in the centre
        SetOptionsToCenter();

        // Start wheel small
        if (selectionWheel != null)
        {
            selectionWheel.transform.localScale =
                Vector3.one * closedScale;

            selectionWheel.SetActive(false);
        }

        currentMode = PlayerMode.Normal;

        UpdateModeText();
        UpdateVisuals();
    }

    private void Update()
    {
        // =========================
        // OPEN WHEEL
        // =========================

        if (Input.GetKeyDown(wheelKey))
        {
            OpenWheel();
        }

        // =========================
        // WHILE OPEN
        // =========================

        if (wheelOpen)
        {
            AnimateWheelOpen();
            AnimateScaleOpen();

            UpdateArrow();
            DetectSelection();
        }

        // =========================
        // RELEASE TAB
        // =========================

        if (Input.GetKeyUp(wheelKey))
        {
            ConfirmSelection();

            wheelOpen = false;
            wheelClosing = true;

            highlightedOption = -1;

            UpdateVisuals();
            UpdateModeText();
        }

        // =========================
        // CLOSING
        // =========================

        if (wheelClosing)
        {
            AnimateWheelClosed();
            AnimateScaleClosed();
            CheckIfWheelFinishedClosing();
        }
    }

    private void OpenWheel()
    {
        wheelClosing = false;
        wheelOpen = true;

        if (selectionWheel != null)
        {
            selectionWheel.SetActive(true);
        }

        highlightedOption = -1;

        UpdateVisuals();
        UpdateHighlightedText();
    }

    // =========================
    // OPEN MOVEMENT
    // =========================

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

    // =========================
    // CLOSE MOVEMENT
    // =========================

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

    // =========================
    // SCALE OPEN
    // =========================

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

    // =========================
    // SCALE CLOSED
    // =========================

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

            if (selectionWheel != null)
            {
                selectionWheel.transform.localScale =
                    Vector3.one * closedScale;

                selectionWheel.SetActive(false);
            }

            wheelClosing = false;
        }
    }

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

    // =========================
    // ARROW
    // =========================

    private void UpdateArrow()
    {
        if (selectionArrow == null ||
            wheelCenter == null ||
            mainCamera == null)
        {
            return;
        }

        Vector2 mousePosition = Input.mousePosition;

        Vector2 centerPosition =
            mainCamera.WorldToScreenPoint(
                wheelCenter.position
            );

        Vector2 direction =
            mousePosition - centerPosition;

        if (direction.sqrMagnitude <= 0.01f)
        {
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

    // =========================
    // SELECTION
    // =========================

    private void DetectSelection()
    {
        if (wheelCenter == null ||
            mainCamera == null)
        {
            return;
        }

        Vector2 mousePosition = Input.mousePosition;

        Vector2 centerPosition =
            mainCamera.WorldToScreenPoint(
                wheelCenter.position
            );

        Vector2 direction =
            mousePosition - centerPosition;

        // Dead zone
        if (direction.magnitude < deadZone)
        {
            highlightedOption = -1;

            UpdateVisuals();
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

        // Inspector Mode
        if (angle >= 300f || angle < 60f)
        {
            highlightedOption = 0;
        }

        // Build Mode
        else if (angle >= 60f && angle < 180f)
        {
            highlightedOption = 1;
        }

        // Remove Mode
        else
        {
            highlightedOption = 2;
        }

        UpdateVisuals();
        UpdateHighlightedText();
    }

    // =========================
    // CONFIRM
    // =========================

    private void ConfirmSelection()
    {
        if (highlightedOption == -1)
        {
            return;
        }

        switch (highlightedOption)
        {
            case 0:
                currentMode = PlayerMode.Normal;
                break;

            case 1:
                currentMode = PlayerMode.Build;
                break;

            case 2:
                currentMode = PlayerMode.Remove;
                break;
        }

        UpdateModeText();

        Debug.Log(
            "Current Player Mode: " +
            currentMode
        );
    }

    // =========================
    // HIGHLIGHT VISUALS
    // =========================

    private void UpdateVisuals()
    {
        if (normalImage != null)
        {
            normalImage.rectTransform.localScale =
                highlightedOption == 0
                ? Vector3.one * selectedScale
                : Vector3.one * normalScale;
        }

        if (buildImage != null)
        {
            buildImage.rectTransform.localScale =
                highlightedOption == 1
                ? Vector3.one * selectedScale
                : Vector3.one * normalScale;
        }

        if (removeImage != null)
        {
            removeImage.rectTransform.localScale =
                highlightedOption == 2
                ? Vector3.one * selectedScale
                : Vector3.one * normalScale;
        }
    }

    // =========================
    // HIGHLIGHT TEXT
    // =========================

    private void UpdateHighlightedText()
    {
        if (modeText == null)
        {
            return;
        }

        switch (highlightedOption)
        {
            case 0:
                modeText.text = "Inspector Mode";
                break;

            case 1:
                modeText.text = "Build Mode";
                break;

            case 2:
                modeText.text = "Remove Mode";
                break;

            default:
                UpdateModeText();
                break;
        }
    }

    // =========================
    // CURRENT MODE TEXT
    // =========================

    private void UpdateModeText()
    {
        if (modeText == null)
        {
            return;
        }

        switch (currentMode)
        {
            case PlayerMode.Normal:
                modeText.text = "Inspector Mode";
                break;

            case PlayerMode.Build:
                modeText.text = "Build Mode";
                break;

            case PlayerMode.Remove:
                modeText.text = "Remove Mode";
                break;
        }
    }

    // =========================
    // PUBLIC MODE CHECKS
    // =========================

    public bool IsNormalMode()
    {
        return currentMode == PlayerMode.Normal;
    }

    public bool IsBuildMode()
    {
        return currentMode == PlayerMode.Build;
    }

    public bool IsRemoveMode()
    {
        return currentMode == PlayerMode.Remove;
    }

    public bool IsWheelOpen()
    {
        return wheelOpen;
    }

    public void SetMode(PlayerMode newMode)
    {
        currentMode = newMode;
        UpdateModeText();
    }
}