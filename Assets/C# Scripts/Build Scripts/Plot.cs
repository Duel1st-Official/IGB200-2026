using UnityEngine;

public class Plot : MonoBehaviour
{
    [Header("Destroyed Warning")]
    [SerializeField] private bool showDestroyedWarning = true;
    [Tooltip("Drag the 16x16 warning sprite here.")]
    [SerializeField] private Sprite destroyedWarningSprite;
    [SerializeField] private Color destroyedWarningColor = Color.white;
    [Tooltip("World-space offset above the destroyed plot.")]
    [SerializeField] private Vector2 destroyedWarningOffset = new Vector2(0f, 0.35f);
    [Tooltip("Warning icon height in world units. Aspect ratio is preserved.")]
    [Min(0.01f)][SerializeField] private float destroyedWarningSize = 0.5f;
    [Min(0f)][SerializeField] private float destroyedWarningBounceHeight = 0.04f;
    [Min(0f)][SerializeField] private float destroyedWarningBounceSpeed = 1.5f;
    [SerializeField] private int destroyedWarningSortingOffset = 20;
    private SpriteRenderer destroyedWarningRenderer;

    [Header("Destroyed plot / debris")]
    [SerializeField] private bool destroyed;
    [SerializeField] private Sprite debrisSprite;
    [SerializeField] private SpriteRenderer debrisRenderer;
    [SerializeField] private string destructionCause;
    [SerializeField] private int criticalSoilDays;
    private int soilCheckedDay = -1;
    public bool IsDestroyed() { return destroyed; }
    public string GetDestructionCause() { return destructionCause; }

    private void Start()
    {
        soilCheckedDay = endDaySystem != null ? endDaySystem.GetCurrentDay() : -1;
        if (destroyed) { DiscardContents(false); ApplyDebrisVisual(); occupied = true; }
    }
    private void Update()
    {
        CheckSoilDamage();
    }

    private void LateUpdate()
    {
        RefreshDestroyedWarning();
    }
    public void CheckSoilDamage()
    {
        if (destroyed || endDaySystem == null || endDaySystem.HasGameEnded()) return;
        int day = endDaySystem.GetCurrentDay();
        if (day < 1) return;
        if (soilCheckedDay < 1 || day < soilCheckedDay) { soilCheckedDay = day; criticalSoilDays = 0; return; }
        if (day == soilCheckedDay) return;
        soilCheckedDay = day;
        if (rangerStation == null) rangerStation = FindFirstObjectByType<RangerStation>();
        if (rangerStation == null) return;
        PlotDisasterSystem settings = PlotDisasterSystem.GetOrCreate();
        criticalSoilDays = rangerStation.GetSoilHealth() <= settings.criticalSoilHealth ? criticalSoilDays + 1 : 0;
        if (criticalSoilDays >= Mathf.Max(1, settings.consecutiveCriticalDays)) DestroyPlot("Critical soil health");
    }
    public bool DestroyPlot(string cause)
    {
        if (destroyed) return false;
        AutoAssignReferences();
        destroyed = true; destructionCause = cause;
        DiscardContents(false);
        occupied = true; planted = false;
        ApplyDebrisVisual();
        return true;
    }
    public void ClearContentsForRemoval()
    {
        DiscardContents(true);
    }
    private void DiscardContents(bool removing)
    {
        AutoAssignReferences();
        if (cropPlot != null) cropPlot.ClearCrop();
        foreach (Trap trap in FindObjectsByType<Trap>(FindObjectsSortMode.None))
            if (trap.GetOwningPlot() == this)
            {
                if (removing) trap.RemoveWithPlot(); else trap.DestroyWithPlot();
            }
    }
    private void ApplyDebrisVisual()
    {
        if (debrisRenderer == null) debrisRenderer = GetComponent<SpriteRenderer>();
        if (debrisRenderer == null) debrisRenderer = GetComponentInChildren<SpriteRenderer>();
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>())
            if (renderer != debrisRenderer) renderer.enabled = false;
        if (debrisRenderer != null)
        {
            if (debrisSprite != null) debrisRenderer.sprite = debrisSprite;
            else if (cropPlot != null && cropPlot.GetEmptyPlotSprite() != null)
                debrisRenderer.sprite = cropPlot.GetEmptyPlotSprite();
            debrisRenderer.color = debrisSprite != null ? Color.white : new Color(0.25f, 0.19f, 0.17f);
            debrisRenderer.enabled = true;
        }
    }
    private void RefreshDestroyedWarning()
    {
        bool visible =
            showDestroyedWarning &&
            destroyed &&
            destroyedWarningSprite != null;

        if (!visible)
        {
            if (destroyedWarningRenderer != null)
            {
                destroyedWarningRenderer.gameObject.SetActive(false);
            }

            return;
        }

        if (destroyedWarningRenderer == null)
        {
            GameObject marker =
                new GameObject("Destroyed Warning Icon");

            marker.layer = gameObject.layer;
            marker.transform.SetParent(transform, false);

            destroyedWarningRenderer =
                marker.AddComponent<SpriteRenderer>();
        }

        destroyedWarningRenderer.gameObject.SetActive(true);
        destroyedWarningRenderer.sprite = destroyedWarningSprite;
        destroyedWarningRenderer.color = destroyedWarningColor;

        SpriteRenderer baseRenderer = debrisRenderer;

        Vector3 basePosition =
            baseRenderer != null
                ? new Vector3(
                    baseRenderer.bounds.center.x,
                    baseRenderer.bounds.max.y,
                    transform.position.z)
                : transform.position;

        float bounce =
            Mathf.Sin(
                Time.time *
                Mathf.Max(0f, destroyedWarningBounceSpeed) *
                Mathf.PI * 2f) *
            Mathf.Max(0f, destroyedWarningBounceHeight);

        Transform markerTransform =
            destroyedWarningRenderer.transform;

        markerTransform.rotation = Quaternion.identity;

        Vector3 objectScale =
            transform.lossyScale;

        float size =
            Mathf.Max(0.01f, destroyedWarningSize) /
            Mathf.Max(
                0.0001f,
                destroyedWarningSprite.bounds.size.y);

        markerTransform.localScale =
            new Vector3(
                size /
                Mathf.Max(
                    0.0001f,
                    Mathf.Abs(objectScale.x)),
                size /
                Mathf.Max(
                    0.0001f,
                    Mathf.Abs(objectScale.y)),
                1f);

        markerTransform.position =
            basePosition +
            new Vector3(
                destroyedWarningOffset.x,
                destroyedWarningOffset.y + bounce,
                0f);

        markerTransform.position -=
            markerTransform.TransformVector(
                destroyedWarningSprite.bounds.center);

        if (baseRenderer != null)
        {
            destroyedWarningRenderer.sortingLayerID =
                baseRenderer.sortingLayerID;

            destroyedWarningRenderer.sortingOrder =
                baseRenderer.sortingOrder +
                destroyedWarningSortingOffset;
        }
    }

    [ContextMenu("Debug - Destroy Plot")]
    private void DebugDestroyPlot() { DestroyPlot("Debug"); }


    private void OnDisable()
    {
        if (destroyedWarningRenderer != null)
        {
            destroyedWarningRenderer.gameObject.SetActive(false);
        }
    }

    // =========================================================
    // PLOT STATE
    // =========================================================

    [Header("Plot State")]

    public bool occupied;

    public bool planted;

    // =========================================================
    // CROP
    // =========================================================

    [Header("Crop")]

    [Tooltip(
        "Crop system attached to this plot. " +
        "If empty, one will automatically be found."
    )]
    [SerializeField]
    private CropPlot cropPlot;

    // =========================================================
    // RANGER STATION
    // =========================================================

    [Header("Environment")]

    [Tooltip(
        "Optional Ranger Station reference. " +
        "If empty, one will automatically be found."
    )]
    [SerializeField]
    private RangerStation rangerStation;

    // =========================================================
    // END DAY SYSTEM
    // =========================================================

    [Header("Action Phase")]

    [Tooltip(
        "Controls whether the player is currently allowed " +
        "to plant crops. If empty, one is automatically found."
    )]
    [SerializeField]
    private EndDaySystem endDaySystem;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField]
    private bool showDebugLogs = false;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        AutoAssignReferences();
    }

    // =========================================================
    // AUTO ASSIGN REFERENCES
    // =========================================================

    private void AutoAssignReferences()
    {
        // -----------------------------------------------------
        // CROP
        // -----------------------------------------------------

        if (cropPlot == null)
        {
            cropPlot =
                GetComponent<CropPlot>();

            if (cropPlot == null)
            {
                cropPlot =
                    GetComponentInChildren<CropPlot>();
            }
        }

        // -----------------------------------------------------
        // RANGER STATION
        // -----------------------------------------------------

        if (rangerStation == null)
        {
            rangerStation =
                FindFirstObjectByType<RangerStation>();
        }

        // -----------------------------------------------------
        // END DAY SYSTEM
        // -----------------------------------------------------

        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }
    }

    // =========================================================
    // ACTION PHASE
    // =========================================================

    public bool CanPerformPlayerAction()
    {
        if (destroyed) return false;
        if (endDaySystem == null)
        {
            endDaySystem =
                FindFirstObjectByType<EndDaySystem>();
        }

        // If there is no EndDaySystem in the scene,
        // preserve the old behaviour rather than breaking
        // planting completely.
        if (endDaySystem == null)
        {
            return true;
        }

        return endDaySystem.IsActionPhaseActive();
    }

    // =========================================================
    // PLANT
    // =========================================================

    public void Plant()
    {
        // =====================================================
        // ACTION PHASE LOCK
        // =====================================================

        if (!CanPerformPlayerAction())
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    gameObject.name +
                    " cannot be planted because the action phase has ended."
                );
            }

            return;
        }

        // -----------------------------------------------------
        // ALREADY PLANTED
        // -----------------------------------------------------

        if (planted)
        {
            return;
        }

        // -----------------------------------------------------
        // SET STATE
        // -----------------------------------------------------

        planted =
            true;

        // -----------------------------------------------------
        // START CROP
        // -----------------------------------------------------

        if (cropPlot == null)
        {
            cropPlot =
                GetComponent<CropPlot>();

            if (cropPlot == null)
            {
                cropPlot =
                    GetComponentInChildren<CropPlot>();
            }
        }

        if (cropPlot != null)
        {
            cropPlot.PlantCrop();
        }

        // =====================================================
        // DAMAGE SOIL
        // =====================================================

        if (rangerStation == null)
        {
            rangerStation =
                FindFirstObjectByType<RangerStation>();
        }

        if (rangerStation != null)
        {
            rangerStation.PlotPlanted();
        }

        // =====================================================
        // DEBUG
        // =====================================================

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " planted."
            );
        }
    }

    // =========================================================
    // CLEAR PLOT
    // =========================================================

    public void ClearPlant()
    {
        // IMPORTANT:
        // No action-phase check here.
        //
        // This method is also used internally when crops are
        // harvested/removed, so automatic/system cleanup must
        // remain possible after the action phase ends.

        planted =
            false;

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " plant cleared."
            );
        }
    }

    // =========================================================
    // CLEAR PLOT AND CROP
    // =========================================================

    public void ClearPlantAndCrop()
    {
        // IMPORTANT:
        // This remains available to internal systems/debugging.
        // Player-facing removal is controlled by the
        // SelectionWheel / placement system.

        planted =
            false;

        if (cropPlot == null)
        {
            cropPlot =
                GetComponent<CropPlot>();

            if (cropPlot == null)
            {
                cropPlot =
                    GetComponentInChildren<CropPlot>();
            }
        }

        if (cropPlot != null)
        {
            cropPlot.ClearCrop();
        }

        if (showDebugLogs)
        {
            Debug.Log(
                gameObject.name +
                " plant and crop cleared."
            );
        }
    }

    // =========================================================
    // OCCUPIED
    // =========================================================

    public void SetOccupied(
        bool value)
    {
        occupied =
            destroyed || value;
    }

    // =========================================================
    // CURRENT DISPLAYED SPRITE
    // =========================================================

    public Sprite GetCurrentDisplayedSprite()
    {
        // Destroyed plots must mirror the debris sprite that is
        // actually visible in the world.
        if (destroyed)
        {
            if (debrisRenderer != null &&
                debrisRenderer.sprite != null)
            {
                return debrisRenderer.sprite;
            }

            if (debrisSprite != null)
            {
                return debrisSprite;
            }
        }

        // Otherwise mirror the CropPlot's current world sprite.
        if (cropPlot == null)
        {
            AutoAssignReferences();
        }

        if (cropPlot != null)
        {
            return cropPlot.GetCurrentDisplayedSprite();
        }

        // Last fallback: use the visible renderer on the plot.
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();

        if (renderer == null)
        {
            renderer = GetComponentInChildren<SpriteRenderer>();
        }

        return renderer != null ? renderer.sprite : null;
    }

    // =========================================================
    // CHECKS
    // =========================================================

    public bool IsPlanted()
    {
        return planted;
    }

    public bool IsOccupied()
    {
        return destroyed || occupied;
    }

    public CropPlot GetCropPlot()
    {
        return cropPlot;
    }

    // =========================================================
    // DEBUG
    // =========================================================

    [ContextMenu("Debug - Plant")]
    private void DebugPlant()
    {
        Plant();
    }

    [ContextMenu("Debug - Clear Plant")]
    private void DebugClearPlant()
    {
        ClearPlantAndCrop();
    }
}
