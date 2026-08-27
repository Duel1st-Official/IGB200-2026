using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("References")]
    public Rigidbody2D rb;
    public Animator animator;
    public SelectionWheel selectionWheel;

    [Header("Action Bar")]
    public GameObject actionBar;
    public RectTransform actionBarFill;

    [Header("Action Bar Settings")]
    public float completedBarDisplayTime = 0.05f;

    private Vector2 movementInput;
    private Vector2 lastMoveDirection = Vector2.down;

    private bool actionLocked;
    private Coroutine actionCoroutine;

    private void Start()
    {
        lastMoveDirection = Vector2.down;

        animator.SetFloat("MoveX", 0f);
        animator.SetFloat("MoveY", 0f);

        animator.SetFloat("LastMoveX", 0f);
        animator.SetFloat("LastMoveY", -1f);

        animator.SetBool("IsMoving", false);
        animator.SetBool("IsBuildMode", false);
        animator.SetBool("IsRemoveMode", false);

        if (actionBar != null)
        {
            actionBar.SetActive(false);
        }

        SetBarFill(0f);
    }

    private void Update()
    {
        UpdateCurrentMode();

        // =========================
        // ACTION LOCK
        // =========================

        if (actionLocked)
        {
            movementInput = Vector2.zero;

            animator.SetBool(
                "IsMoving",
                false
            );

            return;
        }

        // =========================
        // MOVEMENT INPUT
        // =========================

        movementInput = Vector2.zero;

        if (Input.GetKey(KeyCode.W))
        {
            movementInput.y = 1f;
        }

        if (Input.GetKey(KeyCode.S))
        {
            movementInput.y = -1f;
        }

        if (Input.GetKey(KeyCode.A))
        {
            movementInput.x = -1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            movementInput.x = 1f;
        }

        if (movementInput.sqrMagnitude > 1f)
        {
            movementInput.Normalize();
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (actionLocked)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity =
            movementInput * moveSpeed;
    }

    // =========================================================
    // CURRENT MODE
    // =========================================================

    private void UpdateCurrentMode()
    {
        if (selectionWheel == null)
        {
            animator.SetBool(
                "IsBuildMode",
                false
            );

            animator.SetBool(
                "IsRemoveMode",
                false
            );

            return;
        }

        animator.SetBool(
            "IsBuildMode",
            selectionWheel.IsBuildMode()
        );

        animator.SetBool(
            "IsRemoveMode",
            selectionWheel.IsRemoveMode()
        );
    }

    // =========================================================
    // ANIMATOR
    // =========================================================

    private void UpdateAnimator()
    {
        bool isMoving =
            movementInput.sqrMagnitude > 0.01f;

        animator.SetBool(
            "IsMoving",
            isMoving
        );

        if (isMoving)
        {
            Vector2 direction =
                GetCardinalDirection(
                    movementInput
                );

            animator.SetFloat(
                "MoveX",
                direction.x
            );

            animator.SetFloat(
                "MoveY",
                direction.y
            );

            lastMoveDirection =
                direction;

            animator.SetFloat(
                "LastMoveX",
                lastMoveDirection.x
            );

            animator.SetFloat(
                "LastMoveY",
                lastMoveDirection.y
            );
        }
        else
        {
            animator.SetFloat(
                "MoveX",
                0f
            );

            animator.SetFloat(
                "MoveY",
                0f
            );
        }
    }

    private Vector2 GetCardinalDirection(
        Vector2 direction)
    {
        if (Mathf.Abs(direction.x) >
            Mathf.Abs(direction.y))
        {
            return direction.x > 0f
                ? Vector2.right
                : Vector2.left;
        }

        return direction.y > 0f
            ? Vector2.up
            : Vector2.down;
    }

    // =========================================================
    // START ACTION
    // =========================================================

    public void StartAction(float duration)
    {
        // IMPORTANT:
        // Completely cancel the previous bar/action coroutine.
        if (actionCoroutine != null)
        {
            StopCoroutine(actionCoroutine);
            actionCoroutine = null;
        }

        actionLocked = true;

        movementInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        animator.SetBool(
            "IsMoving",
            false
        );

        // =========================
        // RESET + SHOW BAR
        // =========================

        if (actionBar != null)
        {
            actionBar.SetActive(true);
        }

        SetBarFill(0f);

        actionCoroutine =
            StartCoroutine(
                ActionLockRoutine(duration)
            );
    }

    // =========================================================
    // ACTION TIMER
    // =========================================================

    private IEnumerator ActionLockRoutine(
        float duration)
    {
        if (duration <= 0f)
        {
            duration = 0.01f;
        }

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    timer / duration
                );

            SetBarFill(progress);

            yield return null;
        }

        // =========================
        // ACTION COMPLETE
        // =========================

        SetBarFill(1f);

        actionLocked = false;

        // Keep the completed bar visible
        // for a very short moment.
        if (completedBarDisplayTime > 0f)
        {
            yield return new WaitForSeconds(
                completedBarDisplayTime
            );
        }

        // =========================
        // HIDE BAR
        // =========================

        if (actionBar != null)
        {
            actionBar.SetActive(false);
        }

        SetBarFill(0f);

        // IMPORTANT:
        // Only clear this AFTER everything
        // in the coroutine has finished.
        actionCoroutine = null;
    }

    // =========================================================
    // BAR FILL
    // =========================================================

    private void SetBarFill(float amount)
    {
        if (actionBarFill == null)
        {
            return;
        }

        amount =
            Mathf.Clamp01(amount);

        Vector3 scale =
            actionBarFill.localScale;

        scale.x = amount;

        actionBarFill.localScale =
            scale;
    }

    // =========================================================
    // PUBLIC CHECK
    // =========================================================

    public bool IsPerformingAction()
    {
        return actionLocked;
    }

    // =========================================================
    // SAFETY
    // =========================================================

    private void OnDisable()
    {
        if (actionCoroutine != null)
        {
            StopCoroutine(actionCoroutine);
            actionCoroutine = null;
        }

        actionLocked = false;

        if (actionBar != null)
        {
            actionBar.SetActive(false);
        }

        SetBarFill(0f);
    }
}