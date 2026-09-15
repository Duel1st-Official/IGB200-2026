using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class MammalPickup : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    [SerializeField]
    private SelectionWheel selectionWheel;

    [SerializeField]
    private Camera mainCamera;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private Transform player;

    [Tooltip(
        "Crop Plot that spawned this mammal. " +
        "This is automatically assigned."
    )]
    [SerializeField]
    private CropPlot cropOwner;

    // =========================================================
    // MAMMAL
    // =========================================================

    [Header("Mammal")]

    [SerializeField]
    private string mammalName = "Mammal";

    // =========================================================
    // INTERACTION
    // =========================================================

    [Header("Interaction")]

    [Tooltip(
        "Maximum distance from the player that the mammal can be collected."
    )]
    [SerializeField]
    private float pickupDistance = 3f;

    [Tooltip(
        "Allow the mammal to be collected by left clicking it."
    )]
    [SerializeField]
    private bool allowClickPickup = true;

    // =========================================================
    // HOVER
    // =========================================================

    [Header("Hover")]

    [SerializeField]
    private Material hoverMaterial;

    // =========================================================
    // IDLE BOB
    // =========================================================

    [Header("Idle Bob")]

    [Tooltip(
        "Makes the mammal slowly float up and down while waiting."
    )]
    [SerializeField]
    private bool enableIdleBob = true;

    [Tooltip(
        "How far the mammal moves vertically."
    )]
    [SerializeField]
    private float bobHeight = 0.12f;

    [Tooltip(
        "How quickly the mammal bobs."
    )]
    [SerializeField]
    private float bobSpeed = 1.5f;

    [Tooltip(
        "Offsets mammals so they do not all bob together."
    )]
    [SerializeField]
    private bool randomizeBobPhase = true;

    // =========================================================
    // COLLECTION ANIMATION
    // =========================================================

    [Header("Collection Animation")]

    [Tooltip(
        "How long the pickup animation takes."
    )]
    [SerializeField]
    private float collectDuration = 0.45f;

    [Tooltip(
        "How far upward the mammal travels when collected."
    )]
    [SerializeField]
    private float collectRiseDistance = 0.7f;

    [Tooltip(
        "Final scale multiplier at the end of collection."
    )]
    [Range(0f, 1f)]
    [SerializeField]
    private float collectEndScale = 0.25f;

    [Tooltip(
        "Fade the mammal while being collected."
    )]
    [SerializeField]
    private bool fadeOnCollect = true;

    // =========================================================
    // COLLECTION AUDIO
    // =========================================================

    [Header("Collection Audio")]

    [Tooltip(
        "Random sound played when the mammal is collected."
    )]
    [SerializeField]
    private AudioClip[] collectSounds =
        new AudioClip[3];

    [Range(0f, 1f)]
    [SerializeField]
    private float collectSoundVolume = 1f;

    [SerializeField]
    private float collectPitchMin = 0.95f;

    [SerializeField]
    private float collectPitchMax = 1.05f;

    // =========================================================
    // EVENTS
    // =========================================================

    [Header("Events")]

    [SerializeField]
    private UnityEvent onCollected;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLogs = false;

    // =========================================================
    // PRIVATE
    // =========================================================

    private Material normalMaterial;

    private Collider2D mammalCollider;

    private bool collected = false;

    private bool isHovered = false;

    private Vector3 baseLocalPosition;

    private Vector3 originalScale;

    private float bobPhase;

    private Color originalColor;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        AutoAssignReferences();

        // =====================================================
        // MATERIAL
        // =====================================================

        if (spriteRenderer != null)
        {
            normalMaterial =
                spriteRenderer.material;

            originalColor =
                spriteRenderer.color;
        }

        // =====================================================
        // COLLIDER
        // =====================================================

        mammalCollider =
            GetComponent<Collider2D>();

        if (mammalCollider == null)
        {
            mammalCollider =
                GetComponentInChildren<Collider2D>();
        }

        // =====================================================
        // BOB
        // =====================================================

        baseLocalPosition =
            transform.localPosition;

        originalScale =
            transform.localScale;

        if (randomizeBobPhase)
        {
            bobPhase =
                Random.Range(
                    0f,
                    Mathf.PI * 2f
                );
        }
        else
        {
            bobPhase = 0f;
        }
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        AutoAssignReferences();

        // =====================================================
        // IMPORTANT
        //
        // The mammal has now finished being instantiated by
        // CropPlot, so search for its owner again here.
        // =====================================================

        AutoFindCropOwner();

        if (showDebugLogs)
        {
            Debug.Log(
                "[MammalPickup] " +
                mammalName +
                " started." +
                "\nCrop Owner = " +
                (
                    cropOwner != null
                        ? cropOwner.name
                        : "NOT FOUND"
                )
            );
        }
    }

    // =========================================================
    // AUTO ASSIGN
    // =========================================================

    private void AutoAssignReferences()
    {
        // =====================================================
        // CAMERA
        // =====================================================

        if (mainCamera == null)
        {
            mainCamera =
                Camera.main;
        }

        // =====================================================
        // SPRITE RENDERER
        // =====================================================

        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                spriteRenderer =
                    GetComponentInChildren<SpriteRenderer>();
            }
        }

        // =====================================================
        // SELECTION WHEEL
        // =====================================================

        if (selectionWheel == null)
        {
            selectionWheel =
                FindFirstObjectByType<SelectionWheel>(
                    FindObjectsInactive.Include
                );
        }

        // =====================================================
        // PLAYER
        // =====================================================

        if (player == null)
        {
            TryFindPlayer();
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (collected)
        {
            return;
        }

        // =====================================================
        // IDLE BOB
        // =====================================================

        UpdateIdleBob();

        // =====================================================
        // NORMAL MODE ONLY
        // =====================================================

        if (selectionWheel != null)
        {
            if (!selectionWheel.IsNormalMode())
            {
                SetHovered(
                    false
                );

                return;
            }

            if (selectionWheel.IsWheelOpen())
            {
                SetHovered(
                    false
                );

                return;
            }
        }

        // =====================================================
        // UI BLOCKING
        // =====================================================

        if (IsPointerOverUI())
        {
            SetHovered(
                false
            );

            return;
        }

        // =====================================================
        // CAMERA
        // =====================================================

        if (mainCamera == null)
        {
            mainCamera =
                Camera.main;

            if (mainCamera == null)
            {
                return;
            }
        }

        // =====================================================
        // PLAYER
        // =====================================================

        if (player == null)
        {
            TryFindPlayer();
        }

        // =====================================================
        // MOUSE
        // =====================================================

        Vector3 mouseWorld =
            mainCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        mouseWorld.z =
            transform.position.z;

        // =====================================================
        // HOVER
        // =====================================================

        bool hovering =
            IsMouseOverMammal(
                mouseWorld
            );

        bool inRange =
            IsPlayerInRange();

        SetHovered(
            hovering &&
            inRange
        );

        // =====================================================
        // PICKUP
        // =====================================================

        if (allowClickPickup &&
            isHovered &&
            Input.GetMouseButtonDown(0))
        {
            Collect();
        }
    }

    // =========================================================
    // PLAYER
    // =========================================================

    private void TryFindPlayer()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (playerObject != null)
        {
            player =
                playerObject.transform;
        }
    }

    // =========================================================
    // AUTO FIND CROP OWNER
    // =========================================================

    private void AutoFindCropOwner()
    {
        // Already assigned by CropPlot.
        if (cropOwner != null)
        {
            return;
        }

        // =====================================================
        // FIRST TRY PARENT
        // =====================================================

        cropOwner =
            GetComponentInParent<CropPlot>();

        if (cropOwner != null)
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    "[MammalPickup] Found CropPlot through parent: " +
                    cropOwner.name
                );
            }

            return;
        }

        // =====================================================
        // FIND ALL FARM PLOTS
        //
        // CropPlot stores a reference to the mammal that it
        // spawned. We can compare that reference against this
        // MammalPickup.
        // =====================================================

        CropPlot[] cropPlots =
            FindObjectsByType<CropPlot>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (CropPlot candidate in cropPlots)
        {
            if (candidate == null)
            {
                continue;
            }

            GameObject candidateMammal =
                candidate.GetAttractedMammal();

            if (candidateMammal == null)
            {
                continue;
            }

            // =================================================
            // MAMMAL PICKUP IS ON ROOT
            // =================================================

            if (candidateMammal ==
                gameObject)
            {
                cropOwner =
                    candidate;

                break;
            }

            // =================================================
            // MAMMAL PICKUP IS ON CHILD
            // =================================================

            if (transform.IsChildOf(
                candidateMammal.transform
            ))
            {
                cropOwner =
                    candidate;

                break;
            }

            // =================================================
            // MAMMAL ROOT MAY BE A CHILD
            // =================================================

            if (candidateMammal.transform.IsChildOf(
                transform
            ))
            {
                cropOwner =
                    candidate;

                break;
            }
        }

        // =====================================================
        // DEBUG
        // =====================================================

        if (showDebugLogs)
        {
            if (cropOwner != null)
            {
                Debug.Log(
                    "[MammalPickup] Automatically found Crop Owner: " +
                    cropOwner.name
                );
            }
            else
            {
                Debug.LogWarning(
                    "[MammalPickup] Could not automatically find " +
                    "the CropPlot that spawned " +
                    mammalName +
                    "."
                );
            }
        }
    }

    // =========================================================
    // SET CROP OWNER
    // =========================================================

    public void SetCropOwner(
        CropPlot owner)
    {
        cropOwner =
            owner;

        if (showDebugLogs)
        {
            Debug.Log(
                "[MammalPickup] Crop Owner assigned directly: " +
                (
                    cropOwner != null
                        ? cropOwner.name
                        : "NULL"
                )
            );
        }
    }

    // =========================================================
    // IDLE BOB
    // =========================================================

    private void UpdateIdleBob()
    {
        if (!enableIdleBob)
        {
            transform.localPosition =
                baseLocalPosition;

            return;
        }

        float bob =
            Mathf.Sin(
                Time.time *
                bobSpeed +
                bobPhase
            ) *
            bobHeight;

        Vector3 targetPosition =
            baseLocalPosition +
            Vector3.up *
            bob;

        transform.localPosition =
            targetPosition;
    }

    // =========================================================
    // MOUSE OVER
    // =========================================================

    private bool IsMouseOverMammal(
        Vector3 mouseWorld)
    {
        if (spriteRenderer == null)
        {
            return false;
        }

        return
            spriteRenderer.bounds.Contains(
                mouseWorld
            );
    }

    // =========================================================
    // PLAYER RANGE
    // =========================================================

    private bool IsPlayerInRange()
    {
        if (player == null)
        {
            return false;
        }

        float distance =
            Vector2.Distance(
                player.position,
                transform.position
            );

        return
            distance <=
            pickupDistance;
    }

    // =========================================================
    // HOVER
    // =========================================================

    private void SetHovered(
        bool value)
    {
        if (isHovered ==
            value)
        {
            return;
        }

        isHovered =
            value;

        if (spriteRenderer == null)
        {
            return;
        }

        if (isHovered &&
            hoverMaterial != null)
        {
            spriteRenderer.material =
                hoverMaterial;
        }
        else if (normalMaterial != null)
        {
            spriteRenderer.material =
                normalMaterial;
        }
    }

    // =========================================================
    // COLLECT
    // =========================================================

    public void Collect()
    {
        if (collected)
        {
            return;
        }

        // =====================================================
        // IMPORTANT:
        // FIND CROP OWNER BEFORE DOING ANYTHING ELSE
        // =====================================================

        if (cropOwner == null)
        {
            AutoFindCropOwner();
        }

        collected =
            true;

        // =====================================================
        // STOP HOVER
        // =====================================================

        isHovered =
            false;

        if (spriteRenderer != null &&
            normalMaterial != null)
        {
            spriteRenderer.material =
                normalMaterial;
        }

        // =====================================================
        // DISABLE COLLIDER
        // =====================================================

        if (mammalCollider != null)
        {
            mammalCollider.enabled =
                false;
        }

        // =====================================================
        // AUDIO
        // =====================================================

        PlayCollectSound();

        // =====================================================
        // EVENT
        // =====================================================

        onCollected?.Invoke();

        // =====================================================
        // TELL CROP
        //
        // THIS IS WHAT ACTUALLY CAUSES:
        //
        // Mammal collected
        //       ↓
        // CropPlot.MammalCollected()
        //       ↓
        // BatColony.AddBatFood()
        //       ↓
        // Food slider increases
        // =====================================================

        if (cropOwner != null)
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    "[MammalPickup] Sending collection to CropPlot: " +
                    cropOwner.name
                );
            }

            cropOwner.MammalCollected(
                this
            );
        }
        else
        {
            Debug.LogWarning(
                "[MammalPickup] " +
                mammalName +
                " was collected, but its CropPlot owner " +
                "could not be found. No Bat Food was awarded."
            );
        }

        // =====================================================
        // DEBUG
        // =====================================================

        if (showDebugLogs)
        {
            Debug.Log(
                "Collected mammal: " +
                mammalName
            );
        }

        // =====================================================
        // COLLECTION ANIMATION
        // =====================================================

        StartCoroutine(
            CollectAnimation()
        );
    }

    // =========================================================
    // COLLECTION ANIMATION
    // =========================================================

    private IEnumerator CollectAnimation()
    {
        float duration =
            Mathf.Max(
                0.01f,
                collectDuration
            );

        float timer =
            0f;

        // Keep current bob position.
        Vector3 startPosition =
            transform.position;

        Vector3 targetPosition =
            startPosition +
            Vector3.up *
            collectRiseDistance;

        Vector3 startScale =
            transform.localScale;

        Vector3 targetScale =
            originalScale *
            collectEndScale;

        Color startColor =
            spriteRenderer != null
                ? spriteRenderer.color
                : Color.white;

        while (timer <
               duration)
        {
            timer +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    duration
                );

            float eased =
                EaseOutCubic(
                    t
                );

            // =================================================
            // RISE
            // =================================================

            transform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    eased
                );

            // =================================================
            // SHRINK
            // =================================================

            transform.localScale =
                Vector3.Lerp(
                    startScale,
                    targetScale,
                    eased
                );

            // =================================================
            // FADE
            // =================================================

            if (fadeOnCollect &&
                spriteRenderer != null)
            {
                Color color =
                    startColor;

                color.a =
                    Mathf.Lerp(
                        startColor.a,
                        0f,
                        eased
                    );

                spriteRenderer.color =
                    color;
            }

            yield return null;
        }

        Destroy(
            gameObject
        );
    }

    // =========================================================
    // COLLECT SOUND
    // =========================================================

    private void PlayCollectSound()
    {
        AudioClip clip =
            GetRandomValidClip(
                collectSounds
            );

        if (clip == null)
        {
            return;
        }

        // =====================================================
        // DETACHED AUDIO OBJECT
        // =====================================================

        GameObject audioObject =
            new GameObject(
                mammalName +
                " Collect Sound"
            );

        audioObject.transform.position =
            transform.position;

        AudioSource source =
            audioObject.AddComponent<AudioSource>();

        source.enabled =
            true;

        source.playOnAwake =
            false;

        source.loop =
            false;

        source.spatialBlend =
            0f;

        source.clip =
            clip;

        source.volume =
            collectSoundVolume;

        source.pitch =
            Random.Range(
                collectPitchMin,
                collectPitchMax
            );

        if (source.enabled &&
            source.gameObject.activeInHierarchy)
        {
            source.Play();
        }

        float pitch =
            Mathf.Max(
                0.01f,
                Mathf.Abs(
                    source.pitch
                )
            );

        float destroyDelay =
            clip.length /
            pitch +
            0.1f;

        Destroy(
            audioObject,
            destroyDelay
        );
    }

    // =========================================================
    // RANDOM SOUND
    // =========================================================

    private AudioClip GetRandomValidClip(
        AudioClip[] clips)
    {
        if (clips == null ||
            clips.Length == 0)
        {
            return null;
        }

        int validCount =
            0;

        for (int i = 0;
             i < clips.Length;
             i++)
        {
            if (clips[i] != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return null;
        }

        int chosenIndex =
            Random.Range(
                0,
                validCount
            );

        int currentValidIndex =
            0;

        for (int i = 0;
             i < clips.Length;
             i++)
        {
            if (clips[i] == null)
            {
                continue;
            }

            if (currentValidIndex ==
                chosenIndex)
            {
                return clips[i];
            }

            currentValidIndex++;
        }

        return null;
    }

    // =========================================================
    // UI
    // =========================================================

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return
            EventSystem.current
                .IsPointerOverGameObject();
    }

    // =========================================================
    // GETTERS
    // =========================================================

    public string GetMammalName()
    {
        return mammalName;
    }

    public bool IsCollected()
    {
        return collected;
    }

    public CropPlot GetCropOwner()
    {
        return cropOwner;
    }

    // =========================================================
    // EASING
    // =========================================================

    private float EaseOutCubic(
        float x)
    {
        return
            1f -
            Mathf.Pow(
                1f - x,
                3f
            );
    }

    // =========================================================
    // DISABLE
    // =========================================================

    private void OnDisable()
    {
        if (spriteRenderer != null &&
            normalMaterial != null)
        {
            spriteRenderer.material =
                normalMaterial;
        }
    }

    // =========================================================
    // DEBUG
    // =========================================================

    [ContextMenu("Debug - Find Crop Owner")]
    private void DebugFindCropOwner()
    {
        cropOwner =
            null;

        AutoFindCropOwner();

        Debug.Log(
            "[MammalPickup] Crop Owner = " +
            (
                cropOwner != null
                    ? cropOwner.name
                    : "NOT FOUND"
            )
        );
    }

    [ContextMenu("Debug - Collect Mammal")]
    private void DebugCollect()
    {
        Collect();
    }
}