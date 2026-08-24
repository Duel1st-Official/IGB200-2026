using UnityEngine;

public class Plot : MonoBehaviour
{
    [Header("Plot State")]
    public bool occupied;
    public bool planted;

    public void Plant()
    {
        if (planted)
        {
            return;
        }

        planted = true;

        Debug.Log("Plot planted.");
    }
}