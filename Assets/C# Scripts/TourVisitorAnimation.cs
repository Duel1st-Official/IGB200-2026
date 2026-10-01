using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Complete Tour visitor behaviour.
///
/// Automatically handles:
/// - Gentle idle bopping
/// - Random left/right looking
/// - 90% transparency when the player overlaps the visitor
/// - Top-down sorting in front of / behind the player
///
/// No references need to be assigned manually.
/// Add this script to each visitor prefab.
/// </summary>
public class TourVisitorAnimation : MonoBehaviour
{
    // =========================================================
    // IDLE BOP
    // =========================================================

    [Header("Idle Bop")]

    [Min(0f)]
    [SerializeField] private float bopHeight = 0.035f;

    [Min(0f)]
    [SerializeField] private float bopSpeed = 1.5f;

    [Range(0f, 1f)]
    [SerializeField] private float bopSpeedVariation = 0.25f;

    // =========================================================
    // LOOK LEFT / RIGHT
    // =========================================================

    [Header("Look Left / Right")]

    [Min(0.1f)]
    [SerializeField] private float minimumLookDelay = 1.5f;

    [Min(0.1f)]
    [SerializeField] private float maximumLookDelay = 4f;

    [SerializeField] private bool randomStartingDirection = true;

    // =========================================================
    // SPAWN APPEARANCE
    // =========================================================

    [Header("Spawn Appearance")]

    [Tooltip("Visitors smoothly fade in when they first appear.")]
    [SerializeField] private bool smoothSpawnAppearance = true;

    [Tooltip("How long the fade-in takes in seconds.")]
    [Min(0.01f)]
    [SerializeField] private float spawnFadeDuration = 0.45f;

    [Tooltip("Optional small upward movement while appearing.")]
    [SerializeField] private bool useSpawnRise = true;

    [Tooltip("How far below the normal position the visitor starts.")]
    [Min(0f)]
    [SerializeField] private float spawnRiseDistance = 0.08f;

    // =========================================================
    // PLAYER OVERLAP FADE
    // =========================================================

    [Header("Player Overlap Fade")]

    [Tooltip("0.1 = 10% visible, meaning 90% transparent.")]
    [Range(0f, 1f)]
    [SerializeField] private float overlapAlpha = 0.1f;

    [Tooltip("How quickly the visitor fades in and out.")]
    [Min(0.1f)]
    [SerializeField] private float fadeSpeed = 12f;

    [Tooltip("Extra size added around the visitor collider for overlap detection.")]
    [Min(0f)]
    [SerializeField] private float overlapPadding = 0.02f;

    // =========================================================
    // SORTING
    // =========================================================

    [Header("Player Sorting")]

    [Tooltip("Reference sorting order used to decide the visitor's front/behind values. The visitor script does NOT change the player's sorting order.")]
    [SerializeField] private int playerSortingOrder = 100;

    [Tooltip("Visitor sorting order when the player is above / behind the visitor.")]
    [SerializeField] private int visitorInFrontOrder = 110;

    [Tooltip("Visitor sorting order when the player is below / in front of the visitor.")]
    [SerializeField] private int visitorBehindOrder = 90;

    [Header("Visitor Sort Point")]

    [Tooltip("Move this Y offset to the visitor's feet / bottom edge.")]
    [SerializeField] private float sortYOffset = 0f;

    // =========================================================
    // AUTOMATIC REFERENCES
    // =========================================================

    private SpriteRenderer visitorSprite;
    private Collider2D visitorCollider;
    private Transform visualTransform;
    private SortingGroup visitorSortingGroup;

    private GameObject playerObject;
    private Transform player;
    private Collider2D[] playerColliders;
    private SpriteRenderer playerRenderer;
    private SortingGroup playerSortingGroup;

    // =========================================================
    // ANIMATION STATE
    // =========================================================

    private Vector3 originalVisualLocalPosition;
    private Color originalSpriteColor = Color.white;
    private bool originalFlipX;

    private float randomBopPhase;
    private float actualBopSpeed;
    private float nextLookTime;

    private bool initialised;

    private float spawnAppearanceTimer;
    private bool spawnAppearanceFinished;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        AutoAssignVisitor();
        InitialiseVisitor();
        BeginSpawnAppearance();
        AutoAssignPlayer();
    }

    private void Start()
    {
        // Try again in Start in case the player was spawned
        // after this visitor's Awake().
        AutoAssignPlayer();

        UpdateSorting();
    }

    private void Update()
    {
        RecoverMissingReferences();

        UpdateSpawnAppearance();
        UpdateBop();
        UpdateLookDirection();
        UpdatePlayerFade();
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            AutoAssignPlayer();

            if (player == null)
            {
                return;
            }

        }

        UpdateSorting();
    }

    // =========================================================
    // AUTO ASSIGN VISITOR
    // =========================================================

    private void AutoAssignVisitor()
    {
        if (visitorSprite == null)
        {
            visitorSprite =
                GetComponent<SpriteRenderer>();

            if (visitorSprite == null)
            {
                visitorSprite =
                    GetComponentInChildren<SpriteRenderer>(true);
            }
        }

        if (visitorCollider == null)
        {
            visitorCollider =
                GetComponent<Collider2D>();

            if (visitorCollider == null)
            {
                visitorCollider =
                    GetComponentInChildren<Collider2D>(true);
            }
        }

        visitorSortingGroup =
            GetComponent<SortingGroup>();

        if (visitorSortingGroup == null)
        {
            visitorSortingGroup =
                GetComponentInChildren<SortingGroup>(true);
        }

        if (visitorSprite != null)
        {
            visualTransform =
                visitorSprite.transform;
        }
        else
        {
            visualTransform =
                transform;
        }
    }

    // =========================================================
    // AUTO ASSIGN PLAYER
    // =========================================================

    private void AutoAssignPlayer()
    {
        if (playerObject != null &&
            player != null)
        {
            CachePlayerReferences();
            return;
        }

        GameObject foundPlayer = null;

        try
        {
            foundPlayer =
                GameObject.FindGameObjectWithTag("Player");
        }
        catch (UnityException)
        {
            return;
        }

        if (foundPlayer == null)
        {
            return;
        }

        playerObject =
            foundPlayer;

        player =
            foundPlayer.transform;

        CachePlayerReferences();
    }

    private void CachePlayerReferences()
    {
        if (playerObject == null)
        {
            return;
        }

        playerColliders =
            playerObject.GetComponentsInChildren<Collider2D>(true);

        playerSortingGroup =
            playerObject.GetComponent<SortingGroup>();

        if (playerSortingGroup == null)
        {
            playerSortingGroup =
                playerObject.GetComponentInChildren<SortingGroup>(true);
        }

        playerRenderer =
            playerObject.GetComponent<SpriteRenderer>();

        if (playerRenderer == null)
        {
            playerRenderer =
                playerObject.GetComponentInChildren<SpriteRenderer>(true);
        }

        // Support a Player-tagged child object where the actual
        // rendering/collider components are on the root.
        if ((playerColliders == null ||
             playerColliders.Length == 0) &&
            playerObject.transform.root != null)
        {
            playerObject =
                playerObject.transform.root.gameObject;

            player =
                playerObject.transform;

            playerColliders =
                playerObject.GetComponentsInChildren<Collider2D>(true);

            playerSortingGroup =
                playerObject.GetComponent<SortingGroup>();

            if (playerSortingGroup == null)
            {
                playerSortingGroup =
                    playerObject.GetComponentInChildren<SortingGroup>(true);
            }

            playerRenderer =
                playerObject.GetComponent<SpriteRenderer>();

            if (playerRenderer == null)
            {
                playerRenderer =
                    playerObject.GetComponentInChildren<SpriteRenderer>(true);
            }
        }
    }

    private void RecoverMissingReferences()
    {
        if (visitorSprite == null ||
            visitorCollider == null ||
            visualTransform == null)
        {
            AutoAssignVisitor();
        }

        if (playerObject == null ||
            player == null)
        {
            AutoAssignPlayer();
        }

        if (!initialised)
        {
            InitialiseVisitor();
        }
    }

    // =========================================================
    // INITIALISE VISITOR
    // =========================================================

    private void InitialiseVisitor()
    {
        if (initialised)
        {
            return;
        }

        if (visualTransform != null)
        {
            originalVisualLocalPosition =
                visualTransform.localPosition;
        }

        if (visitorSprite != null)
        {
            originalSpriteColor =
                visitorSprite.color;

            originalFlipX =
                visitorSprite.flipX;

            if (randomStartingDirection)
            {
                visitorSprite.flipX =
                    Random.value < 0.5f;
            }
        }

        randomBopPhase =
            Random.Range(
                0f,
                Mathf.PI * 2f
            );

        float variation =
            Random.Range(
                -bopSpeedVariation,
                bopSpeedVariation
            );

        actualBopSpeed =
            Mathf.Max(
                0f,
                bopSpeed * (1f + variation)
            );

        ScheduleNextLook();

        initialised = true;
    }

    // =========================================================
    // SPAWN APPEARANCE
    // =========================================================

    private void BeginSpawnAppearance()
    {
        spawnAppearanceTimer = 0f;

        spawnAppearanceFinished =
            !smoothSpawnAppearance;

        if (visitorSprite != null &&
            smoothSpawnAppearance)
        {
            Color colour =
                visitorSprite.color;

            colour.a = 0f;

            visitorSprite.color =
                colour;
        }
    }

    private void UpdateSpawnAppearance()
    {
        if (!smoothSpawnAppearance ||
            spawnAppearanceFinished)
        {
            return;
        }

        spawnAppearanceTimer +=
            Time.deltaTime;

        if (spawnAppearanceTimer >=
            Mathf.Max(
                0.01f,
                spawnFadeDuration
            ))
        {
            spawnAppearanceTimer =
                Mathf.Max(
                    0.01f,
                    spawnFadeDuration
                );

            spawnAppearanceFinished =
                true;
        }
    }

    private float GetSpawnAppearanceMultiplier()
    {
        if (!smoothSpawnAppearance ||
            spawnAppearanceFinished)
        {
            return 1f;
        }

        float duration =
            Mathf.Max(
                0.01f,
                spawnFadeDuration
            );

        float progress =
            Mathf.Clamp01(
                spawnAppearanceTimer /
                duration
            );

        // Smoothstep gives a softer start and finish than a linear fade.
        return
            progress *
            progress *
            (3f - 2f * progress);
    }

    // =========================================================
    // BOP
    // =========================================================

    private void UpdateBop()
    {
        if (visualTransform == null)
        {
            return;
        }

        float verticalOffset =
            Mathf.Sin(
                (Time.time *
                 actualBopSpeed *
                 Mathf.PI *
                 2f) +
                randomBopPhase
            ) *
            bopHeight;

        Vector3 newPosition =
            originalVisualLocalPosition;

        newPosition.y +=
            verticalOffset;

        if (smoothSpawnAppearance &&
            useSpawnRise &&
            !spawnAppearanceFinished)
        {
            float duration =
                Mathf.Max(
                    0.01f,
                    spawnFadeDuration
                );

            float progress =
                Mathf.Clamp01(
                    spawnAppearanceTimer /
                    duration
                );

            float easedProgress =
                progress *
                progress *
                (3f - 2f * progress);

            newPosition.y -=
                Mathf.Lerp(
                    spawnRiseDistance,
                    0f,
                    easedProgress
                );
        }

        visualTransform.localPosition =
            newPosition;
    }

    // =========================================================
    // LOOK LEFT / RIGHT
    // =========================================================

    private void UpdateLookDirection()
    {
        if (visitorSprite == null ||
            Time.time < nextLookTime)
        {
            return;
        }

        visitorSprite.flipX =
            Random.value < 0.5f;

        ScheduleNextLook();
    }

    private void ScheduleNextLook()
    {
        float minDelay =
            Mathf.Max(
                0.1f,
                minimumLookDelay
            );

        float maxDelay =
            Mathf.Max(
                minDelay,
                maximumLookDelay
            );

        nextLookTime =
            Time.time +
            Random.Range(
                minDelay,
                maxDelay
            );
    }

    // =========================================================
    // PLAYER OVERLAP FADE
    // =========================================================

    private void UpdatePlayerFade()
    {
        if (visitorSprite == null)
        {
            return;
        }

        if (playerObject != null &&
            (playerColliders == null ||
             playerColliders.Length == 0))
        {
            CachePlayerReferences();
        }

        bool playerIsOverlapping =
            CheckPlayerOverlap();

        float normalTargetAlpha =
            playerIsOverlapping
                ? Mathf.Clamp01(overlapAlpha)
                : originalSpriteColor.a;

        float spawnMultiplier =
            GetSpawnAppearanceMultiplier();

        float targetAlpha =
            normalTargetAlpha *
            spawnMultiplier;

        Color currentColor =
            visitorSprite.color;

        if (!spawnAppearanceFinished &&
            smoothSpawnAppearance)
        {
            // During the initial appearance, alpha is driven directly
            // by the smooth spawn transition so it always feels consistent.
            currentColor.a =
                targetAlpha;
        }
        else
        {
            float smoothing =
                1f -
                Mathf.Exp(
                    -fadeSpeed *
                    Time.deltaTime
                );

            currentColor.a =
                Mathf.Lerp(
                    currentColor.a,
                    targetAlpha,
                    smoothing
                );
        }

        visitorSprite.color =
            currentColor;
    }

    private bool CheckPlayerOverlap()
    {
        if (visitorCollider == null ||
            playerColliders == null ||
            playerColliders.Length == 0)
        {
            return false;
        }

        Bounds visitorBounds =
            visitorCollider.bounds;

        visitorBounds.Expand(
            new Vector3(
                overlapPadding * 2f,
                overlapPadding * 2f,
                0f
            )
        );

        for (int i = 0;
             i < playerColliders.Length;
             i++)
        {
            Collider2D playerCollider =
                playerColliders[i];

            if (playerCollider == null ||
                !playerCollider.enabled ||
                !playerCollider.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (visitorBounds.Intersects(
                    playerCollider.bounds))
            {
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // VISITOR SORTING
    // =========================================================

    private void UpdateSorting()
    {
        if (player == null)
        {
            return;
        }

        float visitorSortY =
            transform.position.y +
            sortYOffset;

        // Use the player's feet/bottom edge instead of the player's
        // transform pivot. This makes top-down sorting much more reliable
        // when the player's pivot is near the centre of the sprite.
        float playerSortY =
            GetPlayerSortY();

        // Higher Y = player is behind/above the visitor.
        // The visitor renders in front.
        if (playerSortY > visitorSortY)
        {
            SetVisitorSortingOrder(
                visitorInFrontOrder
            );
        }

        // Lower Y = player is in front/below the visitor.
        // The visitor renders behind.
        else
        {
            SetVisitorSortingOrder(
                visitorBehindOrder
            );
        }
    }

    private float GetPlayerSortY()
    {
        if (playerColliders != null)
        {
            bool foundCollider = false;
            float lowestY = float.PositiveInfinity;

            for (int i = 0; i < playerColliders.Length; i++)
            {
                Collider2D playerCollider =
                    playerColliders[i];

                if (playerCollider == null ||
                    !playerCollider.enabled ||
                    !playerCollider.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float colliderBottom =
                    playerCollider.bounds.min.y;

                if (!foundCollider ||
                    colliderBottom < lowestY)
                {
                    lowestY =
                        colliderBottom;

                    foundCollider =
                        true;
                }
            }

            if (foundCollider)
            {
                return lowestY;
            }
        }

        return
            player != null
                ? player.position.y
                : 0f;
    }

    private void SetVisitorSortingOrder(
        int order)
    {
        if (visitorSortingGroup != null)
        {
            visitorSortingGroup.sortingOrder =
                order;

            return;
        }

        if (visitorSprite != null)
        {
            visitorSprite.sortingOrder =
                order;
        }
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDisable()
    {
        if (!initialised)
        {
            return;
        }

        if (visualTransform != null)
        {
            visualTransform.localPosition =
                originalVisualLocalPosition;
        }

        if (visitorSprite != null)
        {
            visitorSprite.color =
                originalSpriteColor;

            visitorSprite.flipX =
                originalFlipX;
        }
    }

    // =========================================================
    // DEBUG
    // =========================================================

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        // SORTING LINE
        Vector3 sortPoint =
            transform.position +
            new Vector3(
                0f,
                sortYOffset,
                0f
            );

        Gizmos.DrawWireSphere(
            sortPoint,
            0.08f
        );

        Gizmos.DrawLine(
            sortPoint +
            Vector3.left * 0.5f,
            sortPoint +
            Vector3.right * 0.5f
        );

        // OVERLAP AREA
        Collider2D foundCollider =
            GetComponent<Collider2D>();

        if (foundCollider == null)
        {
            foundCollider =
                GetComponentInChildren<Collider2D>();
        }

        if (foundCollider == null)
        {
            return;
        }

        Bounds bounds =
            foundCollider.bounds;

        bounds.Expand(
            new Vector3(
                overlapPadding * 2f,
                overlapPadding * 2f,
                0f
            )
        );

        Gizmos.DrawWireCube(
            bounds.center,
            bounds.size
        );
    }

#endif
}
