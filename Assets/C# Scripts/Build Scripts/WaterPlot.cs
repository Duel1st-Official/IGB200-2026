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
    // WATER STATE
    // =========================================================

    [Header("Water State")]
    [SerializeField] private WaterState currentState = WaterState.Clean;

    [Range(0f, 100f)]
    [SerializeField] private float quality = 100f;

    // =========================================================
    // THRESHOLDS
    // =========================================================

    [Header("Quality Thresholds")]

    [Tooltip("Quality below this becomes Dirty.")]
    [Range(0f, 100f)]
    [SerializeField] private float dirtyThreshold = 65f;

    [Tooltip("Quality below this becomes Polluted.")]
    [Range(0f, 100f)]
    [SerializeField] private float pollutedThreshold = 30f;

    // =========================================================
    // DEGRADATION
    // =========================================================

    [Header("Automatic Degradation")]
    [SerializeField] private bool degradeOverTime = false;

    [Tooltip("Water quality lost per second.")]
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

        quality =
            Mathf.Clamp(
                quality,
                0f,
                100f
            );

        UpdateStateFromQuality();
        RefreshSprite();
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        UpdateStateFromQuality();
        RefreshSprite();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!degradeOverTime)
        {
            return;
        }

        if (quality <= 0f)
        {
            return;
        }

        PolluteWater(
            degradationRate *
            Time.deltaTime
        );
    }

    // =========================================================
    // POLLUTE WATER
    // =========================================================

    public void PolluteWater(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        quality -= amount;

        quality =
            Mathf.Clamp(
                quality,
                0f,
                100f
            );

        UpdateStateFromQuality();
        RefreshSprite();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " water polluted by " +
                amount +
                ". Quality: " +
                quality
            );
        }
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

        quality += amount;

        quality =
            Mathf.Clamp(
                quality,
                0f,
                100f
            );

        UpdateStateFromQuality();
        RefreshSprite();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " water cleaned by " +
                amount +
                ". Quality: " +
                quality
            );
        }
    }

    // =========================================================
    // SET WATER QUALITY
    // =========================================================

    public void SetWaterQuality(float newQuality)
    {
        quality =
            Mathf.Clamp(
                newQuality,
                0f,
                100f
            );

        UpdateStateFromQuality();
        RefreshSprite();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " water quality set to " +
                quality
            );
        }
    }

    // =========================================================
    // MAKE CLEAN
    // =========================================================

    public void MakeClean()
    {
        quality = 100f;

        currentState =
            WaterState.Clean;

        RefreshSprite();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " water made CLEAN."
            );
        }
    }

    // =========================================================
    // MAKE DIRTY
    // =========================================================

    public void MakeDirty()
    {
        // Put quality safely inside Dirty range.

        float dirtyQuality =
            Mathf.Max(
                pollutedThreshold + 1f,
                dirtyThreshold - 1f
            );

        quality =
            Mathf.Clamp(
                dirtyQuality,
                0f,
                100f
            );

        currentState =
            WaterState.Dirty;

        RefreshSprite();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " water made DIRTY."
            );
        }
    }

    // =========================================================
    // MAKE POLLUTED
    // =========================================================

    public void MakePolluted()
    {
        quality =
            Mathf.Clamp(
                pollutedThreshold - 1f,
                0f,
                100f
            );

        currentState =
            WaterState.Polluted;

        RefreshSprite();

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " water made POLLUTED."
            );
        }
    }

    // =========================================================
    // UPDATE STATE FROM QUALITY
    // =========================================================

    private void UpdateStateFromQuality()
    {
        if (quality <= pollutedThreshold)
        {
            currentState =
                WaterState.Polluted;
        }
        else if (quality <= dirtyThreshold)
        {
            currentState =
                WaterState.Dirty;
        }
        else
        {
            currentState =
                WaterState.Clean;
        }
    }

    // =========================================================
    // REFRESH SPRITE
    // =========================================================

    private void RefreshSprite()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        switch (currentState)
        {
            case WaterState.Clean:

                if (cleanSprite != null)
                {
                    spriteRenderer.sprite =
                        cleanSprite;
                }

                break;

            case WaterState.Dirty:

                if (dirtySprite != null)
                {
                    spriteRenderer.sprite =
                        dirtySprite;
                }

                break;

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
    // GET WATER QUALITY
    // =========================================================

    public float GetWaterQuality()
    {
        return quality;
    }

    // =========================================================
    // GET STATE
    // =========================================================

    public WaterState GetWaterState()
    {
        return currentState;
    }

    // =========================================================
    // STATE CHECKS
    // =========================================================

    public bool IsClean()
    {
        return
            currentState ==
            WaterState.Clean;
    }

    public bool IsDirty()
    {
        return
            currentState ==
            WaterState.Dirty;
    }

    public bool IsPolluted()
    {
        return
            currentState ==
            WaterState.Polluted;
    }

    // =========================================================
    // BAT WATER VALUE
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

    // =========================================================
    // CONTEXT MENU DEBUG
    // =========================================================

    [ContextMenu("Debug - Make Clean")]
    private void DebugMakeClean()
    {
        MakeClean();
    }

    [ContextMenu("Debug - Make Dirty")]
    private void DebugMakeDirty()
    {
        MakeDirty();
    }

    [ContextMenu("Debug - Make Polluted")]
    private void DebugMakePolluted()
    {
        MakePolluted();
    }

    [ContextMenu("Debug - Pollute 25")]
    private void DebugPollute25()
    {
        PolluteWater(25f);
    }

    [ContextMenu("Debug - Clean 25")]
    private void DebugClean25()
    {
        CleanWater(25f);
    }
}