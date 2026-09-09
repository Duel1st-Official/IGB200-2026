using System.Collections;
using UnityEngine;

public class BirdSpawner : MonoBehaviour
{
    // =========================================================
    // BIRD PREFABS
    // =========================================================

    [Header("Bird Prefabs")]
    [SerializeField] private GameObject[] birdPrefabs;

    // =========================================================
    // MAP AREA
    // =========================================================

    [Header("Map Flight Area")]
    [SerializeField] private Vector2 mapCenter = Vector2.zero;

    [SerializeField] private float mapWidth = 18f;
    [SerializeField] private float mapHeight = 9f;

    [Tooltip("Birds spawn this far outside the map.")]
    [SerializeField] private float spawnPadding = 2f;

    // =========================================================
    // SPAWNING
    // =========================================================

    [Header("Random Spawning")]
    [SerializeField] private float minimumSpawnDelay = 8f;
    [SerializeField] private float maximumSpawnDelay = 25f;

    [Range(0f, 1f)]
    [SerializeField] private float spawnChance = 0.75f;

    // =========================================================
    // BIRD COUNT
    // =========================================================

    [Header("Bird Count")]
    [SerializeField] private int minimumBirds = 1;
    [SerializeField] private int maximumBirds = 1;

    [SerializeField] private float flockSpawnSpacing = 0.3f;

    // =========================================================
    // SPEED
    // =========================================================

    [Header("Flight Speed")]
    [SerializeField] private float minimumSpeed = 2.5f;
    [SerializeField] private float maximumSpeed = 4f;

    // =========================================================
    // DIAGONAL FLIGHT
    // =========================================================

    [Header("Diagonal Flight")]

    [Tooltip(
        "Minimum vertical component of the diagonal flight."
    )]
    [Range(0.1f, 1f)]
    [SerializeField] private float minimumDiagonalAmount = 0.35f;

    [Tooltip(
        "Maximum vertical component of the diagonal flight."
    )]
    [Range(0.1f, 1.5f)]
    [SerializeField] private float maximumDiagonalAmount = 0.75f;

    [Tooltip(
        "Adds small randomness so every bird does not follow the exact same path."
    )]
    [SerializeField] private float directionVariation = 0.1f;

    // =========================================================
    // PARENT
    // =========================================================

    [Header("Hierarchy")]
    [SerializeField] private Transform birdParent;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool spawnImmediately = false;
    [SerializeField] private bool showDebugLogs = false;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (spawnImmediately)
        {
            StartCoroutine(
                SpawnBirdGroup()
            );
        }

        StartCoroutine(
            SpawnRoutine()
        );
    }

    // =========================================================
    // SPAWN ROUTINE
    // =========================================================

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            float delay =
                Random.Range(
                    minimumSpawnDelay,
                    maximumSpawnDelay
                );

            yield return
                new WaitForSeconds(
                    delay
                );

            if (Random.value >
                spawnChance)
            {
                continue;
            }

            yield return
                SpawnBirdGroup();
        }
    }

    // =========================================================
    // SPAWN GROUP
    // =========================================================

    private IEnumerator SpawnBirdGroup()
    {
        int count =
            Random.Range(
                minimumBirds,
                maximumBirds + 1
            );

        FlightData flight =
            GenerateDiagonalFlight();

        for (int i = 0;
            i < count;
            i++)
        {
            Vector2 spawnPosition =
                flight.spawnPosition;

            // Small position variation if multiple
            // birds appear together.
            spawnPosition +=
                new Vector2(
                    Random.Range(
                        -0.4f,
                        0.4f
                    ),
                    Random.Range(
                        -0.4f,
                        0.4f
                    )
                );

            SpawnBird(
                spawnPosition,
                flight.direction
            );

            if (i <
                count - 1)
            {
                yield return
                    new WaitForSeconds(
                        flockSpawnSpacing
                    );
            }
        }
    }

    // =========================================================
    // SPAWN BIRD
    // =========================================================

    private void SpawnBird(
        Vector2 position,
        Vector2 direction)
    {
        if (birdPrefabs == null ||
            birdPrefabs.Length == 0)
        {
            return;
        }

        GameObject prefab =
            birdPrefabs[
                Random.Range(
                    0,
                    birdPrefabs.Length
                )
            ];

        if (prefab == null)
        {
            return;
        }

        GameObject bird =
            Instantiate(
                prefab,
                position,
                Quaternion.identity,
                birdParent
            );

        FlyingBird flyingBird =
            bird.GetComponent<FlyingBird>();

        if (flyingBird == null)
        {
            flyingBird =
                bird.GetComponentInChildren<FlyingBird>();
        }

        if (flyingBird != null)
        {
            float speed =
                Random.Range(
                    minimumSpeed,
                    maximumSpeed
                );

            flyingBird.Setup(
                direction,
                speed
            );
        }

        if (showDebugLogs)
        {
            Debug.Log(
                "Bird spawned at " +
                position +
                " with diagonal direction " +
                direction
            );
        }
    }

    // =========================================================
    // GENERATE DIAGONAL FLIGHT
    // =========================================================

    private FlightData GenerateDiagonalFlight()
    {
        float halfWidth =
            mapWidth *
            0.5f;

        float halfHeight =
            mapHeight *
            0.5f;

        // Randomly start on left or right side.
        bool startLeft =
            Random.value >
            0.5f;

        // Randomly travel upward or downward.
        bool travelUp =
            Random.value >
            0.5f;

        // =====================================================
        // SPAWN POSITION
        // =====================================================

        float spawnX =
            startLeft
                ? mapCenter.x -
                  halfWidth -
                  spawnPadding
                : mapCenter.x +
                  halfWidth +
                  spawnPadding;

        float spawnY;

        if (travelUp)
        {
            // Start lower on the map.
            spawnY =
                Random.Range(
                    mapCenter.y -
                    halfHeight -
                    spawnPadding,

                    mapCenter.y
                );
        }
        else
        {
            // Start higher on the map.
            spawnY =
                Random.Range(
                    mapCenter.y,
                    mapCenter.y +
                    halfHeight +
                    spawnPadding
                );
        }

        // =====================================================
        // DIRECTION
        // =====================================================

        float horizontalDirection =
            startLeft
                ? 1f
                : -1f;

        float diagonalAmount =
            Random.Range(
                minimumDiagonalAmount,
                maximumDiagonalAmount
            );

        float verticalDirection =
            travelUp
                ? diagonalAmount
                : -diagonalAmount;

        Vector2 direction =
            new Vector2(
                horizontalDirection,
                verticalDirection
            );

        // Small variation.
        direction.x +=
            Random.Range(
                -directionVariation,
                directionVariation
            );

        direction.y +=
            Random.Range(
                -directionVariation,
                directionVariation
            );

        direction.Normalize();

        return new FlightData(
            new Vector2(
                spawnX,
                spawnY
            ),
            direction
        );
    }

    // =========================================================
    // DEBUG SPAWN
    // =========================================================

    [ContextMenu("Spawn Bird")]
    public void DebugSpawnBird()
    {
        StartCoroutine(
            SpawnBirdGroup()
        );
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(
            mapCenter,
            new Vector3(
                mapWidth,
                mapHeight,
                0f
            )
        );
    }

    // =========================================================
    // FLIGHT DATA
    // =========================================================

    private struct FlightData
    {
        public Vector2 spawnPosition;
        public Vector2 direction;

        public FlightData(
            Vector2 position,
            Vector2 flightDirection)
        {
            spawnPosition =
                position;

            direction =
                flightDirection;
        }
    }
}