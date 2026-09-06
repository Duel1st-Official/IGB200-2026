using UnityEngine;

public class InspectionUIManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    public static InspectionUIManager Instance
    {
        get;
        private set;
    }

    // =========================================================
    // CURRENT PANEL
    // =========================================================

    private IInspectionPanel currentPanel;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // Only allow one InspectionUIManager
        // in the scene.

        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // =========================================================
    // OPEN PANEL
    // =========================================================

    public void OpenPanel(
        IInspectionPanel newPanel)
    {
        if (newPanel == null)
        {
            return;
        }

        // =====================================================
        // SAME PANEL
        // =====================================================

        // If this is already the current panel,
        // we don't need to close it.

        if (currentPanel == newPanel)
        {
            return;
        }

        // =====================================================
        // CLOSE OLD PANEL
        // =====================================================

        if (currentPanel != null)
        {
            currentPanel.CloseImmediately();
        }

        // =====================================================
        // SET NEW PANEL
        // =====================================================

        currentPanel =
            newPanel;
    }

    // =========================================================
    // CLEAR PANEL
    // =========================================================

    public void ClearPanel(
        IInspectionPanel panel)
    {
        if (currentPanel ==
            panel)
        {
            currentPanel =
                null;
        }
    }

    // =========================================================
    // CLOSE CURRENT
    // =========================================================

    public void CloseCurrentPanel()
    {
        if (currentPanel == null)
        {
            return;
        }

        IInspectionPanel panelToClose =
            currentPanel;

        currentPanel =
            null;

        panelToClose.CloseImmediately();
    }

    // =========================================================
    // HAS PANEL
    // =========================================================

    public bool HasOpenPanel()
    {
        return
            currentPanel != null;
    }

    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance =
                null;
        }
    }
}