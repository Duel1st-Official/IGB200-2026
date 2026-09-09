using UnityEngine;

public class InteractiveGrass : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    // =========================================================
    // NORMAL WIND
    // =========================================================

    [Header("Random Wind")]
    [SerializeField] private float maxWindAngle = 5f;
    [SerializeField] private float windSmoothness = 2.5f;
    [SerializeField] private float minWindChangeTime = 0.5f;
    [SerializeField] private float maxWindChangeTime = 2f;

    // =========================================================
    // RAIN WIND
    // =========================================================

    [Header("Rain Wind")]

    [Tooltip("How much stronger the grass bends during Rain.")]
    [SerializeField] private float rainSwayMultiplier = 3f;

    [Tooltip("How much faster the grass reacts during Rain.")]
    [SerializeField] private float rainSpeedMultiplier = 2.5f;

    [Tooltip("How much faster wind direction changes during Rain.")]
    [SerializeField] private float rainWindChangeMultiplier = 2.5f;

    [Range(0f, 1f)]
    [SerializeField] private float rainGustAmount = 0.35f;

    [SerializeField] private float rainGustSpeed = 1.5f;

    // =========================================================
    // RAIN & THUNDER WIND
    // =========================================================

    [Header("Rain & Thunder Wind")]

    [Tooltip(
        "How much stronger the grass bends during Rain & Thunder."
    )]
    [SerializeField] private float stormSwayMultiplier = 7f;

    [Tooltip(
        "How much faster the grass reacts during Rain & Thunder."
    )]
    [SerializeField] private float stormSpeedMultiplier = 5f;

    [Tooltip(
        "How much faster wind direction changes during Rain & Thunder."
    )]
    [SerializeField] private float stormWindChangeMultiplier = 6f;

    [Range(0f, 1f)]
    [SerializeField] private float stormGustAmount = 0.7f;

    [SerializeField] private float stormGustSpeed = 3f;

    // =========================================================
    // WEATHER TRANSITION
    // =========================================================

    [Header("Weather Transition")]

    [Tooltip(
        "How quickly grass transitions between weather wind strengths."
    )]
    [SerializeField] private float weatherTransitionSpeed = 4f;

    // =========================================================
    // PLAYER BEND
    // =========================================================

    [Header("Player Bend")]
    [SerializeField] private float maxTiltAngle = 18f;
    [SerializeField] private float bendSpeed = 15f;
    [SerializeField] private float returnSpeed = 8f;

    // =========================================================
    // PLAYER SQUASH
    // =========================================================

    [Header("Player Squash")]
    [SerializeField] private float squashAmount = 0.08f;
    [SerializeField] private float squashSpeed = 15f;

    // =========================================================
    // BUILD SHAKE
    // =========================================================

    [Header("Build Shake")]
    [SerializeField] private float buildShakeAngle = 7f;
    [SerializeField] private float buildShakeSpeed = 35f;

    // =========================================================
    // BREAK EFFECT
    // =========================================================

    [Header("Break Effect")]
    [SerializeField] private GameObject breakParticlePrefab;
    [SerializeField] private float particleLifetime = 2f;

    // =========================================================
    // PRIVATE
    // =========================================================

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
    private float weatherNoiseOffset;

    private float currentSwayMultiplier = 1f;
    private float currentSpeedMultiplier = 1f;
    private float currentWindChangeMultiplier = 1f;
    private float currentGustAmount = 0f;
    private float currentGustSpeed = 1f;

    // =========================================================
    // AWAKE
    // =========================================================

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

        buildShakeOffset =
            Random.Range(
                0f,
                100f
            );

        weatherNoiseOffset =
            Random.Range(
                0f,
                100f
            );

        ResetWindTimer();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (isBreaking)
        {
            return;
        }

        UpdateWeatherWind();
        UpdateRandomWind();
        UpdateRotation();
        UpdateScale();
    }

    // =========================================================
    // WEATHER WIND
    // =========================================================

    private void UpdateWeatherWind()
    {
        float targetSway = 1f;
        float targetSpeed = 1f;
        float targetWindChange = 1f;
        float targetGustAmount = 0f;
        float targetGustSpeed = 1f;

        if (WeatherManager.Instance != null)
        {
            if (WeatherManager.Instance.IsRainAndThunder())
            {
                targetSway =
                    stormSwayMultiplier;

                targetSpeed =
                    stormSpeedMultiplier;

                targetWindChange =
                    stormWindChangeMultiplier;

                targetGustAmount =
                    stormGustAmount;

                targetGustSpeed =
                    stormGustSpeed;
            }
            else if (WeatherManager.Instance.IsRaining())
            {
                targetSway =
                    rainSwayMultiplier;

                targetSpeed =
                    rainSpeedMultiplier;

                targetWindChange =
                    rainWindChangeMultiplier;

                targetGustAmount =
                    rainGustAmount;

                targetGustSpeed =
                    rainGustSpeed;
            }
        }

        float amount =
            1f -
            Mathf.Exp(
                -weatherTransitionSpeed *
                Time.deltaTime
            );

        currentSwayMultiplier =
            Mathf.Lerp(
                currentSwayMultiplier,
                targetSway,
                amount
            );

        currentSpeedMultiplier =
            Mathf.Lerp(
                currentSpeedMultiplier,
                targetSpeed,
                amount
            );

        currentWindChangeMultiplier =
            Mathf.Lerp(
                currentWindChangeMultiplier,
                targetWindChange,
                amount
            );

        currentGustAmount =
            Mathf.Lerp(
                currentGustAmount,
                targetGustAmount,
                amount
            );

        currentGustSpeed =
            Mathf.Lerp(
                currentGustSpeed,
                targetGustSpeed,
                amount
            );
    }

    // =========================================================
    // RANDOM WIND
    // =========================================================

    private void UpdateRandomWind()
    {
        windTimer -=
            Time.deltaTime *
            currentWindChangeMultiplier;

        if (windTimer <= 0f)
        {
            float angleLimit =
                maxWindAngle *
                currentSwayMultiplier;

            targetWindAngle =
                Random.Range(
                    -angleLimit,
                    angleLimit
                );

            ResetWindTimer();
        }

        float smoothness =
            windSmoothness *
            currentSpeedMultiplier;

        currentWindAngle =
            Mathf.Lerp(
                currentWindAngle,
                targetWindAngle,
                1f -
                Mathf.Exp(
                    -smoothness *
                    Time.deltaTime
                )
            );
    }

    // =========================================================
    // WIND TIMER
    // =========================================================

    private void ResetWindTimer()
    {
        windTimer =
            Random.Range(
                minWindChangeTime,
                maxWindChangeTime
            );
    }

    // =========================================================
    // GUST MULTIPLIER
    // =========================================================

    private float GetGustMultiplier()
    {
        if (currentGustAmount <= 0.001f)
        {
            return 1f;
        }

        float noise =
            Mathf.PerlinNoise(
                weatherNoiseOffset,
                Time.time *
                currentGustSpeed
            );

        float minimum =
            1f -
            currentGustAmount;

        float maximum =
            1f +
            currentGustAmount;

        return
            Mathf.Lerp(
                minimum,
                maximum,
                noise
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

            targetAngle =
                shake;
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
        // WEATHER WIND
        // =====================================================

        else
        {
            targetAngle =
                currentWindAngle *
                GetGustMultiplier();
        }

        // =====================================================
        // ROTATION SPEED
        // =====================================================

        float speed;

        if (isBuildShaking)
        {
            speed =
                buildShakeSpeed;
        }
        else if (playerInside)
        {
            speed =
                bendSpeed;
        }
        else
        {
            speed =
                returnSpeed *
                currentSpeedMultiplier;
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
                1f -
                Mathf.Exp(
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
                1f -
                Mathf.Exp(
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

        isBuildShaking =
            true;
    }

    // =========================================================
    // STOP BUILD SHAKE
    // =========================================================

    public void StopBuildShake()
    {
        isBuildShaking =
            false;
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

        playerInside =
            true;

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

        playerInside =
            true;

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

        playerInside =
            false;

        playerBendAngle =
            0f;

        float angleLimit =
            maxWindAngle *
            currentSwayMultiplier;

        targetWindAngle =
            Random.Range(
                -angleLimit,
                angleLimit
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

        isBreaking =
            true;

        isBuildShaking =
            false;

        if (breakParticlePrefab != null)
        {
            GameObject particles =
                Instantiate(
                    breakParticlePrefab,
                    transform.position,
                    Quaternion.identity
                );

            Destroy(
                particles,
                particleLifetime
            );
        }

        Destroy(
            gameObject
        );
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