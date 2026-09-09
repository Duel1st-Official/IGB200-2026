using UnityEngine;

public class PlayerFootsteps : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;

    [Header("Footstep Sounds")]
    [Tooltip("Drag multiple walking sounds here for variation.")]
    [SerializeField] private AudioClip[] footstepSounds;

    [Header("Walking")]
    [Tooltip("Time between each footstep while moving.")]
    [SerializeField] private float stepInterval = 0.35f;

    [Tooltip("Minimum movement needed before footsteps play.")]
    [SerializeField] private float movementThreshold = 0.05f;

    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float minimumVolume = 0.75f;

    [Range(0f, 1f)]
    [SerializeField] private float maximumVolume = 1f;

    [Header("Pitch Variation")]
    [SerializeField] private float minimumPitch = 0.9f;

    [SerializeField] private float maximumPitch = 1.1f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    private Vector3 previousPosition;

    private float stepTimer;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (audioSource == null)
        {
            audioSource =
                GetComponent<AudioSource>();
        }

        previousPosition =
            transform.position;
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        Vector3 movement =
            transform.position -
            previousPosition;

        float movementAmount =
            movement.magnitude;

        bool isMoving =
            movementAmount >
            movementThreshold *
            Time.deltaTime;

        if (isMoving)
        {
            stepTimer -=
                Time.deltaTime;

            if (stepTimer <= 0f)
            {
                PlayFootstep();

                stepTimer =
                    stepInterval;
            }
        }
        else
        {
            // Reset slightly so the first step happens
            // quickly when the player starts moving again.
            stepTimer =
                Mathf.Min(
                    stepTimer,
                    0.1f
                );
        }

        previousPosition =
            transform.position;
    }

    // =========================================================
    // PLAY FOOTSTEP
    // =========================================================

    private void PlayFootstep()
    {
        if (audioSource == null)
        {
            return;
        }

        if (footstepSounds == null ||
            footstepSounds.Length == 0)
        {
            return;
        }

        AudioClip selectedClip =
            footstepSounds[
                Random.Range(
                    0,
                    footstepSounds.Length
                )
            ];

        if (selectedClip == null)
        {
            return;
        }

        audioSource.pitch =
            Random.Range(
                minimumPitch,
                maximumPitch
            );

        float randomVolume =
            Random.Range(
                minimumVolume,
                maximumVolume
            );

        audioSource.PlayOneShot(
            selectedClip,
            randomVolume
        );

        if (showDebugLogs)
        {
            Debug.Log(
                "Footstep: " +
                selectedClip.name
            );
        }
    }

    // =========================================================
    // DISABLE
    // =========================================================

    private void OnDisable()
    {
        stepTimer =
            0f;

        if (audioSource != null)
        {
            audioSource.pitch =
                1f;
        }
    }
}