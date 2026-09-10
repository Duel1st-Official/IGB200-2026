using System.Collections;
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

        lightningActive =
            true;

        globalLight.color =
            lightningColor;

        globalLight.intensity =
            lightningIntensity;

        yield return
            new WaitForSeconds(
                lightningFlashDuration
            );

        globalLight.color =
            targetLightColor;

        globalLight.intensity =
            targetLightIntensity;

        if (Random.value <=
            secondFlashChance)
        {
            yield return
                new WaitForSeconds(
                    lightningSecondFlashDelay
                );

            globalLight.color =
                lightningColor;

            globalLight.intensity =
                lightningIntensity;

            yield return
                new WaitForSeconds(
                    lightningFlashDuration
                );

            globalLight.color =
                targetLightColor;

            globalLight.intensity =
                targetLightIntensity;
        }

        lightningActive =
            false;

        lightningFlashRoutine =
            null;
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