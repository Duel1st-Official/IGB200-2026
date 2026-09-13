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
    [SerializeField] private SelectionWheel selectionWheel;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform player;

    // =========================================================
    // MAMMAL
    // =========================================================

    [Header("Mammal")]
    [SerializeField] private string mammalName = "Mammal";

    // =========================================================
    // INTERACTION
    // =========================================================

    [Header("Interaction")]

    [Tooltip("Maximum distance from the player that the mammal can be collected.")]
    [SerializeField] private float pickupDistance = 3f;

    [Tooltip("Allow the mammal to be collected by left clicking it.")]
    [SerializeField] private bool allowClickPickup = true;

    // =========================================================
    // HOVER
    // =========================================================

    [Header("Hover")]
    [SerializeField] private Material hoverMaterial;

    // =========================================================
    // IDLE BOB
    // =========================================================

    [Header("Idle Bob")]

    [Tooltip("Makes the mammal slowly float up and down while waiting.")]
    [SerializeField] private bool enableIdleBob = true;

    [Tooltip("How far the mammal moves vertically.")]
    [SerializeField] private float bobHeight = 0.12f;

    [Tooltip("How quickly the mammal bobs. Lower = slower.")]
    [SerializeField] private float bobSpeed = 1.5f;

    [Tooltip("Offsets different mammals so they do not all bob at exactly the same time.")]
    [SerializeField] private bool randomizeBobPhase = true;

    // =========================================================
    // COLLECTION ANIMATION
    // =========================================================

    [Header("Collection Animation")]

    [Tooltip("How long the pickup animation takes.")]
    [SerializeField] private float collectDuration = 0.45f;

    [Tooltip("How far upward the mammal travels when collected.")]
    [SerializeField] private float collectRiseDistance = 0.7f;

    [Tooltip("Final scale multiplier at the end of the collection animation.")]
    [Range(0f, 1f)]
    [SerializeField] private float collectEndScale = 0.25f;

    [Tooltip("Fade the mammal out while it is being collected.")]
    [SerializeField] private bool fadeOnCollect = true;

    // =========================================================
    // COLLECTION AUDIO
    // =========================================================

    [Header("Collection Audio")]

    [Tooltip("Random sound played when the mammal is collected.")]
    [SerializeField] private AudioClip[] collectSounds = new AudioClip[3];

    [Range(0f, 1f)]
    [SerializeField] private float collectSoundVolume = 1f;

    [SerializeField] private float collectPitchMin = 0.95f;
    [SerializeField] private float collectPitchMax = 1.05f;

    // =========================================================
    // EVENTS
    // =========================================================

    [Header("Events")]
    [SerializeField] private UnityEvent onCollected;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // =========================================================
    // PRIVATE
    // =========================================================

    private CropPlot cropOwner;

    private Material normalMaterial;

    private Collider2D mammalCollider;

    private bool collected;
    private bool isHovered;

    private Vector3 baseLocalPosition;
    private Vector3 originalScale;

    private float bobPhase;

    private Color originalColor;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // -----------------------------------------------------
        // CAMERA
        // -----------------------------------------------------

        if (mainCamera == null)
        {
            mainCamera =
                Camera.main;
        }

        // -----------------------------------------------------
        // SPRITE RENDERER
        // -----------------------------------------------------

        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponentInChildren<SpriteRenderer>();
        }

        // -----------------------------------------------------
        // MATERIAL
        // -----------------------------------------------------

        if (spriteRenderer != null)
        {
            normalMaterial =
                spriteRenderer.material;

            originalColor =
                spriteRenderer.color;
        }

        // -----------------------------------------------------
        // COLLIDER
        // -----------------------------------------------------

        mammalCollider =
            GetComponent<Collider2D>();

        if (mammalCollider == null)
        {
            mammalCollider =
                GetComponentInChildren<Collider2D>();
        }

        // -----------------------------------------------------
        // BOB
        // -----------------------------------------------------

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
            bobPhase =
                0f;
        }
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // -----------------------------------------------------
        // SELECTION WHEEL
        // -----------------------------------------------------

        if (selectionWheel == null)
        {
            selectionWheel =
                FindFirstObjectByType<SelectionWheel>();
        }

        // -----------------------------------------------------
        // PLAYER
        // -----------------------------------------------------

        if (player == null)
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

        // -----------------------------------------------------
        // IDLE BOB
        // -----------------------------------------------------

        UpdateIdleBob();

        // -----------------------------------------------------
        // NORMAL MODE ONLY
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // UI BLOCKING
        // -----------------------------------------------------

        if (IsPointerOverUI())
        {
            SetHovered(
                false
            );

            return;
        }

        // -----------------------------------------------------
        // CAMERA
        // -----------------------------------------------------

        if (mainCamera == null)
        {
            mainCamera =
                Camera.main;

            if (mainCamera == null)
            {
                return;
            }
        }

        // -----------------------------------------------------
        // PLAYER
        // -----------------------------------------------------

        if (player == null)
        {
            TryFindPlayer();
        }

        // -----------------------------------------------------
        // MOUSE POSITION
        // -----------------------------------------------------

        Vector3 mouseWorld =
            mainCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        mouseWorld.z =
            transform.position.z;

        // -----------------------------------------------------
        // HOVER
        // -----------------------------------------------------

        bool hovering =
            IsMouseOverMammal(
                mouseWorld
            );

        // -----------------------------------------------------
        // RANGE
        // -----------------------------------------------------

        bool inRange =
            IsPlayerInRange();

        SetHovered(
            hovering &&
            inRange
        );

        // -----------------------------------------------------
        // PICKUP
        // -----------------------------------------------------

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

        return spriteRenderer.bounds.Contains(
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

        return distance <=
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
    // SET CROP OWNER
    // =========================================================

    public void SetCropOwner(
        CropPlot owner)
    {
        cropOwner =
            owner;
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

        collected =
            true;

        // -----------------------------------------------------
        // STOP HOVER
        // -----------------------------------------------------

        isHovered =
            false;

        if (spriteRenderer != null &&
            normalMaterial != null)
        {
            spriteRenderer.material =
                normalMaterial;
        }

        // -----------------------------------------------------
        // DISABLE COLLIDER
        // -----------------------------------------------------

        if (mammalCollider != null)
        {
            mammalCollider.enabled =
                false;
        }

        // -----------------------------------------------------
        // PLAY COLLECTION SOUND
        // -----------------------------------------------------

        PlayCollectSound();

        // -----------------------------------------------------
        // EVENT
        // -----------------------------------------------------

        if (onCollected != null)
        {
            onCollected.Invoke();
        }

        // -----------------------------------------------------
        // TELL CROP
        // -----------------------------------------------------

        // This consumes/clears the crop immediately.
        if (cropOwner != null)
        {
            cropOwner.MammalCollected(
                this
            );
        }

        // -----------------------------------------------------
        // DEBUG
        // -----------------------------------------------------

        if (showDebugLogs)
        {
            Debug.Log(
                "Collected mammal: " +
                mammalName
            );
        }

        // -----------------------------------------------------
        // COLLECTION ANIMATION
        // -----------------------------------------------------

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

        // Keep the current bob position so it does not snap.
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

            // Strong movement at the start,
            // slowly settling toward the end.
            float eased =
                EaseOutCubic(
                    t
                );

            // -------------------------------------------------
            // RISE
            // -------------------------------------------------

            transform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    eased
                );

            // -------------------------------------------------
            // SHRINK
            // -------------------------------------------------

            transform.localScale =
                Vector3.Lerp(
                    startScale,
                    targetScale,
                    eased
                );

            // -------------------------------------------------
            // FADE
            // -------------------------------------------------

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

        // -----------------------------------------------------
        // DETACHED AUDIO OBJECT
        // -----------------------------------------------------
        //
        // The sound is played on its own temporary object.
        // This means it continues playing even after the
        // mammal itself has disappeared.
        // -----------------------------------------------------

        GameObject audioObject =
            new GameObject(
                mammalName +
                " Collect Sound"
            );

        audioObject.transform.position =
            transform.position;

        AudioSource source =
            audioObject.AddComponent<AudioSource>();

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

        source.Play();

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
    // RANDOM CLIP
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
    // UI CHECK
    // =========================================================

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return EventSystem.current.IsPointerOverGameObject();
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

    [ContextMenu("Debug - Collect Mammal")]
    private void DebugCollect()
    {
        Collect();
    }
}