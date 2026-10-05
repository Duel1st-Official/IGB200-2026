using UnityEngine;

/// <summary>
/// Simple storm screen overlay for Unity 6 + URP 2D Renderer.
/// No Renderer Feature is required.
///
/// SETUP:
/// 1. Put StormScreenOverlay.shader in Assets/Shaders.
/// 2. Create a Material using GhostBat/StormScreenOverlay.
/// 3. Add this script to the Main Camera.
/// 4. Drag the Material into Storm Material.
/// 5. WeatherManager is found automatically.
///
/// The overlay smoothly appears only during RainAndThunder.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class StormScreenEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeatherManager weatherManager;
    [SerializeField] private Material stormMaterial;

    [Header("Storm Fade")]
    [Range(0f, 1f)]
    [SerializeField] private float stormStrength = 1f;

    [Min(0.01f)]
    [SerializeField] private float fadeInSpeed = 1.5f;

    [Min(0.01f)]
    [SerializeField] private float fadeOutSpeed = 2f;

    [Header("Rendering")]
    [Tooltip("Distance in front of the camera. 1 works well for a normal 2D camera.")]
    [Min(0.05f)]
    [SerializeField] private float distanceFromCamera = 1f;

    [SerializeField] private int sortingOrder = 32000;

    [Header("Debug")]
    [SerializeField] private bool debugForceStorm;

    private Camera targetCamera;
    private GameObject overlayObject;
    private MeshRenderer overlayRenderer;
    private Material runtimeMaterial;
    private float currentStrength;

    private static readonly int EffectStrengthID =
        Shader.PropertyToID("_EffectStrength");

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();

        if (weatherManager == null)
        {
            weatherManager =
                FindFirstObjectByType<WeatherManager>();
        }

        CreateOverlay();
    }

    private void Start()
    {
        if (weatherManager == null)
        {
            weatherManager =
                FindFirstObjectByType<WeatherManager>();
        }

        RefreshOverlaySize();
        ApplyStrength(0f);
    }

    private void LateUpdate()
    {
        if (weatherManager == null)
        {
            weatherManager =
                FindFirstObjectByType<WeatherManager>();
        }

        if (overlayObject == null)
        {
            CreateOverlay();
        }

        RefreshOverlaySize();

        bool stormActive =
            debugForceStorm ||
            (weatherManager != null &&
             weatherManager.IsRainAndThunder());

        float target =
            stormActive
                ? stormStrength
                : 0f;

        float speed =
            target > currentStrength
                ? fadeInSpeed
                : fadeOutSpeed;

        currentStrength =
            Mathf.MoveTowards(
                currentStrength,
                target,
                speed * Time.unscaledDeltaTime
            );

        ApplyStrength(currentStrength);

        if (overlayRenderer != null)
        {
            overlayRenderer.enabled =
                currentStrength > 0.001f;
        }
    }

    private void CreateOverlay()
    {
        if (overlayObject != null)
        {
            return;
        }

        overlayObject =
            GameObject.CreatePrimitive(
                PrimitiveType.Quad
            );

        overlayObject.name =
            "Storm Screen Overlay";

        Collider collider =
            overlayObject.GetComponent<Collider>();

        if (collider != null)
        {
            if (Application.isPlaying)
            {
                Destroy(collider);
            }
            else
            {
                DestroyImmediate(collider);
            }
        }

        overlayObject.transform.SetParent(
            transform,
            false
        );

        overlayObject.transform.localPosition =
            new Vector3(
                0f,
                0f,
                distanceFromCamera
            );

        overlayObject.transform.localRotation =
            Quaternion.identity;

        overlayRenderer =
            overlayObject.GetComponent<MeshRenderer>();

        if (overlayRenderer != null)
        {
            overlayRenderer.sortingOrder =
                sortingOrder;
        }

        if (stormMaterial != null)
        {
            runtimeMaterial =
                new Material(stormMaterial);

            if (overlayRenderer != null)
            {
                overlayRenderer.sharedMaterial =
                    runtimeMaterial;
            }
        }
        else
        {
            Debug.LogWarning(
                "[StormScreenEffect] No Storm Material assigned.",
                this
            );
        }
    }

    private void RefreshOverlaySize()
    {
        if (targetCamera == null ||
            overlayObject == null)
        {
            return;
        }

        overlayObject.transform.localPosition =
            new Vector3(
                0f,
                0f,
                distanceFromCamera
            );

        if (targetCamera.orthographic)
        {
            float height =
                targetCamera.orthographicSize * 2f;

            float width =
                height * targetCamera.aspect;

            overlayObject.transform.localScale =
                new Vector3(
                    width,
                    height,
                    1f
                );
        }
        else
        {
            float height =
                2f *
                distanceFromCamera *
                Mathf.Tan(
                    targetCamera.fieldOfView *
                    0.5f *
                    Mathf.Deg2Rad
                );

            float width =
                height * targetCamera.aspect;

            overlayObject.transform.localScale =
                new Vector3(
                    width,
                    height,
                    1f
                );
        }
    }

    private void ApplyStrength(float value)
    {
        if (runtimeMaterial == null)
        {
            return;
        }

        runtimeMaterial.SetFloat(
            EffectStrengthID,
            Mathf.Clamp01(value)
        );
    }

    public void SetStormStrength(float value)
    {
        stormStrength =
            Mathf.Clamp01(value);
    }

    public void SetDebugStorm(bool enabled)
    {
        debugForceStorm =
            enabled;
    }

    [ContextMenu("Debug - Force Storm Effect")]
    private void DebugStormOn()
    {
        debugForceStorm = true;
    }

    [ContextMenu("Debug - Use Real Weather")]
    private void DebugStormOff()
    {
        debugForceStorm = false;
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(runtimeMaterial);
            }
            else
            {
                DestroyImmediate(runtimeMaterial);
            }
        }

        if (overlayObject != null)
        {
            if (Application.isPlaying)
            {
                Destroy(overlayObject);
            }
            else
            {
                DestroyImmediate(overlayObject);
            }
        }
    }

    private void OnValidate()
    {
        stormStrength =
            Mathf.Clamp01(stormStrength);

        fadeInSpeed =
            Mathf.Max(0.01f, fadeInSpeed);

        fadeOutSpeed =
            Mathf.Max(0.01f, fadeOutSpeed);

        distanceFromCamera =
            Mathf.Max(0.05f, distanceFromCamera);
    }
}
