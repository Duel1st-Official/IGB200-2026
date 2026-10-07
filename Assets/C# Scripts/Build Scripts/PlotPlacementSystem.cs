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
    // DIRT PARTICLES
    // =========================================================

    private void SpawnDirtParticles(
        Vector3 plotPosition)
    {
        if (dirtParticlePrefab == null)
        {
            return;
        }

        GameObject particles =
            Instantiate(
                dirtParticlePrefab,
                plotPosition +
                dirtParticleOffset,
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
    // CREATE DIRT VISUAL
    // =========================================================

    private GameObject CreateDirtVisual(
        GameObject plot,
        Vector2Int cell)
    {
        if (plot == null ||
            dirtSprite == null)
        {
            return null;
        }

        GameObject dirt =
            new GameObject(
                "Dirt Visual"
            );

        dirt.transform.position =
            plot.transform.position +
            dirtOffset;

        dirt.transform.rotation =
            Quaternion.identity;

        dirt.transform.localScale =
            new Vector3(
                dirtFinalScale.x *
                dirtStartingScale,

                dirtFinalScale.y *
                dirtStartingScale,

                1f
            );

        SpriteRenderer renderer =
            dirt.AddComponent<SpriteRenderer>();

        renderer.sprite =
            dirtSprite;

        Material resolvedDirtMaterial = GetDirtLightingMaterial();

        if (resolvedDirtMaterial != null)
        {
            renderer.sharedMaterial = resolvedDirtMaterial;
        }

        // Use the same sorting layer as the plot, but give EVERY dirt
        // sprite the same very-low absolute order. This prevents dirt from
        // ever drawing over another plot on a different grid row.
        SortingGroup plotSortingGroup =
            plot.GetComponent<SortingGroup>();

        if (plotSortingGroup != null)
        {
            renderer.sortingLayerID =
                plotSortingGroup.sortingLayerID;
        }
        else
        {
            SpriteRenderer plotRenderer =
                plot.GetComponentInChildren<SpriteRenderer>();

            if (plotRenderer != null)
            {
                renderer.sortingLayerID =
                    plotRenderer.sortingLayerID;
            }
        }

        renderer.sortingOrder =
            dirtSortingOrder;

        Color startColor =
            renderer.color;

        startColor.a = 0f;

        renderer.color =
            startColor;

        // Keep dirt independent so plot removal shake never affects it.
        DirtFadeAfterPlotRemoved fadeWatcher =
            dirt.AddComponent<DirtFadeAfterPlotRemoved>();

        fadeWatcher.Setup(
            plot,
            renderer,
            dirtFadeDelay,
            dirtFadeDuration
        );

        return dirt;
    }

    // =========================================================
    // DIRT REVEAL ANIMATION
    // =========================================================
    // =========================================================
    // DIRT 2D LIGHTING MATERIAL
    // =========================================================

    private Material GetDirtLightingMaterial()
    {
        if (dirtMaterial != null)
            return dirtMaterial;

        Shader litShader =
            Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");

        if (litShader == null)
        {
            Debug.LogWarning(
                "PlotPlacementSystem: URP 2D Sprite-Lit-Default shader was not found. " +
                "Assign a Sprite-Lit material to Dirt Material."
            );
            return null;
        }

        dirtMaterial = new Material(litShader);
        dirtMaterial.name = "Runtime Dirt - Sprite Lit";
        return dirtMaterial;
    }



    private IEnumerator AnimateDirtReveal(
        GameObject dirt,
        GameObject plot)
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
                finalScale.x *
                dirtStartingScale,

                finalScale.y *
                dirtStartingScale,

                1f
            );

        Vector3 overshootScale =
            finalScale *
            dirtOvershoot;

        overshootScale.z = 1f;

        Color finalColor =
            renderer.color;

        finalColor.a = 1f;

        float growDuration =
            Mathf.Max(
                0.01f,
                dirtRevealDuration *
                0.72f
            );

        float settleDuration =
            Mathf.Max(
                0.01f,
                dirtRevealDuration -
                growDuration
            );

        float timer = 0f;

        // -----------------------------------------------------
        // SPREAD OUTWARD
        // -----------------------------------------------------

        while (timer < growDuration)
        {
            if (dirt == null)
            {
                yield break;
            }

            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    growDuration
                );

            float xT = t;

            if (dirtHorizontalSpread)
            {
                xT =
                    Mathf.Clamp01(
                        t +
                        dirtHorizontalLead
                    );
            }

            float easedX =
                EaseOutCubic(xT);

            float easedY =
                EaseOutCubic(t);

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

            Color color =
                finalColor;

            color.a =
                EaseOutCubic(t);

            renderer.color =
                color;

            yield return null;
        }

        // -----------------------------------------------------
        // SETTLE
        // -----------------------------------------------------

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
                    timer /
                    settleDuration
                );

            dirt.transform.localScale =
                Vector3.Lerp(
                    overshootScale,
                    finalScale,
                    EaseOutCubic(t)
                );

            yield return null;
        }

        if (dirt == null)
        {
            yield break;
        }

        dirt.transform.localScale =
            finalScale;

        renderer.color =
            finalColor;

        // Do NOT parent dirt to the plot.
        // It must remain perfectly still during plot removal/shake.
        // DirtFadeAfterPlotRemoved will fade it once the plot is gone.
    }

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
    // DIRT REVEAL
    // =========================================================

    [Header("Dirt Reveal")]
    [Tooltip("Single transparent dirt sprite spawned underneath every newly built plot.")]
    [SerializeField] private Sprite dirtSprite;

    [Tooltip("Optional material for the dirt. Leave empty to use Sprites/Default.")]
    [SerializeField] private Material dirtMaterial;

    [Tooltip("World-space offset from the plot centre.")]
    [SerializeField] private Vector3 dirtOffset = Vector3.zero;

    [Tooltip("Absolute sorting order for ALL dirt sprites. Keep this lower than every plot sorting order.")]
    [SerializeField] private int dirtSortingOrder = -1000;

    [Tooltip("Starting size of the dirt reveal.")]
    [Range(0.01f, 1f)]
    [SerializeField] private float dirtStartingScale = 0.08f;

    [Tooltip("How long the dirt takes to spread out.")]
    [Min(0.01f)]
    [SerializeField] private float dirtRevealDuration = 0.28f;

    [Tooltip("Small overshoot before the dirt settles.")]
    [Range(1f, 1.3f)]
    [SerializeField] private float dirtOvershoot = 1.07f;

    [Tooltip("Final local scale of the dirt sprite.")]
    [SerializeField] private Vector2 dirtFinalScale = Vector2.one;

    [Tooltip("If enabled, the dirt spreads a little faster horizontally than vertically.")]
    [SerializeField] private bool dirtHorizontalSpread = true;

    [Range(0f, 0.35f)]
    [SerializeField] private float dirtHorizontalLead = 0.12f;

    [Tooltip("Delay after the plot is actually removed before the dirt begins fading.")]
    [Min(0f)]
    [SerializeField] private float dirtFadeDelay = 0.15f;

    [Tooltip("How long the dirt takes to slowly fade away.")]
    [Min(0.05f)]
    [SerializeField] private float dirtFadeDuration = 1.5f;

    // =========================================================
    // DIRT PARTICLES
    // =========================================================

    [Header("Dirt Placement Particles")]
    [Tooltip("Optional particle prefab spawned at the same moment the dirt appears.")]
    [SerializeField] private GameObject dirtParticlePrefab;

    [Min(0f)]
    [SerializeField] private float dirtParticleLifetime = 1.5f;

    [SerializeField] private Vector3 dirtParticleOffset = Vector3.zero;

    // =========================================================
    // BUILD PARTICLES
    // =========================================================

    [Header("Build Particle Effect")]
    public GameObject buildParticlePrefab;

    public float buildParticleLifetime = 2f;

    public Vector3 buildParticleOffset =
        Vector3.zero;

    // =========================================================
    // FARM PLOT BUILD AUDIO
    // =========================================================

    [Header("Farm Plot Build Audio")]

    [Tooltip(
        "Audio Source used to play farm plot placement sounds."
    )]
    [SerializeField] private AudioSource buildAudioSource;

    [Tooltip(
        "Randomly chooses one of these sounds when a farm plot finishes building."
    )]
    [SerializeField]
    private AudioClip[] farmPlotBuildSounds =
        new AudioClip[3];

    [Range(0f, 1f)]
    [SerializeField] private float farmPlotBuildVolume = 1f;

    [Tooltip(
        "Random minimum pitch used for the placement sound."
    )]
    [SerializeField] private float farmPlotBuildPitchMin = 0.95f;

    [Tooltip(
        "Random maximum pitch used for the placement sound."
    )]
    [SerializeField] private float farmPlotBuildPitchMax = 1.05f;

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

        // Automatically find an AudioSource on this object
        // if one was not manually assigned.
        if (buildAudioSource == null)
        {
            buildAudioSource =
                GetComponent<AudioSource>();
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

        // LEFT CLICK
        if (Input.GetMouseButtonDown(0))
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

        if (removable == null) removable = newPlot.AddComponent<RemovableBuildItem>();
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
        // DIRT REVEAL - ONLY AFTER THE PLOT IS FULLY BUILT
        // =====================================================

        if (plot != null)
        {
            Vector2Int dirtCell =
                GetGridCell(
                    plot.transform.position
                );

            GameObject dirtVisual =
                CreateDirtVisual(
                    plot,
                    dirtCell
                );

            if (dirtVisual != null)
            {
                StartCoroutine(
                    AnimateDirtReveal(
                        dirtVisual,
                        plot
                    )
                );
            }

            SpawnDirtParticles(
                plot.transform.position
            );
        }

        // =====================================================
        // FARM PLOT BUILD SOUND
        // =====================================================

        PlayFarmPlotBuildSound();

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

        // =====================================================
        // TUTORIAL - FARM PLOT SUCCESSFULLY BUILT
        // =====================================================

        // Report this only after the complete build animation,
        // dirt reveal setup, particles, sound, and camera shake.
        // This ensures the tutorial advances only for a real,
        // successfully completed Farm Plot placement.
        if (plot != null)
        {
            TutorialEvents.Report(
                TutorialAction.FarmPlotPlaced
            );
        }
    }

    // =========================================================
    // FARM PLOT BUILD SOUND
    // =========================================================

    private void PlayFarmPlotBuildSound()
    {
        if (buildAudioSource == null)
        {
            return;
        }

        if (farmPlotBuildSounds == null ||
            farmPlotBuildSounds.Length == 0)
        {
            return;
        }

        // Count valid clips so empty array
        // elements are ignored.
        int validClipCount = 0;

        for (int i = 0;
             i < farmPlotBuildSounds.Length;
             i++)
        {
            if (farmPlotBuildSounds[i] != null)
            {
                validClipCount++;
            }
        }

        if (validClipCount <= 0)
        {
            return;
        }

        int randomValidIndex =
            Random.Range(
                0,
                validClipCount
            );

        AudioClip selectedClip = null;

        int currentValidIndex = 0;

        for (int i = 0;
             i < farmPlotBuildSounds.Length;
             i++)
        {
            if (farmPlotBuildSounds[i] == null)
            {
                continue;
            }

            if (currentValidIndex ==
                randomValidIndex)
            {
                selectedClip =
                    farmPlotBuildSounds[i];

                break;
            }

            currentValidIndex++;
        }

        if (selectedClip == null)
        {
            return;
        }

        float originalPitch =
            buildAudioSource.pitch;

        buildAudioSource.pitch =
            Random.Range(
                farmPlotBuildPitchMin,
                farmPlotBuildPitchMax
            );

        buildAudioSource.PlayOneShot(
            selectedClip,
            farmPlotBuildVolume
        );

        buildAudioSource.pitch =
            originalPitch;
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

// ============================================================================
// DIRT FADE AFTER PLOT REMOVAL
// ============================================================================

public class DirtFadeAfterPlotRemoved : MonoBehaviour
{
    private GameObject watchedPlot;
    private SpriteRenderer dirtRenderer;
    private float fadeDelay = 0.15f;
    private float fadeDuration = 1.5f;
    private bool fadeStarted = false;

    public void Setup(
        GameObject plot,
        SpriteRenderer renderer,
        float delay,
        float duration)
    {
        watchedPlot = plot;
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

        // Unity destroyed GameObjects compare equal to null.
        // Therefore the dirt remains stationary during the plot's
        // removal animation and only fades once the plot is truly gone.
        if (watchedPlot == null)
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

