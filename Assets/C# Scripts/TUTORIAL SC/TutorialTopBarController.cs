using UnityEngine;
using UnityEngine.UI;

public class TutorialEndDayButtonController : MonoBehaviour
{
    [Header("End Day Button")]
    [Tooltip("Drag the End Day Button here.")]
    [SerializeField] private Button endDayButton;

    [Header("Starting State")]
    [Tooltip("Keeps the End Day button disabled until the tutorial enables it.")]
    [SerializeField] private bool disableOnStart = true;

    private void Awake()
    {
        if (endDayButton != null && disableOnStart)
            endDayButton.interactable = false;
    }

    public void EnableEndDayButton()
    {
        if (endDayButton != null)
            endDayButton.interactable = true;
    }

    public void DisableEndDayButton()
    {
        if (endDayButton != null)
            endDayButton.interactable = false;
    }

    public void SetEndDayButtonEnabled(bool enabled)
    {
        if (endDayButton != null)
            endDayButton.interactable = enabled;
    }

    public bool IsEndDayButtonEnabled()
    {
        return endDayButton != null && endDayButton.interactable;
    }
}
