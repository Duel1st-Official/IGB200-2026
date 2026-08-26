using System.Collections;
using UnityEngine;

public class CameraShake2D : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float defaultDuration = 0.08f;
    [SerializeField] private float defaultStrength = 0.04f;

    private Coroutine shakeCoroutine;
    private Vector3 originalLocalPosition;

    private void Awake()
    {
        originalLocalPosition = transform.localPosition;
    }

    public void Shake()
    {
        Shake(defaultDuration, defaultStrength);
    }

    public void Shake(float duration, float strength)
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }

        shakeCoroutine = StartCoroutine(
            ShakeRoutine(duration, strength)
        );
    }

    private IEnumerator ShakeRoutine(
        float duration,
        float strength)
    {
        originalLocalPosition = transform.localPosition;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float progress =
                Mathf.Clamp01(timer / duration);

            // Gets weaker towards the end.
            float currentStrength =
                strength * (1f - progress);

            Vector2 offset =
                Random.insideUnitCircle *
                currentStrength;

            transform.localPosition =
                originalLocalPosition +
                new Vector3(
                    offset.x,
                    offset.y,
                    0f
                );

            yield return null;
        }

        transform.localPosition =
            originalLocalPosition;

        shakeCoroutine = null;
    }

    private void OnDisable()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        transform.localPosition =
            originalLocalPosition;
    }
}