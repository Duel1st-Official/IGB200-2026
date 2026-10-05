using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum TutorialStepType
{
    Information,
    WaitForKey,
    WaitForAction
}

public enum TutorialPanelPosition
{
    BottomLeft,
    Center
}

[Serializable]
public class TutorialStep
{
    [Header("Content")]
    public Sprite titleSprite;

    [TextArea(3, 8)]
    public string description;

    [Header("Progress")]
    public TutorialStepType stepType = TutorialStepType.Information;
    public KeyCode requiredKey = KeyCode.None;
    public TutorialAction requiredAction = TutorialAction.None;

    [Header("Advancing")]
    [Tooltip("Shows the Continue button for this step.")]
    public bool showContinueButton = false;

    [Tooltip("For Information steps only. Automatically progresses without pressing Continue.")]
    public bool autoAdvance = false;

    [Tooltip("Delay before progressing after Continue, Auto Advance, key press, or required action.")]
    [Min(0f)]
    public float advanceDelay = 0f;

    [Header("Presentation")]
    public bool pauseGame = true;
    public bool dimScreen = true;
    public TutorialPanelPosition panelPosition = TutorialPanelPosition.BottomLeft;
    public Vector2 panelOffset;
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TutorialUI tutorialUI;

    [Header("Panel Position")]
    [Tooltip("Distance from the left and bottom edges when a step uses Bottom Left.")]
    [SerializeField] private Vector2 bottomLeftPanelPadding = new Vector2(40f, 40f);
    [Tooltip("How quickly the panel glides between Bottom Left and Center.")]
    [Min(0.01f)]
    [SerializeField] private float panelMoveSpeed = 8f;

    private Vector2 currentPanelPosition;
    private bool hasPanelPosition;

    [Header("Tutorial")]
    [SerializeField] private TutorialStep[] steps;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private int startingStep;

    [Header("Tutorial End Day Button")]
    [Tooltip("Assign the TutorialEndDayButtonController that keeps End Day locked until the END DAY tutorial step.")]
    [SerializeField] private TutorialEndDayButtonController endDayButtonController;

    [Header("Tutorial Tours")]
    [Tooltip("Optional Tours inspection UI reference.")]
    [SerializeField] private ToursBuildingInspectionUI toursInspectionUI;

    [Tooltip("Dedicated tutorial controller that keeps the Start Tour button disabled until the CONSERVATION TOURS step.")]
    [SerializeField] private TutorialToursButtonController toursButtonController;

    [Tooltip("The inspectable Tours building. Inspection unlocks at the same tutorial step.")]
    [SerializeField] private InspectableToursBuilding inspectableToursBuilding;

    [Header("Player")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Camera worldCamera;

    [Header("Tutorial Audio")]
    [SerializeField] private AudioSource tutorialAudioSource;
    [SerializeField] private AudioClip continueButtonSound;
    [SerializeField] private AudioClip actionStepCompleteSound;
    [SerializeField] private AudioClip tutorialCompleteSound;
    [Range(0f, 1f)][SerializeField] private float continueButtonVolume = 0.8f;
    [Range(0f, 1f)][SerializeField] private float actionStepCompleteVolume = 0.9f;
    [Range(0f, 1f)][SerializeField] private float tutorialCompleteVolume = 1f;

    [Header("Completion")]
    [SerializeField] private bool saveCompletion = true;
    [SerializeField] private string completionPlayerPrefsKey = "TutorialCompleted";
    [SerializeField] private string mainGameSceneName = "Game";
    [SerializeField] private float sceneTransitionDelay = 0.35f;

    private int currentStepIndex = -1;
    private bool running;
    private bool advancing;
    private float previousTimeScale = 1f;

    private Coroutine advanceRoutine;
    private Coroutine autoAdvanceRoutine;

    public bool IsRunning => running;
    public int CurrentStepIndex => currentStepIndex;
    public Transform GetPlayerTransform()
    {
        FindPlayerIfNeeded();
        return playerTransform;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (worldCamera == null)
            worldCamera = Camera.main;

        FindPlayerIfNeeded();

        if (endDayButtonController == null)
            endDayButtonController = FindFirstObjectByType<TutorialEndDayButtonController>();

        if (toursInspectionUI == null)
            toursInspectionUI = FindFirstObjectByType<ToursBuildingInspectionUI>();

        if (toursButtonController == null)
            toursButtonController = FindFirstObjectByType<TutorialToursButtonController>();

        if (inspectableToursBuilding == null)
            inspectableToursBuilding = FindFirstObjectByType<InspectableToursBuilding>();

        if (tutorialAudioSource == null)
        {
            tutorialAudioSource = GetComponent<AudioSource>();

            if (tutorialAudioSource == null)
                tutorialAudioSource = gameObject.AddComponent<AudioSource>();

            tutorialAudioSource.playOnAwake = false;
            tutorialAudioSource.loop = false;
            tutorialAudioSource.spatialBlend = 0f;
        }
    }

    private void OnEnable()
    {
        TutorialEvents.ActionReported += HandleAction;
    }

    private void OnDisable()
    {
        TutorialEvents.ActionReported -= HandleAction;

        if (advanceRoutine != null)
            StopCoroutine(advanceRoutine);

        if (autoAdvanceRoutine != null)
            StopCoroutine(autoAdvanceRoutine);

        RestoreTime();
    }

    private void Start()
    {
        if (tutorialUI != null && tutorialUI.ContinueButton != null)
            tutorialUI.ContinueButton.onClick.AddListener(ContinueCurrentStep);

        if (playOnStart)
            StartTutorial();
    }

    private void Update()
    {
        if (!running || advancing || !ValidStep())
            return;

        TutorialStep step = steps[currentStepIndex];

        if (step.stepType == TutorialStepType.WaitForKey &&
            step.requiredKey != KeyCode.None &&
            Input.GetKeyDown(step.requiredKey))
        {
            RequestAdvance(step.advanceDelay);
        }

        UpdatePresentation();
    }

    public void StartTutorial()
    {
        if (tutorialUI == null || steps == null || steps.Length == 0)
        {
            Debug.LogWarning("TutorialManager needs a TutorialUI and at least one Tutorial Step.");
            return;
        }

        running = true;
        advancing = false;

        ShowStep(Mathf.Clamp(startingStep, 0, steps.Length - 1));
    }

    public void ContinueCurrentStep()
    {
        if (!running || advancing || !ValidStep())
            return;

        TutorialStep step = steps[currentStepIndex];

        if (!step.showContinueButton)
            return;

        PlayTutorialSound(continueButtonSound, continueButtonVolume);
        RequestAdvance(step.advanceDelay);
    }

    public void ReportAction(TutorialAction action)
    {
        HandleAction(action);
    }

    private void HandleAction(TutorialAction action)
    {
        if (!running || advancing || !ValidStep())
            return;

        TutorialStep step = steps[currentStepIndex];

        if (step.stepType == TutorialStepType.WaitForAction &&
            step.requiredAction == action)
        {
            // Action-driven steps have no Continue button, so reward the
            // successful action immediately with the completion/party sound.
            if (!step.showContinueButton)
                PlayTutorialSound(actionStepCompleteSound, actionStepCompleteVolume);

            RequestAdvance(step.advanceDelay);
        }
    }

    private void ShowStep(int index)
    {
        if (index < 0 || index >= steps.Length)
        {
            FinishTutorial();
            return;
        }

        if (autoAdvanceRoutine != null)
        {
            StopCoroutine(autoAdvanceRoutine);
            autoAdvanceRoutine = null;
        }

        currentStepIndex = index;
        advancing = false;
        TutorialStep step = steps[index];

        ApplyPause(step.pauseGame);

        // Unlock End Day exactly when the tutorial reaches the
        // END DAY action step. Earlier tutorial steps keep it disabled.
        if (step.stepType == TutorialStepType.WaitForAction &&
            step.requiredAction == TutorialAction.EndDayPressed &&
            endDayButtonController != null)
        {
            endDayButtonController.EnableEndDayButton();
        }

        if (step.stepType == TutorialStepType.WaitForAction &&
            step.requiredAction == TutorialAction.TourStarted)
        {
            if (inspectableToursBuilding != null)
                inspectableToursBuilding.UnlockForTutorial();

            if (toursInspectionUI != null)
                toursInspectionUI.UnlockStartTourButtonForTutorial();

            if (toursButtonController != null)
                toursButtonController.EnableToursButton();
        }

        tutorialUI.SetContent(
            step.titleSprite,
            step.description,
            index + 1,
            steps.Length,
            step.showContinueButton,
            step.dimScreen);

        UpdatePresentation();
        tutorialUI.Show();

        tutorialUI.SetContinueButtonLabel(
            index == steps.Length - 1
                ? "Finish Tutorial"
                : "Continue");

        // The final tutorial-complete fanfare plays when the last step appears,
        // not when the player presses its Continue button.
        if (index == steps.Length - 1)
            PlayTutorialSound(tutorialCompleteSound, tutorialCompleteVolume);

        if (step.stepType == TutorialStepType.Information &&
            step.autoAdvance &&
            !step.showContinueButton)
        {
            autoAdvanceRoutine = StartCoroutine(
                AutoAdvanceRoutine(step.advanceDelay));
        }
    }

    private IEnumerator AutoAdvanceRoutine(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);
        else
            yield return null;

        autoAdvanceRoutine = null;

        if (running && !advancing)
            RequestAdvance(0f);
    }

    private void RequestAdvance(float delay)
    {
        if (!running || advancing)
            return;

        advancing = true;

        if (autoAdvanceRoutine != null)
        {
            StopCoroutine(autoAdvanceRoutine);
            autoAdvanceRoutine = null;
        }

        advanceRoutine = StartCoroutine(AdvanceRoutine(delay));
    }

    private IEnumerator AdvanceRoutine(float delay)
    {
        // The current step stays visible during this delay.
        // This makes movement/action completion feel less abrupt.
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        // Keep the panel visible between tutorial steps.
        // TutorialUI cross-fades only the content while the panel glides
        // smoothly to the next step's selected position.
        int next = currentStepIndex + 1;
        advanceRoutine = null;

        if (next >= steps.Length)
        {
            FinishTutorial();
            yield break;
        }

        ShowStep(next);
    }

    public void FinishTutorial()
    {
        if (!running)
            return;

        running = false;
        advancing = false;

        if (advanceRoutine != null)
        {
            StopCoroutine(advanceRoutine);
            advanceRoutine = null;
        }

        if (autoAdvanceRoutine != null)
        {
            StopCoroutine(autoAdvanceRoutine);
            autoAdvanceRoutine = null;
        }

        // Keep the final tutorial panel visible. It fades to black together
        // with the game background instead of disappearing first.
        RestoreTime();

        if (saveCompletion)
        {
            PlayerPrefs.SetInt(completionPlayerPrefsKey, 1);
            PlayerPrefs.Save();
        }

        StartCoroutine(LoadMainGameRoutine());
    }

    private IEnumerator LoadMainGameRoutine()
    {
        if (string.IsNullOrWhiteSpace(mainGameSceneName))
        {
            Debug.LogWarning("Tutorial complete, but Main Game Scene Name is empty.");
            yield break;
        }

        // Optional short hold after clicking Finish Tutorial.
        if (sceneTransitionDelay > 0f)
            yield return new WaitForSecondsRealtime(sceneTransitionDelay);

        // Full-screen black overlay sits above both the world and tutorial UI,
        // so the background and panel fade away together.
        if (tutorialUI != null)
            yield return tutorialUI.FadeEverythingToBlack();

        SceneManager.LoadScene(mainGameSceneName);
    }

    private void PlayTutorialSound(AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        if (tutorialAudioSource == null)
            return;

        tutorialAudioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private void ApplyPause(bool pause)
    {
        if (pause)
        {
            if (Time.timeScale > 0f)
                previousTimeScale = Time.timeScale;

            Time.timeScale = 0f;
        }
        else
        {
            RestoreTime();
        }
    }

    private void RestoreTime()
    {
        if (Time.timeScale == 0f)
            Time.timeScale = Mathf.Max(0.01f, previousTimeScale);
    }

    private bool ValidStep()
    {
        return steps != null &&
               currentStepIndex >= 0 &&
               currentStepIndex < steps.Length;
    }

    private void UpdatePresentation()
    {
        if (tutorialUI == null || !ValidStep())
            return;

        TutorialStep step = steps[currentStepIndex];

        Vector2 desiredPosition =
            step.panelPosition == TutorialPanelPosition.Center
                ? GetCenterPanelPosition()
                : GetBottomLeftPanelPosition();

        desiredPosition += step.panelOffset;

        if (!hasPanelPosition)
        {
            currentPanelPosition = desiredPosition;
            hasPanelPosition = true;
        }
        else
        {
            float moveT =
                1f - Mathf.Exp(-panelMoveSpeed * Time.unscaledDeltaTime);

            currentPanelPosition =
                Vector2.Lerp(currentPanelPosition, desiredPosition, moveT);
        }

        tutorialUI.SetPanelScreenPosition(currentPanelPosition);
    }

    private Vector2 GetCenterPanelPosition()
    {
        return new Vector2(
            Screen.width * 0.5f,
            Screen.height * 0.5f);
    }

    private Vector2 GetBottomLeftPanelPosition()
    {
        Vector2 size = tutorialUI.GetPanelScreenSize();

        float halfW = Mathf.Max(100f, size.x * 0.5f);
        float halfH = Mathf.Max(60f, size.y * 0.5f);

        return new Vector2(
            bottomLeftPanelPadding.x + halfW,
            bottomLeftPanelPadding.y + halfH);
    }

    private void FindPlayerIfNeeded()
    {
        if (playerTransform != null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
            playerTransform = player.transform;
    }

    private bool TryGetPlayerScreenPosition(out Vector2 screen)
    {
        screen = Vector2.zero;

        FindPlayerIfNeeded();

        if (playerTransform == null)
            return false;

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (worldCamera == null)
            return false;

        Vector3 point =
            worldCamera.WorldToScreenPoint(playerTransform.position);

        if (point.z < 0f)
            return false;

        screen = new Vector2(point.x, point.y);
        return true;
    }


}
