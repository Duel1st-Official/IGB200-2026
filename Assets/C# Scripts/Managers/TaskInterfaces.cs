using UnityEngine;


/* TaskInterfaces.cs
**
** Shared contracts for player-triggered actions that cost time (hours).
** Any script representing a player action (planting, clearing debris, repairing fencing, etc.)
** should implement ITimeCostAction so it works with existing tile-click / UI handlers
** without them needing to know which specific action it is.
**
** HOW TO USE:
** 1. Implement hourCost — how many hours this action costs when performed.
** 2. Implement TryPerform() — must call TimeManager.instance.TrySpendHours(hourCost) FIRST.
**    If that returns false, TryPerform() must also return false and do nothing else
**    (no game state should change if there aren't enough hours).
** 3. Only apply gameplay effects (e.g. GameManager.instance.ModifyFloraPrey(...)) AFTER
**    TrySpendHours succeeds.
*/

public class TaskInterfaces
{
    
}

public interface ITimeCostAction
{
    int hourCost { get; }
    bool TryPerform();
}

public interface IRepairable
{
    float repairThreshold { get; }
    void Repair(float amount);
}
