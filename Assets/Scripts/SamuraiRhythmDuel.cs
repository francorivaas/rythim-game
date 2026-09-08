using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public enum AttackType
{
    Attack1 = 1,
    Attack2 = 2,
    Attack3 = 3,
    Attack4 = 4
}

public class SamuraiRhythmDuel : MonoBehaviour
{
    private enum DuelState
    {
        Countdown,
        Playing,
        Won,
        Lost
    }

    [Serializable]
    public class DifficultyProfile
    {
        [Tooltip("Este perfil se usa desde este enemigo hasta que exista otro perfil con un número mayor.")]
        [Min(1)]
        public int startsAtEnemy = 1;

        [Tooltip("Tiempo que tarda una nota en viajar desde SpawnPoint hasta HitPoint.")]
        [Min(0.5f)]
        public float noteTravelTime = 2.8f;

        [Header("Pesos de tamaño de grupo")]
        [Tooltip("Peso relativo para generar una nota individual.")]
        [Min(0f)]
        public float singleWeight = 100f;

        [Tooltip("Peso relativo para generar una ráfaga de 2 notas.")]
        [Min(0f)]
        public float twoNoteWeight = 0f;

        [Tooltip("Peso relativo para generar una ráfaga de 3 notas.")]
        [Min(0f)]
        public float threeNoteWeight = 0f;

        [Tooltip("Peso relativo para generar una ráfaga de 4 notas.")]
        [Min(0f)]
        public float fourNoteWeight = 0f;

        [Header("Separación dentro del grupo")]
        [Tooltip("Separación mínima, en segundos, entre impactos de una misma ráfaga.")]
        [Min(0.12f)]
        public float minNoteSpacing = 0.70f;

        [Tooltip("Separación máxima, en segundos, entre impactos de una misma ráfaga.")]
        [Min(0.12f)]
        public float maxNoteSpacing = 0.76f;

        [Header("Descanso entre grupos")]
        [Tooltip("Pausa mínima después de que llega la última nota de un grupo.")]
        [Min(0f)]
        public float minPauseBetweenGroups = 0.95f;

        [Tooltip("Pausa máxima después de que llega la última nota de un grupo.")]
        [Min(0f)]
        public float maxPauseBetweenGroups = 1.25f;
    }

    private class NoteData
    {
        public float targetTime;
        public AttackType attackType;
        public RhythmNoteView view;
    }

    // =====================================================
    // HEALTH
    // =====================================================

    [Header("Health")]
    [SerializeField] private float playerMaxHealth = 100f;
    [SerializeField] private float enemyMaxHealth = 100f;

    [Header("Damage")]
    [SerializeField] private float goodDamage = 15f;
    [SerializeField] private float perfectDamage = 25f;
    [SerializeField] private float earlyDamageToPlayer = 15f;
    [SerializeField] private float wrongDamageToPlayer = 15f;
    [SerializeField] private float missDamageToPlayer = 10f;

    // =====================================================
    // DIFFICULTY CURVE
    // =====================================================

    [Header("Difficulty Curve")]

    [Tooltip("Curva completa. El último perfil sigue aplicándose a todos los enemigos posteriores.")]
    [SerializeField]
    private List<DifficultyProfile> difficultyCurve =
        new List<DifficultyProfile>();

    [Tooltip("Tiempo mínimo de reacción entre la aparición de una ráfaga y la llegada de su primera nota.")]
    [SerializeField] private float minimumBurstReactionTime = 0.65f;

    [Tooltip("Tiempo después de ¡YA! antes de generar el primer grupo.")]
    [SerializeField] private float firstGroupDelay = 0.65f;

    [Tooltip("Opcional: muestra el número del enemigo actual.")]
    [SerializeField] private TMP_Text enemyCounterText;

    [Tooltip("Opcional: muestra los parámetros del perfil actual para debug/balance.")]
    [SerializeField] private TMP_Text difficultyText;

    // =====================================================
    // TIMING WINDOWS
    // =====================================================

    [Header("Timing Windows")]
    [Tooltip("Ventana PERFECT en segundos.")]
    [SerializeField] private float perfectWindow = 0.07f;

    [Tooltip("Ventana GOOD en segundos.")]
    [SerializeField] private float goodWindow = 0.16f;

    // =====================================================
    // RHYTHM UI
    // =====================================================

    [Header("Rhythm Lane")]
    [SerializeField] private RectTransform notesContainer;
    [SerializeField] private RectTransform spawnPoint;
    [SerializeField] private RectTransform hitPoint;
    [SerializeField] private RhythmNoteView notePrefab;

    // =====================================================
    // HEALTH UI
    // =====================================================

    [Header("Health UI")]
    [SerializeField] private Slider playerHealthSlider;
    [SerializeField] private Slider enemyHealthSlider;

    // =====================================================
    // FEEDBACK
    // =====================================================

    [Header("Feedback")]
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private float feedbackDuration = 0.5f;

    // =====================================================
    // COUNTDOWN
    // =====================================================

    [Header("Countdown")]
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private float countdownStepDuration = 0.8f;
    [SerializeField] private float goDuration = 0.45f;

    // =====================================================
    // JUICINESS
    // =====================================================

    [Header("Juiciness")]
    [Tooltip("Controlador opcional de feedback visual. Si queda vacío, el gameplay sigue funcionando.")]
    [SerializeField] private RhythmJuiceController juiceController;

    // =====================================================
    // ROUND
    // =====================================================

    [Header("Round")]
    [SerializeField] private bool autoRestart = true;
    [SerializeField] private float restartDelay = 2f;

    [Tooltip("Pequeña pausa después del golpe mortal antes de mostrar VICTORY.")]
    [SerializeField] private float victoryRevealDelay = 0.35f;

    // =====================================================
    // DEBUG INPUT
    // =====================================================

    [Header("PC Debug")]
    [SerializeField] private bool enableKeyboardInput = true;

    // =====================================================
    // PRIVATE
    // =====================================================

    private float playerHealth;
    private float enemyHealth;

    private float duelTimer;
    private float restartTimer;
    private float feedbackTimer;

    private float countdownTimer;
    private int countdownValue;

    private float victoryRevealTimer;
    private bool victoryMessageShown;

    private int enemiesDefeated;
    private float nextGroupSpawnTime;

    private DifficultyProfile activeDifficulty;
    private DuelState currentState;

    private readonly List<NoteData> notes =
        new List<NoteData>();

    // =====================================================
    // UNITY
    // =====================================================

    private void Start()
    {
        enemiesDefeated = 0;

        EnsureDifficultyCurveExists();
        StartDuel();
    }

    private void Update()
    {
        UpdateFeedback();

        switch (currentState)
        {
            case DuelState.Countdown:
                UpdateCountdown();
                break;

            case DuelState.Playing:
                duelTimer += Time.deltaTime;

                UpdateGroupGenerator();
                UpdateNotes();

                // El input se evalúa antes del MISS automático.
                if (enableKeyboardInput)
                {
                    ReadKeyboardInput();
                }

                ProcessMissedNotes();
                break;

            case DuelState.Won:
            case DuelState.Lost:
                UpdateFinishedDuel();
                break;
        }
    }

    // =====================================================
    // ROUND START
    // =====================================================

    private void StartDuel()
    {
        ClearNotes();

        playerHealth = playerMaxHealth;
        enemyHealth = enemyMaxHealth;

        duelTimer = 0f;
        nextGroupSpawnTime = 0f;

        victoryRevealTimer = 0f;
        victoryMessageShown = false;

        UpdateDifficultyForCurrentEnemy();
        SetHealthUIImmediate();
        UpdateProgressUI();

        if (juiceController != null)
        {
            juiceController.ResetVisuals();
        }

        if (feedbackText != null)
        {
            feedbackText.text = "";
        }

        feedbackTimer = 0f;

        BeginCountdown();
    }

    // =====================================================
    // COUNTDOWN
    // =====================================================

    private void BeginCountdown()
    {
        currentState = DuelState.Countdown;

        countdownValue = 3;
        countdownTimer = countdownStepDuration;

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "3";
        }
    }

    private void UpdateCountdown()
    {
        countdownTimer -= Time.deltaTime;

        if (countdownTimer > 0f)
            return;

        if (countdownValue > 1)
        {
            countdownValue--;
            countdownTimer = countdownStepDuration;

            if (countdownText != null)
            {
                countdownText.text = countdownValue.ToString();
            }

            return;
        }

        if (countdownValue == 1)
        {
            countdownValue = 0;
            countdownTimer = goDuration;

            if (countdownText != null)
            {
                countdownText.text = "¡YA!";
            }

            return;
        }

        StartGameplayAfterCountdown();
    }

    private void StartGameplayAfterCountdown()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        duelTimer = 0f;
        currentState = DuelState.Playing;

        nextGroupSpawnTime = firstGroupDelay;
    }

    // =====================================================
    // DIFFICULTY
    // =====================================================

    private void UpdateDifficultyForCurrentEnemy()
    {
        int currentEnemy = enemiesDefeated + 1;

        activeDifficulty =
            GetDifficultyProfile(currentEnemy);
    }

    private DifficultyProfile GetDifficultyProfile(int enemyNumber)
    {
        EnsureDifficultyCurveExists();

        DifficultyProfile selected = null;
        int highestStartFound = int.MinValue;

        // Elegimos el perfil con startsAtEnemy más alto
        // que todavía sea <= al enemigo actual.
        foreach (DifficultyProfile profile in difficultyCurve)
        {
            if (profile == null)
                continue;

            if (
                profile.startsAtEnemy <= enemyNumber &&
                profile.startsAtEnemy > highestStartFound
            )
            {
                selected = profile;
                highestStartFound =
                    profile.startsAtEnemy;
            }
        }

        // Fallback: si por alguna razón no existe un perfil para
        // el enemigo 1, usamos el perfil con menor startsAtEnemy.
        if (selected == null)
        {
            int lowestStartFound = int.MaxValue;

            foreach (DifficultyProfile profile in difficultyCurve)
            {
                if (profile == null)
                    continue;

                if (profile.startsAtEnemy < lowestStartFound)
                {
                    selected = profile;
                    lowestStartFound =
                        profile.startsAtEnemy;
                }
            }
        }

        return selected;
    }

    private void UpdateProgressUI()
    {
        int currentEnemy =
            enemiesDefeated + 1;

        if (enemyCounterText != null)
        {
            enemyCounterText.text =
                $"ENEMIGO {currentEnemy}";
        }

        if (
            difficultyText != null &&
            activeDifficulty != null
        )
        {
            float totalWeight =
                GetTotalGroupWeight(
                    activeDifficulty
                );

            float p1 = GetPercentage(
                activeDifficulty.singleWeight,
                totalWeight
            );

            float p2 = GetPercentage(
                activeDifficulty.twoNoteWeight,
                totalWeight
            );

            float p3 = GetPercentage(
                activeDifficulty.threeNoteWeight,
                totalWeight
            );

            float p4 = GetPercentage(
                activeDifficulty.fourNoteWeight,
                totalWeight
            );

            difficultyText.text =
                $"E{currentEnemy} | {activeDifficulty.noteTravelTime:0.00}s | " +
                $"x1 {p1:0}% x2 {p2:0}% x3 {p3:0}% x4 {p4:0}%";
        }
    }

    // =====================================================
    // GROUP GENERATOR
    // =====================================================

    private void UpdateGroupGenerator()
    {
        if (duelTimer < nextGroupSpawnTime)
            return;

        SpawnWeightedGroup();
    }

    private void SpawnWeightedGroup()
    {
        if (activeDifficulty == null)
            return;

        int amount =
            ChooseGroupSize(activeDifficulty);

        float travelTime =
            activeDifficulty.noteTravelTime;

        if (amount <= 1)
        {
            float targetTime =
                duelTimer +
                travelTime;

            CreateAndSpawnNote(
                targetTime
            );

            ScheduleNextGroup(
                targetTime,
                activeDifficulty
            );

            return;
        }

        float configuredSpacing =
            UnityEngine.Random.Range(
                activeDifficulty.minNoteSpacing,
                activeDifficulty.maxNoteSpacing
            );

        /*
         * Las notas aparecen todas en el mismo frame.
         *
         * La última queda en SpawnPoint y las anteriores
         * aparecen adelantadas en la pista.
         *
         * Ejemplo de x3:
         *
         * HIT                    SPAWN
         *  |      [2] [4] [1]      |
         * ---------------------------
         */

        float maxSafeSpacing =
            (travelTime -
             minimumBurstReactionTime) /
            (amount - 1);

        maxSafeSpacing =
            Mathf.Max(
                0.12f,
                maxSafeSpacing
            );

        float actualSpacing =
            Mathf.Min(
                configuredSpacing,
                maxSafeSpacing
            );

        for (int i = 0; i < amount; i++)
        {
            int notesAfterThis =
                amount - 1 - i;

            float targetTime =
                duelTimer +
                travelTime -
                (notesAfterThis * actualSpacing);

            CreateAndSpawnNote(
                targetTime
            );
        }

        float lastTargetTime =
            duelTimer +
            travelTime;

        ScheduleNextGroup(
            lastTargetTime,
            activeDifficulty
        );
    }

    private int ChooseGroupSize(
        DifficultyProfile profile
    )
    {
        float totalWeight =
            GetTotalGroupWeight(profile);

        if (totalWeight <= 0f)
        {
            return 1;
        }

        float roll =
            UnityEngine.Random.Range(
                0f,
                totalWeight
            );

        if (
            roll <
            profile.singleWeight
        )
        {
            return 1;
        }

        roll -=
            profile.singleWeight;

        if (
            roll <
            profile.twoNoteWeight
        )
        {
            return 2;
        }

        roll -=
            profile.twoNoteWeight;

        if (
            roll <
            profile.threeNoteWeight
        )
        {
            return 3;
        }

        return 4;
    }

    private float GetTotalGroupWeight(
        DifficultyProfile profile
    )
    {
        if (profile == null)
            return 0f;

        return
            Mathf.Max(0f, profile.singleWeight) +
            Mathf.Max(0f, profile.twoNoteWeight) +
            Mathf.Max(0f, profile.threeNoteWeight) +
            Mathf.Max(0f, profile.fourNoteWeight);
    }

    private float GetPercentage(
        float weight,
        float total
    )
    {
        if (total <= 0f)
            return 0f;

        return
            Mathf.Max(0f, weight) /
            total *
            100f;
    }

    private void ScheduleNextGroup(
        float lastTargetTime,
        DifficultyProfile profile
    )
    {
        float pause =
            UnityEngine.Random.Range(
                profile.minPauseBetweenGroups,
                profile.maxPauseBetweenGroups
            );

        /*
         * La siguiente tanda aparece después de que la última
         * nota de la tanda actual haya llegado, más un descanso.
         *
         * De momento evitamos solapar dos grupos distintos.
         */
        nextGroupSpawnTime =
            lastTargetTime +
            pause;
    }

    // =====================================================
    // NOTE CREATION / MOVEMENT
    // =====================================================

    private void CreateAndSpawnNote(
        float targetTime
    )
    {
        NoteData note =
            new NoteData();

        note.targetTime =
            targetTime;

        note.attackType =
            (AttackType)
            UnityEngine.Random.Range(
                1,
                5
            );

        RhythmNoteView newView =
            Instantiate(
                notePrefab,
                notesContainer
            );

        newView.Setup(
            note.attackType
        );

        note.view =
            newView;

        notes.Add(note);

        // EvaluateAttack siempre usa notes[0].
        notes.Sort(
            (a, b) =>
                a.targetTime.CompareTo(
                    b.targetTime
                )
        );

        // Toda la ráfaga se vuelve visible en el mismo frame.
        UpdateSingleNotePosition(
            note
        );
    }

    private void UpdateNotes()
    {
        for (
            int i = 0;
            i < notes.Count;
            i++
        )
        {
            UpdateSingleNotePosition(
                notes[i]
            );
        }
    }

    private void UpdateSingleNotePosition(
        NoteData note
    )
    {
        if (
            note.view == null ||
            activeDifficulty == null
        )
        {
            return;
        }

        float timeUntilHit =
            note.targetTime -
            duelTimer;

        float progress =
            1f -
            (
                timeUntilHit /
                activeDifficulty.noteTravelTime
            );

        note.view.SetPosition(
            spawnPoint.anchoredPosition,
            hitPoint.anchoredPosition,
            progress
        );
    }

    // =====================================================
    // PLAYER INPUT
    // =====================================================

    public void PressAttack(
        AttackType attack
    )
    {
        if (
            currentState !=
            DuelState.Playing
        )
        {
            return;
        }

        if (notes.Count == 0)
            return;

        EvaluateAttack(attack);
    }

    private void EvaluateAttack(
        AttackType pressedAttack
    )
    {
        NoteData currentNote =
            notes[0];

        float timingOffset =
            duelTimer -
            currentNote.targetTime;

        float absoluteOffset =
            Mathf.Abs(
                timingOffset
            );

        // EARLY
        if (
            timingOffset <
            -goodWindow
        )
        {
            ConsumeCurrentNoteError();

            DamagePlayer(
                earlyDamageToPlayer
            );

            ShowFeedback(
                "EARLY!"
            );

            if (juiceController != null)
            {
                juiceController.PlayErrorFeedback();
            }

            return;
        }

        // WRONG BUTTON
        if (
            pressedAttack !=
            currentNote.attackType
        )
        {
            ConsumeCurrentNoteError();

            DamagePlayer(
                wrongDamageToPlayer
            );

            ShowFeedback(
                "WRONG!"
            );

            if (juiceController != null)
            {
                juiceController.PlayErrorFeedback();
            }

            return;
        }

        // PERFECT
        if (
            absoluteOffset <=
            perfectWindow
        )
        {
            ConsumeCurrentNoteSuccess(
                true
            );

            DamageEnemy(
                perfectDamage,
                true
            );

            ShowFeedback(
                "PERFECT!"
            );

            if (juiceController != null)
            {
                juiceController.PlayPerfectFeedback();
            }

            return;
        }

        // GOOD
        if (
            absoluteOffset <=
            goodWindow
        )
        {
            ConsumeCurrentNoteSuccess(
                false
            );

            DamageEnemy(
                goodDamage,
                false
            );

            ShowFeedback(
                "GOOD!"
            );

            if (juiceController != null)
            {
                juiceController.PlayGoodFeedback();
            }

            return;
        }

        // LATE
        ConsumeCurrentNoteError();

        DamagePlayer(
            missDamageToPlayer
        );

        ShowFeedback(
            "MISS!"
        );

        if (juiceController != null)
        {
            juiceController.PlayErrorFeedback();
        }
    }

    private void ProcessMissedNotes()
    {
        while (
            notes.Count > 0 &&
            duelTimer >
            notes[0].targetTime +
            goodWindow &&
            currentState ==
            DuelState.Playing
        )
        {
            ConsumeCurrentNoteError();

            DamagePlayer(
                missDamageToPlayer
            );

            ShowFeedback(
                "MISS!"
            );

            if (juiceController != null)
            {
                juiceController.PlayErrorFeedback();
            }
        }
    }

    private void ConsumeCurrentNoteSuccess(
        bool perfect
    )
    {
        if (notes.Count == 0)
            return;

        NoteData note =
            notes[0];

        notes.RemoveAt(0);

        if (note.view != null)
        {
            note.view.PlaySuccessAndDestroy(
                perfect
            );
        }

        /*
         * Importante:
         * consumir una nota NO genera la siguiente.
         * Los grupos siguen siendo independientes del input del jugador.
         */
    }

    private void ConsumeCurrentNoteError()
    {
        if (notes.Count == 0)
            return;

        NoteData note =
            notes[0];

        notes.RemoveAt(0);

        if (note.view != null)
        {
            note.view.PlayErrorAndDestroy();
        }

        /*
         * Importante:
         * consumir una nota NO genera la siguiente.
         * Los grupos siguen siendo independientes del input del jugador.
         */
    }

    // =====================================================
    // HEALTH
    // =====================================================

    private void DamageEnemy(
        float damage
    )
    {
        DamageEnemy(
            damage,
            false
        );
    }

    private void DamageEnemy(
        float damage,
        bool perfect
    )
    {
        enemyHealth -=
            damage;

        enemyHealth =
            Mathf.Max(
                enemyHealth,
                0f
            );

        if (juiceController != null)
        {
            juiceController.AnimateHealth(
                enemyHealthSlider,
                enemyHealth
            );

            juiceController.PlayEnemyDamage(
                damage,
                perfect
            );
        }
        else
        {
            UpdateHealthUI();
        }

        if (enemyHealth <= 0f)
        {
            WinDuel();
        }
    }

    private void DamagePlayer(
        float damage
    )
    {
        playerHealth -=
            damage;

        playerHealth =
            Mathf.Max(
                playerHealth,
                0f
            );

        if (juiceController != null)
        {
            juiceController.AnimateHealth(
                playerHealthSlider,
                playerHealth
            );

            juiceController.PlayPlayerDamage(
                damage
            );
        }
        else
        {
            UpdateHealthUI();
        }

        if (playerHealth <= 0f)
        {
            LoseDuel();
        }
    }

    private void SetHealthUIImmediate()
    {
        if (
            playerHealthSlider != null
        )
        {
            playerHealthSlider.maxValue =
                playerMaxHealth;
        }

        if (
            enemyHealthSlider != null
        )
        {
            enemyHealthSlider.maxValue =
                enemyMaxHealth;
        }

        if (juiceController != null)
        {
            juiceController.SetHealthImmediate(
                playerHealthSlider,
                playerHealth
            );

            juiceController.SetHealthImmediate(
                enemyHealthSlider,
                enemyHealth
            );
        }
        else
        {
            UpdateHealthUI();
        }
    }

    private void UpdateHealthUI()
    {
        if (
            playerHealthSlider != null
        )
        {
            playerHealthSlider.maxValue =
                playerMaxHealth;

            playerHealthSlider.value =
                playerHealth;
        }

        if (
            enemyHealthSlider != null
        )
        {
            enemyHealthSlider.maxValue =
                enemyMaxHealth;

            enemyHealthSlider.value =
                enemyHealth;
        }
    }

    // =====================================================
    // RESULT
    // =====================================================

    private void WinDuel()
    {
        // Evita ejecutar dos veces la victoria en el mismo frame.
        if (currentState == DuelState.Won)
            return;

        enemiesDefeated++;

        /*
         * Bloqueamos el gameplay inmediatamente.
         * De esta forma ningún input posterior puede afectar
         * las notas que quedaban en la ráfaga.
         */
        currentState =
            DuelState.Won;

        restartTimer =
            restartDelay;

        victoryRevealTimer =
            victoryRevealDelay;

        victoryMessageShown =
            false;

        /*
         * Las notas restantes ya no son GOOD, PERFECT ni MISS:
         * el combate terminó. Se cancelan visualmente con un
         * fade neutro y se eliminan de la lista lógica.
         */
        CancelPendingNotes();
    }

    private void LoseDuel()
    {
        if (currentState == DuelState.Lost)
            return;

        currentState =
            DuelState.Lost;

        restartTimer =
            restartDelay;

        /*
         * También limpiamos notas pendientes al morir el jugador,
         * evitando que queden congeladas durante DEFEAT.
         */
        CancelPendingNotes();

        ShowFeedback(
            "DEFEAT!"
        );
    }

    private void UpdateFinishedDuel()
    {
        /*
         * En victoria dejamos respirar el golpe final antes de
         * mostrar el mensaje. Esto ocurre aunque Auto Restart
         * esté desactivado.
         */
        if (
            currentState == DuelState.Won &&
            !victoryMessageShown
        )
        {
            victoryRevealTimer -=
                Time.deltaTime;

            if (victoryRevealTimer <= 0f)
            {
                victoryMessageShown =
                    true;

                ShowFeedback(
                    "VICTORY!"
                );
            }
        }

        if (!autoRestart)
            return;

        restartTimer -=
            Time.deltaTime;

        if (
            restartTimer <= 0f
        )
        {
            StartDuel();
        }
    }

    private void CancelPendingNotes()
    {
        if (notes.Count == 0)
            return;

        /*
         * No usamos ConsumeCurrentNoteError/Success porque estas
         * notas no fueron acertadas ni falladas. Son simplemente
         * canceladas por el fin del combate.
         */
        for (int i = 0; i < notes.Count; i++)
        {
            NoteData note =
                notes[i];

            if (note.view != null)
            {
                note.view
                    .PlayCancelledAndDestroy();
            }
        }

        /*
         * Es importante vaciar la lista inmediatamente.
         * Aunque el fade visual dure unas décimas, a nivel de
         * gameplay ya no existe ninguna nota pendiente.
         */
        notes.Clear();
    }

    // =====================================================
    // FEEDBACK
    // =====================================================

    private void ShowFeedback(
        string message
    )
    {
        if (
            feedbackText == null
        )
        {
            return;
        }

        feedbackText.text =
            message;

        feedbackTimer =
            feedbackDuration;
    }

    private void UpdateFeedback()
    {
        if (
            feedbackTimer <= 0f
        )
        {
            return;
        }

        feedbackTimer -=
            Time.deltaTime;

        if (
            feedbackTimer <= 0f &&
            feedbackText != null
        )
        {
            feedbackText.text =
                "";
        }
    }

    // =====================================================
    // KEYBOARD
    // =====================================================

    private void ReadKeyboardInput()
    {
        if (
            Keyboard.current == null
        )
        {
            return;
        }

        if (
            Keyboard.current
            .digit1Key
            .wasPressedThisFrame
        )
        {
            PressAttack(
                AttackType.Attack1
            );
        }

        if (
            Keyboard.current
            .digit2Key
            .wasPressedThisFrame
        )
        {
            PressAttack(
                AttackType.Attack2
            );
        }

        if (
            Keyboard.current
            .digit3Key
            .wasPressedThisFrame
        )
        {
            PressAttack(
                AttackType.Attack3
            );
        }

        if (
            Keyboard.current
            .digit4Key
            .wasPressedThisFrame
        )
        {
            PressAttack(
                AttackType.Attack4
            );
        }
    }

    private void ClearNotes()
    {
        foreach (
            NoteData note
            in notes
        )
        {
            if (note.view != null)
            {
                Destroy(
                    note.view.gameObject
                );
            }
        }

        notes.Clear();
    }

    // =====================================================
    // RECOMMENDED CURVE
    // =====================================================

    [ContextMenu("Apply Recommended Difficulty Curve")]
    private void ApplyRecommendedDifficultyCurve()
    {
        difficultyCurve =
            CreateRecommendedDifficultyCurve();

        firstGroupDelay =
            0.65f;

        minimumBurstReactionTime =
            0.65f;

        perfectWindow =
            0.07f;

        goodWindow =
            0.16f;

        countdownStepDuration =
            0.8f;

        goDuration =
            0.45f;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(
            this
        );
#endif
    }

    private List<DifficultyProfile>
        CreateRecommendedDifficultyCurve()
    {
        return new List<DifficultyProfile>
        {
            // E1 - Learn timing + buttons.
            CreateProfile(
                1, 2.80f,
                100f, 0f, 0f, 0f,
                0.70f, 0.76f,
                0.95f, 1.25f
            ),

            // E2 - Introduce x2 gently.
            CreateProfile(
                2, 2.80f,
                70f, 30f, 0f, 0f,
                0.68f, 0.74f,
                0.85f, 1.15f
            ),

            // E3 - x2 becomes common.
            CreateProfile(
                3, 2.70f,
                40f, 60f, 0f, 0f,
                0.62f, 0.68f,
                0.75f, 1.05f
            ),

            // E4 - First meaningful speed increase.
            CreateProfile(
                4, 2.55f,
                20f, 80f, 0f, 0f,
                0.59f, 0.65f,
                0.70f, 0.95f
            ),

            // E5 - Introduce x3.
            CreateProfile(
                5, 2.55f,
                20f, 60f, 20f, 0f,
                0.57f, 0.63f,
                0.65f, 0.90f
            ),

            // E6 - Consolidate x3.
            CreateProfile(
                6, 2.45f,
                0f, 50f, 50f, 0f,
                0.55f, 0.61f,
                0.60f, 0.85f
            ),

            // E7 - More x3 + more speed.
            CreateProfile(
                7, 2.30f,
                0f, 40f, 60f, 0f,
                0.52f, 0.58f,
                0.55f, 0.80f
            ),

            // E8 - First rare x4.
            CreateProfile(
                8, 2.30f,
                0f, 45f, 45f, 10f,
                0.50f, 0.55f,
                0.50f, 0.75f
            ),

            // E9 - x4 becomes relevant.
            CreateProfile(
                9, 2.20f,
                0f, 30f, 50f, 20f,
                0.48f, 0.52f,
                0.48f, 0.72f
            ),

            // E10+ - Target skill ceiling.
            CreateProfile(
                10, 2.10f,
                0f, 25f, 45f, 30f,
                0.46f, 0.50f,
                0.45f, 0.70f
            )
        };
    }

    private DifficultyProfile CreateProfile(
        int startsAtEnemy,
        float travelTime,
        float singleWeight,
        float twoWeight,
        float threeWeight,
        float fourWeight,
        float minSpacing,
        float maxSpacing,
        float minPause,
        float maxPause
    )
    {
        return new DifficultyProfile
        {
            startsAtEnemy =
                startsAtEnemy,

            noteTravelTime =
                travelTime,

            singleWeight =
                singleWeight,

            twoNoteWeight =
                twoWeight,

            threeNoteWeight =
                threeWeight,

            fourNoteWeight =
                fourWeight,

            minNoteSpacing =
                minSpacing,

            maxNoteSpacing =
                maxSpacing,

            minPauseBetweenGroups =
                minPause,

            maxPauseBetweenGroups =
                maxPause
        };
    }

    private void EnsureDifficultyCurveExists()
    {
        if (
            difficultyCurve != null &&
            difficultyCurve.Count > 0
        )
        {
            return;
        }

        difficultyCurve =
            CreateRecommendedDifficultyCurve();
    }

    private void Reset()
    {
        difficultyCurve =
            CreateRecommendedDifficultyCurve();
    }

    // =====================================================
    // VALIDATION
    // =====================================================

    private void OnValidate()
    {
        playerMaxHealth =
            Mathf.Max(
                1f,
                playerMaxHealth
            );

        enemyMaxHealth =
            Mathf.Max(
                1f,
                enemyMaxHealth
            );

        perfectWindow =
            Mathf.Max(
                0.01f,
                perfectWindow
            );

        goodWindow =
            Mathf.Max(
                perfectWindow,
                goodWindow
            );

        firstGroupDelay =
            Mathf.Max(
                0f,
                firstGroupDelay
            );

        minimumBurstReactionTime =
            Mathf.Max(
                goodWindow + 0.1f,
                minimumBurstReactionTime
            );

        countdownStepDuration =
            Mathf.Max(
                0.05f,
                countdownStepDuration
            );

        goDuration =
            Mathf.Max(
                0.05f,
                goDuration
            );

        victoryRevealDelay =
            Mathf.Max(
                0f,
                victoryRevealDelay
            );

        if (difficultyCurve == null)
            return;

        foreach (
            DifficultyProfile profile
            in difficultyCurve
        )
        {
            if (profile == null)
                continue;

            profile.startsAtEnemy =
                Mathf.Max(
                    1,
                    profile.startsAtEnemy
                );

            profile.noteTravelTime =
                Mathf.Max(
                    0.5f,
                    profile.noteTravelTime
                );

            profile.singleWeight =
                Mathf.Max(
                    0f,
                    profile.singleWeight
                );

            profile.twoNoteWeight =
                Mathf.Max(
                    0f,
                    profile.twoNoteWeight
                );

            profile.threeNoteWeight =
                Mathf.Max(
                    0f,
                    profile.threeNoteWeight
                );

            profile.fourNoteWeight =
                Mathf.Max(
                    0f,
                    profile.fourNoteWeight
                );

            profile.minNoteSpacing =
                Mathf.Max(
                    0.12f,
                    profile.minNoteSpacing
                );

            profile.maxNoteSpacing =
                Mathf.Max(
                    profile.minNoteSpacing,
                    profile.maxNoteSpacing
                );

            profile.minPauseBetweenGroups =
                Mathf.Max(
                    0f,
                    profile.minPauseBetweenGroups
                );

            profile.maxPauseBetweenGroups =
                Mathf.Max(
                    profile.minPauseBetweenGroups,
                    profile.maxPauseBetweenGroups
                );
        }
    }
}