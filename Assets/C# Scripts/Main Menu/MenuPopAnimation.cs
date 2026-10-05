using UnityEngine;
using UnityEngine.EventSystems;

// Used automatically by MainMenuSystem. Unscaled time keeps UI animation responsive.
public class MenuPopAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    private bool hovered, selected, pressed;
    private Vector3 baseScale;
    private void Awake() { baseScale = transform.localScale; }
    private void OnEnable() { hovered = selected = pressed = false; transform.localScale = baseScale; }
    private void Update()
    {
        float target = pressed ? 0.95f : (hovered || selected ? 1.045f : 1f);
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale * target,
            1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));
    }
    public void OnPointerEnter(PointerEventData data) { hovered = true; }
    public void OnPointerExit(PointerEventData data) { hovered = false; pressed = false; }
    public void OnPointerDown(PointerEventData data)
    { if (data.button == PointerEventData.InputButton.Left) pressed = true; }
    public void OnPointerUp(PointerEventData data) { pressed = false; }
    public void OnSelect(BaseEventData data) { selected = true; }
    public void OnDeselect(BaseEventData data) { selected = false; pressed = false; }
    private void OnDisable() { transform.localScale = baseScale; }
}
