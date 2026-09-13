using UnityEngine;
using UnityEngine.Events;

public class CropPlot : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    [Tooltip(
        "Plot component this crop belongs to. " +
        "If empty, it will automatically be found."
    )]
    [SerializeField]
    private Plot plot;

    [Tooltip(
        "Main SpriteRenderer for the entire farm plot. " +
        "If empty, it will automatically use the SpriteRenderer on this object."
    )]
    [SerializeField]
    private SpriteRenderer cropRenderer;

    [Tooltip(
        "Main day system. " +
        "If empty, it will automatically be found in the scene."
    )]
    [SerializeField]
    private EndDaySystem endDaySystem;

    // =========================================================
    // CROP VISUALS
    // =========================================================

    [Header("Crop Visuals")]

    [Tooltip(
        "Original empty farm plot sprite.\n\n" +
        "If left empty, the sprite currently assigned to this prefab's " +
        "SpriteRenderer will automatically be remembered as the empty plot."
    )]
    [SerializeField]
    private Sprite emptyPlotSprite;

    [Tooltip(
        "Each sprite should contain the ENTIRE farm plot plus the crop.\n\n" +
        "Example:\n" +
        "0 = Plot + Seedling\n" +
        "1 = Plot + Small Plant\n" +
        "2 = Plot + Large Plant\n" +
        "3 = Plot + Fully Grown Plant"
    )]
    [SerializeField]
    private Sprite[] growthStageSprites;

    // =========================================================
    // MAMMAL ATTRACTION
    // =========================================================

    [Header("Mammal Attraction")]

    [Tooltip(
        "Possible mammals this crop can attract when fully grown."
    )]
    [SerializeField]
    private GameObject[] mammalPrefabs;

    [Tooltip(
        "Optional spawn point for the mammal.\n\n" +
        "If empty, this script will look for a child named " +
        "\"Mammal Spawn Point\"."
    )]
    [SerializeField]
    private Transform mammalSpawnPoint;

    [Tooltip(
        "Automatically spawn a mammal when the crop becomes fully grown."
    )]
    [SerializeField]
    private bool attractMammalWhenFullyGrown = true;

    // =========================================================
    // EVENTS
    // =========================================================

    [Header("Events")]

    [SerializeField]
    private UnityEvent onCropPlanted;

    [SerializeField]
    private UnityEvent onCropGrown;

    [SerializeField]
    private UnityEvent onCropFullyGrown;

    [SerializeField]
    private UnityEvent onMammalAttracted;

    [SerializeField]
    private UnityEvent onCropHarvested;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLogs = false;

    // =========================================================
    // PRIVATE
    // =========================================================

    private int currentGrowthStage = -1;

    private int lastProcessedDay = -1;

    private bool cropActive = false;

    private bool fullyGrown = false;

    private GameObject attractedMammal;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // =====================================================
        // PLOT
        // =====================================================

        if (plot == null)
        {
            plot =
                GetComponent<Plot>();

            if (plot == null)
            {
                plot =
                    GetComponentInParent<Plot>();
            }

            if (plot == null)
            {
                plot =
                    GetComponentInChildren<Plot>();
            }
        }

        // =====================================================
        // MAIN PLOT SPRITE RENDERER
        // =====================================================

        if (cropRenderer == null)
        {
            cropRenderer =
                GetComponent<SpriteRenderer>();

            if (cropRenderer == null)
            {
                cropRenderer =
                    GetComponentInChildren<SpriteRenderer>();
            }
        }

        // =====================================================
        // REMEMBER EMPTY PLOT SPRITE
        // =====================================================

        if (
            cropRenderer != null &&
            emptyPlotSprite == null
        )
        {
            emptyPlotSprite =
                cropRenderer.sprite;
        }

        // =====================================================
        // MAMMAL SPAWN POINT
        // =====================================================

        if (mammalSpawnPoint == null)
        {
            Transform foundSpawnPoint =
                transform.Find(
                    "Mammal Spawn Point"
                );

            if (foundSpawnPoint != null)
            {
                mammalSpawnPoint =
                    foundSpawnPoint;
            }
        }
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // =====================================================
        // DAY SYSTEM
        // =====================================================

        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }

        // =====================================================
        // EXISTING PLOT STATE
        // =====================================================

        if (
            plot != null &&
            plot.IsPlanted()
        )
        {
            PlantCrop();
        }
        else
        {
            ShowEmptyPlot();
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // -----------------------------------------------------
        // NO ACTIVE CROP
        // -----------------------------------------------------

        if (!cropActive)
        {
            return;
        }

        // -----------------------------------------------------
        // FULLY GROWN
        // -----------------------------------------------------

        if (fullyGrown)
        {
            return;
        }

        // -----------------------------------------------------
        // FIND DAY SYSTEM
        // -----------------------------------------------------

        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();

            if (endDaySystem == null)
            {
                return;
            }
        }

        // -----------------------------------------------------
        // CURRENT DAY
        // -----------------------------------------------------

        int currentDay =
            endDaySystem.GetCurrentDay();

        // -----------------------------------------------------
        // NO NEW DAY
        // -----------------------------------------------------

        if (currentDay <= lastProcessedDay)
        {
            return;
        }

        // -----------------------------------------------------
        // PROCESS MISSED DAYS
        // -----------------------------------------------------

        while (lastProcessedDay < currentDay)
        {
            lastProcessedDay++;

            GrowOneDay();

            if (fullyGrown)
            {
                break;
            }
        }
    }

    // =========================================================
    // PLANT CROP
    // =========================================================

    public void PlantCrop()
    {
        // -----------------------------------------------------
        // NEED GROWTH STAGES
        // -----------------------------------------------------

        if (
            growthStageSprites == null ||
            growthStageSprites.Length == 0
        )
        {
            Debug.LogWarning(
                gameObject.name +
                ": CropPlot has no growth stage sprites assigned."
            );

            return;
        }

        // -----------------------------------------------------
        // FIND DAY SYSTEM
        // -----------------------------------------------------

        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }

        // -----------------------------------------------------
        // REMOVE OLD MAMMAL
        // -----------------------------------------------------

        DestroyAttractedMammal();

        // -----------------------------------------------------
        // RESET CROP STATE
        // -----------------------------------------------------

        cropActive =
            true;

        fullyGrown =
            false;

        currentGrowthStage =
            0;

        // -----------------------------------------------------
        // REMEMBER DAY PLANTED
        // -----------------------------------------------------

        if (endDaySystem != null)
        {
            lastProcessedDay =
                endDaySystem.GetCurrentDay();
        }
        else
        {
            lastProcessedDay =
                0;
        }

        // -----------------------------------------------------
        // SHOW FIRST STAGE
        // -----------------------------------------------------

        RefreshCropSprite();

        // -----------------------------------------------------
        // EVENT
        // -----------------------------------------------------

        onCropPlanted?.Invoke();

        // -----------------------------------------------------
        // DEBUG
        // -----------------------------------------------------

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " planted on Day " +
                lastProcessedDay +
                ". Growth Stage = 0."
            );
        }
    }

    // =========================================================
    // GROW ONE DAY
    // =========================================================

    public void GrowOneDay()
    {
        // -----------------------------------------------------
        // NO CROP
        // -----------------------------------------------------

        if (!cropActive)
        {
            return;
        }

        // -----------------------------------------------------
        // ALREADY FULLY GROWN
        // -----------------------------------------------------

        if (fullyGrown)
        {
            return;
        }

        // -----------------------------------------------------
        // NEED SPRITES
        // -----------------------------------------------------

        if (
            growthStageSprites == null ||
            growthStageSprites.Length == 0
        )
        {
            return;
        }

        // -----------------------------------------------------
        // NEXT GROWTH STAGE
        // -----------------------------------------------------

        currentGrowthStage++;

        // -----------------------------------------------------
        // CLAMP
        // -----------------------------------------------------

        currentGrowthStage =
            Mathf.Clamp(
                currentGrowthStage,
                0,
                growthStageSprites.Length - 1
            );

        // -----------------------------------------------------
        // CHANGE WHOLE PLOT SPRITE
        // -----------------------------------------------------

        RefreshCropSprite();

        // -----------------------------------------------------
        // EVENT
        // -----------------------------------------------------

        onCropGrown?.Invoke();

        // -----------------------------------------------------
        // FULLY GROWN
        // -----------------------------------------------------

        if (
            currentGrowthStage >=
            growthStageSprites.Length - 1
        )
        {
            BecomeFullyGrown();
        }

        // -----------------------------------------------------
        // DEBUG
        // -----------------------------------------------------

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " grew to stage " +
                currentGrowthStage +
                "."
            );
        }
    }

    // =========================================================
    // FULLY GROWN
    // =========================================================

    private void BecomeFullyGrown()
    {
        if (fullyGrown)
        {
            return;
        }

        fullyGrown =
            true;

        // -----------------------------------------------------
        // EVENT
        // -----------------------------------------------------

        onCropFullyGrown?.Invoke();

        // -----------------------------------------------------
        // DEBUG
        // -----------------------------------------------------

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " is fully grown."
            );
        }

        // -----------------------------------------------------
        // ATTRACT MAMMAL
        // -----------------------------------------------------

        if (attractMammalWhenFullyGrown)
        {
            SpawnMammal();
        }
    }

    // =========================================================
    // SPAWN MAMMAL
    // =========================================================

    public void SpawnMammal()
    {
        // -----------------------------------------------------
        // MUST BE FULLY GROWN
        // -----------------------------------------------------

        if (!fullyGrown)
        {
            return;
        }

        // -----------------------------------------------------
        // ALREADY HAS MAMMAL
        // -----------------------------------------------------

        if (attractedMammal != null)
        {
            return;
        }

        // -----------------------------------------------------
        // NEED MAMMALS
        // -----------------------------------------------------

        if (
            mammalPrefabs == null ||
            mammalPrefabs.Length == 0
        )
        {
            if (showDebugLogs)
            {
                Debug.LogWarning(
                    gameObject.name +
                    " has no mammal prefabs assigned."
                );
            }

            return;
        }

        // -----------------------------------------------------
        // CHOOSE MAMMAL
        // -----------------------------------------------------

        GameObject chosenPrefab =
            GetRandomMammalPrefab();

        if (chosenPrefab == null)
        {
            return;
        }

        // -----------------------------------------------------
        // POSITION
        // -----------------------------------------------------

        Vector3 spawnPosition =
            transform.position;

        Quaternion spawnRotation =
            Quaternion.identity;

        if (mammalSpawnPoint != null)
        {
            spawnPosition =
                mammalSpawnPoint.position;

            spawnRotation =
                mammalSpawnPoint.rotation;
        }

        // -----------------------------------------------------
        // SPAWN
        // -----------------------------------------------------

        attractedMammal =
            Instantiate(
                chosenPrefab,
                spawnPosition,
                spawnRotation
            );

        // -----------------------------------------------------
        // CONNECT TO CROP
        // -----------------------------------------------------

        MammalPickup mammalPickup =
            attractedMammal.GetComponent<MammalPickup>();

        if (mammalPickup == null)
        {
            mammalPickup =
                attractedMammal.GetComponentInChildren<MammalPickup>();
        }

        if (mammalPickup != null)
        {
            mammalPickup.SetCropOwner(
                this
            );
        }
        else
        {
            Debug.LogWarning(
                attractedMammal.name +
                " does not have a MammalPickup component."
            );
        }

        // -----------------------------------------------------
        // EVENT
        // -----------------------------------------------------

        onMammalAttracted?.Invoke();

        // -----------------------------------------------------
        // DEBUG
        // -----------------------------------------------------

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " attracted " +
                attractedMammal.name +
                "."
            );
        }
    }

    // =========================================================
    // RANDOM MAMMAL
    // =========================================================

    private GameObject GetRandomMammalPrefab()
    {
        if (
            mammalPrefabs == null ||
            mammalPrefabs.Length == 0
        )
        {
            return null;
        }

        // -----------------------------------------------------
        // RANDOM ATTEMPTS
        // -----------------------------------------------------

        for (
            int attempt = 0;
            attempt < mammalPrefabs.Length * 2;
            attempt++
        )
        {
            int randomIndex =
                Random.Range(
                    0,
                    mammalPrefabs.Length
                );

            if (mammalPrefabs[randomIndex] != null)
            {
                return mammalPrefabs[randomIndex];
            }
        }

        // -----------------------------------------------------
        // FALLBACK
        // -----------------------------------------------------

        for (
            int i = 0;
            i < mammalPrefabs.Length;
            i++
        )
        {
            if (mammalPrefabs[i] != null)
            {
                return mammalPrefabs[i];
            }
        }

        return null;
    }

    // =========================================================
    // MAMMAL COLLECTED
    // =========================================================

    public void MammalCollected(
        MammalPickup mammal)
    {
        if (!cropActive)
        {
            return;
        }

        // -----------------------------------------------------
        // REMOVE REFERENCE
        // -----------------------------------------------------

        if (
            mammal != null &&
            attractedMammal ==
            mammal.gameObject
        )
        {
            attractedMammal =
                null;
        }

        // -----------------------------------------------------
        // CROP GETS CONSUMED
        // -----------------------------------------------------

        HarvestCrop();
    }

    // =========================================================
    // HARVEST CROP
    // =========================================================

    public void HarvestCrop()
    {
        if (!cropActive)
        {
            return;
        }

        // -----------------------------------------------------
        // RESET CROP
        // -----------------------------------------------------

        cropActive =
            false;

        fullyGrown =
            false;

        currentGrowthStage =
            -1;

        lastProcessedDay =
            -1;

        // -----------------------------------------------------
        // RESTORE EMPTY FARM PLOT
        // -----------------------------------------------------

        ShowEmptyPlot();

        // -----------------------------------------------------
        // CLEAR PLOT PLANTED STATE
        // -----------------------------------------------------

        if (plot != null)
        {
            plot.ClearPlant();
        }

        // -----------------------------------------------------
        // EVENT
        // -----------------------------------------------------

        onCropHarvested?.Invoke();

        // -----------------------------------------------------
        // DEBUG
        // -----------------------------------------------------

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " crop consumed. Plot restored to empty."
            );
        }
    }

    // =========================================================
    // CLEAR CROP
    // =========================================================

    public void ClearCrop()
    {
        // -----------------------------------------------------
        // RESET STATE
        // -----------------------------------------------------

        cropActive =
            false;

        fullyGrown =
            false;

        currentGrowthStage =
            -1;

        lastProcessedDay =
            -1;

        // -----------------------------------------------------
        // REMOVE MAMMAL
        // -----------------------------------------------------

        DestroyAttractedMammal();

        // -----------------------------------------------------
        // RESTORE EMPTY PLOT
        // -----------------------------------------------------

        ShowEmptyPlot();
    }

    // =========================================================
    // SHOW EMPTY PLOT
    // =========================================================

    private void ShowEmptyPlot()
    {
        if (cropRenderer == null)
        {
            return;
        }

        cropRenderer.sprite =
            emptyPlotSprite;

        cropRenderer.enabled =
            true;
    }

    // =========================================================
    // REFRESH CROP SPRITE
    // =========================================================

    private void RefreshCropSprite()
    {
        if (cropRenderer == null)
        {
            return;
        }

        // -----------------------------------------------------
        // EMPTY
        // -----------------------------------------------------

        if (
            !cropActive ||
            currentGrowthStage < 0
        )
        {
            ShowEmptyPlot();
            return;
        }

        // -----------------------------------------------------
        // NO GROWTH SPRITES
        // -----------------------------------------------------

        if (
            growthStageSprites == null ||
            growthStageSprites.Length == 0
        )
        {
            ShowEmptyPlot();
            return;
        }

        // -----------------------------------------------------
        // GET CORRECT STAGE
        // -----------------------------------------------------

        int index =
            Mathf.Clamp(
                currentGrowthStage,
                0,
                growthStageSprites.Length - 1
            );

        Sprite stageSprite =
            growthStageSprites[index];

        // -----------------------------------------------------
        // APPLY WHOLE PLOT + CROP SPRITE
        // -----------------------------------------------------

        if (stageSprite != null)
        {
            cropRenderer.sprite =
                stageSprite;
        }
        else
        {
            cropRenderer.sprite =
                emptyPlotSprite;
        }

        cropRenderer.enabled =
            true;
    }

    // =========================================================
    // DESTROY MAMMAL
    // =========================================================

    private void DestroyAttractedMammal()
    {
        if (attractedMammal == null)
        {
            return;
        }

        Destroy(
            attractedMammal
        );

        attractedMammal =
            null;
    }

    // =========================================================
    // GETTERS
    // =========================================================

    public int GetGrowthStage()
    {
        return currentGrowthStage;
    }

    public int GetGrowthStageCount()
    {
        if (growthStageSprites == null)
        {
            return 0;
        }

        return growthStageSprites.Length;
    }

    public bool IsCropActive()
    {
        return cropActive;
    }

    public bool IsFullyGrown()
    {
        return fullyGrown;
    }

    public bool HasAttractedMammal()
    {
        return attractedMammal != null;
    }

    public GameObject GetAttractedMammal()
    {
        return attractedMammal;
    }

    // =========================================================
    // DEBUG
    // =========================================================

    [ContextMenu("Debug - Plant Crop")]
    private void DebugPlantCrop()
    {
        if (plot != null)
        {
            plot.Plant();
        }
        else
        {
            PlantCrop();
        }
    }

    [ContextMenu("Debug - Grow One Day")]
    private void DebugGrowOneDay()
    {
        GrowOneDay();
    }

    [ContextMenu("Debug - Fully Grow")]
    private void DebugFullyGrow()
    {
        if (!cropActive)
        {
            if (plot != null)
            {
                plot.Plant();
            }
            else
            {
                PlantCrop();
            }
        }

        while (!fullyGrown)
        {
            GrowOneDay();
        }
    }

    [ContextMenu("Debug - Spawn Mammal")]
    private void DebugSpawnMammal()
    {
        SpawnMammal();
    }

    [ContextMenu("Debug - Harvest Crop")]
    private void DebugHarvestCrop()
    {
        HarvestCrop();
    }

    [ContextMenu("Debug - Clear Crop")]
    private void DebugClearCrop()
    {
        if (plot != null)
        {
            plot.ClearPlant();
        }

        ClearCrop();
    }
}