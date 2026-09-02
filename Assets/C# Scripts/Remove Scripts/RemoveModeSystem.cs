using System.Collections;
using UnityEngine;

public class RemoveModeSystem : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    public SelectionWheel selectionWheel;
    public Camera mainCamera;
    public Transform player;
    public Animator playerAnimator;
    public PlayerMovement playerMovement;
    public CameraShake2D cameraShake;

    // =========================================================
    // PREVIEWS
    // =========================================================

    [Header("Preview")]
    public GameObject validRemovePreviewPrefab;
    public GameObject invalidRemovePreviewPrefab;

    // =========================================================
    // DETECTION
    // =========================================================

    [Header("Detection")]
    public LayerMask removableLayers;
    public float checkSize = 0.8f;

    // =========================================================
    // RANGE
    // =========================================================

    [Header("Remove Range")]
    public float maxRemoveDistance = 5f;

    // =========================================================
    // ACTION
    // =========================================================

    [Header("Remove Action")]
    public float removeActionDuration = 0.5f;

    // =========================================================
    // WIGGLE
    // =========================================================

    [Header("Destroy Wiggle")]
    public float wiggleAngle = 7f;
    public float wiggleSpeed = 35f;

    // =========================================================
    // PARTICLE
    // =========================================================

    [Header("Destroy Particle Effect")]
    public GameObject destroyParticlePrefab;

    public float destroyParticleLifetime = 2f;

    public Vector3 destroyParticleOffset =
        Vector3.zero;

    // =========================================================
    // PRIVATE
    // =========================================================

    private GameObject validPreview;
    private GameObject invalidPreview;

    private RemovableBuildItem hoveredItem;

    private Coroutine removeCoroutine;

    private bool removing;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        CreatePreviews();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // =====================================================
        // TAB WHEEL
        // =====================================================

        if (selectionWheel == null ||
            selectionWheel.IsWheelOpen())
        {
            HidePreviews();
            hoveredItem = null;
            return;
        }

        // =====================================================
        // REMOVE MODE
        // =====================================================

        if (!selectionWheel.IsRemoveMode())
        {
            HidePreviews();
            hoveredItem = null;
            return;
        }

        // Don't update target while removing.
        if (removing)
        {
            HidePreviews();
            return;
        }

        UpdateHoveredItem();
        UpdatePreview();

        // Player already performing something.
        if (playerMovement != null &&
            playerMovement.IsPerformingAction())
        {
            return;
        }

        // RIGHT CLICK
        if (Input.GetMouseButtonDown(0))
        {
            TryRemove();
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
    // GET MOUSE POSITION
    // =========================================================

    private Vector2 GetMouseWorldPosition()
    {
        if (mainCamera == null)
        {
            return Vector2.zero;
        }

        Vector3 mouse =
            mainCamera.ScreenToWorldPoint(
                Input.mousePosition
            );

        return new Vector2(
            mouse.x,
            mouse.y
        );
    }

    // =========================================================
    // UPDATE HOVER
    // =========================================================

    private void UpdateHoveredItem()
    {
        Vector2 mousePosition =
            GetMouseWorldPosition();

        hoveredItem = null;

        Collider2D[] hits =
            Physics2D.OverlapBoxAll(
                mousePosition,
                Vector2.one *
                checkSize,
                0f,
                removableLayers
            );

        float closestDistance =
            Mathf.Infinity;

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

            if (item == null)
            {
                continue;
            }

            float distance =
                Vector2.Distance(
                    mousePosition,
                    item.transform.position
                );

            if (distance <
                closestDistance)
            {
                closestDistance =
                    distance;

                hoveredItem =
                    item;
            }
        }

        // =====================================================
        // RANGE CHECK
        // =====================================================

        if (hoveredItem != null &&
            player != null)
        {
            float distanceFromPlayer =
                Vector2.Distance(
                    player.position,
                    hoveredItem.transform.position
                );

            if (distanceFromPlayer >
                maxRemoveDistance)
            {
                hoveredItem = null;
            }
        }
    }

    // =========================================================
    // PREVIEW
    // =========================================================

    private void UpdatePreview()
    {
        Vector2 mousePosition =
            GetMouseWorldPosition();

        // =====================================================
        // VALID
        // =====================================================

        if (hoveredItem != null)
        {
            if (validPreview != null)
            {
                validPreview.SetActive(true);

                // Follow the actual object,
                // not a snapped grid location.
                validPreview.transform.position =
                    hoveredItem.transform.position;
            }

            if (invalidPreview != null)
            {
                invalidPreview.SetActive(false);
            }
        }

        // =====================================================
        // INVALID
        // =====================================================

        else
        {
            if (invalidPreview != null)
            {
                invalidPreview.SetActive(true);

                // Normal mouse movement.
                // No snapping.
                invalidPreview.transform.position =
                    mousePosition;
            }

            if (validPreview != null)
            {
                validPreview.SetActive(false);
            }
        }
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
    // TRY REMOVE
    // =========================================================

    private void TryRemove()
    {
        if (removing)
        {
            return;
        }

        if (hoveredItem == null)
        {
            return;
        }

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

        RemovableBuildItem target =
            hoveredItem;

        // =====================================================
        // FACE TARGET
        // =====================================================

        FaceActionPosition(
            target.transform.position
        );

        // =====================================================
        // LOCK PLAYER
        // =====================================================

        if (playerMovement != null)
        {
            playerMovement.StartAction(
                removeActionDuration
            );
        }

        // =====================================================
        // REMOVE ANIMATION
        // =====================================================

        if (playerAnimator != null)
        {
            playerAnimator.ResetTrigger(
                "RemoveAction"
            );

            playerAnimator.SetTrigger(
                "RemoveAction"
            );
        }

        // =====================================================
        // START REMOVE
        // =====================================================

        removeCoroutine =
            StartCoroutine(
                RemoveRoutine(
                    target
                )
            );
    }

    // =========================================================
    // REMOVE ROUTINE
    // =========================================================

    private IEnumerator RemoveRoutine(
        RemovableBuildItem target)
    {
        if (target == null)
        {
            yield break;
        }

        removing = true;

        hoveredItem = null;

        HidePreviews();

        Transform targetTransform =
            target.transform;

        Quaternion originalRotation =
            targetTransform.localRotation;

        float duration =
            Mathf.Max(
                removeActionDuration,
                0.01f
            );

        float timer = 0f;

        // =====================================================
        // WIGGLE DURING ENTIRE REMOVE ACTION
        // =====================================================

        while (timer < duration)
        {
            if (target == null)
            {
                removing = false;
                removeCoroutine = null;
                yield break;
            }

            timer += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    timer /
                    duration
                );

            // Increase the wiggle slightly
            // as destruction gets closer.
            float intensity =
                Mathf.Lerp(
                    0.35f,
                    1f,
                    progress
                );

            float angle =
                Mathf.Sin(
                    timer *
                    wiggleSpeed
                ) *
                wiggleAngle *
                intensity;

            targetTransform.localRotation =
                originalRotation *
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                );

            yield return null;
        }

        // =====================================================
        // RESTORE ROTATION
        // =====================================================

        if (target != null)
        {
            target.transform.localRotation =
                originalRotation;
        }

        // =====================================================
        // DESTROY PARTICLE
        // =====================================================

        if (target != null &&
            destroyParticlePrefab != null)
        {
            GameObject particles =
                Instantiate(
                    destroyParticlePrefab,

                    target.transform.position +
                    destroyParticleOffset,

                    // Always upright.
                    Quaternion.identity
                );

            Destroy(
                particles,
                destroyParticleLifetime
            );
        }

        // =====================================================
        // CAMERA SHAKE
        // =====================================================

        if (cameraShake != null)
        {
            cameraShake.Shake();
        }

        // =====================================================
        // ACTUALLY DESTROY AT THE VERY END
        // =====================================================

        if (target != null)
        {
            target.Remove();
        }

        removing = false;
        removeCoroutine = null;
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
    // DEBUG
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        if (mainCamera == null)
        {
            return;
        }

        Vector2 mousePosition =
            GetMouseWorldPosition();

        Gizmos.DrawWireCube(
            mousePosition,
            Vector3.one *
            checkSize
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