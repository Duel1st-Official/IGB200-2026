using UnityEngine;

public class RemovableBuildItem : MonoBehaviour
{
    [HideInInspector] public Vector2Int gridCell;
    [HideInInspector] public PlotPlacementSystem placementSystem;
    private bool removed;
    public void Setup(Vector2Int cell, PlotPlacementSystem system)
    { gridCell = cell; placementSystem = system; }
    public void Remove()
    {
        if (removed) return;
        removed = true;
        Plot plot = GetComponent<Plot>();
        if (plot == null) plot = GetComponentInChildren<Plot>();
        if (plot != null) plot.ClearContentsForRemoval();
        // Only deliberate player removal frees the cell. Disaster damage never calls this.
        Trap trap = GetComponent<Trap>();
        if (trap == null) trap = GetComponentInChildren<Trap>();
        Plot owner = trap != null ? trap.GetOwningPlot() : null;
        bool removingOnlyTrap = plot == null && owner != null;
        if (removingOnlyTrap) owner.SetOccupied(false);
        if (!removingOnlyTrap && placementSystem != null) placementSystem.FreeGridCell(gridCell);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
