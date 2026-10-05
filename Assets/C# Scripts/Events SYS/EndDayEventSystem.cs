using System;
using System.Collections.Generic;
using UnityEngine;

public class EndDayEventSystem : MonoBehaviour
{
    // =========================================================
    // EVENT TARGET
    // =========================================================

    public enum EventTarget
    {
        None,

        PreyAvailability,
        PredatorPressure,
        FireRisk,
        SoilHealth,

        BatPopulation,
        BatHealth,
        BatFood
    }

    // =========================================================
    // EVENT DATA
    // =========================================================

    [Serializable]
    public class EndDayEvent
    {
        [Tooltip("Internal event name.")]
        public string eventName =
            "New Event";

        [Tooltip("Title eventually displayed in the Night Report UI.")]
        public string title =
            "NEW EVENT";

        [TextArea(2, 5)]
        [Tooltip("Description eventually displayed in the Night Report UI.")]
        public string description =
            "Something happened overnight.";

        [Tooltip("Optional sprite for the future Night Report UI.")]
        public Sprite icon;

        [Tooltip("Use the burnt report background for an extreme destructive event. Does not change its stats or chance.")]
        public bool extremeEvent = false;
        [Tooltip("Destroy random plots when this event is applied. Enable for custom fire events.")]
        public bool destroysPlots;
        [Min(0)] public int plotsToDestroy = 2;

        [Tooltip(
            "Relative chance of this event being selected. " +
            "Higher weight = more common."
        )]
        [Min(0f)]
        public float weight =
            10f;

        [Tooltip("Which game stat this event changes.")]
        public EventTarget target =
            EventTarget.None;

        [Tooltip(
            "Signed amount applied to the target. " +
            "Example: +15 or -10."
        )]
        public float amount =
            0f;
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    [Tooltip("Automatically found if empty.")]
    [SerializeField]
    private RangerStation rangerStation;

    [Tooltip("Automatically found if empty.")]
    [SerializeField]
    private BatColony batColony;

    [Tooltip("Automatically found if empty.")]
    [SerializeField]
    private EndDaySystem endDaySystem;

    // =========================================================
    // EVENT SETTINGS
    // =========================================================

    [Header("Event Settings")]

    [Tooltip(
        "All simple events that can currently be rolled."
    )]
    [SerializeField]
    private List<EndDayEvent> events =
        new List<EndDayEvent>();

    [Tooltip(
        "Automatically create the default Ghost Bat event pool " +
        "when the list is empty."
    )]
    [SerializeField]
    private bool createDefaultEventsIfEmpty =
        true;

    [Tooltip(
        "Prevent the exact same event from being selected " +
        "two times in a row when another valid event exists."
    )]
    [SerializeField]
    private bool preventImmediateRepeat =
        true;

    // =========================================================
    // LAST EVENT
    // =========================================================

    [Header("Last Event")]

    [SerializeField]
    private string lastEventName =
        "";

    [SerializeField]
    private string lastEventTitle =
        "";

    [TextArea(2, 5)]
    [SerializeField]
    private string lastEventDescription =
        "";

    [SerializeField]
    private Sprite lastEventIcon;

    [SerializeField]
    private bool lastEventExtreme;

    [SerializeField]
    private EventTarget lastEventTarget =
        EventTarget.None;

    [SerializeField]
    private float lastEventAmount =
        0f;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLogs =
        true;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        AutoAssignReferences();

        if (createDefaultEventsIfEmpty &&
            (
                events == null ||
                events.Count == 0
            ))
        {
            CreateDefaultEvents();
        }
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        AutoAssignReferences();
    }

    // =========================================================
    // AUTO ASSIGN
    // =========================================================

    private void AutoAssignReferences()
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
    // CREATE DEFAULT EVENTS
    // =========================================================

    [ContextMenu("Events - Create Default Event Pool")]
    public void CreateDefaultEvents()
    {
        if (events == null)
        {
            events =
                new List<EndDayEvent>();
        }

        events.Clear();

        // =====================================================
        // ENVIRONMENT
        // =====================================================

        AddDefaultEvent(
            "Overnight Rain",
            "OVERNIGHT RAIN",
            "Rain swept across the reserve overnight, reducing dry conditions.",
            12f,
            EventTarget.FireRisk,
            -15f
        );

        AddDefaultEvent(
            "Dry Conditions",
            "DRY CONDITIONS",
            "A warm, dry night increased fire danger across the reserve.",
            10f,
            EventTarget.FireRisk,
            10f
        );

        // =====================================================
        // PREY
        // =====================================================

        AddDefaultEvent(
            "Prey Boom",
            "PREY BOOM",
            "Rangers recorded unusually high small-animal activity across Ghost Bat foraging habitat.",
            10f,
            EventTarget.PreyAvailability,
            15f
        );

        AddDefaultEvent(
            "Poor Foraging Conditions",
            "POOR FORAGING CONDITIONS",
            "Low wildlife activity made hunting more difficult around the reserve.",
            10f,
            EventTarget.PreyAvailability,
            -10f
        );

        // =====================================================
        // PREDATORS
        // =====================================================

        AddDefaultEvent(
            "Feral Cat Sighting",
            "FERAL CAT SIGHTING",
            "Camera traps detected a feral cat moving through Ghost Bat foraging habitat.",
            8f,
            EventTarget.PredatorPressure,
            15f
        );

        AddDefaultEvent(
            "Fox Activity",
            "FOX ACTIVITY",
            "Fresh fox tracks were discovered near wildlife habitat during the morning survey.",
            8f,
            EventTarget.PredatorPressure,
            10f
        );

        AddDefaultEvent(
            "Predator Activity Declines",
            "QUIETER PREDATOR ACTIVITY",
            "Ranger surveys found fewer signs of introduced predators around the reserve.",
            8f,
            EventTarget.PredatorPressure,
            -10f
        );

        // =====================================================
        // SOIL / HABITAT
        // =====================================================

        AddDefaultEvent(
            "Native Regrowth",
            "NATIVE REGROWTH",
            "Native vegetation showed strong overnight recovery around restored habitat.",
            8f,
            EventTarget.SoilHealth,
            10f
        );

        AddDefaultEvent(
            "Soil Erosion",
            "SOIL EROSION",
            "Loose soil and disturbed ground reduced habitat quality in part of the reserve.",
            8f,
            EventTarget.SoilHealth,
            -10f
        );

        // =====================================================
        // COLONY FOOD
        // =====================================================

        AddDefaultEvent(
            "Successful Hunting Night",
            "SUCCESSFUL HUNTING NIGHT",
            "The Ghost Bat colony had a productive night of hunting.",
            10f,
            EventTarget.BatFood,
            10f
        );

        AddDefaultEvent(
            "Poor Hunting Night",
            "POOR HUNTING NIGHT",
            "The colony returned from foraging with fewer successful hunts than usual.",
            10f,
            EventTarget.BatFood,
            -10f
        );

        // =====================================================
        // COLONY HEALTH
        // =====================================================

        AddDefaultEvent(
            "Healthy Night",
            "COLONY RESTED WELL",
            "Conditions around the roost remained calm and the colony benefited from an undisturbed night.",
            7f,
            EventTarget.BatHealth,
            5f
        );

        AddDefaultEvent(
            "Roost Disturbance",
            "ROOST DISTURBANCE",
            "Unexpected activity near the roost disturbed several Ghost Bats during the night.",
            7f,
            EventTarget.BatHealth,
            -5f
        );

        // =====================================================
        // POPULATION
        // =====================================================

        AddDefaultEvent(
            "New Pup",
            "A NEW GHOST BAT PUP!",
            "Rangers have confirmed a new Ghost Bat pup within the colony.",
            2f,
            EventTarget.BatPopulation,
            1f
        );

        // =====================================================
        // QUIET NIGHT
        // =====================================================

        AddDefaultEvent(
            "Quiet Night",
            "QUIET NIGHT",
            "The reserve remained calm overnight. Rangers recorded no significant changes.",
            14f,
            EventTarget.None,
            0f
        );

        if (showDebugLogs)
        {
            Debug.Log(
                "[EndDayEventSystem] Created " +
                events.Count +
                " default events."
            );
        }
    }

    // =========================================================
    // EXTREME EVENT POOL
    // =========================================================

    [ContextMenu("Events - Add Missing Extreme Events")]
    public void AddMissingExtremeEvents()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.Undo.RecordObject(this, "Add Extreme Events");
        }
#endif

        if (events == null)
        {
            events = new List<EndDayEvent>();
        }

        // Append only. Existing event settings and icon assignments are preserved.
        AddExtremeEventIfMissing(
            "Bushfire Damage", "BUSHFIRE DAMAGE",
            "A bushfire swept through part of the reserve, burning vegetation and severely damaging the soil.",
            EventTarget.SoilHealth, -25f);

        AddExtremeEventIfMissing(
            "Extreme Heatwave", "EXTREME HEATWAVE",
            "Intense overnight heat placed the Ghost Bat colony under severe stress, reducing colony health.",
            EventTarget.BatHealth, -15f);

        AddExtremeEventIfMissing(
            "Predator Surge", "PREDATOR SURGE",
            "Camera traps recorded a sudden surge of introduced predators across the reserve's foraging habitat.",
            EventTarget.PredatorPressure, 30f);

        AddExtremeEventIfMissing(
            "Prey Collapse", "PREY COLLAPSE",
            "Ranger surveys recorded a sharp collapse in small-animal activity, leaving far fewer hunting opportunities for the colony.",
            EventTarget.PreyAvailability, -25f);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
            if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(this))
            {
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            }
        }
#endif

        if (showDebugLogs)
        {
            Debug.Log("[EndDayEventSystem] Extreme events added where missing. Existing events were preserved.", this);
        }
    }

    private void AddExtremeEventIfMissing(
        string eventName, string title, string description,
        EventTarget target, float amount)
    {
        foreach (EndDayEvent existing in events)
        {
            if (existing != null && string.Equals(
                existing.eventName, eventName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        events.Add(new EndDayEvent
        {
            eventName = eventName,
            title = title,
            description = description,
            weight = 0.5f,
            target = target,
            amount = amount,
            extremeEvent = true
        });
    }

    // =========================================================
    // ADD DEFAULT EVENT
    // =========================================================

    private void AddDefaultEvent(
        string eventName,
        string title,
        string description,
        float weight,
        EventTarget target,
        float amount)
    {
        EndDayEvent newEvent =
            new EndDayEvent();

        newEvent.eventName =
            eventName;

        newEvent.title =
            title;

        newEvent.description =
            description;

        newEvent.weight =
            Mathf.Max(
                0f,
                weight
            );

        newEvent.target =
            target;

        newEvent.amount =
            amount;

        events.Add(
            newEvent
        );
    }

    // =========================================================
    // ROLL RANDOM EVENT
    // =========================================================

    public EndDayEvent RollRandomEvent()
    {
        AutoAssignReferences();

        if (events == null ||
            events.Count == 0)
        {
            if (createDefaultEventsIfEmpty)
            {
                CreateDefaultEvents();
            }
        }

        if (events == null ||
            events.Count == 0)
        {
            Debug.LogWarning(
                "[EndDayEventSystem] There are no events to roll."
            );

            return null;
        }

        List<EndDayEvent> validEvents =
            GetValidEvents();

        if (validEvents.Count == 0)
        {
            Debug.LogWarning(
                "[EndDayEventSystem] No valid weighted events were found."
            );

            return null;
        }

        float totalWeight =
            0f;

        for (int i = 0;
             i < validEvents.Count;
             i++)
        {
            totalWeight +=
                Mathf.Max(
                    0f,
                    validEvents[i].weight
                );
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float randomValue =
            UnityEngine.Random.Range(
                0f,
                totalWeight
            );

        float currentWeight =
            0f;

        EndDayEvent selectedEvent =
            validEvents[
                validEvents.Count - 1
            ];

        for (int i = 0;
             i < validEvents.Count;
             i++)
        {
            currentWeight +=
                Mathf.Max(
                    0f,
                    validEvents[i].weight
                );

            if (randomValue <=
                currentWeight)
            {
                selectedEvent =
                    validEvents[i];

                break;
            }
        }

        ApplyEvent(
            selectedEvent
        );

        return selectedEvent;
    }

    // =========================================================
    // VALID EVENTS
    // =========================================================

    private List<EndDayEvent> GetValidEvents()
    {
        List<EndDayEvent> validEvents =
            new List<EndDayEvent>();

        bool hasAlternative =
            false;

        if (preventImmediateRepeat &&
            !string.IsNullOrEmpty(
                lastEventName
            ))
        {
            for (int i = 0;
                 i < events.Count;
                 i++)
            {
                EndDayEvent eventData =
                    events[i];

                if (eventData == null ||
                    eventData.weight <= 0f)
                {
                    continue;
                }

                if (eventData.eventName !=
                    lastEventName)
                {
                    hasAlternative =
                        true;

                    break;
                }
            }
        }

        for (int i = 0;
             i < events.Count;
             i++)
        {
            EndDayEvent eventData =
                events[i];

            if (eventData == null ||
                eventData.weight <= 0f)
            {
                continue;
            }

            if (preventImmediateRepeat &&
                hasAlternative &&
                eventData.eventName ==
                    lastEventName)
            {
                continue;
            }

            validEvents.Add(
                eventData
            );
        }

        return validEvents;
    }

    // =========================================================
    // APPLY EVENT
    // =========================================================

    public void ApplyEvent(
        EndDayEvent eventData)
    {
        if (eventData == null)
        {
            return;
        }

        AutoAssignReferences();

        float beforeValue =
            GetCurrentTargetValue(
                eventData.target
            );

        switch (eventData.target)
        {
            // =================================================
            // RANGER STATION
            // =================================================

            case EventTarget.PreyAvailability:

                if (rangerStation != null)
                {
                    rangerStation.AddPreyEventModifier(
                        eventData.amount
                    );
                }

                break;

            case EventTarget.PredatorPressure:

                if (rangerStation != null)
                {
                    rangerStation.AddPredatorEventModifier(
                        eventData.amount
                    );
                }

                break;

            case EventTarget.FireRisk:

                if (rangerStation != null)
                {
                    rangerStation.AddFireEventModifier(
                        eventData.amount
                    );
                }

                break;

            case EventTarget.SoilHealth:

                if (rangerStation != null)
                {
                    rangerStation.AddSoilHealth(
                        eventData.amount
                    );
                }

                break;

            // =================================================
            // BAT COLONY
            // =================================================

            case EventTarget.BatPopulation:

                if (batColony != null)
                {
                    int populationChange =
                        Mathf.RoundToInt(
                            eventData.amount
                        );

                    if (populationChange >= 0)
                    {
                        batColony.AddPopulation(
                            populationChange
                        );
                    }
                    else
                    {
                        batColony.RemovePopulation(
                            Mathf.Abs(
                                populationChange
                            )
                        );
                    }
                }

                break;

            case EventTarget.BatHealth:

                if (batColony != null)
                {
                    batColony.AddBatHealth(
                        eventData.amount
                    );
                }

                break;

            case EventTarget.BatFood:

                if (batColony != null)
                {
                    batColony.AddBatFood(
                        eventData.amount
                    );
                }

                break;

            case EventTarget.None:
            default:
                break;
        }

        int plotsLost = 0;
        // Exact legacy fire-event name, not FireRisk (risk alone is not a fire).
        bool legacyFire = string.Equals(eventData.eventName, "Bushfire Damage", StringComparison.OrdinalIgnoreCase);
        if (eventData.destroysPlots || legacyFire)
        {
            PlotDisasterSystem disasters = PlotDisasterSystem.GetOrCreate();
            int count = eventData.destroysPlots ? eventData.plotsToDestroy : disasters.defaultFirePlotCount;
            plotsLost = disasters.DestroyRandomPlots(count, "Fire event: " + eventData.title);
        }

        float afterValue =
            GetCurrentTargetValue(
                eventData.target
            );

        StoreLastEvent(
            eventData
        );
        if (plotsLost > 0) lastEventDescription += "\nPlots destroyed: " + plotsLost + ". Clear the debris before rebuilding.";

        if (showDebugLogs)
        {
            Debug.Log(
                "====================================" +
                "\n[END DAY EVENT]" +
                "\n" +
                eventData.title +
                "\n" +
                eventData.description +
                "\nTarget: " +
                eventData.target +
                "\nRequested Change: " +
                FormatSignedNumber(
                    eventData.amount
                ) +
                "\nValue: " +
                beforeValue.ToString("0.#") +
                " -> " +
                afterValue.ToString("0.#") +
                "\n===================================="
            );
        }
    }

    // =========================================================
    // CURRENT TARGET VALUE
    // =========================================================

    private float GetCurrentTargetValue(
        EventTarget target)
    {
        switch (target)
        {
            case EventTarget.PreyAvailability:

                return
                    rangerStation != null
                        ? rangerStation.GetPreyAvailability()
                        : 0f;

            case EventTarget.PredatorPressure:

                return
                    rangerStation != null
                        ? rangerStation.GetPredatorPressure()
                        : 0f;

            case EventTarget.FireRisk:

                return
                    rangerStation != null
                        ? rangerStation.GetFireRisk()
                        : 0f;

            case EventTarget.SoilHealth:

                return
                    rangerStation != null
                        ? rangerStation.GetSoilHealth()
                        : 0f;

            case EventTarget.BatPopulation:

                return
                    batColony != null
                        ? batColony.GetPopulation()
                        : 0f;

            case EventTarget.BatHealth:

                return
                    batColony != null
                        ? batColony.GetBatHealth()
                        : 0f;

            case EventTarget.BatFood:

                return
                    batColony != null
                        ? batColony.GetBatFood()
                        : 0f;

            default:
                return 0f;
        }
    }

    // =========================================================
    // STORE LAST EVENT
    // =========================================================

    private void StoreLastEvent(
        EndDayEvent eventData)
    {
        lastEventExtreme = eventData.extremeEvent;

        lastEventName =
            eventData.eventName;

        lastEventTitle =
            eventData.title;

        lastEventDescription =
            eventData.description;

        lastEventIcon =
            eventData.icon;

        lastEventTarget =
            eventData.target;

        lastEventAmount =
            eventData.amount;
    }

    // =========================================================
    // PUBLIC GETTERS
    // =========================================================

    public string GetLastEventName()
    {
        return lastEventName;
    }

    public string GetLastEventTitle()
    {
        return lastEventTitle;
    }

    public string GetLastEventDescription()
    {
        return lastEventDescription;
    }

    public Sprite GetLastEventIcon()
    {
        return lastEventIcon;
    }

    public EventTarget GetLastEventTarget()
    {
        return lastEventTarget;
    }

    public float GetLastEventAmount()
    {
        return lastEventAmount;
    }

    public int GetEventCount()
    {
        return
            events != null
                ? events.Count
                : 0;
    }

    public bool IsLastEventExtreme()
    {
        return lastEventExtreme;
    }

    // =========================================================
    // FORMAT NUMBER
    // =========================================================

    private string FormatSignedNumber(
        float value)
    {
        if (value > 0f)
        {
            return
                "+" +
                value.ToString("0.#");
        }

        return
            value.ToString("0.#");
    }

    // =========================================================
    // FIND EVENT
    // =========================================================

    private EndDayEvent FindEvent(
        string eventName)
    {
        if (events == null)
        {
            return null;
        }

        for (int i = 0;
             i < events.Count;
             i++)
        {
            EndDayEvent eventData =
                events[i];

            if (eventData == null)
            {
                continue;
            }

            if (eventData.eventName ==
                eventName)
            {
                return eventData;
            }
        }

        return null;
    }

    // =========================================================
    // APPLY NAMED EVENT
    // =========================================================

    private void ApplyNamedEvent(
        string eventName)
    {
        EndDayEvent eventData =
            FindEvent(
                eventName
            );

        if (eventData == null)
        {
            Debug.LogWarning(
                "[EndDayEventSystem] Event not found: " +
                eventName
            );

            return;
        }

        ApplyEvent(
            eventData
        );
    }

    // =========================================================
    // DEBUG
    // =========================================================

    [ContextMenu("Debug - Roll Random Event")]
    private void DebugRollRandomEvent()
    {
        RollRandomEvent();
    }

    [ContextMenu("Debug - Overnight Rain")]
    private void DebugOvernightRain()
    {
        ApplyNamedEvent(
            "Overnight Rain"
        );
    }

    [ContextMenu("Debug - Dry Conditions")]
    private void DebugDryConditions()
    {
        ApplyNamedEvent(
            "Dry Conditions"
        );
    }

    [ContextMenu("Debug - Prey Boom")]
    private void DebugPreyBoom()
    {
        ApplyNamedEvent(
            "Prey Boom"
        );
    }

    [ContextMenu("Debug - Poor Foraging Conditions")]
    private void DebugPoorForaging()
    {
        ApplyNamedEvent(
            "Poor Foraging Conditions"
        );
    }

    [ContextMenu("Debug - Feral Cat Sighting")]
    private void DebugFeralCat()
    {
        ApplyNamedEvent(
            "Feral Cat Sighting"
        );
    }

    [ContextMenu("Debug - Fox Activity")]
    private void DebugFoxActivity()
    {
        ApplyNamedEvent(
            "Fox Activity"
        );
    }

    [ContextMenu("Debug - Predator Activity Declines")]
    private void DebugPredatorDecline()
    {
        ApplyNamedEvent(
            "Predator Activity Declines"
        );
    }

    [ContextMenu("Debug - Native Regrowth")]
    private void DebugNativeRegrowth()
    {
        ApplyNamedEvent(
            "Native Regrowth"
        );
    }

    [ContextMenu("Debug - Soil Erosion")]
    private void DebugSoilErosion()
    {
        ApplyNamedEvent(
            "Soil Erosion"
        );
    }

    [ContextMenu("Debug - Successful Hunting Night")]
    private void DebugSuccessfulHunt()
    {
        ApplyNamedEvent(
            "Successful Hunting Night"
        );
    }

    [ContextMenu("Debug - Poor Hunting Night")]
    private void DebugPoorHunt()
    {
        ApplyNamedEvent(
            "Poor Hunting Night"
        );
    }

    [ContextMenu("Debug - Healthy Night")]
    private void DebugHealthyNight()
    {
        ApplyNamedEvent(
            "Healthy Night"
        );
    }

    [ContextMenu("Debug - Roost Disturbance")]
    private void DebugRoostDisturbance()
    {
        ApplyNamedEvent(
            "Roost Disturbance"
        );
    }

    [ContextMenu("Debug - New Pup")]
    private void DebugNewPup()
    {
        ApplyNamedEvent(
            "New Pup"
        );
    }

    [ContextMenu("Debug - Quiet Night")]
    private void DebugQuietNight()
    {
        ApplyNamedEvent(
            "Quiet Night"
        );
    }

    [ContextMenu("Debug - Clear Environmental Event Modifiers")]
    private void DebugClearEnvironmentalModifiers()
    {
        AutoAssignReferences();

        if (rangerStation != null)
        {
            rangerStation.ClearEventModifiers();
        }
    }

    [ContextMenu("Debug - Auto Assign References")]
    private void DebugAutoAssignReferences()
    {
        AutoAssignReferences();

        Debug.Log(
            "[EndDayEventSystem]" +
            "\nRanger Station: " +
            (
                rangerStation != null
                    ? rangerStation.name
                    : "NOT FOUND"
            ) +
            "\nBat Colony: " +
            (
                batColony != null
                    ? batColony.name
                    : "NOT FOUND"
            ) +
            "\nEnd Day System: " +
            (
                endDaySystem != null
                    ? endDaySystem.name
                    : "NOT FOUND"
            ) +
            "\nEvents: " +
            GetEventCount()
        );
    }
}

