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

    private class NoteData
    {
        public float targetTime;
        public AttackType attackType;
        public RhythmNoteView view;
    }

    [Header("Health")]
    [SerializeField] private float playerMaxHealth = 100f;
    [SerializeField] private float enemyMaxHealth = 100f;

    [Header("Damage")]
    [SerializeField] private float goodDamage = 15f;
    [SerializeField] private float perfectDamage = 25f;
    [SerializeField] private float earlyDamageToPlayer = 15f;
    [SerializeField] private float wrongDamageToPlayer = 15f;
    [SerializeField] private float missDamageToPlayer = 10f;

    [Header("Enemy 1 - Single Notes")]
    [Tooltip("Tiempo de espera después de ¡YA! antes de que aparezca la primera nota.")]
    [SerializeField] private float firstNoteDelay = 0.65f;

    [Tooltip("Pausa mínima entre una nota individual y la siguiente.")]
    [SerializeField] private float minSingleNotePause = 0.45f;

    [Tooltip("Pausa máxima entre una nota individual y la siguiente.")]
    [SerializeField] private float maxSingleNotePause = 0.90f;

    [Header("Bursts - Enemy 2+")]
    [Tooltip("Número de enemigo a partir del cual aparecen ráfagas.")]
    [SerializeField] private int burstStartEnemy = 2;

    [Tooltip("Cantidad mínima de notas que aparecen juntas.")]
    [SerializeField] private int minNotesPerBurst = 2;

    [Tooltip("Cantidad máxima de notas que aparecen juntas.")]
    [SerializeField] private int maxNotesPerBurst = 4;

    [Tooltip("Separación temporal mínima entre las notas de una misma ráfaga.")]
    [SerializeField] private float minBurstNoteInterval = 0.38f;

    [Tooltip("Separación temporal máxima entre las notas de una misma ráfaga.")]
    [SerializeField] private float maxBurstNoteInterval = 0.48f;

    [Tooltip("Tiempo mínimo para reaccionar a la primera nota de una ráfaga.")]
    [SerializeField] private float minimumBurstReactionTime = 0.75f;

    [Tooltip("Pausa mínima después de que termina una ráfaga antes de que aparezca la siguiente.")]
    [SerializeField] private float minPauseBetweenBursts = 0.45f;

    [Tooltip("Pausa máxima después de que termina una ráfaga antes de que aparezca la siguiente.")]
    [SerializeField] private float maxPauseBetweenBursts = 0.85f;

    [Header("Progressive Difficulty")]
    [Tooltip("Tiempo de viaje inicial de una nota desde SpawnPoint hasta HitPoint.")]
    [SerializeField] private float noteTravelTime = 2.60f;

    [Tooltip("Cada cuántos enemigos derrotados aumenta la velocidad.")]
    [SerializeField] private int enemiesPerSpeedIncrease = 2;

    [Tooltip("Cuántos segundos se reduce el tiempo de viaje por cada aumento de dificultad.")]
    [SerializeField] private float travelTimeReductionPerTier = 0.12f;

    [Tooltip("Límite mínimo del tiempo de viaje.")]
    [SerializeField] private float minimumNoteTravelTime = 2.00f;

    [Tooltip("Opcional: muestra el número del enemigo actual.")]
    [SerializeField] private TMP_Text enemyCounterText;

    [Tooltip("Opcional: muestra datos de dificultad para debug.")]
    [SerializeField] private TMP_Text difficultyText;

    [Header("Timing Windows")]
    [Tooltip("Ventana PERFECT en segundos.")]
    [SerializeField] private float perfectWindow = 0.07f;

    [Tooltip("Ventana GOOD en segundos.")]
    [SerializeField] private float goodWindow = 0.16f;

    [Header("Rhythm Lane")]
    [SerializeField] private RectTransform notesContainer;

    [Tooltip("Lugar donde aparecen las notas.")]
    [SerializeField] private RectTransform spawnPoint;

    [Tooltip("Lugar exacto donde debemos tocar.")]
    [SerializeField] private RectTransform hitPoint;

    [SerializeField] private RhythmNoteView notePrefab;

    [Header("Health UI")]
    [SerializeField] private Slider playerHealthSlider;
    [SerializeField] private Slider enemyHealthSlider;

    [Header("Feedback")]
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private float feedbackDuration = 0.5f;

    [Header("Countdown")]
    [Tooltip("Texto grande para mostrar 3, 2, 1, ¡YA!")]
    [SerializeField] private TMP_Text countdownText;

    [Tooltip("Duración de cada número.")]
    [SerializeField] private float countdownStepDuration = 0.8f;

    [Tooltip("Tiempo que permanece ¡YA! antes de comenzar.")]
    [SerializeField] private float goDuration = 0.45f;

    [Header("Round")]
    [SerializeField] private bool autoRestart = true;
    [SerializeField] private float restartDelay = 2f;

    [Header("PC Debug")]
    [Tooltip("Permite usar 1,2,3,4 en teclado.")]
    [SerializeField] private bool enableKeyboardInput = true;

    private float playerHealth;
    private float enemyHealth;

    private float duelTimer;
    private float restartTimer;
    private float feedbackTimer;

    private float countdownTimer;
    private int countdownValue;

    private int enemiesDefeated;
    private float currentNoteTravelTime;

    // ÚNICO reloj encargado de decidir cuándo aparece el próximo grupo.
    private float nextGroupSpawnTime;

    private DuelState currentState;

    private readonly List<NoteData> notes =
        new List<NoteData>();

    private void Start()
    {
        enemiesDefeated = 0;
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

                // Input antes de MISS para favorecer al jugador en el frame límite.
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

    private void StartDuel()
    {
        ClearNotes();

        playerHealth = playerMaxHealth;
        enemyHealth = enemyMaxHealth;

        duelTimer = 0f;
        nextGroupSpawnTime = 0f;

        UpdateDifficulty();
        UpdateHealthUI();
        UpdateProgressUI();

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

        // Ya no existe GenerateInitialNotes().
        // El grupo aparece completo cuando llega este momento.
        nextGroupSpawnTime = firstNoteDelay;
    }

    // =====================================================
    // DIFFICULTY
    // =====================================================

    private void UpdateDifficulty()
    {
        int safeEnemiesPerIncrease =
            Mathf.Max(1, enemiesPerSpeedIncrease);

        int difficultyTier =
            enemiesDefeated / safeEnemiesPerIncrease;

        currentNoteTravelTime = Mathf.Max(
            minimumNoteTravelTime,
            noteTravelTime -
            (difficultyTier * travelTimeReductionPerTier)
        );
    }

    private void UpdateProgressUI()
    {
        int currentEnemy = enemiesDefeated + 1;

        if (enemyCounterText != null)
        {
            enemyCounterText.text =
                $"ENEMIGO {currentEnemy}";
        }

        if (difficultyText != null)
        {
            int safeEnemiesPerIncrease =
                Mathf.Max(1, enemiesPerSpeedIncrease);

            int difficultyTier =
                enemiesDefeated / safeEnemiesPerIncrease;

            string generationMode =
                currentEnemy >= burstStartEnemy
                    ? $"RÁFAGA {minNotesPerBurst}-{maxNotesPerBurst}"
                    : "SIMPLE";

            difficultyText.text =
                $"VEL. {difficultyTier + 1} | {currentNoteTravelTime:0.00}s | {generationMode}";
        }
    }

    // =====================================================
    // GROUP GENERATOR
    // =====================================================

    private void UpdateGroupGenerator()
    {
        if (duelTimer < nextGroupSpawnTime)
            return;

        int currentEnemy = enemiesDefeated + 1;

        if (currentEnemy < burstStartEnemy)
        {
            SpawnSingleNoteGroup();
        }
        else
        {
            SpawnBurstGroup();
        }
    }

    // ENEMY 1
    private void SpawnSingleNoteGroup()
    {
        float targetTime =
            duelTimer + currentNoteTravelTime;

        CreateAndSpawnNote(targetTime);

        float pause =
            Random.Range(
                minSingleNotePause,
                maxSingleNotePause
            );

        // La siguiente nota no aparece hasta que ésta terminó.
        nextGroupSpawnTime =
            targetTime + pause;
    }

    // ENEMY 2+
    private void SpawnBurstGroup()
    {
        int amount =
            Random.Range(
                minNotesPerBurst,
                maxNotesPerBurst + 1
            );

        amount = Mathf.Clamp(amount, 2, 4);

        /*
         * ESTA ES LA DIFERENCIA CLAVE:
         *
         * Las 2-4 notas se instancian AHORA, dentro de este mismo método
         * y por lo tanto en el mismo frame.
         *
         * La última queda en SpawnPoint.
         * Las anteriores aparecen adelantadas en la pista.
         *
         * Ejemplo:
         *
         * HIT                           SPAWN
         *  |          [3] [1] [4] [2]    |
         * ------------------------------------------------
         */

        float configuredInterval =
            Random.Range(
                minBurstNoteInterval,
                maxBurstNoteInterval
            );

        // Garantizamos que la primera nota nunca aparezca demasiado
        // cerca de la HitZone cuando la velocidad aumenta.
        float maxSafeInterval =
            configuredInterval;

        if (amount > 1)
        {
            maxSafeInterval =
                (currentNoteTravelTime - minimumBurstReactionTime) /
                (amount - 1);

            maxSafeInterval =
                Mathf.Max(0.12f, maxSafeInterval);
        }

        float burstInterval =
            Mathf.Min(
                configuredInterval,
                maxSafeInterval
            );

        for (int i = 0; i < amount; i++)
        {
            int notesAfterThis =
                amount - 1 - i;

            float targetTime =
                duelTimer +
                currentNoteTravelTime -
                (notesAfterThis * burstInterval);

            // La vista se instancia INMEDIATAMENTE.
            CreateAndSpawnNote(targetTime);
        }

        float lastTargetTime =
            duelTimer + currentNoteTravelTime;

        float pause =
            Random.Range(
                minPauseBetweenBursts,
                maxPauseBetweenBursts
            );

        // Por default no superponemos dos ráfagas.
        nextGroupSpawnTime =
            lastTargetTime + pause;
    }

    // =====================================================
    // NOTE CREATION / MOVEMENT
    // =====================================================

    private void CreateAndSpawnNote(float targetTime)
    {
        NoteData note =
            new NoteData();

        note.targetTime = targetTime;

        note.attackType =
            (AttackType)Random.Range(1, 5);

        RhythmNoteView newView =
            Instantiate(
                notePrefab,
                notesContainer
            );

        newView.Setup(
            note.attackType
        );

        note.view = newView;

        notes.Add(note);

        // El input siempre evalúa notes[0], por eso debe estar ordenada.
        notes.Sort(
            (a, b) =>
                a.targetTime.CompareTo(b.targetTime)
        );

        // Hace que la ráfaga completa sea visible en ESTE frame.
        UpdateSingleNotePosition(note);
    }

    private void UpdateNotes()
    {
        for (int i = 0; i < notes.Count; i++)
        {
            UpdateSingleNotePosition(notes[i]);
        }
    }

    private void UpdateSingleNotePosition(NoteData note)
    {
        if (note.view == null)
            return;

        float timeUntilHit =
            note.targetTime - duelTimer;

        float progress =
            1f -
            (timeUntilHit / currentNoteTravelTime);

        note.view.SetPosition(
            spawnPoint.anchoredPosition,
            hitPoint.anchoredPosition,
            progress
        );
    }

    // =====================================================
    // INPUT / TIMING
    // =====================================================

    public void PressAttack(AttackType attack)
    {
        if (currentState != DuelState.Playing)
            return;

        if (notes.Count == 0)
            return;

        EvaluateAttack(attack);
    }

    private void EvaluateAttack(AttackType pressedAttack)
    {
        NoteData currentNote = notes[0];

        float timingOffset =
            duelTimer -
            currentNote.targetTime;

        float absoluteOffset =
            Mathf.Abs(timingOffset);

        if (timingOffset < -goodWindow)
        {
            DamagePlayer(
                earlyDamageToPlayer
            );

            ShowFeedback("EARLY!");
            ConsumeCurrentNote();
            return;
        }

        if (
            pressedAttack !=
            currentNote.attackType
        )
        {
            DamagePlayer(
                wrongDamageToPlayer
            );

            ShowFeedback("WRONG!");
            ConsumeCurrentNote();
            return;
        }

        if (
            absoluteOffset <=
            perfectWindow
        )
        {
            DamageEnemy(
                perfectDamage
            );

            ShowFeedback("PERFECT!");
            ConsumeCurrentNote();
            return;
        }

        if (
            absoluteOffset <=
            goodWindow
        )
        {
            DamageEnemy(
                goodDamage
            );

            ShowFeedback("GOOD!");
            ConsumeCurrentNote();
            return;
        }

        DamagePlayer(
            missDamageToPlayer
        );

        ShowFeedback("MISS!");
        ConsumeCurrentNote();
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
            DamagePlayer(
                missDamageToPlayer
            );

            ShowFeedback("MISS!");
            ConsumeCurrentNote();
        }
    }

    private void ConsumeCurrentNote()
    {
        if (notes.Count == 0)
            return;

        NoteData note =
            notes[0];

        if (note.view != null)
        {
            Destroy(
                note.view.gameObject
            );
        }

        notes.RemoveAt(0);

        /*
         * MUY IMPORTANTE:
         *
         * NO llamamos GenerateNextNote().
         * NO generamos otra nota al acertar/fallar.
         *
         * La generación es independiente del input.
         * UpdateGroupGenerator() crea el próximo grupo completo.
         */
    }

    // =====================================================
    // HEALTH / RESULTS
    // =====================================================

    private void DamageEnemy(float damage)
    {
        enemyHealth -= damage;

        enemyHealth =
            Mathf.Max(
                enemyHealth,
                0f
            );

        UpdateHealthUI();

        if (enemyHealth <= 0f)
        {
            WinDuel();
        }
    }

    private void DamagePlayer(float damage)
    {
        playerHealth -= damage;

        playerHealth =
            Mathf.Max(
                playerHealth,
                0f
            );

        UpdateHealthUI();

        if (playerHealth <= 0f)
        {
            LoseDuel();
        }
    }

    private void UpdateHealthUI()
    {
        if (playerHealthSlider != null)
        {
            playerHealthSlider.maxValue =
                playerMaxHealth;

            playerHealthSlider.value =
                playerHealth;
        }

        if (enemyHealthSlider != null)
        {
            enemyHealthSlider.maxValue =
                enemyMaxHealth;

            enemyHealthSlider.value =
                enemyHealth;
        }
    }

    private void WinDuel()
    {
        enemiesDefeated++;

        currentState =
            DuelState.Won;

        restartTimer =
            restartDelay;

        ShowFeedback("VICTORY!");
    }

    private void LoseDuel()
    {
        currentState =
            DuelState.Lost;

        restartTimer =
            restartDelay;

        ShowFeedback("DEFEAT!");
    }

    private void UpdateFinishedDuel()
    {
        if (!autoRestart)
            return;

        restartTimer -=
            Time.deltaTime;

        if (restartTimer <= 0f)
        {
            StartDuel();
        }
    }

    // =====================================================
    // FEEDBACK
    // =====================================================

    private void ShowFeedback(
        string message
    )
    {
        if (feedbackText == null)
            return;

        feedbackText.text =
            message;

        feedbackTimer =
            feedbackDuration;
    }

    private void UpdateFeedback()
    {
        if (feedbackTimer <= 0f)
            return;

        feedbackTimer -=
            Time.deltaTime;

        if (feedbackTimer <= 0f)
        {
            if (feedbackText != null)
            {
                feedbackText.text = "";
            }
        }
    }

    // =====================================================
    // KEYBOARD
    // =====================================================

    private void ReadKeyboardInput()
    {
        if (Keyboard.current == null)
            return;

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
        foreach (NoteData note in notes)
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
    // DEFAULT BALANCE
    // =====================================================

    [ContextMenu("Apply Recommended Balance Defaults")]
    private void ApplyRecommendedBalanceDefaults()
    {
        firstNoteDelay = 0.65f;
        minSingleNotePause = 0.45f;
        maxSingleNotePause = 0.90f;

        burstStartEnemy = 2;
        minNotesPerBurst = 2;
        maxNotesPerBurst = 4;

        minBurstNoteInterval = 0.38f;
        maxBurstNoteInterval = 0.48f;
        minimumBurstReactionTime = 0.75f;

        minPauseBetweenBursts = 0.45f;
        maxPauseBetweenBursts = 0.85f;

        noteTravelTime = 2.60f;
        enemiesPerSpeedIncrease = 2;
        travelTimeReductionPerTier = 0.12f;
        minimumNoteTravelTime = 2.00f;

        perfectWindow = 0.07f;
        goodWindow = 0.16f;

        countdownStepDuration = 0.8f;
        goDuration = 0.45f;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

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

        firstNoteDelay =
            Mathf.Max(
                0f,
                firstNoteDelay
            );

        minSingleNotePause =
            Mathf.Max(
                0f,
                minSingleNotePause
            );

        maxSingleNotePause =
            Mathf.Max(
                minSingleNotePause,
                maxSingleNotePause
            );

        burstStartEnemy =
            Mathf.Max(
                2,
                burstStartEnemy
            );

        minNotesPerBurst =
            Mathf.Clamp(
                minNotesPerBurst,
                2,
                4
            );

        maxNotesPerBurst =
            Mathf.Clamp(
                maxNotesPerBurst,
                minNotesPerBurst,
                4
            );

        minBurstNoteInterval =
            Mathf.Max(
                0.12f,
                minBurstNoteInterval
            );

        maxBurstNoteInterval =
            Mathf.Max(
                minBurstNoteInterval,
                maxBurstNoteInterval
            );

        minimumBurstReactionTime =
            Mathf.Max(
                goodWindow + 0.1f,
                minimumBurstReactionTime
            );

        minPauseBetweenBursts =
            Mathf.Max(
                0f,
                minPauseBetweenBursts
            );

        maxPauseBetweenBursts =
            Mathf.Max(
                minPauseBetweenBursts,
                maxPauseBetweenBursts
            );

        noteTravelTime =
            Mathf.Max(
                0.5f,
                noteTravelTime
            );

        enemiesPerSpeedIncrease =
            Mathf.Max(
                1,
                enemiesPerSpeedIncrease
            );

        travelTimeReductionPerTier =
            Mathf.Max(
                0f,
                travelTimeReductionPerTier
            );

        minimumNoteTravelTime =
            Mathf.Clamp(
                minimumNoteTravelTime,
                0.5f,
                noteTravelTime
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
    }
}