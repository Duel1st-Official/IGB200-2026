using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("References")]
    public Rigidbody2D rb;
    public Animator animator;

    [Header("Placement")]
    public KeyCode placeKey = KeyCode.E;

    [Header("Movement Info")]
    public Vector2 movementInput;
    public Vector2 lastMoveDirection = Vector2.down;

    [Header("Animation Values")]
    public float moveX;
    public float moveY;

    private void Start()
    {
        lastMoveDirection = Vector2.down;

        animator.SetFloat("MoveX", 0f);
        animator.SetFloat("MoveY", 0f);

        animator.SetFloat("LastMoveX", 0f);
        animator.SetFloat("LastMoveY", -1f);

        animator.SetBool("IsMoving", false);
    }

    private void Update()
    {
        // =========================
        // PLACEMENT
        // =========================

        if (Input.GetKeyDown(placeKey))
        {
            PlayPlacement();
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
        rb.linearVelocity = movementInput * moveSpeed;
    }

    private void UpdateAnimator()
    {
        bool isMoving = movementInput.sqrMagnitude > 0.01f;

        animator.SetBool("IsMoving", isMoving);

        if (isMoving)
        {
            Vector2 direction = GetCardinalDirection(movementInput);

            moveX = direction.x;
            moveY = direction.y;

            animator.SetFloat("MoveX", moveX);
            animator.SetFloat("MoveY", moveY);

            lastMoveDirection = direction;

            animator.SetFloat("LastMoveX", lastMoveDirection.x);
            animator.SetFloat("LastMoveY", lastMoveDirection.y);
        }
        else
        {
            moveX = 0f;
            moveY = 0f;

            animator.SetFloat("MoveX", 0f);
            animator.SetFloat("MoveY", 0f);
        }
    }

    private void PlayPlacement()
    {
        // Stop movement immediately
        movementInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        animator.SetBool("IsMoving", false);

        // Fire one-shot placement animation
        animator.SetTrigger("Place");
    }

    private Vector2 GetCardinalDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            return direction.x > 0
                ? Vector2.right
                : Vector2.left;
        }

        return direction.y > 0
            ? Vector2.up
            : Vector2.down;
    }
}