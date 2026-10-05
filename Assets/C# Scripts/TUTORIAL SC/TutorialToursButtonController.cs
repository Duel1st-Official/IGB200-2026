using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tutorial-only controller for the Tours "Start Tour" button.
/// Keeps the button disabled until TutorialManager reaches the
/// CONSERVATION TOURS step.
///
/// Put this on any active GameObject in the Tutorial scene and
/// assign the Start Tour Button.
/// </summary>
public class TutorialToursButtonController : MonoBehaviour
{
    [Header("Tours Button")]
    [SerializeField] private Button startTourButton;

    [Header("Tutorial State")]
    [Tooltip("If enabled, the Start Tour button begins disabled.")]
    [SerializeField] private bool startLocked = true;

    private bool tutorialUnlocked;

    private void Awake()
    {
        tutorialUnlocked = !startLocked;
        ApplyState();
    }

    private void Start()
    {
        ApplyState();
    }

    private void LateUpdate()
    {
        // Important: ToursBuildingInspectionUI refreshes its button state
        // while the panel is open. LateUpdate makes this controller the
        // final authority during the tutorial, just like the End Day lock.
        if (!tutorialUnlocked)
            ApplyState();
    }

    public void EnableToursButton()
    {
        tutorialUnlocked = true;
        ApplyState();
    }

    public void DisableToursButton()
    {
        tutorialUnlocked = false;
        ApplyState();
    }

    public bool IsToursButtonUnlocked()
    {
        return tutorialUnlocked;
    }

    private void ApplyState()
    {
        if (startTourButton == null)
            return;

        if (!tutorialUnlocked)
            startTourButton.interactable = false;
    }
}
