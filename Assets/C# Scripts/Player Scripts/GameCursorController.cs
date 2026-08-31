using UnityEngine;

public class GameCursorController : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private SelectionWheel selectionWheel;

    // =========================================================
    // SETTINGS
    // =========================================================

    [Header("Cursor Settings")]
    [SerializeField] private bool hideInBuildMode = true;
    [SerializeField] private bool hideInRemoveMode = true;

    // =========================================================
    // PRIVATE
    // =========================================================

    private bool cursorCurrentlyVisible = true;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // Auto find SelectionWheel
        if (selectionWheel == null)
        {
            selectionWheel =
                FindFirstObjectByType<SelectionWheel>();
        }

        // Start with cursor visible
        SetCursorVisible(true);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (selectionWheel == null)
        {
            SetCursorVisible(true);
            return;
        }

        // =====================================================
        // WHEEL OPEN
        // =====================================================
        // Always show the cursor while choosing a mode.
        // =====================================================

        if (selectionWheel.IsWheelOpen())
        {
            SetCursorVisible(true);
            return;
        }

        // =====================================================
        // BUILD MODE
        // =====================================================

        if (hideInBuildMode &&
            selectionWheel.IsBuildMode())
        {
            SetCursorVisible(false);
            return;
        }

        // =====================================================
        // REMOVE MODE
        // =====================================================

        if (hideInRemoveMode &&
            selectionWheel.IsRemoveMode())
        {
            SetCursorVisible(false);
            return;
        }

        // =====================================================
        // NORMAL MODE
        // =====================================================

        SetCursorVisible(true);
    }

    // =========================================================
    // SET CURSOR
    // =========================================================

    private void SetCursorVisible(bool visible)
    {
        if (cursorCurrentlyVisible == visible)
        {
            return;
        }

        cursorCurrentlyVisible = visible;

        Cursor.visible = visible;

        // Keep cursor free to move.
        Cursor.lockState =
            CursorLockMode.None;
    }

    // =========================================================
    // APPLICATION FOCUS
    // =========================================================

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            return;
        }

        RefreshCursor();
    }

    // =========================================================
    // REFRESH
    // =========================================================

    private void RefreshCursor()
    {
        if (selectionWheel == null)
        {
            SetCursorVisible(true);
            return;
        }

        if (selectionWheel.IsWheelOpen())
        {
            SetCursorVisible(true);
            return;
        }

        bool shouldHide =
            (hideInBuildMode &&
             selectionWheel.IsBuildMode()) ||

            (hideInRemoveMode &&
             selectionWheel.IsRemoveMode());

        SetCursorVisible(
            !shouldHide
        );
    }

    // =========================================================
    // DISABLE
    // =========================================================

    private void OnDisable()
    {
        // Never leave the system cursor hidden
        // if this component gets disabled.

        Cursor.visible = true;
        Cursor.lockState =
            CursorLockMode.None;

        cursorCurrentlyVisible = true;
    }
}