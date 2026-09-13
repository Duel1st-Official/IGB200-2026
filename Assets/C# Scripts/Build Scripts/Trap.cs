using UnityEngine;

public class Trap : MonoBehaviour
{
    // =========================================================
    // TRAP STATE
    // =========================================================

    public enum TrapState
    {
        Empty,
        Set,
        Caught
    }

    // =========================================================
    // STATE
    // =========================================================

    [Header("Trap State")]
    [SerializeField]
    private TrapState currentState =
        TrapState.Empty;

    [SerializeField] private bool startsWithBait = false;

    // =========================================================
    // VISUALS
    // =========================================================

    [Header("Trap Visuals")]

    [Tooltip("SpriteRenderer used to display the trap.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Shown when the trap is empty.")]
    [SerializeField] private Sprite emptyTrapSprite;

    [Tooltip("Shown when the trap is set and contains bait.")]
    [SerializeField] private Sprite baitedTrapSprite;

    [Tooltip("Shown when a Feral Cat has been caught.")]
    [SerializeField] private Sprite feralCatCaughtSprite;

    [Tooltip("Shown when a Fox has been caught.")]
    [SerializeField] private Sprite foxCaughtSprite;

    [Tooltip(
        "Fallback sprite used if an unknown predator is caught."
    )]
    [SerializeField] private Sprite genericCaughtSprite;

    // =========================================================
    // DAY SYSTEM
    // =========================================================

    [Header("Day System")]

    [Tooltip(
        "Used to make one capture attempt when a new day begins."
    )]
    [SerializeField] private EndDaySystem endDaySystem;

    // =========================================================
    // WEATHER CAPTURE CHANCE
    // =========================================================

    [Header("Daily Capture Chance")]

    [Tooltip("Chance to catch a predator after a Sunny day.")]
    [Range(0f, 1f)]
    [SerializeField] private float sunnyCaptureChance = 0.70f;

    [Tooltip("Chance to catch a predator after a Rain day.")]
    [Range(0f, 1f)]
    [SerializeField] private float rainCaptureChance = 0.50f;

    [Tooltip(
        "Chance to catch a predator after a thunderstorm day."
    )]
    [Range(0f, 1f)]
    [SerializeField] private float thunderCaptureChance = 0.30f;

    // =========================================================
    // PREDATORS
    // =========================================================

    [Header("Predators")]

    [Tooltip(
        "Possible predators that can be captured by this trap."
    )]
    [SerializeField]
    private string[] predatorNames =
    {
        "Feral Cat",
        "Fox"
    };

    // =========================================================
    // CAUGHT PREDATOR
    // =========================================================

    [Header("Caught Predator")]

    [Tooltip(
        "Name of the predator currently inside the trap."
    )]
    [SerializeField] private string caughtMammalName = "";

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [Tooltip(
        "Predator used by the Debug Catch Predator option."
    )]
    [SerializeField]
    private string debugMammalName =
        "Feral Cat";

    [SerializeField] private bool showDebugLogs = true;

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
        // -----------------------------------------------------
        // SPRITE RENDERER
        // -----------------------------------------------------

        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponentInChildren<SpriteRenderer>();
        }

        // -----------------------------------------------------
        // DAY SYSTEM
        // -----------------------------------------------------

        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }

        if (endDaySystem != null)
        {
            lastProcessedDay =
                endDaySystem.GetCurrentDay();
        }

        recordedDayWeather =
            GetCurrentWeatherName();

        if (startsWithBait)
        {
            SetTrap();
        }
        else
        {
            MakeEmpty();
        }

        RefreshTrapSprite();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        CheckForNewDay();
    }

    // =========================================================
    // NEW DAY CHECK
    // =========================================================

    private void CheckForNewDay()
    {
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

        // -----------------------------------------------------
        // SAVE PREVIOUS WEATHER
        // -----------------------------------------------------

        string previousWeather =
            recordedDayWeather;

        // -----------------------------------------------------
        // TRY TO CATCH PREDATOR
        // -----------------------------------------------------

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

        // -----------------------------------------------------
        // RECORD NEW DAY
        // -----------------------------------------------------

        lastProcessedDay =
            currentDay;

        recordedDayWeather =
            GetCurrentWeatherName();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " entered Day " +
                currentDay +
                " | Previous Weather: " +
                previousWeather +
                " | Trap State: " +
                currentState
            );
        }
    }

    // =========================================================
    // DAILY CAPTURE ATTEMPT
    // =========================================================

    private void AttemptDailyCapture(
        string previousWeather)
    {
        if (currentState !=
            TrapState.Set)
        {
            return;
        }

        if (!hasBait)
        {
            return;
        }

        float captureChance =
            GetCaptureChanceForWeather(
                previousWeather
            );

        float roll =
            Random.value;

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " capture roll | Weather: " +
                previousWeather +
                " | Chance: " +
                Mathf.RoundToInt(
                    captureChance * 100f
                ) +
                "% | Roll: " +
                roll.ToString("0.00")
            );
        }

        if (roll <=
            captureChance)
        {
            string predator =
                GetRandomPredatorName();

            TriggerTrap(
                predator
            );
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    gameObject.name +
                    " caught nothing overnight."
                );
            }
        }
    }

    // =========================================================
    // WEATHER CAPTURE CHANCE
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
    // CURRENT WEATHER
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

        for (int i = 0;
             i < predatorNames.Length;
             i++)
        {
            if (!string.IsNullOrWhiteSpace(
                predatorNames[i]))
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return "Unknown Predator";
        }

        int targetIndex =
            Random.Range(
                0,
                validCount
            );

        int validIndex =
            0;

        for (int i = 0;
             i < predatorNames.Length;
             i++)
        {
            if (string.IsNullOrWhiteSpace(
                predatorNames[i]))
            {
                continue;
            }

            if (validIndex ==
                targetIndex)
            {
                return predatorNames[i];
            }

            validIndex++;
        }

        return "Unknown Predator";
    }

    // =========================================================
    // SET TRAP
    // =========================================================

    public void SetTrap()
    {
        hasBait =
            true;

        hasCaughtAnimal =
            false;

        caughtMammalName =
            "";

        currentState =
            TrapState.Set;

        // -----------------------------------------------------
        // REMEMBER WHICH DAY IT WAS SET
        // -----------------------------------------------------

        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }

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

        // -----------------------------------------------------
        // SHOW BAIT SPRITE
        // -----------------------------------------------------

        RefreshTrapSprite();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " has been SET with bait on Day " +
                dayTrapWasSet +
                "."
            );
        }
    }

    // =========================================================
    // TRIGGER TRAP
    // =========================================================

    public void TriggerTrap(
        string mammalName)
    {
        if (currentState !=
            TrapState.Set)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
            mammalName))
        {
            mammalName =
                "Unknown Predator";
        }

        // -----------------------------------------------------
        // BAIT IS CONSUMED
        // -----------------------------------------------------

        hasBait =
            false;

        // -----------------------------------------------------
        // STORE ANIMAL
        // -----------------------------------------------------

        hasCaughtAnimal =
            true;

        caughtMammalName =
            mammalName;

        currentState =
            TrapState.Caught;

        // -----------------------------------------------------
        // SHOW ANIMAL SPRITE
        // -----------------------------------------------------

        RefreshTrapSprite();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " caught predator: " +
                caughtMammalName
            );
        }
    }

    // =========================================================
    // SIMPLE TRIGGER
    // =========================================================

    public void TriggerTrap()
    {
        TriggerTrap(
            debugMammalName
        );
    }

    // =========================================================
    // RELOCATE / COLLECT
    // =========================================================

    public void CollectCaughtMammal()
    {
        if (currentState !=
            TrapState.Caught)
        {
            return;
        }

        string relocatedPredator =
            caughtMammalName;

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

        // -----------------------------------------------------
        // RETURN TO EMPTY SPRITE
        // -----------------------------------------------------

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

    // =========================================================
    // RELOCATE ALIAS
    // =========================================================

    public void RelocateCaughtPredator()
    {
        CollectCaughtMammal();
    }

    // =========================================================
    // RESET TRAP
    // =========================================================

    public void ResetTrap()
    {
        SetTrap();
    }

    // =========================================================
    // MAKE EMPTY
    // =========================================================

    public void MakeEmpty()
    {
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

        // -----------------------------------------------------
        // SHOW EMPTY SPRITE
        // -----------------------------------------------------

        RefreshTrapSprite();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " is EMPTY."
            );
        }
    }

    // =========================================================
    // ADD BAIT
    // =========================================================

    public void AddBait()
    {
        SetTrap();
    }

    // =========================================================
    // CLEAR CAUGHT ANIMAL
    // =========================================================

    public void ClearCaughtAnimal()
    {
        CollectCaughtMammal();
    }

    // =========================================================
    // REFRESH TRAP SPRITE
    // =========================================================

    public void RefreshTrapSprite()
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

        if (spriteRenderer == null)
        {
            return;
        }

        // =====================================================
        // EMPTY
        // =====================================================

        if (currentState ==
            TrapState.Empty)
        {
            if (emptyTrapSprite != null)
            {
                spriteRenderer.sprite =
                    emptyTrapSprite;
            }

            return;
        }

        // =====================================================
        // SET / BAITED
        // =====================================================

        if (currentState ==
            TrapState.Set)
        {
            if (baitedTrapSprite != null)
            {
                spriteRenderer.sprite =
                    baitedTrapSprite;
            }
            else if (emptyTrapSprite != null)
            {
                // Fallback if bait sprite is not assigned.
                spriteRenderer.sprite =
                    emptyTrapSprite;
            }

            return;
        }

        // =====================================================
        // CAUGHT
        // =====================================================

        if (currentState ==
            TrapState.Caught)
        {
            Sprite caughtSprite =
                GetCaughtPredatorSprite();

            if (caughtSprite != null)
            {
                spriteRenderer.sprite =
                    caughtSprite;
            }
        }
    }

    // =========================================================
    // GET CAUGHT PREDATOR SPRITE
    // =========================================================

    private Sprite GetCaughtPredatorSprite()
    {
        if (string.IsNullOrWhiteSpace(
            caughtMammalName))
        {
            if (genericCaughtSprite != null)
            {
                return genericCaughtSprite;
            }

            return emptyTrapSprite;
        }

        string normalizedName =
            caughtMammalName
                .Trim()
                .ToLowerInvariant();

        // =====================================================
        // FERAL CAT
        // =====================================================

        if (normalizedName ==
                "feral cat" ||
            normalizedName ==
                "cat")
        {
            if (feralCatCaughtSprite != null)
            {
                return feralCatCaughtSprite;
            }
        }

        // =====================================================
        // FOX
        // =====================================================

        if (normalizedName ==
                "fox" ||
            normalizedName ==
                "red fox")
        {
            if (foxCaughtSprite != null)
            {
                return foxCaughtSprite;
            }
        }

        // =====================================================
        // FALLBACK
        // =====================================================

        if (genericCaughtSprite != null)
        {
            return genericCaughtSprite;
        }

        return emptyTrapSprite;
    }

    // =========================================================
    // DEBUG - SET TRAP
    // =========================================================

    [ContextMenu("Debug - Set Trap With Bait")]
    private void DebugSetTrap()
    {
        SetTrap();
    }

    // =========================================================
    // DEBUG - EMPTY
    // =========================================================

    [ContextMenu("Debug - Make Empty")]
    private void DebugMakeEmpty()
    {
        MakeEmpty();
    }

    // =========================================================
    // DEBUG - CATCH FERAL CAT
    // =========================================================

    [ContextMenu("Debug - Catch Feral Cat")]
    private void DebugCatchFeralCat()
    {
        currentState =
            TrapState.Set;

        hasBait =
            true;

        TriggerTrap(
            "Feral Cat"
        );
    }

    // =========================================================
    // DEBUG - CATCH FOX
    // =========================================================

    [ContextMenu("Debug - Catch Fox")]
    private void DebugCatchFox()
    {
        currentState =
            TrapState.Set;

        hasBait =
            true;

        TriggerTrap(
            "Fox"
        );
    }

    // =========================================================
    // DEBUG - CATCH CUSTOM PREDATOR
    // =========================================================

    [ContextMenu("Debug - Catch Predator")]
    private void DebugCatchPredator()
    {
        currentState =
            TrapState.Set;

        hasBait =
            true;

        TriggerTrap(
            debugMammalName
        );
    }

    // =========================================================
    // DEBUG - SUNNY CAPTURE
    // =========================================================

    [ContextMenu("Debug - Sunny Capture Roll")]
    private void DebugSunnyCaptureRoll()
    {
        if (!IsSet())
        {
            SetTrap();
        }

        AttemptDailyCapture(
            "Sunny"
        );
    }

    // =========================================================
    // DEBUG - RAIN CAPTURE
    // =========================================================

    [ContextMenu("Debug - Rain Capture Roll")]
    private void DebugRainCaptureRoll()
    {
        if (!IsSet())
        {
            SetTrap();
        }

        AttemptDailyCapture(
            "Rain"
        );
    }

    // =========================================================
    // DEBUG - THUNDER CAPTURE
    // =========================================================

    [ContextMenu("Debug - Thunder Capture Roll")]
    private void DebugThunderCaptureRoll()
    {
        if (!IsSet())
        {
            SetTrap();
        }

        AttemptDailyCapture(
            "RainAndThunder"
        );
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
        return hasBait;
    }

    public bool HasCaughtAnimal()
    {
        return hasCaughtAnimal;
    }

    public bool IsEmpty()
    {
        return currentState ==
               TrapState.Empty;
    }

    public bool IsSet()
    {
        return currentState ==
               TrapState.Set;
    }

    public bool IsCaught()
    {
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
}