using UnityEngine;
using UnityEngine.Rendering;

public class WorldObjectSorting : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private SpriteRenderer objectRenderer;

    [Header("Sorting")]
    [SerializeField] private int playerSortingOrder = 100;

    [Tooltip("Object order when the player is behind it.")]
    [SerializeField] private int objectInFrontOrder = 110;

    [Tooltip("Object order when the player is in front of it.")]
    [SerializeField] private int objectBehindOrder = 90;

    [Header("Sort Point")]
    [Tooltip(
        "Move this Y offset to the visual bottom/front edge of the object."
    )]
    [SerializeField] private float sortYOffset = -1f;

    private SpriteRenderer playerRenderer;

    private SortingGroup playerSortingGroup;
    private SortingGroup objectSortingGroup;

    private void Awake()
    {
        // =====================================================
        // FIND PLAYER
        // =====================================================

        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag(
                    "Player"
                );

            if (playerObject != null)
            {
                player =
                    playerObject.transform;
            }
        }

        // =====================================================
        // FIND OBJECT RENDERER
        // =====================================================

        if (objectRenderer == null)
        {
            objectRenderer =
                GetComponent<SpriteRenderer>();

            if (objectRenderer == null)
            {
                objectRenderer =
                    GetComponentInChildren<SpriteRenderer>();
            }
        }

        // =====================================================
        // FIND OBJECT SORTING GROUP
        // =====================================================

        objectSortingGroup =
            GetComponent<SortingGroup>();

        // =====================================================
        // FIND PLAYER RENDERING
        // =====================================================

        if (player != null)
        {
            playerSortingGroup =
                player.GetComponent<SortingGroup>();

            playerRenderer =
                player.GetComponent<SpriteRenderer>();

            if (playerRenderer == null)
            {
                playerRenderer =
                    player.GetComponentInChildren<SpriteRenderer>();
            }
        }
    }

    private void Start()
    {
        SetPlayerSortingOrder();
        UpdateSorting();
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        UpdateSorting();
    }

    // =========================================================
    // PLAYER SORTING
    // =========================================================

    private void SetPlayerSortingOrder()
    {
        if (playerSortingGroup != null)
        {
            playerSortingGroup.sortingOrder =
                playerSortingOrder;

            return;
        }

        if (playerRenderer != null)
        {
            playerRenderer.sortingOrder =
                playerSortingOrder;
        }
    }

    // =========================================================
    // UPDATE SORTING
    // =========================================================

    private void UpdateSorting()
    {
        float sortY =
            transform.position.y +
            sortYOffset;

        // =====================================================
        // PLAYER IS ABOVE / BEHIND OBJECT
        // =====================================================

        if (player.position.y > sortY)
        {
            SetObjectSortingOrder(
                objectInFrontOrder
            );
        }

        // =====================================================
        // PLAYER IS BELOW / IN FRONT OF OBJECT
        // =====================================================

        else
        {
            SetObjectSortingOrder(
                objectBehindOrder
            );
        }
    }

    // =========================================================
    // SET OBJECT ORDER
    // =========================================================

    private void SetObjectSortingOrder(
        int order)
    {
        if (objectSortingGroup != null)
        {
            objectSortingGroup.sortingOrder =
                order;

            return;
        }

        if (objectRenderer != null)
        {
            objectRenderer.sortingOrder =
                order;
        }
    }

    // =========================================================
    // DEBUG
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Vector3 sortPoint =
            transform.position +
            new Vector3(
                0f,
                sortYOffset,
                0f
            );

        Gizmos.DrawWireSphere(
            sortPoint,
            0.1f
        );

        Gizmos.DrawLine(
            sortPoint +
            Vector3.left * 2f,
            sortPoint +
            Vector3.right * 2f
        );
    }
}