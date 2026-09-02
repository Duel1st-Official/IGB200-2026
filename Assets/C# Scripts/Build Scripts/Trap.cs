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
    // CAUGHT MAMMAL
    // =========================================================

    [Header("Caught Mammal")]

    [Tooltip("Name of the mammal currently inside the trap.")]
    [SerializeField] private string caughtMammalName = "";

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [Tooltip("Mammal name used by the Debug Catch Mammal option.")]
    [SerializeField]
    private string debugMammalName =
        "Bandicoot";

    [SerializeField] private bool showDebugLogs = true;

    // =========================================================
    // PRIVATE
    // =========================================================

    private bool hasBait;
    private bool hasCaughtAnimal;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (startsWithBait)
        {
            SetTrap();
        }
        else
        {
            MakeEmpty();
        }
    }

    // =========================================================
    // SET TRAP
    // =========================================================

    public void SetTrap()
    {
        hasBait = true;

        hasCaughtAnimal = false;

        caughtMammalName = "";

        currentState =
            TrapState.Set;

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " has been set."
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
                "Unknown Mammal";
        }

        hasBait =
            false;

        hasCaughtAnimal =
            true;

        caughtMammalName =
            mammalName;

        currentState =
            TrapState.Caught;

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " caught: " +
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
    // COLLECT CAUGHT MAMMAL
    // =========================================================

    public void CollectCaughtMammal()
    {
        if (currentState !=
            TrapState.Caught)
        {
            return;
        }

        string collectedMammal =
            caughtMammalName;

        // For now we DO NOT store it anywhere.
        // We simply remove it from the trap.

        hasCaughtAnimal =
            false;

        hasBait =
            false;

        caughtMammalName =
            "";

        currentState =
            TrapState.Empty;

        if (showDebugLogs)
        {
            Debug.Log(
                "Collected " +
                collectedMammal +
                " from " +
                gameObject.name +
                "."
            );
        }
    }

    // =========================================================
    // RESET TRAP
    // =========================================================

    public void ResetTrap()
    {
        hasCaughtAnimal =
            false;

        hasBait =
            true;

        caughtMammalName =
            "";

        currentState =
            TrapState.Set;

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " has been reset."
            );
        }
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

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " is empty."
            );
        }
    }

    // =========================================================
    // ADD BAIT
    // =========================================================

    public void AddBait()
    {
        hasCaughtAnimal =
            false;

        hasBait =
            true;

        caughtMammalName =
            "";

        currentState =
            TrapState.Set;

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " has been baited."
            );
        }
    }

    // =========================================================
    // CLEAR CAUGHT ANIMAL
    // =========================================================

    public void ClearCaughtAnimal()
    {
        CollectCaughtMammal();
    }

    // =========================================================
    // DEBUG TEST
    // =========================================================

    [ContextMenu("Debug Catch Mammal")]
    private void DebugCatchMammal()
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
}