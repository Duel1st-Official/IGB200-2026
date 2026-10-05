using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class WeatherManager : MonoBehaviour
{
    // =========================================================
    // SINGLETON
    // =========================================================

    public static WeatherManager Instance
    {
        get;
        private set;
    }

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
    // CURRENT WEATHER
    // =========================================================

    [Header("Current Weather")]
    [SerializeField]
    private WeatherType currentWeather =
        WeatherType.Sunny;

    [Header("Procedural Daily Weather")]
    [Tooltip("Approximate percentage of sunny days over time. 90 = mostly sunny, 0 = always wet, 100 = always sunny. Wet days are mostly rain with occasional storms. Weather still forms short spells.")]
    [Range(0f, 100f)]
    [SerializeField] private float sunnyWeatherChance = 90f;

    // Preserve the existing seed while keeping the Inspector simple.
    [SerializeField, HideInInspector] private int weatherSeed = 12345;

    private System.Random dailyWeatherRandom;

    // Called once by EndDaySystem after the Night Report closes.
    // This is a seeded, state-dependent pattern, not a climate simulation.
    public void AdvanceDailyWeather()
    {
        // Remember the weather for the day that has just finished.
        // Storm damage is applied AFTER the storm day, when End Day
        // advances the game into the next morning.
        WeatherType weatherThatJustEnded =
            currentWeather;

        if (dailyWeatherRandom == null)
        {
            dailyWeatherRandom = new System.Random(weatherSeed);
        }

        WeatherType nextWeather;

        if (debugOverrideDailyWeather)
        {
            nextWeather =
                debugWeather;
        }
        else
        {
            nextWeather =
                ChooseDailyWeather(
                    currentWeather,
                    sunnyWeatherChance,
                    dailyWeatherRandom.NextDouble()
                );
        }

        // Damage belongs to the storm day that just finished, not
        // the new day that is about to begin.
        if (weatherThatJustEnded ==
            WeatherType.RainAndThunder)
        {
            ApplyLightningStormPlotDamage();
        }

        SetWeather(nextWeather);
    }

    private static WeatherType ChooseDailyWeather(
        WeatherType previous, float sunnyPercent, double roll)
    {
        if (float.IsNaN(sunnyPercent) || float.IsInfinity(sunnyPercent)) sunnyPercent = 90f;
        double sunny = System.Math.Max(0d, System.Math.Min(100d, sunnyPercent)) / 100d;
        if (sunny <= 0d) return roll < 0.9d ? WeatherType.Rain : WeatherType.RainAndThunder;
        if (sunny >= 1d) return WeatherType.Sunny;

        // A little persistence creates spells while retaining the slider's
        // long-run sunny proportion. The remaining wet days are 90% rain.
        double nextSunny = 0.75d * sunny + (previous == WeatherType.Sunny ? 0.25d : 0d);
        if (roll < nextSunny) return WeatherType.Sunny;
        return roll < nextSunny + (1d - nextSunny) * 0.9d
            ? WeatherType.Rain : WeatherType.RainAndThunder;
    }

    // =========================================================
    // LIGHTNING STORM PLOT DAMAGE
    // =========================================================

    private void ApplyLightningStormPlotDamage()
    {
        if (!lightningStormDestroysPlots)
        {
            return;
        }

        List<StormPlotTarget> availableTargets =
            BuildStormPlotTargetList();

        if (availableTargets.Count <= 0)
        {
            if (showStormDamageLogs)
            {
                Debug.Log(
                    "[WeatherManager] Lightning storm found no " +
                    "available plots to destroy."
                );
            }

            return;
        }

        int minimum =
            Mathf.Max(
                0,
                minimumStormPlotsDestroyed
            );

        int maximum =
            Mathf.Max(
                minimum,
                maximumStormPlotsDestroyed
            );

        int requestedCount =
            Random.Range(
                minimum,
                maximum + 1
            );

        int destroyCount =
            Mathf.Min(
                requestedCount,
                availableTargets.Count
            );

        // Fisher-Yates partial shuffle. This guarantees every selected
        // target is unique, so one plot cannot consume two lightning hits.
        for (int i = 0; i < destroyCount; i++)
        {
            int randomIndex =
                Random.Range(
                    i,
                    availableTargets.Count
                );

            StormPlotTarget temp =
                availableTargets[i];

            availableTargets[i] =
                availableTargets[randomIndex];

            availableTargets[randomIndex] =
                temp;

            StormPlotTarget target =
                availableTargets[i];

            if (target.Destroy("Lightning storm"))
            {
                if (showStormDamageLogs)
                {
                    Debug.Log(
                        "[WeatherManager] Lightning storm destroyed " +
                        target.DisplayName +
                        "."
                    );
                }
            }
        }

        if (showStormDamageLogs)
        {
            Debug.Log(
                "[WeatherManager] Lightning storm damage complete. " +
                destroyCount +
                " plot(s) selected from " +
                availableTargets.Count +
                " available plot(s)."
            );
        }
    }

    private List<StormPlotTarget> BuildStormPlotTargetList()
    {
        List<StormPlotTarget> targets =
            new List<StormPlotTarget>();

        // ---------------------------------------------------------
        // FARM / CROP PLOTS
        // ---------------------------------------------------------

        Plot[] farmPlots =
            FindObjectsByType<Plot>(
                FindObjectsSortMode.None
            );

        foreach (Plot plot in farmPlots)
        {
            if (plot == null ||
                plot.IsDestroyed())
            {
                continue;
            }

            targets.Add(
                new StormPlotTarget(plot)
            );
        }

        // ---------------------------------------------------------
        // WATER PLOTS
        // ---------------------------------------------------------

        WaterPlot[] waterPlots =
            FindObjectsByType<WaterPlot>(
                FindObjectsSortMode.None
            );

        foreach (WaterPlot waterPlot in waterPlots)
        {
            if (waterPlot == null ||
                waterPlot.IsDestroyed())
            {
                continue;
            }

            targets.Add(
                new StormPlotTarget(waterPlot)
            );
        }

        // ---------------------------------------------------------
        // TRAP PLOTS
        // ---------------------------------------------------------
        //
        // If a Trap belongs to a normal Plot, that Plot already owns it.
        // Destroying the Plot automatically destroys its Trap too.
        // Therefore only standalone Trap objects are added here. This
        // prevents the same physical plot from being selected twice.

        Trap[] traps =
            FindObjectsByType<Trap>(
                FindObjectsSortMode.None
            );

        foreach (Trap trap in traps)
        {
            if (trap == null ||
                trap.IsDestroyed())
            {
                continue;
            }

            if (trap.GetOwningPlot() != null)
            {
                continue;
            }

            targets.Add(
                new StormPlotTarget(trap)
            );
        }

        return targets;
    }

    private sealed class StormPlotTarget
    {
        private readonly Plot plot;
        private readonly WaterPlot waterPlot;
        private readonly Trap trap;

        public string DisplayName
        {
            get
            {
                if (plot != null)
                {
                    return plot.gameObject.name;
                }

                if (waterPlot != null)
                {
                    return waterPlot.gameObject.name;
                }

                if (trap != null)
                {
                    return trap.gameObject.name;
                }

                return "Unknown Plot";
            }
        }

        public StormPlotTarget(Plot target)
        {
            plot = target;
        }

        public StormPlotTarget(WaterPlot target)
        {
            waterPlot = target;
        }

        public StormPlotTarget(Trap target)
        {
            trap = target;
        }

        public bool Destroy(string cause)
        {
            if (plot != null)
            {
                return plot.DestroyPlot(cause);
            }

            if (waterPlot != null)
            {
                return waterPlot.DestroyWaterPlot(cause);
            }

            if (trap != null)
            {
                return trap.DestroyTrap(cause);
            }

            return false;
        }
    }

    // =========================================================
    // RAIN
    // =========================================================

    [Header("Rain")]
    [SerializeField] private ParticleSystem rainParticles;

    [SerializeField] private float rainTransitionSpeed = 2.5f;

    [SerializeField] private float rainEmissionMultiplier = 1f;

    // =========================================================
    // GLOBAL LIGHT
    // =========================================================

    [Header("Global Light")]
    [SerializeField] private Light2D globalLight;

    [SerializeField] private float lightTransitionSpeed = 3f;

    // =========================================================
    // SUNNY LIGHT
    // =========================================================

    [Header("Morning and Sunset")]
    [SerializeField] private bool enableTimeOfDayLighting = true;
    [SerializeField] private EndDaySystem endDaySystem;
    [Range(0f, 1f)][SerializeField] private float timeOfDayStrength = 0.75f;
    [SerializeField] private Color morningTint = new Color(1f, 0.86f, 0.68f, 1f);
    [SerializeField] private Color sunsetTint = new Color(1f, 0.64f, 0.48f, 1f);
    [Range(0f, 2f)][SerializeField] private float morningBrightness = 0.9f;
    [Range(0f, 2f)][SerializeField] private float sunsetBrightness = 0.75f;

    private void RefreshLightingTargets()
    {
        // Always rebuild from weather, avoiding accumulated colour multiplication.
        switch (currentWeather)
        {
            case WeatherType.Rain:
                targetLightColor = rainLightColor;
                targetLightIntensity = rainLightIntensity;
                break;
            case WeatherType.RainAndThunder:
                targetLightColor = stormLightColor;
                targetLightIntensity = stormLightIntensity;
                break;
            default:
                targetLightColor = sunnyLightColor;
                targetLightIntensity = sunnyLightIntensity;
                break;
        }
        if (!enableTimeOfDayLighting) return;
        if (endDaySystem == null) endDaySystem = FindFirstObjectByType<EndDaySystem>();
        if (endDaySystem == null) return;
        float start = endDaySystem.GetDayStartHour() * 60f + endDaySystem.GetDayStartMinute();
        float end = endDaySystem.GetDayEndHour() * 60f + endDaySystem.GetDayEndMinute();
        float now = endDaySystem.GetCurrentHour() * 60f + endDaySystem.GetCurrentMinute();
        if (end <= start) return;
        float progress = Mathf.InverseLerp(start, end, now);
        Color tint = Color.white;
        float brightness = 1f;
        if (progress < 0.3f)
        {
            float blend = Mathf.SmoothStep(0f, 1f, progress / 0.3f);
            tint = Color.Lerp(morningTint, Color.white, blend);
            brightness = Mathf.Lerp(morningBrightness, 1f, blend);
        }
        else if (progress > 0.65f)
        {
            float blend = Mathf.SmoothStep(0f, 1f, (progress - 0.65f) / 0.35f);
            tint = Color.Lerp(Color.white, sunsetTint, blend);
            brightness = Mathf.Lerp(1f, sunsetBrightness, blend);
        }
        float strength = Mathf.Clamp01(timeOfDayStrength);
        targetLightColor *= Color.Lerp(Color.white, tint, strength);
        targetLightColor.a = 1f;
        targetLightIntensity *= Mathf.Lerp(1f, Mathf.Max(0f, brightness), strength);
    }


    [Header("Sunny Lighting")]
    [SerializeField]
    private Color sunnyLightColor =
        Color.white;

    [SerializeField] private float sunnyLightIntensity = 1f;

    // =========================================================
    // RAIN LIGHT
    // =========================================================

    [Header("Rain Lighting")]
    [SerializeField]
    private Color rainLightColor =
        new Color(
            0.72f,
            0.78f,
            0.85f,
            1f
        );

    [SerializeField] private float rainLightIntensity = 0.8f;

    // =========================================================
    // STORM LIGHT
    // =========================================================

    [Header("Storm Lighting")]
    [SerializeField]
    private Color stormLightColor =
        new Color(
            0.55f,
            0.62f,
            0.72f,
            1f
        );

    [SerializeField] private float stormLightIntensity = 0.65f;

    // =========================================================
    // LIGHTNING
    // =========================================================

    [Header("Lightning")]

    [SerializeField] private float minimumLightningInterval = 4f;

    [SerializeField] private float maximumLightningInterval = 12f;

    [SerializeField]
    private Color lightningColor =
        Color.white;

    [SerializeField] private float lightningIntensity = 1.5f;

    [SerializeField] private float lightningFlashDuration = 0.08f;

    [SerializeField] private float lightningSecondFlashDelay = 0.08f;

    [Range(0f, 1f)]
    [SerializeField] private float secondFlashChance = 0.65f;

    [Header("Realistic Lightning Flash")]
    [Tooltip("How long the first bright flash takes to fade back toward the storm lighting.")]
    [Min(0.01f)]
    [SerializeField] private float lightningFadeDuration = 0.22f;

    [Tooltip("Minimum brightness multiplier used for the optional secondary flicker.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float secondaryFlashMinStrength = 0.45f;

    [Tooltip("Maximum brightness multiplier used for the optional secondary flicker.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float secondaryFlashMaxStrength = 0.8f;

    [Tooltip("How long the secondary flicker fades back into the storm.")]
    [Min(0.01f)]
    [SerializeField] private float secondaryFlashFadeDuration = 0.28f;

    [Tooltip("Adds a tiny random variation to each lightning flash so strikes do not look identical.")]
    [Range(0f, 0.25f)]
    [SerializeField] private float lightningBrightnessVariation = 0.12f;

    [Tooltip("Chance that a strike gets a very quick initial pre-flash before the main flash.")]
    [Range(0f, 1f)]
    [SerializeField] private float preFlashChance = 0.30f;

    [Tooltip("Duration of the subtle pre-flash.")]
    [Min(0.005f)]
    [SerializeField] private float preFlashDuration = 0.025f;

    // =========================================================
    // LIGHTNING STORM PLOT DAMAGE
    // =========================================================

    [Header("Lightning Storm Plot Damage")]

    [Tooltip("When enabled, each new Rain + Thunder day destroys a random number of plots.")]
    [SerializeField] private bool lightningStormDestroysPlots = true;

    [Tooltip("Minimum number of plots destroyed by a lightning storm.")]
    [Min(0)]
    [SerializeField] private int minimumStormPlotsDestroyed = 1;

    [Tooltip("Maximum number of plots destroyed by a lightning storm.")]
    [Min(0)]
    [SerializeField] private int maximumStormPlotsDestroyed = 5;

    [Tooltip("Print which plots were destroyed by the storm.")]
    [SerializeField] private bool showStormDamageLogs = true;

    // =========================================================
    // WEATHER AUDIO SOURCES
    // =========================================================

    [Header("Weather Audio Sources")]

    [Tooltip(
        "First looping ambience source."
    )]
    [SerializeField] private AudioSource weatherAudioSourceA;

    [Tooltip(
        "Second looping ambience source used for crossfading."
    )]
    [SerializeField] private AudioSource weatherAudioSourceB;

    [Tooltip(
        "Separate source used for thunder."
    )]
    [SerializeField] private AudioSource thunderAudioSource;

    // =========================================================
    // SUNNY AUDIO
    // =========================================================

    [Header("Sunny Ambience")]

    [SerializeField] private AudioClip sunnyAmbience;

    [Range(0f, 1f)]
    [SerializeField] private float sunnyVolume = 0.45f;

    // =========================================================
    // RAIN AUDIO
    // =========================================================

    [Header("Rain Ambience")]

    [SerializeField] private AudioClip rainAmbience;

    [Range(0f, 1f)]
    [SerializeField] private float rainVolume = 0.7f;

    // =========================================================
    // STORM AUDIO
    // =========================================================

    [Header("Rain + Thunder Ambience")]

    [SerializeField] private AudioClip stormAmbience;

    [Range(0f, 1f)]
    [SerializeField] private float stormVolume = 0.85f;

    // =========================================================
    // THUNDER AUDIO
    // =========================================================

    [Header("Thunder Sounds")]

    [SerializeField]
    private AudioClip[] thunderSounds =
        new AudioClip[3];

    [Range(0f, 1f)]
    [SerializeField] private float thunderVolume = 1f;

    [SerializeField] private float thunderPitchMin = 0.95f;

    [SerializeField] private float thunderPitchMax = 1.05f;

    [SerializeField] private float thunderDelay = 0.15f;

    // =========================================================
    // AUDIO TRANSITION
    // =========================================================

    [Header("Audio Transition")]

    [SerializeField] private float audioFadeSpeed = 2f;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;

    [Header("Debug Weather Override")]
    [Tooltip(
        "FOR TESTING ONLY. When enabled, every new day uses Debug Weather " +
        "instead of the procedural weather roll."
    )]
    [SerializeField] private bool debugOverrideDailyWeather = false;

    [Tooltip(
        "Weather forced on new days while Debug Override Daily Weather is enabled."
    )]
    [SerializeField]
    private WeatherType debugWeather =
        WeatherType.Sunny;

    [Tooltip(
        "Optional: while enabled in Play Mode, changing Debug Weather in the " +
        "Inspector immediately applies it to the current day too."
    )]
    [SerializeField] private bool debugApplyWeatherLive = false;

    private WeatherType lastDebugWeather;

    // =========================================================
    // PRIVATE
    // =========================================================

    private float targetRainAmount = 0f;
    private float currentRainAmount = 0f;

    private float originalRainEmissionRate = 0f;

    private Color targetLightColor;
    private float targetLightIntensity;

    private Coroutine lightningRoutine;
    private Coroutine lightningFlashRoutine;
    private Coroutine thunderRoutine;

    private bool lightningActive = false;

    private AudioSource activeWeatherSource;
    private AudioSource inactiveWeatherSource;

    private AudioClip targetWeatherClip;
    private float targetWeatherVolume;

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

        Instance = this;

        // =====================================================
        // RAIN SETUP
        // =====================================================

        if (rainParticles != null)
        {
            ParticleSystem.EmissionModule emission =
                rainParticles.emission;

            originalRainEmissionRate =
                emission.rateOverTime.constant;

            emission.rateOverTime =
                0f;

            currentRainAmount =
                0f;
        }

        // =====================================================
        // AUDIO SETUP
        // =====================================================

        SetupAudioSources();

        activeWeatherSource =
            weatherAudioSourceA;

        inactiveWeatherSource =
            weatherAudioSourceB;
    }

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        lastDebugWeather =
            debugWeather;

        // Respect the configured starting weather (Sunny by default).
        SetWeather(
            currentWeather
        );
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // DEBUG ONLY:
        // Lets you change Debug Weather from the Inspector during Play Mode.
        if (debugApplyWeatherLive &&
            debugWeather != lastDebugWeather)
        {
            lastDebugWeather =
                debugWeather;

            SetWeather(
                debugWeather
            );
        }

        UpdateRain();

        UpdateGlobalLight();

        UpdateWeatherAudio();
    }

    // =========================================================
    // AUDIO SOURCE SETUP
    // =========================================================

    private void SetupAudioSources()
    {
        if (weatherAudioSourceA == null)
        {
            weatherAudioSourceA =
                gameObject.AddComponent<AudioSource>();
        }

        weatherAudioSourceA.playOnAwake =
            false;

        weatherAudioSourceA.loop =
            true;

        weatherAudioSourceA.spatialBlend =
            0f;

        if (weatherAudioSourceB == null)
        {
            weatherAudioSourceB =
                gameObject.AddComponent<AudioSource>();
        }

        weatherAudioSourceB.playOnAwake =
            false;

        weatherAudioSourceB.loop =
            true;

        weatherAudioSourceB.spatialBlend =
            0f;

        if (thunderAudioSource == null)
        {
            thunderAudioSource =
                gameObject.AddComponent<AudioSource>();
        }

        thunderAudioSource.playOnAwake =
            false;

        thunderAudioSource.loop =
            false;

        thunderAudioSource.spatialBlend =
            0f;

        weatherAudioSourceA.volume =
            0f;

        weatherAudioSourceB.volume =
            0f;
    }

    // =========================================================
    // SET WEATHER
    // =========================================================

    public void SetWeather(
        WeatherType newWeather)
    {
        currentWeather =
            newWeather;

        switch (currentWeather)
        {
            case WeatherType.Sunny:

                targetRainAmount =
                    0f;

                targetLightColor =
                    sunnyLightColor;

                targetLightIntensity =
                    sunnyLightIntensity;

                StopLightningRoutine();

                SetWeatherAmbience(
                    sunnyAmbience,
                    sunnyVolume
                );

                break;

            case WeatherType.Rain:

                targetRainAmount =
                    1f;

                targetLightColor =
                    rainLightColor;

                targetLightIntensity =
                    rainLightIntensity;

                StartRainParticles();

                StopLightningRoutine();

                SetWeatherAmbience(
                    rainAmbience,
                    rainVolume
                );

                break;

            case WeatherType.RainAndThunder:

                targetRainAmount =
                    1f;

                targetLightColor =
                    stormLightColor;

                targetLightIntensity =
                    stormLightIntensity;

                StartRainParticles();

                StartLightningRoutine();

                SetWeatherAmbience(
                    stormAmbience,
                    stormVolume
                );

                break;
        }

        RefreshLightingTargets();

        if (showDebugLogs)
        {
            Debug.Log(
                "Weather changed to: " +
                currentWeather
            );
        }
    }

    // =========================================================
    // RAIN UPDATE
    // =========================================================

    private void UpdateRain()
    {
        if (rainParticles == null)
        {
            return;
        }

        float smoothAmount =
            1f -
            Mathf.Exp(
                -rainTransitionSpeed *
                Time.deltaTime
            );

        currentRainAmount =
            Mathf.Lerp(
                currentRainAmount,
                targetRainAmount,
                smoothAmount
            );

        ParticleSystem.EmissionModule emission =
            rainParticles.emission;

        emission.rateOverTime =
            originalRainEmissionRate *
            rainEmissionMultiplier *
            currentRainAmount;

        if (targetRainAmount <= 0f &&
            currentRainAmount <= 0.01f)
        {
            currentRainAmount =
                0f;

            emission.rateOverTime =
                0f;

            if (rainParticles.isPlaying)
            {
                rainParticles.Stop(
                    true,
                    ParticleSystemStopBehavior
                        .StopEmittingAndClear
                );
            }
        }
    }

    // =========================================================
    // START RAIN
    // =========================================================

    private void StartRainParticles()
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
    // GLOBAL LIGHT
    // =========================================================

    private void UpdateGlobalLight()
    {
        RefreshLightingTargets();
        if (globalLight == null)
        {
            return;
        }

        if (lightningActive)
        {
            return;
        }

        float smoothAmount =
            1f -
            Mathf.Exp(
                -lightTransitionSpeed *
                Time.deltaTime
            );

        globalLight.color =
            Color.Lerp(
                globalLight.color,
                targetLightColor,
                smoothAmount
            );

        globalLight.intensity =
            Mathf.Lerp(
                globalLight.intensity,
                targetLightIntensity,
                smoothAmount
            );
    }

    // =========================================================
    // SET WEATHER AMBIENCE
    // =========================================================

    private void SetWeatherAmbience(
        AudioClip clip,
        float volume)
    {
        targetWeatherClip =
            clip;

        targetWeatherVolume =
            volume;

        if (clip == null)
        {
            return;
        }

        if (activeWeatherSource != null &&
            activeWeatherSource.clip == clip &&
            activeWeatherSource.isPlaying)
        {
            return;
        }

        AudioSource oldSource =
            activeWeatherSource;

        activeWeatherSource =
            inactiveWeatherSource;

        inactiveWeatherSource =
            oldSource;

        if (activeWeatherSource == null)
        {
            return;
        }

        activeWeatherSource.clip =
            clip;

        activeWeatherSource.volume =
            0f;

        activeWeatherSource.loop =
            true;

        activeWeatherSource.Play();
    }

    // =========================================================
    // UPDATE WEATHER AUDIO
    // =========================================================

    private void UpdateWeatherAudio()
    {
        float fadeAmount =
            audioFadeSpeed *
            Time.deltaTime;

        // =====================================================
        // ACTIVE AUDIO
        // =====================================================

        if (activeWeatherSource != null)
        {
            float wantedVolume =
                0f;

            if (targetWeatherClip != null &&
                activeWeatherSource.clip ==
                targetWeatherClip)
            {
                wantedVolume =
                    targetWeatherVolume;
            }

            activeWeatherSource.volume =
                Mathf.MoveTowards(
                    activeWeatherSource.volume,
                    wantedVolume,
                    fadeAmount
                );
        }

        // =====================================================
        // OLD AUDIO
        // =====================================================

        if (inactiveWeatherSource != null)
        {
            inactiveWeatherSource.volume =
                Mathf.MoveTowards(
                    inactiveWeatherSource.volume,
                    0f,
                    fadeAmount
                );

            if (inactiveWeatherSource.volume <=
                    0.001f &&
                inactiveWeatherSource.isPlaying)
            {
                inactiveWeatherSource.Stop();

                inactiveWeatherSource.clip =
                    null;
            }
        }

        // =====================================================
        // NO AUDIO CLIP
        // =====================================================

        if (targetWeatherClip == null &&
            activeWeatherSource != null)
        {
            activeWeatherSource.volume =
                Mathf.MoveTowards(
                    activeWeatherSource.volume,
                    0f,
                    fadeAmount
                );

            if (activeWeatherSource.volume <=
                    0.001f &&
                activeWeatherSource.isPlaying)
            {
                activeWeatherSource.Stop();

                activeWeatherSource.clip =
                    null;
            }
        }
    }

    // =========================================================
    // START LIGHTNING
    // =========================================================

    private void StartLightningRoutine()
    {
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
    // STOP LIGHTNING
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

        if (thunderRoutine != null)
        {
            StopCoroutine(
                thunderRoutine
            );

            thunderRoutine =
                null;
        }

        lightningActive =
            false;
    }

    // =========================================================
    // LIGHTNING LOOP
    // =========================================================

    private IEnumerator LightningRoutine()
    {
        while (currentWeather ==
               WeatherType.RainAndThunder)
        {
            float waitTime =
                Random.Range(
                    minimumLightningInterval,
                    maximumLightningInterval
                );

            yield return
                new WaitForSeconds(
                    waitTime
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
        if (IsRainAndThunder()) PlotDisasterSystem.GetOrCreate().LightningStrike();
        if (globalLight != null)
        {
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

        PlayThunderWithDelay();
    }

    // =========================================================
    // LIGHTNING FLASH
    // =========================================================

    private IEnumerator LightningFlashRoutine()
    {
        if (globalLight == null)
        {
            yield break;
        }

        lightningActive = true;

        // Refresh first so the flash always returns to the correct
        // storm + time-of-day lighting, even late in the day.
        RefreshLightingTargets();

        Color baseColor = targetLightColor;
        float baseIntensity = targetLightIntensity;

        float variation =
            Random.Range(
                1f - lightningBrightnessVariation,
                1f + lightningBrightnessVariation
            );

        float mainPeak =
            Mathf.Max(
                baseIntensity,
                lightningIntensity * variation
            );

        // A small pre-flash makes some strikes feel like the sky is
        // illuminating just before the main electrical discharge.
        if (Random.value <= preFlashChance)
        {
            float preStrength =
                Random.Range(0.25f, 0.45f);

            globalLight.color =
                Color.Lerp(
                    baseColor,
                    lightningColor,
                    preStrength
                );

            globalLight.intensity =
                Mathf.Lerp(
                    baseIntensity,
                    mainPeak,
                    preStrength
                );

            yield return
                new WaitForSeconds(
                    preFlashDuration
                );

            globalLight.color = baseColor;
            globalLight.intensity = baseIntensity;

            yield return
                new WaitForSeconds(
                    Random.Range(0.015f, 0.045f)
                );
        }

        // Main flash.
        globalLight.color = lightningColor;
        globalLight.intensity = mainPeak;

        yield return
            new WaitForSeconds(
                Mathf.Max(
                    0.01f,
                    lightningFlashDuration
                )
            );

        // Instead of snapping straight back to the storm colour,
        // smoothly fade the light down.
        yield return
            FadeLightningToBase(
                lightningColor,
                mainPeak,
                baseColor,
                baseIntensity,
                lightningFadeDuration
            );

        // Many real lightning discharges pulse several times through
        // the same channel. Keep the existing second-flash chance,
        // but vary its strength so it is not a mechanical duplicate.
        if (Random.value <= secondFlashChance)
        {
            yield return
                new WaitForSeconds(
                    Mathf.Max(
                        0f,
                        lightningSecondFlashDelay +
                        Random.Range(-0.025f, 0.035f)
                    )
                );

            float minSecondary =
                Mathf.Min(
                    secondaryFlashMinStrength,
                    secondaryFlashMaxStrength
                );

            float maxSecondary =
                Mathf.Max(
                    secondaryFlashMinStrength,
                    secondaryFlashMaxStrength
                );

            float secondaryStrength =
                Random.Range(
                    minSecondary,
                    maxSecondary
                );

            float secondaryPeak =
                Mathf.Lerp(
                    baseIntensity,
                    mainPeak,
                    secondaryStrength
                );

            Color secondaryColor =
                Color.Lerp(
                    baseColor,
                    lightningColor,
                    Mathf.Lerp(
                        0.7f,
                        1f,
                        secondaryStrength
                    )
                );

            globalLight.color = secondaryColor;
            globalLight.intensity = secondaryPeak;

            yield return
                new WaitForSeconds(
                    Mathf.Max(
                        0.015f,
                        lightningFlashDuration *
                        Random.Range(0.45f, 0.8f)
                    )
                );

            yield return
                FadeLightningToBase(
                    secondaryColor,
                    secondaryPeak,
                    baseColor,
                    baseIntensity,
                    secondaryFlashFadeDuration
                );
        }

        // Final exact restoration prevents tiny floating-point differences
        // from accumulating between strikes.
        globalLight.color = baseColor;
        globalLight.intensity = baseIntensity;

        lightningActive = false;
        lightningFlashRoutine = null;
    }

    private IEnumerator FadeLightningToBase(
        Color startColor,
        float startIntensity,
        Color baseColor,
        float baseIntensity,
        float duration)
    {
        duration =
            Mathf.Max(
                0.01f,
                duration
            );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            // Fast initial drop with a softer tail looks less like a UI flash.
            float smoothT =
                1f -
                Mathf.Pow(
                    1f - t,
                    2.5f
                );

            globalLight.color =
                Color.Lerp(
                    startColor,
                    baseColor,
                    smoothT
                );

            globalLight.intensity =
                Mathf.Lerp(
                    startIntensity,
                    baseIntensity,
                    smoothT
                );

            yield return null;
        }

        globalLight.color = baseColor;
        globalLight.intensity = baseIntensity;
    }

    // =========================================================
    // THUNDER DELAY
    // =========================================================

    private void PlayThunderWithDelay()
    {
        if (thunderRoutine != null)
        {
            StopCoroutine(
                thunderRoutine
            );
        }

        thunderRoutine =
            StartCoroutine(
                ThunderDelayRoutine()
            );
    }

    private IEnumerator ThunderDelayRoutine()
    {
        if (thunderDelay > 0f)
        {
            yield return
                new WaitForSeconds(
                    thunderDelay
                );
        }

        PlayThunderSound();

        thunderRoutine =
            null;
    }

    // =========================================================
    // THUNDER SOUND
    // =========================================================

    private void PlayThunderSound()
    {
        if (thunderAudioSource == null)
        {
            return;
        }

        if (thunderSounds == null ||
            thunderSounds.Length == 0)
        {
            return;
        }

        int validClipCount =
            0;

        for (int i = 0;
             i < thunderSounds.Length;
             i++)
        {
            if (thunderSounds[i] != null)
            {
                validClipCount++;
            }
        }

        if (validClipCount <= 0)
        {
            return;
        }

        int randomValidIndex =
            Random.Range(
                0,
                validClipCount
            );

        AudioClip selectedClip =
            null;

        int currentValidIndex =
            0;

        for (int i = 0;
             i < thunderSounds.Length;
             i++)
        {
            if (thunderSounds[i] == null)
            {
                continue;
            }

            if (currentValidIndex ==
                randomValidIndex)
            {
                selectedClip =
                    thunderSounds[i];

                break;
            }

            currentValidIndex++;
        }

        if (selectedClip == null)
        {
            return;
        }

        float originalPitch =
            thunderAudioSource.pitch;

        thunderAudioSource.pitch =
            Random.Range(
                thunderPitchMin,
                thunderPitchMax
            );

        thunderAudioSource.PlayOneShot(
            selectedClip,
            thunderVolume
        );

        thunderAudioSource.pitch =
            originalPitch;

        if (showDebugLogs)
        {
            Debug.Log(
                "Thunder: " +
                selectedClip.name
            );
        }
    }

    // =========================================================
    // GET CURRENT WEATHER
    // =========================================================

    public WeatherType GetCurrentWeather()
    {
        return currentWeather;
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

    public bool IsThunder()
    {
        return
            currentWeather ==
            WeatherType.RainAndThunder;
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

    // Compatibility with anything already
    // calling MakeThunder().
    public void MakeThunder()
    {
        MakeRainAndThunder();
    }

    // =========================================================
    // DEBUG WEATHER CONTROL
    // =========================================================

    public void ApplyDebugWeatherNow()
    {
        SetWeather(
            debugWeather
        );

        lastDebugWeather =
            debugWeather;
    }

    public void SetDebugWeatherOverride(
        bool enabled)
    {
        debugOverrideDailyWeather =
            enabled;
    }

    public bool IsDebugWeatherOverrideEnabled()
    {
        return debugOverrideDailyWeather;
    }

    [ContextMenu("Debug Weather - Apply Selected Weather Now")]
    private void DebugApplySelectedWeatherNow()
    {
        ApplyDebugWeatherNow();
    }

    [ContextMenu("Debug Weather - Enable Daily Override")]
    private void DebugEnableWeatherOverride()
    {
        debugOverrideDailyWeather =
            true;
    }

    [ContextMenu("Debug Weather - Disable Daily Override")]
    private void DebugDisableWeatherOverride()
    {
        debugOverrideDailyWeather =
            false;
    }

    // =========================================================
    // DEBUG
    // =========================================================

    [ContextMenu("Weather - Sunny")]
    private void DebugSunny()
    {
        MakeSunny();
    }

    [ContextMenu("Weather - Rain")]
    private void DebugRain()
    {
        MakeRain();
    }

    [ContextMenu("Weather - Rain + Thunder")]
    private void DebugStorm()
    {
        MakeRainAndThunder();
    }

    [ContextMenu("Weather - Lightning Strike")]
    private void DebugLightning()
    {
        TriggerLightning();
    }

    [ContextMenu("Audio - Test Thunder")]
    private void DebugThunder()
    {
        PlayThunderSound();
    }

    // =========================================================
    // DISABLE
    // =========================================================

    private void OnDisable()
    {
        StopLightningRoutine();
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


