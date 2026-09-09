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
    // AUDIO SOURCE
    // =========================================================

    [Header("Inspection Sounds")]

    [Tooltip(
        "Audio Source used for inspection hover and click sounds."
    )]
    [SerializeField] private AudioSource audioSource;

    // =========================================================
    // HOVER AUDIO
    // =========================================================

    [Header("Hover Sounds")]

    [Tooltip(
        "Random sound played when hovering over an inspectable object."
    )]
    [SerializeField]
    private AudioClip[] hoverSounds =
        new AudioClip[3];

    [Range(0f, 1f)]
    [SerializeField] private float hoverVolume = 0.65f;

    [SerializeField] private float hoverPitchMin = 0.95f;

    [SerializeField] private float hoverPitchMax = 1.05f;

    // =========================================================
    // CLICK AUDIO
    // =========================================================

    [Header("Inspect / Click Sounds")]

    [Tooltip(
        "Random sound played when an inspection panel is opened."
    )]
    [SerializeField]
    private AudioClip[] clickSounds =
        new AudioClip[3];

    [Range(0f, 1f)]
    [SerializeField] private float clickVolume = 1f;

    [SerializeField] private float clickPitchMin = 0.95f;

    [SerializeField] private float clickPitchMax = 1.05f;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

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

        // Automatically find the AudioSource
        // on this GameObject if one has not
        // been manually assigned.

        if (audioSource == null)
        {
            audioSource =
                GetComponent<AudioSource>();
        }
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
        // we don't need to close or reopen it.

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

        // =====================================================
        // INSPECTION CLICK SOUND
        // =====================================================

        PlayClickSound();
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
    // HOVER SOUND
    // =========================================================

    public void PlayHoverSound()
    {
        PlayRandomSound(
            hoverSounds,
            hoverVolume,
            hoverPitchMin,
            hoverPitchMax,
            "Inspection Hover"
        );
    }

    // =========================================================
    // CLICK SOUND
    // =========================================================

    public void PlayClickSound()
    {
        PlayRandomSound(
            clickSounds,
            clickVolume,
            clickPitchMin,
            clickPitchMax,
            "Inspection Click"
        );
    }

    // =========================================================
    // RANDOM SOUND
    // =========================================================

    private void PlayRandomSound(
        AudioClip[] sounds,
        float volume,
        float pitchMin,
        float pitchMax,
        string soundType)
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

        // Count only valid clips.
        int validClipCount = 0;

        for (int i = 0;
             i < sounds.Length;
             i++)
        {
            if (sounds[i] != null)
            {
                validClipCount++;
            }
        }

        if (validClipCount <= 0)
        {
            return;
        }

        // Pick one of the valid clips.
        int randomValidIndex =
            Random.Range(
                0,
                validClipCount
            );

        AudioClip selectedClip = null;

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
                pitchMin,
                pitchMax
            );

        audioSource.PlayOneShot(
            selectedClip,
            volume
        );

        audioSource.pitch =
            originalPitch;

        if (showDebugLogs)
        {
            Debug.Log(
                soundType +
                " Sound: " +
                selectedClip.name
            );
        }
    }

    // =========================================================
    // DEBUG SOUND TESTS
    // =========================================================

    [ContextMenu("Sound - Test Inspection Hover")]
    private void DebugHoverSound()
    {
        PlayHoverSound();
    }

    [ContextMenu("Sound - Test Inspection Click")]
    private void DebugClickSound()
    {
        PlayClickSound();
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