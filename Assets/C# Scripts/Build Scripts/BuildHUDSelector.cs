using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuildHUDSelector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [SerializeField] private SelectionWheel selectionWheel;
    [SerializeField] private BuildItemSelector buildItemSelector;

    // =========================================================
    // HUD
    // =========================================================

    [Header("HUD")]
    [SerializeField] private GameObject hudRoot;

    [Tooltip("The main RectTransform that scales.")]
    [SerializeField] private RectTransform animatedRoot;

    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private TMP_Text itemNameText;

    // =========================================================
    // ARROWS
    // =========================================================

    [Header("Arrows")]
    [SerializeField] private RectTransform leftArrow;
    [SerializeField] private RectTransform rightArrow;

    [Tooltip("How far the arrows move outward when switching.")]
    [SerializeField] private float arrowMoveDistance = 18f;

    [Tooltip("How quickly the arrows move outward.")]
    [SerializeField] private float arrowOutDuration = 0.08f;

    [Tooltip("How quickly the arrows return.")]
    [SerializeField] private float arrowReturnDuration = 0.12f;

    // =========================================================
    // ICON
    // =========================================================

    [Header("Item Icon")]
    [SerializeField] private Image itemIcon;

    [SerializeField] private Sprite plotIcon;
    [SerializeField] private Sprite trapIcon;
    [SerializeField] private Sprite waterPlotIcon;

    // =========================================================
    // OPTIONAL TEXT
    // =========================================================

    [Header("Optional Text")]
    [SerializeField] private TMP_Text controlHintText;

    [SerializeField]
    private string controlHint =
        "Mouse Wheel";

    // =========================================================
    // HOVER TRANSPARENCY
    // =========================================================

    [Header("Hover Transparency")]

    [Tooltip("HUD transparency while the mouse is over it. 0 = invisible, 1 = fully visible.")]
    [Range(0f, 1f)]
    [SerializeField] private float hoveredAlpha = 0.2f;

    [Tooltip("Normal HUD transparency.")]
    [Range(0f, 1f)]
    [SerializeField] private float normalAlpha = 1f;

    [Tooltip("How quickly the HUD fades when hovered.")]
    [SerializeField] private float fadeOutDuration = 0.12f;

    [Tooltip("How quickly the HUD fades back in.")]
    [SerializeField] private float fadeInDuration = 0.15f;

    // =========================================================
    // APPEAR ANIMATION
    // =========================================================

    [Header("Appear Animation")]

    [Tooltip("Scale the HUD starts at.")]
    [SerializeField] private float startScale = 0.65f;

    [Tooltip("Initial pop scale.")]
    [SerializeField] private float appearPopScale = 1.15f;

    [Tooltip("Normal resting scale.")]
    [SerializeField] private float normalScale = 1f;

    [SerializeField] private float appearPopDuration = 0.12f;
    [SerializeField] private float appearSettleDuration = 0.10f;

    // =========================================================
    // SWITCH SCALE
    // =========================================================

    [Header("Switch Scale")]

    [Tooltip("How large the HUD grows when switching items.")]
    [SerializeField] private float switchScale = 1.12f;

    [Tooltip("How fast the HUD grows.")]
    [SerializeField] private float switchGrowDuration = 0.07f;

    [Tooltip("How fast the HUD settles back.")]
    [SerializeField] private float switchReturnDuration = 0.12f;

    // =========================================================
    // AUTO HIDE
    // =========================================================

    [Header("Auto Hide")]

    [Tooltip("How long the HUD stays visible after the last switch.")]
    [SerializeField] private float visibleDuration = 1.5f;

    [Tooltip("Scale the HUD shrinks to before disappearing.")]
    [SerializeField] private float hideScale = 0.75f;

    [SerializeField] private float hideDuration = 0.15f;

    // =========================================================
    // SETTINGS
    // =========================================================

    [Header("Settings")]
    [SerializeField] private bool hideWhileSelectionWheelOpen = true;

    // =========================================================
    // PRIVATE
    // =========================================================

    private BuildItemSelector.BuildItem lastItem;

    private Coroutine animationCoroutine;
    private Coroutine hideCoroutine;
    private Coroutine fadeCoroutine;

    private bool hasInitialItem;
    private bool hudVisible;
    private bool isMouseOverHUD;

    private Vector2 leftArrowOriginalPosition;
    private Vector2 rightArrowOriginalPosition;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // =====================================================
        // AUTO FIND
        // =====================================================

        if (selectionWheel == null)
        {
            selectionWheel =
                FindFirstObjectByType<SelectionWheel>();
        }

        if (buildItemSelector == null)
        {
            buildItemSelector =
                FindFirstObjectByType<BuildItemSelector>();
        }

        // =====================================================
        // ANIMATED ROOT
        // =====================================================

        if (animatedRoot == null &&
            hudRoot != null)
        {
            animatedRoot =
                hudRoot.GetComponent<RectTransform>();
        }

        // =====================================================
        // CANVAS GROUP
        // =====================================================

        if (canvasGroup == null &&
            hudRoot != null)
        {
            canvasGroup =
                hudRoot.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup =
                    hudRoot.AddComponent<CanvasGroup>();
            }
        }

        // =====================================================
        // SAVE ARROW POSITIONS
        // =====================================================

        if (leftArrow != null)
        {
            leftArrowOriginalPosition =
                leftArrow.anchoredPosition;
        }

        if (rightArrow != null)
        {
            rightArrowOriginalPosition =
                rightArrow.anchoredPosition;
        }

        // =====================================================
        // CONTROL HINT
        // =====================================================

        if (controlHintText != null)
        {
            controlHintText.text =
                controlHint;
        }

        // =====================================================
        // START STATE
        // =====================================================

        if (animatedRoot != null)
        {
            animatedRoot.localScale =
                Vector3.one * startScale;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                normalAlpha;
        }

        ResetArrowPositions();

        if (hudRoot != null)
        {
            hudRoot.SetActive(false);
        }
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (selectionWheel == null ||
            buildItemSelector == null)
        {
            return;
        }

        // =====================================================
        // NOT BUILD MODE
        // =====================================================

        if (!selectionWheel.IsBuildMode())
        {
            hasInitialItem = false;

            HideImmediately();

            return;
        }

        // =====================================================
        // SELECTION WHEEL OPEN
        // =====================================================

        if (hideWhileSelectionWheelOpen &&
            selectionWheel.IsWheelOpen())
        {
            HideImmediately();

            return;
        }

        BuildItemSelector.BuildItem currentItem =
            buildItemSelector.GetCurrentBuildItem();

        // =====================================================
        // FIRST ITEM
        // =====================================================
        //
        // Remember the starting item.
        // Do not show the HUD until the player scrolls.
        // =====================================================

        if (!hasInitialItem)
        {
            lastItem =
                currentItem;

            hasInitialItem =
                true;

            UpdateHUD(
                currentItem
            );

            return;
        }

        // =====================================================
        // ITEM CHANGED
        // =====================================================

        if (currentItem != lastItem)
        {
            lastItem =
                currentItem;

            UpdateHUD(
                currentItem
            );

            OnBuildItemChanged();
        }
    }

    // =========================================================
    // ITEM CHANGED
    // =========================================================

    private void OnBuildItemChanged()
    {
        RestartHideTimer();

        if (animationCoroutine != null)
        {
            StopCoroutine(
                animationCoroutine
            );
        }

        // =====================================================
        // HIDDEN
        // =====================================================

        if (!hudVisible)
        {
            animationCoroutine =
                StartCoroutine(
                    AppearAnimation()
                );
        }

        // =====================================================
        // ALREADY VISIBLE
        // =====================================================

        else
        {
            animationCoroutine =
                StartCoroutine(
                    SwitchAnimation()
                );
        }
    }

    // =========================================================
    // UPDATE HUD
    // =========================================================

    private void UpdateHUD(
        BuildItemSelector.BuildItem item)
    {
        switch (item)
        {
            // =================================================
            // PLOT
            // =================================================

            case BuildItemSelector.BuildItem.Plot:

                if (itemNameText != null)
                {
                    itemNameText.text =
                        "PLOT";
                }

                SetIcon(
                    plotIcon
                );

                break;

            // =================================================
            // TRAP
            // =================================================

            case BuildItemSelector.BuildItem.Trap:

                if (itemNameText != null)
                {
                    itemNameText.text =
                        "TRAP";
                }

                SetIcon(
                    trapIcon
                );

                break;

            // =================================================
            // WATER PLOT
            // =================================================

            case BuildItemSelector.BuildItem.WaterPlot:

                if (itemNameText != null)
                {
                    itemNameText.text =
                        "WATER PLOT";
                }

                SetIcon(
                    waterPlotIcon
                );

                break;
        }
    }

    // =========================================================
    // ICON
    // =========================================================

    private void SetIcon(
        Sprite sprite)
    {
        if (itemIcon == null)
        {
            return;
        }

        itemIcon.sprite =
            sprite;

        itemIcon.enabled =
            sprite != null;
    }

    // =========================================================
    // POINTER ENTER
    // =========================================================

    public void OnPointerEnter(
        PointerEventData eventData)
    {
        if (!hudVisible)
        {
            return;
        }

        isMouseOverHUD =
            true;

        StartFade(
            hoveredAlpha,
            fadeOutDuration
        );
    }

    // =========================================================
    // POINTER EXIT
    // =========================================================

    public void OnPointerExit(
        PointerEventData eventData)
    {
        isMouseOverHUD =
            false;

        StartFade(
            normalAlpha,
            fadeInDuration
        );
    }

    // =========================================================
    // START FADE
    // =========================================================

    private void StartFade(
        float targetAlpha,
        float duration)
    {
        if (canvasGroup == null)
        {
            return;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(
                fadeCoroutine
            );
        }

        fadeCoroutine =
            StartCoroutine(
                FadeCanvasGroup(
                    targetAlpha,
                    duration
                )
            );
    }

    // =========================================================
    // FADE CANVAS GROUP
    // =========================================================

    private IEnumerator FadeCanvasGroup(
        float targetAlpha,
        float duration)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        float startAlpha =
            canvasGroup.alpha;

        float timer =
            0f;

        duration =
            Mathf.Max(
                0.01f,
                duration
            );

        while (timer < duration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    duration
                );

            float eased =
                EaseOutCubic(t);

            canvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    eased
                );

            yield return null;
        }

        canvasGroup.alpha =
            targetAlpha;

        fadeCoroutine =
            null;
    }

    // =========================================================
    // APPEAR ANIMATION
    // =========================================================

    private IEnumerator AppearAnimation()
    {
        if (hudRoot == null ||
            animatedRoot == null)
        {
            yield break;
        }

        hudVisible =
            true;

        hudRoot.SetActive(
            true
        );

        // =====================================================
        // ALPHA
        // =====================================================

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                isMouseOverHUD
                ? hoveredAlpha
                : normalAlpha;
        }

        // =====================================================
        // START SCALE
        // =====================================================

        animatedRoot.localScale =
            Vector3.one *
            startScale;

        ResetArrowPositions();

        // =====================================================
        // POP OUT
        // =====================================================

        yield return StartCoroutine(
            AnimateScale(
                startScale,
                appearPopScale,
                appearPopDuration
            )
        );

        // =====================================================
        // SETTLE
        // =====================================================

        yield return StartCoroutine(
            AnimateScale(
                appearPopScale,
                normalScale,
                appearSettleDuration
            )
        );

        // =====================================================
        // ARROWS
        // =====================================================

        yield return StartCoroutine(
            ArrowPushAnimation()
        );

        animationCoroutine =
            null;
    }

    // =========================================================
    // SWITCH ANIMATION
    // =========================================================

    private IEnumerator SwitchAnimation()
    {
        if (animatedRoot == null)
        {
            yield break;
        }

        animatedRoot.localScale =
            Vector3.one *
            normalScale;

        ResetArrowPositions();

        // =====================================================
        // TARGETS
        // =====================================================

        Vector3 startHUDScale =
            Vector3.one *
            normalScale;

        Vector3 targetHUDScale =
            Vector3.one *
            switchScale;

        Vector2 leftTarget =
            leftArrowOriginalPosition +
            Vector2.left *
            arrowMoveDistance;

        Vector2 rightTarget =
            rightArrowOriginalPosition +
            Vector2.right *
            arrowMoveDistance;

        float timer =
            0f;

        float duration =
            Mathf.Max(
                0.01f,
                Mathf.Max(
                    switchGrowDuration,
                    arrowOutDuration
                )
            );

        // =====================================================
        // GROW + PUSH ARROWS
        // =====================================================

        while (timer < duration)
        {
            timer +=
                Time.unscaledDeltaTime;

            // =================================================
            // SCALE
            // =================================================

            float scaleT =
                Mathf.Clamp01(
                    timer /
                    Mathf.Max(
                        0.01f,
                        switchGrowDuration
                    )
                );

            float scaleEase =
                EaseOutCubic(
                    scaleT
                );

            animatedRoot.localScale =
                Vector3.Lerp(
                    startHUDScale,
                    targetHUDScale,
                    scaleEase
                );

            // =================================================
            // ARROWS
            // =================================================

            float arrowT =
                Mathf.Clamp01(
                    timer /
                    Mathf.Max(
                        0.01f,
                        arrowOutDuration
                    )
                );

            float arrowEase =
                EaseOutCubic(
                    arrowT
                );

            if (leftArrow != null)
            {
                leftArrow.anchoredPosition =
                    Vector2.Lerp(
                        leftArrowOriginalPosition,
                        leftTarget,
                        arrowEase
                    );
            }

            if (rightArrow != null)
            {
                rightArrow.anchoredPosition =
                    Vector2.Lerp(
                        rightArrowOriginalPosition,
                        rightTarget,
                        arrowEase
                    );
            }

            yield return null;
        }

        // =====================================================
        // RETURN
        // =====================================================

        yield return StartCoroutine(
            ReturnFromSwitch()
        );

        animationCoroutine =
            null;
    }

    // =========================================================
    // RETURN FROM SWITCH
    // =========================================================

    private IEnumerator ReturnFromSwitch()
    {
        if (animatedRoot == null)
        {
            yield break;
        }

        float timer =
            0f;

        float duration =
            Mathf.Max(
                0.01f,
                Mathf.Max(
                    switchReturnDuration,
                    arrowReturnDuration
                )
            );

        Vector3 scaleStart =
            animatedRoot.localScale;

        Vector3 scaleTarget =
            Vector3.one *
            normalScale;

        Vector2 leftStart =
            leftArrow != null
            ? leftArrow.anchoredPosition
            : leftArrowOriginalPosition;

        Vector2 rightStart =
            rightArrow != null
            ? rightArrow.anchoredPosition
            : rightArrowOriginalPosition;

        while (timer < duration)
        {
            timer +=
                Time.unscaledDeltaTime;

            // =================================================
            // SCALE
            // =================================================

            float scaleT =
                Mathf.Clamp01(
                    timer /
                    Mathf.Max(
                        0.01f,
                        switchReturnDuration
                    )
                );

            float scaleEase =
                EaseOutCubic(
                    scaleT
                );

            animatedRoot.localScale =
                Vector3.Lerp(
                    scaleStart,
                    scaleTarget,
                    scaleEase
                );

            // =================================================
            // ARROWS
            // =================================================

            float arrowT =
                Mathf.Clamp01(
                    timer /
                    Mathf.Max(
                        0.01f,
                        arrowReturnDuration
                    )
                );

            float arrowEase =
                EaseOutCubic(
                    arrowT
                );

            if (leftArrow != null)
            {
                leftArrow.anchoredPosition =
                    Vector2.Lerp(
                        leftStart,
                        leftArrowOriginalPosition,
                        arrowEase
                    );
            }

            if (rightArrow != null)
            {
                rightArrow.anchoredPosition =
                    Vector2.Lerp(
                        rightStart,
                        rightArrowOriginalPosition,
                        arrowEase
                    );
            }

            yield return null;
        }

        animatedRoot.localScale =
            Vector3.one *
            normalScale;

        ResetArrowPositions();
    }

    // =========================================================
    // FIRST APPEAR ARROW PUSH
    // =========================================================

    private IEnumerator ArrowPushAnimation()
    {
        Vector2 leftTarget =
            leftArrowOriginalPosition +
            Vector2.left *
            arrowMoveDistance;

        Vector2 rightTarget =
            rightArrowOriginalPosition +
            Vector2.right *
            arrowMoveDistance;

        float timer =
            0f;

        // =====================================================
        // OUT
        // =====================================================

        while (timer < arrowOutDuration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    Mathf.Max(
                        0.01f,
                        arrowOutDuration
                    )
                );

            float eased =
                EaseOutCubic(
                    t
                );

            if (leftArrow != null)
            {
                leftArrow.anchoredPosition =
                    Vector2.Lerp(
                        leftArrowOriginalPosition,
                        leftTarget,
                        eased
                    );
            }

            if (rightArrow != null)
            {
                rightArrow.anchoredPosition =
                    Vector2.Lerp(
                        rightArrowOriginalPosition,
                        rightTarget,
                        eased
                    );
            }

            yield return null;
        }

        // =====================================================
        // RETURN
        // =====================================================

        timer =
            0f;

        while (timer < arrowReturnDuration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    Mathf.Max(
                        0.01f,
                        arrowReturnDuration
                    )
                );

            float eased =
                EaseOutCubic(
                    t
                );

            if (leftArrow != null)
            {
                leftArrow.anchoredPosition =
                    Vector2.Lerp(
                        leftTarget,
                        leftArrowOriginalPosition,
                        eased
                    );
            }

            if (rightArrow != null)
            {
                rightArrow.anchoredPosition =
                    Vector2.Lerp(
                        rightTarget,
                        rightArrowOriginalPosition,
                        eased
                    );
            }

            yield return null;
        }

        ResetArrowPositions();
    }

    // =========================================================
    // SCALE
    // =========================================================

    private IEnumerator AnimateScale(
        float from,
        float to,
        float duration)
    {
        if (animatedRoot == null)
        {
            yield break;
        }

        duration =
            Mathf.Max(
                0.01f,
                duration
            );

        float timer =
            0f;

        Vector3 start =
            Vector3.one *
            from;

        Vector3 target =
            Vector3.one *
            to;

        while (timer < duration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    duration
                );

            float eased =
                EaseOutBack(
                    t
                );

            animatedRoot.localScale =
                Vector3.LerpUnclamped(
                    start,
                    target,
                    eased
                );

            yield return null;
        }

        animatedRoot.localScale =
            target;
    }

    // =========================================================
    // AUTO HIDE
    // =========================================================

    private void RestartHideTimer()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(
                hideCoroutine
            );
        }

        hideCoroutine =
            StartCoroutine(
                HideTimer()
            );
    }

    private IEnumerator HideTimer()
    {
        yield return new WaitForSecondsRealtime(
            visibleDuration
        );

        if (animationCoroutine != null)
        {
            StopCoroutine(
                animationCoroutine
            );

            animationCoroutine =
                null;
        }

        yield return StartCoroutine(
            HideAnimation()
        );

        hideCoroutine =
            null;
    }

    // =========================================================
    // HIDE ANIMATION
    // =========================================================

    private IEnumerator HideAnimation()
    {
        if (hudRoot == null ||
            animatedRoot == null)
        {
            yield break;
        }

        Vector3 start =
            animatedRoot.localScale;

        Vector3 target =
            Vector3.one *
            hideScale;

        float timer =
            0f;

        float duration =
            Mathf.Max(
                0.01f,
                hideDuration
            );

        while (timer < duration)
        {
            timer +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    timer /
                    duration
                );

            float eased =
                EaseInCubic(
                    t
                );

            animatedRoot.localScale =
                Vector3.Lerp(
                    start,
                    target,
                    eased
                );

            yield return null;
        }

        hudRoot.SetActive(
            false
        );

        animatedRoot.localScale =
            Vector3.one *
            startScale;

        ResetArrowPositions();

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                normalAlpha;
        }

        isMouseOverHUD =
            false;

        hudVisible =
            false;
    }

    // =========================================================
    // IMMEDIATE HIDE
    // =========================================================

    private void HideImmediately()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(
                animationCoroutine
            );

            animationCoroutine =
                null;
        }

        if (hideCoroutine != null)
        {
            StopCoroutine(
                hideCoroutine
            );

            hideCoroutine =
                null;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(
                fadeCoroutine
            );

            fadeCoroutine =
                null;
        }

        hudVisible =
            false;

        isMouseOverHUD =
            false;

        if (animatedRoot != null)
        {
            animatedRoot.localScale =
                Vector3.one *
                startScale;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                normalAlpha;
        }

        ResetArrowPositions();

        if (hudRoot != null)
        {
            hudRoot.SetActive(
                false
            );
        }
    }

    // =========================================================
    // RESET ARROWS
    // =========================================================

    private void ResetArrowPositions()
    {
        if (leftArrow != null)
        {
            leftArrow.anchoredPosition =
                leftArrowOriginalPosition;
        }

        if (rightArrow != null)
        {
            rightArrow.anchoredPosition =
                rightArrowOriginalPosition;
        }
    }

    // =========================================================
    // EASING
    // =========================================================

    private float EaseOutCubic(
        float x)
    {
        return
            1f -
            Mathf.Pow(
                1f - x,
                3f
            );
    }

    private float EaseOutBack(
        float x)
    {
        const float c1 =
            1.70158f;

        const float c3 =
            c1 + 1f;

        return
            1f +
            c3 *
            Mathf.Pow(
                x - 1f,
                3f
            ) +
            c1 *
            Mathf.Pow(
                x - 1f,
                2f
            );
    }

    private float EaseInCubic(
        float x)
    {
        return
            x * x * x;
    }
}