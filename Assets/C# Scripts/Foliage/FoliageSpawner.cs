using System.Collections.Generic;
using UnityEngine;

public class FoliageSpawner : MonoBehaviour
{
    [Header("Foliage Prefabs")]
    [SerializeField] private GameObject[] foliagePrefabs;

    [Header("Map Grid")]
    [SerializeField] private int gridWidth = 18;
    [SerializeField] private int gridHeight = 9;
    [SerializeField] private float gridSize = 1f;

    [Header("Generation")]
    [Range(0f, 1f)]
    [SerializeField] private float spawnChance = 0.45f;

    [SerializeField] private int maxFoliagePerCell = 1;

    [Header("Position Variation")]
    [SerializeField] private bool randomPositionInsideCell = true;

    [Range(0f, 0.5f)]
    [SerializeField] private float positionVariation = 0.35f;

    [Header("Visual Variation")]
    [SerializeField] private bool randomFlipX = true;

    [SerializeField] private bool randomScale = true;
    [SerializeField] private float minScaleMultiplier = 0.9f;
    [SerializeField] private float maxScaleMultiplier = 1.1f;

    [Header("Collision")]
    [SerializeField] private LayerMask blockedLayers;
    [SerializeField] private float collisionCheckSize = 0.3f;

    [Header("Random Seed")]
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int seed = 12345;

    [Header("Parent")]
    [SerializeField] private Transform foliageParent;

    private readonly List<GameObject> spawnedFoliage =
        new List<GameObject>();

    private void Start()
    {
        GenerateFoliage();
    }

    // =========================================================
    // GENERATE
    // =========================================================

    public void GenerateFoliage()
    {
        ClearFoliage();

        if (foliagePrefabs == null ||
            foliagePrefabs.Length == 0)
        {
            Debug.LogWarning(
                "No foliage prefabs assigned."
            );

            return;
        }

        // Random seed every play
        if (useRandomSeed)
        {
            Random.InitState(
                System.Environment.TickCount
            );
        }
        else
        {
            Random.InitState(seed);
        }

        int minX =
            -(gridWidth / 2);

        int minY =
            -(gridHeight / 2);

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                int cellX =
                    minX + x;

                int cellY =
                    minY + y;

                Vector2 cellPosition =
                    new Vector2(
                        cellX * gridSize,
                        cellY * gridSize
                    );

                TryGenerateInCell(
                    cellPosition
                );
            }
        }
    }

    // =========================================================
    // CELL GENERATION
    // =========================================================

    private void TryGenerateInCell(
        Vector2 cellPosition)
    {
        for (
            int i = 0;
            i < maxFoliagePerCell;
            i++)
        {
            // Random chance that this cell
            // has no foliage.
            if (Random.value >
                spawnChance)
            {
                continue;
            }

            Vector2 spawnPosition =
                cellPosition;

            // =========================================
            // RANDOM POSITION INSIDE CELL
            // =========================================

            if (randomPositionInsideCell)
            {
                float offsetX =
                    Random.Range(
                        -positionVariation,
                        positionVariation
                    );

                float offsetY =
                    Random.Range(
                        -positionVariation,
                        positionVariation
                    );

                spawnPosition +=
                    new Vector2(
                        offsetX,
                        offsetY
                    );
            }

            // =========================================
            // BLOCKED AREA CHECK
            // =========================================

            if (IsPositionBlocked(
                spawnPosition))
            {
                continue;
            }

            SpawnFoliage(
                spawnPosition
            );
        }
    }

    // =========================================================
    // SPAWN FOLIAGE
    // =========================================================

    private void SpawnFoliage(
        Vector2 position)
    {
        int randomIndex =
            Random.Range(
                0,
                foliagePrefabs.Length
            );

        GameObject prefab =
            foliagePrefabs[
                randomIndex
            ];

        if (prefab == null)
        {
            return;
        }

        GameObject newFoliage =
            Instantiate(
                prefab,
                position,
                Quaternion.identity,
                foliageParent
            );

        // =========================================
        // RANDOM SCALE
        // =========================================

        if (randomScale)
        {
            float scaleMultiplier =
                Random.Range(
                    minScaleMultiplier,
                    maxScaleMultiplier
                );

            newFoliage.transform.localScale *=
                scaleMultiplier;
        }

        // =========================================
        // RANDOM HORIZONTAL FLIP
        // =========================================

        if (randomFlipX)
        {
            SpriteRenderer sprite =
                newFoliage.GetComponent
                <SpriteRenderer>();

            if (sprite == null)
            {
                sprite =
                    newFoliage.GetComponentInChildren
                    <SpriteRenderer>();
            }

            if (sprite != null)
            {
                sprite.flipX =
                    Random.value > 0.5f;
            }
        }

        spawnedFoliage.Add(
            newFoliage
        );
    }

    // =========================================================
    // COLLISION CHECK
    // =========================================================

    private bool IsPositionBlocked(
        Vector2 position)
    {
        Collider2D hit =
            Physics2D.OverlapBox(
                position,
                Vector2.one *
                collisionCheckSize,
                0f,
                blockedLayers
            );

        return hit != null;
    }

    // =========================================================
    // CLEAR
    // =========================================================

    public void ClearFoliage()
    {
        for (
            int i =
                spawnedFoliage.Count - 1;
            i >= 0;
            i--)
        {
            if (spawnedFoliage[i] != null)
            {
                Destroy(
                    spawnedFoliage[i]
                );
            }
        }

        spawnedFoliage.Clear();
    }

    // =========================================================
    // DEBUG
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (gridSize <= 0f)
        {
            return;
        }

        float width =
            gridWidth *
            gridSize;

        float height =
            gridHeight *
            gridSize;

        float centerX =
            gridWidth % 2 == 0
                ? -gridSize * 0.5f
                : 0f;

        float centerY =
            gridHeight % 2 == 0
                ? -gridSize * 0.5f
                : 0f;

        Gizmos.DrawWireCube(
            transform.position +
            new Vector3(
                centerX,
                centerY,
                0f
            ),
            new Vector3(
                width,
                height,
                0f
            )
        );
    }
}