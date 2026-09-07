using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class AttackButtonUI :
    MonoBehaviour,
    IPointerDownHandler
{
    [Header("Gameplay")]
    [SerializeField] private SamuraiRhythmDuel duelManager;
    [SerializeField] private AttackType attackType;

    [Header("Button Juice")]
    [Tooltip("Si queda vacío, usa el RectTransform de este objeto.")]
    [SerializeField] private RectTransform visualTarget;

    [SerializeField] private float pressedScale = 0.88f;
    [SerializeField] private float overshootScale = 1.06f;
    [SerializeField] private float punchDuration = 0.14f;

    private Vector3 baseScale;
    private Coroutine punchRoutine;

    private void Awake()
    {
        if (visualTarget == null)
        {
            visualTarget =
                transform as RectTransform;
        }

        if (visualTarget != null)
        {
            baseScale =
                visualTarget.localScale;
        }
    }

    /*
     * Para un rhythm game mobile disparamos en POINTER DOWN,
     * no al soltar el dedo.
     *
     * IMPORTANTE:
     * quitá el listener Press() del Button.onClick para no
     * mandar dos inputs.
     */
    public void OnPointerDown(
        PointerEventData eventData
    )
    {
        TriggerPress();
    }

    // Se mantiene público por compatibilidad/testing.
    public void Press()
    {
        TriggerPress();
    }

    private void TriggerPress()
    {
        duelManager?.PressAttack(
            attackType
        );

        PlayPunch();
    }

    private void PlayPunch()
    {
        if (visualTarget == null)
            return;

        if (punchRoutine != null)
        {
            StopCoroutine(
                punchRoutine
            );

            visualTarget.localScale =
                baseScale;
        }

        punchRoutine =
            StartCoroutine(
                PunchRoutine()
            );
    }

    private IEnumerator PunchRoutine()
    {
        float firstHalf =
            punchDuration * 0.42f;

        float elapsed = 0f;

        while (elapsed < firstHalf)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / firstHalf
                );

            visualTarget.localScale =
                Vector3.Lerp(
                    baseScale,
                    baseScale *
                    pressedScale,
                    t
                );

            yield return null;
        }

        float secondHalf =
            punchDuration -
            firstHalf;

        elapsed = 0f;

        while (elapsed < secondHalf)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / secondHalf
                );

            float wave =
                Mathf.Sin(
                    t * Mathf.PI
                );

            float scale =
                Mathf.Lerp(
                    pressedScale,
                    1f,
                    t
                ) +
                wave *
                (overshootScale - 1f);

            visualTarget.localScale =
                baseScale * scale;

            yield return null;
        }

        visualTarget.localScale =
            baseScale;

        punchRoutine = null;
    }

    private void OnDisable()
    {
        if (visualTarget != null)
        {
            visualTarget.localScale =
                baseScale;
        }
    }
}
