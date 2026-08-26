using System.Collections;
using UnityEngine;

public class RemoveModeSystem : MonoBehaviour
{
    [Header("References")]
    public SelectionWheel selectionWheel;
    public Camera mainCamera;
    public Transform player;
    public Animator playerAnimator;
    public PlayerMovement playerMovement;
    public CameraShake2D cameraShake;

    [Header("Preview")]
    public GameObject validRemovePreviewPrefab;
    public GameObject invalidRemovePreviewPrefab;

    [Header("Detection")]
    public LayerMask removableLayers;
    public float checkSize = 0.4f;

    [Header("Remove Range")]
    public float maxRemoveDistance = 2.5f;

    [Header("Action Animation")]
    public float removeActionDuration = 0.5f;

    [Header("Destroy Wiggle")]
    public float wiggleAmount = 6f;
    public float wiggleSpeed = 30f;

    private GameObject validPreview;
    private GameObject invalidPreview;

    private RemovableBuildItem hoveredItem;
    private RemovableBuildItem itemBeingRemoved;

    private Vector2 mouseWorldPosition;

    private Coroutine removeCoroutine;

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

        // Don't allow another removal
        // while one is already happening
        if (itemBeingRemoved != null)
        {
            HidePreviews();
            return;
        }

        DetectRemovableItem();
        UpdatePreview();

        // =========================
        // ACTION LOCK
        // =========================

        if (playerMovement != null &&
            playerMovement.IsPerformingAction())
        {
            return;
        }

        // =========================
        // RIGHT CLICK
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
    }

    // =========================================================
    // RANGE
    // =========================================================

    private bool IsWithinRange()
    {
        if (player == null)
        {
            return true;
        }

        float distance =
            Vector2.Distance(
                player.position,
                mouseWorldPosition
            );

        return distance <=
               maxRemoveDistance;
    }

    // =========================================================
    // DETECT ITEM
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
    // PREVIEW
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
                    mouseWorldPosition;
            }

            if (validPreview != null)
            {
                validPreview.SetActive(false);
            }
        }
    }

    // =========================================================
    // TRY REMOVE
    // =========================================================

    private void TryRemoveItem()
    {
        if (selectionWheel != null &&
            selectionWheel.IsWheelOpen())
        {
            return;
        }

        if (itemBeingRemoved != null)
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

        // =========================
        // SAVE ITEM
        // =========================

        itemBeingRemoved =
            hoveredItem;

        hoveredItem = null;

        Vector2 targetPosition =
            itemBeingRemoved.transform.position;

        HidePreviews();

        // =========================
        // FACE TARGET
        // =========================

        FaceActionPosition(
            targetPosition
        );

        // =========================
        // START ACTION
        // =========================

        if (playerMovement != null)
        {
            playerMovement.StartAction(
                removeActionDuration
            );
        }

        // =========================
        // REMOVE ANIMATION
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
        // START WIGGLE + REMOVE
        // =========================

        removeCoroutine =
            StartCoroutine(
                RemoveWithWiggle()
            );
    }

    // =========================================================
    // WIGGLE THEN DESTROY
    // =========================================================

    private IEnumerator RemoveWithWiggle()
    {
        if (itemBeingRemoved == null)
        {
            yield break;
        }

        Transform itemTransform =
            itemBeingRemoved.transform;

        Quaternion originalRotation =
            itemTransform.localRotation;

        float timer = 0f;

        while (timer < removeActionDuration)
        {
            timer += Time.deltaTime;

            if (itemBeingRemoved == null)
            {
                yield break;
            }

            // =========================
            // WIGGLE
            // =========================

            float progress =
                Mathf.Clamp01(
                    timer / removeActionDuration
                );

            // Gets stronger as destruction gets closer
            float strength =
                Mathf.Lerp(
                    0.3f,
                    1f,
                    progress
                );

            float rotation =
                Mathf.Sin(
                    timer * wiggleSpeed
                )
                * wiggleAmount
                * strength;

            itemTransform.localRotation =
                originalRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    rotation
                );

            yield return null;
        }

        // Restore rotation right before destroy
        if (itemBeingRemoved != null)
        {
            itemTransform.localRotation =
                originalRotation;
        }

        // =========================
        // CAMERA SHAKE
        // =========================

        if (cameraShake != null)
        {
            cameraShake.Shake();
        }

        // =========================
        // DESTROY
        // =========================

        if (itemBeingRemoved != null)
        {
            itemBeingRemoved.Remove();
        }

        itemBeingRemoved = null;
        removeCoroutine = null;
    }

    // =========================================================
    // FACE TARGET
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

        if (direction.sqrMagnitude <=
            0.001f)
        {
            return;
        }

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

    // =========================================================
    // ANIMATOR DIRECTION
    // =========================================================

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
            Gizmos.DrawWireSphere(
                player.position,
                maxRemoveDistance
            );
        }
    }
}