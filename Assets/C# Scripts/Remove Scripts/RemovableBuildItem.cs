using UnityEngine;

public class RemovableBuildItem : MonoBehaviour
{
    [HideInInspector]
    public Vector2Int gridCell;

    [HideInInspector]
    public PlotPlacementSystem placementSystem;

    public void Setup(
        Vector2Int cell,
        PlotPlacementSystem system)
    {
        gridCell = cell;
        placementSystem = system;
    }

    public void Remove()
    {
        if (placementSystem != null)
        {
            placementSystem.FreeGridCell(
                gridCell
            );
        }

        Destroy(gameObject);
    }
}