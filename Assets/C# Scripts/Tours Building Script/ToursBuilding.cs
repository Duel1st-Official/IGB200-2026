using UnityEngine;

public class ToursBuilding : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("Tour Ready Alert")]
    [SerializeField] private bool showTourReadyAlert = true;
    [Tooltip("Drag your exclamation-mark icon sprite here. No icon is shown until assigned.")]
    [SerializeField] private Sprite tourReadyAlertIcon;
    [SerializeField] private Color tourReadyAlertColor = Color.white;
    [Tooltip("Offset above the top of the building sprite, in world units.")]
    [SerializeField] private Vector2 tourReadyAlertOffset = new Vector2(0f, 0.2f);
    [Tooltip("Icon height in world units. Aspect ratio is preserved.")]
    [Min(0.01f)][SerializeField] private float tourReadyAlertSize = 0.5f;
    [Min(0f)][SerializeField] private float tourReadyAlertBounceHeight = 0.06f;
    [Min(0f)][SerializeField] private float tourReadyAlertBounceSpeed = 2f;
    [SerializeField] private int tourReadyAlertSortingOffset = 10;
    [Tooltip("The building artwork used to place and sort the icon. Auto-found if empty.")]
    [SerializeField] private SpriteRenderer buildingRenderer;
    private SpriteRenderer tourReadyAlertRenderer;

    private void LateUpdate()
    {
        RefreshTourReadyAlert();
    }

    private void RefreshTourReadyAlert()
    {
        if (endDaySystem == null) FindReferences();
        bool visible = showTourReadyAlert && tourReadyAlertIcon != null &&
            endDaySystem != null && !endDaySystem.HasGameEnded() &&
            endDaySystem.IsActionPhaseActive() && IsTourRecommended();
        if (!visible)
        {
            if (tourReadyAlertRenderer != null) tourReadyAlertRenderer.gameObject.SetActive(false);
            return;
        }

        if (tourReadyAlertRenderer == null)
        {
            if (buildingRenderer == null)
                buildingRenderer = GetComponentInChildren<SpriteRenderer>();
            GameObject marker = new GameObject("Tour Ready Icon");
            marker.layer = gameObject.layer;
            marker.transform.SetParent(transform, false);
            tourReadyAlertRenderer = marker.AddComponent<SpriteRenderer>();
        }

        tourReadyAlertRenderer.gameObject.SetActive(true);
        tourReadyAlertRenderer.sprite = tourReadyAlertIcon;
        tourReadyAlertRenderer.color = tourReadyAlertColor;
        Vector3 basePosition = buildingRenderer != null
            ? new Vector3(buildingRenderer.bounds.center.x, buildingRenderer.bounds.max.y, transform.position.z)
            : transform.position;
        float bounce = Mathf.Sin(Time.time * Mathf.Max(0f, tourReadyAlertBounceSpeed) * Mathf.PI * 2f)
            * Mathf.Max(0f, tourReadyAlertBounceHeight);

        Transform markerTransform = tourReadyAlertRenderer.transform;
        markerTransform.rotation = Quaternion.identity;
        Vector3 scale = transform.lossyScale;
        float size = Mathf.Max(0.01f, tourReadyAlertSize) /
            Mathf.Max(0.0001f, tourReadyAlertIcon.bounds.size.y);
        markerTransform.localScale = new Vector3(
            size / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
            size / Mathf.Max(0.0001f, Mathf.Abs(scale.y)), 1f);
        // Position the visible icon centre consistently even with a non-centred sprite pivot.
        markerTransform.position = basePosition +
            new Vector3(tourReadyAlertOffset.x, tourReadyAlertOffset.y + bounce, 0f);
        markerTransform.position -= markerTransform.TransformVector(tourReadyAlertIcon.bounds.center);

        if (buildingRenderer != null)
        {
            tourReadyAlertRenderer.sortingLayerID = buildingRenderer.sortingLayerID;
            tourReadyAlertRenderer.sortingOrder = buildingRenderer.sortingOrder + tourReadyAlertSortingOffset;
        }
    }

    private void OnDisable()
    {
        if (tourReadyAlertRenderer != null) tourReadyAlertRenderer.gameObject.SetActive(false);
    }


    [Header("References")]

    [SerializeField]
    private RangerStation rangerStation;

    [SerializeField]
    private BatColony batColony;

    [SerializeField]
    private EndDaySystem endDaySystem;

    // =========================================================
    // REPUTATION
    // =========================================================

    [Header("Colony Reputation")]

    [Range(0f, 100f)]
    [SerializeField]
    private float colonyReputation = 50f;

    [Min(0)]
    [SerializeField]
    private int toursCompleted = 0;

    // =========================================================
    // DAILY REPUTATION LOSS
    // =========================================================

    [Header("Daily Reputation")]

    [Tooltip(
        "Reputation lost whenever a new day begins."
    )]
    [Min(0f)]
    [SerializeField]
    private float dailyReputationLoss = 10f;

    // =========================================================
    // TOUR REWARDS
    // =========================================================

    [Header("Tour Reputation Rewards")]

    [Tooltip(
        "Minimum Reputation earned from a completed tour."
    )]
    [Min(0f)]
    [SerializeField]
    private float minimumTourReward = 30f;

    [Tooltip(
        "Maximum Reputation earned from a completed tour."
    )]
    [Min(0f)]
    [SerializeField]
    private float maximumTourReward = 70f;

    // =========================================================
    // TOUR SCORE WEIGHTS
    // =========================================================

    [Header("Tour Score Weights")]

    [Tooltip("Importance of Bat Health.")]
    [Min(0f)]
    [SerializeField]
    private float healthWeight = 30f;

    [Tooltip("Importance of colony Food.")]
    [Min(0f)]
    [SerializeField]
    private float foodWeight = 15f;

    [Tooltip("Importance of colony Water.")]
    [Min(0f)]
    [SerializeField]
    private float waterWeight = 15f;

    [Tooltip("Importance of Prey Availability.")]
    [Min(0f)]
    [SerializeField]
    private float preyWeight = 10f;

    [Tooltip(
        "Importance of low Predator Pressure."
    )]
    [Min(0f)]
    [SerializeField]
    private float predatorWeight = 10f;

    [Tooltip(
        "Importance of low Fire Risk."
    )]
    [Min(0f)]
    [SerializeField]
    private float fireWeight = 10f;

    [Tooltip("Importance of Soil Health.")]
    [Min(0f)]
    [SerializeField]
    private float soilWeight = 10f;

    // =========================================================
    // TOUR TIMING
    // =========================================================

    [Header("Tour Timing")]

    [Tooltip(
        "Recommended number of days between tours. " +
        "This does NOT currently prevent the player touring early."
    )]
    [Min(1)]
    [SerializeField]
    private int recommendedDaysBetweenTours = 3;

    [Tooltip(
        "Day the most recent tour was completed. " +
        "-1 means no tour has been completed yet."
    )]
    [SerializeField]
    private int lastTourDay = -1;

    // =========================================================
    // TOUR DISTURBANCE
    // =========================================================

    [Header("Tour Disturbance")]

    [Tooltip(
        "Small Health cost caused by visitors disturbing the colony."
    )]
    [Min(0f)]
    [SerializeField]
    private float tourHealthCost = 1f;

    // =========================================================
    // LAST TOUR INFORMATION
    // =========================================================

    [Header("Last Tour")]

    [Range(0f, 100f)]
    [SerializeField]
    private float lastTourScore = 0f;

    [SerializeField]
    private float lastTourReward = 0f;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLogs = false;

    [SerializeField]
    private int lastProcessedDay = -1;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        FindReferences();
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        FindReferences();

        // -----------------------------------------------------
        // Start tracking the current day.
        //
        // Reputation should NOT immediately lose 10 when
        // entering the scene.
        // -----------------------------------------------------

        if (endDaySystem != null)
        {
            lastProcessedDay =
                endDaySystem.GetCurrentDay();
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        ProcessPendingDays();
    }

    public void ProcessPendingDays()
    {
        if (endDaySystem != null && endDaySystem.HasGameEnded()) return;
        if (endDaySystem == null ||
            rangerStation == null ||
            batColony == null)
        {
            FindReferences();
        }

        if (endDaySystem == null)
        {
            return;
        }

        int currentDay =
            endDaySystem.GetCurrentDay();

        // -----------------------------------------------------
        // INITIALISE
        // -----------------------------------------------------

        if (currentDay < 1) return;

        if (lastProcessedDay < 1)
        {
            lastProcessedDay =
                currentDay;

            return;
        }

        // -----------------------------------------------------
        // SAME DAY
        // -----------------------------------------------------

        if (currentDay <= lastProcessedDay)
        {
            return;
        }

        // -----------------------------------------------------
        // PROCESS EVERY NEW DAY
        // -----------------------------------------------------

        while (lastProcessedDay < currentDay)
        {
            lastProcessedDay++;

            ProcessNewDay();
        }
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private void FindReferences()
    {
        if (rangerStation == null)
        {
            rangerStation =
                FindFirstObjectByType<RangerStation>();
        }

        if (batColony == null)
        {
            batColony =
                FindFirstObjectByType<BatColony>();
        }

        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }
    }

    // =========================================================
    // NEW DAY
    // =========================================================

    private void ProcessNewDay()
    {
        float oldReputation =
            colonyReputation;

        RemoveReputation(
            dailyReputationLoss
        );

        if (showDebugLogs)
        {
            Debug.Log(
                "[TOURS] NEW DAY" +
                "\nDay: " +
                lastProcessedDay +
                "\nDaily Reputation Loss: -" +
                dailyReputationLoss +
                "\nReputation: " +
                oldReputation +
                " -> " +
                colonyReputation +
                "\nDays Since Tour: " +
                GetDaysSinceLastTour()
            );
        }
    }

    // =========================================================
    // REPUTATION
    // =========================================================

    public void SetReputation(
        float value)
    {
        colonyReputation =
            Mathf.Clamp(
                value,
                0f,
                100f
            );

        DebugState();
    }

    public void AddReputation(
        float amount)
    {
        SetReputation(
            colonyReputation +
            amount
        );
    }

    public void RemoveReputation(
        float amount)
    {
        SetReputation(
            colonyReputation -
            amount
        );
    }

    // =========================================================
    // COMPLETE DYNAMIC TOUR
    // =========================================================

    public void CompleteTour()
    {
        if (endDaySystem != null && endDaySystem.HasGameEnded()) return;
        FindReferences();

        // -----------------------------------------------------
        // Calculate condition BEFORE visitor disturbance.
        // -----------------------------------------------------

        lastTourScore =
            CalculateTourScore();

        lastTourReward =
            CalculateTourRewardFromScore(
                lastTourScore
            );

        toursCompleted++;
        if (tourReadyAlertRenderer != null)
            tourReadyAlertRenderer.gameObject.SetActive(false);

        // -----------------------------------------------------
        // RECORD TOUR DAY
        // -----------------------------------------------------

        if (endDaySystem != null)
        {
            lastTourDay =
                endDaySystem.GetCurrentDay();
        }

        // -----------------------------------------------------
        // REPUTATION REWARD
        // -----------------------------------------------------

        AddReputation(
            lastTourReward
        );

        // -----------------------------------------------------
        // TOUR DISTURBANCE
        // -----------------------------------------------------

        if (batColony != null &&
            tourHealthCost > 0f)
        {
            batColony.AddBatHealth(
                -tourHealthCost
            );
        }

        if (showDebugLogs)
        {
            Debug.Log(
                "====================================" +
                "\n[TOUR COMPLETED]" +
                "\n====================================" +
                "\nTour Score: " +
                lastTourScore.ToString("0.0") +
                "/100" +
                "\nQuality: " +
                GetTourQualityFromScore(
                    lastTourScore
                ) +
                "\nReputation Reward: +" +
                lastTourReward.ToString("0.0") +
                "\nCurrent Reputation: " +
                colonyReputation.ToString("0.0") +
                "\nHealth Disturbance: -" +
                tourHealthCost +
                "\nTours Completed: " +
                toursCompleted
            );
        }
    }

    // =========================================================
    // LEGACY COMPLETE TOUR
    //
    // Kept so older scripts do not break.
    // =========================================================

    public void CompleteTour(
        float reputationReward)
    {
        if (endDaySystem != null && endDaySystem.HasGameEnded()) return;
        FindReferences();

        toursCompleted++;
        if (tourReadyAlertRenderer != null)
            tourReadyAlertRenderer.gameObject.SetActive(false);

        lastTourReward =
            Mathf.Max(
                0f,
                reputationReward
            );

        lastTourScore =
            CalculateTourScore();

        if (endDaySystem != null)
        {
            lastTourDay =
                endDaySystem.GetCurrentDay();
        }

        AddReputation(
            lastTourReward
        );

        if (batColony != null &&
            tourHealthCost > 0f)
        {
            batColony.AddBatHealth(
                -tourHealthCost
            );
        }

        if (showDebugLogs)
        {
            Debug.Log(
                "[TOURS] Legacy tour completed." +
                "\nReputation +" +
                lastTourReward
            );
        }
    }

    // =========================================================
    // CALCULATE TOUR SCORE
    // =========================================================

    public float CalculateTourScore()
    {
        FindReferences();

        if (batColony == null ||
            rangerStation == null)
        {
            return 50f;
        }

        // =====================================================
        // COLONY VALUES
        // =====================================================

        float health =
            Mathf.Clamp(
                batColony.GetBatHealth(),
                0f,
                100f
            );

        float food =
            Mathf.Clamp(
                batColony.GetBatFood(),
                0f,
                100f
            );

        float water =
            Mathf.Clamp(
                batColony.GetBatWater(),
                0f,
                100f
            );

        // =====================================================
        // ENVIRONMENT VALUES
        // =====================================================

        float prey =
            Mathf.Clamp(
                rangerStation.GetPreyAvailability(),
                0f,
                100f
            );

        float predatorPressure =
            Mathf.Clamp(
                rangerStation.GetPredatorPressure(),
                0f,
                100f
            );

        float fireRisk =
            Mathf.Clamp(
                rangerStation.GetFireRisk(),
                0f,
                100f
            );

        float soil =
            Mathf.Clamp(
                rangerStation.GetSoilHealth(),
                0f,
                100f
            );

        // -----------------------------------------------------
        // LOW predator/fire values are GOOD.
        // Convert them into safety scores.
        // -----------------------------------------------------

        float predatorSafety =
            100f -
            predatorPressure;

        float fireSafety =
            100f -
            fireRisk;

        // =====================================================
        // TOTAL WEIGHT
        // =====================================================

        float totalWeight =
            healthWeight +
            foodWeight +
            waterWeight +
            preyWeight +
            predatorWeight +
            fireWeight +
            soilWeight;

        if (totalWeight <= 0f)
        {
            return 0f;
        }

        // =====================================================
        // WEIGHTED SCORE
        // =====================================================

        float weightedScore =
            (health * healthWeight) +
            (food * foodWeight) +
            (water * waterWeight) +
            (prey * preyWeight) +
            (predatorSafety * predatorWeight) +
            (fireSafety * fireWeight) +
            (soil * soilWeight);

        float finalScore =
            weightedScore /
            totalWeight;

        return Mathf.Clamp(
            finalScore,
            0f,
            100f
        );
    }

    // =========================================================
    // CALCULATE TOUR REWARD
    // =========================================================

    public float CalculateTourReward()
    {
        return
            CalculateTourRewardFromScore(
                CalculateTourScore()
            );
    }

    // =========================================================
    // SCORE -> REWARD
    //
    // 0 score   = +30
    // 50 score  = +50
    // 100 score = +70
    // =========================================================

    private float CalculateTourRewardFromScore(
        float score)
    {
        float normalizedScore =
            Mathf.Clamp01(
                score /
                100f
            );

        return Mathf.Lerp(
            minimumTourReward,
            maximumTourReward,
            normalizedScore
        );
    }

    // =========================================================
    // TOUR QUALITY
    // =========================================================

    public string GetTourQuality()
    {
        return
            GetTourQualityFromScore(
                CalculateTourScore()
            );
    }

    public string GetLastTourQuality()
    {
        return
            GetTourQualityFromScore(
                lastTourScore
            );
    }

    private string GetTourQualityFromScore(
        float score)
    {
        if (score >= 90f)
        {
            return "EXCELLENT";
        }

        if (score >= 75f)
        {
            return "GREAT";
        }

        if (score >= 50f)
        {
            return "GOOD";
        }

        if (score >= 25f)
        {
            return "POOR";
        }

        return "TERRIBLE";
    }

    // =========================================================
    // TOUR TIMING
    // =========================================================

    public int GetDaysSinceLastTour()
    {
        FindReferences();

        // No tour yet.
        if (lastTourDay < 0)
        {
            return recommendedDaysBetweenTours;
        }

        if (endDaySystem == null)
        {
            return 0;
        }

        return Mathf.Max(
            0,
            endDaySystem.GetCurrentDay() -
            lastTourDay
        );
    }

    public bool IsTourRecommended()
    {
        // -----------------------------------------------------
        // The first ever tour is immediately available.
        // -----------------------------------------------------

        if (lastTourDay < 0)
        {
            return true;
        }

        return
            GetDaysSinceLastTour() >=
            recommendedDaysBetweenTours;
    }

    public int GetDaysUntilRecommendedTour()
    {
        if (lastTourDay < 0)
        {
            return 0;
        }

        return Mathf.Max(
            0,
            recommendedDaysBetweenTours -
            GetDaysSinceLastTour()
        );
    }

    // =========================================================
    // GETTERS
    // =========================================================

    public float GetReputation()
    {
        return colonyReputation;
    }

    public int GetToursCompleted()
    {
        return toursCompleted;
    }

    public float GetDailyReputationLoss()
    {
        return dailyReputationLoss;
    }

    public float GetMinimumTourReward()
    {
        return minimumTourReward;
    }

    public float GetMaximumTourReward()
    {
        return maximumTourReward;
    }

    public float GetLastTourScore()
    {
        return lastTourScore;
    }

    public float GetLastTourReward()
    {
        return lastTourReward;
    }

    public int GetLastTourDay()
    {
        return lastTourDay;
    }

    public int GetRecommendedDaysBetweenTours()
    {
        return recommendedDaysBetweenTours;
    }

    // =========================================================
    // DEBUG
    // =========================================================

    private void DebugState()
    {
        if (!showDebugLogs)
        {
            return;
        }

        Debug.Log(
            "[TOURS BUILDING]" +
            "\nReputation: " +
            colonyReputation.ToString("0.0") +
            "\nTours Completed: " +
            toursCompleted +
            "\nCurrent Tour Score: " +
            CalculateTourScore().ToString("0.0") +
            "\nPotential Reward: +" +
            CalculateTourReward().ToString("0.0")
        );
    }

    [ContextMenu("Debug - Increase Reputation")]
    private void DebugIncreaseReputation()
    {
        AddReputation(
            10f
        );
    }

    [ContextMenu("Debug - Decrease Reputation")]
    private void DebugDecreaseReputation()
    {
        RemoveReputation(
            10f
        );
    }

    [ContextMenu("Debug - Apply Daily Reputation Loss")]
    private void DebugDailyReputationLoss()
    {
        RemoveReputation(
            dailyReputationLoss
        );
    }

    [ContextMenu("Debug - Complete Dynamic Tour")]
    private void DebugCompleteTour()
    {
        CompleteTour();
    }

    [ContextMenu("Debug - Print Tour Score")]
    private void DebugPrintTourScore()
    {
        Debug.Log(
            "[TOURS] Current Tour Score: " +
            CalculateTourScore().ToString("0.0") +
            "/100" +
            "\nQuality: " +
            GetTourQuality() +
            "\nPotential Reward: +" +
            CalculateTourReward().ToString("0.0")
        );
    }
}

