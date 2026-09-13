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
    // NORMAL HOVER AUDIO
    // =========================================================

    [Header("Normal Hover Sounds")]

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
    // NORMAL INSPECTION AUDIO
    // =========================================================

    [Header("Normal Inspect / Click Sounds")]

    [Tooltip(
        "Used for normal inspectable objects such as plots, traps and water plots."
    )]
    [SerializeField]
    private AudioClip[] clickSounds =
        new AudioClip[3];

    [Range(0f, 1f)]
    [SerializeField] private float clickVolume = 1f;

    [SerializeField] private float clickPitchMin = 0.95f;
    [SerializeField] private float clickPitchMax = 1.05f;

    // =========================================================
    // CAVE INSPECTION AUDIO
    // =========================================================

    [Header("Cave Inspection Sounds")]

    [Tooltip(
        "Unique sounds played when opening the Cave inspection panel."
    )]
    [SerializeField]
    private AudioClip[] caveInspectSounds =
        new AudioClip[3];

    [Range(0f, 1f)]
    [SerializeField] private float caveInspectVolume = 1f;

    [SerializeField] private float caveInspectPitchMin = 0.95f;
    [SerializeField] private float caveInspectPitchMax = 1.05f;

    // =========================================================
    // TOURS BUILDING INSPECTION AUDIO
    // =========================================================

    [Header("Tours Building Inspection Sounds")]

    [Tooltip(
        "Unique sounds played when opening the Tours Building inspection panel."
    )]
    [SerializeField]
    private AudioClip[] toursBuildingInspectSounds =
        new AudioClip[3];

    [Range(0f, 1f)]
    [SerializeField] private float toursBuildingInspectVolume = 1f;

    [SerializeField] private float toursBuildingInspectPitchMin = 0.95f;
    [SerializeField] private float toursBuildingInspectPitchMax = 1.05f;

    // =========================================================
    // RANGER STATION INSPECTION AUDIO
    // =========================================================

    [Header("Ranger Station Inspection Sounds")]

    [Tooltip(
        "Unique sounds played when opening the Ranger Station inspection panel."
    )]
    [SerializeField]
    private AudioClip[] rangerStationInspectSounds =
        new AudioClip[3];

    [Range(0f, 1f)]
    [SerializeField] private float rangerStationInspectVolume = 1f;

    [SerializeField] private float rangerStationInspectPitchMin = 0.95f;
    [SerializeField] private float rangerStationInspectPitchMax = 1.05f;

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

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
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
        // PLAY CORRECT INSPECTION SOUND
        // =====================================================

        PlayInspectionSound(
            newPanel
        );
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
    // NORMAL CLICK SOUND
    // =========================================================

    public void PlayClickSound()
    {
        PlayRandomSound(
            clickSounds,
            clickVolume,
            clickPitchMin,
            clickPitchMax,
            "Normal Inspection"
        );
    }

    // =========================================================
    // DETERMINE INSPECTION SOUND
    // =========================================================

    private void PlayInspectionSound(
        IInspectionPanel panel)
    {
        if (panel == null)
        {
            return;
        }

        string panelTypeName =
            panel.GetType().Name;

        // =====================================================
        // CAVE
        // =====================================================

        if (panelTypeName ==
            "CaveInspectionUI")
        {
            PlayCaveInspectSound();
            return;
        }

        // =====================================================
        // RANGER STATION
        // =====================================================

        if (panelTypeName ==
            "RangerStationInspectionUI")
        {
            PlayRangerStationInspectSound();
            return;
        }

        // =====================================================
        // TOURS BUILDING
        // =====================================================

        // Supports either naming convention in case
        // your Tours UI script uses either name.

        if (panelTypeName ==
                "ToursBuildingInspectionUI" ||
            panelTypeName ==
                "ToursInspectionUI")
        {
            PlayToursBuildingInspectSound();
            return;
        }

        // =====================================================
        // EVERYTHING ELSE
        // =====================================================

        PlayClickSound();
    }

    // =========================================================
    // CAVE SOUND
    // =========================================================

    public void PlayCaveInspectSound()
    {
        PlayRandomSound(
            caveInspectSounds,
            caveInspectVolume,
            caveInspectPitchMin,
            caveInspectPitchMax,
            "Cave Inspection"
        );
    }

    // =========================================================
    // TOURS BUILDING SOUND
    // =========================================================

    public void PlayToursBuildingInspectSound()
    {
        PlayRandomSound(
            toursBuildingInspectSounds,
            toursBuildingInspectVolume,
            toursBuildingInspectPitchMin,
            toursBuildingInspectPitchMax,
            "Tours Building Inspection"
        );
    }

    // =========================================================
    // RANGER STATION SOUND
    // =========================================================

    public void PlayRangerStationInspectSound()
    {
        PlayRandomSound(
            rangerStationInspectSounds,
            rangerStationInspectVolume,
            rangerStationInspectPitchMin,
            rangerStationInspectPitchMax,
            "Ranger Station Inspection"
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

        // =====================================================
        // COUNT VALID CLIPS
        // =====================================================

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

        // =====================================================
        // RANDOM VALID CLIP
        // =====================================================

        int randomValidIndex =
            Random.Range(
                0,
                validClipCount
            );

        AudioClip selectedClip =
            null;

        int currentValidIndex =
            0;

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

        // =====================================================
        // PITCH
        // =====================================================

        audioSource.pitch =
            Random.Range(
                pitchMin,
                pitchMax
            );

        // =====================================================
        // PLAY
        // =====================================================

        audioSource.PlayOneShot(
            selectedClip,
            volume
        );

        // =====================================================
        // DEBUG
        // =====================================================

        if (showDebugLogs)
        {
            Debug.Log(
                soundType +
                " Sound: " +
                selectedClip.name +
                " | Panel: " +
                (
                    currentPanel != null
                    ? currentPanel.GetType().Name
                    : "None"
                )
            );
        }
    }

    // =========================================================
    // DEBUG SOUND TESTS
    // =========================================================

    [ContextMenu("Sound - Test Normal Hover")]
    private void DebugHoverSound()
    {
        PlayHoverSound();
    }

    [ContextMenu("Sound - Test Normal Inspection")]
    private void DebugClickSound()
    {
        PlayClickSound();
    }

    [ContextMenu("Sound - Test Cave Inspection")]
    private void DebugCaveSound()
    {
        PlayCaveInspectSound();
    }

    [ContextMenu("Sound - Test Tours Building Inspection")]
    private void DebugToursBuildingSound()
    {
        PlayToursBuildingInspectSound();
    }

    [ContextMenu("Sound - Test Ranger Station Inspection")]
    private void DebugRangerStationSound()
    {
        PlayRangerStationInspectSound();
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