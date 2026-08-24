using System.Collections.Generic;
using UnityEngine;

public class PlotPlacementSystem : MonoBehaviour
{
    [Header("References")]
    public SelectionWheel selectionWheel;
    public Camera mainCamera;
    public Transform player;
    public Animator playerAnimator;
    public PlayerMovement playerMovement;

    [Header("Plot")]
    public GameObject plotPrefab;

    [Header("Preview Prefabs")]
    public GameObject validPreviewPrefab;
    public GameObject invalidPreviewPrefab;

    [Header("Grid")]
    public float gridSize = 0.5f;

    [Header("Placement Range")]
    public int maxPlacementBlocks = 5;

    [Header("Collision")]
    public LayerMask blockingLayers;
    public float checkSize = 0.4f;

    [Header("Action Animation")]
    public float placeActionDuration = 0.5f;

    private GameObject validPreview;
    private GameObject invalidPreview;

    private Vector2 currentGridPosition;
    private bool canPlace;

    private HashSet<Vector2Int> occupiedCells =
        new HashSet<Vector2Int>();

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
            HidePreviews();
            return;
        }

        // =========================
        // BUILD MODE ONLY
        // =========================

        if (!selectionWheel.IsBuildMode())
        {
            HidePreviews();
            return;
        }

        // =========================
        // UPDATE PLACEMENT
        // =========================

        UpdateGridPosition();
        CheckPlacement();
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
        // RIGHT CLICK TO PLACE
        // =========================

        if (Input.GetMouseButtonDown(1))
        {
            TryPlacePlot();
        }
    }

    // =========================================================
    // CREATE PREVIEWS
    // =========================================================

    private void CreatePreviews()
    {
        if (validPreviewPrefab != null)
        {
            validPreview =
                Instantiate(validPreviewPrefab);

            validPreview.SetActive(false);
        }

        if (invalidPreviewPrefab != null)
        {
            invalidPreview =
                Instantiate(invalidPreviewPrefab);

            invalidPreview.SetActive(false);
        }
    }

    // =========================================================
    // MOUSE TO GRID
    // =========================================================

    private void UpdateGridPosition()
    {
        if (mainCamera == null)
        {
            return;
        }

        Vector3 mouseWorldPosition =
            mainCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        mouseWorldPosition.z = 0f;

        float snappedX =
            Mathf.Round(
                mouseWorldPosition.x / gridSize
            ) * gridSize;

        float snappedY =
            Mathf.Round(
                mouseWorldPosition.y / gridSize
            ) * gridSize;

        currentGridPosition =
            new Vector2(
                snappedX,
                snappedY
            );
    }

    // =========================================================
    // GRID CELL
    // =========================================================

    public Vector2Int GetGridCell(
        Vector2 position)
    {
        int x =
            Mathf.RoundToInt(
                position.x / gridSize
            );

        int y =
            Mathf.RoundToInt(
                position.y / gridSize
            );

        return new Vector2Int(
            x,
            y
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
            maxPlacementBlocks *
            gridSize;

        float distance =
            Vector2.Distance(
                player.position,
                currentGridPosition
            );

        return distance <= maxDistance;
    }

    // =========================================================
    // CHECK PLACEMENT
    // =========================================================

    private void CheckPlacement()
    {
        // Too far away
        if (!IsWithinRange())
        {
            canPlace = false;
            return;
        }

        Vector2Int cell =
            GetGridCell(
                currentGridPosition
            );

        // Already occupied
        if (occupiedCells.Contains(cell))
        {
            canPlace = false;
            return;
        }

        // World collision
        Collider2D hit =
            Physics2D.OverlapBox(
                currentGridPosition,
                Vector2.one * checkSize,
                0f,
                blockingLayers
            );

        if (hit != null)
        {
            canPlace = false;
            return;
        }

        canPlace = true;
    }

    // =========================================================
    // PREVIEW
    // =========================================================

    private void UpdatePreview()
    {
        // =========================
        // VALID
        // =========================

        if (canPlace)
        {
            if (validPreview != null)
            {
                validPreview.SetActive(true);

                validPreview.transform.position =
                    currentGridPosition;
            }

            if (invalidPreview != null)
            {
                invalidPreview.SetActive(false);
            }
        }

        // =========================
        // INVALID
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
    // PLACE PLOT
    // =========================================================

    private void TryPlacePlot()
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
                "Too far away to place a plot."
            );

            return;
        }

        if (!canPlace)
        {
            Debug.Log(
                "Cannot place plot here."
            );

            return;
        }

        if (plotPrefab == null)
        {
            Debug.LogWarning(
                "Plot Prefab has not been assigned."
            );

            return;
        }

        Vector2Int cell =
            GetGridCell(
                currentGridPosition
            );

        // =========================
        // FACE TARGET
        // =========================

        FaceActionPosition(
            currentGridPosition
        );

        // =========================
        // STOP PLAYER
        // =========================

        if (playerMovement != null)
        {
            playerMovement.StartAction(
                placeActionDuration
            );
        }

        // =========================
        // PLAY ANIMATION
        // =========================

        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger(
                "PlaceAction"
            );

            playerAnimator.SetTrigger(
                "PlaceAction"
            );
        }

        // =========================
        // CREATE PLOT
        // =========================

        GameObject newPlot =
            Instantiate(
                plotPrefab,
                currentGridPosition,
                Quaternion.identity
            );

        // =========================
        // SETUP REMOVABLE ITEM
        // =========================

        RemovableBuildItem removable =
            newPlot.GetComponent
            <RemovableBuildItem>();

        if (removable != null)
        {
            removable.Setup(
                cell,
                this
            );
        }

        // =========================
        // OCCUPY CELL
        // =========================

        occupiedCells.Add(cell);

        CheckPlacement();
        UpdatePreview();
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
    // FREE GRID CELL
    // =========================================================

    public void FreeGridCell(
        Vector2Int cell)
    {
        occupiedCells.Remove(cell);
    }

    // =========================================================
    // DEBUG
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(
            currentGridPosition,
            Vector3.one * checkSize
        );

        if (player != null)
        {
            float radius =
                maxPlacementBlocks *
                gridSize;

            Gizmos.DrawWireSphere(
                player.position,
                radius
            );
        }
    }
}