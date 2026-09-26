using UnityEngine;
using UnityEngine.Events;

public class CropPlot : MonoBehaviour
{
    public Sprite GetEmptyPlotSprite() { return emptyPlotSprite; }
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    [Tooltip(
        "Plot component this crop belongs to. " +
        "Automatically found if left empty."
    )]
    [SerializeField]
    private Plot plot;

    [Tooltip(
        "Main SpriteRenderer for the farm plot. " +
        "Automatically found if left empty."
    )]
    [SerializeField]
    private SpriteRenderer cropRenderer;

    [Tooltip(
        "Automatically finds the EndDaySystem in the scene if left empty."
    )]
    [SerializeField]
    private EndDaySystem endDaySystem;

    [Tooltip(
        "Automatically finds the BatColony in the scene if left empty."
    )]
    [SerializeField]
    private BatColony batColony;

    [Tooltip("Environment stats used for daily prey arrival. Automatically found if empty.")]
    [SerializeField] private RangerStation rangerStation;

    [Header("Daily Prey Arrival")]
    [Tooltip("Daily arrival chance at 100 Soil Health and 100 Prey Availability. Checks start the day AFTER maturity.")]
    [Range(0f, 100f)]
    [SerializeField] private float maximumDailyPreyChance = 80f;

    public float GetDailyPreyArrivalChance()
    {
        if (plot != null && plot.IsDestroyed()) return 0f;
        if (rangerStation == null)
            rangerStation = FindFirstObjectByType<RangerStation>();
        if (rangerStation == null) return 0f;
        float chance = Mathf.Clamp(maximumDailyPreyChance, 0f, 100f) *
            Mathf.Clamp01(rangerStation.GetSoilHealth() / 100f) *
            Mathf.Clamp01(rangerStation.GetPreyAvailability() / 100f);
        return float.IsNaN(chance) ? 0f : chance;
    }

    private void TryDailyPreyArrival()
    {
        if (!cropActive || !fullyGrown || attractedMammal != null ||
            !attractMammalWhenFullyGrown) return;

        float chance = GetDailyPreyArrivalChance();
        if (chance > 0f && (chance >= 100f || Random.value < chance / 100f))
            SpawnMammal();
    }

    // =========================================================
    // CROP VISUALS
    // =========================================================

    [Header("Crop Visuals")]

    [Tooltip(
        "Original empty farm plot sprite. " +
        "If empty, the starting SpriteRenderer sprite is remembered."
    )]
    [SerializeField]
    private Sprite emptyPlotSprite;

    [Tooltip(
        "Each sprite should contain the entire farm plot plus crop.\n\n" +
        "0 = Seedling\n" +
        "1 = Small Plant\n" +
        "2 = Large Plant\n" +
        "3 = Fully Grown"
    )]
    [SerializeField]
    private Sprite[] growthStageSprites;

    // =========================================================
    // MAMMAL ATTRACTION
    // =========================================================

    [Header("Mammal Attraction")]

    [Tooltip(
        "Possible mammals attracted when the crop becomes fully grown."
    )]
    [SerializeField]
    private GameObject[] mammalPrefabs;

    [Tooltip(
        "Spawn point for attracted mammals. " +
        "If empty, searches for a child called 'Mammal Spawn Point'."
    )]
    [SerializeField]
    private Transform mammalSpawnPoint;

    [Tooltip(
        "Enable daily prey arrival checks after the crop has matured."
    )]
    [SerializeField]
    private bool attractMammalWhenFullyGrown = true;

    // =========================================================
    // BAT FOOD
    // =========================================================

    [Header("Bat Food Reward")]

    [Tooltip(
        "Food added to the Ghost Bat colony when the player " +
        "collects an attracted mammal."
    )]
    [Min(0f)]
    [SerializeField]
    private float foodPerMammalCollected = 20f;

    [Tooltip(
        "Enable Food rewards when attracted mammals are collected."
    )]
    [SerializeField]
    private bool rewardBatFoodOnCollection = true;

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
    private UnityEvent onMammalCollected;

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

    private bool foodRewardGivenForCurrentMammal = false;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        AutoAssignReferences();
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // Run again in Start in case another scene object
        // had not finished initializing during Awake.
        AutoAssignReferences();

        // =====================================================
        // EXISTING PLOT STATE
        // =====================================================

        if (plot != null &&
            plot.IsPlanted())
        {
            PlantCrop();
        }
        else
        {
            ShowEmptyPlot();
        }
    }

    // =========================================================
    // AUTO ASSIGN REFERENCES
    // =========================================================

    private void AutoAssignReferences()
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
        // SPRITE RENDERER
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

        if (cropRenderer != null &&
            emptyPlotSprite == null)
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

            if (foundSpawnPoint == null)
            {
                Transform[] children =
                    GetComponentsInChildren<Transform>(
                        true
                    );

                foreach (Transform child in children)
                {
                    if (child.name ==
                        "Mammal Spawn Point")
                    {
                        foundSpawnPoint =
                            child;

                        break;
                    }
                }
            }

            if (foundSpawnPoint != null)
            {
                mammalSpawnPoint =
                    foundSpawnPoint;
            }
        }

        // =====================================================
        // END DAY SYSTEM
        // =====================================================

        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>(
                    FindObjectsInactive.Include
                );

            if (endDaySystem == null &&
                showDebugLogs)
            {
                Debug.LogWarning(
                    "[CropPlot] " +
                    gameObject.name +
                    " could not find EndDaySystem."
                );
            }
        }

        // =====================================================
        // BAT COLONY
        // =====================================================

        if (batColony == null)
        {
            batColony =
                FindFirstObjectByType<BatColony>(
                    FindObjectsInactive.Include
                );

            if (batColony == null &&
                showDebugLogs)
            {
                Debug.LogWarning(
                    "[CropPlot] " +
                    gameObject.name +
                    " could not find BatColony."
                );
            }
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (plot != null) { plot.CheckSoilDamage(); if (plot.IsDestroyed()) return; }
        if (!cropActive)
        {
            return;
        }

        // =====================================================
        // MAKE SURE END DAY SYSTEM EXISTS
        // =====================================================

        if (endDaySystem == null)
        {
            AutoAssignReferences();

            if (endDaySystem == null)
            {
                return;
            }
        }

        // =====================================================
        // CURRENT DAY
        // =====================================================

        int currentDay =
            endDaySystem.GetCurrentDay();

        if (currentDay < 1) return;
        if (lastProcessedDay < 1)
        {
            lastProcessedDay = currentDay;
            return;
        }

        if (currentDay <= lastProcessedDay)
        {
            return;
        }

        // =====================================================
        // PROCESS MISSED DAYS
        // =====================================================

        while (lastProcessedDay < currentDay)
        {
            lastProcessedDay++;

            // Choose the branch BEFORE growth: reaching maturity today
            // cannot also run today's prey check.
            if (fullyGrown)
                TryDailyPreyArrival();
            else
                GrowOneDay();

            if (!cropActive) break;
        }
    }

    // =========================================================
    // PLANT CROP
    // =========================================================

    public void PlantCrop()
    {
        if (plot != null && plot.IsDestroyed()) return;
        AutoAssignReferences();

        if (growthStageSprites == null ||
            growthStageSprites.Length == 0)
        {
            Debug.LogWarning(
                gameObject.name +
                ": CropPlot has no growth stage sprites assigned."
            );

            return;
        }

        // =====================================================
        // REMOVE OLD MAMMAL
        // =====================================================

        DestroyAttractedMammal();

        // =====================================================
        // RESET REWARD
        // =====================================================

        foodRewardGivenForCurrentMammal =
            false;

        // =====================================================
        // RESET CROP STATE
        // =====================================================

        cropActive =
            true;

        fullyGrown =
            false;

        currentGrowthStage =
            0;

        // =====================================================
        // REMEMBER DAY PLANTED
        // =====================================================

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

        // =====================================================
        // SPRITE
        // =====================================================

        RefreshCropSprite();

        // =====================================================
        // EVENT
        // =====================================================

        onCropPlanted?.Invoke();

        // =====================================================
        // DEBUG
        // =====================================================

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
        if (plot != null && plot.IsDestroyed()) return;
        if (!cropActive)
        {
            return;
        }

        if (fullyGrown)
        {
            return;
        }

        if (growthStageSprites == null ||
            growthStageSprites.Length == 0)
        {
            return;
        }

        // =====================================================
        // NEXT STAGE
        // =====================================================

        currentGrowthStage++;

        currentGrowthStage =
            Mathf.Clamp(
                currentGrowthStage,
                0,
                growthStageSprites.Length - 1
            );

        RefreshCropSprite();

        onCropGrown?.Invoke();

        // =====================================================
        // FULLY GROWN
        // =====================================================

        if (currentGrowthStage >=
            growthStageSprites.Length - 1)
        {
            BecomeFullyGrown();
        }

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

        onCropFullyGrown?.Invoke();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " is fully grown."
            );
        }

        // Prey checks begin on the following processed day.
    }

    // =========================================================
    // SPAWN MAMMAL
    // =========================================================

    public void SpawnMammal()
    {
        if (plot != null && plot.IsDestroyed()) return;
        if (!fullyGrown)
        {
            return;
        }

        if (attractedMammal != null)
        {
            return;
        }

        GameObject mammalPrefab =
            GetRandomMammalPrefab();

        if (mammalPrefab == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning(
                    gameObject.name +
                    ": No mammal prefab available."
                );
            }

            return;
        }

        Vector3 spawnPosition =
            mammalSpawnPoint != null
                ? mammalSpawnPoint.position
                : transform.position;

        Quaternion spawnRotation =
            mammalSpawnPoint != null
                ? mammalSpawnPoint.rotation
                : Quaternion.identity;

        attractedMammal =
            Instantiate(
                mammalPrefab,
                spawnPosition,
                spawnRotation
            );

        // A newly spawned mammal can reward food.
        foodRewardGivenForCurrentMammal =
            false;

        onMammalAttracted?.Invoke();

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
        if (mammalPrefabs == null ||
            mammalPrefabs.Length == 0)
        {
            return null;
        }

        // =====================================================
        // RANDOM ATTEMPTS
        // =====================================================

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

        // =====================================================
        // FALLBACK
        // =====================================================

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

        // =====================================================
        // VALIDATE THE MAMMAL
        // =====================================================

        bool validMammal =
            mammal != null &&
            attractedMammal ==
            mammal.gameObject;

        // MammalPickup may be on a child object.
        if (!validMammal &&
            mammal != null &&
            attractedMammal != null)
        {
            validMammal =
                mammal.transform.IsChildOf(
                    attractedMammal.transform
                );
        }

        // =====================================================
        // GIVE FOOD
        // =====================================================

        if (validMammal)
        {
            GiveBatFoodReward();

            attractedMammal =
                null;
        }
        else if (attractedMammal == null)
        {
            // Compatibility fallback if the pickup destroys
            // itself before notifying CropPlot.
            GiveBatFoodReward();
        }

        // =====================================================
        // EVENT
        // =====================================================

        onMammalCollected?.Invoke();

        // =====================================================
        // CONSUME CROP
        // =====================================================

        HarvestCrop();
    }

    // =========================================================
    // GIVE BAT FOOD
    // =========================================================

    private void GiveBatFoodReward()
    {
        if (plot != null && plot.IsDestroyed()) return;
        if (!rewardBatFoodOnCollection)
        {
            return;
        }

        if (foodRewardGivenForCurrentMammal)
        {
            return;
        }

        // =====================================================
        // AUTO FIND COLONY
        // =====================================================

        if (batColony == null)
        {
            AutoAssignReferences();
        }

        if (batColony == null)
        {
            Debug.LogWarning(
                "[CropPlot] Could not give Bat Food because " +
                "no BatColony exists in the scene."
            );

            return;
        }

        // =====================================================
        // OLD VALUE
        // =====================================================

        float oldFood =
            batColony.GetBatFood();

        // =====================================================
        // ADD FOOD
        // =====================================================

        batColony.AddBatFood(
            foodPerMammalCollected
        );

        foodRewardGivenForCurrentMammal =
            true;

        // =====================================================
        // DEBUG
        // =====================================================

        if (showDebugLogs)
        {
            Debug.Log(
                "[CropPlot] PREY COLLECTED!" +
                "\nFood Reward: +" +
                foodPerMammalCollected +
                "\nBat Food: " +
                oldFood +
                " -> " +
                batColony.GetBatFood()
            );
        }
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

        cropActive =
            false;

        fullyGrown =
            false;

        currentGrowthStage =
            -1;

        lastProcessedDay =
            -1;

        // =====================================================
        // EMPTY SPRITE
        // =====================================================

        ShowEmptyPlot();

        // =====================================================
        // CLEAR PLOT
        // =====================================================

        if (plot != null)
        {
            plot.ClearPlant();
        }

        // =====================================================
        // EVENT
        // =====================================================

        onCropHarvested?.Invoke();

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
        cropActive =
            false;

        fullyGrown =
            false;

        currentGrowthStage =
            -1;

        lastProcessedDay =
            -1;

        foodRewardGivenForCurrentMammal =
            false;

        DestroyAttractedMammal();

        ShowEmptyPlot();

        if (plot != null)
        {
            plot.ClearPlant();
        }
    }

    // =========================================================
    // SHOW EMPTY PLOT
    // =========================================================

    private void ShowEmptyPlot()
    {
        if (plot != null && plot.IsDestroyed()) return;
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
        if (plot != null && plot.IsDestroyed()) return;
        if (cropRenderer == null)
        {
            return;
        }

        // =====================================================
        // EMPTY
        // =====================================================

        if (!cropActive ||
            currentGrowthStage < 0)
        {
            ShowEmptyPlot();
            return;
        }

        // =====================================================
        // NO SPRITES
        // =====================================================

        if (growthStageSprites == null ||
            growthStageSprites.Length == 0)
        {
            ShowEmptyPlot();
            return;
        }

        // =====================================================
        // CURRENT STAGE
        // =====================================================

        int index =
            Mathf.Clamp(
                currentGrowthStage,
                0,
                growthStageSprites.Length - 1
            );

        Sprite stageSprite =
            growthStageSprites[index];

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
    // DESTROY ATTRACTED MAMMAL
    // =========================================================

    private void DestroyAttractedMammal()
    {
        if (attractedMammal == null)
        {
            return;
        }

        attractedMammal.SetActive(false);
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

    public float GetFoodReward()
    {
        return foodPerMammalCollected;
    }

    // =========================================================
    // DEBUG
    // =========================================================

    [ContextMenu("Debug - Auto Assign References")]
    private void DebugAutoAssignReferences()
    {
        AutoAssignReferences();

        Debug.Log(
            "[CropPlot] Auto Assignment:" +
            "\nEnd Day System = " +
            (
                endDaySystem != null
                    ? endDaySystem.name
                    : "NOT FOUND"
            ) +
            "\nBat Colony = " +
            (
                batColony != null
                    ? batColony.name
                    : "NOT FOUND"
            )
        );
    }

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

        while (
            cropActive &&
            !fullyGrown
        )
        {
            GrowOneDay();
        }
    }

    [ContextMenu("Debug - Spawn Mammal")]
    private void DebugSpawnMammal()
    {
        if (!fullyGrown)
        {
            DebugFullyGrow();
        }

        SpawnMammal();
    }

    [ContextMenu("Debug - Give Bat Food Reward")]
    private void DebugGiveBatFoodReward()
    {
        foodRewardGivenForCurrentMammal =
            false;

        GiveBatFoodReward();
    }

    [ContextMenu("Debug - Clear Crop")]
    private void DebugClearCrop()
    {
        ClearCrop();
    }
}

