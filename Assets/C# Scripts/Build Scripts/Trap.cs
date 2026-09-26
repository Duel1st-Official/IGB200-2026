using UnityEngine;

public class Trap : MonoBehaviour
{
    [Header("Destroyed plot / debris")]
    [SerializeField] private bool destroyed;
    [SerializeField] private Sprite debrisSprite;
    [SerializeField] private SpriteRenderer debrisRenderer;
    [SerializeField] private string destructionCause;
    [SerializeField] private int criticalSoilDays;
    private int soilCheckedDay = -1;
    public bool IsDestroyed() { return destroyed; }
    public string GetDestructionCause() { return destructionCause; }
    public bool DestroyTrap(string cause)
    {
        if (destroyed) return false;
        destroyed = true; destructionCause = cause;
        hasBait = false; hasCaughtAnimal = false; caughtMammalName = "";
        currentState = TrapState.Destroyed; dayTrapWasSet = -1;
        ApplyDebrisVisual();
        return true;
    }
    private void ApplyDebrisVisual()
    {
        if (debrisRenderer == null) debrisRenderer = spriteRenderer;
        if (debrisRenderer == null) debrisRenderer = GetComponentInChildren<SpriteRenderer>();
        if (debrisRenderer == null) return;
        if (spriteRenderer != null && spriteRenderer != debrisRenderer) spriteRenderer.enabled = false;
        debrisRenderer.enabled = true;
        if (debrisSprite != null) debrisRenderer.sprite = debrisSprite;
        debrisRenderer.color = debrisSprite != null ? Color.white : new Color(0.25f, 0.19f, 0.17f);
    }
    public void RemoveWithPlot()
    {
        Plot owner = GetOwningPlot();
        if (owner != null && (owner.gameObject == gameObject || owner.transform.IsChildOf(transform))) return;
        gameObject.SetActive(false); Destroy(gameObject);
    }
    private void CheckSoilDamage()
    {
        if (destroyed || endDaySystem == null || endDaySystem.HasGameEnded()) return;
        int day = endDaySystem.GetCurrentDay();
        if (day < 1) return;
        if (soilCheckedDay < 1 || day < soilCheckedDay) { soilCheckedDay = day; criticalSoilDays = 0; return; }
        if (day == soilCheckedDay) return;
        soilCheckedDay = day;
        if (rangerStation == null) rangerStation = FindFirstObjectByType<RangerStation>();
        if (rangerStation == null) return;
        PlotDisasterSystem settings = PlotDisasterSystem.GetOrCreate();
        criticalSoilDays = rangerStation.GetSoilHealth() <= settings.criticalSoilHealth ? criticalSoilDays + 1 : 0;
        if (criticalSoilDays >= Mathf.Max(1, settings.consecutiveCriticalDays)) DestroyTrap("Critical soil health");
    }
    [ContextMenu("Debug - Destroy Trap")]
    private void DebugDestroyTrap() { DestroyTrap("Debug"); }


    [Header("Plot ownership")]
    [Tooltip("Optional explicit owner. Otherwise resolves parent Plot, then matching plot grid cell.")]
    [SerializeField] private Plot owningPlot;

    public Plot GetOwningPlot()
    {
        if (owningPlot != null) return owningPlot;
        owningPlot = GetComponentInParent<Plot>();
        if (owningPlot != null) return owningPlot;
        foreach (Plot candidate in FindObjectsByType<Plot>(FindObjectsSortMode.None))
        {
            RemovableBuildItem item = candidate.GetComponentInParent<RemovableBuildItem>();
            if (item != null && item.placementSystem != null)
            {
                if (item.placementSystem.GetGridCell(transform.position) == item.gridCell)
                { owningPlot = candidate; break; }
            }
            else if (Vector2.Distance(transform.position, candidate.transform.position) < 0.15f)
            { owningPlot = candidate; break; }
        }
        return owningPlot;
    }
    private bool PlotIsDestroyed()
    {
        Plot owner = GetOwningPlot();
        return destroyed || (owner != null && owner.IsDestroyed());
    }
    public void DestroyWithPlot()
    {
        DestroyTrap("Owning plot destroyed");
    }
    // =========================================================
    // STATE
    // =========================================================

    public enum TrapState
    {
        Empty,
        Set,
        Caught,
        Destroyed
    }

    [Header("Trap State")]
    [SerializeField]
    private TrapState currentState =
        TrapState.Empty;

    [SerializeField]
    private bool startsWithBait = false;

    // =========================================================
    // VISUALS
    // =========================================================

    [Header("Trap Visuals")]

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private Sprite emptyTrapSprite;

    [SerializeField]
    private Sprite baitedTrapSprite;

    [SerializeField]
    private Sprite feralCatCaughtSprite;

    [SerializeField]
    private Sprite foxCaughtSprite;

    [SerializeField]
    private Sprite genericCaughtSprite;

    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    [SerializeField]
    private EndDaySystem endDaySystem;

    [SerializeField]
    private RangerStation rangerStation;

    // =========================================================
    // CAPTURE CHANCES
    // =========================================================

    [Header("Daily Capture Chance")]

    [Range(0f, 1f)]
    [SerializeField]
    private float sunnyCaptureChance = 0.70f;

    [Range(0f, 1f)]
    [SerializeField]
    private float rainCaptureChance = 0.50f;

    [Range(0f, 1f)]
    [SerializeField]
    private float thunderCaptureChance = 0.30f;

    // =========================================================
    // PREDATORS
    // =========================================================

    [Header("Predators")]

    [SerializeField]
    private string[] predatorNames =
    {
        "Feral Cat",
        "Fox"
    };

    // =========================================================
    // ECOSYSTEM
    // =========================================================

    [Header("Predator Pressure Reduction")]

    [SerializeField]
    private float feralCatPredatorReduction = 6f;

    [SerializeField]
    private float foxPredatorReduction = 8f;

    [SerializeField]
    private float genericPredatorReduction = 5f;

    // =========================================================
    // CAUGHT PREDATOR
    // =========================================================

    [Header("Caught Predator")]

    [SerializeField]
    private string caughtMammalName = "";

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private string debugMammalName = "Feral Cat";

    [SerializeField]
    private bool showDebugLogs = true;

    // =========================================================
    // PRIVATE
    // =========================================================

    private bool hasBait;
    private bool hasCaughtAnimal;

    private int lastProcessedDay = -1;
    private int dayTrapWasSet = -1;

    private string recordedDayWeather = "Sunny";

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                spriteRenderer =
                    GetComponentInChildren<SpriteRenderer>();
            }
        }

        FindReferences();
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (destroyed) { currentState = TrapState.Destroyed; hasBait = false; hasCaughtAnimal = false; caughtMammalName = ""; ApplyDebrisVisual(); return; }
        if (PlotIsDestroyed()) { DestroyWithPlot(); return; }
        FindReferences();

        if (endDaySystem != null)
        {
            lastProcessedDay =
                endDaySystem.GetCurrentDay();
        }

        recordedDayWeather =
            GetCurrentWeatherName();

        // Starting state is setup, not a player action.
        if (startsWithBait)
        {
            SetTrapInternal();
        }
        else
        {
            MakeEmpty();
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        CheckSoilDamage(); if (destroyed) return;
        CheckForNewDay();
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private void FindReferences()
    {
        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }

        if (rangerStation == null)
        {
            rangerStation =
                FindFirstObjectByType<RangerStation>();
        }
    }

    // =========================================================
    // ACTION PHASE
    // =========================================================

    public bool CanPerformPlayerAction()
    {
        if (PlotIsDestroyed()) return false;
        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }

        if (endDaySystem == null)
        {
            return true;
        }

        return endDaySystem.IsActionPhaseActive();
    }

    // =========================================================
    // NEW DAY
    // =========================================================

    private void CheckForNewDay()
    {
        if (PlotIsDestroyed()) { DestroyWithPlot(); return; }
        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();

            if (endDaySystem == null)
            {
                return;
            }
        }

        int currentDay =
            endDaySystem.GetCurrentDay();

        if (lastProcessedDay < 0)
        {
            lastProcessedDay =
                currentDay;

            recordedDayWeather =
                GetCurrentWeatherName();

            return;
        }

        if (currentDay ==
            lastProcessedDay)
        {
            return;
        }

        string previousWeather =
            recordedDayWeather;

        // IMPORTANT:
        // This is automatic overnight processing.
        // It is intentionally NOT blocked by the action phase.

        if (currentState ==
                TrapState.Set &&
            hasBait &&
            currentDay >
                dayTrapWasSet)
        {
            AttemptDailyCapture(
                previousWeather
            );
        }

        lastProcessedDay =
            currentDay;

        recordedDayWeather =
            GetCurrentWeatherName();
    }

    // =========================================================
    // CAPTURE ATTEMPT
    // =========================================================

    private void AttemptDailyCapture(
        string previousWeather)
    {
        if (!IsSet() ||
            !hasBait)
        {
            return;
        }

        float chance =
            GetCaptureChanceForWeather(
                previousWeather
            );

        float roll =
            Random.value;

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " Capture | Weather: " +
                previousWeather +
                " | Chance: " +
                Mathf.RoundToInt(chance * 100f) +
                "% | Roll: " +
                roll.ToString("0.00")
            );
        }

        if (roll <= chance)
        {
            TriggerTrap(
                GetRandomPredatorName()
            );
        }
        else if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " caught nothing overnight."
            );
        }
    }

    // =========================================================
    // WEATHER CHANCE
    // =========================================================

    private float GetCaptureChanceForWeather(
        string weather)
    {
        if (weather ==
            "Rain")
        {
            return rainCaptureChance;
        }

        if (weather ==
                "RainAndThunder" ||
            weather ==
                "Thunder" ||
            weather ==
                "Storm")
        {
            return thunderCaptureChance;
        }

        return sunnyCaptureChance;
    }

    // =========================================================
    // WEATHER
    // =========================================================

    private string GetCurrentWeatherName()
    {
        if (WeatherManager.Instance == null)
        {
            return "Sunny";
        }

        return
            WeatherManager.Instance
                .GetCurrentWeather()
                .ToString();
    }

    // =========================================================
    // RANDOM PREDATOR
    // =========================================================

    private string GetRandomPredatorName()
    {
        if (predatorNames == null ||
            predatorNames.Length == 0)
        {
            return "Unknown Predator";
        }

        int validCount =
            0;

        foreach (string predator
                 in predatorNames)
        {
            if (!string.IsNullOrWhiteSpace(
                predator))
            {
                validCount++;
            }
        }

        if (validCount <= 0)
        {
            return "Unknown Predator";
        }

        int target =
            Random.Range(
                0,
                validCount
            );

        int current =
            0;

        foreach (string predator
                 in predatorNames)
        {
            if (string.IsNullOrWhiteSpace(
                predator))
            {
                continue;
            }

            if (current ==
                target)
            {
                return predator;
            }

            current++;
        }

        return "Unknown Predator";
    }

    // =========================================================
    // SET TRAP - PLAYER ACTION
    // =========================================================

    public void SetTrap()
    {
        // =====================================================
        // ACTION PHASE LOCK
        // =====================================================

        if (!CanPerformPlayerAction())
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    gameObject.name +
                    " cannot be set because the action phase has ended."
                );
            }

            return;
        }

        SetTrapInternal();
    }

    // =========================================================
    // INTERNAL SET TRAP
    // =========================================================

    private void SetTrapInternal()
    {
        if (PlotIsDestroyed()) { DestroyWithPlot(); return; }
        hasBait =
            true;

        hasCaughtAnimal =
            false;

        caughtMammalName =
            "";

        currentState =
            TrapState.Set;

        FindReferences();

        if (endDaySystem != null)
        {
            dayTrapWasSet =
                endDaySystem.GetCurrentDay();
        }
        else
        {
            dayTrapWasSet =
                lastProcessedDay;
        }

        RefreshTrapSprite();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " set with meat bait."
            );
        }
    }

    // =========================================================
    // TRIGGER
    // =========================================================

    public void TriggerTrap(
        string mammalName)
    {
        // IMPORTANT:
        // No action lock.
        // This is used by automatic overnight capture.

        if (!IsSet())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
            mammalName))
        {
            mammalName =
                "Unknown Predator";
        }

        hasBait =
            false;

        hasCaughtAnimal =
            true;

        caughtMammalName =
            mammalName;

        currentState =
            TrapState.Caught;

        RefreshTrapSprite();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " caught " +
                caughtMammalName +
                "."
            );
        }
    }

    public void TriggerTrap()
    {
        TriggerTrap(
            debugMammalName
        );
    }

    // =========================================================
    // RELOCATE
    // =========================================================

    public void CollectCaughtMammal()
    {
        // =====================================================
        // ACTION PHASE LOCK
        // =====================================================

        if (!CanPerformPlayerAction())
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    gameObject.name +
                    " predator cannot be relocated because " +
                    "the action phase has ended."
                );
            }

            return;
        }

        if (!IsCaught())
        {
            return;
        }

        string relocatedPredator =
            caughtMammalName;

        ReducePredatorPressure(
            relocatedPredator
        );

        hasCaughtAnimal =
            false;

        hasBait =
            false;

        caughtMammalName =
            "";

        currentState =
            TrapState.Empty;

        dayTrapWasSet =
            -1;

        RefreshTrapSprite();

        if (showDebugLogs)
        {
            Debug.Log(
                "Relocated " +
                relocatedPredator +
                " from " +
                gameObject.name +
                "."
            );
        }
    }

    public void RelocateCaughtPredator()
    {
        CollectCaughtMammal();
    }

    // =========================================================
    // PREDATOR PRESSURE
    // =========================================================

    private void ReducePredatorPressure(
        string predatorName)
    {
        if (rangerStation == null)
        {
            rangerStation =
                FindFirstObjectByType<RangerStation>();
        }

        if (rangerStation == null)
        {
            return;
        }

        float reduction =
            genericPredatorReduction;

        string normalized =
            string.IsNullOrWhiteSpace(
                predatorName)
                ? ""
                : predatorName
                    .Trim()
                    .ToLowerInvariant();

        if (normalized ==
                "feral cat" ||
            normalized ==
                "cat")
        {
            reduction =
                feralCatPredatorReduction;
        }
        else if (
            normalized ==
                "fox" ||
            normalized ==
                "red fox")
        {
            reduction =
                foxPredatorReduction;
        }

        rangerStation.RemovePredatorPressure(
            reduction
        );

        if (showDebugLogs)
        {
            Debug.Log(
                predatorName +
                " relocated | Predator Pressure -" +
                reduction
            );
        }
    }

    // =========================================================
    // RESET
    // =========================================================

    public void ResetTrap()
    {
        SetTrap();
    }

    public void AddBait()
    {
        SetTrap();
    }

    // =========================================================
    // EMPTY
    // =========================================================

    public void MakeEmpty()
    {
        if (destroyed) return;
        hasCaughtAnimal =
            false;

        hasBait =
            false;

        caughtMammalName =
            "";

        currentState =
            TrapState.Empty;

        dayTrapWasSet =
            -1;

        RefreshTrapSprite();
    }

    public void ClearCaughtAnimal()
    {
        CollectCaughtMammal();
    }

    // =========================================================
    // SPRITE
    // =========================================================

    public void RefreshTrapSprite()
    {
        if (destroyed) { currentState = TrapState.Destroyed; hasBait = false; hasCaughtAnimal = false; caughtMammalName = ""; ApplyDebrisVisual(); return; }
        if (PlotIsDestroyed()) { DestroyWithPlot(); return; }
        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                spriteRenderer =
                    GetComponentInChildren<SpriteRenderer>();
            }
        }

        if (spriteRenderer == null)
        {
            return;
        }

        if (IsEmpty())
        {
            if (emptyTrapSprite != null)
            {
                spriteRenderer.sprite =
                    emptyTrapSprite;
            }

            return;
        }

        if (IsSet())
        {
            if (baitedTrapSprite != null)
            {
                spriteRenderer.sprite =
                    baitedTrapSprite;
            }
            else if (emptyTrapSprite != null)
            {
                spriteRenderer.sprite =
                    emptyTrapSprite;
            }

            return;
        }

        Sprite caughtSprite =
            GetCaughtPredatorSprite();

        if (caughtSprite != null)
        {
            spriteRenderer.sprite =
                caughtSprite;
        }
    }

    // =========================================================
    // CAUGHT SPRITE
    // =========================================================

    private Sprite GetCaughtPredatorSprite()
    {
        string normalized =
            string.IsNullOrWhiteSpace(
                caughtMammalName)
                ? ""
                : caughtMammalName
                    .Trim()
                    .ToLowerInvariant();

        if (normalized ==
                "feral cat" ||
            normalized ==
                "cat")
        {
            if (feralCatCaughtSprite != null)
            {
                return feralCatCaughtSprite;
            }
        }

        if (normalized ==
                "fox" ||
            normalized ==
                "red fox")
        {
            if (foxCaughtSprite != null)
            {
                return foxCaughtSprite;
            }
        }

        if (genericCaughtSprite != null)
        {
            return genericCaughtSprite;
        }

        return emptyTrapSprite;
    }

    // =========================================================
    // GETTERS
    // =========================================================

    public TrapState GetState()
    {
        return currentState;
    }

    public string GetCaughtMammalName()
    {
        return caughtMammalName;
    }

    public bool HasBait()
    {
        if (PlotIsDestroyed()) return false;
        return hasBait;
    }

    public bool HasCaughtAnimal()
    {
        if (PlotIsDestroyed()) return false;
        return hasCaughtAnimal;
    }

    public bool IsEmpty()
    {
        if (destroyed) return false;
        return currentState ==
               TrapState.Empty;
    }

    public bool IsSet()
    {
        if (PlotIsDestroyed()) return false;
        return currentState ==
               TrapState.Set;
    }

    public bool IsCaught()
    {
        if (PlotIsDestroyed()) return false;
        return currentState ==
               TrapState.Caught;
    }

    public int GetDayTrapWasSet()
    {
        return dayTrapWasSet;
    }

    public string GetRecordedWeather()
    {
        return recordedDayWeather;
    }

    // =========================================================
    // DEBUG
    // =========================================================

    [ContextMenu("Debug - Set Trap With Bait")]
    private void DebugSetTrap()
    {
        SetTrap();
    }

    [ContextMenu("Debug - Make Empty")]
    private void DebugEmpty()
    {
        MakeEmpty();
    }

    [ContextMenu("Debug - Catch Feral Cat")]
    private void DebugCatchCat()
    {
        if (PlotIsDestroyed()) return;
        currentState =
            TrapState.Set;

        hasBait =
            true;

        TriggerTrap(
            "Feral Cat"
        );
    }

    [ContextMenu("Debug - Catch Fox")]
    private void DebugCatchFox()
    {
        if (PlotIsDestroyed()) return;
        currentState =
            TrapState.Set;

        hasBait =
            true;

        TriggerTrap(
            "Fox"
        );
    }

    [ContextMenu("Debug - Relocate Current Predator")]
    private void DebugRelocate()
    {
        RelocateCaughtPredator();
    }

    [ContextMenu("Debug - Sunny Capture Roll")]
    private void DebugSunny()
    {
        if (!IsSet())
        {
            SetTrapInternal();
        }

        AttemptDailyCapture(
            "Sunny"
        );
    }

    [ContextMenu("Debug - Rain Capture Roll")]
    private void DebugRain()
    {
        if (!IsSet())
        {
            SetTrapInternal();
        }

        AttemptDailyCapture(
            "Rain"
        );
    }

    [ContextMenu("Debug - Thunder Capture Roll")]
    private void DebugThunder()
    {
        if (!IsSet())
        {
            SetTrapInternal();
        }

        AttemptDailyCapture(
            "RainAndThunder"
        );
    }
}


