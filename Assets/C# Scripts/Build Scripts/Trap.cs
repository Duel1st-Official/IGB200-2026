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
    // SETTINGS
    // =========================================================

    [Header("Trap State")]
    [SerializeField] private TrapState currentState = TrapState.Empty;

    [Header("Trap Settings")]
    [SerializeField] private bool startsWithBait = false;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // =========================================================
    // RUNTIME DATA
    // =========================================================

    [Header("Runtime")]
    [SerializeField] private bool hasBait;
    [SerializeField] private bool hasCaughtAnimal;

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

        currentState = TrapState.Set;

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

    public void TriggerTrap()
    {
        // Only a set trap can catch something.
        if (currentState != TrapState.Set)
        {
            return;
        }

        hasCaughtAnimal = true;
        hasBait = false;

        currentState = TrapState.Caught;

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " has caught an animal."
            );
        }
    }

    // =========================================================
    // RESET TRAP
    // =========================================================

    public void ResetTrap()
    {
        hasCaughtAnimal = false;
        hasBait = true;

        currentState = TrapState.Set;

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " has been reset."
            );
        }
    }

    // =========================================================
    // EMPTY TRAP
    // =========================================================

    public void MakeEmpty()
    {
        hasCaughtAnimal = false;
        hasBait = false;

        currentState = TrapState.Empty;

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " is now empty."
            );
        }
    }

    // =========================================================
    // ADD BAIT
    // =========================================================

    public void AddBait()
    {
        hasBait = true;
        hasCaughtAnimal = false;

        currentState = TrapState.Set;

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " has been baited."
            );
        }
    }

    // =========================================================
    // REMOVE CAUGHT ANIMAL
    // =========================================================

    public void ClearCaughtAnimal()
    {
        if (currentState != TrapState.Caught)
        {
            return;
        }

        hasCaughtAnimal = false;
        hasBait = false;

        currentState = TrapState.Empty;

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " has been cleared."
            );
        }
    }

    // =========================================================
    // GETTERS
    // =========================================================

    public TrapState GetState()
    {
        return currentState;
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
        return currentState == TrapState.Empty;
    }

    public bool IsSet()
    {
        return currentState == TrapState.Set;
    }

    public bool IsCaught()
    {
        return currentState == TrapState.Caught;
    }
}