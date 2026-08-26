using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSort : MonoBehaviour
{
    [Header("Y Sorting")]
    [SerializeField] private int sortingOffset = 0;
    [SerializeField] private int sortingPrecision = 100;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateSorting();
    }

    private void LateUpdate()
    {
        UpdateSorting();
    }

    private void UpdateSorting()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sortingOrder =
            Mathf.RoundToInt(
                -transform.position.y *
                sortingPrecision
            )
            + sortingOffset;
    }
}