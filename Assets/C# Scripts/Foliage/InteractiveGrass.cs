using UnityEngine;

public class InteractiveGrass : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    [Header("Random Wind")]
    [SerializeField] private float maxWindAngle = 5f;
    [SerializeField] private float windSmoothness = 2.5f;
    [SerializeField] private float minWindChangeTime = 0.5f;
    [SerializeField] private float maxWindChangeTime = 2f;

    [Header("Player Bend")]
    [SerializeField] private float maxTiltAngle = 18f;
    [SerializeField] private float bendSpeed = 15f;
    [SerializeField] private float returnSpeed = 8f;

    [Header("Player Squash")]
    [SerializeField] private float squashAmount = 0.08f;
    [SerializeField] private float squashSpeed = 15f;

    [Header("Build Shake")]
    [SerializeField] private float buildShakeAngle = 7f;
    [SerializeField] private float buildShakeSpeed = 35f;

    [Header("Break Effect")]
    [SerializeField] private GameObject breakParticlePrefab;
    [SerializeField] private float particleLifetime = 2f;

    private Quaternion originalRotation;
    private Vector3 originalScale;

    private float currentWindAngle;
    private float targetWindAngle;
    private float windTimer;

    private float playerBendAngle;

    private bool playerInside;
    private bool isBuildShaking;
    private bool isBreaking;

    private float buildShakeOffset;

    private void Awake()
    {
        originalRotation =
            transform.localRotation;

        originalScale =
            transform.localScale;

        currentWindAngle =
            Random.Range(
                -maxWindAngle,
                maxWindAngle
            );

        targetWindAngle =
            Random.Range(
                -maxWindAngle,
                maxWindAngle
            );

        // Makes nearby grass shake differently
        // instead of moving perfectly together.
        buildShakeOffset =
            Random.Range(
                0f,
                100f
            );

        ResetWindTimer();
    }

    private void Update()
    {
        if (isBreaking)
        {
            return;
        }

        UpdateRandomWind();
        UpdateRotation();
        UpdateScale();
    }

    // =========================================================
    // RANDOM WIND
    // =========================================================

    private void UpdateRandomWind()
    {
        windTimer -= Time.deltaTime;

        if (windTimer <= 0f)
        {
            targetWindAngle =
                Random.Range(
                    -maxWindAngle,
                    maxWindAngle
                );

            ResetWindTimer();
        }

        currentWindAngle =
            Mathf.Lerp(
                currentWindAngle,
                targetWindAngle,
                1f - Mathf.Exp(
                    -windSmoothness *
                    Time.deltaTime
                )
            );
    }

    private void ResetWindTimer()
    {
        windTimer =
            Random.Range(
                minWindChangeTime,
                maxWindChangeTime
            );
    }

    // =========================================================
    // ROTATION
    // =========================================================

    private void UpdateRotation()
    {
        float targetAngle;

        // =====================================================
        // BUILD SHAKE
        // =====================================================

        if (isBuildShaking)
        {
            float shake =
                Mathf.Sin(
                    (Time.time + buildShakeOffset) *
                    buildShakeSpeed
                ) *
                buildShakeAngle;

            targetAngle = shake;
        }

        // =====================================================
        // PLAYER BEND
        // =====================================================

        else if (playerInside)
        {
            targetAngle =
                playerBendAngle +
                currentWindAngle *
                0.2f;
        }

        // =====================================================
        // NORMAL WIND
        // =====================================================

        else
        {
            targetAngle =
                currentWindAngle;
        }

        float speed;

        if (isBuildShaking)
        {
            // Very responsive while building.
            speed = buildShakeSpeed;
        }
        else if (playerInside)
        {
            speed = bendSpeed;
        }
        else
        {
            speed = returnSpeed;
        }

        float currentAngle =
            Mathf.DeltaAngle(
                0f,
                transform.localEulerAngles.z -
                originalRotation.eulerAngles.z
            );

        float newAngle =
            Mathf.Lerp(
                currentAngle,
                targetAngle,
                1f - Mathf.Exp(
                    -speed *
                    Time.deltaTime
                )
            );

        transform.localRotation =
            originalRotation *
            Quaternion.Euler(
                0f,
                0f,
                newAngle
            );
    }

    // =========================================================
    // SCALE
    // =========================================================

    private void UpdateScale()
    {
        Vector3 targetScale =
            originalScale;

        if (playerInside &&
            !isBuildShaking)
        {
            targetScale.x =
                originalScale.x *
                (1f + squashAmount);

            targetScale.y =
                originalScale.y *
                (1f - squashAmount);
        }

        transform.localScale =
            Vector3.Lerp(
                transform.localScale,
                targetScale,
                1f - Mathf.Exp(
                    -squashSpeed *
                    Time.deltaTime
                )
            );
    }

    // =========================================================
    // START BUILD SHAKE
    // =========================================================

    public void StartBuildShake()
    {
        if (isBreaking)
        {
            return;
        }

        isBuildShaking = true;
    }

    // =========================================================
    // STOP BUILD SHAKE
    // =========================================================

    public void StopBuildShake()
    {
        isBuildShaking = false;
    }

    // =========================================================
    // PLAYER ENTER
    // =========================================================

    private void OnTriggerEnter2D(
        Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInside = true;

        BendAwayFromPlayer(
            other.transform
        );
    }

    // =========================================================
    // PLAYER STAY
    // =========================================================

    private void OnTriggerStay2D(
        Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInside = true;

        BendAwayFromPlayer(
            other.transform
        );
    }

    // =========================================================
    // PLAYER EXIT
    // =========================================================

    private void OnTriggerExit2D(
        Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInside = false;
        playerBendAngle = 0f;

        targetWindAngle =
            Random.Range(
                -maxWindAngle,
                maxWindAngle
            );

        ResetWindTimer();
    }

    // =========================================================
    // BEND AWAY FROM PLAYER
    // =========================================================

    private void BendAwayFromPlayer(
        Transform player)
    {
        float difference =
            player.position.x -
            transform.position.x;

        if (difference < 0f)
        {
            playerBendAngle =
                -maxTiltAngle;
        }
        else
        {
            playerBendAngle =
                maxTiltAngle;
        }
    }

    // =========================================================
    // BREAK GRASS
    // =========================================================

    public void BreakGrass()
    {
        if (isBreaking)
        {
            return;
        }

        isBreaking = true;
        isBuildShaking = false;

        // =====================================================
        // PARTICLE
        // =====================================================

        if (breakParticlePrefab != null)
        {
            GameObject particles =
                Instantiate(
                    breakParticlePrefab,
                    transform.position,

                    // Particle does NOT inherit
                    // grass rotation.
                    Quaternion.identity
                );

            Destroy(
                particles,
                particleLifetime
            );
        }

        // =====================================================
        // DESTROY GRASS
        // =====================================================

        Destroy(gameObject);
    }

    // =========================================================
    // RESET
    // =========================================================

    private void OnDisable()
    {
        if (isBreaking)
        {
            return;
        }

        transform.localRotation =
            originalRotation;

        transform.localScale =
            originalScale;
    }
}