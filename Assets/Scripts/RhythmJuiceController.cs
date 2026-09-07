using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RhythmJuiceController : MonoBehaviour
{
    [Header("Shake")]
    [Tooltip("Idealmente, el panel padre que contiene barra + HitZone + notas.")]
    [SerializeField] private RectTransform shakeTarget;
    [SerializeField] private float errorShakeDuration = 0.16f;
    [SerializeField] private float errorShakeMagnitude = 10f;

    [Header("HitZone Flash")]
    [SerializeField] private Graphic hitZoneGraphic;
    [SerializeField] private Color goodFlashColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color perfectFlashColor = new Color(0.35f, 1f, 0.55f, 1f);
    [SerializeField] private Color errorFlashColor = new Color(1f, 0.25f, 0.25f, 1f);
    [SerializeField] private float flashDuration = 0.14f;

    [Header("Health Animation")]
    [SerializeField] private float healthAnimationDuration = 0.22f;

    [Header("Character Recoil")]
    [SerializeField] private RectTransform playerVisual;
    [SerializeField] private RectTransform enemyVisual;
    [SerializeField] private float goodEnemyRecoil = 13f;
    [SerializeField] private float perfectEnemyRecoil = 22f;
    [SerializeField] private float playerRecoil = 16f;
    [SerializeField] private float recoilDuration = 0.15f;

    [Header("Floating Damage")]
    [SerializeField] private FloatingCombatText floatingTextPrefab;
    [SerializeField] private RectTransform playerDamageAnchor;
    [SerializeField] private RectTransform enemyDamageAnchor;

    [Header("Perfect Hit Stop")]
    [SerializeField] private bool enablePerfectHitStop = true;
    [SerializeField] private float perfectHitStopDuration = 0.055f;

    private Vector2 shakeBasePosition;
    private Vector2 playerBasePosition;
    private Vector2 enemyBasePosition;
    private Color hitZoneBaseColor;

    private Coroutine shakeRoutine;
    private Coroutine flashRoutine;
    private Coroutine playerRecoilRoutine;
    private Coroutine enemyRecoilRoutine;
    private Coroutine hitStopRoutine;

    private readonly Dictionary<Slider, Coroutine>
        healthRoutines =
        new Dictionary<Slider, Coroutine>();

    private float timeScaleBeforeHitStop = 1f;

    private void Awake()
    {
        CacheBaseValues();
    }

    private void CacheBaseValues()
    {
        if (shakeTarget != null)
        {
            shakeBasePosition =
                shakeTarget.anchoredPosition;
        }

        if (playerVisual != null)
        {
            playerBasePosition =
                playerVisual.anchoredPosition;
        }

        if (enemyVisual != null)
        {
            enemyBasePosition =
                enemyVisual.anchoredPosition;
        }

        if (hitZoneGraphic != null)
        {
            hitZoneBaseColor =
                hitZoneGraphic.color;
        }
    }

    public void ResetVisuals()
    {
        StopVisualCoroutines();

        CacheBaseValues();

        if (shakeTarget != null)
        {
            shakeTarget.anchoredPosition =
                shakeBasePosition;
        }

        if (playerVisual != null)
        {
            playerVisual.anchoredPosition =
                playerBasePosition;
        }

        if (enemyVisual != null)
        {
            enemyVisual.anchoredPosition =
                enemyBasePosition;
        }

        if (hitZoneGraphic != null)
        {
            hitZoneGraphic.color =
                hitZoneBaseColor;
        }
    }

    // =====================================================
    // TIMING FEEDBACK
    // =====================================================

    public void PlayGoodFeedback()
    {
        PlayHitZoneFlash(
            goodFlashColor
        );
    }

    public void PlayPerfectFeedback()
    {
        PlayHitZoneFlash(
            perfectFlashColor
        );

        if (enablePerfectHitStop)
        {
            PlayHitStop();
        }
    }

    public void PlayErrorFeedback()
    {
        PlayHitZoneFlash(
            errorFlashColor
        );

        PlayShake();
    }

    // =====================================================
    // DAMAGE FEEDBACK
    // =====================================================

    public void PlayEnemyDamage(
        float damage,
        bool perfect
    )
    {
        SpawnFloatingText(
            enemyDamageAnchor,
            damage,
            perfect
        );

        PlayEnemyRecoil(
            perfect
                ? perfectEnemyRecoil
                : goodEnemyRecoil
        );
    }

    public void PlayPlayerDamage(
        float damage
    )
    {
        SpawnFloatingText(
            playerDamageAnchor,
            damage,
            false
        );

        PlayPlayerRecoil();
    }

    // =====================================================
    // HEALTH
    // =====================================================

    public void SetHealthImmediate(
        Slider slider,
        float target
    )
    {
        if (slider == null)
            return;

        StopHealthRoutine(
            slider
        );

        slider.value =
            target;
    }

    public void AnimateHealth(
        Slider slider,
        float target
    )
    {
        if (slider == null)
            return;

        StopHealthRoutine(
            slider
        );

        Coroutine routine =
            StartCoroutine(
                HealthRoutine(
                    slider,
                    target
                )
            );

        healthRoutines[slider] =
            routine;
    }

    private IEnumerator HealthRoutine(
        Slider slider,
        float target
    )
    {
        float start =
            slider.value;

        float elapsed = 0f;

        while (
            elapsed <
            healthAnimationDuration
        )
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    healthAnimationDuration
                );

            // SmoothStep: transición suave sin sensación "lineal".
            float eased =
                t * t *
                (3f - 2f * t);

            slider.value =
                Mathf.Lerp(
                    start,
                    target,
                    eased
                );

            yield return null;
        }

        slider.value =
            target;

        healthRoutines.Remove(
            slider
        );
    }

    private void StopHealthRoutine(
        Slider slider
    )
    {
        if (
            healthRoutines.TryGetValue(
                slider,
                out Coroutine routine
            )
        )
        {
            if (routine != null)
            {
                StopCoroutine(
                    routine
                );
            }

            healthRoutines.Remove(
                slider
            );
        }
    }

    // =====================================================
    // SHAKE
    // =====================================================

    private void PlayShake()
    {
        if (shakeTarget == null)
            return;

        if (shakeRoutine != null)
        {
            StopCoroutine(
                shakeRoutine
            );

            shakeTarget.anchoredPosition =
                shakeBasePosition;
        }

        shakeRoutine =
            StartCoroutine(
                ShakeRoutine()
            );
    }

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;

        while (
            elapsed <
            errorShakeDuration
        )
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    errorShakeDuration
                );

            float strength =
                1f - t;

            Vector2 offset =
                Random.insideUnitCircle *
                errorShakeMagnitude *
                strength;

            // Preferimos un shake mayormente horizontal.
            offset.y *= 0.35f;

            shakeTarget.anchoredPosition =
                shakeBasePosition +
                offset;

            yield return null;
        }

        shakeTarget.anchoredPosition =
            shakeBasePosition;

        shakeRoutine = null;
    }

    // =====================================================
    // HIT ZONE FLASH
    // =====================================================

    private void PlayHitZoneFlash(
        Color flashColor
    )
    {
        if (hitZoneGraphic == null)
            return;

        if (flashRoutine != null)
        {
            StopCoroutine(
                flashRoutine
            );
        }

        flashRoutine =
            StartCoroutine(
                FlashRoutine(
                    flashColor
                )
            );
    }

    private IEnumerator FlashRoutine(
        Color flashColor
    )
    {
        hitZoneGraphic.color =
            flashColor;

        float elapsed = 0f;

        while (
            elapsed <
            flashDuration
        )
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    flashDuration
                );

            hitZoneGraphic.color =
                Color.Lerp(
                    flashColor,
                    hitZoneBaseColor,
                    t
                );

            yield return null;
        }

        hitZoneGraphic.color =
            hitZoneBaseColor;

        flashRoutine = null;
    }

    // =====================================================
    // RECOIL
    // =====================================================

    private void PlayEnemyRecoil(
        float distance
    )
    {
        if (enemyVisual == null)
            return;

        if (enemyRecoilRoutine != null)
        {
            StopCoroutine(
                enemyRecoilRoutine
            );

            enemyVisual.anchoredPosition =
                enemyBasePosition;
        }

        enemyRecoilRoutine =
            StartCoroutine(
                RecoilRoutine(
                    enemyVisual,
                    enemyBasePosition,
                    Vector2.right,
                    distance,
                    true
                )
            );
    }

    private void PlayPlayerRecoil()
    {
        if (playerVisual == null)
            return;

        if (playerRecoilRoutine != null)
        {
            StopCoroutine(
                playerRecoilRoutine
            );

            playerVisual.anchoredPosition =
                playerBasePosition;
        }

        playerRecoilRoutine =
            StartCoroutine(
                RecoilRoutine(
                    playerVisual,
                    playerBasePosition,
                    Vector2.left,
                    playerRecoil,
                    false
                )
            );
    }

    private IEnumerator RecoilRoutine(
        RectTransform target,
        Vector2 basePosition,
        Vector2 direction,
        float distance,
        bool enemy
    )
    {
        float elapsed = 0f;

        while (
            elapsed <
            recoilDuration
        )
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    recoilDuration
                );

            // Sin(pi*t): sale y vuelve a la posición original.
            float amount =
                Mathf.Sin(
                    t * Mathf.PI
                );

            target.anchoredPosition =
                basePosition +
                direction *
                distance *
                amount;

            yield return null;
        }

        target.anchoredPosition =
            basePosition;

        if (enemy)
        {
            enemyRecoilRoutine = null;
        }
        else
        {
            playerRecoilRoutine = null;
        }
    }

    // =====================================================
    // FLOATING DAMAGE
    // =====================================================

    private void SpawnFloatingText(
        RectTransform anchor,
        float damage,
        bool critical
    )
    {
        if (
            floatingTextPrefab == null ||
            anchor == null
        )
        {
            return;
        }

        FloatingCombatText instance =
            Instantiate(
                floatingTextPrefab,
                anchor
            );

        RectTransform instanceRect =
            instance.transform
            as RectTransform;

        if (instanceRect != null)
        {
            instanceRect.anchoredPosition =
                Vector2.zero;
        }

        instance.Play(
            $"-{damage:0}",
            critical
        );
    }

    // =====================================================
    // HIT STOP
    // =====================================================

    private void PlayHitStop()
    {
        if (hitStopRoutine != null)
        {
            StopCoroutine(
                hitStopRoutine
            );

            Time.timeScale =
                timeScaleBeforeHitStop;
        }

        hitStopRoutine =
            StartCoroutine(
                HitStopRoutine()
            );
    }

    private IEnumerator HitStopRoutine()
    {
        timeScaleBeforeHitStop =
            Time.timeScale;

        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(
            perfectHitStopDuration
        );

        Time.timeScale =
            timeScaleBeforeHitStop;

        hitStopRoutine = null;
    }

    private void StopVisualCoroutines()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        if (playerRecoilRoutine != null)
        {
            StopCoroutine(playerRecoilRoutine);
            playerRecoilRoutine = null;
        }

        if (enemyRecoilRoutine != null)
        {
            StopCoroutine(enemyRecoilRoutine);
            enemyRecoilRoutine = null;
        }

        if (hitStopRoutine != null)
        {
            StopCoroutine(hitStopRoutine);
            hitStopRoutine = null;

            Time.timeScale =
                timeScaleBeforeHitStop;
        }
    }

    private void OnDisable()
    {
        StopVisualCoroutines();
    }
}
