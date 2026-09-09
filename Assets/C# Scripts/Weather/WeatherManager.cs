using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class WeatherManager : MonoBehaviour
{
    // =========================================================
    // WEATHER TYPE
    // =========================================================

    public enum WeatherType
    {
        Sunny,
        Rain,
        RainAndThunder
    }

    // =========================================================
    // SINGLETON
    // =========================================================

    public static WeatherManager Instance
    {
        get;
        private set;
    }

    // =========================================================
    // WEATHER
    // =========================================================

    [Header("Weather")]
    [SerializeField]
    private WeatherType currentWeather =
        WeatherType.Sunny;

    // =========================================================
    // RAIN
    // =========================================================

    [Header("Rain")]

    [Tooltip("Main looping rain particle system.")]
    [SerializeField] private ParticleSystem rainParticles;

    [Tooltip("How quickly rain fades in and out.")]
    [SerializeField] private float rainFadeSpeed = 8f;

    // =========================================================
    // GLOBAL LIGHT
    // =========================================================

    [Header("Global Light")]

    [SerializeField] private Light2D globalLight;

    [Header("Sunny Light")]

    [SerializeField]
    private Color sunnyLightColor =
        Color.white;

    [SerializeField]
    private float sunnyLightIntensity =
        1f;

    [Header("Rain Light")]

    [SerializeField]
    private Color rainLightColor =
        new Color(
            0.72f,
            0.78f,
            0.86f,
            1f
        );

    [SerializeField]
    private float rainLightIntensity =
        0.75f;

    [Header("Storm Light")]

    [SerializeField]
    private Color stormLightColor =
        new Color(
            0.6f,
            0.68f,
            0.8f,
            1f
        );

    [SerializeField]
    private float stormLightIntensity =
        0.6f;

    [Header("Light Transition")]

    [Tooltip("How quickly the light smoothly changes between weather.")]
    [SerializeField]
    private float lightTransitionSpeed =
        1.5f;

    // =========================================================
    // LIGHTNING
    // =========================================================

    [Header("Lightning")]

    [SerializeField]
    private bool enableLightning =
        true;

    [SerializeField]
    private float minimumLightningDelay =
        4f;

    [SerializeField]
    private float maximumLightningDelay =
        10f;

    [SerializeField]
    private Color lightningColor =
        Color.white;

    [SerializeField]
    private float lightningIntensity =
        1.6f;

    [SerializeField]
    private float lightningFlashDuration =
        0.08f;

    [Header("Double Flash")]

    [SerializeField]
    private bool allowDoubleFlash =
        true;

    [Range(0f, 1f)]
    [SerializeField]
    private float doubleFlashChance =
        0.45f;

    [SerializeField]
    private float doubleFlashDelay =
        0.08f;

    // =========================================================
    // THUNDER PARTICLES
    // =========================================================

    [Header("Thunder / Lightning Burst")]

    [Tooltip(
        "Non-looping particle system played during lightning."
    )]
    [SerializeField] private ParticleSystem thunderBurstParticles;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [SerializeField] private bool debugSunny;

    [SerializeField] private bool debugRain;

    [SerializeField] private bool debugRainAndThunder;

    [SerializeField] private bool debugLightning;

    [SerializeField]
    private bool showDebugLogs =
        false;

    // =========================================================
    // PRIVATE
    // =========================================================

    private float normalRainEmissionRate;

    private float currentRainEmissionRate;

    private float targetRainEmissionRate;

    private Color targetLightColor;

    private float targetLightIntensity;

    private Coroutine lightningRoutine;

    private Coroutine lightningFlashRoutine;

    private bool lightningActive;

    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // =====================================================
        // SINGLETON
        // =====================================================

        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance =
            this;

        // =====================================================
        // RAIN SETUP
        // =====================================================

        if (rainParticles != null)
        {
            ParticleSystem.EmissionModule emission =
                rainParticles.emission;

            normalRainEmissionRate =
                GetEmissionRate(
                    emission
                );

            currentRainEmissionRate =
                0f;

            targetRainEmissionRate =
                0f;

            SetRainEmission(
                0f
            );

            rainParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }

        // =====================================================
        // THUNDER SETUP
        // =====================================================

        if (thunderBurstParticles != null)
        {
            thunderBurstParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // Random weather every time the game starts.

        WeatherType randomWeather =
            (WeatherType)Random.Range(
                0,
                3
            );

        SetWeather(
            randomWeather
        );
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        UpdateRain();

        UpdateGlobalLight();

        HandleDebugControls();
    }

    // =========================================================
    // SET WEATHER
    // =========================================================

    public void SetWeather(
        WeatherType newWeather)
    {
        currentWeather =
            newWeather;

        // =====================================================
        // SUNNY
        // =====================================================

        if (currentWeather ==
            WeatherType.Sunny)
        {
            targetRainEmissionRate =
                0f;

            targetLightColor =
                sunnyLightColor;

            targetLightIntensity =
                sunnyLightIntensity;

            // Stop absolutely all storm behaviour.
            StopLightningRoutine();

            if (thunderBurstParticles != null)
            {
                thunderBurstParticles.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );
            }
        }

        // =====================================================
        // RAIN
        // =====================================================

        else if (currentWeather ==
                 WeatherType.Rain)
        {
            targetRainEmissionRate =
                normalRainEmissionRate;

            targetLightColor =
                rainLightColor;

            targetLightIntensity =
                rainLightIntensity;

            // Rain has NO lightning.
            StopLightningRoutine();

            if (thunderBurstParticles != null)
            {
                thunderBurstParticles.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );
            }

            StartRainIfNeeded();
        }

        // =====================================================
        // RAIN & THUNDER
        // =====================================================

        else if (currentWeather ==
                 WeatherType.RainAndThunder)
        {
            targetRainEmissionRate =
                normalRainEmissionRate;

            targetLightColor =
                stormLightColor;

            targetLightIntensity =
                stormLightIntensity;

            StartRainIfNeeded();

            StartLightningRoutine();
        }

        if (showDebugLogs)
        {
            Debug.Log(
                "Weather changed to: " +
                currentWeather
            );
        }
    }

    // =========================================================
    // START RAIN
    // =========================================================

    private void StartRainIfNeeded()
    {
        if (rainParticles == null)
        {
            return;
        }

        if (!rainParticles.isPlaying)
        {
            rainParticles.Play();
        }
    }

    // =========================================================
    // UPDATE RAIN
    // =========================================================

    private void UpdateRain()
    {
        if (rainParticles == null)
        {
            return;
        }

        // =====================================================
        // FADE EMISSION
        // =====================================================

        float amount =
            1f -
            Mathf.Exp(
                -rainFadeSpeed *
                Time.deltaTime
            );

        currentRainEmissionRate =
            Mathf.Lerp(
                currentRainEmissionRate,
                targetRainEmissionRate,
                amount
            );

        if (Mathf.Abs(
                currentRainEmissionRate -
                targetRainEmissionRate) <
            0.01f)
        {
            currentRainEmissionRate =
                targetRainEmissionRate;
        }

        SetRainEmission(
            currentRainEmissionRate
        );

        // =====================================================
        // SUNNY = FULLY STOP RAIN
        // =====================================================

        if (currentWeather ==
            WeatherType.Sunny)
        {
            if (currentRainEmissionRate <=
                0.01f)
            {
                currentRainEmissionRate =
                    0f;

                SetRainEmission(
                    0f
                );

                if (rainParticles.isPlaying)
                {
                    rainParticles.Stop(
                        true,
                        ParticleSystemStopBehavior.StopEmittingAndClear
                    );
                }
            }
        }

        // =====================================================
        // RAIN WEATHER SHOULD BE PLAYING
        // =====================================================

        else
        {
            StartRainIfNeeded();
        }
    }

    // =========================================================
    // SET RAIN EMISSION
    // =========================================================

    private void SetRainEmission(
        float rate)
    {
        if (rainParticles == null)
        {
            return;
        }

        ParticleSystem.EmissionModule emission =
            rainParticles.emission;

        emission.rateOverTime =
            new ParticleSystem.MinMaxCurve(
                Mathf.Max(
                    0f,
                    rate
                )
            );
    }

    // =========================================================
    // GET ORIGINAL EMISSION RATE
    // =========================================================

    private float GetEmissionRate(
        ParticleSystem.EmissionModule emission)
    {
        ParticleSystem.MinMaxCurve rate =
            emission.rateOverTime;

        switch (rate.mode)
        {
            case ParticleSystemCurveMode.Constant:

                return
                    rate.constant;

            case ParticleSystemCurveMode.TwoConstants:

                return
                    (
                        rate.constantMin +
                        rate.constantMax
                    ) *
                    0.5f;

            default:

                return
                    rate.constantMax;
        }
    }

    // =========================================================
    // UPDATE GLOBAL LIGHT
    // =========================================================

    private void UpdateGlobalLight()
    {
        if (globalLight == null)
        {
            return;
        }

        // Lightning controls the light while flashing.
        if (lightningActive)
        {
            return;
        }

        float delta =
            Time.deltaTime;

        // Smooth exponential transition.
        float smoothAmount =
            1f -
            Mathf.Exp(
                -lightTransitionSpeed *
                delta
            );

        // =====================================================
        // SMOOTH COLOR
        // =====================================================

        globalLight.color =
            Color.Lerp(
                globalLight.color,
                targetLightColor,
                smoothAmount
            );

        // =====================================================
        // SMOOTH INTENSITY
        // =====================================================

        globalLight.intensity =
            Mathf.Lerp(
                globalLight.intensity,
                targetLightIntensity,
                smoothAmount
            );
    }

    // =========================================================
    // START LIGHTNING ROUTINE
    // =========================================================

    private void StartLightningRoutine()
    {
        if (!enableLightning)
        {
            return;
        }

        if (currentWeather !=
            WeatherType.RainAndThunder)
        {
            return;
        }

        if (lightningRoutine != null)
        {
            return;
        }

        lightningRoutine =
            StartCoroutine(
                LightningRoutine()
            );
    }

    // =========================================================
    // STOP LIGHTNING ROUTINE
    // =========================================================

    private void StopLightningRoutine()
    {
        if (lightningRoutine != null)
        {
            StopCoroutine(
                lightningRoutine
            );

            lightningRoutine =
                null;
        }

        if (lightningFlashRoutine != null)
        {
            StopCoroutine(
                lightningFlashRoutine
            );

            lightningFlashRoutine =
                null;
        }

        lightningActive =
            false;
    }

    // =========================================================
    // LIGHTNING ROUTINE
    // =========================================================

    private IEnumerator LightningRoutine()
    {
        while (currentWeather ==
               WeatherType.RainAndThunder)
        {
            float delay =
                Random.Range(
                    minimumLightningDelay,
                    maximumLightningDelay
                );

            yield return
                new WaitForSeconds(
                    delay
                );

            if (currentWeather !=
                WeatherType.RainAndThunder)
            {
                break;
            }

            TriggerLightning();
        }

        lightningRoutine =
            null;
    }

    // =========================================================
    // TRIGGER LIGHTNING
    // =========================================================

    public void TriggerLightning()
    {
        // Do NOT allow weather lightning while
        // Sunny or normal Rain.
        if (currentWeather !=
            WeatherType.RainAndThunder)
        {
            return;
        }

        if (!enableLightning)
        {
            return;
        }

        if (lightningFlashRoutine != null)
        {
            StopCoroutine(
                lightningFlashRoutine
            );
        }

        lightningFlashRoutine =
            StartCoroutine(
                LightningFlashRoutine()
            );
    }

    // =========================================================
    // LIGHTNING FLASH ROUTINE
    // =========================================================

    private IEnumerator LightningFlashRoutine()
    {
        if (currentWeather !=
            WeatherType.RainAndThunder)
        {
            yield break;
        }

        lightningActive =
            true;

        // =====================================================
        // FIRST FLASH
        // =====================================================

        if (globalLight != null)
        {
            globalLight.color =
                lightningColor;

            globalLight.intensity =
                lightningIntensity;
        }

        PlayThunderBurst();

        yield return
            new WaitForSeconds(
                lightningFlashDuration
            );

        if (currentWeather !=
            WeatherType.RainAndThunder)
        {
            lightningActive =
                false;

            lightningFlashRoutine =
                null;

            yield break;
        }

        if (globalLight != null)
        {
            globalLight.color =
                targetLightColor;

            globalLight.intensity =
                targetLightIntensity;
        }

        // =====================================================
        // POSSIBLE SECOND FLASH
        // =====================================================

        bool doDoubleFlash =
            allowDoubleFlash &&
            Random.value <=
            doubleFlashChance;

        if (doDoubleFlash)
        {
            yield return
                new WaitForSeconds(
                    doubleFlashDelay
                );

            if (currentWeather !=
                WeatherType.RainAndThunder)
            {
                lightningActive =
                    false;

                lightningFlashRoutine =
                    null;

                yield break;
            }

            if (globalLight != null)
            {
                globalLight.color =
                    lightningColor;

                globalLight.intensity =
                    lightningIntensity;
            }

            yield return
                new WaitForSeconds(
                    lightningFlashDuration *
                    0.65f
                );

            if (globalLight != null)
            {
                globalLight.color =
                    targetLightColor;

                globalLight.intensity =
                    targetLightIntensity;
            }
        }

        lightningActive =
            false;

        lightningFlashRoutine =
            null;
    }

    // =========================================================
    // PLAY THUNDER BURST
    // =========================================================

    private void PlayThunderBurst()
    {
        if (currentWeather !=
            WeatherType.RainAndThunder)
        {
            return;
        }

        if (thunderBurstParticles == null)
        {
            return;
        }

        thunderBurstParticles.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        thunderBurstParticles.Play();
    }

    // =========================================================
    // GET CURRENT WEATHER
    // =========================================================

    public WeatherType GetCurrentWeather()
    {
        return
            currentWeather;
    }

    // =========================================================
    // WEATHER CHECKS
    // =========================================================

    public bool IsSunny()
    {
        return
            currentWeather ==
            WeatherType.Sunny;
    }

    public bool IsRaining()
    {
        return
            currentWeather ==
            WeatherType.Rain ||
            currentWeather ==
            WeatherType.RainAndThunder;
    }

    public bool IsRainOnly()
    {
        return
            currentWeather ==
            WeatherType.Rain;
    }

    public bool IsRainAndThunder()
    {
        return
            currentWeather ==
            WeatherType.RainAndThunder;
    }

    // Compatibility with your older scripts.
    public bool IsThunder()
    {
        return
            IsRainAndThunder();
    }

    // =========================================================
    // WEATHER SHORTCUTS
    // =========================================================

    public void MakeSunny()
    {
        SetWeather(
            WeatherType.Sunny
        );
    }

    public void MakeRain()
    {
        SetWeather(
            WeatherType.Rain
        );
    }

    public void MakeRainAndThunder()
    {
        SetWeather(
            WeatherType.RainAndThunder
        );
    }

    // Compatibility with older scripts.
    public void MakeThunder()
    {
        MakeRainAndThunder();
    }

    // =========================================================
    // DEBUG CHECKBOXES
    // =========================================================

    private void HandleDebugControls()
    {
        if (debugSunny)
        {
            debugSunny =
                false;

            MakeSunny();
        }

        if (debugRain)
        {
            debugRain =
                false;

            MakeRain();
        }

        if (debugRainAndThunder)
        {
            debugRainAndThunder =
                false;

            MakeRainAndThunder();
        }

        if (debugLightning)
        {
            debugLightning =
                false;

            // This will only trigger during
            // Rain & Thunder.
            TriggerLightning();
        }
    }

    // =========================================================
    // CONTEXT MENU DEBUG
    // =========================================================

    [ContextMenu("Weather - Sunny")]
    private void DebugMakeSunny()
    {
        MakeSunny();
    }

    [ContextMenu("Weather - Rain")]
    private void DebugMakeRain()
    {
        MakeRain();
    }

    [ContextMenu("Weather - Rain & Thunder")]
    private void DebugMakeRainAndThunder()
    {
        MakeRainAndThunder();
    }

    [ContextMenu("Weather - Random")]
    private void DebugRandomWeather()
    {
        WeatherType randomWeather =
            (WeatherType)Random.Range(
                0,
                3
            );

        SetWeather(
            randomWeather
        );
    }

    [ContextMenu("Weather - Test Lightning")]
    private void DebugTestLightning()
    {
        TriggerLightning();
    }

    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance =
                null;
        }
    }
}