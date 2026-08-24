using UnityEngine;

public class RemoveModeSystem : MonoBehaviour
{
    [Header("References")]
    public SelectionWheel selectionWheel;
    public Camera mainCamera;
    public Transform player;
    public Animator playerAnimator;
    public PlayerMovement playerMovement;

    [Header("Preview")]
    public GameObject validRemovePreviewPrefab;
    public GameObject invalidRemovePreviewPrefab;

    [Header("Detection")]
    public LayerMask removableLayers;
    public float checkSize = 0.4f;

    [Header("Grid")]
    public float gridSize = 0.5f;

    [Header("Remove Range")]
    public int maxRemoveBlocks = 5;

    [Header("Action Animation")]
    public float removeActionDuration = 0.5f;

    private GameObject validPreview;
    private GameObject invalidPreview;

    private RemovableBuildItem hoveredItem;

    private Vector2 mouseWorldPosition;
    private Vector2 currentGridPosition;

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        CreatePreviews();
    }

    private void Update()
    {
        // =========================
        // TAB WHEEL OPEN
        // =========================

        if (selectionWheel == null ||
            selectionWheel.IsWheelOpen())
        {
            hoveredItem = null;

            HidePreviews();

            return;
        }

        // =========================
        // REMOVE MODE ONLY
        // =========================

        if (!selectionWheel.IsRemoveMode())
        {
            hoveredItem = null;

            HidePreviews();

            return;
        }

        UpdateMousePosition();
        DetectRemovableItem();
        UpdatePreview();

        // =========================
        // PLAYER CURRENTLY ACTING
        // =========================

        if (playerMovement != null &&
            playerMovement.IsPerformingAction())
        {
            return;
        }

        // =========================
        // RIGHT CLICK TO REMOVE
        // =========================

        if (Input.GetMouseButtonDown(1))
        {
            TryRemoveItem();
        }
    }

    // =========================================================
    // CREATE PREVIEWS
    // =========================================================

    private void CreatePreviews()
    {
        if (validRemovePreviewPrefab != null)
        {
            validPreview =
                Instantiate(
                    validRemovePreviewPrefab
                );

            validPreview.SetActive(false);
        }

        if (invalidRemovePreviewPrefab != null)
        {
            invalidPreview =
                Instantiate(
                    invalidRemovePreviewPrefab
                );

            invalidPreview.SetActive(false);
        }
    }

    // =========================================================
    // MOUSE POSITION
    // =========================================================

    private void UpdateMousePosition()
    {
        if (mainCamera == null)
        {
            return;
        }

        Vector3 worldPosition =
            mainCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        worldPosition.z = 0f;

        mouseWorldPosition =
            new Vector2(
                worldPosition.x,
                worldPosition.y
            );

        float snappedX =
            Mathf.Round(
                worldPosition.x / gridSize
            ) * gridSize;

        float snappedY =
            Mathf.Round(
                worldPosition.y / gridSize
            ) * gridSize;

        currentGridPosition =
            new Vector2(
                snappedX,
                snappedY
            );
    }

    // =========================================================
    // RANGE CHECK
    // =========================================================

    private bool IsWithinRange()
    {
        if (player == null)
        {
            return true;
        }

        float maxDistance =
            maxRemoveBlocks *
            gridSize;

        float distance =
            Vector2.Distance(
                player.position,
                currentGridPosition
            );

        return distance <= maxDistance;
    }

    // =========================================================
    // DETECT REMOVABLE ITEM
    // =========================================================

    private void DetectRemovableItem()
    {
        hoveredItem = null;

        if (!IsWithinRange())
        {
            return;
        }

        Collider2D[] hits =
            Physics2D.OverlapBoxAll(
                mouseWorldPosition,
                Vector2.one * checkSize,
                0f,
                removableLayers
            );

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            RemovableBuildItem item =
                hit.GetComponent
                <RemovableBuildItem>();

            if (item == null)
            {
                item =
                    hit.GetComponentInParent
                    <RemovableBuildItem>();
            }

            if (item != null)
            {
                hoveredItem = item;
                return;
            }
        }
    }

    // =========================================================
    // UPDATE PREVIEW
    // =========================================================

    private void UpdatePreview()
    {
        bool withinRange =
            IsWithinRange();

        // =========================
        // GREEN
        // =========================

        if (hoveredItem != null &&
            withinRange)
        {
            if (validPreview != null)
            {
                validPreview.SetActive(true);

                validPreview.transform.position =
                    hoveredItem.transform.position;
            }

            if (invalidPreview != null)
            {
                invalidPreview.SetActive(false);
            }
        }

        // =========================
        // RED
        // =========================

        else
        {
            if (invalidPreview != null)
            {
                invalidPreview.SetActive(true);

                invalidPreview.transform.position =
                    currentGridPosition;
            }

            if (validPreview != null)
            {
                validPreview.SetActive(false);
            }
        }
    }

    // =========================================================
    // REMOVE ITEM
    // =========================================================

    private void TryRemoveItem()
    {
        // Extra safety
        if (selectionWheel != null &&
            selectionWheel.IsWheelOpen())
        {
            return;
        }

        if (playerMovement != null &&
            playerMovement.IsPerformingAction())
        {
            return;
        }

        if (!IsWithinRange())
        {
            Debug.Log(
                "Too far away to remove this item."
            );

            return;
        }

        if (hoveredItem == null)
        {
            Debug.Log(
                "Nothing removable here."
            );

            return;
        }

        Vector2 targetPosition =
            hoveredItem.transform.position;

        // =========================
        // FACE TARGET
        // =========================

        FaceActionPosition(
            targetPosition
        );

        // =========================
        // STOP PLAYER
        // =========================

        if (playerMovement != null)
        {
            playerMovement.StartAction(
                removeActionDuration
            );
        }

        // =========================
        // PLAY ANIMATION
        // =========================

        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger(
                "RemoveAction"
            );

            playerAnimator.SetTrigger(
                "RemoveAction"
            );
        }

        // =========================
        // REMOVE OBJECT
        // =========================

        hoveredItem.Remove();

        hoveredItem = null;
    }

    // =========================================================
    // FACE ACTION POSITION
    // =========================================================

    private void FaceActionPosition(
        Vector2 targetPosition)
    {
        if (player == null ||
            playerAnimator == null)
        {
            return;
        }

        Vector2 direction =
            targetPosition -
            (Vector2)player.position;

        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        // LEFT / RIGHT
        if (Mathf.Abs(direction.x) >
            Mathf.Abs(direction.y))
        {
            if (direction.x > 0f)
            {
                SetAnimatorDirection(
                    1f,
                    0f
                );
            }
            else
            {
                SetAnimatorDirection(
                    -1f,
                    0f
                );
            }
        }

        // UP / DOWN
        else
        {
            if (direction.y > 0f)
            {
                SetAnimatorDirection(
                    0f,
                    1f
                );
            }
            else
            {
                SetAnimatorDirection(
                    0f,
                    -1f
                );
            }
        }
    }

    private void SetAnimatorDirection(
        float x,
        float y)
    {
        playerAnimator.SetFloat(
            "LastMoveX",
            x
        );

        playerAnimator.SetFloat(
            "LastMoveY",
            y
        );
    }

    // =========================================================
    // HIDE PREVIEWS
    // =========================================================

    private void HidePreviews()
    {
        if (validPreview != null)
        {
            validPreview.SetActive(false);
        }

        if (invalidPreview != null)
        {
            invalidPreview.SetActive(false);
        }
    }

    // =========================================================
    // DEBUG
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(
            mouseWorldPosition,
            Vector3.one * checkSize
        );

        if (player != null)
        {
            float radius =
                maxRemoveBlocks *
                gridSize;

            Gizmos.DrawWireSphere(
                player.position,
                radius
            );
        }
    }
}