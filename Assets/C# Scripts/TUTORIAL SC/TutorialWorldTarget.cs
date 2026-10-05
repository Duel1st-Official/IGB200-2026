using UnityEngine;

public class TutorialWorldTarget : MonoBehaviour
{
    [SerializeField] private string targetID = "Target";
    [SerializeField] private Transform focusPoint;

    public string TargetID => targetID;

    public Vector3 GetWorldPosition()
    {
        return focusPoint != null ? focusPoint.position : transform.position;
    }
}
