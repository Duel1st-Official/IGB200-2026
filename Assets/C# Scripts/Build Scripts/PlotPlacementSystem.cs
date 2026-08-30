using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PlotPlacementSystem : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    public SelectionWheel selectionWheel;
    public Camera mainCamera;
    public Transform player;
    public Animator playerAnimator;
    public PlayerMovement playerMovement;
    public CameraShake2D cameraShake;

    private bool plotPlacementActive = false;

    // =========================================================
    // PLOT
    // =========================================================

    [Header("Plot")]
    public GameObject plotPrefab;

    // =========================================================
    // PREVIEWS
    // =========================================================

    [Header("Preview Prefabs")]
    public GameObject validPreviewPrefab;
    public GameObject invalidPreviewPrefab;

    // =========================================================
    // GRID
    // =========================================================

    [Header("Grid")]
    public float gridSize = 1f;

    // =========================================================
    // BUILD AREA
    // =========================================================

    [Header("Build Area")]
    public int gridWidth = 18;
    public int gridHeight = 9;

    // =========================================================
    // PLAYER RANGE
    // =========================================================

    [Header("Placement Range")]
    public int maxPlacementBlocks = 5;

    // =========================================================
    // COLLISION
    // =========================================================

    [Header("Collision")]
    public LayerMask blockingLayers;
    public float checkSize = 0.8f;

    // =========================================================
    // GRASS
    // =========================================================

    [Header("Grass / Foliage")]
    public LayerMask grassLayers;
    public float grassBreakCheckSize = 0.9f;

    // =========================================================
    // SORTING
    // =========================================================

    [Header("Plot Sorting")]
    public int baseSortingOrder = 0;
    public int sortingOrderPerRow = 10;

    // =========================================================
    // ACTION
    // =========================================================

    [Header("Action")]
    public float placeActionDuration = 0.5f;

    // =========================================================
    // BUILD SCALE
    // =========================================================

    [Header("Build Scale Animation")]
    public float startingScale = 0.12f;
    public float smallScale = 0.35f;
    public float middleScale = 0.65f;

    // =========================================================
    // BUILD PARTICLES
    // =========================================================

    [Header("Build Particle Effect")]
    public GameObject buildParticlePrefab;

    public float buildParticleLifetime = 2f;

    public Vector3 buildParticleOffset =
        Vector3.zero;

    // =========================================================
    // PRIVATE
    // =========================================================

    private GameObject validPreview;
    private GameObject invalidPreview;

    private Vector2 currentGridPosition;

    private bool canPlace;

    private readonly HashSet<Vector2Int> occupiedCells =
        new HashSet<Vector2Int>();

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        CreatePreviews();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!plotPlacementActive)
        {
            HidePreviews();
            return;
        }

        // Selection wheel open.
        if (selectionWheel == null ||
            selectionWheel.IsWheelOpen())
        {
            HidePreviews();
            return;
        }

        // Not Build Mode.
        if (!selectionWheel.IsBuildMode())
        {
            HidePreviews();
            return;
        }

        UpdateGridPosition();

        CheckPlacement();

        UpdatePreview();

        // Player already doing something.
        if (playerMovement != null &&
            playerMovement.IsPerformingAction())
        {
            return;
        }

        // RIGHT CLICK
        if (Input.GetMouseButtonDown(1))
        {
            TryPlacePlot();
        }
    }

    // =========================================================
    // CREATE PREVIEWS
    // =========================================================

    private void CreatePreviews()
    {
        if (validPreviewPrefab != null)
        {
            validPreview =
                Instantiate(
                    validPreviewPrefab
                );

            validPreview.SetActive(false);
        }

        if (invalidPreviewPrefab != null)
        {
            invalidPreview =
                Instantiate(
                    invalidPreviewPrefab
                );

            invalidPreview.SetActive(false);
        }
    }

    // =========================================================
    // UPDATE GRID POSITION
    // =========================================================

    private void UpdateGridPosition()
    {
        if (mainCamera == null)
        {
            return;
        }

        Vector3 mouseWorldPosition =
            mainCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        mouseWorldPosition.z = 0f;

        float snappedX =
            Mathf.Round(
                mouseWorldPosition.x /
                gridSize
            ) * gridSize;

        float snappedY =
            Mathf.Round(
                mouseWorldPosition.y /
                gridSize
            ) * gridSize;

        currentGridPosition =
            new Vector2(
                snappedX,
                snappedY
            );
    }

    // =========================================================
    // GET GRID CELL
    // =========================================================

    public Vector2Int GetGridCell(
        Vector2 position)
    {
        int x =
            Mathf.RoundToInt(
                position.x /
                gridSize
            );

        int y =
            Mathf.RoundToInt(
                position.y /
                gridSize
            );

        return new Vector2Int(
            x,
            y
        );
    }

    // =========================================================
    // BUILD AREA
    // =========================================================

    private bool IsInsideBuildArea()
    {
        Vector2Int cell =
            GetGridCell(
                currentGridPosition
            );

        int minX =
            -(gridWidth / 2);

        int maxX =
            minX +
            gridWidth -
            1;

        int minY =
            -(gridHeight / 2);

        int maxY =
            minY +
            gridHeight -
            1;

        if (cell.x < minX ||
            cell.x > maxX)
        {
            return false;
        }

        if (cell.y < minY ||
            cell.y > maxY)
        {
            return false;
        }

        return true;
    }

    // =========================================================
    // PLAYER RANGE
    // =========================================================

    private bool IsWithinRange()
    {
        if (player == null)
        {
            return true;
        }

        float maxDistance =
            maxPlacementBlocks *
            gridSize;

        float distance =
            Vector2.Distance(
                player.position,
                currentGridPosition
            );

        return distance <=
               maxDistance;
    }

    // =========================================================
    // CHECK PLACEMENT
    // =========================================================

    private void CheckPlacement()
    {
        if (!IsInsideBuildArea())
        {
            canPlace = false;
            return;
        }

        if (!IsWithinRange())
        {
            canPlace = false;
            return;
        }

        Vector2Int cell =
            GetGridCell(
                currentGridPosition
            );

        // Already occupied.
        if (occupiedCells.Contains(cell))
        {
            canPlace = false;
            return;
        }

        // Collision check.
        Collider2D hit =
            Physics2D.OverlapBox(
                currentGridPosition,
                Vector2.one *
                checkSize,
                0f,
                blockingLayers
            );

        if (hit != null)
        {
            canPlace = false;
            return;
        }

        canPlace = true;
    }

    // =========================================================
    // PREVIEW
    // =========================================================

    private void UpdatePreview()
    {
        if (canPlace)
        {
            if (validPreview != null)
            {
                validPreview.SetActive(true);

                validPreview.transform.position =
                    currentGridPosition;
            }

            if (invalidPreview != null)
            {
                invalidPreview.SetActive(false);
            }
        }
        else
        {
            if (invalidPreview != null)
            {
                invalidPreview.SetActive(true);

                invalidPreview.transform.position =
                    currentGridPosition;
            }

            if (validPreview != null)
            {
                validPreview.SetActive(false);
            }
        }
    }

    // =========================================================
    // HIDE PREVIEWS
    // =========================================================

    private void HidePreviews()
    {
        if (validPreview != null)
        {
            validPreview.SetActive(false);
        }

        if (invalidPreview != null)
        {
            invalidPreview.SetActive(false);
        }
    }

    // =========================================================
    // PLACE
    // =========================================================

    private void TryPlacePlot()
    {
        if (selectionWheel != null &&
            selectionWheel.IsWheelOpen())
        {
            return;
        }

        if (playerMovement != null &&
            playerMovement.IsPerformingAction())
        {
            return;
        }

        if (!IsInsideBuildArea())
        {
            return;
        }

        if (!IsWithinRange())
        {
            return;
        }

        if (!canPlace)
        {
            return;
        }

        if (plotPrefab == null)
        {
            Debug.LogWarning(
                "Plot Prefab is missing."
            );

            return;
        }

        Vector2Int cell =
            GetGridCell(
                currentGridPosition
            );

        // =====================================================
        // FACE PLOT
        // =====================================================

        FaceActionPosition(
            currentGridPosition
        );

        // =====================================================
        // LOCK PLAYER
        // =====================================================

        if (playerMovement != null)
        {
            playerMovement.StartAction(
                placeActionDuration
            );
        }

        // =====================================================
        // PLAYER ANIMATION
        // =====================================================

        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger(
                "PlaceAction"
            );

            playerAnimator.SetTrigger(
                "PlaceAction"
            );
        }

        // =====================================================
        // CREATE PLOT
        // =====================================================

        GameObject newPlot =
            Instantiate(
                plotPrefab,
                currentGridPosition,
                Quaternion.identity
            );

        // =====================================================
        // SORTING
        // =====================================================

        SetPlotSorting(
            newPlot,
            cell
        );

        // =====================================================
        // SCALE
        // =====================================================

        Vector3 finalScale =
            newPlot.transform.localScale;

        newPlot.transform.localScale =
            finalScale *
            startingScale;

        // =====================================================
        // REMOVABLE
        // =====================================================

        RemovableBuildItem removable =
            newPlot.GetComponent
            <RemovableBuildItem>();

        if (removable != null)
        {
            removable.Setup(
                cell,
                this
            );
        }

        // =====================================================
        // OCCUPY CELL
        // =====================================================

        occupiedCells.Add(cell);

        HidePreviews();

        // =====================================================
        // BUILD ROUTINE
        // =====================================================

        StartCoroutine(
            BuildPlotRoutine(
                newPlot,
                finalScale
            )
        );

        CheckPlacement();
    }

    // =========================================================
    // SORTING
    // =========================================================

    private void SetPlotSorting(
        GameObject plot,
        Vector2Int cell)
    {
        if (plot == null)
        {
            return;
        }

        SortingGroup sortingGroup =
            plot.GetComponent<SortingGroup>();

        if (sortingGroup == null)
        {
            sortingGroup =
                plot.AddComponent<SortingGroup>();
        }

        int calculatedOrder =
            baseSortingOrder -
            (
                cell.y *
                sortingOrderPerRow
            );

        sortingGroup.sortingOrder =
            calculatedOrder;
    }

    // =========================================================
    // FIND GRASS AND START SHAKE
    // =========================================================

    private InteractiveGrass[] StartGrassBuildShake(
        Vector2 position)
    {
        Collider2D[] hits =
            Physics2D.OverlapBoxAll(
                position,
                Vector2.one *
                grassBreakCheckSize,
                0f,
                grassLayers
            );

        HashSet<InteractiveGrass> foundGrass =
            new HashSet<InteractiveGrass>();

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            InteractiveGrass grass =
                hit.GetComponent
                <InteractiveGrass>();

            if (grass == null)
            {
                grass =
                    hit.GetComponentInParent
                    <InteractiveGrass>();
            }

            if (grass != null)
            {
                foundGrass.Add(grass);
            }
        }

        InteractiveGrass[] grassArray =
            new InteractiveGrass[
                foundGrass.Count
            ];

        foundGrass.CopyTo(
            grassArray
        );

        foreach (
            InteractiveGrass grass
            in grassArray)
        {
            if (grass != null)
            {
                grass.StartBuildShake();
            }
        }

        return grassArray;
    }

    // =========================================================
    // BUILD ROUTINE
    // =========================================================

    private IEnumerator BuildPlotRoutine(
        GameObject plot,
        Vector3 finalScale)
    {
        if (plot == null)
        {
            yield break;
        }

        // Grass underneath starts shaking.
        InteractiveGrass[] shakingGrass =
            StartGrassBuildShake(
                plot.transform.position
            );

        float duration =
            Mathf.Max(
                placeActionDuration,
                0.01f
            );

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    timer /
                    duration
                );

            float scaleMultiplier;

            // =================================================
            // STAGE 1
            // TINY -> SMALL
            // =================================================

            if (progress < 0.25f)
            {
                float t =
                    progress /
                    0.25f;

                scaleMultiplier =
                    Mathf.Lerp(
                        startingScale,
                        smallScale,
                        EaseOutCubic(t)
                    );
            }

            // =================================================
            // STAGE 2
            // SMALL -> MEDIUM
            // =================================================

            else if (progress < 0.60f)
            {
                float t =
                    (progress - 0.25f) /
                    0.35f;

                scaleMultiplier =
                    Mathf.Lerp(
                        smallScale,
                        middleScale,
                        EaseOutCubic(t)
                    );
            }

            // =================================================
            // STAGE 3
            // MEDIUM -> FULL
            // =================================================

            else
            {
                float t =
                    (progress - 0.60f) /
                    0.40f;

                scaleMultiplier =
                    Mathf.Lerp(
                        middleScale,
                        1f,
                        EaseOutExpo(t)
                    );
            }

            if (plot != null)
            {
                plot.transform.localScale =
                    finalScale *
                    scaleMultiplier;
            }

            yield return null;
        }

        // =====================================================
        // FINAL SCALE
        // =====================================================

        if (plot != null)
        {
            plot.transform.localScale =
                finalScale;
        }

        // =====================================================
        // BREAK GRASS
        // =====================================================

        foreach (
            InteractiveGrass grass
            in shakingGrass)
        {
            if (grass != null)
            {
                grass.BreakGrass();
            }
        }

        // =====================================================
        // BUILD PARTICLE
        // =====================================================

        if (plot != null &&
            buildParticlePrefab != null)
        {
            GameObject particles =
                Instantiate(
                    buildParticlePrefab,

                    plot.transform.position +
                    buildParticleOffset,

                    // Never inherit plot rotation.
                    Quaternion.identity
                );

            Destroy(
                particles,
                buildParticleLifetime
            );
        }

        // =====================================================
        // CAMERA SHAKE
        // =====================================================

        if (cameraShake != null)
        {
            cameraShake.Shake();
        }
    }

    // =========================================================
    // EASING
    // =========================================================

    private float EaseOutCubic(
        float t)
    {
        t =
            Mathf.Clamp01(t);

        return 1f -
               Mathf.Pow(
                   1f - t,
                   3f
               );
    }

    private float EaseOutExpo(
        float t)
    {
        t =
            Mathf.Clamp01(t);

        if (t >= 1f)
        {
            return 1f;
        }

        return 1f -
               Mathf.Pow(
                   2f,
                   -10f * t
               );
    }

    // =========================================================
    // FACE ACTION POSITION
    // =========================================================

    private void FaceActionPosition(
        Vector2 targetPosition)
    {
        if (player == null ||
            playerAnimator == null)
        {
            return;
        }

        Vector2 direction =
            targetPosition -
            (Vector2)player.position;

        if (direction.sqrMagnitude <=
            0.001f)
        {
            return;
        }

        if (Mathf.Abs(direction.x) >
            Mathf.Abs(direction.y))
        {
            if (direction.x > 0f)
            {
                SetAnimatorDirection(
                    1f,
                    0f
                );
            }
            else
            {
                SetAnimatorDirection(
                    -1f,
                    0f
                );
            }
        }
        else
        {
            if (direction.y > 0f)
            {
                SetAnimatorDirection(
                    0f,
                    1f
                );
            }
            else
            {
                SetAnimatorDirection(
                    0f,
                    -1f
                );
            }
        }
    }

    // =========================================================
    // ANIMATOR DIRECTION
    // =========================================================

    private void SetAnimatorDirection(
        float x,
        float y)
    {
        playerAnimator.SetFloat(
            "LastMoveX",
            x
        );

        playerAnimator.SetFloat(
            "LastMoveY",
            y
        );
    }

    // =========================================================
    // FREE CELL
    // =========================================================

    public void FreeGridCell(
        Vector2Int cell)
    {
        occupiedCells.Remove(cell);
    }

    // =========================================================
    // DEBUG
    // =========================================================

    public void ActivatePlotPlacement()
    {
        plotPlacementActive = true;
    }

    public void DeactivatePlotPlacement()
    {
        plotPlacementActive = false;
        HidePreviews();
    }

    public bool IsPlotPlacementActive()
    {
        return plotPlacementActive;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(
            currentGridPosition,
            Vector3.one *
            checkSize
        );

        if (player != null)
        {
            float radius =
                maxPlacementBlocks *
                gridSize;

            Gizmos.DrawWireSphere(
                player.position,
                radius
            );
        }

        if (gridSize <= 0f)
        {
            return;
        }

        float worldWidth =
            gridWidth *
            gridSize;

        float worldHeight =
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
            new Vector3(
                centerX,
                centerY,
                0f
            ),
            new Vector3(
                worldWidth,
                worldHeight,
                0f
            )
        );
    }
}