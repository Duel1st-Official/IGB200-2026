using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class WaterPlotPlacementSystem : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private SelectionWheel selectionWheel;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform player;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private CameraShake2D cameraShake;

    // =========================================================
    // WATER PLOT PREFAB
    // =========================================================

    [Header("Water Plot")]
    [SerializeField] private GameObject waterPlotPrefab;

    // =========================================================
    // PREVIEWS
    // =========================================================

    [Header("Placement Previews")]
    [SerializeField] private GameObject validPreviewPrefab;
    [SerializeField] private GameObject invalidPreviewPrefab;

    // =========================================================
    // GRID
    // =========================================================

    [Header("Grid")]
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private int gridWidth = 18;
    [SerializeField] private int gridHeight = 9;

    // =========================================================
    // RANGE
    // =========================================================

    [Header("Placement Range")]
    [SerializeField] private float maxPlacementBlocks = 5f;

    // =========================================================
    // COLLISION
    // =========================================================

    [Header("Collision")]
    [Tooltip("Include Structure, PlacedObject and anything else water plots cannot overlap.")]
    [SerializeField] private LayerMask blockingLayers;

    [SerializeField] private float checkSize = 0.9f;

    // =========================================================
    // GRASS / FOLIAGE
    // =========================================================

    [Header("Grass / Foliage")]
    [SerializeField] private LayerMask grassLayers;
    [SerializeField] private float grassBreakCheckSize = 0.9f;

    // =========================================================
    // PLACEMENT ACTIVE
    // =========================================================

    [Header("Water Plot Placement")]
    [SerializeField] private bool waterPlotPlacementActive = false;

    // =========================================================
    // ACTION
    // =========================================================

    [Header("Place Action")]
    [SerializeField] private float placeActionDuration = 0.5f;

    // =========================================================
    // GROWTH
    // =========================================================

    [Header("Build Animation")]
    [SerializeField]
    private Vector3 startingScale =
        new Vector3(0.15f, 0.15f, 1f);

    [SerializeField]
    private Vector3 smallScale =
        new Vector3(0.45f, 0.45f, 1f);

    [SerializeField]
    private Vector3 middleScale =
        new Vector3(0.75f, 0.75f, 1f);

    // =========================================================
    // DIRT REVEAL
    // =========================================================

    [Header("Dirt Reveal")]
    [Tooltip("Single transparent dirt sprite spawned underneath the water plot after placement finishes.")]
    [SerializeField] private Sprite dirtSprite;

    [Tooltip("Optional material. Leave empty to use the normal sprite material.")]
    [SerializeField] private Material dirtMaterial;

    [SerializeField] private Vector3 dirtOffset = Vector3.zero;

    [Tooltip("Absolute sorting order used by every dirt sprite. Keep lower than all placed objects.")]
    [SerializeField] private int dirtSortingOrder = -1000;

    [Range(0.01f, 1f)]
    [SerializeField] private float dirtStartingScale = 0.08f;

    [Min(0.01f)]
    [SerializeField] private float dirtRevealDuration = 0.28f;

    [Range(1f, 1.3f)]
    [SerializeField] private float dirtOvershoot = 1.07f;

    [SerializeField] private Vector2 dirtFinalScale = Vector2.one;

    [SerializeField] private bool dirtHorizontalSpread = true;

    [Range(0f, 0.35f)]
    [SerializeField] private float dirtHorizontalLead = 0.12f;

    [Min(0f)]
    [SerializeField] private float dirtFadeDelay = 0.15f;

    [Min(0.05f)]
    [SerializeField] private float dirtFadeDuration = 1.5f;

    [Header("Dirt Placement Particles")]
    [SerializeField] private GameObject dirtParticlePrefab;

    [Min(0f)]
    [SerializeField] private float dirtParticleLifetime = 1.5f;

    [SerializeField] private Vector3 dirtParticleOffset = Vector3.zero;

    // =========================================================
    // PARTICLE
    // =========================================================

    [Header("Placement Effect")]
    [SerializeField] private GameObject buildParticlePrefab;
    [SerializeField] private float buildParticleLifetime = 2f;
    [SerializeField] private Vector3 buildParticleOffset = Vector3.zero;

    // =========================================================
    // WATER PLOT BUILD AUDIO
    // =========================================================

    [Header("Water Plot Build Audio")]

    [Tooltip("AudioSource used for Water Plot placement sounds.")]
    [SerializeField] private AudioSource buildAudioSource;

    [Tooltip("Random Water Plot build sound played when placement finishes.")]
    [SerializeField]
    private AudioClip[] waterPlotBuildSounds =
        new AudioClip[3];

    [Range(0f, 1f)]
    [SerializeField] private float waterPlotBuildVolume = 1f;

    [SerializeField] private float waterPlotBuildPitchMin = 0.95f;
    [SerializeField] private float waterPlotBuildPitchMax = 1.05f;

    // =========================================================
    // SORTING
    // =========================================================

    [Header("Sorting")]
    [SerializeField] private int baseSortingOrder = 10;
    [SerializeField] private int sortingOrderPerRow = 1;

    // =========================================================
    // PRIVATE
    // =========================================================

    private GameObject validPreview;
    private GameObject invalidPreview;

    private Vector2 currentGridPosition;

    private bool placementValid;
    private bool currentlyPlacing;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // =====================================================
        // CAMERA
        // =====================================================

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // =====================================================
        // SELECTION WHEEL
        // =====================================================

        if (selectionWheel == null)
        {
            selectionWheel =
                FindFirstObjectByType<SelectionWheel>();
        }

        // =====================================================
        // PLAYER
        // =====================================================

        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player =
                    playerObject.transform;

                if (playerAnimator == null)
                {
                    playerAnimator =
                        playerObject.GetComponent<Animator>();
                }

                if (playerMovement == null)
                {
                    playerMovement =
                        playerObject.GetComponent<PlayerMovement>();
                }
            }
        }

        // =====================================================
        // CAMERA SHAKE
        // =====================================================

        if (cameraShake == null)
        {
            cameraShake =
                FindFirstObjectByType<CameraShake2D>();
        }

        // =====================================================
        // AUDIO SOURCE
        // =====================================================

        if (buildAudioSource == null)
        {
            buildAudioSource =
                GetComponent<AudioSource>();
        }

        // =====================================================
        // VALID PREVIEW
        // =====================================================

        if (validPreviewPrefab != null)
        {
            validPreview =
                Instantiate(validPreviewPrefab);

            validPreview.SetActive(false);
        }

        // =====================================================
        // INVALID PREVIEW
        // =====================================================

        if (invalidPreviewPrefab != null)
        {
            invalidPreview =
                Instantiate(invalidPreviewPrefab);

            invalidPreview.SetActive(false);
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // Water plot not selected
        if (!waterPlotPlacementActive)
        {
            HidePreviews();
            return;
        }

        // Must be in Build Mode
        if (!IsBuildMode())
        {
            HidePreviews();
            return;
        }

        // Don't place while selection wheel is open
        if (selectionWheel != null &&
            selectionWheel.IsWheelOpen())
        {
            HidePreviews();
            return;
        }

        // Don't allow repeated placement during animation
        if (currentlyPlacing)
        {
            HidePreviews();
            return;
        }

        UpdateGridPosition();

        placementValid =
            CheckPlacement();

        UpdatePreview();

        // Left click to place
        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceWaterPlot();
        }
    }

    // =========================================================
    // BUILD MODE
    // =========================================================

    private bool IsBuildMode()
    {
        if (selectionWheel == null)
        {
            return false;
        }

        return selectionWheel.IsBuildMode();
    }

    // =========================================================
    // GRID POSITION
    // =========================================================

    private void UpdateGridPosition()
    {
        if (mainCamera == null)
        {
            return;
        }

        Vector3 mouseWorld =
            mainCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        mouseWorld.z = 0f;

        float snappedX =
            Mathf.Round(
                mouseWorld.x / gridSize
            ) * gridSize;

        float snappedY =
            Mathf.Round(
                mouseWorld.y / gridSize
            ) * gridSize;

        currentGridPosition =
            new Vector2(
                snappedX,
                snappedY
            );
    }

    // =========================================================
    // CHECK PLACEMENT
    // =========================================================

    private bool CheckPlacement()
    {
        if (!IsInsideBuildArea(
            currentGridPosition))
        {
            return false;
        }

        if (!IsWithinRange(
            currentGridPosition))
        {
            return false;
        }

        Collider2D blockingObject =
            Physics2D.OverlapBox(
                currentGridPosition,
                Vector2.one * checkSize,
                0f,
                blockingLayers
            );

        if (blockingObject != null)
        {
            return false;
        }

        return true;
    }

    // =========================================================
    // BUILD AREA
    // =========================================================

    private bool IsInsideBuildArea(
        Vector2 position)
    {
        Vector2Int cell =
            GetGridCell(position);

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

        return
            cell.x >= minX &&
            cell.x <= maxX &&
            cell.y >= minY &&
            cell.y <= maxY;
    }

    // =========================================================
    // RANGE
    // =========================================================

    private bool IsWithinRange(
        Vector2 position)
    {
        if (player == null)
        {
            return false;
        }

        float maxDistance =
            maxPlacementBlocks *
            gridSize;

        float distance =
            Vector2.Distance(
                player.position,
                position
            );

        return distance <=
               maxDistance;
    }

    // =========================================================
    // GRID CELL
    // =========================================================

    private Vector2Int GetGridCell(
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
    // PREVIEW
    // =========================================================

    private void UpdatePreview()
    {
        if (placementValid)
        {
            if (validPreview != null)
            {
                validPreview.transform.position =
                    currentGridPosition;

                validPreview.SetActive(true);
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
                invalidPreview.transform.position =
                    currentGridPosition;

                invalidPreview.SetActive(true);
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
    // TRY PLACE
    // =========================================================

    private void TryPlaceWaterPlot()
    {
        if (!placementValid)
        {
            return;
        }

        if (waterPlotPrefab == null)
        {
            Debug.LogWarning(
                "WaterPlotPlacementSystem has no Water Plot Prefab."
            );

            return;
        }

        if (currentlyPlacing)
        {
            return;
        }

        StartCoroutine(
            PlaceWaterPlotRoutine()
        );
    }

    // =========================================================
    // PLACE ROUTINE
    // =========================================================

    private IEnumerator PlaceWaterPlotRoutine()
    {
        currentlyPlacing = true;

        Vector2 placementPosition =
            currentGridPosition;

        HidePreviews();

        // =====================================================
        // FIND GRASS UNDER WATER PLOT
        // =====================================================

        Collider2D[] grassHits =
            Physics2D.OverlapBoxAll(
                placementPosition,
                Vector2.one *
                grassBreakCheckSize,
                0f,
                grassLayers
            );

        // =====================================================
        // SHAKE GRASS
        // =====================================================

        foreach (Collider2D hit in grassHits)
        {
            if (hit == null)
            {
                continue;
            }

            InteractiveGrass grass =
                hit.GetComponent<InteractiveGrass>();

            if (grass == null)
            {
                grass =
                    hit.GetComponentInParent<InteractiveGrass>();
            }

            if (grass != null)
            {
                grass.StartBuildShake();
            }
        }

        // =====================================================
        // FACE PLAYER
        // =====================================================

        FacePlayerTowards(
            placementPosition
        );

        // =====================================================
        // PLAYER ACTION
        // =====================================================

        if (playerMovement != null)
        {
            playerMovement.StartAction(
                placeActionDuration
            );
        }

        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(
                "PlaceAction"
            );
        }

        // =====================================================
        // SPAWN WATER PLOT
        // =====================================================

        GameObject newWaterPlot =
            Instantiate(
                waterPlotPrefab,
                placementPosition,
                Quaternion.identity
            );

        // =====================================================
        // SORTING
        // =====================================================

        Vector2Int cell =
            GetGridCell(
                placementPosition
            );

        SetWaterPlotSorting(
            newWaterPlot,
            cell
        );

        // =====================================================
        // START SMALL
        // =====================================================

        Vector3 finalScale =
            newWaterPlot.transform.localScale;

        newWaterPlot.transform.localScale =
            Vector3.Scale(
                finalScale,
                startingScale
            );

        // =====================================================
        // GROW
        // =====================================================

        yield return StartCoroutine(
            AnimateWaterPlotGrowth(
                newWaterPlot.transform,
                finalScale
            )
        );

        // =====================================================
        // DIRT - AFTER WATER PLOT IS FULLY PLACED
        // =====================================================

        GameObject dirtVisual =
            CreateDirtVisual(
                newWaterPlot
            );

        if (dirtVisual != null)
        {
            StartCoroutine(
                AnimateDirtReveal(
                    dirtVisual
                )
            );
        }

        SpawnDirtParticles(
            newWaterPlot.transform.position
        );

        // =====================================================
        // BUILD SOUND
        // =====================================================

        PlayWaterPlotBuildSound();

        // =====================================================
        // BREAK GRASS
        // =====================================================

        foreach (Collider2D hit in grassHits)
        {
            if (hit == null)
            {
                continue;
            }

            InteractiveGrass grass =
                hit.GetComponent<InteractiveGrass>();

            if (grass == null)
            {
                grass =
                    hit.GetComponentInParent<InteractiveGrass>();
            }

            if (grass != null)
            {
                grass.BreakGrass();
            }
        }

        // =====================================================
        // PARTICLE
        // =====================================================

        if (buildParticlePrefab != null)
        {
            GameObject particle =
                Instantiate(
                    buildParticlePrefab,
                    newWaterPlot.transform.position +
                    buildParticleOffset,
                    Quaternion.identity
                );

            Destroy(
                particle,
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

        currentlyPlacing = false;
    }

    // =========================================================
    // GROWTH ANIMATION
    // =========================================================

    private IEnumerator AnimateWaterPlotGrowth(
        Transform waterPlotTransform,
        Vector3 finalScale)
    {
        if (waterPlotTransform == null)
        {
            yield break;
        }

        float totalDuration =
            Mathf.Max(
                0.01f,
                placeActionDuration
            );

        float stageOneDuration =
            totalDuration * 0.25f;

        float stageTwoDuration =
            totalDuration * 0.35f;

        float stageThreeDuration =
            totalDuration * 0.40f;

        Vector3 start =
            Vector3.Scale(
                finalScale,
                startingScale
            );

        Vector3 small =
            Vector3.Scale(
                finalScale,
                smallScale
            );

        Vector3 middle =
            Vector3.Scale(
                finalScale,
                middleScale
            );

        // Tiny -> Small
        yield return ScaleStage(
            waterPlotTransform,
            start,
            small,
            stageOneDuration,
            false
        );

        // Small -> Medium
        yield return ScaleStage(
            waterPlotTransform,
            small,
            middle,
            stageTwoDuration,
            false
        );

        // Medium -> Full
        yield return ScaleStage(
            waterPlotTransform,
            middle,
            finalScale,
            stageThreeDuration,
            true
        );

        if (waterPlotTransform != null)
        {
            waterPlotTransform.localScale =
                finalScale;
        }
    }

    // =========================================================
    // SCALE STAGE
    // =========================================================

    private IEnumerator ScaleStage(
        Transform target,
        Vector3 from,
        Vector3 to,
        float duration,
        bool useExpo)
    {
        float timer = 0f;

        while (timer < duration)
        {
            if (target == null)
            {
                yield break;
            }

            timer +=
                Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    duration
                );

            float easedT;

            if (useExpo)
            {
                easedT =
                    EaseOutExpo(t);
            }
            else
            {
                easedT =
                    EaseOutCubic(t);
            }

            target.localScale =
                Vector3.LerpUnclamped(
                    from,
                    to,
                    easedT
                );

            yield return null;
        }

        if (target != null)
        {
            target.localScale =
                to;
        }
    }

    // =========================================================
    // CREATE DIRT
    // =========================================================

    private GameObject CreateDirtVisual(
        GameObject placedObject)
    {
        if (placedObject == null ||
            dirtSprite == null)
        {
            return null;
        }

        GameObject dirt =
            new GameObject(
                "Dirt Visual"
            );

        dirt.transform.position =
            placedObject.transform.position +
            dirtOffset;

        dirt.transform.rotation =
            Quaternion.identity;

        dirt.transform.localScale =
            new Vector3(
                dirtFinalScale.x * dirtStartingScale,
                dirtFinalScale.y * dirtStartingScale,
                1f
            );

        SpriteRenderer renderer =
            dirt.AddComponent<SpriteRenderer>();

        renderer.sprite = dirtSprite;

        if (dirtMaterial != null)
        {
            renderer.material = dirtMaterial;
        }

        SortingGroup placedSorting =
            placedObject.GetComponent<SortingGroup>();

        if (placedSorting != null)
        {
            renderer.sortingLayerID =
                placedSorting.sortingLayerID;
        }
        else
        {
            SpriteRenderer placedRenderer =
                placedObject.GetComponentInChildren<SpriteRenderer>();

            if (placedRenderer != null)
            {
                renderer.sortingLayerID =
                    placedRenderer.sortingLayerID;
            }
        }

        // Absolute low order means dirt can never cover another
        // plot, trap, or water plot on a different grid row.
        renderer.sortingOrder =
            dirtSortingOrder;

        Color startColor =
            renderer.color;

        startColor.a = 0f;
        renderer.color = startColor;

        WaterPlotDirtFadeAfterRemoved fadeWatcher =
            dirt.AddComponent<WaterPlotDirtFadeAfterRemoved>();

        fadeWatcher.Setup(
            placedObject,
            renderer,
            dirtFadeDelay,
            dirtFadeDuration
        );

        return dirt;
    }

    // =========================================================
    // ANIMATE DIRT
    // =========================================================

    private IEnumerator AnimateDirtReveal(
        GameObject dirt)
    {
        if (dirt == null)
        {
            yield break;
        }

        SpriteRenderer renderer =
            dirt.GetComponent<SpriteRenderer>();

        if (renderer == null)
        {
            yield break;
        }

        Vector3 finalScale =
            new Vector3(
                dirtFinalScale.x,
                dirtFinalScale.y,
                1f
            );

        Vector3 startScale =
            new Vector3(
                finalScale.x * dirtStartingScale,
                finalScale.y * dirtStartingScale,
                1f
            );

        Vector3 overshootScale =
            finalScale * dirtOvershoot;

        overshootScale.z = 1f;

        Color finalColor = renderer.color;
        finalColor.a = 1f;

        float growDuration =
            Mathf.Max(
                0.01f,
                dirtRevealDuration * 0.72f
            );

        float settleDuration =
            Mathf.Max(
                0.01f,
                dirtRevealDuration - growDuration
            );

        float timer = 0f;

        while (timer < growDuration)
        {
            if (dirt == null)
            {
                yield break;
            }

            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer / growDuration
                );

            float xT =
                dirtHorizontalSpread
                    ? Mathf.Clamp01(t + dirtHorizontalLead)
                    : t;

            float easedX = EaseOutCubic(xT);
            float easedY = EaseOutCubic(t);

            dirt.transform.localScale =
                new Vector3(
                    Mathf.Lerp(
                        startScale.x,
                        overshootScale.x,
                        easedX
                    ),
                    Mathf.Lerp(
                        startScale.y,
                        overshootScale.y,
                        easedY
                    ),
                    1f
                );

            Color color = finalColor;
            color.a = EaseOutCubic(t);
            renderer.color = color;

            yield return null;
        }

        timer = 0f;

        while (timer < settleDuration)
        {
            if (dirt == null)
            {
                yield break;
            }

            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer / settleDuration
                );

            dirt.transform.localScale =
                Vector3.Lerp(
                    overshootScale,
                    finalScale,
                    EaseOutCubic(t)
                );

            yield return null;
        }

        if (dirt != null)
        {
            dirt.transform.localScale = finalScale;
            renderer.color = finalColor;
        }

        // Dirt deliberately remains unparented so removal shake
        // on the placed object can never move the dirt.
    }

    // =========================================================
    // DIRT PARTICLES
    // =========================================================

    private void SpawnDirtParticles(
        Vector3 position)
    {
        if (dirtParticlePrefab == null)
        {
            return;
        }

        GameObject particles =
            Instantiate(
                dirtParticlePrefab,
                position + dirtParticleOffset,
                Quaternion.identity
            );

        if (dirtParticleLifetime > 0f)
        {
            Destroy(
                particles,
                dirtParticleLifetime
            );
        }
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

    private float EaseOutExpo(
        float x)
    {
        if (x >= 1f)
        {
            return 1f;
        }

        return
            1f -
            Mathf.Pow(
                2f,
                -10f * x
            );
    }

    // =========================================================
    // FACE PLAYER
    // =========================================================

    private void FacePlayerTowards(
        Vector2 target)
    {
        if (player == null ||
            playerAnimator == null)
        {
            return;
        }

        Vector2 direction =
            target -
            (Vector2)player.position;

        if (direction.sqrMagnitude <=
            0.001f)
        {
            return;
        }

        if (Mathf.Abs(direction.x) >
            Mathf.Abs(direction.y))
        {
            float x =
                direction.x > 0f
                ? 1f
                : -1f;

            playerAnimator.SetFloat(
                "LastMoveX",
                x
            );

            playerAnimator.SetFloat(
                "LastMoveY",
                0f
            );
        }
        else
        {
            float y =
                direction.y > 0f
                ? 1f
                : -1f;

            playerAnimator.SetFloat(
                "LastMoveX",
                0f
            );

            playerAnimator.SetFloat(
                "LastMoveY",
                y
            );
        }
    }

    // =========================================================
    // SORTING
    // =========================================================

    private void SetWaterPlotSorting(
        GameObject waterPlot,
        Vector2Int cell)
    {
        if (waterPlot == null)
        {
            return;
        }

        SortingGroup sortingGroup =
            waterPlot.GetComponent<SortingGroup>();

        if (sortingGroup == null)
        {
            sortingGroup =
                waterPlot.AddComponent<SortingGroup>();
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
    // WATER PLOT BUILD SOUND
    // =========================================================

    private void PlayWaterPlotBuildSound()
    {
        if (buildAudioSource == null)
        {
            return;
        }

        if (waterPlotBuildSounds == null ||
            waterPlotBuildSounds.Length == 0)
        {
            return;
        }

        int validClipCount = 0;

        // Count non-null sounds.
        for (int i = 0;
             i < waterPlotBuildSounds.Length;
             i++)
        {
            if (waterPlotBuildSounds[i] != null)
            {
                validClipCount++;
            }
        }

        if (validClipCount <= 0)
        {
            return;
        }

        // Pick one of the valid sounds.
        int randomValidIndex =
            Random.Range(
                0,
                validClipCount
            );

        AudioClip selectedClip = null;
        int currentValidIndex = 0;

        for (int i = 0;
             i < waterPlotBuildSounds.Length;
             i++)
        {
            if (waterPlotBuildSounds[i] == null)
            {
                continue;
            }

            if (currentValidIndex ==
                randomValidIndex)
            {
                selectedClip =
                    waterPlotBuildSounds[i];

                break;
            }

            currentValidIndex++;
        }

        if (selectedClip == null)
        {
            return;
        }

        buildAudioSource.pitch =
            Random.Range(
                waterPlotBuildPitchMin,
                waterPlotBuildPitchMax
            );

        buildAudioSource.PlayOneShot(
            selectedClip,
            waterPlotBuildVolume
        );
    }

    // =========================================================
    // ACTIVATE WATER PLOT
    // =========================================================

    public void ActivateWaterPlotPlacement()
    {
        waterPlotPlacementActive =
            true;
    }

    // =========================================================
    // DEACTIVATE WATER PLOT
    // =========================================================

    public void DeactivateWaterPlotPlacement()
    {
        waterPlotPlacementActive =
            false;

        HidePreviews();
    }

    // =========================================================
    // SET ACTIVE
    // =========================================================

    public void SetWaterPlotPlacementActive(
        bool active)
    {
        waterPlotPlacementActive =
            active;

        if (!active)
        {
            HidePreviews();
        }
    }

    // =========================================================
    // IS ACTIVE
    // =========================================================

    public bool IsWaterPlotPlacementActive()
    {
        return waterPlotPlacementActive;
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (gridSize <= 0f)
        {
            return;
        }

        Vector3 size =
            new Vector3(
                gridWidth * gridSize,
                gridHeight * gridSize,
                0f
            );

        Gizmos.DrawWireCube(
            Vector3.zero,
            size
        );
    }
}

// ============================================================================
// DIRT FADE AFTER WATER PLOT REMOVAL
// ============================================================================

public class WaterPlotDirtFadeAfterRemoved : MonoBehaviour
{
    private GameObject watchedObject;
    private SpriteRenderer dirtRenderer;
    private float fadeDelay = 0.15f;
    private float fadeDuration = 1.5f;
    private bool fadeStarted = false;

    public void Setup(
        GameObject placedObject,
        SpriteRenderer renderer,
        float delay,
        float duration)
    {
        watchedObject = placedObject;
        dirtRenderer = renderer;
        fadeDelay = Mathf.Max(0f, delay);
        fadeDuration = Mathf.Max(0.05f, duration);
    }

    private void Update()
    {
        if (fadeStarted)
        {
            return;
        }

        // Dirt remains still throughout the object's removal animation.
        // Fade starts only after the placed object has actually been destroyed.
        if (watchedObject == null)
        {
            fadeStarted = true;
            StartCoroutine(FadeAwayRoutine());
        }
    }

    private IEnumerator FadeAwayRoutine()
    {
        if (fadeDelay > 0f)
        {
            yield return new WaitForSeconds(fadeDelay);
        }

        if (dirtRenderer == null)
        {
            Destroy(gameObject);
            yield break;
        }

        Color startingColor = dirtRenderer.color;
        float startingAlpha = startingColor.a;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer / fadeDuration
                );

            float smoothT =
                t * t * (3f - (2f * t));

            Color color = startingColor;

            color.a =
                Mathf.Lerp(
                    startingAlpha,
                    0f,
                    smoothT
                );

            dirtRenderer.color = color;

            yield return null;
        }

        Color finalColor = dirtRenderer.color;
        finalColor.a = 0f;
        dirtRenderer.color = finalColor;

        Destroy(gameObject);
    }
}

