using UnityEngine;

public class FlyingBird : MonoBehaviour
{
    // =========================================================
    // MOVEMENT
    // =========================================================

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    [Tooltip("Small sideways wandering while the bird flies.")]
    [SerializeField] private float driftAmount = 0.15f;

    [SerializeField] private float driftSpeed = 1.5f;

    // =========================================================
    // ROTATION
    // =========================================================

    [Header("Flight Rotation")]

    [Tooltip(
        "The sprite naturally faces North-East, so this offset aligns " +
        "the artwork with the movement direction."
    )]
    [SerializeField] private float spriteDirectionOffset = -45f;

    [Tooltip(
        "How quickly the bird rotates toward its flight direction."
    )]
    [SerializeField] private float rotationSpeed = 10f;

    [Tooltip(
        "If ON, rotation is smoothed. If OFF, it instantly faces the flight direction."
    )]
    [SerializeField] private bool smoothRotation = true;

    // =========================================================
    // LIFETIME
    // =========================================================

    [Header("Lifetime")]
    [SerializeField] private float maximumLifetime = 30f;

    // =========================================================
    // PRIVATE
    // =========================================================

    private Vector2 direction = new Vector2(1f, 1f).normalized;

    private float driftOffset;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        driftOffset =
            Random.Range(
                0f,
                100f
            );
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        FaceFlightDirectionImmediate();

        Destroy(
            gameObject,
            maximumLifetime
        );
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        MoveBird();
        RotateTowardsFlightDirection();
    }

    // =========================================================
    // SETUP
    // =========================================================

    public void Setup(
        Vector2 flightDirection,
        float speed)
    {
        if (flightDirection.sqrMagnitude >
            0.001f)
        {
            direction =
                flightDirection.normalized;
        }

        moveSpeed =
            speed;

        FaceFlightDirectionImmediate();
    }

    // =========================================================
    // MOVEMENT
    // =========================================================

    private void MoveBird()
    {
        Vector2 perpendicular =
            new Vector2(
                -direction.y,
                direction.x
            );

        float drift =
            Mathf.Sin(
                (
                    Time.time +
                    driftOffset
                ) *
                driftSpeed
            ) *
            driftAmount;

        Vector2 movement =
            direction *
            moveSpeed;

        movement +=
            perpendicular *
            drift;

        transform.position +=
            (Vector3)(
                movement *
                Time.deltaTime
            );
    }

    // =========================================================
    // ROTATE TOWARD FLIGHT
    // =========================================================

    private void RotateTowardsFlightDirection()
    {
        float targetAngle =
            GetFlightAngle();

        Quaternion targetRotation =
            Quaternion.Euler(
                0f,
                0f,
                targetAngle
            );

        if (!smoothRotation)
        {
            transform.rotation =
                targetRotation;

            return;
        }

        float amount =
            1f -
            Mathf.Exp(
                -rotationSpeed *
                Time.deltaTime
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                amount
            );
    }

    // =========================================================
    // IMMEDIATE ROTATION
    // =========================================================

    private void FaceFlightDirectionImmediate()
    {
        float targetAngle =
            GetFlightAngle();

        transform.rotation =
            Quaternion.Euler(
                0f,
                0f,
                targetAngle
            );
    }

    // =========================================================
    // GET FLIGHT ANGLE
    // =========================================================

    private float GetFlightAngle()
    {
        float movementAngle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) *
            Mathf.Rad2Deg;

        return
            movementAngle +
            spriteDirectionOffset;
    }
}