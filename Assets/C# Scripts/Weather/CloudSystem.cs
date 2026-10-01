using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight 2D cloud atmosphere system for a top-down game.
///
/// HOW TO USE:
/// 1. Create an empty GameObject named "Cloud System".
/// 2. Add this component.
/// 3. Assign one or more transparent cloud sprites to Cloud Sprites.
/// 4. Set the Sorting Layer to a layer that renders above the world but below UI.
/// 5. Leave Weather Manager empty if you want it auto-found.
///
/// Weather is read from the existing WeatherManager:
/// Sunny, Rain, RainAndThunder.
/// Clouds are pooled/recycled rather than constantly instantiated/destroyed.
/// </summary>
public class CloudSystem : MonoBehaviour
{
    [System.Serializable]
    public class CloudWeatherSettings
    {
        [Header("Amount")]
        [Min(0)]
        public int cloudCount = 3;

        [Header("Appearance")]
        public Color cloudColour = Color.white;

        [Range(0f, 1f)]
        public float opacity = 0.10f;

        [Min(0.01f)]
        public float minimumScale = 0.8f;

        [Min(0.01f)]
        public float maximumScale = 1.2f;

        [Header("Movement")]
        [Min(0f)]
        public float minimumSpeed = 0.15f;

        [Min(0f)]
        public float maximumSpeed = 0.30f;
    }

    private class CloudInstance
    {
        public GameObject gameObject;
        public Transform transform;
        public SpriteRenderer renderer;
        public SpriteRenderer shadowRenderer;

        public float speed;
        public float targetScale;
        public float currentAlpha;
        public float targetAlpha;

        public bool wanted;
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    [Tooltip("Automatically finds the WeatherManager if empty.")]
    [SerializeField] private WeatherManager weatherManager;

    [Tooltip(
        "Optional camera reference. Used to keep the cloud field centred " +
        "around the player's view. Automatically uses Camera.main if empty."
    )]
    [SerializeField] private Camera targetCamera;

    // =========================================================
    // CLOUD SPRITES
    // =========================================================

    [Header("Cloud Sprites")]

    [Tooltip(
        "Drag your transparent pixel-art cloud sprites here. " +
        "A random sprite is chosen whenever a cloud is recycled."
    )]
    [SerializeField] private Sprite[] cloudSprites;

    // =========================================================
    // CLOUD AREA
    // =========================================================

    [Header("Cloud Area")]

    [Tooltip("Width of the cloud field around the camera.")]
    [Min(1f)]
    [SerializeField] private float areaWidth = 30f;

    [Tooltip("Height of the cloud field around the camera.")]
    [Min(1f)]
    [SerializeField] private float areaHeight = 20f;

    [Tooltip(
        "Extra distance beyond the visible cloud field used before " +
        "a cloud is recycled to the opposite side."
    )]
    [Min(0f)]
    [SerializeField] private float recyclePadding = 3f;

    [Tooltip(
        "If enabled, the cloud field follows the camera so clouds remain " +
        "around the player while exploring."
    )]
    [SerializeField] private bool followCamera = true;

    // =========================================================
    // DIRECTION
    // =========================================================

    [Header("Cloud Movement")]

    [Tooltip("World-space direction clouds travel.")]
    [SerializeField]
    private Vector2 movementDirection =
        new Vector2(1f, -0.08f);

    [Tooltip(
        "Adds a little speed variation whenever a cloud is recycled."
    )]
    [Range(0f, 1f)]
    [SerializeField] private float speedVariation = 0.15f;

    // =========================================================
    // SUNNY
    // =========================================================

    [Header("Sunny Weather")]

    [SerializeField]
    private CloudWeatherSettings sunnySettings =
        new CloudWeatherSettings
        {
            cloudCount = 3,
            cloudColour = new Color(1f, 1f, 1f, 1f),
            opacity = 0.10f,
            minimumScale = 0.8f,
            maximumScale = 1.2f,
            minimumSpeed = 0.12f,
            maximumSpeed = 0.24f
        };

    // =========================================================
    // RAIN
    // =========================================================

    [Header("Rain Weather")]

    [SerializeField]
    private CloudWeatherSettings rainSettings =
        new CloudWeatherSettings
        {
            cloudCount = 7,
            cloudColour = new Color(0.78f, 0.80f, 0.84f, 1f),
            opacity = 0.16f,
            minimumScale = 1f,
            maximumScale = 1.5f,
            minimumSpeed = 0.20f,
            maximumSpeed = 0.36f
        };

    // =========================================================
    // STORM
    // =========================================================

    [Header("Lightning Storm Weather")]

    [SerializeField]
    private CloudWeatherSettings stormSettings =
        new CloudWeatherSettings
        {
            cloudCount = 11,
            cloudColour = new Color(0.52f, 0.55f, 0.60f, 1f),
            opacity = 0.22f,
            minimumScale = 1.15f,
            maximumScale = 1.8f,
            minimumSpeed = 0.30f,
            maximumSpeed = 0.52f
        };

    // =========================================================
    // TRANSITIONS
    // =========================================================

    [Header("Smooth Transitions")]

    [Tooltip("How quickly clouds fade in and out when weather changes.")]
    [Min(0.01f)]
    [SerializeField] private float opacityTransitionSpeed = 1.5f;

    [Tooltip("How quickly cloud colours transition between weather types.")]
    [Min(0.01f)]
    [SerializeField] private float colourTransitionSpeed = 1.5f;

    [Tooltip("How quickly cloud sizes transition.")]
    [Min(0.01f)]
    [SerializeField] private float scaleTransitionSpeed = 1.5f;

    // =========================================================
    // RENDERING
    // =========================================================

    [Header("Rendering")]

    [Tooltip(
        "Cloud sorting layer. Create a Clouds layer above world objects " +
        "but keep it below Screen Space UI."
    )]
    [SerializeField] private string sortingLayerName = "Clouds";

    [SerializeField] private int sortingOrder = 0;

    [Tooltip(
        "World Z used for generated cloud objects. In a normal 2D setup " +
        "sorting layers/orders are more important than this value."
    )]
    [SerializeField] private float cloudZ = 0f;

    [Tooltip(
        "Extra sorting order used to force clouds in front of normal world sprites. " +
        "Keep the Clouds sorting layer above your Player/World layers for the strongest guarantee."
    )]
    [Min(0)]
    [SerializeField] private int frontSortingBoost = 100;

    // =========================================================
    // WEATHER CLOUD SHADOWS
    // =========================================================

    [Header("Sunny Cloud Shadows")]

    [Tooltip("Adds a subtle moving shadow beneath clouds during Sunny weather.")]
    [SerializeField] private bool enableSunnyCloudShadows = true;

    [Tooltip("Shadow tint. Keep the alpha low so gameplay remains readable.")]
    [SerializeField]
    private Color sunnyShadowColour =
        new Color(0.08f, 0.10f, 0.12f, 0.10f);

    [Tooltip(
        "World-space offset of the cloud shadow from the cloud. " +
        "A small diagonal offset makes it feel like sunlight is casting the shadow."
    )]
    [SerializeField]
    private Vector2 sunnyShadowOffset =
        new Vector2(0.35f, -0.35f);

    [Tooltip("Shadow size relative to its cloud.")]
    [Range(0.5f, 2f)]
    [SerializeField] private float sunnyShadowScale = 1.05f;

    [Tooltip(
        "Sorting layer used by the shadow. Set this to the same world layer as your ground/objects."
    )]
    [SerializeField] private string shadowSortingLayerName = "Default";

    [Tooltip(
        "Sorting order for cloud shadows. Adjust this so shadows appear over terrain " +
        "but below the player and other important world sprites."
    )]
    [SerializeField] private int shadowSortingOrder = 5;

    [Tooltip("How quickly shadows fade in/out when weather changes.")]
    [Min(0.01f)]
    [SerializeField] private float shadowTransitionSpeed = 1.5f;

    [Header("Storm Cloud Shadows")]
    [SerializeField] private bool enableStormCloudShadows = true;
    [SerializeField] private Color stormShadowColour = new Color(0.035f, 0.045f, 0.065f, 0.18f);
    [SerializeField] private Vector2 stormShadowOffset = new Vector2(0.18f, -0.22f);
    [Range(0.5f, 2.5f)][SerializeField] private float stormShadowScale = 1.18f;
    [Range(0f, 2f)][SerializeField] private float stormShadowStrength = 1f;

    // =========================================================
    // PIXEL ART
    // =========================================================

    [Header("Pixel Art")]

    [Tooltip(
        "Uses Point filtering on assigned cloud sprite textures at runtime. " +
        "Disable this if those textures are shared with art that needs Bilinear filtering."
    )]
    [SerializeField] private bool forcePointFiltering = false;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField] private bool showDebugLogs = false;

    [SerializeField] private bool debugOverrideWeather = false;

    [SerializeField]
    private DebugCloudWeather debugWeather =
        DebugCloudWeather.Sunny;

    public enum DebugCloudWeather
    {
        Sunny,
        Rain,
        RainAndThunder
    }

    // =========================================================
    // PRIVATE
    // =========================================================

    private readonly List<CloudInstance> clouds =
        new List<CloudInstance>();

    private Transform cloudContainer;

    private string lastWeatherName = "";

    private CloudWeatherSettings activeSettings;

    private Vector3 cloudFieldCentre;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        AutoAssignReferences();
        CreateContainer();

        if (forcePointFiltering)
        {
            ApplyPointFiltering();
        }
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        AutoAssignReferences();

        cloudFieldCentre =
            GetCloudFieldCentre();

        RefreshWeather(true);
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (weatherManager == null)
        {
            weatherManager =
                FindFirstObjectByType<WeatherManager>();
        }

        if (targetCamera == null)
        {
            targetCamera =
                Camera.main;
        }

        cloudFieldCentre =
            GetCloudFieldCentre();

        RefreshWeather(false);
        UpdateClouds();
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private void AutoAssignReferences()
    {
        if (weatherManager == null)
        {
            weatherManager =
                FindFirstObjectByType<WeatherManager>();
        }

        if (targetCamera == null)
        {
            targetCamera =
                Camera.main;
        }
    }

    // =========================================================
    // CONTAINER
    // =========================================================

    private void CreateContainer()
    {
        if (cloudContainer != null)
        {
            return;
        }

        Transform existing =
            transform.Find("Cloud Container");

        if (existing != null)
        {
            cloudContainer =
                existing;

            return;
        }

        GameObject container =
            new GameObject("Cloud Container");

        cloudContainer =
            container.transform;

        cloudContainer.SetParent(
            transform,
            false
        );
    }

    // =========================================================
    // WEATHER
    // =========================================================

    private void RefreshWeather(
        bool force)
    {
        string weatherName =
            GetWeatherName();

        if (!force &&
            weatherName == lastWeatherName)
        {
            return;
        }

        lastWeatherName =
            weatherName;

        activeSettings =
            GetSettingsForWeather(
                weatherName
            );

        if (activeSettings == null)
        {
            activeSettings =
                sunnySettings;
        }

        int desiredCount =
            Mathf.Max(
                0,
                activeSettings.cloudCount
            );

        EnsureCloudPool(
            desiredCount
        );

        ApplyWeatherToClouds(
            desiredCount,
            force
        );

        if (showDebugLogs)
        {
            Debug.Log(
                "[CloudSystem] Weather: " +
                weatherName +
                " | Clouds: " +
                desiredCount +
                " | Opacity: " +
                activeSettings.opacity.ToString("0.00")
            );
        }
    }

    private string GetWeatherName()
    {
        if (debugOverrideWeather)
        {
            return
                debugWeather.ToString();
        }

        if (weatherManager == null)
        {
            return "Sunny";
        }

        return
            weatherManager
                .GetCurrentWeather()
                .ToString();
    }

    private CloudWeatherSettings GetSettingsForWeather(
        string weatherName)
    {
        if (string.IsNullOrWhiteSpace(
            weatherName))
        {
            return sunnySettings;
        }

        string normalized =
            weatherName
                .Trim()
                .ToLowerInvariant();

        if (normalized ==
                "rainandthunder" ||
            normalized ==
                "thunder" ||
            normalized ==
                "storm" ||
            normalized ==
                "lightningstorm")
        {
            return stormSettings;
        }

        if (normalized ==
            "rain")
        {
            return rainSettings;
        }

        return sunnySettings;
    }

    // =========================================================
    // POOL
    // =========================================================

    private void EnsureCloudPool(
        int desiredCount)
    {
        if (cloudSprites == null ||
            cloudSprites.Length == 0)
        {
            return;
        }

        while (clouds.Count <
               desiredCount)
        {
            CreateCloud();
        }
    }

    private void CreateCloud()
    {
        CreateContainer();

        GameObject cloudObject =
            new GameObject(
                "Cloud " +
                (clouds.Count + 1)
            );

        cloudObject.layer =
            gameObject.layer;

        cloudObject.transform.SetParent(
            cloudContainer,
            true
        );

        SpriteRenderer renderer =
            cloudObject.AddComponent<SpriteRenderer>();

        renderer.sprite =
            GetRandomCloudSprite();

        renderer.sortingLayerName =
            sortingLayerName;

        renderer.sortingOrder =
            sortingOrder;

        renderer.color =
            new Color(
                1f,
                1f,
                1f,
                0f
            );

        // Cloud itself is deliberately pushed in front of gameplay.
        renderer.sortingOrder =
            sortingOrder +
            frontSortingBoost;

        // -----------------------------------------------------
        // SUNNY SHADOW
        // -----------------------------------------------------

        GameObject shadowObject =
            new GameObject("Cloud Shadow");

        shadowObject.layer =
            cloudObject.layer;

        shadowObject.transform.SetParent(
            cloudObject.transform,
            false
        );

        shadowObject.transform.localPosition =
            new Vector3(
                sunnyShadowOffset.x,
                sunnyShadowOffset.y,
                0f
            );

        shadowObject.transform.localScale =
            Vector3.one *
            sunnyShadowScale;

        SpriteRenderer shadowRenderer =
            shadowObject.AddComponent<SpriteRenderer>();

        shadowRenderer.sprite =
            renderer.sprite;

        shadowRenderer.sortingLayerName =
            shadowSortingLayerName;

        shadowRenderer.sortingOrder =
            shadowSortingOrder;

        Color startingShadowColour =
            sunnyShadowColour;

        startingShadowColour.a = 0f;

        shadowRenderer.color =
            startingShadowColour;

        shadowRenderer.enabled =
            false;

        CloudInstance cloud =
            new CloudInstance
            {
                gameObject =
                    cloudObject,

                transform =
                    cloudObject.transform,

                renderer =
                    renderer,

                shadowRenderer =
                    shadowRenderer,

                currentAlpha =
                    0f,

                targetAlpha =
                    0f,

                targetScale =
                    1f,

                speed =
                    0.2f,

                wanted =
                    false
            };

        clouds.Add(
            cloud
        );

        RandomiseCloud(
            cloud,
            true
        );
    }

    // =========================================================
    // APPLY WEATHER
    // =========================================================

    private void ApplyWeatherToClouds(
        int desiredCount,
        bool immediate)
    {
        for (int i = 0;
             i < clouds.Count;
             i++)
        {
            CloudInstance cloud =
                clouds[i];

            bool wanted =
                i < desiredCount;

            bool becameWanted =
                wanted &&
                !cloud.wanted;

            cloud.wanted =
                wanted;

            cloud.targetAlpha =
                wanted
                    ? Mathf.Clamp01(
                        activeSettings.opacity
                    )
                    : 0f;

            if (wanted)
            {
                cloud.targetScale =
                    Random.Range(
                        Mathf.Min(
                            activeSettings.minimumScale,
                            activeSettings.maximumScale
                        ),
                        Mathf.Max(
                            activeSettings.minimumScale,
                            activeSettings.maximumScale
                        )
                    );

                cloud.speed =
                    GetRandomWeatherSpeed();

                if (becameWanted)
                {
                    RandomiseCloud(
                        cloud,
                        false
                    );
                }
            }

            if (immediate)
            {
                cloud.currentAlpha =
                    cloud.targetAlpha;

                ApplyCloudColour(
                    cloud,
                    activeSettings.cloudColour,
                    cloud.currentAlpha
                );

                cloud.transform.localScale =
                    Vector3.one *
                    cloud.targetScale;
            }
        }
    }

    // =========================================================
    // UPDATE CLOUDS
    // =========================================================

    private void UpdateClouds()
    {
        if (activeSettings == null)
        {
            return;
        }

        float delta =
            Time.deltaTime;

        Vector2 direction =
            movementDirection.sqrMagnitude >
            0.0001f
                ? movementDirection.normalized
                : Vector2.right;

        for (int i = 0;
             i < clouds.Count;
             i++)
        {
            CloudInstance cloud =
                clouds[i];

            if (cloud == null ||
                cloud.transform == null ||
                cloud.renderer == null)
            {
                continue;
            }

            // -------------------------------------------------
            // MOVEMENT
            // -------------------------------------------------

            if (cloud.wanted ||
                cloud.currentAlpha > 0.001f)
            {
                cloud.transform.position +=
                    new Vector3(
                        direction.x,
                        direction.y,
                        0f
                    ) *
                    cloud.speed *
                    delta;

                if (IsOutsideCloudArea(
                    cloud.transform.position))
                {
                    RecycleCloud(
                        cloud,
                        direction
                    );
                }
            }

            // -------------------------------------------------
            // OPACITY
            // -------------------------------------------------

            cloud.currentAlpha =
                Mathf.MoveTowards(
                    cloud.currentAlpha,
                    cloud.targetAlpha,
                    opacityTransitionSpeed *
                    delta
                );

            // -------------------------------------------------
            // COLOUR
            // -------------------------------------------------

            Color currentColour =
                cloud.renderer.color;

            Color targetColour =
                activeSettings.cloudColour;

            targetColour.a =
                cloud.currentAlpha;

            cloud.renderer.color =
                Color.Lerp(
                    currentColour,
                    targetColour,
                    1f -
                    Mathf.Exp(
                        -colourTransitionSpeed *
                        delta
                    )
                );

            cloud.renderer.color =
                new Color(
                    cloud.renderer.color.r,
                    cloud.renderer.color.g,
                    cloud.renderer.color.b,
                    cloud.currentAlpha
                );

            // -------------------------------------------------
            // SCALE
            // -------------------------------------------------

            Vector3 targetScale =
                Vector3.one *
                cloud.targetScale;

            cloud.transform.localScale =
                Vector3.Lerp(
                    cloud.transform.localScale,
                    targetScale,
                    1f -
                    Mathf.Exp(
                        -scaleTransitionSpeed *
                        delta
                    )
                );

            // -------------------------------------------------
            // RENDERING
            // -------------------------------------------------

            cloud.renderer.sortingLayerName =
                sortingLayerName;

            cloud.renderer.sortingOrder =
                sortingOrder +
                frontSortingBoost;

            cloud.renderer.enabled =
                cloud.currentAlpha >
                0.001f;

            // -------------------------------------------------
            // SUNNY SHADOW
            // -------------------------------------------------

            UpdateWeatherShadow(
                cloud,
                delta
            );
        }
    }

    // =========================================================
    // RANDOMISE
    // =========================================================

    private void RandomiseCloud(
        CloudInstance cloud,
        bool anywhereInArea)
    {
        if (cloud == null)
        {
            return;
        }

        Sprite sprite =
            GetRandomCloudSprite();

        if (sprite != null)
        {
            cloud.renderer.sprite =
                sprite;

            if (cloud.shadowRenderer != null)
            {
                cloud.shadowRenderer.sprite =
                    sprite;
            }
        }

        float halfWidth =
            areaWidth * 0.5f;

        float halfHeight =
            areaHeight * 0.5f;

        Vector3 position =
            cloudFieldCentre;

        if (anywhereInArea)
        {
            position.x +=
                Random.Range(
                    -halfWidth,
                    halfWidth
                );

            position.y +=
                Random.Range(
                    -halfHeight,
                    halfHeight
                );
        }
        else
        {
            position.x +=
                Random.Range(
                    -halfWidth,
                    halfWidth
                );

            position.y +=
                Random.Range(
                    -halfHeight,
                    halfHeight
                );
        }

        position.z =
            cloudZ;

        cloud.transform.position =
            position;

        if (activeSettings != null)
        {
            cloud.targetScale =
                Random.Range(
                    Mathf.Min(
                        activeSettings.minimumScale,
                        activeSettings.maximumScale
                    ),
                    Mathf.Max(
                        activeSettings.minimumScale,
                        activeSettings.maximumScale
                    )
                );

            cloud.speed =
                GetRandomWeatherSpeed();
        }

        // Random horizontal flip makes repeated sprites less obvious.
        Vector3 scale =
            cloud.transform.localScale;

        scale.x =
            Mathf.Abs(scale.x) *
            (Random.value < 0.5f
                ? -1f
                : 1f);

        cloud.transform.localScale =
            scale;
    }

    private float GetRandomWeatherSpeed()
    {
        if (activeSettings == null)
        {
            return 0.2f;
        }

        float minimum =
            Mathf.Min(
                activeSettings.minimumSpeed,
                activeSettings.maximumSpeed
            );

        float maximum =
            Mathf.Max(
                activeSettings.minimumSpeed,
                activeSettings.maximumSpeed
            );

        float speed =
            Random.Range(
                minimum,
                maximum
            );

        float variation =
            1f +
            Random.Range(
                -speedVariation,
                speedVariation
            );

        return
            Mathf.Max(
                0f,
                speed * variation
            );
    }

    // =========================================================
    // RECYCLE
    // =========================================================

    private bool IsOutsideCloudArea(
        Vector3 position)
    {
        float halfWidth =
            areaWidth * 0.5f +
            recyclePadding;

        float halfHeight =
            areaHeight * 0.5f +
            recyclePadding;

        Vector3 difference =
            position -
            cloudFieldCentre;

        return
            Mathf.Abs(difference.x) >
                halfWidth ||
            Mathf.Abs(difference.y) >
                halfHeight;
    }

    private void RecycleCloud(
        CloudInstance cloud,
        Vector2 direction)
    {
        float halfWidth =
            areaWidth * 0.5f;

        float halfHeight =
            areaHeight * 0.5f;

        Vector3 position =
            cloudFieldCentre;

        // Spawn from the side opposite the primary movement direction.
        if (Mathf.Abs(direction.x) >=
            Mathf.Abs(direction.y))
        {
            position.x =
                cloudFieldCentre.x +
                (
                    direction.x >= 0f
                        ? -halfWidth
                        : halfWidth
                );

            position.y =
                cloudFieldCentre.y +
                Random.Range(
                    -halfHeight,
                    halfHeight
                );
        }
        else
        {
            position.y =
                cloudFieldCentre.y +
                (
                    direction.y >= 0f
                        ? -halfHeight
                        : halfHeight
                );

            position.x =
                cloudFieldCentre.x +
                Random.Range(
                    -halfWidth,
                    halfWidth
                );
        }

        position.z =
            cloudZ;

        cloud.transform.position =
            position;

        Sprite sprite =
            GetRandomCloudSprite();

        if (sprite != null)
        {
            cloud.renderer.sprite =
                sprite;

            if (cloud.shadowRenderer != null)
            {
                cloud.shadowRenderer.sprite =
                    sprite;
            }
        }

        cloud.targetScale =
            Random.Range(
                Mathf.Min(
                    activeSettings.minimumScale,
                    activeSettings.maximumScale
                ),
                Mathf.Max(
                    activeSettings.minimumScale,
                    activeSettings.maximumScale
                )
            );

        cloud.speed =
            GetRandomWeatherSpeed();
    }

    // =========================================================
    // CLOUD FIELD CENTRE
    // =========================================================

    private Vector3 GetCloudFieldCentre()
    {
        if (followCamera &&
            targetCamera != null)
        {
            Vector3 position =
                targetCamera.transform.position;

            position.z =
                cloudZ;

            return position;
        }

        Vector3 centre =
            transform.position;

        centre.z =
            cloudZ;

        return centre;
    }

    // =========================================================
    // SPRITES
    // =========================================================

    private Sprite GetRandomCloudSprite()
    {
        if (cloudSprites == null ||
            cloudSprites.Length == 0)
        {
            return null;
        }

        int validCount =
            0;

        foreach (Sprite sprite
                 in cloudSprites)
        {
            if (sprite != null)
            {
                validCount++;
            }
        }

        if (validCount <= 0)
        {
            return null;
        }

        int target =
            Random.Range(
                0,
                validCount
            );

        int current =
            0;

        foreach (Sprite sprite
                 in cloudSprites)
        {
            if (sprite == null)
            {
                continue;
            }

            if (current ==
                target)
            {
                return sprite;
            }

            current++;
        }

        return null;
    }

    // =========================================================
    // SUNNY SHADOW
    // =========================================================

    private void UpdateWeatherShadow(CloudInstance cloud, float delta)
    {
        if (cloud == null || cloud.shadowRenderer == null || activeSettings == null) return;
        bool sunny = IsSunnyWeather();
        bool storm = IsStormWeather();
        Color wantedColour = sunnyShadowColour;
        Vector2 wantedOffset = sunnyShadowOffset;
        float wantedScale = sunnyShadowScale;
        float wantedBaseAlpha = 0f;

        if (storm && enableStormCloudShadows && cloud.wanted)
        {
            wantedColour = stormShadowColour;
            wantedOffset = stormShadowOffset;
            wantedScale = stormShadowScale;
            wantedBaseAlpha = Mathf.Clamp01(stormShadowColour.a * stormShadowStrength);
        }
        else if (sunny && enableSunnyCloudShadows && cloud.wanted)
        {
            wantedBaseAlpha = Mathf.Clamp01(sunnyShadowColour.a);
        }

        float visibleFraction = Mathf.Clamp01(cloud.currentAlpha / Mathf.Max(0.001f, activeSettings.opacity));
        Color target = wantedColour;
        target.a = wantedBaseAlpha * visibleFraction;
        float transition = 1f - Mathf.Exp(-shadowTransitionSpeed * delta);
        cloud.shadowRenderer.color = Color.Lerp(cloud.shadowRenderer.color, target, transition);
        cloud.shadowRenderer.sprite = cloud.renderer.sprite;
        cloud.shadowRenderer.sortingLayerName = shadowSortingLayerName;
        cloud.shadowRenderer.sortingOrder = shadowSortingOrder;
        cloud.shadowRenderer.transform.localPosition = Vector3.Lerp(
            cloud.shadowRenderer.transform.localPosition,
            new Vector3(wantedOffset.x, wantedOffset.y, 0f), transition);
        cloud.shadowRenderer.transform.localScale = Vector3.Lerp(
            cloud.shadowRenderer.transform.localScale, Vector3.one * wantedScale, transition);
        cloud.shadowRenderer.enabled = cloud.shadowRenderer.color.a > 0.001f;
    }

    private bool IsSunnyWeather()
    {
        string weatherName =
            GetWeatherName();

        if (string.IsNullOrWhiteSpace(
            weatherName))
        {
            return true;
        }

        return
            weatherName
                .Trim()
                .Equals(
                    "Sunny",
                    System.StringComparison.OrdinalIgnoreCase
                );
    }

    private bool IsStormWeather()
    {
        string weatherName = GetWeatherName();
        if (string.IsNullOrWhiteSpace(weatherName)) return false;
        string normalized = weatherName.Trim().ToLowerInvariant();
        return normalized == "rainandthunder" || normalized == "thunder" ||
               normalized == "storm" || normalized == "lightningstorm";
    }

    // =========================================================
    // COLOUR
    // =========================================================

    private void ApplyCloudColour(
        CloudInstance cloud,
        Color colour,
        float alpha)
    {
        if (cloud == null ||
            cloud.renderer == null)
        {
            return;
        }

        colour.a =
            Mathf.Clamp01(alpha);

        cloud.renderer.color =
            colour;
    }

    // =========================================================
    // PIXEL FILTERING
    // =========================================================

    private void ApplyPointFiltering()
    {
        if (cloudSprites == null)
        {
            return;
        }

        foreach (Sprite sprite
                 in cloudSprites)
        {
            if (sprite == null ||
                sprite.texture == null)
            {
                continue;
            }

            sprite.texture.filterMode =
                FilterMode.Point;
        }
    }

    // =========================================================
    // PUBLIC REFRESH
    // =========================================================

    public void RefreshClouds()
    {
        lastWeatherName =
            "";

        RefreshWeather(
            true
        );
    }

    // =========================================================
    // DEBUG
    // =========================================================

    [ContextMenu("Debug Clouds - Sunny")]
    private void DebugSunny()
    {
        debugOverrideWeather =
            true;

        debugWeather =
            DebugCloudWeather.Sunny;

        RefreshClouds();
    }

    [ContextMenu("Debug Clouds - Rain")]
    private void DebugRain()
    {
        debugOverrideWeather =
            true;

        debugWeather =
            DebugCloudWeather.Rain;

        RefreshClouds();
    }

    [ContextMenu("Debug Clouds - Lightning Storm")]
    private void DebugStorm()
    {
        debugOverrideWeather =
            true;

        debugWeather =
            DebugCloudWeather.RainAndThunder;

        RefreshClouds();
    }

    [ContextMenu("Debug Clouds - Use Real Weather")]
    private void DebugUseRealWeather()
    {
        debugOverrideWeather =
            false;

        RefreshClouds();
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private void OnValidate()
    {
        areaWidth =
            Mathf.Max(
                1f,
                areaWidth
            );

        areaHeight =
            Mathf.Max(
                1f,
                areaHeight
            );

        recyclePadding =
            Mathf.Max(
                0f,
                recyclePadding
            );

        opacityTransitionSpeed =
            Mathf.Max(
                0.01f,
                opacityTransitionSpeed
            );

        colourTransitionSpeed =
            Mathf.Max(
                0.01f,
                colourTransitionSpeed
            );

        scaleTransitionSpeed =
            Mathf.Max(
                0.01f,
                scaleTransitionSpeed
            );

        frontSortingBoost =
            Mathf.Max(
                0,
                frontSortingBoost
            );

        sunnyShadowScale =
            Mathf.Clamp(
                sunnyShadowScale,
                0.5f,
                2f
            );

        shadowTransitionSpeed =
            Mathf.Max(
                0.01f,
                shadowTransitionSpeed
            );

        stormShadowScale = Mathf.Clamp(stormShadowScale, 0.5f, 2.5f);
        stormShadowStrength = Mathf.Clamp(stormShadowStrength, 0f, 2f);

        ValidateSettings(
            sunnySettings
        );

        ValidateSettings(
            rainSettings
        );

        ValidateSettings(
            stormSettings
        );
    }

    private void ValidateSettings(
        CloudWeatherSettings settings)
    {
        if (settings == null)
        {
            return;
        }

        settings.cloudCount =
            Mathf.Max(
                0,
                settings.cloudCount
            );

        settings.opacity =
            Mathf.Clamp01(
                settings.opacity
            );

        settings.minimumScale =
            Mathf.Max(
                0.01f,
                settings.minimumScale
            );

        settings.maximumScale =
            Mathf.Max(
                settings.minimumScale,
                settings.maximumScale
            );

        settings.minimumSpeed =
            Mathf.Max(
                0f,
                settings.minimumSpeed
            );

        settings.maximumSpeed =
            Mathf.Max(
                settings.minimumSpeed,
                settings.maximumSpeed
            );
    }

    // =========================================================
    // GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Vector3 centre =
            Application.isPlaying
                ? cloudFieldCentre
                : transform.position;

        Gizmos.DrawWireCube(
            centre,
            new Vector3(
                areaWidth,
                areaHeight,
                0f
            )
        );
    }
}
