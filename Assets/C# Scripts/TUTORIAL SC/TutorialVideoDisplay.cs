using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(RectTransform), typeof(RawImage))]
public class TutorialVideoDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private RawImage videoImage;
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Playback")]
    [SerializeField] private bool loop = true;
    [SerializeField] private bool muteAudio = true;

    [Header("Hover Enlarge")]
    [Range(1f, 2f)][SerializeField] private float hoverScale = 1.35f;
    [Min(0.01f)][SerializeField] private float hoverSpeed = 12f;

    [Header("Render Texture")]
    [SerializeField] private Vector2Int resolution = new Vector2Int(640, 360);
    [SerializeField] private FilterMode filterMode = FilterMode.Bilinear;

    private RectTransform rect;
    private Vector3 baseScale = Vector3.one;
    private RenderTexture renderTexture;
    private bool hovered;
    private bool initialized;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (rect == null)
            rect = GetComponent<RectTransform>();

        if (videoImage == null)
            videoImage = GetComponent<RawImage>();

        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer == null)
            videoPlayer = gameObject.AddComponent<VideoPlayer>();

        if (!initialized && rect != null)
            baseScale = rect.localScale;

        if (videoImage != null)
            videoImage.raycastTarget = true;

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = loop;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        }

        if (renderTexture == null)
            CreateTexture();

        initialized = true;
    }

    private void Update()
    {
        EnsureInitialized();

        if (rect == null)
            return;

        Vector3 target = baseScale * (hovered ? hoverScale : 1f);
        float t = 1f - Mathf.Exp(-hoverSpeed * Time.unscaledDeltaTime);
        rect.localScale = Vector3.Lerp(rect.localScale, target, t);
    }

    public void SetVideo(VideoClip clip)
    {
        // SetVideo can be called while the Video Tutorial Panel is inactive.
        // Do not assume Awake/OnEnable has already initialized these references.
        EnsureInitialized();

        hovered = false;

        if (rect != null)
            rect.localScale = baseScale;

        if (videoPlayer == null)
            return;

        videoPlayer.Stop();
        videoPlayer.clip = clip;

        bool hasClip = clip != null;

        if (videoImage != null)
            videoImage.enabled = hasClip;

        if (!hasClip)
            return;

        videoPlayer.isLooping = loop;

        if (videoPlayer.audioTrackCount > 0)
            videoPlayer.SetDirectAudioMute(0, muteAudio);

        // Only start playback if this video object is currently active.
        // If the action panel is enabled afterward, OnEnable initializes it
        // and the TutorialManager's step setup can safely call SetVideo again.
        if (isActiveAndEnabled && gameObject.activeInHierarchy)
            videoPlayer.Play();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        transform.SetAsLastSibling();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
    }

    private void CreateTexture()
    {
        if (videoPlayer == null || videoImage == null)
            return;

        if (renderTexture != null)
            return;

        renderTexture = new RenderTexture(
            Mathf.Max(64, resolution.x),
            Mathf.Max(64, resolution.y),
            0,
            RenderTextureFormat.ARGB32);

        renderTexture.name = "TutorialVideo_RuntimeTexture";
        renderTexture.filterMode = filterMode;
        renderTexture.wrapMode = TextureWrapMode.Clamp;
        renderTexture.Create();

        videoPlayer.targetTexture = renderTexture;
        videoImage.texture = renderTexture;
    }

    private void OnDisable()
    {
        hovered = false;

        if (rect != null)
            rect.localScale = baseScale;

        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Pause();
    }

    private void OnDestroy()
    {
        if (videoPlayer != null && videoPlayer.targetTexture == renderTexture)
            videoPlayer.targetTexture = null;

        if (videoImage != null && videoImage.texture == renderTexture)
            videoImage.texture = null;

        if (renderTexture == null)
            return;

        renderTexture.Release();
        Destroy(renderTexture);
        renderTexture = null;
    }
}
