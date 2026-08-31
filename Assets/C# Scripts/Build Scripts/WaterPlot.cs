using UnityEngine;

public class WaterPlot : MonoBehaviour
{
    // =========================================================
    // WATER STATE
    // =========================================================

    public enum WaterState
    {
        Clean,
        Dirty,
        Polluted
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    // =========================================================
    // WATER SPRITES
    // =========================================================

    [Header("Water Sprites")]
    [SerializeField] private Sprite cleanSprite;
    [SerializeField] private Sprite dirtySprite;
    [SerializeField] private Sprite pollutedSprite;

    // =========================================================
    // CURRENT STATE
    // =========================================================

    [Header("Current Water State")]
    [SerializeField]
    private WaterState currentState =
        WaterState.Clean;

    // =========================================================
    // WATER QUALITY
    // =========================================================

    [Header("Water Quality")]
    [Range(0f, 100f)]
    [SerializeField] private float waterQuality = 100f;

    [Tooltip("Water at or below this value becomes Dirty.")]
    [SerializeField] private float dirtyThreshold = 65f;

    [Tooltip("Water at or below this value becomes Polluted.")]
    [SerializeField] private float pollutedThreshold = 30f;

    // =========================================================
    // OPTIONAL NATURAL DEGRADATION
    // =========================================================

    [Header("Natural Degradation")]
    [SerializeField] private bool degradeOverTime = false;

    [Tooltip("How much water quality is lost per second.")]
    [SerializeField] private float degradationRate = 0.25f;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // Automatically find the SpriteRenderer
        // if it was not assigned manually.

        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                spriteRenderer =
                    GetComponentInChildren<SpriteRenderer>();
            }
        }
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        waterQuality =
            Mathf.Clamp(
                waterQuality,
                0f,
                100f
            );

        UpdateWaterState();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // =====================================================
        // NATURAL WATER DEGRADATION
        // =====================================================

        if (degradeOverTime &&
            waterQuality > 0f)
        {
            waterQuality -=
                degradationRate *
                Time.deltaTime;

            waterQuality =
                Mathf.Clamp(
                    waterQuality,
                    0f,
                    100f
                );

            UpdateWaterState();
        }
    }

    // =========================================================
    // UPDATE WATER STATE
    // =========================================================

    private void UpdateWaterState()
    {
        WaterState newState;

        // =====================================================
        // POLLUTED
        // =====================================================

        if (waterQuality <=
            pollutedThreshold)
        {
            newState =
                WaterState.Polluted;
        }

        // =====================================================
        // DIRTY
        // =====================================================

        else if (waterQuality <=
                 dirtyThreshold)
        {
            newState =
                WaterState.Dirty;
        }

        // =====================================================
        // CLEAN
        // =====================================================

        else
        {
            newState =
                WaterState.Clean;
        }

        // If nothing changed,
        // don't unnecessarily update the sprite.

        if (newState ==
            currentState)
        {
            UpdateSprite();
            return;
        }

        currentState =
            newState;

        UpdateSprite();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " Water State = " +
                currentState +
                " | Quality = " +
                waterQuality
            );
        }
    }

    // =========================================================
    // UPDATE SPRITE
    // =========================================================

    private void UpdateSprite()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        switch (currentState)
        {
            // =================================================
            // CLEAN
            // =================================================

            case WaterState.Clean:

                if (cleanSprite != null)
                {
                    spriteRenderer.sprite =
                        cleanSprite;
                }

                break;

            // =================================================
            // DIRTY
            // =================================================

            case WaterState.Dirty:

                if (dirtySprite != null)
                {
                    spriteRenderer.sprite =
                        dirtySprite;
                }

                break;

            // =================================================
            // POLLUTED
            // =================================================

            case WaterState.Polluted:

                if (pollutedSprite != null)
                {
                    spriteRenderer.sprite =
                        pollutedSprite;
                }

                break;
        }
    }

    // =========================================================
    // DAMAGE WATER
    // =========================================================

    public void PolluteWater(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        waterQuality -=
            amount;

        waterQuality =
            Mathf.Clamp(
                waterQuality,
                0f,
                100f
            );

        UpdateWaterState();
    }

    // =========================================================
    // CLEAN WATER
    // =========================================================

    public void CleanWater(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        waterQuality +=
            amount;

        waterQuality =
            Mathf.Clamp(
                waterQuality,
                0f,
                100f
            );

        UpdateWaterState();
    }

    // =========================================================
    // SET QUALITY DIRECTLY
    // =========================================================

    public void SetWaterQuality(float amount)
    {
        waterQuality =
            Mathf.Clamp(
                amount,
                0f,
                100f
            );

        UpdateWaterState();
    }

    // =========================================================
    // MAKE CLEAN
    // =========================================================

    public void MakeClean()
    {
        waterQuality =
            100f;

        UpdateWaterState();
    }

    // =========================================================
    // MAKE DIRTY
    // =========================================================

    public void MakeDirty()
    {
        // Put quality safely
        // between the two thresholds.

        waterQuality =
            Mathf.Clamp(
                dirtyThreshold - 1f,
                pollutedThreshold + 1f,
                100f
            );

        UpdateWaterState();
    }

    // =========================================================
    // MAKE POLLUTED
    // =========================================================

    public void MakePolluted()
    {
        waterQuality =
            Mathf.Max(
                0f,
                pollutedThreshold - 1f
            );

        UpdateWaterState();
    }

    // =========================================================
    // GETTERS
    // =========================================================

    public WaterState GetWaterState()
    {
        return currentState;
    }

    public float GetWaterQuality()
    {
        return waterQuality;
    }

    public bool IsClean()
    {
        return currentState ==
               WaterState.Clean;
    }

    public bool IsDirty()
    {
        return currentState ==
               WaterState.Dirty;
    }

    public bool IsPolluted()
    {
        return currentState ==
               WaterState.Polluted;
    }

    // =========================================================
    // BAT HABITAT VALUE
    // =========================================================

    public float GetBatWaterValue()
    {
        switch (currentState)
        {
            case WaterState.Clean:
                return 1f;

            case WaterState.Dirty:
                return 0.5f;

            case WaterState.Polluted:
                return 0f;
        }

        return 0f;
    }
}