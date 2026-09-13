using UnityEngine;

public class FarmPlotSoundEffects : MonoBehaviour
{
    // =========================================================
    // AUDIO SOURCE
    // =========================================================

    [Header("Audio Source")]
    [SerializeField] private AudioSource audioSource;

    // =========================================================
    // BUILD
    // =========================================================

    [Header("Build Sounds")]
    [Tooltip("Random sound played when this farm plot is built.")]
    [SerializeField]
    private AudioClip[] buildSounds =
        new AudioClip[3];

    [Range(0f, 1f)]
    [SerializeField] private float buildVolume = 1f;

    // =========================================================
    // REMOVE
    // =========================================================

    [Header("Remove Sounds")]
    [Tooltip("Random sound played when the player starts removing this plot.")]
    [SerializeField]
    private AudioClip[] removeSounds =
        new AudioClip[3];

    [Range(0f, 1f)]
    [SerializeField] private float removeVolume = 1f;

    // =========================================================
    // BREAKING
    // =========================================================

    [Header("Breaking Sounds")]
    [Tooltip("Random sound played when the farm plot actually breaks.")]
    [SerializeField]
    private AudioClip[] breakingSounds =
        new AudioClip[3];

    [Range(0f, 1f)]
    [SerializeField] private float breakingVolume = 1f;

    // =========================================================
    // INSPECT
    // =========================================================

    [Header("Inspect Sounds")]
    [Tooltip("Random sound played when inspecting the farm plot.")]
    [SerializeField]
    private AudioClip[] inspectSounds =
        new AudioClip[3];

    [Range(0f, 1f)]
    [SerializeField] private float inspectVolume = 0.8f;

    // =========================================================
    // PITCH
    // =========================================================

    [Header("Pitch Variation")]

    [Tooltip("Minimum random pitch.")]
    [SerializeField] private float minimumPitch = 0.95f;

    [Tooltip("Maximum random pitch.")]
    [SerializeField] private float maximumPitch = 1.05f;

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
        if (audioSource == null)
        {
            audioSource =
                GetComponent<AudioSource>();
        }
    }

    // =========================================================
    // BUILD
    // =========================================================

    public void PlayBuildSound()
    {
        PlayRandomSound(
            buildSounds,
            buildVolume,
            "Build"
        );
    }

    // =========================================================
    // REMOVE
    // =========================================================

    public void PlayRemoveSound()
    {
        PlayRandomSound(
            removeSounds,
            removeVolume,
            "Remove"
        );
    }

    // =========================================================
    // BREAK
    // =========================================================

    public void PlayBreakingSound()
    {
        PlayRandomSound(
            breakingSounds,
            breakingVolume,
            "Breaking"
        );
    }

    // =========================================================
    // INSPECT
    // =========================================================

    public void PlayInspectSound()
    {
        PlayRandomSound(
            inspectSounds,
            inspectVolume,
            "Inspect"
        );
    }

    // =========================================================
    // RANDOM SOUND
    // =========================================================

    private void PlayRandomSound(
        AudioClip[] sounds,
        float volume,
        string soundName)
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

        int randomSound =
            Random.Range(
                0,
                validSoundCount
            );

        AudioClip selectedClip = null;

        int currentValidSound = 0;

        for (int i = 0;
             i < sounds.Length;
             i++)
        {
            if (sounds[i] == null)
            {
                continue;
            }

            if (currentValidSound ==
                randomSound)
            {
                selectedClip =
                    sounds[i];

                break;
            }

            currentValidSound++;
        }

        if (selectedClip == null)
        {
            return;
        }

        float originalPitch =
            audioSource.pitch;

        audioSource.pitch =
            Random.Range(
                minimumPitch,
                maximumPitch
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
                "Farm Plot Sound: " +
                soundName +
                " - " +
                selectedClip.name
            );
        }
    }

    // =========================================================
    // DEBUG TESTS
    // =========================================================

    [ContextMenu("Sound - Test Build")]
    private void DebugBuildSound()
    {
        PlayBuildSound();
    }

    [ContextMenu("Sound - Test Remove")]
    private void DebugRemoveSound()
    {
        PlayRemoveSound();
    }

    [ContextMenu("Sound - Test Breaking")]
    private void DebugBreakingSound()
    {
        PlayBreakingSound();
    }

    [ContextMenu("Sound - Test Inspect")]
    private void DebugInspectSound()
    {
        PlayInspectSound();
    }

    // =========================================================
    // DISABLE
    // =========================================================

    private void OnDisable()
    {
        if (audioSource != null)
        {
            audioSource.pitch = 1f;
        }
    }
}