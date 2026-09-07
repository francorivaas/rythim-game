using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class FloatingCombatText : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text textLabel;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation")]
    [SerializeField] private float duration = 0.55f;
    [SerializeField] private float riseDistance = 70f;
    [SerializeField] private float horizontalRandomness = 18f;
    [SerializeField] private float normalStartScale = 1f;
    [SerializeField] private float criticalStartScale = 1.25f;

    private void Awake()
    {
        if (rectTransform == null)
        {
            rectTransform =
                transform as RectTransform;
        }

        if (canvasGroup == null)
        {
            canvasGroup =
                GetComponent<CanvasGroup>();
        }
    }

    public void Play(
        string message,
        bool critical
    )
    {
        if (textLabel != null)
        {
            textLabel.text =
                message;
        }

        StartCoroutine(
            AnimationRoutine(
                critical
            )
        );
    }

    private IEnumerator AnimationRoutine(
        bool critical
    )
    {
        Vector2 start =
            rectTransform.anchoredPosition;

        start.x +=
            Random.Range(
                -horizontalRandomness,
                horizontalRandomness
            );

        rectTransform.anchoredPosition =
            start;

        float startScale =
            critical
                ? criticalStartScale
                : normalStartScale;

        rectTransform.localScale =
            Vector3.one *
            startScale;

        canvasGroup.alpha = 1f;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            float eased =
                1f -
                Mathf.Pow(
                    1f - t,
                    2f
                );

            rectTransform.anchoredPosition =
                start +
                Vector2.up *
                riseDistance *
                eased;

            rectTransform.localScale =
                Vector3.one *
                Mathf.Lerp(
                    startScale,
                    0.9f,
                    t
                );

            // Mantiene el texto sólido al principio
            // y hace fade principalmente en la segunda mitad.
            canvasGroup.alpha =
                1f -
                Mathf.Clamp01(
                    (t - 0.35f) /
                    0.65f
                );

            yield return null;
        }

        Destroy(gameObject);
    }
}
