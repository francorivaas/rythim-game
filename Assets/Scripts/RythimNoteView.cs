using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class RhythmNoteView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private Image background;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Optional Colors")]
    [SerializeField] private Color attack1Color = Color.red;
    [SerializeField] private Color attack2Color = Color.blue;
    [SerializeField] private Color attack3Color = Color.green;
    [SerializeField] private Color attack4Color = Color.yellow;

    [Header("Success Juice")]
    [SerializeField] private float goodDuration = 0.14f;
    [SerializeField] private float perfectDuration = 0.18f;
    [SerializeField] private float goodPopScale = 1.18f;
    [SerializeField] private float perfectPopScale = 1.32f;

    [Header("Error Disappear")]
    [SerializeField] private float errorDisappearDuration = 0.08f;
    [SerializeField] private float errorEndScale = 0.75f;

    private Vector3 baseScale;
    private bool resolving;

    private void Awake()
    {
        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        baseScale = rectTransform.localScale;
    }

    public void Setup(AttackType attackType)
    {
        resolving = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        rectTransform.localScale = baseScale;

        if (numberText != null)
        {
            numberText.text =
                ((int)attackType).ToString();
        }

        if (background == null)
            return;

        switch (attackType)
        {
            case AttackType.Attack1:
                background.color = attack1Color;
                break;

            case AttackType.Attack2:
                background.color = attack2Color;
                break;

            case AttackType.Attack3:
                background.color = attack3Color;
                break;

            case AttackType.Attack4:
                background.color = attack4Color;
                break;
        }
    }

    public void SetPosition(
        Vector2 start,
        Vector2 end,
        float progress
    )
    {
        if (resolving)
            return;

        rectTransform.anchoredPosition =
            Vector2.LerpUnclamped(
                start,
                end,
                progress
            );
    }

    public void PlaySuccessAndDestroy(bool perfect)
    {
        if (resolving)
            return;

        resolving = true;

        StartCoroutine(
            SuccessRoutine(perfect)
        );
    }

    public void PlayErrorAndDestroy()
    {
        if (resolving)
            return;

        resolving = true;

        StartCoroutine(
            ErrorRoutine()
        );
    }

    private IEnumerator SuccessRoutine(bool perfect)
    {
        float duration =
            perfect
                ? perfectDuration
                : goodDuration;

        float peakScale =
            perfect
                ? perfectPopScale
                : goodPopScale;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            // Sin(pi*t) sube y vuelve, generando un "pop".
            float pop =
                Mathf.Sin(
                    t * Mathf.PI
                );

            rectTransform.localScale =
                baseScale *
                Mathf.Lerp(
                    1f,
                    peakScale,
                    pop
                );

            if (canvasGroup != null)
            {
                canvasGroup.alpha =
                    1f - t;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator ErrorRoutine()
    {
        float elapsed = 0f;

        while (
            elapsed <
            errorDisappearDuration
        )
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    errorDisappearDuration
                );

            rectTransform.localScale =
                Vector3.Lerp(
                    baseScale,
                    baseScale *
                    errorEndScale,
                    t
                );

            if (canvasGroup != null)
            {
                canvasGroup.alpha =
                    1f - t;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
